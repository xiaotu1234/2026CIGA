# 船锚模拟代码内调参量说明

本文档只记录当前仍写在 `Assets/Scripts/Simulation/SimulationController.cs` 里的调参量、限制值和内部倍率。

不包含 `item_config.json` 里已经可调的参数。

## 阶段和坐标换算

### WaterEntryDuration = 0.8f

作用：入水阶段持续时间。

使用位置：`Run()`。

逻辑：`simulationElapsed <= WaterEntryDuration` 时执行 `WaterEntryTick()`，超过后进入下沉阶段。

影响：值越大，入水阶段持续越久；值越小，越快进入下沉阶段。

### AnchorScale = 0.55f

作用：把拼装界面的船锚缩放到模拟界面。

使用位置：`BuildAnchorVisualAndPhysics()`。

逻辑：模拟部件尺寸和相对位置都会乘 `AnchorScale`。

影响：值越大，模拟里的锚越大；值越小，模拟里的锚越小。因为碰撞体尺寸来自模拟尺寸，所以它不只是视觉缩放，也会影响碰撞和关节布局。

### PhysicsScale = 0.012f

作用：UI 像素坐标到 Unity 2D 物理世界坐标的换算比例。

使用位置：`ToPhysicsPosition()`、`ToUiPosition()`、碰撞体尺寸、绳子挂点位置。

逻辑：

```text
physicsPosition = uiPosition * PhysicsScale
uiPosition = physicsPosition / PhysicsScale
```

影响：值越大，同样 UI 尺寸对应的物理尺寸越大；会影响碰撞体、速度换算、绳子挂点、地形碰撞位置。这个值是坐标系统尺度，不建议当普通玩法系数频繁调。

### InversePhysicsScale = 1f / PhysicsScale

作用：物理坐标回 UI 坐标的反向换算。

使用位置：`ToUiPosition()`、速度显示、船速换算、抗上浮速度判断。

影响：随 `PhysicsScale` 自动变化，不是独立调参。

## 连接和断裂

### MinEffectiveJointHealth = 0.01f

作用：关节最小有效血量。

使用位置：`ApplyJointDamage()`、`BuildPhysicsJoints()`。

逻辑：如果 `joint.currentHealth <= MinEffectiveJointHealth`，关节视为断裂；如果 `sourceJoint.maxHealth <= MinEffectiveJointHealth`，不创建物理关节。

影响：值越大，关节更早被判定为断；值越小，更接近血量真正到 0 才断。

### globals.jointFrequencyPerDefense = 0.025

作用：关节防御力转 Unity 关节频率的倍率。

使用位置：`BuildPhysicsJoints()`，由 `Assets/Resources_Prototype/TestData/item_config.json` 的 `globals` 读取。

影响：防御越高，生成的关节越硬；防御越低，连接越软。血量只负责扣血和断裂，不再决定关节硬度。

### joint.dampingRatio = 0.65f

作用：Unity 固定关节阻尼比。

使用位置：`BuildPhysicsJoints()`。

影响：值越大，关节振荡衰减越快；值越小，连接更容易来回弹。

### breakForce / breakTorque = Infinity

作用：关闭 Unity 原生关节断裂。

使用位置：`BuildPhysicsJoints()`。

逻辑：关节不再因为 Unity `breakForce` 直接断，而是由当前血量系统扣血到 0 后调用 `BreakJoint()`。

## 绳子拉力

### RopePullAngleDegrees = 45f

作用：绳子实际拉力方向角度。

使用位置：`GetRopePullDirection()`。

影响：改变拉力方向。角度不同会改变水平拉动和竖向拉动比例，也会影响关节反力和整体姿态。

## 反上浮阻力

### AntiLiftDragLinear = 0.08f

作用：物体向上运动时的反向阻力线性系数。

使用位置：`ApplyWeightBuoyancyAndWaterDrag()` 内的 `ApplyAntiLiftPenalty()`。

公式：

```text
upwardSpeedPixels = body.velocity.y / PhysicsScale
acceleration = min(AntiLiftDragMaxAcceleration, upwardSpeedPixels * AntiLiftDragLinear)
force = down * acceleration * mass
```

影响：值越大，物体向上运动时越快被压回；值越小，上浮/弹起更明显。

### AntiLiftDragMaxAcceleration = 18f

作用：反上浮阻力的最大加速度限制。

使用位置：`ApplyWeightBuoyancyAndWaterDrag()` 内的 `ApplyAntiLiftPenalty()`。

影响：限制向下压制力的上限，防止向上速度很大时生成过大的向下力。值越大，上浮会被更强地压住；值越小，上浮更容易保留。

## 水中力

### globals.itemMassScale = 1.0

作用：物品质量系数，由 `item_config.json` 的 `globals.itemMassScale` 控制。物品重量会乘以该系数后写入 Unity `Rigidbody2D.mass`。

使用位置：`BuildAnchorVisualAndPhysics()`。

公式：

```text
body.mass = max(0.1, material.weight * itemMassScale)
```

影响：值越大，物体惯性越大，同样的力越难改变速度；值越小，物体更容易被力推动。它只控制刚体质量，不控制重量下压力。

### WaterAngularDragForceScale = 0.22f

作用：水中旋转阻尼系数。

使用位置：`ApplyWeightBuoyancyAndWaterDrag()`。

公式：

```text
torque = -angularVelocity * clamp(itemFriction * WaterAngularDragForceScale, 0.02, 1.2)
```

影响：值越大，水中旋转更快停；值越小，物体更容易持续翻滚。

### water angular drag clamp = 0.02f 到 1.2f

作用：限制水中旋转阻尼强度的最小和最大值。

使用位置：`ApplyWeightBuoyancyAndWaterDrag()`。

影响：保证水中角阻尼不会低于 `0.02`，也不会高于 `1.2`。

### globals.waterDragForceScale

作用：水阻强度系数，由 `item_config.json` 的 `globals.waterDragForceScale` 控制。

使用位置：`ApplyWeightBuoyancyAndWaterDrag()`。

公式：

```text
waterDrag = -velocity * material.dragCoeff * globalConfig.waterDragForceScale * submerged
```

影响：值越大，水中速度衰减越快；值越小，水阻越弱。不再做代码层最小/最大钳制。

## 水面力

### globals.waterSurfaceTensionCoefficient

作用：水面张力系数，由 `item_config.json` 的 `globals.waterSurfaceTensionCoefficient` 控制。

使用位置：`ApplyWaterSurfaceTension()`。

公式：

```text
tension = waterSurfaceTensionCoefficient * contactStrength * (1 + downwardSpeed) * dragCoeff
```

影响：值越大，穿过水面时竖向张力/托举越强；值越小，水面冲击越弱。不再叠加代码层额外倍率。

## 掉落速度软限制

### globals.dropSpeedLimitMetersPerSecond = 4.0

作用：掉落速度软上限。物体下沉速度超过该值后，会按超出量施加向上阻力。

使用位置：`ApplyDropSpeedResistance()`。

公式：

```text
if velocity.y < -dropSpeedLimitMetersPerSecond:
    velocity.y = -dropSpeedLimitMetersPerSecond
```

影响：值越大，允许的软掉落速度越高，同时超速后的回拉更强；值越小，更早进入软阻力，但回拉更弱。

## 海底地形和碰撞

### globals.seabedFriction = 0.7

作用：海底 `PhysicsMaterial2D.friction`，由 `item_config.json` 的 `globals.seabedFriction` 控制。

使用位置：创建 `SimulationSeabed` 物理材质。

影响：值越大，Unity 原生触底摩擦越强；值越小，物体更容易沿海底滑动。

### globals.seabedBounciness = 0.02

作用：海底 `PhysicsMaterial2D.bounciness`，由 `item_config.json` 的 `globals.seabedBounciness` 控制。

使用位置：创建 `SimulationSeabed` 物理材质。

影响：值越大，触底越容易反弹；值越小，海底越不弹。

### SeabedColliderRadius = 0.28f

作用：海底 EdgeCollider2D 的边缘半径。

使用位置：`EnsureSeabedSurfaceCollider()`。

影响：值越大，海底碰撞体越厚，越容易接触；值越小，海底碰撞更薄。

### SeabedSpawnPadding = 360f

作用：海底地形在屏幕外提前生成的距离。

使用位置：`BuildUnevenSeabed()`、`EnsureSeabedCoverage()`。

影响：值越大，屏幕外预生成地形越多；值越小，可能更接近屏幕边缘才生成。

### SeabedDespawnPadding = 260f

作用：海底地形离开屏幕后延迟清理的距离。

使用位置：`UpdateSeabedTerrain()`。

影响：值越大，离屏地形保留更久；值越小，更快清理。

### SeabedMinY = -1172f

作用：海底地形最低 UI 高度。

使用位置：`AddSeabedSegment()`。

影响：值越小，海底能生成得越深。

### SeabedMaxY = -890f

作用：海底地形最高 UI 高度。

使用位置：`AddSeabedSegment()`。

影响：值越大，海底能生成得越高。

### SeabedMaxStepY = 8f

作用：相邻海底段最大高度差。

使用位置：`AddSeabedSegment()`。

影响：值越大，海底更崎岖；值越小，海底更平缓。当前调低到 8f，让相邻地形段高差更小。

### SeabedSegmentLength = 36f

作用：单段海底地形的水平长度。

使用位置：`BuildUnevenSeabed()`、`EnsureSeabedCoverage()`、`AddSeabedSegment()`。

影响：值越大，海底段更长，地形变化更稀疏；值越小，地形变化更密。

### SeabedVisualThickness = 100f

作用：海底填充图形厚度。

使用位置：`RefreshSeabedFill()`。

影响：只影响海底视觉填充厚度，不直接影响碰撞。

## 船、危险区和镜头表现

### AnchorSpeedPixelsPerMeter = 24f

作用：锚水平移动速度换算成船速的比例。

使用位置：`ApplyAnchorDrivenShipMotion()`、`UpdateSeabedTerrain()`。

公式：

```text
shipVelocity = anchorMotionPixelsPerSecond / AnchorSpeedPixelsPerMeter
```

影响：值越大，同样画面速度换算出的船速越小；值越小，船速显示和危险区进度变化越快。海底滚动会使用 `shipVelocity * AnchorSpeedPixelsPerMeter`，因此和船速上限保持一致。

### shipVelocity clamp = -42f 到 42f

作用：限制船速范围。

使用位置：`ApplyAnchorDrivenShipMotion()`。

影响：防止船速数值无限大。海底滚动速度也会跟随这个限制后的船速。

### DangerZoneRevealDistance = 120f

作用：危险区显示距离。

使用位置：`UpdateDangerZoneVisibility()`。

影响：剩余距离小于该值时显示危险区。

### DangerZoneLeftAnchor = 0.88f

作用：危险区在屏幕中的水平锚点位置。

使用位置：`UpdateDangerZoneVisibility()`。

影响：只影响危险区提示的位置。

### CameraAnchorTargetY = 35f

作用：镜头跟随锚时的目标 Y 偏移。

使用位置：`UpdateCameraFollow()`。

影响：改变镜头纵向跟随位置。

### MaxCameraOffsetY = 1250f

作用：镜头最大纵向偏移限制。

使用位置：`UpdateCameraFollow()`。

影响：防止镜头向下/向上偏移过大。

### SurfaceY = 130f

作用：水面 UI 高度。

使用位置：入水比例计算、初始锚位置、水面逻辑。

影响：改变水面判定高度。

### InitialAnchorPosition = (-330f, SurfaceY)

作用：模拟开始时锚的初始 UI 位置。

使用位置：`BuildAnchorVisualAndPhysics()`。

影响：改变模拟开始时锚出现的位置。

## 绳子视觉

### RopeVisualAngleDegrees = 60f

作用：绳子视觉显示角度。

使用位置：`DrawRope()`。

影响：只影响绳子画面朝向，不直接影响实际拉力方向。

### RopeVisualLengthPadding = 220f

作用：绳子视觉长度补偿。

使用位置：`DrawRope()`。

影响：只影响绳子画面长度，不直接影响实际拉力。
