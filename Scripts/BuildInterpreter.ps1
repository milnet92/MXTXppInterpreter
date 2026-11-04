# Builds the C# interpreter (Projects\XppInterpreter) and copies it into the MXppTools package.
# PackagesLocalDirectory\MXppTools must be a junction to Metadata\MXppTools, so the AOS loads the new DLL after the restart.
# Execute as Administrator. Debug is the configuration of the committed DLL.
#
# -IncludeModel also compiles the X++ model, its labels and copies the web resources.
# Use it when X++ code changed, or the C# classes X++ calls did. Without it, IIS only needs the new DLL.
param
(
    [string]$Configuration = "Debug",
    [switch]$IncludeModel
)

$ErrorActionPreference = "Stop"

$RepoPath = Split-Path $PSScriptRoot
$ProjectPath = Join-Path $RepoPath "Projects\XppInterpreter"
$PackageBinPath = Join-Path $RepoPath "Metadata\MXppTools\bin"

function Get-LocalDeploymentFolder
{
    foreach ($Folder in "K:\AosService", "C:\AosService", "E:\AosService")
    {
        if (Test-Path -Path $Folder)
        {
            return $Folder
        }
    }

    throw "Cannot find the AOSService folder in any known location"
}

function Build-Model
{
    $DeploymentFolder = Get-LocalDeploymentFolder
    $Packages = Join-Path $DeploymentFolder "PackagesLocalDirectory"
    $Package = Join-Path $Packages "MXppTools"
    $LogFolder = Join-Path $env:TEMP "MXppToolsBuild"

    New-Item -ItemType Directory -Force -Path $LogFolder | Out-Null

    Write-Host "Compiling the MXppTools model"
    & "$Packages\bin\xppc.exe" -metadata="$Packages" -compilermetadata="$Packages" -modelmodule="MXppTools" -output="$Package\bin" -referenceFolder="$Packages" -refPath="$Package\bin" -refPath="$Packages\bin" -log="$LogFolder\xppc.log" -xmlLog="$LogFolder\xppc.xml" | Out-Null

    Get-Content "$LogFolder\xppc.log" | Select-String -Pattern "Error:|^\s*Errors:|^\s*Warnings:" | ForEach-Object { Write-Host $_.Line }

    if ($LASTEXITCODE -ne 0)
    {
        throw "The model has compilation errors. See $LogFolder\xppc.log"
    }

    Write-Host "Compiling the labels"
    & "$Packages\bin\LabelC.exe" -metadata="$Packages" -output="$Package\Resources" -modelmodule="MXppTools" -outlog="$LogFolder\labelc.log" -errlog="$LogFolder\labelc.err" | Out-Null

    Write-Host "Copying the web resources"
    foreach ($Folder in "Html", "Scripts", "Images")
    {
        Copy-Item (Join-Path $RepoPath "Metadata\MXppTools\MXppTools\AxResource\ResourceContent\$Folder\*") (Join-Path $DeploymentFolder "webroot\Resources\$Folder") -Force
    }
}

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

    if ($IncludeModel)
    {
        Build-Model
    }
}
finally
{
    Write-Host "Starting IIS"
    iisreset /start
}
