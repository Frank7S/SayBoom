using UnityEngine;
using System.Collections.Generic;

namespace SayBoom.Core
{
    /// <summary>
    /// 游戏对象系统接口 - 管理动态游戏对象（障碍物、道具等）
    /// </summary>
    public interface IGameObjectSystem : IGameSystem
    {
        // 对象管理方法
        T CreateGameObject<T>(string prefabName, Vector3 position) where T : IGameObject;
        void DestroyGameObject(IGameObject gameObject);
        void RegisterGameObject(IGameObject gameObject);
        void UnregisterGameObject(IGameObject gameObject);
        
        // 查询方法
        T GetGameObject<T>(string objectName) where T : IGameObject;
        List<T> GetGameObjects<T>() where T : IGameObject;
        List<IGameObject> GetGameObjectsInRange(Vector3 center, float radius);
        
        // 对象池管理
        void PoolObject(IGameObject gameObject);
        T GetPooledObject<T>() where T : IGameObject;
        void ClearPool();
        
        // 事件
        System.Action<IGameObject> OnObjectCreated { get; set; }
        System.Action<IGameObject> OnObjectDestroyed { get; set; }
    }
    
    /// <summary>
    /// 游戏对象基础接口
    /// </summary>
    public interface IGameObject
    {
        string ObjectName { get; }
        GameObjectType ObjectType { get; }
        Transform Transform { get; }
        bool IsActive { get; set; }
        bool IsPooled { get; set; }
        
        void Initialize();
        void OnActivated();
        void OnDeactivated();
        void OnPlayerInteraction(GameObject player);
        void OnDestroy();
    }
    
    /// <summary>
    /// 障碍物接口
    /// </summary>
    public interface IObstacle : IGameObject
    {
        ObstacleType ObstacleType { get; }
        float Damage { get; set; }
        bool IsDeadly { get; set; }
        
        void OnPlayerHit(GameObject player);
        void SetObstacleActive(bool active);
    }
    
    /// <summary>
    /// 道具接口
    /// </summary>
    public interface IPickup : IGameObject
    {
        PickupType PickupType { get; }
        float Value { get; set; }
        bool IsCollected { get; }
        
        void OnPickedUp(GameObject player);
        void ApplyEffect(GameObject player);
    }
    
    /// <summary>
    /// 游戏对象类型
    /// </summary>
    public enum GameObjectType
    {
        Obstacle,       // 障碍物
        Pickup,         // 道具
        Interactive,    // 可交互对象
        Decoration,     // 装饰品
        Effect,         // 特效
        Projectile      // 投射物
    }
    
    /// <summary>
    /// 障碍物类型
    /// </summary>
    public enum ObstacleType
    {
        Static,         // 静态障碍物
        Moving,         // 移动障碍物
        Rotating,       // 旋转障碍物
        Triggered,      // 触发式障碍物
        Timed           // 定时障碍物
    }
    
    /// <summary>
    /// 道具类型
    /// </summary>
    public enum PickupType
    {
        HealthBoost,    // 生命增强
        SpeedBoost,     // 速度增强
        JumpBoost,      // 跳跃增强
        AudioBoost,     // 音频增强
        Coin,           // 金币
        Key,            // 钥匙
        PowerUp         // 特殊能力
    }
} 