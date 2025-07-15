using UnityEngine;
using System.Collections.Generic;
using SayBoom.Core;

namespace SayBoom.Level
{
    /// <summary>
    /// 关卡数据 - 用于保存和加载关卡配置
    /// </summary>
    [CreateAssetMenu(fileName = "LevelData", menuName = "SayBoom/Level/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("关卡基本信息")]
        public string levelName = "New Level";
        [TextArea(3, 5)]
        public string description = "关卡描述";
        public Sprite levelIcon;
        
        [Header("关卡布局")]
        public Vector3 startPosition = Vector3.zero;
        public Vector3 endPosition = new Vector3(10f, 0f, 0f);
        public Vector2 levelBounds = new Vector2(20f, 10f);
        
        [Header("平台数据")]
        public List<PlatformData> platforms = new List<PlatformData>();
        
        [Header("收集品")]
        public List<Vector3> collectiblePositions = new List<Vector3>();
        
        [Header("检查点")]
        public List<CheckpointData> checkpoints = new List<CheckpointData>();
        
        [Header("关卡设置")]
        public float timeLimit = 0f; // 0表示无时间限制
        public int requiredCollectibles = 0; // 0表示无收集要求
        public LevelDifficulty difficulty = LevelDifficulty.Normal;
    }
    
    /// <summary>
    /// 平台数据
    /// </summary>
    [System.Serializable]
    public class PlatformData
    {
        public Vector3 position;
        public Vector2 size = Vector2.one;
        public Material material;
        public bool isMoving = false;
        public Vector3 moveTarget;
        public float moveSpeed = 1f;
    }
    
    /// <summary>
    /// 检查点数据
    /// </summary>
    [System.Serializable]
    public class CheckpointData
    {
        public string name;
        public Vector3 position;
        public bool isActive = true;
    }
    
    /// <summary>
    /// 关卡难度
    /// </summary>
    public enum LevelDifficulty
    {
        Easy,
        Normal,
        Hard,
        Expert
    }
}

// 以下是关卡元素组件的实现

namespace SayBoom.Level
{
    /// <summary>
    /// 平台元素
    /// </summary>
    public class PlatformElement : MonoBehaviour, ILevelElement
    {
        public string ElementName => gameObject.name;
        public LevelElementType ElementType => LevelElementType.Platform;
        public Transform Transform => transform;
        public bool IsActive { get; set; } = true;
        
        [Header("平台设置")]
        public bool isOneWay = false;
        public bool isMoving = false;
        public Vector3 moveTarget;
        public float moveSpeed = 1f;
        
        private Vector3 startPosition;
        private bool movingToTarget = true;
        
        void Start()
        {
            Initialize();
            startPosition = transform.position;
        }
        
        void Update()
        {
            if (isMoving && IsActive)
            {
                MovePlatform();
            }
        }
        
        public void Initialize()
        {
            // 确保有2D碰撞器（移除3D碰撞器如果存在）
            var collider3D = GetComponent<Collider>();
            if (collider3D != null)
            {
                DestroyImmediate(collider3D);
            }
            
            if (GetComponent<Collider2D>() == null)
            {
                gameObject.AddComponent<BoxCollider2D>();
            }
            
            // 确保在Ground层
            if (gameObject.layer == 0) // 如果在Default层
            {
                int groundLayer = LayerMask.NameToLayer("Ground");
                if (groundLayer != -1)
                {
                    gameObject.layer = groundLayer;
                }
            }
        }
        
        public void OnPlayerEnter(GameObject player)
        {
            Debug.Log($"玩家进入平台: {ElementName}");
        }
        
        public void OnPlayerExit(GameObject player)
        {
            Debug.Log($"玩家离开平台: {ElementName}");
        }
        
        public void OnPlayerInteract(GameObject player)
        {
            // 平台一般不需要交互
        }
        
        void MovePlatform()
        {
            Vector3 target = movingToTarget ? startPosition + moveTarget : startPosition;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            
            if (Vector3.Distance(transform.position, target) < 0.1f)
            {
                movingToTarget = !movingToTarget;
            }
        }
    }
    
    /// <summary>
    /// 收集品元素
    /// </summary>
    public class CollectibleElement : MonoBehaviour, ILevelElement
    {
        public string ElementName => gameObject.name;
        public LevelElementType ElementType => LevelElementType.Collectible;
        public Transform Transform => transform;
        public bool IsActive { get; set; } = true;
        
        [Header("收集品设置")]
        public int value = 1;
        public bool isCollected = false;
        public float rotationSpeed = 90f;
        public AnimationCurve bobCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        private Vector3 startPosition;
        private float bobTimer;
        
        void Start()
        {
            Initialize();
            startPosition = transform.position;
        }
        
        void Update()
        {
            if (!isCollected && IsActive)
            {
                // 旋转动画
                transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
                
                // 上下浮动动画
                bobTimer += Time.deltaTime;
                float bobOffset = bobCurve.Evaluate((bobTimer % 2f) / 2f) * 0.5f;
                transform.position = startPosition + Vector3.up * bobOffset;
            }
        }
        
        public void Initialize()
        {
            // 确保是触发器
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }
        
        public void OnPlayerEnter(GameObject player)
        {
            if (!isCollected)
            {
                Collect(player);
            }
        }
        
        public void OnPlayerExit(GameObject player)
        {
            // 收集品不需要退出逻辑
        }
        
        public void OnPlayerInteract(GameObject player)
        {
            if (!isCollected)
            {
                Collect(player);
            }
        }
        
        void Collect(GameObject player)
        {
            isCollected = true;
            IsActive = false;
            
            // 简单的收集效果
            StartCoroutine(CollectEffect());
            
            Debug.Log($"收集品被收集: {ElementName} (价值: {value})");
        }
        
        System.Collections.IEnumerator CollectEffect()
        {
            Vector3 originalScale = transform.localScale;
            float timer = 0f;
            
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 1.5f, timer / 0.25f);
                if (timer > 0.25f) scale = Mathf.Lerp(1.5f, 0f, (timer - 0.25f) / 0.25f);
                
                transform.localScale = originalScale * scale;
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter(other.gameObject);
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter(other.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 检查点元素
    /// </summary>
    public class CheckpointElement : MonoBehaviour, ILevelElement
    {
        public string ElementName => checkpointName;
        public LevelElementType ElementType => LevelElementType.Checkpoint;
        public Transform Transform => transform;
        public bool IsActive { get; set; } = true;
        
        [Header("检查点设置")]
        public string checkpointName = "Checkpoint";
        public bool isActivated = false;
        public Color activeColor = Color.green;
        public Color inactiveColor = Color.gray;
        
        private Renderer objectRenderer;
        
        void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            objectRenderer = GetComponent<Renderer>();
            UpdateVisuals();
            
            // 确保是触发器
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }
        
        public void OnPlayerEnter(GameObject player)
        {
            if (!isActivated)
            {
                ActivateCheckpoint(player);
            }
        }
        
        public void OnPlayerExit(GameObject player)
        {
            // 检查点不需要退出逻辑
        }
        
        public void OnPlayerInteract(GameObject player)
        {
            ActivateCheckpoint(player);
        }
        
        void ActivateCheckpoint(GameObject player)
        {
            isActivated = true;
            UpdateVisuals();
            
            Debug.Log($"检查点已激活: {checkpointName}");
            
            // 这里可以保存玩家位置等
        }
        
        void UpdateVisuals()
        {
            if (objectRenderer != null)
            {
                objectRenderer.material.color = isActivated ? activeColor : inactiveColor;
            }
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter(other.gameObject);
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter(other.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 目标点元素
    /// </summary>
    public class GoalElement : MonoBehaviour, ILevelElement
    {
        public string ElementName => "Goal";
        public LevelElementType ElementType => LevelElementType.Goal;
        public Transform Transform => transform;
        public bool IsActive { get; set; } = true;
        
        [Header("目标设置")]
        public bool requireAllCollectibles = false;
        public float celebrationDuration = 2f;
        
        void Start()
        {
            Initialize();
        }
        
        public void Initialize()
        {
            // 确保是触发器
            var collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }
            
            var collider2D = GetComponent<Collider2D>();
            if (collider2D != null)
            {
                collider2D.isTrigger = true;
            }
        }
        
        public void OnPlayerEnter(GameObject player)
        {
            CompleteLevel(player);
        }
        
        public void OnPlayerExit(GameObject player)
        {
            // 目标点不需要退出逻辑
        }
        
        public void OnPlayerInteract(GameObject player)
        {
            CompleteLevel(player);
        }
        
        void CompleteLevel(GameObject player)
        {
            Debug.Log("关卡完成！");
            
            // 这里可以触发关卡完成事件
            StartCoroutine(CelebrationEffect());
        }
        
        System.Collections.IEnumerator CelebrationEffect()
        {
            float timer = 0f;
            Vector3 originalScale = transform.localScale;
            
            while (timer < celebrationDuration)
            {
                timer += Time.deltaTime;
                float scale = 1f + Mathf.Sin(timer * 10f) * 0.2f;
                transform.localScale = originalScale * scale;
                
                // 变换颜色
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color color = Color.HSVToRGB((timer / celebrationDuration) % 1f, 1f, 1f);
                    renderer.material.color = color;
                }
                
                yield return null;
            }
            
            transform.localScale = originalScale;
        }
        
        void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter(other.gameObject);
            }
        }
        
        void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerEnter(other.gameObject);
            }
        }
    }
} 