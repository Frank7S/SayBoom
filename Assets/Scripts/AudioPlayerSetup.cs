using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 快速设置音频控制玩家的帮助脚本
/// 在Inspector中点击"快速创建音频控制玩家"按钮即可自动设置
/// </summary>
public class AudioPlayerSetup : MonoBehaviour
{
    [Header("快速设置")]
    [Tooltip("点击下方按钮快速创建音频控制的2D玩家对象")]
    public bool showInstructions = true;
    
    [Header("创建设置")]
    public Sprite playerSprite;
    public Color playerColor = Color.white;
    public Vector2 playerSize = Vector2.one;
    public Vector3 spawnPosition = Vector3.zero;
    
    [Space(10)]
    [Header("移动预设参数")]
    public float moveSpeed = 5f;
    public float jumpForce = 12f;
    public float gravity = 25f;
    
    [Header("音频预设参数")]
    public float scaleSensitivity = 200f;  // 修改默认值为200
    public float minScale = 0.5f;
    public float maxScale = 3f;
    
    // 在Inspector中显示按钮和说明
    void OnValidate()
    {
        // 这个方法在Inspector值改变时调用
    }
    
    /// <summary>
    /// 创建基础的2D游戏对象
    /// </summary>
    public GameObject CreateBasic2DPlayer()
    {
        // 创建游戏对象
        GameObject player = new GameObject("AudioControlledPlayer");
        
        // 添加SpriteRenderer
        SpriteRenderer spriteRenderer = player.AddComponent<SpriteRenderer>();
        
        // 设置精灵
        if (playerSprite != null)
        {
            spriteRenderer.sprite = playerSprite;
        }
        else
        {
            // 如果没有提供精灵，创建一个简单的正方形
            spriteRenderer.sprite = CreateDefaultSquareSprite();
        }
        
        spriteRenderer.color = playerColor;
        
        // 设置位置和大小
        player.transform.position = spawnPosition;
        player.transform.localScale = new Vector3(playerSize.x, playerSize.y, 1f);
        
        // 添加AudioControlledPlayer脚本（它会自动添加PlayerController和AudioController）
        AudioControlledPlayer audioController = player.AddComponent<AudioControlledPlayer>();
        
        // 等待组件自动设置完成，然后配置预设参数
        audioController.SetMoveSpeed(moveSpeed);
        audioController.SetJumpForce(jumpForce);
        audioController.SetScaleSensitivity(scaleSensitivity);
        
        // 添加Collider2D（可选）
        player.AddComponent<BoxCollider2D>();

        
        Debug.Log("已创建音频控制玩家对象：" + player.name);
        
        return player;
    }
    
    /// <summary>
    /// 创建带有音频文件输入的玩家
    /// </summary>
    public GameObject CreatePlayerWithAudioClip(AudioClip audioClip)
    {
        GameObject player = CreateBasic2DPlayer();
        
        // 添加AudioSource组件
        AudioSource audioSource = player.AddComponent<AudioSource>();
        audioSource.clip = audioClip;
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.volume = 0.5f;
        
        // 设置AudioControlledPlayer使用这个AudioSource
        AudioControlledPlayer audioController = player.GetComponent<AudioControlledPlayer>();
        audioController.SetAudioSource(audioSource);
        
        Debug.Log("已创建带音频文件的玩家对象，音频文件：" + audioClip.name);
        
        return player;
    }
    
    /// <summary>
    /// 创建默认的正方形精灵
    /// </summary>
    private Sprite CreateDefaultSquareSprite()
    {
        // 创建一个简单的白色正方形纹理
        Texture2D texture = new Texture2D(64, 64);
        Color[] pixels = new Color[64 * 64];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // 创建精灵
        return Sprite.Create(texture, new Rect(0, 0, 64, 64), Vector2.one * 0.5f);
    }
    
    /// <summary>
    /// 创建简单的地面平台
    /// </summary>
    public void CreateSimpleGround()
    {
        // 创建地面对象
        GameObject ground = new GameObject("Ground");
        
        // 添加SpriteRenderer
        SpriteRenderer groundRenderer = ground.AddComponent<SpriteRenderer>();
        groundRenderer.sprite = CreateGroundSprite();
        groundRenderer.color = new Color(0.5f, 0.3f, 0.1f); // 棕色
        
        // 设置位置和大小
        ground.transform.position = new Vector3(0, -3f, 0);
        ground.transform.localScale = new Vector3(10f, 1f, 1f);
        
        // 添加碰撞器
        BoxCollider2D groundCollider = ground.AddComponent<BoxCollider2D>();
        
        Debug.Log("已创建地面平台：" + ground.name);
    }
    
    /// <summary>
    /// 创建地面精灵
    /// </summary>
    private Sprite CreateGroundSprite()
    {
        // 创建一个简单的棕色矩形纹理
        Texture2D texture = new Texture2D(64, 32);
        Color[] pixels = new Color[64 * 32];
        
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = Color.white;
        }
        
        texture.SetPixels(pixels);
        texture.Apply();
        
        // 创建精灵
        return Sprite.Create(texture, new Rect(0, 0, 64, 32), Vector2.one * 0.5f);
    }
    
    /// <summary>
    /// 设置摄像机为2D模式
    /// </summary>
    public void Setup2DCamera()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = 5f;
            mainCamera.transform.position = new Vector3(0, 0, -10);
            
            Debug.Log("已设置主摄像机为2D模式");
        }
        else
        {
            Debug.LogWarning("未找到主摄像机！");
        }
    }
    
    /// <summary>
    /// 完整的场景设置
    /// </summary>
    [ContextMenu("完整设置场景")]
    public void CompleteSceneSetup()
    {
        // 设置2D摄像机
        Setup2DCamera();
        
        // 创建玩家
        GameObject player = CreateBasic2DPlayer();
        
        // 选中创建的对象
        #if UNITY_EDITOR
        UnityEditor.Selection.activeGameObject = player;
        #endif
        
        // 创建一个简单的地面平台
        CreateSimpleGround();
        
        Debug.Log("场景设置完成！可以开始游戏了。");
        Debug.Log("控制说明：A/D移动，空格跳跃，对着麦克风说话控制缩放");
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AudioPlayerSetup))]
public class AudioPlayerSetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        AudioPlayerSetup setup = (AudioPlayerSetup)target;
        
        EditorGUILayout.Space();
        
        if (setup.showInstructions)
        {
            EditorGUILayout.HelpBox(
                "这个脚本帮助你快速设置音频控制的2D游戏对象（马里奥式跳跃）。\n\n" +
                "步骤：\n" +
                "1. 可选：拖拽一个精灵到Player Sprite字段\n" +
                "2. 调整移动和音频设置（可选）\n" +
                "3. 点击下方的按钮创建对象\n" +
                "4. 创建地面平台（添加带Collider2D的GameObject）\n" +
                "5. 运行游戏：A/D移动，空格跳跃，对着麦克风说话控制缩放",
                MessageType.Info
            );
        }
        
        EditorGUILayout.Space();
        
        // 创建按钮
        if (GUILayout.Button("快速创建音频控制玩家", GUILayout.Height(30)))
        {
            setup.CompleteSceneSetup();
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("仅创建玩家对象", GUILayout.Height(25)))
        {
            GameObject player = setup.CreateBasic2DPlayer();
            Selection.activeGameObject = player;
        }
        
        if (GUILayout.Button("仅设置2D摄像机", GUILayout.Height(25)))
        {
            setup.Setup2DCamera();
        }
        
        if (GUILayout.Button("创建地面平台", GUILayout.Height(25)))
        {
            setup.CreateSimpleGround();
        }
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("高级选项", EditorStyles.boldLabel);
        
        if (GUILayout.Button("删除这个设置脚本"))
        {
            if (EditorUtility.DisplayDialog("确认删除", "确定要删除这个设置脚本吗？", "删除", "取消"))
            {
                DestroyImmediate(setup);
            }
        }
    }
}
#endif 