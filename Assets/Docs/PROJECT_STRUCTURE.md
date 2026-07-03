# Project Structure 项目目录结构说明

本文档说明 `Assets` 目录下与原型相关的文件夹用途。接手开发时先读本文件，再读 `UI_FLOW.md` 和 `DEVELOPMENT_PLAN.md`。


## 每个目录内的 _FOLDER_README.md

`Assets` 下每个文件夹都放置了 `_FOLDER_README.md`。

用途：

- 说明该目录是干什么的。
- 说明可以放什么。
- 说明不要放什么。
- 预留“后续限制词”区域，后续可继续补充命名规则、禁止资源类型、禁止职责混入等约束。

程序 AI 在向某个目录新增文件前，应先阅读该目录内的 `_FOLDER_README.md`。

## 顶层目录

```text
Assets/
  _AI_README.md
  Docs/
  Scenes/
  Scripts/
  Prefabs/
  ScriptableObjects/
  Art/
  ArtResources/
  Resources_Prototype/
```

## _AI_README.md

项目给 AI 或开发者的入口说明。

用途：

- 快速了解玩法目标。
- 快速了解界面流。
- 指向关键文档。
- 标记不要先做的内容。

## Docs

策划、架构和开发计划文档。

```text
Docs/
  PROJECT_STRUCTURE.md
  UI_FLOW.md
  DEVELOPMENT_PLAN.md
  PROTOTYPE_PLAN.md
  ARCHITECTURE.md
  DATA_SCHEMA.md
  TODO_FOR_PROGRAMMER_AI.md
```

用途：

- `PROJECT_STRUCTURE.md`：说明目录结构。
- `UI_FLOW.md`：说明主菜单、开局、拼装、模拟、结算的界面拆分。
- `DEVELOPMENT_PLAN.md`：说明模块拆分、优先级和验收标准。
- `PROTOTYPE_PLAN.md`：说明第一版原型开发步骤。
- `ARCHITECTURE.md`：说明脚本模块职责。
- `DATA_SCHEMA.md`：说明配置数据结构。
- `TODO_FOR_PROGRAMMER_AI.md`：给程序 AI 的任务清单。

## Scenes

Unity 场景目录。

建议：

```text
Scenes/
  Prototype.unity
```

用途：

- 第一版只需要一个 `Prototype.unity`。
- 不要一开始拆多个正式场景。
- 所有 UI 可以先放在一个场景里，用 `ViewFlowController` 控制显隐。

## Scripts

全部 C# 脚本目录。

```text
Scripts/
  Core/
  Config/
  Build/
  Simulation/
  Pieces/
  UI/
  Debug/
```

### Scripts/Core

局内流程和界面流控制。

建议脚本：

- `GameBootstrap.cs`
- `GameStateMachine.cs`
- `RoundController.cs`
- `ViewFlowController.cs`

### Scripts/Config

配置类和 ScriptableObject 定义。

建议脚本：

- `MaterialConfig.cs`
- `AnchorPieceConfig.cs`
- `AttachConfig.cs`
- `StageConfig.cs`
- `LevelConfig.cs`

### Scripts/Build

拼装阶段逻辑。

建议脚本：

- `BuildController.cs`
- `PieceDragController.cs`
- `PieceRotateController.cs`
- `AttachOutline.cs`
- `AttachDetector.cs`
- `AttachJoint.cs`
- `AttachGraph.cs`
- `BuildRiskEvaluator.cs`

### Scripts/Simulation

下锚模拟逻辑。

建议脚本：

- `SimulationController.cs`
- `WaterEntryStage.cs`
- `SinkStage.cs`
- `SeabedStage.cs`
- `ForceModel.cs`
- `JointDamageSystem.cs`
- `PieceMotionState.cs`
- `ShipDriftSimulator.cs`
- `RopeForceSystem.cs`
- `ResultEvaluator.cs`

### Scripts/Pieces

材料物体本体逻辑。

建议脚本：

- `AnchorPiece.cs`
- `PiecePhysicsBody.cs`
- `PieceVisual.cs`
- `PieceColliderSetup.cs`

### Scripts/UI

UI 界面脚本。

```text
Scripts/UI/
  MainMenu/
  Settings/
  StartBriefing/
  Build/
  Simulation/
  Result/
```

用途：

- `MainMenu`：开始 / 设置 / 退出。
- `Settings`：音量、画面质量占位、调试开关。
- `StartBriefing`：开局局势和捞取材料展示。
- `Build`：拼装船锚界面和风险提示。
- `Simulation`：下锚模拟界面和阶段显示。
- `Result`：结算界面和失败原因列表。

### Scripts/Debug

调试显示与验证工具。

建议脚本：

- `DebugOverlay.cs`
- `ForceGizmoDrawer.cs`
- `AttachDebugDrawer.cs`

## Prefabs

Prefab 目录。

```text
Prefabs/
  Gameplay/
  Pieces/
  UI/
```

### Prefabs/Gameplay

局内环境与玩法对象。

建议放：

- `Ship.prefab`
- `Rope.prefab`
- `WaterSurface.prefab`
- `Seabed.prefab`
- `DangerZone.prefab`

### Prefabs/Pieces

材料物品 Prefab。

第一版建议只做：

- `IronBlock.prefab`
- `WoodPlank.prefab`
- `Hook.prefab`
- `Stone.prefab`
- `Tire.prefab`

### Prefabs/UI

UI Prefab。

```text
Prefabs/UI/
  MainMenu/
  Settings/
  StartBriefing/
  Build/
  Simulation/
  Result/
```

建议放：

- `MainMenuView.prefab`
- `SettingsView.prefab`
- `StartBriefingView.prefab`
- `BuildView.prefab`
- `SimulationView.prefab`
- `ResultView.prefab`

## ScriptableObjects

数据配置资源目录。

```text
ScriptableObjects/
  Materials/
  Pieces/
  Levels/
  StageConfigs/
```

用途：

- `Materials`：材料属性，如重量、密度、粘合、水阻、摩擦、抓地。
- `Pieces`：具体物品配置，如铁块、木板、钩子、石块、轮胎。
- `Levels`：关卡/局配置，如船重、海况、水深、危险区距离、目标时间。
- `StageConfigs`：入水、下沉、触底阶段参数。

## Art

原型美术资源目录。

```text
Art/
  Sprites/
    Pieces/
    Environment/
    UI/
  Materials/
  VFX/
```

用途：

- `Sprites/Pieces`：材料图形。
- `Sprites/Environment`：船、水面、海底、危险区。
- `Sprites/UI`：按钮、面板、图标。
- `Materials`：Unity 材质。
- `VFX`：水花、冲击、断裂等特效占位。

## ArtResources

项目已有资源目录。

规则：

- 如果项目已有美术资源放在这里，不要随意移动。
- 新原型资源优先放 `Art`。
- 如果后续确认 `ArtResources` 是正式资源根，再统一迁移规划。

## Resources_Prototype

原型测试数据目录。

```text
Resources_Prototype/
  TestData/
```

用途：

- 临时测试数据。
- 不建议正式上线依赖此目录。
- 原型阶段可用于快速加载测试配置。

## .codex_trash

项目根目录下的回收备份目录，不在 `Assets` 内。

用途：

- 存放被移动走的旧目录或误建目录。
- 不直接永久删除项目文件。
- 例如旧的空 `Perfabs` 目录被移到这里。

## 命名注意

- 使用 `Prefabs`，不要使用 `Perfabs`。
- UI 脚本按界面分文件夹。
- 原型只做一个 `Prototype.unity` 场景。
- 不要把所有 UI 放在一个脚本里。
- 不要把拼装、模拟、结算逻辑混到 UI 脚本里。