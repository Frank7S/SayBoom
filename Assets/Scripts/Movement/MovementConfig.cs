using UnityEngine;

namespace SayBoom.Movement
{
    /// <summary>
    /// 移动系统配置 - 可共享的参数设置
    /// 团队成员可以创建不同的配置文件来测试不同的跳跃手感
    /// </summary>
    [CreateAssetMenu(fileName = "MovementConfig", menuName = "SayBoom/Movement/Movement Config")]
    public class MovementConfig : ScriptableObject
    {
        [Header("配置信息")]
        [TextArea(3, 5)]
        public string description = "描述这个配置的特点和用途";
        
        [Space(10)]
        [Header("基础移动参数")]
        [Tooltip("水平移动速度")]
        public float moveSpeed = 5f;
        
        [Tooltip("移动加速度")]
        public float acceleration = 10f;
        
        [Tooltip("移动减速度")]
        public float deceleration = 10f;
        
        [Tooltip("空中控制力度")]
        [Range(0.1f, 1.0f)]
        public float airControlMultiplier = 0.7f;
        
        [Space(10)]
        [Header("跳跃手感配置")]
        [Tooltip("跳跃初始力度")]
        public float jumpForce = 12f;
        
        [Tooltip("重力强度")]
        public float gravity = 25f;
        
        [Tooltip("最大下落速度")]
        public float maxFallSpeed = 20f;
        
        [Tooltip("跳跃缓冲时间（秒）")]
        public float jumpBufferTime = 0.15f;
        
        [Tooltip("土狼时间（秒）")]
        public float coyoteTime = 0.15f;
        
        [Tooltip("可变跳跃系数")]
        [Range(0.1f, 0.9f)]
        public float variableJumpMultiplier = 0.5f;
        
        [Tooltip("着地速度保持系数")]
        [Range(0.0f, 1.0f)]
        public float landingSpeedRetention = 0.8f;
        
        [Space(10)]
        [Header("地面检测")]
        [Tooltip("地面检测半径")]
        public float groundCheckRadius = 0.2f;
        
        [Tooltip("地面图层")]
        public LayerMask groundLayerMask = 1;
        
        /// <summary>
        /// 应用配置到移动系统
        /// </summary>
        public void ApplyToMovementSystem(MovementSystem movementSystem)
        {
            if (movementSystem == null) return;
            
            movementSystem.MoveSpeed = moveSpeed;
            movementSystem.JumpForce = jumpForce;
            movementSystem.Gravity = gravity;
            
            Debug.Log($"已应用移动配置: {name}");
        }
        
        /// <summary>
        /// 从移动系统保存当前配置
        /// </summary>
        public void SaveFromMovementSystem(MovementSystem movementSystem)
        {
            if (movementSystem == null) return;
            
            moveSpeed = movementSystem.MoveSpeed;
            jumpForce = movementSystem.JumpForce;
            gravity = movementSystem.Gravity;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"已保存当前参数到配置: {name}");
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        [ContextMenu("重置为默认值")]
        public void ResetToDefaults()
        {
            description = "默认的移动配置";
            moveSpeed = 5f;
            acceleration = 10f;
            deceleration = 10f;
            airControlMultiplier = 0.7f;
            jumpForce = 12f;
            gravity = 25f;
            maxFallSpeed = 20f;
            jumpBufferTime = 0.15f;
            coyoteTime = 0.15f;
            variableJumpMultiplier = 0.5f;
            landingSpeedRetention = 0.8f;
            groundCheckRadius = 0.2f;
            groundLayerMask = 1;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
} 