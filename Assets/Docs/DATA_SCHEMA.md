# 数据结构建议

## MaterialConfig

```text
id
displayName
weight
density
adhesive
tensileStrength
shearStrength
dragCoeff
frictionCoeff
supportCoeff
gripCoeff
tags
```

## AnchorPieceConfig

```text
id
displayName
materialConfig
visualSprite
attachOutlineData
physicsColliderData
size
defaultMass
```

## AttachConfig

```text
snapTolerance = 8 px
maxAttachAngle = 18 deg
minAttachLength = 16 px
maxPenetration = 6 px
hardPenetration = 18 px
elasticDisplacementLimit = 4 px
maxJointDisplacement = 18 px
displacementGain = 0.12
attachLengthDecay = 0.08
sinkBreakDelay = 0.6 sec
```

## AttachJoint Runtime Data

```text
pieceA
pieceB
initialAttachLength
currentAttachLength
relativeDisplacement
damage
isBroken
jointState
```

`jointState`：

```text
Stable
Stretching
Sliding
Broken
```

## StageConfig

```text
waterImpactBase
waterImpactDuration
currentForceBase
ropeForceBase
seabedCurrentDecay
seabedFrictionMul
seabedSupportMul
stableDuration
dangerZoneDistance
shipDriftForceBase
shipSafeMargin
```

## LevelConfig

```text
shipWeight
seaState
waterDepth
seabedType
buildTime
materialCount
dangerZoneDistance
stableDuration
```
