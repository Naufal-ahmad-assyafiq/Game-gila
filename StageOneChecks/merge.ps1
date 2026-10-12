$ErrorActionPreference = 'Stop'
$taskRoot = 'C:\Users\Naufal Ahmad A\game gila - test'
$mainPath = Join-Path $taskRoot 'Assets/FpsHorrorKit/Scenes/MainScene.unity'
$baseline = [IO.File]::ReadAllText((Join-Path $taskRoot 'StageOneChecks/MainScene.before.txt'))
if ([IO.File]::ReadAllText($mainPath) -cne $baseline) { throw 'MainScene changed since the baseline; cannot merge safely.' }
$fixturePath = Join-Path $taskRoot 'StageOneChecks/UnityHarness/Assets/FpsHorrorKit/Scenes/MainScene.unity'
$generated = [IO.File]::ReadAllText($fixturePath)
$blockPattern = '(?ms)^--- !u!(\d+) &(\d+)[^\r\n]*\r?\n.*?(?=^--- !u!|\z)'
$oldBlocks = @{}
foreach ($match in [regex]::Matches($baseline, $blockPattern)) { $oldBlocks[$match.Groups[2].Value] = $match.Value }
$fixture = @{}
foreach ($match in [regex]::Matches($generated, $blockPattern)) { $fixture[$match.Groups[2].Value] = $match.Value }
$stageGameObject = ($fixture.GetEnumerator() | Where-Object { $_.Value -match '(?m)^  m_Name: StageOne_LockedDoor\r?$' }).Key
if (!$stageGameObject) { throw 'Missing stage root.' }
$stageTransform = ($fixture.GetEnumerator() | Where-Object { $_.Value -match '^--- !u!4 ' -and $_.Value -match "m_GameObject: \{fileID: $stageGameObject\}" }).Key
$doorComponent = ($fixture.GetEnumerator() | Where-Object { $_.Value -match 'm_Script: .*b895553cef17b224a80e38b8eb04a373' }).Key
if (!$doorComponent -or !$stageTransform) { throw 'Missing fixture references.' }
$descendantGameObjects = @{}
$descendantTransforms = @{}
$descendantTransforms[$stageTransform] = $true
$descendantGameObjects[$stageGameObject] = $true
do {
    $changed = $false
    foreach ($entry in $fixture.GetEnumerator()) {
        if ($entry.Value -match '^--- !u!4 ' -and $entry.Value -match 'm_Father: \{fileID: (\d+)\}') {
            if ($descendantTransforms.ContainsKey($Matches[1]) -and !$descendantTransforms.ContainsKey($entry.Key)) {
                $descendantTransforms[$entry.Key] = $true
                if ($entry.Value -match 'm_GameObject: \{fileID: (\d+)\}') { $descendantGameObjects[$Matches[1]] = $true }
                $changed = $true
            }
        }
    }
} while ($changed)
$newBlocks = @{}
foreach ($entry in $fixture.GetEnumerator()) {
    if ($descendantGameObjects.ContainsKey($entry.Key)) { $newBlocks[$entry.Key] = $entry.Value }
    elseif ($entry.Value -match 'm_GameObject: \{fileID: (\d+)\}' -and $descendantGameObjects.ContainsKey($Matches[1])) { $newBlocks[$entry.Key] = $entry.Value }
}
$idMap = @{}
$counter = [long]9100000000000000
foreach ($id in ($newBlocks.Keys | Sort-Object)) {
    $counter++
    $newId = $counter.ToString()
    if ($oldBlocks.ContainsKey($newId)) { throw 'ID collision.' }
    $idMap[$id] = $newId
}
$strippedDoorId = '9100000000000000'
$idMap[$doorComponent] = $strippedDoorId
$newText = ''
foreach ($entry in ($newBlocks.GetEnumerator() | Sort-Object Key)) {
    $block = [regex]::Replace($entry.Value, '(?m)^(--- !u!\d+ &)(\d+)', { param($m) $m.Groups[1].Value + $idMap[$m.Groups[2].Value] })
    $block = [regex]::Replace($block, '\{fileID: (\d+)\}', { param($m) if ($idMap.ContainsKey($m.Groups[1].Value)) { '{fileID: ' + $idMap[$m.Groups[1].Value] + '}' } else { $m.Value } })
    $newText += $block
}
$eol = if ($baseline.Contains("`r`n")) { "`r`n" } else { "`n" }
$newText = $newText -replace '\r?\n', $eol
$stripped = @'
--- !u!114 &9100000000000000 stripped
MonoBehaviour:
  m_CorrespondingSourceObject: {fileID: 4584004032838334049, guid: aa570ccf2b4cb1e448feba4aecbf4d5d, type: 3}
  m_PrefabInstance: {fileID: 1149439012087369111}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 0}
  m_Enabled: 1
  m_EditorHideFlags: 0
  m_Script: {fileID: 11500000, guid: b895553cef17b224a80e38b8eb04a373, type: 3}
  m_Name: 
  m_EditorClassIdentifier: 
'@
$newText += ($stripped -replace '\r?\n', $eol) + $eol
$targetId = '1149439012087369111'
$doorBlock = $oldBlocks[$targetId]
$overrides = ''
$values = [ordered]@{ isLocked='1'; hasKey='0'; doorLockedText='Pintu terkunci - cari kunci'; useKeyText='Gunakan kunci [E]'; interactText='Buka/Tutup pintu [E]' }
foreach ($field in $values.Keys) {
    $overrides += '    - target: {fileID: 4584004032838334049, guid: aa570ccf2b4cb1e448feba4aecbf4d5d, type: 3}' + $eol
    $overrides += '      propertyPath: ' + $field + $eol
    $overrides += '      value: ' + $values[$field] + $eol
    $overrides += '      objectReference: {fileID: 0}' + $eol
}
$newDoorBlock = $doorBlock.Replace('    m_RemovedComponents: []', $overrides + '    m_RemovedComponents: []')
$rootsId = '9223372036854775807'
$newRoots = $oldBlocks[$rootsId].TrimEnd("`r", "`n") + $eol + '  - {fileID: ' + $idMap[$stageTransform] + '}' + $eol
$result = $baseline.Replace($doorBlock, $newDoorBlock).Replace($oldBlocks[$rootsId], $newText + $newRoots)
[IO.File]::WriteAllText($mainPath, $result, [Text.UTF8Encoding]::new($false))
$finalBlocks = @{}
foreach ($match in [regex]::Matches($result, $blockPattern)) { $finalBlocks[$match.Groups[2].Value] = $match.Value }
foreach ($entry in $oldBlocks.GetEnumerator()) {
    if ($entry.Key -ne $targetId -and $entry.Key -ne $rootsId -and $finalBlocks[$entry.Key] -cne $entry.Value) { throw "Unexpected change to existing block $($entry.Key)" }
}
Write-Output "Preserved $($oldBlocks.Count - 2) existing scene blocks byte-for-byte. Added $($newBlocks.Count + 1) blocks."
$sourceAssets = Join-Path $taskRoot 'StageOneChecks/UnityHarness/Assets/HorrorPrototype'
$targetAssets = Join-Path $taskRoot 'Assets/HorrorPrototype'
foreach ($name in @('Placeholder_Key_Yellow.mat','Placeholder_Ghost_White.mat','Placeholder_Ghost_Red.mat','Placeholder_Ghost_Dark.mat','Placeholder_Scare.wav')) {
    Copy-Item -LiteralPath (Join-Path $sourceAssets $name) -Destination (Join-Path $targetAssets $name)
    Copy-Item -LiteralPath (Join-Path $sourceAssets ($name+'.meta')) -Destination (Join-Path $targetAssets ($name+'.meta'))
}
foreach ($name in @('DoorJumpscare.cs.meta','GhostAppearance.cs.meta')) {
    Copy-Item -LiteralPath (Join-Path $sourceAssets ('Scripts/'+$name)) -Destination (Join-Path $targetAssets ('Scripts/'+$name))
}
$colors = @{
    'Placeholder_Key_Yellow'=@('1','0.75','0.02')
    'Placeholder_Ghost_White'=@('0.8','0.9','1')
    'Placeholder_Ghost_Red'=@('1','0.015','0.01')
    'Placeholder_Ghost_Dark'=@('0.005','0.005','0.005')
}
foreach ($name in $colors.Keys) {
    $path = Join-Path $targetAssets ($name+'.mat')
    $material = [IO.File]::ReadAllText($path)
    $material = $material -replace 'm_Shader: \{[^\r\n]+\}', 'm_Shader: {fileID: 4800000, guid: c4edd00ff2db5b24391a4fcb1762e459, type: 3}'
    $color = $colors[$name]
    $floats = "m_Floats:`n    - _EmissiveExposureWeight: 0`n    - _IncludeIndirectLighting: 0`n    - _SurfaceType: 0`n    - _ZWrite: 1"
    $material = $material.Replace('m_Floats: []', $floats)
    $material = $material.Replace('    - _Color: {r: 1, g: 1, b: 1, a: 1}',
        "    - _UnlitColor: {r: $($color[0]), g: $($color[1]), b: $($color[2]), a: 1}`n    - _EmissiveColor: {r: $($color[0]), g: $($color[1]), b: $($color[2]), a: 1}")
    [IO.File]::WriteAllText($path, $material, [Text.UTF8Encoding]::new($false))
}
Write-Output 'Merged scene and copied placeholder assets with HDRP/Unlit materials.'
