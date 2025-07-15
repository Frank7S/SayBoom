using UnityEngine;

namespace SayBoom.Audio
{
    /// <summary>
    /// 音频系统配置 - 可共享的音频参数设置
    /// 团队成员可以创建不同的配置文件来测试不同的音频响应手感
    /// </summary>
    [CreateAssetMenu(fileName = "AudioConfig", menuName = "SayBoom/Audio/Audio Config")]
    public class AudioConfig : ScriptableObject
    {
        [Header("配置信息")]
        [TextArea(3, 5)]
        public string description = "描述这个音频配置的特点和用途";
        
        [Space(10)]
        [Header("音频输入设置")]
        [Tooltip("是否使用默认麦克风")]
        public bool useDefaultMicrophone = true;
        
        [Tooltip("指定的麦克风名称（如果不使用默认）")]
        public string specificMicrophoneName = "";
        
        [Space(10)]
        [Header("音频处理参数")]
        [Tooltip("音频敏感度")]
        public float sensitivity = 200f;
        
        [Tooltip("最小响应阈值")]
        public float minThreshold = 0.001f;
        
        [Tooltip("最大响应阈值")]
        public float maxThreshold = 0.1f;
        
        [Tooltip("音频平滑系数")]
        [Range(0.1f, 1.0f)]
        public float smoothingFactor = 0.8f;
        
        [Tooltip("频谱采样数")]
        public int sampleSize = 1024;
        
        [Tooltip("FFT窗口类型")]
        public FFTWindow fftWindow = FFTWindow.Rectangular;
        
        [Space(10)]
        [Header("音频效果配置")]
        [Tooltip("效果强度")]
        public float effectIntensity = 1.0f;
        
        [Tooltip("效果持续时间")]
        public float effectDuration = 0.1f;
        
        [Tooltip("效果响应曲线")]
        public AnimationCurve effectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        /// <summary>
        /// 应用配置到音频系统
        /// </summary>
        public void ApplyToAudioSystem(AudioSystem audioSystem)
        {
            if (audioSystem == null) return;
            
            audioSystem.Sensitivity = sensitivity;
            audioSystem.MinThreshold = minThreshold;
            audioSystem.MaxThreshold = maxThreshold;
            
            Debug.Log($"已应用音频配置: {name}");
        }
        
        /// <summary>
        /// 从音频系统保存当前配置
        /// </summary>
        public void SaveFromAudioSystem(AudioSystem audioSystem)
        {
            if (audioSystem == null) return;
            
            sensitivity = audioSystem.Sensitivity;
            minThreshold = audioSystem.MinThreshold;
            maxThreshold = audioSystem.MaxThreshold;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
            
            Debug.Log($"已保存当前音频参数到配置: {name}");
        }
        
        /// <summary>
        /// 创建默认配置
        /// </summary>
        [ContextMenu("重置为默认值")]
        public void ResetToDefaults()
        {
            description = "默认的音频配置";
            useDefaultMicrophone = true;
            specificMicrophoneName = "";
            sensitivity = 200f;
            minThreshold = 0.001f;
            maxThreshold = 0.1f;
            smoothingFactor = 0.8f;
            sampleSize = 1024;
            fftWindow = FFTWindow.Rectangular;
            effectIntensity = 1.0f;
            effectDuration = 0.1f;
            effectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
} 