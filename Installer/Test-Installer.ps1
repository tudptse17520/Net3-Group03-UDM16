$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$registration = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{F5839222-74BC-465D-986C-8962797E4FF1}_is1'
if (Test-Path -LiteralPath $registration) { throw 'Caro is already installed. Skip this isolated install/uninstall test to preserve it.' }
$installRoot = Join-Path $repoRoot ('artifacts\install-smoke-' + (Get-Date -Format yyyyMMdd-HHmmss))
$setup = Join-Path $repoRoot 'artifacts\installer\CaroSetup.exe'
$installLog = Join-Path $repoRoot 'artifacts\install.log'
$processes = @()
$desktopShortcut = Join-Path ([Environment]::GetFolderPath('Desktop')) 'Caro.lnk'
$desktopBackup = $null
$shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Caro\Caro.lnk'
$menuBackup = $null
if (Test-Path -LiteralPath $shortcutPath) {
    $menuBackup = Join-Path $repoRoot ('artifacts\menu-shortcut-backup-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.lnk')
    Copy-Item -LiteralPath $shortcutPath -Destination $menuBackup
}
if (Test-Path -LiteralPath $desktopShortcut) {
    $desktopBackup = Join-Path $repoRoot ('artifacts\desktop-shortcut-backup-' + (Get-Date -Format yyyyMMdd-HHmmss) + '.lnk')
    Copy-Item -LiteralPath $desktopShortcut -Destination $desktopBackup
}
try {
    $setupArguments = @('/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/CURRENTUSER','/GROUP="Caro QA"',('/DIR="' + $installRoot + '"'),('/LOG="' + $installLog + '"'))
    $setupProcess = Start-Process -FilePath $setup -ArgumentList $setupArguments -WindowStyle Hidden -Wait -PassThru
    if ($setupProcess.ExitCode -ne 0) { throw 'Installer returned an error.' }
    $exe = Join-Path $installRoot 'Caro.exe'
    if (!(Test-Path -LiteralPath $exe)) { throw 'Installed Caro.exe missing.' }
    $shortcutPath = Join-Path ([Environment]::GetFolderPath('Programs')) 'Caro\Caro.lnk'
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    if ($shortcut.TargetPath -ne $exe -or $shortcut.IconLocation -notlike ($exe + '*')) { throw 'Shortcut target/icon mismatch.' }
    if ((Get-ItemProperty -LiteralPath $registration).DisplayName -ne 'Caro') { throw 'Uninstall branding mismatch.' }
    if (!(Test-Path -LiteralPath $desktopShortcut)) { throw 'Default desktop shortcut missing.' }
    if ($shell.CreateShortcut($desktopShortcut).TargetPath -ne $exe) { throw 'Desktop shortcut target mismatch.' }
    Write-Output 'PASS: installation, default Desktop shortcut, Start Menu shortcut/icon, uninstall registration.'
    dotnet run --project (Join-Path $repoRoot 'Code/UDM_16_CaroGame/Test/ReleaseSmokeTests') -p:NuGetAudit=false -- $installRoot --installed --startup-only
    if ($LASTEXITCODE -ne 0) { throw 'Installed application UI/game/network tests failed.' }
    $serverPath = Join-Path $installRoot 'Server\CaroServer.exe'
} finally {
    try {
    foreach ($p in $processes) { if (!$p.HasExited) { $p.Kill(); $p.WaitForExit() } }
    if (Test-Path -LiteralPath (Join-Path $installRoot 'unins000.exe')) {
        # This is only the isolated test installation created above.
        $uninstall = Start-Process -FilePath (Join-Path $installRoot 'unins000.exe') -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART' -WindowStyle Hidden -Wait -PassThru
        if ($uninstall.ExitCode -ne 0) { throw 'Uninstaller returned an error.' }
    }
    } finally {
        if ($desktopBackup) { Copy-Item -LiteralPath $desktopBackup -Destination $desktopShortcut -Force }
        if ($menuBackup) {
            New-Item -ItemType Directory -Path (Split-Path -Parent $shortcutPath) -Force | Out-Null
            Copy-Item -LiteralPath $menuBackup -Destination $shortcutPath -Force
        }
    }
}
if (Test-Path -LiteralPath (Join-Path $installRoot 'Caro.exe')) { throw 'Uninstall left Caro.exe.' }
if (Test-Path -LiteralPath $registration) { throw 'Uninstall left application registration.' }
if (!$desktopBackup -and (Test-Path -LiteralPath $desktopShortcut)) { throw 'Uninstall left desktop shortcut.' }
if ($desktopBackup -and (Get-FileHash -LiteralPath $desktopBackup).Hash -ne (Get-FileHash -LiteralPath $desktopShortcut).Hash) { throw 'Original desktop shortcut not restored.' }
if (!$menuBackup -and (Test-Path -LiteralPath $shortcutPath)) { throw 'Uninstall left Start Menu shortcut.' }
if ($menuBackup -and (Get-FileHash -LiteralPath $menuBackup).Hash -ne (Get-FileHash -LiteralPath $shortcutPath).Hash) { throw 'Original Start Menu shortcut not restored.' }
if (@(Get-Process CaroServer -ErrorAction SilentlyContinue | Where-Object { $_.Path -eq $serverPath }).Count -ne 0) { throw 'Uninstall left its local server running.' }
Write-Output 'PASS: uninstall removed binaries/shortcut/registration and stopped its local server; user settings preserved.'


