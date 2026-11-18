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

. (Join-Path $PSScriptRoot "ModelRun.ps1")

$Tool = Join-Path (Split-Path $PSScriptRoot) "Projects\XppInterpreter.Tests\Conformance\conformance-tool.js"

# The tool writes these: keep them in line with conformance-tool.js
$Classes = @{ Compiled = "DEVXppProbe"; Interpreted = "DEVXppConformance" }
$Results = @{ Update = "C:\Windows\Temp\DEVXppProbe.txt"; Compare = "C:\Windows\Temp\DEVXppConformance.txt" }

switch ($Mode)
{
    { $_ -in "Compiled", "Interpreted" }
    {
        $Packages = Get-PackagesFolder
        $AxClass = Get-AxClassFolder $Packages $Model

        Add-ModuleReferences $Packages $Model $AddModuleReferences

        foreach ($File in $Results.Values) { Remove-Item $File -ErrorAction SilentlyContinue }
        Remove-ModelRunClasses $AxClass $Classes.Values

        node $Tool $Mode.ToLowerInvariant() $AxClass

        if ((Invoke-ModelCompile $Packages $Model) -gt 0)
        {
            throw "The model doesn't compile. A snippet the compiler refuses gets '!! REJECTED: <message>' in conformance.txt."
        }

        Restart-Aos $NoRestart
        Write-Host "Open /?mi=SysClassRunner&cls=$($Classes[$Mode]) and wait for '$($Classes[$Mode]) finished', or run the next step: it waits."
    }

    { $_ -in "Update", "Compare" }
    {
        try
        {
            Wait-Results $Results[$Mode] $StallSeconds
        }
        catch
        {
            throw "$($_.Exception.Message)`nIf the AOS restarted, that case ends the process: mark it with '!! CRASHES' in conformance.txt and run again."
        }

        node $Tool $Mode.ToLowerInvariant()
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

        foreach ($File in $Results.Values) { Remove-Item $File -ErrorAction SilentlyContinue }
    }
}
