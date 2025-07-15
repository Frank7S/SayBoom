# 地面检测问题解决指南

## 🔧 问题概述

本指南解决常见的地面检测问题：
- 玩家碰到关卡生成的方块掉下去
- Tilemap平台无法识别为地面
- 3D/2D物理系统混用问题

## ✅ 已实施的解决方案

### 1. 关卡生成平台修复
- **问题**：使用3D碰撞器(BoxCollider)，但地面检测用2D物理
- **解决**：自动转换为2D碰撞器(BoxCollider2D)
- **自动设置**：平台创建时自动设置正确的层级

### 2. 智能层级检测
- **问题**：只检测Default层，忽略其他地面层
- **解决**：自动检测常见地面层(Default、Ground、Tilemap、Platform)
- **配置**：LayerMask设为-1时自动智能配置

### 3. Tilemap自动支持
- **问题**：Tilemap需要专门配置才能被识别
- **解决**：自动添加TilemapSupport组件
- **功能**：自动配置碰撞器、层级、CompositeCollider

## 🚀 使用方法

### 方法一：自动配置（推荐）
1. **启动游戏**：系统会自动检测并配置所有地面
2. **检查调试信息**：左上角显示检测的层级信息
3. **验证工作状态**：观察玩家是否能正常站立

### 方法二：手动配置Tilemap
1. **选择Tilemap对象**
2. **右键菜单** → "重新配置Tilemap"
3. **或者添加组件**：TilemapSupport

### 方法三：手动设置层级
```csharp
// 代码中设置
tilemapSupport.SetLayer("Ground");
// 或
tilemapSupport.SetLayer(8); // Ground层通常是8
```

## 🔍 问题诊断

### 检查清单

#### 1. 地面检测配置
- [ ] 地面层遮罩是否包含正确的层级
- [ ] 地面检测半径是否合适(默认0.2f)
- [ ] 地面检测点位置是否正确(玩家脚部)

#### 2. 平台碰撞器
- [ ] 是否使用2D碰撞器(BoxCollider2D)
- [ ] 碰撞器大小是否正确
- [ ] 是否在正确的层级上

#### 3. Tilemap配置
- [ ] 是否有TilemapCollider2D组件
- [ ] 是否使用CompositeCollider2D(可选)
- [ ] Rigidbody2D是否设为Static

### 调试工具

#### 1. 实时调试信息
- **位置**：屏幕左上角
- **显示**：在地面状态、检测层级、速度等
- **切换**：MovementSystem中的showDebugInfo

#### 2. Scene视图Gizmos
- **地面检测范围**：红色/绿色圆圈
- **在地面时**：绿色圆圈
- **离开地面时**：红色圆圈

#### 3. 控制台日志
```
[MovementSystem] 自动配置地面层检测: 265
[LevelSystem] 为Tilemap自动添加支持组件: Grid
[TilemapSupport] 设置到层: Ground
```

## ⚙️ 高级配置

### 自定义地面层
```csharp
// 在MovementSystem中
groundLayerMask = (1 << 0) |        // Default
                  (1 << 8) |        // Ground  
                  (1 << 9) |        // Tilemap
                  (1 << 10);        // Platform
```

### 关闭自动配置
```csharp
// 在TilemapSupport中
[SerializeField] private bool autoSetupOnStart = false;
[SerializeField] private bool setToGroundLayer = false;
[SerializeField] private bool addCompositeCollider = false;
```

### 手动创建支持的平台
```csharp
// 使用关卡系统API
levelSystem.CreatePlatform(position, size);
// 自动配置2D碰撞器和正确层级
```

## 📋 层级设置建议

### Unity层级配置
```
Layer 0: Default (默认地面)
Layer 8: Ground (主要地面层)
Layer 9: Tilemap (瓦片地图层)
Layer 10: Platform (移动平台层)
Layer 11: OneWayPlatform (单向平台层)
```

### 物理设置
```
Edit → Project Settings → Physics 2D
→ Layer Collision Matrix
→ 确保Player层与地面层可以碰撞
```

## 🐛 常见问题

### Q: 玩家还是会掉下去
**A**: 检查以下几点：
1. 平台是否有BoxCollider2D
2. 玩家是否有Rigidbody2D
3. 地面检测LayerMask是否包含平台层级
4. 地面检测点是否在正确位置

### Q: Tilemap没有被检测到
**A**: 
1. 确保Tilemap有TilemapCollider2D
2. 检查TilemapSupport组件是否被添加
3. 验证层级设置是否正确

### Q: 移动平台不工作
**A**:
1. 确保平台有PlatformElement组件
2. 检查isMoving和moveTarget设置
3. 验证moveSpeed > 0

### Q: 地面检测范围太小/太大
**A**:
1. 调整groundCheckRadius参数
2. 检查groundCheck位置
3. 在Scene视图中观察Gizmos显示

## 🔄 系统工作流程

1. **系统初始化**
   - 关卡系统启动
   - 自动检测现有Tilemap
   - 配置地面层检测

2. **平台创建**
   - 使用CreatePrimitive创建基础形状
   - 移除3D碰撞器，添加2D碰撞器
   - 设置到Ground层

3. **地面检测**
   - Physics2D.OverlapCircle检测
   - 多层级支持
   - 实时状态更新

4. **调试反馈**
   - 控制台日志输出
   - 实时GUI显示
   - Scene视图Gizmos

这个系统确保了2D和3D混用场景下的地面检测稳定性，支持各种类型的平台和地面。 