# 原型实现计划

## 总流程

不要做一屏式 UI。原型按以下界面推进：

```text
StartBriefingView
  -> BuildView
  -> SimulationView
  -> ResultView
```

## 第 1 步：场景骨架

创建 `Scenes/Prototype.unity`。

场景需要包含：

- Camera：2D 正交相机。
- RootCanvas：承载主菜单、设置、开局、拼装、模拟、结算界面。
- MainMenuView：开始界面。
- SettingsView：设置界面或弹窗。
- StartBriefingView：开局局势。
- BuildView：拼装。
- SimulationView：模拟。
- ResultView：结算。
- Ship：船。
- WaterSurface：水面线。
- Seabed：海底线。
- DangerZone：危险区边界。

## 第 2 步：MainMenuView

实现目标：

- 显示游戏标题。
- 显示开始按钮。
- 显示设置按钮。
- 显示退出按钮。
- 点击“开始”进入 StartBriefingView。
- 点击“设置”打开 SettingsView。
- 点击“退出”退出应用；编辑器内可只打印日志。

关键脚本：

- `Scripts/UI/MainMenu/MainMenuView.cs`
- `Scripts/UI/Settings/SettingsView.cs`
- `Scripts/Core/ViewFlowController.cs`
## 第 3 步：StartBriefingView

实现目标：

- 展示船重、海况、水深、海底类型。
- 展示危险区距离和稳船目标时间。
- 展示本局捞到的材料。
- 点击“开始拼装”进入 BuildView。

关键脚本：

- `Scripts/UI/StartBriefing/StartBriefingView.cs`
- `Scripts/Core/RoundController.cs`
- `Scripts/Core/ViewFlowController.cs`

## 第 4 步：BuildView

实现目标：

- 从材料栏生成物品。
- 拖拽物品。
- 旋转物品。
- 翻转物品。
- 松手后检测附近物品边框。
- 边框满足距离、角度、贴合长度后生成连接。
- 生成连接后显示连接高亮。
- 选择船锚绳绑点。
- 提交时只做风险提示，不硬拦。

关键脚本：

- `Scripts/UI/Build/BuildView.cs`
- `Scripts/UI/Build/BuildRiskPanel.cs`
- `Scripts/Build/BuildController.cs`
- `Scripts/Build/PieceDragController.cs`
- `Scripts/Build/PieceRotateController.cs`
- `Scripts/Build/AttachOutline.cs`
- `Scripts/Build/AttachDetector.cs`
- `Scripts/Build/AttachJoint.cs`
- `Scripts/Build/AttachGraph.cs`
- `Scripts/Build/BuildRiskEvaluator.cs`

## 第 5 步：SimulationView

实现目标：

- 从 BuildView 的结构生成可模拟船锚。
- 播放入水、下沉、触底三阶段。
- 显示船、船锚绳、船锚、水面、海底、危险区。
- 显示船距离危险区和稳船倒计时。
- 模拟阶段不允许继续编辑。

入水阶段：

- 船锚底部碰到水面时，从下方对底部材料施加向上冲力。
- 弱连接可以被直接冲开。

下沉阶段：

- 计算重力、浮力、水阻、水流横向力、船锚绳拉力。
- 连接先产生位移，再滑移，再降低贴合长度，最后断开。

触底阶段：

- 水流横向力降低。
- 加入海底支撑、摩擦、抓地。
- 船向危险区漂移，船锚通过绳子反拉船。

关键脚本：

- `Scripts/UI/Simulation/SimulationView.cs`
- `Scripts/UI/Simulation/StageProgressView.cs`
- `Scripts/Simulation/SimulationController.cs`
- `Scripts/Simulation/WaterEntryStage.cs`
- `Scripts/Simulation/SinkStage.cs`
- `Scripts/Simulation/SeabedStage.cs`
- `Scripts/Simulation/ShipDriftSimulator.cs`
- `Scripts/Simulation/RopeForceSystem.cs`
- `Scripts/Simulation/JointDamageSystem.cs`

## 第 6 步：ResultView

实现目标：

- 显示成功、勉强成功或失败。
- 显示船是否进入危险区。
- 显示船距离危险区剩余距离。
- 显示船锚主体损伤。
- 显示最多 3 条失败原因。
- 支持重新挑战 / 下一局 / 返回拼装。

关键脚本：

- `Scripts/UI/Result/ResultView.cs`
- `Scripts/UI/Result/ResultReasonList.cs`
- `Scripts/Simulation/ResultEvaluator.cs`

## 最小可玩验收

- 能从 MainMenuView 点击开始进入 StartBriefingView。
- 能从 StartBriefingView 进入 BuildView。
- 能拼 3-5 个物品。
- 能看到连接高亮。
- 能看到未连通风险提示。
- 能从 BuildView 进入 SimulationView。
- 入水能冲开弱连接。
- 下沉时连接先滑移再断。
- 触底后船锚能减缓船漂移。
- 船未进危险区成功，进危险区失败。
- 能进入 ResultView 查看原因。