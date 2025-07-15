using UnityEngine;

namespace SayBoom.Core
{
    /// <summary>
    /// 移动系统接口 - 定义移动相关的功能
    /// </summary>
    public interface IMovementSystem : IGameSystem
    {
        // 移动状态
        Vector2 Velocity { get; }
        bool IsGrounded { get; }
        bool IsMoving { get; }
        
        // 移动参数
        float MoveSpeed { get; set; }
        float JumpForce { get; set; }
        float Gravity { get; set; }
        
        // 移动控制方法
        void Move(Vector2 input);
        void Jump();
        void ForceJump();
        void StopMovement();
        void SetPosition(Vector3 position);
        
        // 事件
        System.Action OnLanded { get; set; }
        System.Action OnJumped { get; set; }
        System.Action OnStartedMoving { get; set; }
        System.Action OnStoppedMoving { get; set; }
    }
} 