using UnityEngine;
using System.Collections.Generic;

namespace SayBoom.Core
{
    /// <summary>
    /// 关卡系统接口 - 定义关卡管理相关的功能
    /// </summary>
    public interface ILevelSystem : IGameSystem
    {
        // 关卡状态
        string CurrentLevelName { get; }
        int CurrentLevelIndex { get; }
        bool IsLevelLoaded { get; }
        
        // 关卡管理方法
        void LoadLevel(string levelName);
        void LoadLevel(int levelIndex);
        void UnloadCurrentLevel();
        void ReloadCurrentLevel();
        
        // 关卡元素管理
        void RegisterLevelElement(ILevelElement element);
        void UnregisterLevelElement(ILevelElement element);
        T GetLevelElement<T>(string elementName) where T : ILevelElement;
        List<T> GetLevelElements<T>() where T : ILevelElement;
        
        // 关卡事件
        System.Action<string> OnLevelLoaded { get; set; }
        System.Action<string> OnLevelUnloaded { get; set; }
        System.Action<ILevelElement> OnElementAdded { get; set; }
        System.Action<ILevelElement> OnElementRemoved { get; set; }
    }
    
    /// <summary>
    /// 关卡元素接口 - 关卡中的所有可交互对象都应实现此接口
    /// </summary>
    public interface ILevelElement
    {
        string ElementName { get; }
        LevelElementType ElementType { get; }
        Transform Transform { get; }
        bool IsActive { get; set; }
        
        void Initialize();
        void OnPlayerEnter(GameObject player);
        void OnPlayerExit(GameObject player);
        void OnPlayerInteract(GameObject player);
    }
    
    /// <summary>
    /// 关卡元素类型
    /// </summary>
    public enum LevelElementType
    {
        Platform,       // 平台
        Obstacle,       // 障碍物
        Collectible,    // 收集品
        Trigger,        // 触发器
        Checkpoint,     // 检查点
        Goal,           // 目标点
        Decoration,     // 装饰品
        Interactive     // 可交互对象
    }
} 