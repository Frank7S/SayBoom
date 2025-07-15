using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SayBoom.Core;

namespace SayBoom.GameObjects
{
    /// <summary>
    /// 游戏对象系统 - 为障碍物和道具开发预留的扩展框架
    /// 未来的开发者可以基于此系统轻松添加新的游戏对象
    /// </summary>
    public class GameObjectSystem : MonoBehaviour, IGameObjectSystem
    {
        [Header("=== 游戏对象系统配置 ===")]
        [Header("预制体资源")]
        [SerializeField] private GameObject[] obstaclePrefabs;
        [SerializeField] private GameObject[] pickupPrefabs;
        [SerializeField] private GameObject[] interactivePrefabs;
        
        [Header("对象池配置")]
        [SerializeField] private int initialPoolSize = 10;
        [SerializeField] private bool expandPool = true;
        [SerializeField] private Transform poolContainer;
        
        [Header("调试选项")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool showObjectCount = true;
        
        // 对象管理
        private List<IGameObject> activeObjects;
        private Dictionary<string, IGameObject> namedObjects;
        private Dictionary<System.Type, Queue<IGameObject>> objectPools;
        
        // 接口实现
        public string SystemName => "GameObject System";
        public bool IsInitialized { get; private set; }
        public bool IsActive { get; set; } = true;
        
        // 事件
        public System.Action<IGameObject> OnObjectCreated { get; set; }
        public System.Action<IGameObject> OnObjectDestroyed { get; set; }
        
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
            
            SetupPoolContainer();
            InitializeObjectPools();
            
            IsInitialized = true;
            Debug.Log($"[GameObjectSystem] 初始化完成 - 障碍物和道具系统就绪");
        }
        
        public void UpdateSystem()
        {
            UpdateActiveObjects();
        }
        
        public void FixedUpdateSystem()
        {
            // 固定更新中处理物理相关的游戏对象逻辑
        }
        
        public void Cleanup()
        {
            DestroyAllObjects();
            ClearPools();
            
            // 清理事件
            OnObjectCreated = null;
            OnObjectDestroyed = null;
        }
        
        #endregion
        
        #region IGameObjectSystem接口实现
        
        public T CreateGameObject<T>(string prefabName, Vector3 position) where T : IGameObject
        {
            // 首先尝试从对象池获取
            T pooledObject = GetPooledObject<T>();
            if (pooledObject != null)
            {
                pooledObject.Transform.position = position;
                pooledObject.IsActive = true;
                pooledObject.IsPooled = false;
                pooledObject.OnActivated();
                
                RegisterGameObject(pooledObject);
                OnObjectCreated?.Invoke(pooledObject);
                
                return pooledObject;
            }
            
            // 如果池中没有，创建新对象
            GameObject prefab = FindPrefab(prefabName);
            if (prefab == null)
            {
                Debug.LogError($"未找到预制体: {prefabName}");
                return default(T);
            }
            
            GameObject instance = Instantiate(prefab, position, Quaternion.identity);
            T gameObj = instance.GetComponent<T>();
            
            if (gameObj == null)
            {
                Debug.LogError($"预制体 {prefabName} 没有实现接口 {typeof(T).Name}");
                Destroy(instance);
                return default(T);
            }
            
            gameObj.Initialize();
            RegisterGameObject(gameObj);
            OnObjectCreated?.Invoke(gameObj);
            
            return gameObj;
        }
        
        public void DestroyGameObject(IGameObject gameObject)
        {
            if (gameObject == null) return;
            
            UnregisterGameObject(gameObject);
            
            // 如果支持对象池，放入池中而不是销毁
            if (gameObject.IsPooled)
            {
                PoolObject(gameObject);
            }
            else
            {
                gameObject.OnDestroy();
                if (gameObject.Transform != null)
                {
                    Destroy(gameObject.Transform.gameObject);
                }
            }
            
            OnObjectDestroyed?.Invoke(gameObject);
        }
        
        public void RegisterGameObject(IGameObject gameObject)
        {
            if (gameObject == null) return;
            
            if (!activeObjects.Contains(gameObject))
            {
                activeObjects.Add(gameObject);
                
                if (!string.IsNullOrEmpty(gameObject.ObjectName))
                {
                    namedObjects[gameObject.ObjectName] = gameObject;
                }
                
                Debug.Log($"已注册游戏对象: {gameObject.ObjectName} ({gameObject.ObjectType})");
            }
        }
        
        public void UnregisterGameObject(IGameObject gameObject)
        {
            if (gameObject == null) return;
            
            if (activeObjects.Remove(gameObject))
            {
                if (!string.IsNullOrEmpty(gameObject.ObjectName) && namedObjects.ContainsKey(gameObject.ObjectName))
                {
                    namedObjects.Remove(gameObject.ObjectName);
                }
                
                Debug.Log($"已注销游戏对象: {gameObject.ObjectName}");
            }
        }
        
        public T GetGameObject<T>(string objectName) where T : IGameObject
        {
            if (string.IsNullOrEmpty(objectName) || !namedObjects.ContainsKey(objectName))
                return default(T);
            
            var obj = namedObjects[objectName];
            return obj is T ? (T)obj : default(T);
        }
        
        public List<T> GetGameObjects<T>() where T : IGameObject
        {
            return activeObjects.OfType<T>().ToList();
        }
        
        public List<IGameObject> GetGameObjectsInRange(Vector3 center, float radius)
        {
            return activeObjects.Where(obj => 
                obj.Transform != null && 
                Vector3.Distance(obj.Transform.position, center) <= radius
            ).ToList();
        }
        
        public void PoolObject(IGameObject gameObject)
        {
            if (gameObject == null) return;
            
            var type = gameObject.GetType();
            if (!objectPools.ContainsKey(type))
            {
                objectPools[type] = new Queue<IGameObject>();
            }
            
            gameObject.IsActive = false;
            gameObject.IsPooled = true;
            gameObject.OnDeactivated();
            
            // 移动到池容器下
            if (gameObject.Transform != null && poolContainer != null)
            {
                gameObject.Transform.SetParent(poolContainer);
                gameObject.Transform.gameObject.SetActive(false);
            }
            
            objectPools[type].Enqueue(gameObject);
        }
        
        public T GetPooledObject<T>() where T : IGameObject
        {
            var type = typeof(T);
            if (!objectPools.ContainsKey(type) || objectPools[type].Count == 0)
            {
                return default(T);
            }
            
            var obj = objectPools[type].Dequeue();
            if (obj.Transform != null)
            {
                obj.Transform.gameObject.SetActive(true);
            }
            
            return (T)obj;
        }
        
        public void ClearPool()
        {
            foreach (var pool in objectPools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj.Transform != null)
                    {
                        Destroy(obj.Transform.gameObject);
                    }
                }
            }
            objectPools.Clear();
        }
        
        #endregion
        
        #region 对象创建辅助方法
        
        /// <summary>
        /// 快速创建障碍物
        /// </summary>
        public IObstacle CreateObstacle(string prefabName, Vector3 position, ObstacleType obstacleType = ObstacleType.Static)
        {
            var obstacle = CreateGameObject<IObstacle>(prefabName, position);
            if (obstacle != null)
            {
                Debug.Log($"已创建障碍物: {prefabName} at {position}");
            }
            return obstacle;
        }
        
        /// <summary>
        /// 快速创建道具
        /// </summary>
        public IPickup CreatePickup(string prefabName, Vector3 position, PickupType pickupType = PickupType.Coin)
        {
            var pickup = CreateGameObject<IPickup>(prefabName, position);
            if (pickup != null)
            {
                Debug.Log($"已创建道具: {prefabName} at {position}");
            }
            return pickup;
        }
        
        /// <summary>
        /// 批量创建对象
        /// </summary>
        public List<T> CreateMultipleObjects<T>(string prefabName, Vector3[] positions) where T : IGameObject
        {
            var objects = new List<T>();
            foreach (var position in positions)
            {
                var obj = CreateGameObject<T>(prefabName, position);
                if (obj != null)
                {
                    objects.Add(obj);
                }
            }
            return objects;
        }
        
        #endregion
        
        #region 工具方法
        
        void SetupComponents()
        {
            activeObjects = new List<IGameObject>();
            namedObjects = new Dictionary<string, IGameObject>();
            objectPools = new Dictionary<System.Type, Queue<IGameObject>>();
        }
        
        void SetupPoolContainer()
        {
            if (poolContainer == null)
            {
                GameObject containerObj = new GameObject("ObjectPool");
                containerObj.transform.SetParent(transform);
                poolContainer = containerObj.transform;
            }
        }
        
        void InitializeObjectPools()
        {
            // 预填充对象池
            // 这里可以根据需要预创建一些常用对象
        }
        
        void UpdateActiveObjects()
        {
            // 更新活动对象，清理无效引用
            for (int i = activeObjects.Count - 1; i >= 0; i--)
            {
                var obj = activeObjects[i];
                if (obj == null || obj.Transform == null)
                {
                    activeObjects.RemoveAt(i);
                }
            }
        }
        
        void DestroyAllObjects()
        {
            var objectsToDestroy = new List<IGameObject>(activeObjects);
            foreach (var obj in objectsToDestroy)
            {
                DestroyGameObject(obj);
            }
            activeObjects.Clear();
            namedObjects.Clear();
        }
        
        void ClearPools()
        {
            foreach (var pool in objectPools.Values)
            {
                while (pool.Count > 0)
                {
                    var obj = pool.Dequeue();
                    if (obj.Transform != null)
                    {
                        Destroy(obj.Transform.gameObject);
                    }
                }
            }
            objectPools.Clear();
        }
        
        GameObject FindPrefab(string prefabName)
        {
            // 搜索所有预制体数组
            foreach (var prefab in obstaclePrefabs.Concat(pickupPrefabs).Concat(interactivePrefabs))
            {
                if (prefab != null && prefab.name == prefabName)
                {
                    return prefab;
                }
            }
            return null;
        }
        
        #endregion
        
        #region 调试和统计
        
        void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 320, Screen.height - 200, 300, 180));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== 游戏对象系统调试 ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            if (showObjectCount)
            {
                GUILayout.Label($"活动对象总数: {activeObjects.Count}");
                GUILayout.Label($"障碍物数量: {GetGameObjects<IObstacle>().Count}");
                GUILayout.Label($"道具数量: {GetGameObjects<IPickup>().Count}");
                
                int pooledCount = objectPools.Values.Sum(pool => pool.Count);
                GUILayout.Label($"池中对象数: {pooledCount}");
            }
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("清空池"))
            {
                ClearPool();
            }
            if (GUILayout.Button("销毁全部"))
            {
                DestroyAllObjects();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 获取系统统计信息
        /// </summary>
        public Dictionary<string, int> GetSystemStats()
        {
            return new Dictionary<string, int>
            {
                ["ActiveObjects"] = activeObjects.Count,
                ["Obstacles"] = GetGameObjects<IObstacle>().Count,
                ["Pickups"] = GetGameObjects<IPickup>().Count,
                ["PooledObjects"] = objectPools.Values.Sum(pool => pool.Count)
            };
        }
        
        #endregion
    }
} 