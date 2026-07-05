param(
  [string]$Excel,
  [string]$Output
)

$ErrorActionPreference = "Stop"

Add-Type -AssemblyName System.IO.Compression.FileSystem

$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent (Split-Path -Parent $ScriptDir)
if (-not $Excel) {
  $Excel = Join-Path $ScriptDir "excel.xlsx"
}
if (-not $Output) {
  $Output = Join-Path $ProjectRoot "Assets\Resources_Prototype\TestData"
}

$RequiredSheets = @("item", "global", "level", "Sheet1")

function Resolve-FullPath([string]$PathValue) {
  if ([System.IO.Path]::IsPathRooted($PathValue)) {
    return [System.IO.Path]::GetFullPath($PathValue)
  }
  return [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $PathValue))
}

function Get-DisplayPath([string]$PathValue) {
  $full = Resolve-FullPath $PathValue
  $root = Resolve-FullPath $ProjectRoot
  if ($full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
    return $full.Substring($root.Length).TrimStart("\").Replace("\", "/")
  }
  return $full
}

function Read-ZipText($Zip, [string]$Name) {
  $entry = $Zip.GetEntry($Name)
  if ($null -eq $entry) {
    return $null
  }
  $reader = [System.IO.StreamReader]::new($entry.Open())
  try {
    return $reader.ReadToEnd()
  }
  finally {
    $reader.Dispose()
  }
}

function Get-XmlText($Node) {
  if ($null -eq $Node) {
    return ""
  }
  if ($Node -is [string]) {
    return [string]$Node
  }
  return [string]$Node.InnerText
}

function Get-ColumnIndex([string]$CellRef) {
  $letters = ([regex]::Match($CellRef, "^[A-Z]+")).Value
  $index = 0
  foreach ($char in $letters.ToCharArray()) {
    $index = ($index * 26) + ([int][char]$char - [int][char]'A' + 1)
  }
  return $index
}

function Get-RowIndex([string]$CellRef) {
  return [int]([regex]::Match($CellRef, "\d+")).Value
}

function Convert-SharedFormula([string]$Formula, [int]$ColumnDelta, [int]$RowDelta) {
  return [regex]::Replace($Formula, '(?<![A-Za-z0-9_])(\$?)([A-Z]{1,3})(\$?)(\d+)', {
    param($match)
    $absCol = $match.Groups[1].Value
    $colText = $match.Groups[2].Value
    $absRow = $match.Groups[3].Value
    $rowText = $match.Groups[4].Value
    $colIndex = Get-ColumnIndex $colText
    $rowIndex = [int]$rowText
    if ($absCol -ne '$') {
      $colIndex += $ColumnDelta
    }
    if ($absRow -ne '$') {
      $rowIndex += $RowDelta
    }
    $letters = ""
    while ($colIndex -gt 0) {
      $mod = ($colIndex - 1) % 26
      $letters = [char]([int][char]'A' + $mod) + $letters
      $colIndex = [math]::Floor(($colIndex - 1) / 26)
    }
    return "$absCol$letters$absRow$rowIndex"
  })
}

function Convert-CellValue($Cell, [string[]]$SharedStrings) {
  $type = [string]$Cell.t
  $valueNode = $Cell.v
  if ($null -eq $valueNode) {
    return $null
  }
  $raw = [string]$valueNode
  if ($type -eq "s") {
    return $SharedStrings[[int]$raw]
  }
  if ($type -eq "str" -or $type -eq "inlineStr") {
    return $raw.Trim()
  }
  if ($type -eq "b") {
    return $raw -eq "1"
  }
  $number = 0.0
  if ([double]::TryParse($raw, [System.Globalization.NumberStyles]::Float, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$number)) {
    if ($number % 1 -eq 0) {
      return [int]$number
    }
    return $number
  }
  return $raw.Trim()
}

function Read-Workbook([string]$WorkbookPath) {
  $zip = [System.IO.Compression.ZipFile]::OpenRead($WorkbookPath)
  try {
    $sharedStrings = @()
    $sharedXmlText = Read-ZipText $zip "xl/sharedStrings.xml"
    if ($sharedXmlText) {
      [xml]$sharedXml = $sharedXmlText
      foreach ($si in $sharedXml.sst.si) {
        $textParts = @()
        if ($si.t) {
          $textParts += Get-XmlText $si.t
        }
        if ($si.r) {
          foreach ($run in $si.r) {
            $textParts += Get-XmlText $run.t
          }
        }
        $sharedStrings += ($textParts -join "")
      }
    }

    [xml]$workbookXml = Read-ZipText $zip "xl/workbook.xml"
    [xml]$relsXml = Read-ZipText $zip "xl/_rels/workbook.xml.rels"
    $relationships = @{}
    foreach ($rel in $relsXml.Relationships.Relationship) {
      $relationships[[string]$rel.Id] = [string]$rel.Target
    }

    $sheets = @{}
    foreach ($sheet in $workbookXml.workbook.sheets.sheet) {
      $sheetName = [string]$sheet.name
      $rid = $sheet.GetAttribute("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships")
      $target = $relationships[$rid]
      if (-not $target.StartsWith("xl/")) {
        $target = "xl/$target"
      }
      [xml]$sheetXml = Read-ZipText $zip $target
      $sharedFormulas = @{}
      $rows = @{}

      foreach ($row in $sheetXml.worksheet.sheetData.row) {
        $rowIndex = [int]$row.r
        $cells = @{}
        foreach ($cell in $row.c) {
          $cellRef = [string]$cell.r
          $colIndex = Get-ColumnIndex $cellRef
          $formula = $null
          $formulaNode = $null
          foreach ($childNode in $cell.ChildNodes) {
            if ($childNode.LocalName -eq "f") {
              $formulaNode = $childNode
              break
            }
          }
          if ($formulaNode) {
            $formulaText = [string]$formulaNode.InnerText
            $sharedId = [string]$formulaNode.GetAttribute("si")
            if ($formulaText) {
              $formula = "=$formulaText"
              if ($sharedId) {
                $sharedFormulas[$sharedId] = @{
                  Formula = $formulaText
                  Column = $colIndex
                  Row = $rowIndex
                }
              }
            }
            elseif ($sharedId -and $sharedFormulas.ContainsKey($sharedId)) {
              $base = $sharedFormulas[$sharedId]
              $formula = "=" + (Convert-SharedFormula $base.Formula ($colIndex - $base.Column) ($rowIndex - $base.Row))
            }
          }
          $cells[$colIndex] = @{
            Value = Convert-CellValue $cell $sharedStrings
            Formula = $formula
          }
        }
        $rows[$rowIndex] = $cells
      }
      $sheets[$sheetName] = $rows
    }
    return $sheets
  }
  finally {
    $zip.Dispose()
  }
}

function Get-CellValue($Sheets, [string]$SheetName, [int]$Row, [int]$Column) {
  if ($Sheets[$SheetName].ContainsKey($Row) -and $Sheets[$SheetName][$Row].ContainsKey($Column)) {
    return $Sheets[$SheetName][$Row][$Column].Value
  }
  return $null
}

function Get-CellFormula($Sheets, [string]$SheetName, [int]$Row, [int]$Column) {
  if ($Sheets[$SheetName].ContainsKey($Row) -and $Sheets[$SheetName][$Row].ContainsKey($Column)) {
    return $Sheets[$SheetName][$Row][$Column].Formula
  }
  return $null
}

function Get-MaxRow($Sheets, [string]$SheetName) {
  if ($Sheets[$SheetName].Count -eq 0) {
    return 0
  }
  return ($Sheets[$SheetName].Keys | Measure-Object -Maximum).Maximum
}

function Read-Json([string]$PathValue) {
  return (Get-Content -Raw -Encoding UTF8 $PathValue | ConvertFrom-Json)
}

function ConvertTo-JsonLiteral($Value) {
  if ($null -eq $Value) {
    return "null"
  }
  if ($Value -is [string]) {
    return '"' + ($Value -replace '\\', '\\' -replace '"', '\"' -replace "`r", '\r' -replace "`n", '\n' -replace "`t", '\t') + '"'
  }
  if ($Value -is [bool]) {
    if ($Value) {
      return "true"
    }
    return "false"
  }
  if ($Value -is [byte] -or $Value -is [int16] -or $Value -is [int32] -or $Value -is [int64] -or $Value -is [single] -or $Value -is [double] -or $Value -is [decimal]) {
    return [System.Convert]::ToString($Value, [System.Globalization.CultureInfo]::InvariantCulture)
  }
  return ConvertTo-JsonLiteral ([string]$Value)
}

function Get-JsonProperties($Value) {
  if ($Value -is [System.Collections.IDictionary]) {
    return @($Value.Keys | ForEach-Object {
      [pscustomobject]@{ Name = [string]$_; Value = $Value[$_] }
    })
  }
  return @($Value.PSObject.Properties | Where-Object { $_.MemberType -eq "NoteProperty" -or $_.MemberType -eq "Property" } | ForEach-Object {
    [pscustomobject]@{ Name = $_.Name; Value = $_.Value }
  })
}

function ConvertTo-PrettyJson($Value, [int]$Depth) {
  if ($null -eq $Value -or $Value -is [string] -or $Value -is [bool] -or $Value -is [ValueType]) {
    return ConvertTo-JsonLiteral $Value
  }
  if ($Value -is [System.Collections.IEnumerable] -and -not ($Value -is [string]) -and -not ($Value -is [System.Collections.IDictionary]) -and -not ($Value -is [pscustomobject])) {
    $items = @($Value)
    if ($items.Count -eq 0) {
      return "[]"
    }
    $currentIndent = " " * ($Depth * 2)
    $childIndent = " " * (($Depth + 1) * 2)
    $lines = @("[")
    for ($index = 0; $index -lt $items.Count; $index++) {
      $suffix = if ($index -lt $items.Count - 1) { "," } else { "" }
      $lines += $childIndent + (ConvertTo-PrettyJson $items[$index] ($Depth + 1)) + $suffix
    }
    $lines += "$currentIndent]"
    return ($lines -join "`n")
  }

  $properties = @(Get-JsonProperties $Value)
  if ($properties.Count -eq 0) {
    return "{}"
  }
  $objectIndent = " " * ($Depth * 2)
  $propertyIndent = " " * (($Depth + 1) * 2)
  $objectLines = @("{")
  for ($index = 0; $index -lt $properties.Count; $index++) {
    $property = $properties[$index]
    $suffix = if ($index -lt $properties.Count - 1) { "," } else { "" }
    $objectLines += $propertyIndent + (ConvertTo-JsonLiteral $property.Name) + ": " + (ConvertTo-PrettyJson $property.Value ($Depth + 1)) + $suffix
  }
  $objectLines += "$objectIndent}"
  return ($objectLines -join "`n")
}

function Write-Json([string]$PathValue, $Data) {
  $json = (ConvertTo-PrettyJson $Data 0) + "`n"
  $encoding = [System.Text.UTF8Encoding]::new($false)
  [System.IO.File]::WriteAllText((Resolve-FullPath $PathValue), $json, $encoding)
}

function Set-Property($Object, [string]$Name, $Value) {
  if ($Object.PSObject.Properties[$Name]) {
    $Object.$Name = $Value
  }
  else {
    $Object | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
  }
}

function Ensure-RequiredFiles([string]$ExcelPath, [string]$OutputDir) {
  if (-not (Test-Path $ExcelPath)) {
    throw "Workbook not found: $ExcelPath"
  }
  $lockPath = Join-Path (Split-Path -Parent $ExcelPath) ("~$" + (Split-Path -Leaf $ExcelPath))
  if (Test-Path $lockPath) {
    throw "Workbook appears to be open in Excel. Save and close it, then run this tool again."
  }
  if (-not (Test-Path $OutputDir)) {
    throw "Output directory not found: $OutputDir"
  }
  foreach ($name in @("item_config.json", "level_config.json", "global_config.json", "sheet1_config.json")) {
    $path = Join-Path $OutputDir $name
    if (-not (Test-Path $path)) {
      throw "Template JSON not found: $path"
    }
  }
}

function Export-Items($Sheets, [string]$OutputDir, [string]$SourceWorkbook, [string]$Now) {
  $data = Read-Json (Join-Path $OutputDir "item_config.json")
  $items = @()
  for ($row = 2; $row -le (Get-MaxRow $Sheets "item"); $row++) {
    $values = for ($col = 1; $col -le 11; $col++) { Get-CellValue $Sheets "item" $row $col }
    if (-not ($values | Where-Object { $null -ne $_ })) {
      continue
    }
    if ($null -eq $values[0]) {
      continue
    }
    $items += [pscustomobject][ordered]@{
      itemId = $values[0]
      itemName = $values[1]
      materialName = $values[2]
      prefab = $values[3]
      weightKg = $values[4]
      defense = $values[5]
      health = $values[6]
      sizeFactorReference = $values[7]
      guaranteedSpawnCount = $values[8]
      randomWeight = $values[9]
      unlockLevel = $values[10]
    }
  }
  Set-Property $data "sourceWorkbook" $SourceWorkbook
  Set-Property $data "generatedAt" $Now
  Set-Property $data "note" "Item source weight is stored in kilograms and used directly by gameplay and UI."
  Set-Property $data "sourceSheet" "item"
  Set-Property $data "items" $items
  Write-Json (Join-Path $OutputDir "item_config.json") $data
  return $items.Count
}

function Export-Levels($Sheets, [string]$OutputDir, [string]$SourceWorkbook, [string]$Now) {
  $data = Read-Json (Join-Path $OutputDir "level_config.json")
  $levels = @()
  for ($row = 2; $row -le (Get-MaxRow $Sheets "level"); $row++) {
    $values = for ($col = 1; $col -le 12; $col++) { Get-CellValue $Sheets "level" $row $col }
    if (-not ($values | Where-Object { $null -ne $_ })) {
      continue
    }
    if ($null -eq $values[0]) {
      continue
    }
    $formula = Get-CellFormula $Sheets "level" $row 4
    $levels += [pscustomobject][ordered]@{
      levelId = $values[0]
      shipWeightDisplay = $values[1]
      initialDistance = $values[2]
      shipSpeedDisplay = $values[3]
      basePullForce = $values[4]
      baseItemCount = $values[5]
      minRandomItemCount = $values[6]
      minTotalItemWeight = $values[7]
      recommendedWeightKg = $values[8]
      itemWeightCoefficient = $values[9]
      stormLevel = $values[10]
      buildTimeSeconds = $values[11]
      shipSpeedFormula = $formula
    }
  }
  Set-Property $data "sourceWorkbook" $SourceWorkbook
  Set-Property $data "generatedAt" $Now
  Set-Property $data "sourceSheet" "level"
  Set-Property $data "levels" $levels
  Write-Json (Join-Path $OutputDir "level_config.json") $data
  return $levels.Count
}

function Export-Entries($Sheets, [string]$OutputDir, [string]$JsonName, [string]$SheetName, [int]$FirstRow, [string]$SourceWorkbook, [string]$Now) {
  $data = Read-Json (Join-Path $OutputDir $JsonName)
  for ($index = 0; $index -lt $data.entries.Count; $index++) {
    $data.entries[$index].value = Get-CellValue $Sheets $SheetName ($FirstRow + $index) 2
  }
  $parameters = [ordered]@{}
  foreach ($entry in $data.entries) {
    if ($entry.key) {
      $parameters[$entry.key] = $entry.value
    }
  }
  Set-Property $data "sourceWorkbook" $SourceWorkbook
  Set-Property $data "generatedAt" $Now
  Set-Property $data "sourceSheet" $SheetName
  Set-Property $data "parameters" ([pscustomobject]$parameters)
  Write-Json (Join-Path $OutputDir $JsonName) $data
  return $data.entries.Count
}

try {
  $excelPath = Resolve-FullPath $Excel
  $outputDir = Resolve-FullPath $Output
  Ensure-RequiredFiles $excelPath $outputDir

  $sheets = Read-Workbook $excelPath
  $missing = @($RequiredSheets | Where-Object { -not $sheets.ContainsKey($_) })
  if ($missing.Count -gt 0) {
    throw "Workbook missing required sheet(s): $($missing -join ', ')"
  }

  $now = [System.DateTime]::UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
  $sourceWorkbook = Get-DisplayPath $excelPath

  $itemCount = Export-Items $sheets $outputDir $sourceWorkbook $now
  $levelCount = Export-Levels $sheets $outputDir $sourceWorkbook $now
  $globalCount = Export-Entries $sheets $outputDir "global_config.json" "global" 2 $sourceWorkbook $now
  $sheet1Count = Export-Entries $sheets $outputDir "sheet1_config.json" "Sheet1" 1 $sourceWorkbook $now

  Write-Host "[config] Source: $sourceWorkbook"
  Write-Host "[config] Output: $(Get-DisplayPath $outputDir)"
  Write-Host "[config] Items: $itemCount"
  Write-Host "[config] Levels: $levelCount"
  Write-Host "[config] Global entries: $globalCount"
  Write-Host "[config] Sheet1 entries: $sheet1Count"
  exit 0
}
catch {
  Write-Error "[config] Export failed: $($_.Exception.Message)"
  exit 1
}
