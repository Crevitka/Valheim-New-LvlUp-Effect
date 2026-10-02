param(
    [string]$ValheimDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim',
    [string]$BepInExDir = ''
)
$ErrorActionPreference = 'Stop'
if (!$BepInExDir) { $BepInExDir = Join-Path $ValheimDir 'BepInEx' }
$managed = Join-Path $ValheimDir 'valheim_Data\Managed'
$core = Join-Path $BepInExDir 'core'
$outDir = Join-Path $PSScriptRoot 'bin\Release'
New-Item -ItemType Directory -Force $outDir | Out-Null
$sdk = (& dotnet --list-sdks | Select-Object -Last 1)
if ($sdk -notmatch '^([^ ]+) \[(.+)\]') { throw 'A .NET SDK is required.' }
$compiler = Join-Path $Matches[2] "$($Matches[1])\Roslyn\bincore\csc.dll"
$arguments = @('-noconfig', '-nostdlib+', '-target:library', '-langversion:latest', '-optimize+', "-out:$outDir\NewLvlUpEffect.dll")
foreach ($assembly in @('mscorlib','System','System.Core','netstandard','assembly_valheim','assembly_guiutils','Assembly-CSharp','UnityEngine','UnityEngine.CoreModule','UnityEngine.TextRenderingModule','UnityEngine.UI','UnityEngine.UIModule','Unity.TextMeshPro')) {
    $arguments += "-r:$managed\$assembly.dll"
}
$arguments += "-r:$core\BepInEx.dll", "-r:$core\0Harmony.dll", (Join-Path $PSScriptRoot 'Plugin.cs')
& dotnet $compiler @arguments
if ($LASTEXITCODE -ne 0) { throw 'NewLvlUpEffect compilation failed' }
Write-Output "Built $outDir\NewLvlUpEffect.dll"
