using UnityEngine;
using System.Collections.Generic;
using SayBoom.Core;
using SayBoom.Movement;
using SayBoom.Audio;
using SayBoom.Level;
using SayBoom.GameObjects;

namespace SayBoom.Core
{
    /// <summary>
    /// 游戏管理器 - 协调各个系统模块的中央管理器
    /// 负责系统初始化、更新协调和模块间通信
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("=== 游戏管理器配置 ===")]
        [Header("系统组件")]
        [SerializeField] private MovementSystem movementSystem;
        [SerializeField] private AudioSystem audioSystem;
        [SerializeField] private LevelSystem levelSystem;
        [SerializeField] private GameObjectSystem gameObjectSystem;
        
        [Header("系统设置")]
        [SerializeField] private bool autoInitializeSystems = true;
        [SerializeField] private bool enableSystemUpdates = true;
        
        [Header("调试选项")]
        [SerializeField] private bool showSystemStatus = true;
        [SerializeField] private bool enablePerformanceMonitoring = false;
        
        // 系统管理
        private List<IGameSystem> allSystems;
        private Dictionary<string, IGameSystem> systemsByName;
        private bool isInitialized = false;
        
        // 性能监控
        private float lastUpdateTime;
        private float averageFrameTime;
        
        // 单例模式
        public static GameManager Instance { get; private set; }
        
        // 系统访问属性
        public IMovementSystem Movement => movementSystem;
        public IAudioSystem Audio => audioSystem;
        public ILevelSystem Level => levelSystem;
        public IGameObjectSystem GameObjects => gameObjectSystem;
        
        #region Unity生命周期
        
        void Awake()
        {
            // 单例设置
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                SetupSystems();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        void Start()
        {
            if (autoInitializeSystems)
            {
                InitializeAllSystems();
            }
        }
        
        void Update()
        {
            if (!enableSystemUpdates || !isInitialized) return;
            
            float frameStart = Time.realtimeSinceStartup;
            
            UpdateAllSystems();
            
            if (enablePerformanceMonitoring)
            {
                UpdatePerformanceStats(frameStart);
            }
        }
        
        void FixedUpdate()
        {
            if (!enableSystemUpdates || !isInitialized) return;
            
            FixedUpdateAllSystems();
        }
        
        void OnApplicationPause(bool pauseStatus)
        {
            HandleApplicationPause(pauseStatus);
        }
        
        void OnApplicationFocus(bool hasFocus)
        {
            HandleApplicationFocus(hasFocus);
        }
        
        void OnDestroy()
        {
            CleanupAllSystems();
        }
        
        #endregion
        
        #region 系统管理
        
        /// <summary>
        /// 设置系统引用
        /// </summary>
        void SetupSystems()
        {
            allSystems = new List<IGameSystem>();
            systemsByName = new Dictionary<string, IGameSystem>();
            
            // 自动查找系统组件
            if (movementSystem == null)
                movementSystem = FindObjectOfType<MovementSystem>();
            if (audioSystem == null)
                audioSystem = FindObjectOfType<AudioSystem>();
            if (levelSystem == null)
                levelSystem = FindObjectOfType<LevelSystem>();
            if (gameObjectSystem == null)
                gameObjectSystem = FindObjectOfType<GameObjectSystem>();
            
            // 添加到系统列表
            RegisterSystem(movementSystem);
            RegisterSystem(audioSystem);
            RegisterSystem(levelSystem);
            RegisterSystem(gameObjectSystem);
        }
        
        /// <summary>
        /// 注册系统
        /// </summary>
        void RegisterSystem(IGameSystem system)
        {
            if (system != null)
            {
                allSystems.Add(system);
                systemsByName[system.SystemName] = system;
                Debug.Log($"[GameManager] 已注册系统: {system.SystemName}");
            }
        }
        
        /// <summary>
        /// 初始化所有系统
        /// </summary>
        public void InitializeAllSystems()
        {
            if (isInitialized)
            {
                Debug.LogWarning("系统已经初始化过了");
                return;
            }
            
            Debug.Log("[GameManager] 开始初始化所有系统...");
            
            foreach (var system in allSystems)
            {
                if (system != null && !system.IsInitialized)
                {
                    try
                    {
                        system.Initialize();
                        Debug.Log($"[GameManager] ✓ {system.SystemName} 初始化成功");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[GameManager] ✗ {system.SystemName} 初始化失败: {e.Message}");
                    }
                }
            }
            
            SetupSystemInteractions();
            isInitialized = true;
            
            Debug.Log("[GameManager] 所有系统初始化完成！");
        }
        
        /// <summary>
        /// 设置系统间交互
        /// </summary>
        void SetupSystemInteractions()
        {
            // 设置移动系统和音频系统的交互
            if (movementSystem != null && audioSystem != null)
            {
                // 音频触发跳跃
                audioSystem.OnAudioThresholdReached += (level) =>
                {
                    if (level > 0.05f && movementSystem.IsGrounded)
                    {
                        movementSystem.ForceJump();
                    }
                };
                
                // 移动事件触发音频效果
                movementSystem.OnJumped += () =>
                {
                    // 可以在这里添加跳跃音效
                };
                
                movementSystem.OnLanded += () =>
                {
                    // 可以在这里添加着地音效
                };
            }
            
            // 设置关卡系统和其他系统的交互
            if (levelSystem != null)
            {
                levelSystem.OnLevelLoaded += OnLevelLoaded;
                levelSystem.OnLevelUnloaded += OnLevelUnloaded;
            }
            
            Debug.Log("[GameManager] 系统间交互设置完成");
        }
        
        /// <summary>
        /// 更新所有系统
        /// </summary>
        void UpdateAllSystems()
        {
            foreach (var system in allSystems)
            {
                if (system != null && system.IsActive)
                {
                    try
                    {
                        system.UpdateSystem();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[GameManager] {system.SystemName} 更新异常: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 固定更新所有系统
        /// </summary>
        void FixedUpdateAllSystems()
        {
            foreach (var system in allSystems)
            {
                if (system != null && system.IsActive)
                {
                    try
                    {
                        system.FixedUpdateSystem();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[GameManager] {system.SystemName} 固定更新异常: {e.Message}");
                    }
                }
            }
        }
        
        /// <summary>
        /// 清理所有系统
        /// </summary>
        void CleanupAllSystems()
        {
            foreach (var system in allSystems)
            {
                if (system != null)
                {
                    try
                    {
                        system.Cleanup();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"[GameManager] {system.SystemName} 清理异常: {e.Message}");
                    }
                }
            }
            
            allSystems.Clear();
            systemsByName.Clear();
        }
        
        #endregion
        
        #region 公共接口
        
        /// <summary>
        /// 获取指定系统
        /// </summary>
        public T GetSystem<T>() where T : class, IGameSystem
        {
            foreach (var system in allSystems)
            {
                if (system is T targetSystem)
                {
                    return targetSystem;
                }
            }
            return null;
        }
        
        /// <summary>
        /// 根据名称获取系统
        /// </summary>
        public IGameSystem GetSystem(string systemName)
        {
            return systemsByName.ContainsKey(systemName) ? systemsByName[systemName] : null;
        }
        
        /// <summary>
        /// 启用/禁用系统
        /// </summary>
        public void SetSystemActive(string systemName, bool active)
        {
            var system = GetSystem(systemName);
            if (system != null)
            {
                system.IsActive = active;
                Debug.Log($"[GameManager] {systemName} 已{(active ? "启用" : "禁用")}");
            }
        }
        
        /// <summary>
        /// 重新初始化指定系统
        /// </summary>
        public void ReinitializeSystem(string systemName)
        {
            var system = GetSystem(systemName);
            if (system != null)
            {
                system.Cleanup();
                system.Initialize();
                Debug.Log($"[GameManager] 已重新初始化系统: {systemName}");
            }
        }
        
        #endregion
        
        #region 事件处理
        
        void OnLevelLoaded(string levelName)
        {
            Debug.Log($"[GameManager] 关卡已加载: {levelName}");
            
            // 重置玩家位置到起点
            if (movementSystem != null && levelSystem != null)
            {
                // 这里可以设置玩家位置到关卡起点
            }
        }
        
        void OnLevelUnloaded(string levelName)
        {
            Debug.Log($"[GameManager] 关卡已卸载: {levelName}");
            
            // 清理关卡相关的游戏对象
            if (gameObjectSystem != null)
            {
                // 这里可以清理关卡特定的对象
            }
        }
        
        void HandleApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // 暂停所有系统
                foreach (var system in allSystems)
                {
                    if (system != null)
                    {
                        system.IsActive = false;
                    }
                }
            }
            else
            {
                // 恢复所有系统
                foreach (var system in allSystems)
                {
                    if (system != null)
                    {
                        system.IsActive = true;
                    }
                }
            }
        }
        
        void HandleApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                // 失去焦点时可以暂停音频录制
                if (audioSystem != null)
                {
                    audioSystem.StopRecording();
                }
            }
            else
            {
                // 恢复焦点时重新开始录制
                if (audioSystem != null)
                {
                    audioSystem.StartRecording();
                }
            }
        }
        
        #endregion
        
        #region 性能监控
        
        void UpdatePerformanceStats(float frameStart)
        {
            float frameTime = Time.realtimeSinceStartup - frameStart;
            averageFrameTime = Mathf.Lerp(averageFrameTime, frameTime, 0.1f);
        }
        
        public float GetAverageFrameTime()
        {
            return averageFrameTime;
        }
        
        public float GetCurrentFPS()
        {
            return averageFrameTime > 0 ? 1f / averageFrameTime : 0f;
        }
        
        #endregion
        
        #region 调试和可视化
        
        void OnGUI()
        {
            if (!showSystemStatus || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 250, 10, 240, 300));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== 游戏管理器状态 ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"初始化状态: {(isInitialized ? "✓" : "✗")}");
            GUILayout.Label($"系统数量: {allSystems.Count}");
            
            if (enablePerformanceMonitoring)
            {
                GUILayout.Label($"平均帧时间: {averageFrameTime * 1000:F2}ms");
                GUILayout.Label($"当前FPS: {GetCurrentFPS():F1}");
            }
            
            GUILayout.Space(10);
            GUILayout.Label("系统状态:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            foreach (var system in allSystems)
            {
                if (system != null)
                {
                    string status = system.IsInitialized ? (system.IsActive ? "✓ 运行中" : "⏸ 已暂停") : "✗ 未初始化";
                    GUILayout.Label($"{system.SystemName}: {status}");
                }
            }
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("重新初始化"))
            {
                CleanupAllSystems();
                SetupSystems();
                InitializeAllSystems();
            }
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        /// <summary>
        /// 获取系统状态报告
        /// </summary>
        public string GetSystemStatusReport()
        {
            var report = new System.Text.StringBuilder();
            report.AppendLine("=== 游戏管理器状态报告 ===");
            report.AppendLine($"初始化状态: {isInitialized}");
            report.AppendLine($"系统总数: {allSystems.Count}");
            report.AppendLine();
            
            foreach (var system in allSystems)
            {
                if (system != null)
                {
                    report.AppendLine($"{system.SystemName}:");
                    report.AppendLine($"  - 已初始化: {system.IsInitialized}");
                    report.AppendLine($"  - 活动状态: {system.IsActive}");
                }
            }
            
            return report.ToString();
        }
        
        #endregion
    }
} 