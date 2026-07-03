# 模块架构

## 界面流总控

游戏按 5 个主界面拆分，不做一屏承载全部内容。

```text
MainMenuView -> StartBriefingView -> BuildView -> SimulationView -> ResultView
```

## Core

负责局内流程和界面切换。

| 脚本 | 职责 |
|---|---|
| `GameBootstrap.cs` | 初始化原型场景 |
| `GameStateMachine.cs` | 控制 Briefing / Build / Simulate / Result 状态 |
| `RoundController.cs` | 管理一局游戏的开始、提交、模拟、结算 |
| `ViewFlowController.cs` | 控制主菜单、开局、拼装、模拟、结算界面的显隐与跳转 |

## Config

负责可调参数和 ScriptableObject 数据。

| 脚本 | 职责 |
|---|---|
| `MaterialConfig.cs` | 材料基础属性 |
| `AnchorPieceConfig.cs` | 单个物品配置 |
| `AttachConfig.cs` | 边框贴合参数 |
| `StageConfig.cs` | 入水、下沉、触底阶段参数 |
| `LevelConfig.cs` | 船重、海况、水深、危险区等关卡参数 |

## Build

负责拼装阶段，只被 BuildView 使用。

| 脚本 | 职责 |
|---|---|
| `BuildController.cs` | 拼装阶段总控 |
| `PieceDragController.cs` | 拖拽 |
| `PieceRotateController.cs` | 旋转和翻转 |
| `AttachOutline.cs` | 拼装边框数据 |
| `AttachDetector.cs` | 检测边框贴合 |
| `AttachJoint.cs` | 连接数据和状态 |
| `AttachGraph.cs` | 连接图和主体连通 |
| `BuildRiskEvaluator.cs` | 提交前风险提示 |

## Simulation

负责下锚后的三阶段，只被 SimulationView 使用。

| 脚本 | 职责 |
|---|---|
| `SimulationController.cs` | 模拟阶段总控 |
| `WaterEntryStage.cs` | 入水短时冲力 |
| `SinkStage.cs` | 下沉持续受力 |
| `SeabedStage.cs` | 触底摩擦、支撑、抓地 |
| `ForceModel.cs` | 力计算函数 |
| `JointDamageSystem.cs` | 位移、滑移、贴合长度损伤、断开 |
| `PieceMotionState.cs` | 单个材料的运动状态 |
| `ShipDriftSimulator.cs` | 船向危险区漂移 |
| `RopeForceSystem.cs` | 船锚绳反拉力 |
| `ResultEvaluator.cs` | 成功失败判断 |

## Pieces

负责材料物体本体。

| 脚本 | 职责 |
|---|---|
| `AnchorPiece.cs` | 单个材料对象入口 |
| `PiecePhysicsBody.cs` | 质量、速度、受力 |
| `PieceVisual.cs` | 显示图形 |
| `PieceColliderSetup.cs` | 实际碰撞箱配置 |

## UI

负责主菜单、设置和四个局内界面。

| 脚本 | 职责 |
|---|---|
| `MainMenu/MainMenuView.cs` | 开始、设置、退出 |
| `Settings/SettingsView.cs` | 设置弹窗或设置界面 |
| `StartBriefing/StartBriefingView.cs` | 开局局势和材料预览 |
| `Build/BuildView.cs` | 拼装界面 |
| `Build/BuildRiskPanel.cs` | 拼装风险提示 |
| `Simulation/SimulationView.cs` | 下锚模拟界面 |
| `Simulation/StageProgressView.cs` | 入水 / 下沉 / 触底阶段显示 |
| `Result/ResultView.cs` | 结算界面 |
| `Result/ResultReasonList.cs` | 失败原因列表 |

## Debug

只为原型验证服务。

| 脚本 | 职责 |
|---|---|
| `DebugOverlay.cs` | 显示参数和状态 |
| `ForceGizmoDrawer.cs` | 画力方向 |
| `AttachDebugDrawer.cs` | 画边框、贴合长度、连接状态 |