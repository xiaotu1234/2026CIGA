# UI Flow 界面拆分

## 总原则

不要把开始菜单、开局信息、拼装、模拟和结算塞进同一屏。每个界面只服务一个核心注意力。

```text
开始界面
  -> 开局局势 / 捞取材料
  -> 拼装船锚
  -> 下锚模拟
  -> 结算反馈
```

## 0. MainMenuView：开始界面

### 目的

作为游戏入口，提供最基础的开始、设置、退出。

### 显示内容

- 游戏标题。
- 开始按钮。
- 设置按钮。
- 退出按钮。
- 可选：版本号、原型提示。

### 交互

- 点击“开始”进入 StartBriefingView。
- 点击“设置”打开 SettingsView。
- 点击“退出”退出应用。Unity 编辑器内可只打印日志或返回空状态。

### 不做

- 不展示局内详细参数。
- 不展示材料列表。
- 不进行拼装。

## SettingsView：设置界面

### 目的

原型阶段只保留基础设置，不做复杂菜单。

### 显示内容

- 音量开关或滑条。
- 画面质量占位。
- 调试显示开关。
- 返回按钮。

### 交互

- 点击“返回”回到 MainMenuView。

## 1. StartBriefingView：开局局势 / 捞取材料

### 目的

让玩家先理解本局条件，再进入拼装。

### 显示内容

- 船只重量。
- 海况等级。
- 水深。
- 海底类型。
- 危险区距离。
- 稳船目标时间。
- 本局捞到的材料列表。

### 交互

- 点击“开始拼装”进入 BuildView。
- 点击“返回”回到 MainMenuView。
- 原型阶段不需要复杂动画，可以用静态材料列表代替捞取演出。

### 不做

- 不在这里拼装。
- 不在这里展示完整受力模拟。

## 2. BuildView：拼装船锚

### 目的

只让玩家专心拼船锚。

### 显示内容

- 拼装工作区。
- 材料栏。
- 边框贴合提示。
- 连接高亮。
- 船锚绳绑点。
- 风险提示。
- 本局局势摘要。

局势摘要只保留少量信息：

```text
船重 / 海况 / 海底 / 目标时间
```

### 交互

- 拖拽物品。
- 旋转物品。
- 翻转物品。
- 选择绑点。
- 撤销。
- 清空。
- 下锚。
- 返回开局局势。

### 风险提示

提交下锚不硬拦，只提示：

- 主体未连通。
- 有拼接但断链。
- 绑点风险。
- 重量风险。
- 底部风险。
- 触底风险。

### 不做

- 不实时模拟船漂移。
- 不展示完整入水 / 下沉 / 触底过程。

## 3. SimulationView：下锚模拟

### 目的

看船锚是否救得住船。

### 显示内容

- 船。
- 船锚绳。
- 船锚主体。
- 水面。
- 海底。
- 危险区。
- 当前阶段：入水 / 下沉 / 触底。
- 船距离危险区。
- 稳船倒计时。
- 关键连接损伤提示。

### 阶段

1. 入水：底部短时大冲力，弱连接可以被冲开。
2. 下沉：重力、浮力、水流横向力、绳力，连接先位移再损伤。
3. 触底：水流降低，加入摩擦、支撑、抓地，船锚反拉船。

### 交互

- 原型阶段可以有“加速”和“重开”。
- 不允许继续编辑船锚。

## 4. ResultView：结算反馈

### 目的

告诉玩家结果和原因。

### 显示内容

- 成功 / 勉强成功 / 失败。
- 船是否进入危险区。
- 船距离危险区剩余距离。
- 船锚主体损伤。
- 主要失败原因，最多 3 条。

### 交互

- 重新挑战。
- 下一局。
- 返回拼装。
- 返回主菜单。

## UI 脚本建议

```text
Scripts/UI/MainMenu/MainMenuView.cs
Scripts/UI/Settings/SettingsView.cs
Scripts/UI/StartBriefing/StartBriefingView.cs
Scripts/UI/Build/BuildView.cs
Scripts/UI/Simulation/SimulationView.cs
Scripts/UI/Result/ResultView.cs
```

## UI Prefab 建议

```text
Prefabs/UI/MainMenu/MainMenuView.prefab
Prefabs/UI/Settings/SettingsView.prefab
Prefabs/UI/StartBriefing/StartBriefingView.prefab
Prefabs/UI/Build/BuildView.prefab
Prefabs/UI/Simulation/SimulationView.prefab
Prefabs/UI/Result/ResultView.prefab
```