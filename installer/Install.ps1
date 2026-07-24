# One-click install for Air Mouse Remote (EASYTONE / G10 on Windows)
$ErrorActionPreference = 'Stop'
$root = Split-Path (Split-Path -Parent $MyInvocation.MyCommand.Path)
$appDir = Join-Path $root 'app'
$installDir = Join-Path $env:LOCALAPPDATA 'AirMouseRemote'
$exeName = 'AirMouseRemote.exe'
$srcExe = Join-Path $appDir $exeName

Write-Host '========================================'
Write-Host ' Air Mouse Remote - Install'
Write-Host ' For EASYTONE / G10-class air mice'
Write-Host '========================================'
Write-Host ''

if (-not (Test-Path $srcExe)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
    $cs = Join-Path $appDir 'AirMouseRemote.cs'
    if (-not (Test-Path $cs)) { throw "Missing $cs" }
    Write-Host 'Building...'
    & $csc /nologo /target:winexe /r:System.dll /r:System.Windows.Forms.dll /r:System.Drawing.dll /out:$srcExe $cs
    if (-not (Test-Path $srcExe)) { throw 'Build failed' }
}

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item $srcExe (Join-Path $installDir $exeName) -Force
Copy-Item (Join-Path $appDir 'Set-BestMic.ps1') (Join-Path $installDir 'Set-BestMic.ps1') -Force -ErrorAction SilentlyContinue
Copy-Item (Join-Path $root 'docs\DEVICE-GUIDE.md') (Join-Path $installDir 'DEVICE-GUIDE.md') -Force -ErrorAction SilentlyContinue

foreach ($p in @('VoiceBridge', 'G10MicHid', 'AirMouseRemote')) {
    Get-Process -Name $p -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
}

$startup = [Environment]::GetFolderPath('Startup')
Get-ChildItem $startup -ErrorAction SilentlyContinue | Where-Object { $_.Name -match 'VoiceBridge|G10 MIC|Air Mouse' } |
    Remove-Item -Force -ErrorAction SilentlyContinue

$wsh = New-Object -ComObject WScript.Shell
$exeFull = Join-Path $installDir $exeName

$sc = $wsh.CreateShortcut((Join-Path $startup 'Air Mouse Remote.lnk'))
$sc.TargetPath = $exeFull
$sc.WorkingDirectory = $installDir
$sc.Description = 'Air mouse MIC button to Windows Voice Typing Win+H'
$sc.Save()

$desk = [Environment]::GetFolderPath('Desktop')
$tools = Join-Path $desk 'Audio & Voice Tools'
New-Item -ItemType Directory -Force -Path $tools | Out-Null

$sc2 = $wsh.CreateShortcut((Join-Path $tools 'Air Mouse Remote.lnk'))
$sc2.TargetPath = $exeFull
$sc2.WorkingDirectory = $installDir
$sc2.Save()

$guide = Join-Path $installDir 'DEVICE-GUIDE.md'
if (Test-Path $guide) {
    $sc3 = $wsh.CreateShortcut((Join-Path $tools 'Air Mouse Device Guide.lnk'))
    $sc3.TargetPath = $guide
    $sc3.Save()
}

$kaPath = Join-Path $installDir 'KeepAlive.ps1'
@(
    "`$ErrorActionPreference='SilentlyContinue'"
    "`$exe = Join-Path `$env:LOCALAPPDATA 'AirMouseRemote\AirMouseRemote.exe'"
    "while (`$true) {"
    "  if (-not (Get-Process -Name AirMouseRemote -ErrorAction SilentlyContinue)) {"
    "    if (Test-Path `$exe) { Start-Process `$exe }"
    "  }"
    "  Start-Sleep 5"
    "}"
) | Set-Content $kaPath -Encoding UTF8

$vbsPath = Join-Path $installDir 'Start-KeepAlive.vbs'
$vbs = "Set sh = CreateObject(`"WScript.Shell`")`r`nsh.Run `"powershell.exe -NoProfile -WindowStyle Hidden -ExecutionPolicy Bypass -File `"`"$kaPath`"`"`", 0, False`r`n"
Set-Content $vbsPath $vbs -Encoding ASCII

$scKa = $wsh.CreateShortcut((Join-Path $startup 'Air Mouse Remote KeepAlive.lnk'))
$scKa.TargetPath = 'wscript.exe'
$scKa.Arguments = '"' + $vbsPath + '"'
$scKa.Save()

Write-Host "Installed to: $installDir"
Write-Host 'Startup: enabled'
Write-Host 'Shortcut: Desktop / Audio and Voice Tools / Air Mouse Remote'
Write-Host ''
Write-Host 'Launching...'
Start-Process $exeFull -WorkingDirectory $installDir
Start-Process wscript.exe -ArgumentList ('"' + $vbsPath + '"')
Write-Host 'Done.'
