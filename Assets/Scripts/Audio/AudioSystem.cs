using UnityEngine;
using System.Collections.Generic;
using SayBoom.Core;

namespace SayBoom.Audio
{
    /// <summary>
    /// 音频系统 - 专门负责音频处理手感优化的独立模块
    /// 开发者可以专注于调整音频响应而不影响其他系统
    /// </summary>
    public class AudioSystem : MonoBehaviour, IAudioSystem
    {
        [Header("=== 音频系统配置 ===")]
        [Header("音频输入设置")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private bool useDefaultMicrophone = true;
        [SerializeField] private string specificMicrophoneName = "";
        
        [Header("音频处理调优区域")]
        [Tooltip("音频敏感度 - 影响响应强度")]
        [SerializeField] private float sensitivity = 200f;
        
        [Tooltip("最小阈值 - 低于此值不响应")]
        [SerializeField] private float minThreshold = 0.001f;
        
        [Tooltip("最大阈值 - 高于此值的响应会被限制")]
        [SerializeField] private float maxThreshold = 0.1f;
        
        [Tooltip("音频平滑系数 - 影响响应的平滑度")]
        [Range(0.1f, 1.0f)]
        [SerializeField] private float smoothingFactor = 0.8f;
        
        [Tooltip("频谱采样数")]
        [SerializeField] private int sampleSize = 1024;
        
        [Tooltip("FFT窗口类型")]
        [SerializeField] private FFTWindow fftWindow = FFTWindow.Rectangular;
        
        [Header("音频效果配置")]
        [SerializeField] private float effectIntensity = 1.0f;
        [SerializeField] private float effectDuration = 0.1f;
        [SerializeField] private AnimationCurve effectCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Header("调试选项")]
        [SerializeField] private bool showDebugInfo = true;
        [SerializeField] private bool enableVisualization = true;
        
        // 音频数据
        private float[] audioSpectrum;
        private float currentAudioLevel;
        private float smoothedAudioLevel;
        
        // 音频效果管理
        private Dictionary<GameObject, Dictionary<AudioEffectType, AudioEffect>> activeEffects;
        
        // 接口实现
        public string SystemName => "Audio System";
        public bool IsInitialized { get; private set; }
        public bool IsActive { get; set; } = true;
        
        public float CurrentAudioLevel => smoothedAudioLevel;
        public bool IsRecording { get; private set; }
        public bool HasAudioInput => audioSource != null && audioSource.clip != null;
        
        public float Sensitivity 
        { 
            get => sensitivity; 
            set => sensitivity = value; 
        }
        
        public float MinThreshold 
        { 
            get => minThreshold; 
            set => minThreshold = value; 
        }
        
        public float MaxThreshold 
        { 
            get => maxThreshold; 
            set => maxThreshold = value; 
        }
        
        // 事件
        public System.Action<float> OnAudioLevelChanged { get; set; }
        public System.Action<float> OnAudioThresholdReached { get; set; }
        public System.Action OnRecordingStarted { get; set; }
        public System.Action OnRecordingStopped { get; set; }
        
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
            
            SetupAudioComponents();
            SetupMicrophoneInput();
            InitializeEffectSystem();
            
            IsInitialized = true;
            Debug.Log($"[AudioSystem] 初始化完成 - 音频处理调优就绪");
        }
        
        public void UpdateSystem()
        {
            ProcessAudioData();
            UpdateAudioEffects();
            HandleAudioEvents();
        }
        
        public void FixedUpdateSystem()
        {
            // 固定更新中处理物理相关的音频效果
        }
        
        public void Cleanup()
        {
            StopRecording();
            ClearAllEffects();
            
            // 清理事件
            OnAudioLevelChanged = null;
            OnAudioThresholdReached = null;
            OnRecordingStarted = null;
            OnRecordingStopped = null;
        }
        
        #endregion
        
        #region IAudioSystem接口实现
        
        public void StartRecording()
        {
            if (IsRecording) return;
            
            SetupMicrophoneInput();
            IsRecording = true;
            OnRecordingStarted?.Invoke();
        }
        
        public void StopRecording()
        {
            if (!IsRecording) return;
            
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            IsRecording = false;
            OnRecordingStopped?.Invoke();
        }
        
        public void SetAudioSource(AudioSource source)
        {
            audioSource = source;
        }
        
        public void ProcessAudioInput(float[] samples)
        {
            if (samples == null || samples.Length == 0) return;
            
            // 计算音频强度
            float level = 0f;
            for (int i = 0; i < Mathf.Min(samples.Length, 64); i++)
            {
                level += samples[i];
            }
            level /= Mathf.Min(samples.Length, 64);
            
            UpdateAudioLevel(level);
        }
        
        public void ApplyAudioEffect(GameObject target, AudioEffectType effectType)
        {
            if (target == null) return;
            
            if (!activeEffects.ContainsKey(target))
            {
                activeEffects[target] = new Dictionary<AudioEffectType, AudioEffect>();
            }
            
            if (!activeEffects[target].ContainsKey(effectType))
            {
                AudioEffect effect = CreateEffect(effectType, target);
                activeEffects[target][effectType] = effect;
            }
            
            activeEffects[target][effectType].IsActive = true;
        }
        
        public void RemoveAudioEffect(GameObject target, AudioEffectType effectType)
        {
            if (target == null || !activeEffects.ContainsKey(target)) return;
            
            if (activeEffects[target].ContainsKey(effectType))
            {
                activeEffects[target][effectType].IsActive = false;
            }
        }
        
        #endregion
        
        #region 核心音频处理逻辑
        
        void ProcessAudioData()
        {
            if (audioSource == null || !audioSource.isPlaying) return;
            
            // 获取音频频谱数据
            AudioListener.GetSpectrumData(audioSpectrum, 0, fftWindow);
            
            // 计算音频强度
            float rawLevel = 0f;
            int frequencyBands = Mathf.Min(64, sampleSize);
            
            for (int i = 0; i < frequencyBands; i++)
            {
                rawLevel += audioSpectrum[i];
            }
            rawLevel /= frequencyBands;
            
            UpdateAudioLevel(rawLevel);
        }
        
        void UpdateAudioLevel(float rawLevel)
        {
            // 应用阈值限制
            currentAudioLevel = Mathf.Clamp(rawLevel, minThreshold, maxThreshold);
            
            // 平滑处理
            smoothedAudioLevel = Mathf.Lerp(smoothedAudioLevel, currentAudioLevel, smoothingFactor * Time.deltaTime * 30f);
            
            // 触发事件
            OnAudioLevelChanged?.Invoke(smoothedAudioLevel);
            
            // 检查阈值
            if (smoothedAudioLevel > minThreshold)
            {
                OnAudioThresholdReached?.Invoke(smoothedAudioLevel);
            }
        }
        
        void UpdateAudioEffects()
        {
            var objectsToRemove = new List<GameObject>();
            
            foreach (var targetEffects in activeEffects)
            {
                var target = targetEffects.Key;
                if (target == null)
                {
                    objectsToRemove.Add(target);
                    continue;
                }
                
                var effectsToRemove = new List<AudioEffectType>();
                
                foreach (var effect in targetEffects.Value)
                {
                    if (effect.Value.IsActive)
                    {
                        UpdateEffect(effect.Value);
                    }
                    else if (!effect.Value.IsActive && effect.Value.CurrentIntensity <= 0)
                    {
                        effectsToRemove.Add(effect.Key);
                    }
                }
                
                // 移除已结束的效果
                foreach (var effectType in effectsToRemove)
                {
                    targetEffects.Value.Remove(effectType);
                }
            }
            
            // 移除已销毁的对象
            foreach (var obj in objectsToRemove)
            {
                activeEffects.Remove(obj);
            }
        }
        
        void UpdateEffect(AudioEffect effect)
        {
            float targetIntensity = effect.IsActive ? smoothedAudioLevel * sensitivity : 0f;
            effect.CurrentIntensity = Mathf.Lerp(effect.CurrentIntensity, targetIntensity, Time.deltaTime / effectDuration);
            
            ApplyEffectToTarget(effect);
        }
        
        void ApplyEffectToTarget(AudioEffect effect)
        {
            float intensity = effect.CurrentIntensity * effectIntensity;
            float curveValue = effectCurve.Evaluate(intensity);
            
            switch (effect.EffectType)
            {
                case AudioEffectType.Scale:
                    ApplyScaleEffect(effect.Target, curveValue);
                    break;
                case AudioEffectType.Color:
                    ApplyColorEffect(effect.Target, curveValue);
                    break;
                case AudioEffectType.Rotation:
                    ApplyRotationEffect(effect.Target, curveValue);
                    break;
                case AudioEffectType.Position:
                    ApplyPositionEffect(effect.Target, curveValue);
                    break;
            }
        }
        
        void HandleAudioEvents()
        {
            // 可以在这里添加基于音频级别的特殊事件处理
        }
        
        #endregion
        
        #region 音频效果实现
        
        void ApplyScaleEffect(GameObject target, float intensity)
        {
            if (target == null) return;
            
            Vector3 baseScale = Vector3.one;
            float scaleMultiplier = 1f + intensity;
            target.transform.localScale = Vector3.Lerp(target.transform.localScale, baseScale * scaleMultiplier, Time.deltaTime * 10f);
        }
        
        void ApplyColorEffect(GameObject target, float intensity)
        {
            if (target == null) return;
            
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color baseColor = Color.white;
                Color targetColor = Color.Lerp(baseColor, Color.red, intensity);
                renderer.material.color = Color.Lerp(renderer.material.color, targetColor, Time.deltaTime * 5f);
            }
        }
        
        void ApplyRotationEffect(GameObject target, float intensity)
        {
            if (target == null) return;
            
            float rotationSpeed = intensity * 180f; // 度/秒
            target.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
        
        void ApplyPositionEffect(GameObject target, float intensity)
        {
            if (target == null) return;
            
            // 简单的震动效果
            Vector3 randomOffset = Random.insideUnitSphere * intensity * 0.1f;
            randomOffset.z = 0; // 2D游戏
            target.transform.localPosition += randomOffset;
        }
        
        #endregion
        
        #region 初始化和设置
        
        void SetupComponents()
        {
            activeEffects = new Dictionary<GameObject, Dictionary<AudioEffectType, AudioEffect>>();
            audioSpectrum = new float[sampleSize];
        }
        
        void SetupAudioComponents()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
        }
        
        void SetupMicrophoneInput()
        {
            if (!useDefaultMicrophone && !string.IsNullOrEmpty(specificMicrophoneName))
            {
                // 使用指定的麦克风
                if (System.Array.Exists(Microphone.devices, device => device == specificMicrophoneName))
                {
                    StartMicrophoneRecording(specificMicrophoneName);
                }
                else
                {
                    Debug.LogWarning($"指定的麦克风 '{specificMicrophoneName}' 未找到，使用默认麦克风");
                    StartDefaultMicrophone();
                }
            }
            else
            {
                StartDefaultMicrophone();
            }
        }
        
        void StartDefaultMicrophone()
        {
            if (Microphone.devices.Length > 0)
            {
                string defaultMic = Microphone.devices[0];
                StartMicrophoneRecording(defaultMic);
            }
            else
            {
                Debug.LogWarning("未检测到可用的麦克风设备！");
            }
        }
        
        void StartMicrophoneRecording(string microphoneName)
        {
            audioSource.clip = Microphone.Start(microphoneName, true, 1, AudioSettings.outputSampleRate);
            audioSource.loop = true;
            
            // 等待麦克风开始录制
            while (!(Microphone.GetPosition(microphoneName) > 0)) { }
            
            audioSource.Play();
            IsRecording = true;
            
            Debug.Log($"[AudioSystem] 已连接麦克风: {microphoneName}");
        }
        
        void InitializeEffectSystem()
        {
            // 初始化效果系统
        }
        
        AudioEffect CreateEffect(AudioEffectType effectType, GameObject target)
        {
            return new AudioEffect
            {
                EffectType = effectType,
                Target = target,
                IsActive = false,
                CurrentIntensity = 0f
            };
        }
        
        void ClearAllEffects()
        {
            activeEffects.Clear();
        }
        
        #endregion
        
        #region 调试和可视化
        
        void OnGUI()
        {
            if (!showDebugInfo || !Application.isPlaying) return;
            
            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 300, 250));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("=== 音频系统调试信息 ===", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label($"原始音频级别: {currentAudioLevel:F4}");
            GUILayout.Label($"平滑音频级别: {smoothedAudioLevel:F4}");
            GUILayout.Label($"正在录制: {IsRecording}");
            GUILayout.Label($"活动效果数量: {CountActiveEffects()}");
            
            GUILayout.Space(10);
            GUILayout.Label("快速调整参数:", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"敏感度: {sensitivity:F0}");
            sensitivity = GUILayout.HorizontalSlider(sensitivity, 50f, 500f, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"最小阈值: {minThreshold:F4}");
            minThreshold = GUILayout.HorizontalSlider(minThreshold, 0.0001f, 0.01f, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            GUILayout.BeginHorizontal();
            GUILayout.Label($"平滑度: {smoothingFactor:F2}");
            smoothingFactor = GUILayout.HorizontalSlider(smoothingFactor, 0.1f, 1.0f, GUILayout.Width(100));
            GUILayout.EndHorizontal();
            
            // 可视化音频级别
            if (enableVisualization)
            {
                GUILayout.Space(10);
                Rect barRect = GUILayoutUtility.GetRect(280, 20);
                GUI.Box(barRect, "");
                
                Rect fillRect = new Rect(barRect.x + 2, barRect.y + 2, (barRect.width - 4) * (smoothedAudioLevel / maxThreshold), barRect.height - 4);
                GUI.color = Color.Lerp(Color.green, Color.red, smoothedAudioLevel / maxThreshold);
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
        int CountActiveEffects()
        {
            int count = 0;
            foreach (var targetEffects in activeEffects)
            {
                foreach (var effect in targetEffects.Value)
                {
                    if (effect.Value.IsActive) count++;
                }
            }
            return count;
        }
        
        #endregion
    }
    
    /// <summary>
    /// 音频效果数据结构
    /// </summary>
    [System.Serializable]
    public class AudioEffect
    {
        public AudioEffectType EffectType;
        public GameObject Target;
        public bool IsActive;
        public float CurrentIntensity;
    }
} 