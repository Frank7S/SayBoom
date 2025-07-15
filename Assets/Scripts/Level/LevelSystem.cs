using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SayBoom.Core;

namespace SayBoom.Level
{
    /// <summary>
    /// 关卡系统 - 专门负责关卡搭建和管理的独立模块
    /// 开发者可以专注于关卡设计而不影响其他系统
    /// </summary>
    public class LevelSystem : MonoBehaviour, ILevelSystem
    {
        [Header("=== 关卡系统配置 ===")]
        [Header("关卡管理")]
        [SerializeField] private string currentLevelName = "";
        [SerializeField] private int currentLevelIndex = 0;
        [SerializeField] private Transform levelContainer;
        
        [Header("关卡资源")]
        [SerializeField] private GameObject[] levelPrefabs;
        [SerializeField] private LevelData[] levelDataList;
        
        [Header("自动生成工具")]
        [SerializeField] private bool autoGenerateBasicLevel = true;
        [SerializeField] private Vector2 levelSize = new Vector2(20f, 10f);
        [SerializeField] private float platformSpacing = 3f;
        
        [Header("调试选项")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showLevelBounds = true;
        
        // 关卡元素管理
        private List<ILevelElement> levelElements;
        private Dictionary<string, ILevelElement> namedElements;
        private GameObject currentLevelObject;
        
        // 接口实现
        public string SystemName => "Level System";
        public bool IsInitialized { get; private set; }
        public bool IsActive { get; set; } = true;
        
        public string CurrentLevelName => currentLevelName;
        public int CurrentLevelIndex => currentLevelIndex;
        public bool IsLevelLoaded => currentLevelObject != null;
        
        // 事件
        public System.Action<string> OnLevelLoaded { get; set; }
        public System.Action<string> OnLevelUnloaded { get; set; }
        public System.Action<ILevelElement> OnElementAdded { get; set; }
        public System.Action<ILevelElement> OnElementRemoved { get; set; }
        
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
            
            SetupLevelContainer();
            AutoDetectExistingTilemaps();
            
            if (autoGenerateBasicLevel && levelPrefabs.Length == 0)
            {
                GenerateBasicLevel();
            }
            
            IsInitialized = true;
            Debug.Log($"[LevelSystem] 初始化完成 - 关卡搭建系统就绪");
        }
        
        public void UpdateSystem()
        {
            UpdateLevelElements();
        }
        
        public void FixedUpdateSystem()
        {
            // 固定更新中处理物理相关的关卡逻辑
        }
        
        public void Cleanup()
        {
            UnloadCurrentLevel();
            ClearAllElements();
            
            // 清理事件
            OnLevelLoaded = null;
            OnLevelUnloaded = null;
            OnElementAdded = null;
            OnElementRemoved = null;
        }
        
        #endregion
        
        #region ILevelSystem接口实现
        
        public void LoadLevel(string levelName)
        {
            if (string.IsNullOrEmpty(levelName))
            {
                Debug.LogWarning("关卡名称不能为空");
                return;
            }
            
            // 先卸载当前关卡
            if (IsLevelLoaded)
            {
                UnloadCurrentLevel();
            }
            
            // 查找关卡预制体
            GameObject levelPrefab = System.Array.Find(levelPrefabs, prefab => prefab.name == levelName);
            if (levelPrefab != null)
            {
                LoadLevelFromPrefab(levelPrefab, levelName);
            }
            else
            {
                // 查找关卡数据
                LevelData levelData = System.Array.Find(levelDataList, data => data.levelName == levelName);
                if (levelData != null)
                {
                    LoadLevelFromData(levelData);
                }
                else
                {
                    Debug.LogError($"未找到关卡: {levelName}");
                }
            }
        }
        
        public void LoadLevel(int levelIndex)
        {
            if (levelIndex < 0)
            {
                Debug.LogWarning("关卡索引不能为负数");
                return;
            }
            
            if (levelPrefabs != null && levelIndex < levelPrefabs.Length)
            {
                LoadLevelFromPrefab(levelPrefabs[levelIndex], levelPrefabs[levelIndex].name);
                currentLevelIndex = levelIndex;
            }
            else if (levelDataList != null && levelIndex < levelDataList.Length)
            {
                LoadLevelFromData(levelDataList[levelIndex]);
                currentLevelIndex = levelIndex;
            }
            else
            {
                Debug.LogError($"关卡索引超出范围: {levelIndex}");
            }
        }
        
        public void UnloadCurrentLevel()
        {
            if (!IsLevelLoaded) return;
            
            string oldLevelName = currentLevelName;
            
            // 清理关卡元素
            ClearAllElements();
            
            // 销毁关卡对象
            if (currentLevelObject != null)
            {
                DestroyImmediate(currentLevelObject);
                currentLevelObject = null;
            }
            
            currentLevelName = "";
            OnLevelUnloaded?.Invoke(oldLevelName);
            
            Debug.Log($"已卸载关卡: {oldLevelName}");
        }
        
        public void ReloadCurrentLevel()
        {
            if (string.IsNullOrEmpty(currentLevelName)) return;
            
            string levelName = currentLevelName;
            UnloadCurrentLevel();
            LoadLevel(levelName);
        }
        
        public void RegisterLevelElement(ILevelElement element)
        {
            if (element == null) return;
            
            if (!levelElements.Contains(element))
            {
                levelElements.Add(element);
                
                if (!string.IsNullOrEmpty(element.ElementName))
                {
                    namedElements[element.ElementName] = element;
                }
                
                OnElementAdded?.Invoke(element);
                
                Debug.Log($"已注册关卡元素: {element.ElementName} ({element.ElementType})");
            }
        }
        
        public void UnregisterLevelElement(ILevelElement element)
        {
            if (element == null) return;
            
            if (levelElements.Remove(element))
            {
                if (!string.IsNullOrEmpty(element.ElementName) && namedElements.ContainsKey(element.ElementName))
                {
                    namedElements.Remove(element.ElementName);
                }
                
                OnElementRemoved?.Invoke(element);
                
                Debug.Log($"已注销关卡元素: {element.ElementName}");
            }
        }
        
        public T GetLevelElement<T>(string elementName) where T : ILevelElement
        {
            if (string.IsNullOrEmpty(elementName) || !namedElements.ContainsKey(elementName))
                return default(T);
            
            var element = namedElements[elementName];
            return element is T ? (T)element : default(T);
        }
        
        public List<T> GetLevelElements<T>() where T : ILevelElement
        {
            return levelElements.OfType<T>().ToList();
        }
        
        #endregion
        
        #region 关卡加载逻辑
        
        void LoadLevelFromPrefab(GameObject prefab, string levelName)
        {
            currentLevelObject = Instantiate(prefab, levelContainer);
            currentLevelName = levelName;
            
            // 注册关卡中的所有元素
            RegisterLevelElementsInObject(currentLevelObject);
            
            OnLevelLoaded?.Invoke(levelName);
            Debug.Log($"已加载关卡预制体: {levelName}");
        }
        
        void LoadLevelFromData(LevelData levelData)
        {
            if (levelData == null) return;
            
            currentLevelObject = new GameObject($"Level_{levelData.levelName}");
            currentLevelObject.transform.SetParent(levelContainer);
            currentLevelName = levelData.levelName;
            
            // 根据关卡数据生成关卡
            GenerateLevelFromData(levelData);
            
            OnLevelLoaded?.Invoke(levelData.levelName);
            Debug.Log($"已从数据加载关卡: {levelData.levelName}");
        }
        
        void GenerateLevelFromData(LevelData levelData)
        {
            // 创建基础平台
            foreach (var platformData in levelData.platforms)
            {
                CreatePlatform(platformData.position, platformData.size, platformData.material);
            }
            
            // 创建收集品
            foreach (var collectiblePos in levelData.collectiblePositions)
            {
                CreateCollectible(collectiblePos);
            }
            
            // 创建起点和终点
            if (levelData.startPosition != Vector3.zero)
            {
                CreateCheckpoint(levelData.startPosition, "Start");
            }
            
            if (levelData.endPosition != Vector3.zero)
            {
                CreateGoal(levelData.endPosition);
            }
        }
        
        #endregion
        
        #region 关卡生成工具
        
        [ContextMenu("生成基础关卡")]
        public void GenerateBasicLevel()
        {
            if (IsLevelLoaded)
            {
                UnloadCurrentLevel();
            }
            
            currentLevelObject = new GameObject("GeneratedLevel");
            currentLevelObject.transform.SetParent(levelContainer);
            currentLevelName = "Generated_Basic_Level";
            
            // 创建地面
            CreatePlatform(new Vector3(0, -4f, 0), new Vector2(levelSize.x, 1f), null);
            
            // 创建一些平台
            for (int i = 1; i < 4; i++)
            {
                float x = -levelSize.x * 0.4f + i * platformSpacing;
                float y = -2f + i * 1.5f;
                CreatePlatform(new Vector3(x, y, 0), new Vector2(3f, 0.5f), null);
            }
            
            // 创建起点和终点
            CreateCheckpoint(new Vector3(-levelSize.x * 0.4f, -2f, 0), "Start");
            CreateGoal(new Vector3(levelSize.x * 0.4f, 2f, 0));
            
            // 创建一些收集品
            for (int i = 0; i < 3; i++)
            {
                float x = -4f + i * 4f;
                CreateCollectible(new Vector3(x, 0f, 0));
            }
            
            OnLevelLoaded?.Invoke(currentLevelName);
            Debug.Log("已生成基础关卡");
        }
        
        public GameObject CreatePlatform(Vector3 position, Vector2 size, Material material = null)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = $"Platform_{position.x:F1}_{position.y:F1}";
            platform.transform.SetParent(currentLevelObject.transform);
            platform.transform.position = position;
            platform.transform.localScale = new Vector3(size.x, size.y, 1f);
            
            // 设置为Ground层 (Layer 8)
            platform.layer = LayerMask.NameToLayer("Ground");
            if (platform.layer == -1) platform.layer = 0; // 如果Ground层不存在，使用Default层
            
            // 移除3D碰撞器，添加2D碰撞器以兼容2D物理检测
            DestroyImmediate(platform.GetComponent<BoxCollider>());
            var collider2D = platform.AddComponent<BoxCollider2D>();
            collider2D.size = size;
            
            // 添加关卡元素组件
            var platformElement = platform.AddComponent<PlatformElement>();
            RegisterLevelElement(platformElement);
            
            // 设置材质
            if (material != null)
            {
                platform.GetComponent<Renderer>().material = material;
            }
            else
            {
                // 默认棕色平台
                platform.GetComponent<Renderer>().material.color = new Color(0.6f, 0.4f, 0.2f);
            }
            
            return platform;
        }
        
        public GameObject CreateCollectible(Vector3 position)
        {
            GameObject collectible = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            collectible.name = $"Collectible_{position.x:F1}_{position.y:F1}";
            collectible.transform.SetParent(currentLevelObject.transform);
            collectible.transform.position = position;
            collectible.transform.localScale = Vector3.one * 0.5f;
            
            // 移除碰撞器，添加触发器
            DestroyImmediate(collectible.GetComponent<SphereCollider>());
            var trigger = collectible.AddComponent<SphereCollider>();
            trigger.isTrigger = true;
            
            // 设置金色材质
            collectible.GetComponent<Renderer>().material.color = Color.yellow;
            
            // 添加关卡元素组件
            var collectibleElement = collectible.AddComponent<CollectibleElement>();
            RegisterLevelElement(collectibleElement);
            
            return collectible;
        }
        
        public GameObject CreateCheckpoint(Vector3 position, string checkpointName)
        {
            GameObject checkpoint = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            checkpoint.name = $"Checkpoint_{checkpointName}";
            checkpoint.transform.SetParent(currentLevelObject.transform);
            checkpoint.transform.position = position;
            checkpoint.transform.localScale = new Vector3(1f, 2f, 1f);
            
            // 设置为触发器
            checkpoint.GetComponent<CapsuleCollider>().isTrigger = true;
            
            // 设置绿色材质
            checkpoint.GetComponent<Renderer>().material.color = Color.green;
            
            // 添加关卡元素组件
            var checkpointElement = checkpoint.AddComponent<CheckpointElement>();
            checkpointElement.checkpointName = checkpointName;
            RegisterLevelElement(checkpointElement);
            
            return checkpoint;
        }
        
        public GameObject CreateGoal(Vector3 position)
        {
            GameObject goal = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            goal.name = "Goal";
            goal.transform.SetParent(currentLevelObject.transform);
            goal.transform.position = position;
            goal.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            
            // 设置为触发器
            goal.GetComponent<CapsuleCollider>().isTrigger = true;
            
            // 设置蓝色材质
            goal.GetComponent<Renderer>().material.color = Color.blue;
            
            // 添加关卡元素组件
            var goalElement = goal.AddComponent<GoalElement>();
            RegisterLevelElement(goalElement);
            
            return goal;
        }
        
        #endregion
        
        #region 工具方法
        
        void SetupComponents()
        {
            levelElements = new List<ILevelElement>();
            namedElements = new Dictionary<string, ILevelElement>();
        }
        
        void SetupLevelContainer()
        {
            if (levelContainer == null)
            {
                GameObject containerObj = new GameObject("LevelContainer");
                containerObj.transform.SetParent(transform);
                levelContainer = containerObj.transform;
            }
        }
        
        void AutoDetectExistingTilemaps()
        {
            // 查找场景中的所有Tilemap
            UnityEngine.Tilemaps.Tilemap[] tilemaps = FindObjectsOfType<UnityEngine.Tilemaps.Tilemap>();
            
            foreach (var tilemap in tilemaps)
            {
                // 检查是否已经有TilemapSupport组件
                TilemapSupport support = tilemap.GetComponent<TilemapSupport>();
                if (support == null)
                {
                    // 自动添加TilemapSupport组件
                    support = tilemap.gameObject.AddComponent<TilemapSupport>();
                    Debug.Log($"[LevelSystem] 为Tilemap自动添加支持组件: {tilemap.name}");
                }
                
                // 注册到关卡系统
                RegisterLevelElement(support);
            }
        }
        
        void RegisterLevelElementsInObject(GameObject levelObject)
        {
            var elements = levelObject.GetComponentsInChildren<ILevelElement>();
            foreach (var element in elements)
            {
                RegisterLevelElement(element);
            }
        }
        
        void UpdateLevelElements()
        {
            // 更新活动的关卡元素
            foreach (var element in levelElements.ToList())
            {
                if (element.Transform == null)
                {
                    UnregisterLevelElement(element);
                }
            }
        }
        
        void ClearAllElements()
        {
            levelElements.Clear();
            namedElements.Clear();
        }
        
        #endregion
        
        #region 调试和可视化
        
        void OnDrawGizmosSelected()
        {
            if (!showLevelBounds) return;
            
            // 绘制关卡边界
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(levelSize.x, levelSize.y, 1f));
            
            // 绘制平台间距指示
            if (platformSpacing > 0)
            {
                Gizmos.color = Color.yellow;
                for (int i = 0; i < 5; i++)
                {
                    float x = -levelSize.x * 0.4f + i * platformSpacing;
                    Gizmos.DrawLine(new Vector3(x, -levelSize.y * 0.5f, 0), new Vector3(x, levelSize.y * 0.5f, 0));
                }
            }
        }
        
        void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(10, Screen.height - 150, 300, 140));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== 关卡系统调试信息 ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"当前关卡: {currentLevelName}");
            GUILayout.Label($"关卡索引: {currentLevelIndex}");
            GUILayout.Label($"已加载关卡: {IsLevelLoaded}");
            GUILayout.Label($"关卡元素数量: {levelElements.Count}");
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("重载关卡"))
            {
                ReloadCurrentLevel();
            }
            if (GUILayout.Button("生成基础关卡"))
            {
                GenerateBasicLevel();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        #endregion
    }
} 