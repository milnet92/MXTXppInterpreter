# Compares the assemblies that the compiled MXppTools model and XppInterpreter.dll reference with the ones of another commit.
# An assembly that a build server doesn't have breaks the build of every client: see "Build dependencies" in Docs\Development.md.
#
#   .\CompareBuildReferences.ps1 -BaseRef upstream/main
#
# Compile the current model first (BuildInterpreter.ps1 -IncludeModel). The base is compiled into a temporary folder.
# Exits with 1 when the current code references an assembly that the base doesn't.
param
(
    [Parameter(Mandatory = $true)]
    [string]$BaseRef
)

$ErrorActionPreference = "Stop"

$RepoPath = Split-Path $PSScriptRoot
$WorkFolder = Join-Path $env:TEMP "MXppToolsReferences"

function Get-PackagesFolder
{
    foreach ($Folder in "K:\AosService", "C:\AosService", "E:\AosService")
    {
        if (Test-Path -Path $Folder)
        {
            return Join-Path $Folder "PackagesLocalDirectory"
        }
    }

    throw "Cannot find the AOSService folder in any known location"
}

function Get-Ildasm
{
    $Found = Get-ChildItem "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows" -Recurse -Filter ildasm.exe -ErrorAction SilentlyContinue |
        Where-Object { $_.DirectoryName -like "*x64" } |
        Sort-Object FullName |
        Select-Object -Last 1

    if (-not $Found)
    {
        throw "ildasm.exe not found: install the .NET Framework developer tools of the Windows SDK"
    }

    return $Found.FullName
}

# The simple names of the assemblies the files reference
function Get-References([string]$Ildasm, [string[]]$Files)
{
    $Names = New-Object System.Collections.Generic.SortedSet[string]

    foreach ($File in $Files)
    {
        foreach ($Line in & $Ildasm /text /nobar $File)
        {
            if ($Line -match '^\.assembly extern\s+(?:/\*[^*]*\*/\s*)?(?:retargetable\s+)?(?:windowsruntime\s+)?(\S+)')
            {
                [void]$Names.Add($Matches[1])
            }
        }
    }

    return ,@($Names)
}

function Get-ModelFiles([string]$Folder)
{
    return @(Get-ChildItem $Folder -File | Where-Object { $_.Name -like "Dynamics.AX.MXppTools*" -and $_.Extension -in ".netmodule", ".dll" } | ForEach-Object FullName)
}

function Compare-Lists([string]$Title, [string[]]$Base, [string[]]$Current)
{
    $Added = @($Current | Where-Object { $_ -notin $Base })
    $Removed = @($Base | Where-Object { $_ -notin $Current })

    Write-Host ""
    Write-Host "$Title`: $($Base.Count) in $BaseRef, $($Current.Count) now"
    $Added | ForEach-Object { Write-Host "  added:   $_" }
    $Removed | ForEach-Object { Write-Host "  removed: $_" }

    if ($Added.Count -eq 0 -and $Removed.Count -eq 0)
    {
        Write-Host "  the same"
    }

    return $Added.Count
}

$Packages = Get-PackagesFolder
$Ildasm = Get-Ildasm

if (Test-Path $WorkFolder)
{
    Remove-Item $WorkFolder -Recurse -Force
}

New-Item -ItemType Directory -Force -Path (Join-Path $WorkFolder "out") | Out-Null

# A pipe between two programs would go through PowerShell as text, so the archive goes to a file
$Archive = Join-Path $WorkFolder "base.tar"
git -C $RepoPath archive --output="$Archive" $BaseRef Metadata/MXppTools
if ($LASTEXITCODE -ne 0) { throw "git archive failed for $BaseRef" }

tar -xf $Archive -C $WorkFolder
if ($LASTEXITCODE -ne 0) { throw "tar failed" }

$BaseMetadata = Join-Path $WorkFolder "Metadata"
$BaseOutput = Join-Path $WorkFolder "out"

Write-Host "Compiling MXppTools of $BaseRef"
& "$Packages\bin\xppc.exe" -metadata="$BaseMetadata" -compilermetadata="$Packages" -modelmodule="MXppTools" -output="$BaseOutput" -referenceFolder="$Packages" -refPath="$BaseMetadata\MXppTools\bin" -refPath="$Packages\bin" -log="$WorkFolder\xppc.log" -xmlLog="$WorkFolder\xppc.xml" | Out-Null
Get-Content "$WorkFolder\xppc.log" | Select-String -Pattern "^\s*(Errors|Warnings):" | ForEach-Object { Write-Host $_.Line.Trim() }

$BaseModel = Get-References $Ildasm (Get-ModelFiles $BaseOutput)
$CurrentModel = Get-References $Ildasm (Get-ModelFiles (Join-Path $Packages "MXppTools\bin"))
$BaseDll = Get-References $Ildasm @(Join-Path $BaseMetadata "MXppTools\bin\XppInterpreter.dll")
$CurrentDll = Get-References $Ildasm @(Join-Path $RepoPath "Metadata\MXppTools\bin\XppInterpreter.dll")

$Added = (Compare-Lists "Compiled model" $BaseModel $CurrentModel) + (Compare-Lists "XppInterpreter.dll" $BaseDll $CurrentDll)

exit ([int]($Added -gt 0))
