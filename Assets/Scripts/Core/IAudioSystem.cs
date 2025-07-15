using UnityEngine;

namespace SayBoom.Core
{
    /// <summary>
    /// 音频系统接口 - 定义音频相关的功能
    /// </summary>
    public interface IAudioSystem : IGameSystem
    {
        // 音频状态
        float CurrentAudioLevel { get; }
        bool IsRecording { get; }
        bool HasAudioInput { get; }
        
        // 音频参数
        float Sensitivity { get; set; }
        float MinThreshold { get; set; }
        float MaxThreshold { get; set; }
        
        // 音频控制方法
        void StartRecording();
        void StopRecording();
        void SetAudioSource(AudioSource source);
        void ProcessAudioInput(float[] samples);
        
        // 音频效果接口
        void ApplyAudioEffect(GameObject target, AudioEffectType effectType);
        void RemoveAudioEffect(GameObject target, AudioEffectType effectType);
        
        // 事件
        System.Action<float> OnAudioLevelChanged { get; set; }
        System.Action<float> OnAudioThresholdReached { get; set; }
        System.Action OnRecordingStarted { get; set; }
        System.Action OnRecordingStopped { get; set; }
    }
    
    /// <summary>
    /// 音频效果类型
    /// </summary>
    public enum AudioEffectType
    {
        Scale,      // 缩放效果
        Color,      // 颜色效果
        Rotation,   // 旋转效果
        Position,   // 位置效果
        Custom      // 自定义效果
    }
} 