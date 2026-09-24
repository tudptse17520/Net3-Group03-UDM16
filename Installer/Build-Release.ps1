param([string]$InnoCompiler = '')
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$solutionRoot = Join-Path $repoRoot 'Code\UDM_16_CaroGame'
$publishRoot = Join-Path $repoRoot 'artifacts\publish\Caro'
dotnet publish (Join-Path $solutionRoot 'CaroClient\CaroClient.csproj') -p:PublishProfile=win-x64 -o $publishRoot
if ($LASTEXITCODE -ne 0) { throw 'Caro publish failed.' }
dotnet publish (Join-Path $solutionRoot 'CaroServer\CaroServer.csproj') -p:PublishProfile=win-x64 -o (Join-Path $publishRoot 'Server')
if ($LASTEXITCODE -ne 0) { throw 'Server publish failed.' }
if (!$InnoCompiler) {
    $candidates = @("${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe", "$env:ProgramFiles\Inno Setup 7\ISCC.exe", (Join-Path $repoRoot 'artifacts\inno\ISCC.exe'))
    $InnoCompiler = $candidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
}
if ($InnoCompiler) {
    & $InnoCompiler (Join-Path $PSScriptRoot 'CaroInstaller.iss')
    if ($LASTEXITCODE -ne 0) { throw 'Installer compilation failed.' }
} else { Write-Warning 'Publish completed. Install Inno Setup and run this script again to compile CaroSetup.exe.' }
