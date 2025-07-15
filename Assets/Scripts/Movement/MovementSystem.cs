using UnityEngine;
using SayBoom.Core;

namespace SayBoom.Movement
{
    /// <summary>
    /// 移动系统 - 专门负责跳跃手感优化的独立模块
    /// 开发者可以专注于调整跳跃参数而不影响其他系统
    /// </summary>
    public class MovementSystem : MonoBehaviour, IMovementSystem
    {
        [Header("=== 移动系统配置 ===")]
        [Header("基础移动参数")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 10f;
        [SerializeField] private float deceleration = 10f;
        [SerializeField] private float airControlMultiplier = 0.7f;
        
        [Header("跳跃手感调优区域")]
        [Tooltip("跳跃力度 - 影响跳跃高度")]
        [SerializeField] private float jumpForce = 12f;
        
        [Tooltip("重力强度 - 影响下落速度")]
        [SerializeField] private float gravity = 25f;
        
        [Tooltip("最大下落速度")]
        [SerializeField] private float maxFallSpeed = 20f;
        
        [Tooltip("跳跃缓冲时间 - 提前按跳跃键的容错时间")]
        [SerializeField] private float jumpBufferTime = 0.15f;
        
        [Tooltip("土狼时间 - 离开地面后仍可跳跃的时间")]
        [SerializeField] private float coyoteTime = 0.15f;
        
        [Tooltip("可变跳跃系数 - 松开跳跃键时的速度衰减")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float variableJumpMultiplier = 0.5f;
        
        [Tooltip("着地时的速度衰减")]
        [Range(0.0f, 1.0f)]
        [SerializeField] private float landingSpeedRetention = 0.8f;
        
        [Header("地面检测")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundLayerMask = -1;
        
        [Header("调试选项")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool enableGizmos = true;
        
        // 物理组件
        private Rigidbody2D rb;
        
        // 移动状态
        private Vector2 currentInput;
        private float currentVelocityX;
        private bool wasGrounded;
        
        // 跳跃状态
        private float jumpBufferTimer;
        private float coyoteTimer;
        private bool jumpPressed;
        private bool jumpHeld;
        
        // 接口实现
        public string SystemName => "Movement System";
        public bool IsInitialized { get; private set; }
        public bool IsActive { get; set; } = true;
        
        /// <summary>
        /// 当前跳跃高度（根据跳跃力度和重力计算）
        /// </summary>
        public float CurrentJumpHeight => (jumpForce * jumpForce) / (2f * gravity);
        
        public Vector2 Velocity => rb != null ? rb.velocity : Vector2.zero;
        public bool IsGrounded { get; private set; }
        public bool IsMoving => Mathf.Abs(currentVelocityX) > 0.1f;
        
        public float MoveSpeed 
        { 
            get => moveSpeed; 
            set => moveSpeed = value; 
        }
        
        public float JumpForce 
        { 
            get => jumpForce; 
            set => jumpForce = value; 
        }
        
        public float Gravity 
        { 
            get => gravity; 
            set => gravity = value; 
        }
        
        /// <summary>
        /// 设置跳跃高度（自动计算对应的跳跃力度）
        /// </summary>
        public void SetJumpHeight(float height)
        {
            float newJumpForce = Mathf.Sqrt(2f * gravity * height);
            JumpForce = newJumpForce;
        }
        
        // 其他参数的公开访问属性
        public float CoyoteTime { get => coyoteTime; set => coyoteTime = value; }
        public float JumpBufferTime { get => jumpBufferTime; set => jumpBufferTime = value; }
        public float VariableJumpMultiplier { get => variableJumpMultiplier; set => variableJumpMultiplier = value; }
        
        // 事件
        public System.Action OnLanded { get; set; }
        public System.Action OnJumped { get; set; }
        public System.Action OnStartedMoving { get; set; }
        public System.Action OnStoppedMoving { get; set; }
        
        #region Unity生命周期
        
        void Awake()
        {
            SetupComponents();
        }
        
        void Start()
        {
            Initialize();
        }
        
        void Update()
        {
            if (!IsActive) return;
            UpdateSystem();
        }
        
        void FixedUpdate()
        {
            if (!IsActive) return;
            FixedUpdateSystem();
        }
        
        #endregion
        
        #region IGameSystem接口实现
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            SetupPhysics();
            SetupGroundCheck();
            
            IsInitialized = true;
            Debug.Log($"[MovementSystem] 初始化完成 - 跳跃手感调优就绪");
        }
        
        public void UpdateSystem()
        {
            HandleInput();
            CheckGrounded();
            UpdateTimers();
            HandleMovementEvents();
        }
        
        public void FixedUpdateSystem()
        {
            HandleMovement();
            HandleJump();
            ApplyGravity();
            ClampVelocity();
        }
        
        public void Cleanup()
        {
            // 清理资源
            OnLanded = null;
            OnJumped = null;
            OnStartedMoving = null;
            OnStoppedMoving = null;
        }
        
        #endregion
        
        #region IMovementSystem接口实现
        
        public void Move(Vector2 input)
        {
            currentInput = input;
        }
        
        public void Jump()
        {
            jumpPressed = true;
            jumpBufferTimer = jumpBufferTime;
        }
        
        public void ForceJump()
        {
            if (rb != null)
            {
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                OnJumped?.Invoke();
            }
        }
        
        public void StopMovement()
        {
            currentInput = Vector2.zero;
            currentVelocityX = 0;
            if (rb != null)
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }
        }
        
        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }
        
        #endregion
        
        #region 核心移动逻辑
        
        void HandleInput()
        {
            // 自动获取输入（也可以通过Move方法外部控制）
            if (currentInput == Vector2.zero)
            {
                currentInput.x = Input.GetAxisRaw("Horizontal");
            }
            
            // 跳跃输入
            if (Input.GetButtonDown("Jump"))
            {
                Jump();
            }
            
            jumpHeld = Input.GetButton("Jump");
        }
        
        void CheckGrounded()
        {
            wasGrounded = IsGrounded;
            IsGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayerMask);
            
            // 刚着地事件
            if (IsGrounded && !wasGrounded)
            {
                OnLanded?.Invoke();
                coyoteTimer = coyoteTime;
                
                // 着地时保持部分水平速度
                currentVelocityX *= landingSpeedRetention;
            }
        }
        
        void UpdateTimers()
        {
            // 跳跃缓冲计时器
            if (jumpBufferTimer > 0)
                jumpBufferTimer -= Time.deltaTime;
            
            // 土狼时间计时器
            if (IsGrounded)
                coyoteTimer = coyoteTime;
            else if (coyoteTimer > 0)
                coyoteTimer -= Time.deltaTime;
        }
        
        void HandleMovement()
        {
            float targetVelocity = currentInput.x * moveSpeed;
            float currentAcceleration = acceleration;
            
            // 空中控制调整
            if (!IsGrounded)
            {
                currentAcceleration *= airControlMultiplier;
            }
            
            // 平滑加速和减速
            if (Mathf.Abs(currentInput.x) > 0.1f)
            {
                currentVelocityX = Mathf.MoveTowards(currentVelocityX, targetVelocity, currentAcceleration * Time.fixedDeltaTime);
            }
            else
            {
                currentVelocityX = Mathf.MoveTowards(currentVelocityX, 0, deceleration * Time.fixedDeltaTime);
            }
            
            // 应用水平速度
            rb.velocity = new Vector2(currentVelocityX, rb.velocity.y);
        }
        
        void HandleJump()
        {
            // 检查跳跃条件：有跳跃缓冲 && (在地面 || 土狼时间内)
            if (jumpBufferTimer > 0 && coyoteTimer > 0)
            {
                // 执行跳跃
                rb.velocity = new Vector2(rb.velocity.x, jumpForce);
                
                // 清除计时器
                jumpBufferTimer = 0;
                coyoteTimer = 0;
                jumpPressed = false;
                
                OnJumped?.Invoke();
            }
            
            // 可变跳跃高度 - 松开跳跃键时减少向上速度
            if (!jumpHeld && rb.velocity.y > 0)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpMultiplier);
            }
        }
        
        void ApplyGravity()
        {
            if (!IsGrounded)
            {
                rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y - gravity * Time.fixedDeltaTime);
            }
        }
        
        void ClampVelocity()
        {
            // 限制下落速度
            if (rb.velocity.y < -maxFallSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -maxFallSpeed);
            }
        }
        
        void HandleMovementEvents()
        {
            // 开始移动事件
            if (!IsMoving && Mathf.Abs(currentVelocityX) > 0.1f)
            {
                OnStartedMoving?.Invoke();
            }
            // 停止移动事件
            else if (IsMoving && Mathf.Abs(currentVelocityX) <= 0.1f)
            {
                OnStoppedMoving?.Invoke();
            }
        }
        
        #endregion
        
        #region 初始化和设置
        
        void SetupComponents()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
        }
        
        void SetupPhysics()
        {
            rb.freezeRotation = true;
            rb.gravityScale = 0; // 使用自定义重力
            rb.drag = 0;
            rb.angularDrag = 0;
        }
        
        void SetupGroundCheck()
        {
            if (groundCheck == null)
            {
                GameObject groundCheckObj = new GameObject("GroundCheck");
                groundCheckObj.transform.SetParent(transform);
                groundCheckObj.transform.localPosition = new Vector3(0, -0.5f, 0);
                groundCheck = groundCheckObj.transform;
            }
            
            // 智能配置地面层检测
            SetupGroundLayers();
        }
        
        void SetupGroundLayers()
        {
            // 如果使用默认的-1（全部层），则配置常用地面层
            if (groundLayerMask == -1)
            {
                int layerMask = 0;
                
                // 添加常见地面层
                layerMask |= (1 << 0);  // Default层
                
                // 尝试添加Ground层
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1)
                {
                    layerMask |= (1 << groundLayer);
                }
                
                // 尝试添加Tilemap层
                int tilemapLayer = LayerMask.NameToLayer("Tilemap");
                if (tilemapLayer != -1)
                {
                    layerMask |= (1 << tilemapLayer);
                }
                
                // 尝试添加Platform层
                int platformLayer = LayerMask.NameToLayer("Platform");
                if (platformLayer != -1)
                {
                    layerMask |= (1 << platformLayer);
                }
                
                groundLayerMask = layerMask;
                Debug.Log($"[MovementSystem] 自动配置地面层检测: {groundLayerMask}");
            }
        }
        
        #endregion
        
        #region 调试和可视化
        
        void OnDrawGizmosSelected()
        {
            if (!enableGizmos) return;
            
            // 地面检测区域
            if (groundCheck != null)
            {
                Gizmos.color = IsGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
            }
            
            // 移动方向指示
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, Vector3.right * currentInput.x);
            
            // 速度向量
            if (Application.isPlaying && rb != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(transform.position, rb.velocity * 0.1f);
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== 移动系统调试信息 ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"速度: {rb.velocity:F2}");
            GUILayout.Label($"在地面: {IsGrounded}");
            GUILayout.Label($"正在移动: {IsMoving}");
            GUILayout.Label($"土狼时间: {coyoteTimer:F2}s");
            GUILayout.Label($"跳跃缓冲: {jumpBufferTimer:F2}s");
            GUILayout.Label($"输入: {currentInput}");
            GUILayout.Label($"地面层遮罩: {groundLayerMask.value}");
            GUILayout.Label($"当前跳跃高度: {CurrentJumpHeight:F2}单位");
            
            GUILayout.Space(10);
            GUILayout.Label("策划调整参数:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            GUILayout.BeginHorizontal();
            float oldJumpHeight = CurrentJumpHeight;
            GUILayout.Label($"跳跃高度: {oldJumpHeight:F1}单位");
            float newJumpHeight = GUILayout.HorizontalSlider(oldJumpHeight, 1f, 8f, GUILayout.Width(100));
            if (Mathf.Abs(newJumpHeight - oldJumpHeight) > 0.01f)
            {
                SetJumpHeight(newJumpHeight);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"重力: {gravity:F1}");
            gravity = GUILayout.HorizontalSlider(gravity, 10f, 50f, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"计算跳跃力: {jumpForce:F1}", new GUIStyle(GUI.skin.label) { fontSize = 10 });
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
    }
} 