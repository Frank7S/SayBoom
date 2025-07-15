using UnityEngine;

namespace SayBoom.Core
{
    /// <summary>
    /// 游戏系统基础接口 - 所有游戏系统都应实现此接口
    /// </summary>
    public interface IGameSystem
    {
        /// <summary>
        /// 系统名称
        /// </summary>
        string SystemName { get; }
        
        /// <summary>
        /// 系统是否已初始化
        /// </summary>
        bool IsInitialized { get; }
        
        /// <summary>
        /// 系统是否处于活动状态
        /// </summary>
        bool IsActive { get; set; }
        
        /// <summary>
        /// 初始化系统
        /// </summary>
        void Initialize();
        
        /// <summary>
        /// 更新系统（每帧调用）
        /// </summary>
        void UpdateSystem();
        
        /// <summary>
        /// 固定更新系统（物理帧调用）
        /// </summary>
        void FixedUpdateSystem();
        
        /// <summary>
        /// 清理系统资源
        /// </summary>
        void Cleanup();
    }
} 