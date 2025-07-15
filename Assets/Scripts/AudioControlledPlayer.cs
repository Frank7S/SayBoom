using UnityEngine;

public class AudioControlledPlayer : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;
    
    [Header("音频设置")]
    public AudioSource audioSource;
    public float scaleSensitivity = 2f;
    public float minScale = 0.5f;
    public float maxScale = 3f;
    public float scaleSmoothing = 5f;
    
    [Header("音频采样设置")]
    public int sampleSize = 1024;
    public FFTWindow fftWindow = FFTWindow.Rectangular;
    
    private Vector3 originalScale;
    private Vector3 targetScale;
    private float[] audioSpectrum;
    
    void Start()
    {
        // 保存原始缩放
        originalScale = transform.localScale;
        targetScale = originalScale;
        
        // 初始化音频频谱数组
        audioSpectrum = new float[sampleSize];
        
        // 如果没有指定音频源，尝试获取组件
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
        
        // 如果仍然没有音频源，添加一个并设置为从麦克风输入
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            SetupMicrophoneInput();
        }
    }
    
    void Update()
    {
        HandleMovement();
        HandleAudioScaling();
    }
    
    void HandleMovement()
    {
        // 获取输入
        float horizontal = Input.GetAxis("Horizontal"); // A/D 键
        float vertical = Input.GetAxis("Vertical");     // W/S 键
        
        // 计算移动向量
        Vector3 movement = new Vector3(horizontal, vertical, 0) * moveSpeed * Time.deltaTime;
        
        // 应用移动
        transform.Translate(movement);
    }
    
    void HandleAudioScaling()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            // 获取音频频谱数据
            AudioListener.GetSpectrumData(audioSpectrum, 0, fftWindow);
            
            // 计算音频强度（取前几个频段的平均值）
            float audioLevel = 0f;
            int frequencyBands = Mathf.Min(64, sampleSize); // 只取前64个频段
            
            for (int i = 0; i < frequencyBands; i++)
            {
                audioLevel += audioSpectrum[i];
            }
            
            audioLevel /= frequencyBands;
            
            // 将音频级别转换为缩放因子
            float scaleMultiplier = 1f + (audioLevel * scaleSensitivity);
            scaleMultiplier = Mathf.Clamp(scaleMultiplier, minScale, maxScale);
            
            // 设置目标缩放
            targetScale = originalScale * scaleMultiplier;
        }
        else
        {
            // 如果没有音频播放，恢复到原始大小
            targetScale = originalScale;
        }
        
        // 平滑过渡到目标缩放
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, scaleSmoothing * Time.deltaTime);
    }
    
    void SetupMicrophoneInput()
    {
        // 检查是否有可用的麦克风
        if (Microphone.devices.Length > 0)
        {
            // 使用默认麦克风
            string microphoneName = Microphone.devices[0];
            
            // 创建音频片段从麦克风录制
            audioSource.clip = Microphone.Start(microphoneName, true, 1, AudioSettings.outputSampleRate);
            audioSource.loop = true;
            
            // 等待麦克风开始录制
            while (!(Microphone.GetPosition(microphoneName) > 0)) { }
            
            // 播放麦克风输入
            audioSource.Play();
            
            Debug.Log($"已连接麦克风: {microphoneName}");
        }
        else
        {
            Debug.LogWarning("未检测到可用的麦克风设备！");
        }
    }
    
    // 可选：添加手动音频控制方法
    public void SetAudioSource(AudioSource newAudioSource)
    {
        audioSource = newAudioSource;
    }
    
    // 可选：获取当前音频级别（用于调试）
    public float GetCurrentAudioLevel()
    {
        if (audioSpectrum == null) return 0f;
        
        float level = 0f;
        int frequencyBands = Mathf.Min(64, sampleSize);
        
        for (int i = 0; i < frequencyBands; i++)
        {
            level += audioSpectrum[i];
        }
        
        return level / frequencyBands;
    }
    
    // 在Inspector中显示当前状态
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying)
        {
            // 绘制移动范围指示器
            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
            // 绘制缩放范围指示器
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position, originalScale * minScale);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, originalScale * maxScale);
        }
    }
} 