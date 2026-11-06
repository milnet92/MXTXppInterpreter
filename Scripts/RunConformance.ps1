# Runs Projects\XppInterpreter.Tests\Conformance\conformance.txt on this environment: compiled by the real X++ compiler,
# and through the interpreter. See Docs\Development.md. Execute as Administrator: it restarts IIS.
#
#   .\RunConformance.ps1 -Mode Compiled -Model MyModel -AddModuleReferences ContactPerson
#       open /?mi=SysClassRunner&cls=DEVXppProbe
#   .\RunConformance.ps1 -Mode Update              keeps what the compiler gave as the expected values
#   .\RunConformance.ps1 -Mode Interpreted -Model MyModel
#       open /?mi=SysClassRunner&cls=DEVXppConformance
#   .\RunConformance.ps1 -Mode Compare             shows where the interpreter differs
#   .\RunConformance.ps1 -Mode Cleanup -Model MyModel
#
# The generated classes go to the model you name, which is changed only until Cleanup. Use a model of your own that references
# ApplicationSuite: the D365 cases use its tables. -AddModuleReferences adds what else they need, and Cleanup takes it back.
param
(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Compiled', 'Update', 'Interpreted', 'Compare', 'Cleanup')]
    [string]$Mode,

    [string]$Model,

    [string[]]$AddModuleReferences = @(),

    [switch]$NoRestart,

    # Update and Compare wait for the class to finish. A run that stops writing for this long has ended the AOS.
    [int]$StallSeconds = 90
)

$ErrorActionPreference = "Stop"

$RepoPath = Split-Path $PSScriptRoot
$Tool = Join-Path $RepoPath "Projects\XppInterpreter.Tests\Conformance\conformance-tool.js"
$WorkFolder = Join-Path $env:TEMP "MXppToolsConformance"

# The tool writes these: keep them in line with conformance-tool.js
$Classes = @{ Compiled = "DEVXppProbe"; Interpreted = "DEVXppConformance" }
$Results = @{ Update = "C:\Windows\Temp\DEVXppProbe.txt"; Compare = "C:\Windows\Temp\DEVXppConformance.txt" }

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

function Get-Descriptor([string]$Packages)
{
    $Descriptor = Join-Path $Packages "$Model\Descriptor\$Model.xml"

    if (-not (Test-Path $Descriptor))
    {
        throw "Model $Model not found: $Descriptor"
    }

    return $Descriptor
}

function Add-ModuleReferences([string]$Packages)
{
    if ($AddModuleReferences.Count -eq 0)
    {
        return
    }

    $Descriptor = Get-Descriptor $Packages
    $Backup = Join-Path $WorkFolder "$Model.descriptor.xml"

    # The first run keeps the original, so a second run doesn't take its own change as the original
    if (-not (Test-Path $Backup))
    {
        Copy-Item $Descriptor $Backup
    }

    $Text = [IO.File]::ReadAllText($Descriptor)

    foreach ($Reference in $AddModuleReferences)
    {
        if ($Text -notmatch "<d2p1:string>$Reference</d2p1:string>")
        {
            $Text = $Text.Replace("</ModuleReferences>", "`t<d2p1:string>$Reference</d2p1:string>`r`n`t</ModuleReferences>")
            Write-Host "Added the reference to $Reference until Cleanup"
        }
    }

    [IO.File]::WriteAllText($Descriptor, $Text)
}

function Restore-Descriptor([string]$Packages)
{
    $Backup = Join-Path $WorkFolder "$Model.descriptor.xml"

    if (Test-Path $Backup)
    {
        Copy-Item $Backup (Get-Descriptor $Packages) -Force
        Remove-Item $Backup
        Write-Host "Restored the descriptor of $Model"
    }
}

# Returns the number of errors. A crash of the compiler has no diagnostic, only a line in the log.
function Invoke-ModelCompile([string]$Packages)
{
    $Log = Join-Path $WorkFolder "xppc.log"
    $XmlLog = Join-Path $WorkFolder "xppc.xml"

    Write-Host "Compiling $Model"
    & "$Packages\bin\xppc.exe" -metadata="$Packages" -compilermetadata="$Packages" -modelmodule="$Model" -output="$Packages\$Model\bin" -referenceFolder="$Packages" -refPath="$Packages\bin" -log="$Log" -xmlLog="$XmlLog" | Out-Null

    Get-Content $Log | Select-String -Pattern "^\s*(Errors|Warnings):" | ForEach-Object { Write-Host $_.Line.Trim() }
    Get-Content $Log | Select-String -Pattern "Fatal Error" | ForEach-Object { Write-Host $_.Line.Substring(0, [Math]::Min(300, $_.Line.Length)) }

    [xml]$Xml = Get-Content $XmlLog
    $Errors = @($Xml.SelectNodes("//Diagnostic") | Where-Object { $_.Severity -eq "Error" })

    foreach ($Diagnostic in $Errors)
    {
        Write-Host ("{0,-44} line {1}: {2}" -f ($Diagnostic.Path -split "/")[-1], $Diagnostic.Line, $Diagnostic.Message)
    }

    if ($LASTEXITCODE -ne 0 -and $Errors.Count -eq 0)
    {
        return 1
    }

    return $Errors.Count
}

function Restart-Aos
{
    if ($NoRestart)
    {
        Write-Host "Restart IIS before running the class"
        return
    }

    iisreset /restart | Select-Object -Last 1 | ForEach-Object { Write-Host $_ }
}

function Wait-Results([string]$File)
{
    Write-Host "Waiting for $File"

    $LastLength = -1
    $Stalled = 0

    while ($true)
    {
        if (Test-Path $File)
        {
            $Lines = @(Get-Content $File)

            if ($Lines.Count -gt 0 -and $Lines[-1] -eq "FINISHED")
            {
                return
            }

            $Length = (Get-Item $File).Length

            if ($Length -eq $LastLength)
            {
                $Stalled += 5
            }
            else
            {
                $Stalled = 0
                $LastLength = $Length
            }

            if ($Stalled -ge $StallSeconds)
            {
                $Last = if ($Lines.Count -gt 0) { $Lines[-1] } else { "" }
                throw "The run stopped after: $Last`nIf the AOS restarted, that case ends the process: mark it with '!! CRASHES' in conformance.txt and run again."
            }
        }

        Start-Sleep -Seconds 5
    }
}

New-Item -ItemType Directory -Force -Path $WorkFolder | Out-Null

switch ($Mode)
{
    { $_ -in "Compiled", "Interpreted" }
    {
        if (-not $Model) { throw "-Model is required" }

        $Packages = Get-PackagesFolder
        $AxClass = Join-Path $Packages "$Model\$Model\AxClass"

        Add-ModuleReferences $Packages

        foreach ($File in $Results.Values) { Remove-Item $File -ErrorAction SilentlyContinue }
        foreach ($Class in $Classes.Values) { Remove-Item (Join-Path $AxClass "$Class.xml") -ErrorAction SilentlyContinue }

        node $Tool $Mode.ToLowerInvariant() $AxClass

        if ((Invoke-ModelCompile $Packages) -gt 0)
        {
            throw "The model doesn't compile. A snippet the compiler refuses gets '!! REJECTED: <message>' in conformance.txt."
        }

        Restart-Aos
        Write-Host "Open /?mi=SysClassRunner&cls=$($Classes[$Mode]) and wait for '$($Classes[$Mode]) finished', or run the next step: it waits."
    }

    { $_ -in "Update", "Compare" }
    {
        Wait-Results $Results[$Mode]
        node $Tool $Mode.ToLowerInvariant()
    }

    "Cleanup"
    {
        if (-not $Model) { throw "-Model is required" }

        $Packages = Get-PackagesFolder
        $AxClass = Join-Path $Packages "$Model\$Model\AxClass"

        foreach ($Class in $Classes.Values) { Remove-Item (Join-Path $AxClass "$Class.xml") -ErrorAction SilentlyContinue }

        Restore-Descriptor $Packages

        if ((Invoke-ModelCompile $Packages) -gt 0)
        {
            throw "$Model doesn't compile after the cleanup"
        }

        Restart-Aos

        foreach ($File in $Results.Values) { Remove-Item $File -ErrorAction SilentlyContinue }
    }
}
