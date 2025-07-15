using UnityEngine;
using UnityEngine.Tilemaps;
using SayBoom.Core;
using SayBoom.Level;

namespace SayBoom.Level
{
    /// <summary>
    /// Tilemap支持组件 - 自动配置tilemap为可识别的地面
    /// </summary>
    [RequireComponent(typeof(Tilemap))]
    [RequireComponent(typeof(TilemapRenderer))]
    public class TilemapSupport : MonoBehaviour, ILevelElement
    {
        public string ElementName => gameObject.name;
        public LevelElementType ElementType => LevelElementType.Platform;
        public Transform Transform => transform;
        public bool IsActive { get; set; } = true;
        
        [Header("Tilemap配置")]
        [SerializeField] private bool autoSetupOnStart = true;
        [SerializeField] private bool setToGroundLayer = true;
        [SerializeField] private bool addCompositeCollider = true;
        
        private Tilemap tilemap;
        private TilemapRenderer tilemapRenderer;
        private TilemapCollider2D tilemapCollider;
        private CompositeCollider2D compositeCollider;
        
        void Start()
        {
            if (autoSetupOnStart)
            {
                Initialize();
            }
        }
        
        public void Initialize()
        {
            SetupComponents();
            SetupLayers();
            SetupColliders();
            RegisterWithLevelSystem();
            
            Debug.Log($"[TilemapSupport] Tilemap已配置为地面: {gameObject.name}");
        }
        
        void SetupComponents()
        {
            tilemap = GetComponent<Tilemap>();
            tilemapRenderer = GetComponent<TilemapRenderer>();
            tilemapCollider = GetComponent<TilemapCollider2D>();
            
            if (tilemapCollider == null)
            {
                tilemapCollider = gameObject.AddComponent<TilemapCollider2D>();
            }
        }
        
        void SetupLayers()
        {
            if (!setToGroundLayer) return;
            
            // 尝试设置到Ground层
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer == -1)
            {
                // 如果Ground层不存在，尝试Tilemap层
                groundLayer = LayerMask.NameToLayer("Tilemap");
            }
            
            if (groundLayer != -1)
            {
                gameObject.layer = groundLayer;
                Debug.Log($"[TilemapSupport] 设置到层: {LayerMask.LayerToName(groundLayer)}");
            }
            else
            {
                Debug.LogWarning("[TilemapSupport] 未找到Ground或Tilemap层，使用Default层");
            }
        }
        
        void SetupColliders()
        {
            if (!addCompositeCollider) return;
            
            // 检查是否需要添加Rigidbody2D（Composite Collider需要）
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
                rb.bodyType = RigidbodyType2D.Static; // 平台通常是静态的
            }
            
            // 添加CompositeCollider2D来合并碰撞器
            if (compositeCollider == null)
            {
                compositeCollider = gameObject.AddComponent<CompositeCollider2D>();
            }
            
            // 配置TilemapCollider2D使用CompositeCollider
            if (tilemapCollider != null)
            {
                tilemapCollider.usedByComposite = true;
            }
        }
        
        void RegisterWithLevelSystem()
        {
            // 查找并注册到关卡系统
            LevelSystem levelSystem = FindObjectOfType<LevelSystem>();
            if (levelSystem != null)
            {
                levelSystem.RegisterLevelElement(this);
            }
        }
        
        public void OnPlayerEnter(GameObject player)
        {
            // Tilemap平台通常不需要特殊的进入逻辑
        }
        
        public void OnPlayerExit(GameObject player)
        {
            // Tilemap平台通常不需要特殊的离开逻辑
        }
        
        public void OnPlayerInteract(GameObject player)
        {
            // Tilemap平台通常不支持交互
        }
        
        /// <summary>
        /// 手动设置Tilemap层级
        /// </summary>
        public void SetLayer(int layer)
        {
            gameObject.layer = layer;
        }
        
        /// <summary>
        /// 手动设置Tilemap层级（通过名称）
        /// </summary>
        public void SetLayer(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer != -1)
            {
                SetLayer(layer);
            }
            else
            {
                Debug.LogWarning($"[TilemapSupport] 未找到层: {layerName}");
            }
        }
        
        #region 编辑器工具
        
        [ContextMenu("重新配置Tilemap")]
        public void ReconfigureTilemap()
        {
            Initialize();
        }
        
        [ContextMenu("添加到Ground层")]
        public void SetToGroundLayer()
        {
            SetLayer("Ground");
        }
        
        [ContextMenu("添加到Tilemap层")]
        public void SetToTilemapLayer()
        {
            SetLayer("Tilemap");
        }
        
        #endregion
    }
} 