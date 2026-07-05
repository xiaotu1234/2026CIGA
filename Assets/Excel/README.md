# 配置表本地更新工具

## 使用方式

1. 把最新配置表放到本目录，并命名为 `excel.xlsx`。
2. 保存并关闭 Excel。
3. 双击运行 `update_config.bat`。
4. 工具会更新以下文件：
   - `Assets/Resources_Prototype/TestData/item_config.json`
   - `Assets/Resources_Prototype/TestData/level_config.json`
   - `Assets/Resources_Prototype/TestData/global_config.json`
   - `Assets/Resources_Prototype/TestData/sheet1_config.json`
5. 回到 Unity，等待资源刷新。

## 注意

- 表内道具重量按 kg 处理，会原样写入 `weightKg`，不会除以 1000。
- 关卡表第 4 列如果是公式，工具会把 Excel 当前缓存值写入 `shipSpeedDisplay`，并把公式文本记录到 `shipSpeedFormula`。
- 如果 Excel 仍然打开，工具会停止并提示先保存关闭，避免读到未保存的旧数据。
- 运行环境需要本机有 Python，并安装 `openpyxl`：

```bat
python -m pip install openpyxl
```

## 命令行用法

```bat
Assets\Excel\update_config.bat
```

也可以指定其他表或输出目录：

```bat
python Assets\Excel\update_config.py --excel Assets\Excel\excel.xlsx --output Assets\Resources_Prototype\TestData
```
