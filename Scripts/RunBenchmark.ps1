# Measures the interpreter against compiled X++ on this environment, with the snippets of
# Projects\XppInterpreter.Tests\Benchmark\benchmark.txt. See Docs\Development.md. Execute as Administrator: it restarts IIS.
#
#   .\RunBenchmark.ps1 -Mode Run -Model MyModel -AddModuleReferences ContactPerson
#       open /?mi=SysClassRunner&cls=DEVXppBenchmark&cmp=USMF
#   .\RunBenchmark.ps1 -Mode Report -Save before        shows the times and keeps them under a name
#       change the interpreter, deploy it, open the class again
#   .\RunBenchmark.ps1 -Mode Report -CompareWith before  shows the times next to the ones kept
#   .\RunBenchmark.ps1 -Mode Cleanup -Model MyModel
#
# -Filter b4 runs only the cases whose name starts with it. The model is changed only until Cleanup, as with RunConformance.ps1.
# After a deploy of the interpreter the class is still compiled: opening it again is enough, and Report waits for the new run.
param
(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Run', 'Report', 'Cleanup')]
    [string]$Mode,

    [string]$Model,

    [string[]]$AddModuleReferences = @(),

    [string]$Filter,

    [string]$Save,

    [string]$CompareWith,

    [switch]$NoRestart,

    # Report waits for the class to finish. The slowest case takes about 20 seconds before the first optimizations.
    [int]$StallSeconds = 180
)

$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "ModelRun.ps1")

$Tool = Join-Path (Split-Path $PSScriptRoot) "Projects\XppInterpreter.Tests\Benchmark\benchmark-tool.js"
$Results = "C:\Windows\Temp\DEVXppBenchmark.txt"

function Get-SavedResults([string]$Name)
{
    return Join-Path $ModelRunWorkFolder "benchmark-$Name.txt"
}

switch ($Mode)
{
    "Run"
    {
        $Packages = Get-PackagesFolder
        $AxClass = Get-AxClassFolder $Packages $Model

        Add-ModuleReferences $Packages $Model $AddModuleReferences
        Remove-Item $Results -ErrorAction SilentlyContinue

        node $Tool generate $AxClass $Filter

        if ((Invoke-ModelCompile $Packages $Model) -gt 0)
        {
            throw "The model doesn't compile. The CustTable cases need -AddModuleReferences ContactPerson."
        }

        Restart-Aos $NoRestart
        Write-Host "Open /?mi=SysClassRunner&cls=DEVXppBenchmark&cmp=USMF, then run -Mode Report: it waits."
    }

    "Report"
    {
        Wait-Results $Results $StallSeconds

        $Arguments = @("report")

        if ($Save) { $Arguments += "--save", (Get-SavedResults $Save) }
        if ($CompareWith) { $Arguments += "--compare", (Get-SavedResults $CompareWith) }

        node $Tool @Arguments
    }

    "Cleanup"
    {
        $Packages = Get-PackagesFolder
        $AxClass = Get-AxClassFolder $Packages $Model

        Remove-ModelRunClasses $AxClass
        Restore-Descriptor $Packages $Model

        if ((Invoke-ModelCompile $Packages $Model) -gt 0)
        {
            throw "$Model doesn't compile after the cleanup"
        }

        Restart-Aos $NoRestart
        Remove-Item $Results -ErrorAction SilentlyContinue
    }
}
