# Shared by RunConformance.ps1 and RunBenchmark.ps1. Both write temporary classes into a model of yours, compile it,
# restart the AOS and read the file the class writes when it is opened with SysClassRunner. Dot-source this file.

$ModelRunWorkFolder = Join-Path $env:TEMP "MXppToolsModelRun"

# Every class these scripts write: a cleanup removes them all, so the model compiles again without the added references
$ModelRunClasses = "DEVXppProbe", "DEVXppConformance", "DEVXppBenchmark", "DEVXppBenchmarkHelper"

New-Item -ItemType Directory -Force -Path $ModelRunWorkFolder | Out-Null

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

function Get-AxClassFolder([string]$Packages, [string]$Model)
{
    if (-not $Model) { throw "-Model is required" }

    return Join-Path $Packages "$Model\$Model\AxClass"
}

function Get-Descriptor([string]$Packages, [string]$Model)
{
    $Descriptor = Join-Path $Packages "$Model\Descriptor\$Model.xml"

    if (-not (Test-Path $Descriptor))
    {
        throw "Model $Model not found: $Descriptor"
    }

    return $Descriptor
}

function Add-ModuleReferences([string]$Packages, [string]$Model, [string[]]$References)
{
    if ($References.Count -eq 0)
    {
        return
    }

    $Descriptor = Get-Descriptor $Packages $Model
    $Backup = Join-Path $ModelRunWorkFolder "$Model.descriptor.xml"

    # The first run keeps the original, so a second run doesn't take its own change as the original
    if (-not (Test-Path $Backup))
    {
        Copy-Item $Descriptor $Backup
    }

    $Text = [IO.File]::ReadAllText($Descriptor)

    foreach ($Reference in $References)
    {
        if ($Text -notmatch "<d2p1:string>$Reference</d2p1:string>")
        {
            $Text = $Text.Replace("</ModuleReferences>", "`t<d2p1:string>$Reference</d2p1:string>`r`n`t</ModuleReferences>")
            Write-Host "Added the reference to $Reference until Cleanup"
        }
    }

    [IO.File]::WriteAllText($Descriptor, $Text)
}

function Restore-Descriptor([string]$Packages, [string]$Model)
{
    $Backup = Join-Path $ModelRunWorkFolder "$Model.descriptor.xml"

    if (Test-Path $Backup)
    {
        Copy-Item $Backup (Get-Descriptor $Packages $Model) -Force
        Remove-Item $Backup
        Write-Host "Restored the descriptor of $Model"
    }
}

function Remove-ModelRunClasses([string]$AxClass, [string[]]$Names = $ModelRunClasses)
{
    foreach ($Name in $Names)
    {
        Remove-Item (Join-Path $AxClass "$Name.xml") -ErrorAction SilentlyContinue
    }
}

# Returns the number of errors. A crash of the compiler has no diagnostic, only a line in the log.
function Invoke-ModelCompile([string]$Packages, [string]$Model)
{
    $Log = Join-Path $ModelRunWorkFolder "xppc.log"
    $XmlLog = Join-Path $ModelRunWorkFolder "xppc.xml"

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

function Restart-Aos([bool]$Skip)
{
    if ($Skip)
    {
        Write-Host "Restart IIS before running the class"
        return
    }

    iisreset /restart | Select-Object -Last 1 | ForEach-Object { Write-Host $_ }
}

# Waits until the class wrote FINISHED. A run that stops writing for $StallSeconds has ended the AOS, or never started.
function Wait-Results([string]$File, [int]$StallSeconds)
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
                throw "The run stopped after: $Last"
            }
        }

        Start-Sleep -Seconds 5
    }
}
