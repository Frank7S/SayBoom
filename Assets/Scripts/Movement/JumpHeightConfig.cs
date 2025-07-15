using UnityEngine;

namespace SayBoom.Movement
{
    /// <summary>
    /// 策划专用跳跃高度配置 - 简化的跳跃参数调整
    /// </summary>
    [CreateAssetMenu(fileName = "JumpHeightConfig", menuName = "SayBoom/Movement/Jump Height Config")]
    public class JumpHeightConfig : ScriptableObject
    {
        [Header("策划调整参数")]
        [Tooltip("跳跃高度（Unity单位）- 角色能跳多高")]
        [Range(1f, 8f)]
        public float jumpHeight = 3f;
        
        [Tooltip("重力强度 - 影响下落速度和跳跃感觉")]
        [Range(10f, 50f)]
        public float gravity = 25f;
        
        [Space(10)]
        [Header("高级参数")]
        [Tooltip("可变跳跃系数 - 控制短按和长按跳跃的高度差")]
        [Range(0.1f, 0.9f)]
        public float variableJumpMultiplier = 0.5f;
        
        [Tooltip("土狼时间 - 离开平台后还能跳跃的时间窗口")]
        [Range(0f, 0.3f)]
        public float coyoteTime = 0.15f;
        
        [Tooltip("跳跃缓冲时间 - 提前按跳跃键的容错时间")]
        [Range(0f, 0.3f)]
        public float jumpBufferTime = 0.15f;
        
        [Space(10)]
        [Header("计算结果显示")]
        [SerializeField, Tooltip("自动计算的跳跃力度")]
        private float calculatedJumpForce;
        
        [SerializeField, Tooltip("理论最大跳跃高度")]
        private float maxJumpHeight;
        
        [SerializeField, Tooltip("短按跳跃高度")]
        private float minJumpHeight;
        
        /// <summary>
        /// 获取计算后的跳跃力度
        /// </summary>
        public float JumpForce => calculatedJumpForce;
        
        /// <summary>
        /// 计算跳跃相关参数
        /// </summary>
        void CalculateJumpParameters()
        {
            // 使用物理公式计算跳跃力度：v = sqrt(2 * g * h)
            calculatedJumpForce = Mathf.Sqrt(2f * gravity * jumpHeight);
            
            // 计算最大跳跃高度（理论值）
            maxJumpHeight = jumpHeight;
            
            // 计算最小跳跃高度（短按时）
            float shortPressVelocity = calculatedJumpForce * variableJumpMultiplier;
            minJumpHeight = (shortPressVelocity * shortPressVelocity) / (2f * gravity);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        /// <summary>
        /// 编辑器中值改变时自动重新计算
        /// </summary>
        void OnValidate()
        {
            CalculateJumpParameters();
        }
        
        /// <summary>
        /// 应用配置到移动系统
        /// </summary>
        public void ApplyToMovementSystem(MovementSystem movementSystem)
        {
            if (movementSystem == null) return;
            
            CalculateJumpParameters();
            
            movementSystem.JumpForce = JumpForce;
            movementSystem.Gravity = gravity;
            movementSystem.CoyoteTime = coyoteTime;
            movementSystem.JumpBufferTime = jumpBufferTime;
            movementSystem.VariableJumpMultiplier = variableJumpMultiplier;
            
            Debug.Log($"[JumpHeightConfig] 已应用跳跃配置 - 高度:{jumpHeight:F1}, 力度:{JumpForce:F1}");
        }
        
        /// <summary>
        /// 从移动系统读取当前参数
        /// </summary>
        public void LoadFromMovementSystem(MovementSystem movementSystem)
        {
            if (movementSystem == null) return;
            
            // 反向计算跳跃高度
            jumpHeight = (movementSystem.JumpForce * movementSystem.JumpForce) / (2f * movementSystem.Gravity);
            gravity = movementSystem.Gravity;
            coyoteTime = movementSystem.CoyoteTime;
            jumpBufferTime = movementSystem.JumpBufferTime;
            variableJumpMultiplier = movementSystem.VariableJumpMultiplier;
            
            CalculateJumpParameters();
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        
        [ContextMenu("重置为默认值")]
        public void ResetToDefaults()
        {
            jumpHeight = 3f;
            gravity = 25f;
            variableJumpMultiplier = 0.5f;
            coyoteTime = 0.15f;
            jumpBufferTime = 0.15f;
            
            CalculateJumpParameters();
        }
        
        [ContextMenu("测试不同跳跃高度")]
        public void TestDifferentHeights()
        {
            Debug.Log("=== 跳跃高度测试 ===");
            Debug.Log($"1单位跳跃: 力度 {Mathf.Sqrt(2f * gravity * 1f):F1}");
            Debug.Log($"2单位跳跃: 力度 {Mathf.Sqrt(2f * gravity * 2f):F1}");
            Debug.Log($"3单位跳跃: 力度 {Mathf.Sqrt(2f * gravity * 3f):F1}");
            Debug.Log($"4单位跳跃: 力度 {Mathf.Sqrt(2f * gravity * 4f):F1}");
            Debug.Log($"5单位跳跃: 力度 {Mathf.Sqrt(2f * gravity * 5f):F1}");
        }
    }
} 