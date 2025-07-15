# SayBoom 团队协作开发指南

## 🎯 项目概述

SayBoom 采用了模块化架构设计，让团队成员可以独立开发不同的功能模块而不会相互干扰。每个模块都有清晰的职责边界和接口定义。

## 👥 团队分工

### 1. 跳跃手感负责人
**负责模块:** MovementSystem  
**工作目录:** `Assets/Scripts/Movement/`  
**主要职责:**
- 优化马里奥式跳跃手感
- 调试跳跃参数 (jumpForce, gravity, coyoteTime等)
- 实现新的移动机制 (二段跳、冲刺等)

### 2. 音频处理负责人  
**负责模块:** AudioSystem  
**工作目录:** `Assets/Scripts/Audio/`  
**主要职责:**
- 优化音频响应效果
- 调整音频敏感度和阈值
- 实现新的音频效果类型

### 3. 关卡搭建负责人
**负责模块:** LevelSystem  
**工作目录:** `Assets/Scripts/Level/`  
**主要职责:**
- 设计和搭建游戏关卡
- 创建关卡数据和预制体
- 开发关卡编辑工具

### 4. 障碍物道具负责人
**负责模块:** GameObjectSystem  
**工作目录:** `Assets/Scripts/GameObject/`  
**主要职责:**
- 设计各种障碍物类型
- 实现道具系统和效果
- 管理对象池和性能优化

## 🏗️ 架构设计原则

### 1. 接口分离
每个系统都实现统一的 `IGameSystem` 接口，确保标准化和可替换性。

### 2. 模块独立
各模块间通过接口通信，避免直接依赖具体实现。

### 3. 配置驱动
使用 ScriptableObject 配置文件来共享参数，便于团队协作。

### 4. 事件解耦
通过事件系统实现模块间的松耦合通信。

## 📁 代码组织结构

```
Assets/Scripts/
├── Core/                    # 核心接口和管理器
│   ├── IGameSystem.cs      # 系统基础接口
│   ├── IMovementSystem.cs  # 移动系统接口
│   ├── IAudioSystem.cs     # 音频系统接口
│   ├── ILevelSystem.cs     # 关卡系统接口
│   ├── IGameObjectSystem.cs# 游戏对象系统接口
│   └── GameManager.cs      # 游戏总管理器
├── Movement/               # 移动模块 (跳跃手感负责人)
│   ├── MovementSystem.cs   # 移动系统实现
│   └── MovementConfig.cs   # 移动配置文件
├── Audio/                  # 音频模块 (音频处理负责人)
│   ├── AudioSystem.cs      # 音频系统实现
│   └── AudioConfig.cs      # 音频配置文件
├── Level/                  # 关卡模块 (关卡搭建负责人)
│   ├── LevelSystem.cs      # 关卡系统实现
│   └── LevelData.cs        # 关卡数据和元素
└── GameObject/             # 游戏对象模块 (障碍物道具负责人)
    └── GameObjectSystem.cs # 游戏对象系统实现
```

## 🚀 快速开始指南

### 跳跃手感开发者

1. **打开移动系统脚本**
   ```
   Assets/Scripts/Movement/MovementSystem.cs
   ```

2. **关键调试参数位置**
   ```csharp
   [Header("跳跃手感调优区域")]
   [SerializeField] private float jumpForce = 12f;        // 跳跃力度
   [SerializeField] private float gravity = 25f;          // 重力强度
   [SerializeField] private float jumpBufferTime = 0.15f; // 跳跃缓冲
   [SerializeField] private float coyoteTime = 0.15f;     // 土狼时间
   ```

3. **实时调试工具**
   - 运行游戏时，左上角显示调试面板
   - 可以实时调整跳跃参数
   - 使用滑条快速测试不同数值

4. **保存和共享配置**
   ```csharp
   // 创建配置文件
   Right-click → Create → SayBoom → Movement → Movement Config
   
   // 保存当前参数到配置
   MovementConfig.SaveFromMovementSystem(movementSystem);
   
   // 应用配置到系统
   MovementConfig.ApplyToMovementSystem(movementSystem);
   ```

### 音频处理开发者

1. **打开音频系统脚本**
   ```
   Assets/Scripts/Audio/AudioSystem.cs
   ```

2. **关键调试参数位置**
   ```csharp
   [Header("音频处理调优区域")]
   [SerializeField] private float sensitivity = 200f;      // 音频敏感度
   [SerializeField] private float minThreshold = 0.001f;   // 最小阈值
   [SerializeField] private float maxThreshold = 0.1f;     // 最大阈值
   [SerializeField] private float smoothingFactor = 0.8f;  // 平滑系数
   ```

3. **实时调试工具**
   - 运行游戏时，右上角显示音频调试面板
   - 实时音频级别可视化
   - 可以调整敏感度和阈值

4. **添加新的音频效果**
   ```csharp
   // 在 AudioEffectType 枚举中添加新类型
   public enum AudioEffectType
   {
       Scale, Color, Rotation, Position,
       YourNewEffect  // 添加你的新效果
   }
   
   // 在 ApplyEffectToTarget 方法中实现效果
   case AudioEffectType.YourNewEffect:
       ApplyYourNewEffect(effect.Target, curveValue);
       break;
   ```

### 关卡搭建开发者

1. **打开关卡系统脚本**
   ```
   Assets/Scripts/Level/LevelSystem.cs
   ```

2. **快速生成测试关卡**
   ```csharp
   // 在 Inspector 中右键菜单
   Context Menu → 生成基础关卡
   
   // 或在运行时调用
   levelSystem.GenerateBasicLevel();
   ```

3. **创建关卡数据文件**
   ```
   Right-click → Create → SayBoom → Level → Level Data
   ```

4. **关卡元素开发**
   ```csharp
   // 所有关卡元素都实现 ILevelElement 接口
   public class YourCustomElement : MonoBehaviour, ILevelElement
   {
       public string ElementName => "CustomElement";
       public LevelElementType ElementType => LevelElementType.Interactive;
       
       public void OnPlayerEnter(GameObject player) { 
           // 玩家进入时的逻辑
       }
   }
   ```

### 障碍物道具开发者

1. **打开游戏对象系统脚本**
   ```
   Assets/Scripts/GameObject/GameObjectSystem.cs
   ```

2. **创建新的障碍物**
   ```csharp
   // 实现 IObstacle 接口
   public class YourObstacle : MonoBehaviour, IObstacle
   {
       public string ObjectName => "CustomObstacle";
       public GameObjectType ObjectType => GameObjectType.Obstacle;
       public ObstacleType ObstacleType => ObstacleType.Moving;
       
       public void OnPlayerHit(GameObject player) {
           // 玩家碰撞时的逻辑
       }
   }
   ```

3. **创建新的道具**
   ```csharp
   // 实现 IPickup 接口
   public class YourPickup : MonoBehaviour, IPickup
   {
       public string ObjectName => "CustomPickup";
       public GameObjectType ObjectType => GameObjectType.Pickup;
       public PickupType PickupType => PickupType.PowerUp;
       
       public void OnPickedUp(GameObject player) {
           // 道具被拾取时的逻辑
       }
   }
   ```

## 🔧 开发工作流程

### 1. 开始开发前
- 确保理解你负责的模块接口
- 查看相关的配置文件格式
- 了解调试工具的使用方法

### 2. 开发过程中
- 只修改你负责的模块文件
- 使用配置文件来调试参数
- 通过事件系统与其他模块通信

### 3. 测试和调试
- 使用内置的调试面板
- 创建测试配置文件
- 在 GameManager 中查看系统状态

### 4. 完成开发后
- 提交你的模块代码
- 分享优化后的配置文件
- 更新相关文档

## 📝 代码规范

### 1. 命名规范
- 类名使用 PascalCase: `MovementSystem`
- 方法名使用 PascalCase: `HandleJump()`
- 变量名使用 camelCase: `jumpForce`
- 常量使用 UPPER_SNAKE_CASE: `MAX_JUMP_COUNT`

### 2. 注释规范
```csharp
/// <summary>
/// 方法或类的简要说明
/// </summary>
/// <param name="paramName">参数说明</param>
/// <returns>返回值说明</returns>
public void YourMethod(float paramName)
{
    // 行内注释说明具体逻辑
}
```

### 3. Inspector 标签
```csharp
[Header("功能模块名称")]
[Tooltip("参数的详细说明")]
[SerializeField] private float yourParameter = 1.0f;
```

## 🐛 调试指南

### 1. 移动系统调试
- 查看左上角的移动系统调试面板
- 实时调整跳跃参数
- 观察 Gizmos 显示的地面检测区域

### 2. 音频系统调试
- 查看右上角的音频系统调试面板
- 观察音频级别可视化条
- 调整敏感度直到达到理想效果

### 3. 关卡系统调试
- 查看左下角的关卡系统调试面板
- 使用快速重载和生成功能
- 检查关卡元素注册状态

### 4. 游戏对象系统调试
- 查看右下角的游戏对象系统面板
- 监控对象池使用情况
- 检查活动对象数量

### 5. 总体系统调试
- 查看右上角的游戏管理器状态
- 监控各系统的初始化和运行状态
- 使用重新初始化功能解决问题

## 🤝 协作最佳实践

### 1. 版本控制
- 每个模块独立提交
- 提交信息格式: `[模块名] 功能描述`
- 例如: `[Movement] 添加二段跳功能`

### 2. 配置文件共享
- 优化好的配置文件提交到仓库
- 在配置文件的描述中说明改进点
- 团队成员可以直接使用或作为参考

### 3. 接口约定
- 不要修改核心接口
- 如需扩展接口，先与团队讨论
- 保持向后兼容性

### 4. 测试协调
- 各模块完成后进行集成测试
- 使用 GameManager 统一管理测试场景
- 及时反馈模块间的交互问题

## 🆘 常见问题解决

### Q: 我的模块没有正确初始化？
A: 检查 GameManager 是否正确引用了你的系统组件，或设置 `autoInitializeSystems = true`

### Q: 参数修改后没有生效？
A: 确保在运行时修改，或者重新初始化系统

### Q: 系统间通信出现问题？
A: 检查事件订阅是否正确，确保在系统初始化后订阅事件

### Q: 性能问题如何定位？
A: 启用 GameManager 的性能监控功能，查看帧时间统计

## 🎉 恭喜！

你现在已经掌握了 SayBoom 模块化开发的所有要点。开始享受独立开发的乐趣吧！

记住：**模块化设计让我们可以专注于自己的领域，同时保持整体协调。**

---

*如有任何问题，请查阅代码注释或咨询其他团队成员。祝开发顺利！* 🚀 