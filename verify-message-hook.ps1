param([string]$ValheimDir = 'C:\Program Files (x86)\Steam\steamapps\common\Valheim')
$ErrorActionPreference = 'Stop'
Add-Type -Path (Join-Path $ValheimDir 'BepInEx/core/Mono.Cecil.dll')
$game = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $ValheimDir 'valheim_Data/Managed/assembly_valheim.dll'))
$mod = [Mono.Cecil.AssemblyDefinition]::ReadAssembly((Join-Path $PSScriptRoot 'bin/Release/NewLvlUpEffect.dll'))
try {
    $player = $game.MainModule.Types | Where-Object Name -eq 'Player'
    $method = @($player.Methods | Where-Object Name -eq 'Message')
    if ($method.Count -ne 1 -or $method[0].Parameters[1].ParameterType.FullName -ne 'System.String') { throw 'Player.Message signature changed' }
    if (!($method[0].Body.Instructions | Where-Object { $_.Operand -is [Mono.Cecil.MethodReference] -and $_.Operand.DeclaringType.Name -eq 'MessageHud' -and $_.Operand.Name -eq 'ShowMessage' })) { throw 'Player.Message no longer forwards to MessageHud.ShowMessage' }
    $plugin = $mod.MainModule.Types | Where-Object FullName -eq 'NewLvlUpEffect.Plugin'
    $patch = $plugin.NestedTypes | Where-Object Name -eq 'SkillMessagePatch'
    $attribute = $patch.CustomAttributes | Where-Object { $_.AttributeType.FullName -eq 'HarmonyLib.HarmonyPatch' }
    if ($attribute.ConstructorArguments[0].Value.FullName -ne 'Player' -or $attribute.ConstructorArguments[1].Value -ne 'Message') { throw 'Patch must target the concrete Player override, not Character.Message' }
    $prefix = $patch.Methods | Where-Object Name -eq 'Prefix'
    if ($prefix.Parameters[0].ParameterType.FullName -ne 'Player') { throw 'Incorrect __instance type' }
    Write-Output 'PASS: compiled Harmony patch targets Player.Message, whose game implementation calls MessageHud.ShowMessage.'
} finally { $mod.Dispose(); $game.Dispose() }
