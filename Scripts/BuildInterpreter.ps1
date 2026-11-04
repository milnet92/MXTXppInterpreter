# Builds the C# interpreter (Projects\XppInterpreter) and copies it into the MXppTools package.
# PackagesLocalDirectory\MXppTools must be a junction to Metadata\MXppTools, so the AOS loads the new DLL after the restart.
# Execute as Administrator. Debug is the configuration of the committed DLL.
# If you change classes or members that X++ calls, also build the MXppTools model.
param
(
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"

$RepoPath = Split-Path $PSScriptRoot
$ProjectPath = Join-Path $RepoPath "Projects\XppInterpreter"
$PackageBinPath = Join-Path $RepoPath "Metadata\MXppTools\bin"

$VsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$MSBuild = & $VsWhere -latest -products * -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1

Write-Host "Building XppInterpreter ($Configuration) with $MSBuild"
& $MSBuild (Join-Path $ProjectPath "XppInterpreter.csproj") /p:Configuration=$Configuration /v:minimal /nologo
if ($LASTEXITCODE -ne 0)
{
    throw "Build failed"
}

# The AOS keeps the DLL locked while it is running
Write-Host "Stopping IIS"
iisreset /stop
try
{
    Copy-Item (Join-Path $ProjectPath "bin\$Configuration\XppInterpreter.*") $PackageBinPath -Force
}
finally
{
    Write-Host "Starting IIS"
    iisreset /start
}
