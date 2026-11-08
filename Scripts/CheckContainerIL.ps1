# Compiles the X++ statements that put a value in a container or read it back, disassembles them
# and checks that the compiler still makes the calls the interpreter copies. See "Reading the IL" in Docs\Development.md.
#
#   .\CheckContainerIL.ps1 -Model <model>
#
# Use a model of your own. The script writes the class DEVXppContainerIL there, compiles the model, and removes the class at the end.
# Exits with 1 when something differs.
param
(
    [Parameter(Mandatory = $true)]
    [string]$Model
)

$ErrorActionPreference = "Stop"

$RepoPath = Split-Path $PSScriptRoot
$WorkFolder = Join-Path $env:TEMP "MXppToolsContainerIL"
$ClassName = "DEVXppContainerIL"

$Statements = @(
    @{
        Name     = "literal"
        Source   = "public static container literal(UserInfo _record, int _int, str _str, container _inner, anytype _any) { container c = [_record, _int, _str, _inner, _any]; return c; }"
        Expected = "ContainerPack AddToContainer ContainerPack AddToContainer ContainerPack AddToContainer AddToContainer ContainerPack AddToContainer"
    },
    @{
        Name     = "multipleAssignment"
        Source   = "public static str multipleAssignment(container _c) { UserInfo record; int value; [record, value] = _c; return record.Id; }"
        Expected = "qpeek qpeek"
    },
    @{
        Name     = "peek"
        Source   = "public static str peek(container _c) { UserInfo record = conPeek(_c, 1); return record.Id; }"
        Expected = "qpeek"
    },
    @{
        Name     = "insertRecord"
        Source   = "public static container insertRecord(container _c, UserInfo _record) { return conIns(_c, 1, _record); }"
        Expected = "qins"
    },
    @{
        Name     = "addRecord"
        Source   = "public static container addRecord(container _c, UserInfo _record) { _c += _record; return _c; }"
        Expected = "PlusRecord"
    }
)

# What the kernel functions do with an element
$KernelFunctions = @(
    @{ Name = "ContainerPack"; Contains = "isinst     Microsoft.Dynamics.Ax.Xpp.Common" },
    @{ Name = "qpeek"; Contains = "ContainerUnpack" },
    @{ Name = "qins"; Contains = "ContainerPack" }
)

# The interpreter's side of the same statements
$InterpreterCode = @(
    @{ File = "Projects\XppInterpreter\Interpreter\Bytecode\Container.cs"; Contains = "Casting.ContainerPack(context.Stack.Pop())" },
    @{ File = "Projects\XppInterpreter\Interpreter\Bytecode\ContainerStore.cs"; Contains = "Casting.ContainerUnpack(" },
    @{ File = "Metadata\MXppTools\MXppTools\AxClass\MXTXppCastingProxy.xml"; Contains = "PredefinedFunctions::ContainerPack(value)" },
    @{ File = "Metadata\MXppTools\MXppTools\AxClass\MXTXppCastingProxy.xml"; Contains = "PredefinedFunctions::ContainerUnpack(value)" },
    @{ File = "Metadata\MXppTools\MXppTools\AxClass\MXTXppPredefinedFunctions.xml"; Contains = "return conIns(con, start, value);" },
    @{ File = "Metadata\MXppTools\MXppTools\AxClass\MXTXppPredefinedFunctions.xml"; Contains = "return conPeek(_container, number);" },
    @{ File = "Metadata\MXppTools\MXppTools\AxClass\MXTXppBinaryOperationProxy.xml"; Contains = "items += record;" }
)

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

function Write-ProbeClass([string]$File)
{
    $Methods = ($Statements | ForEach-Object { "<Method><Name>$($_.Name)</Name><Source><![CDATA[`n    $($_.Source)`n]]></Source></Method>" }) -join "`n"

    $Xml = @"
<?xml version="1.0" encoding="utf-8"?>
<AxClass xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
    <Name>$ClassName</Name>
    <SourceCode>
        <Declaration><![CDATA[
public class $ClassName
{
}
]]></Declaration>
        <Methods>
$Methods
        </Methods>
    </SourceCode>
</AxClass>
"@

    Set-Content -Path $File -Value $Xml -Encoding UTF8
}

function Invoke-ModelCompile([string]$Packages)
{
    $Log = Join-Path $WorkFolder "xppc.log"
    $XmlLog = Join-Path $WorkFolder "xppc.xml"

    & "$Packages\bin\xppc.exe" -metadata="$Packages" -compilermetadata="$Packages" -modelmodule="$Model" -output="$Packages\$Model\bin" -referenceFolder="$Packages" -refPath="$Packages\bin" -log="$Log" -xmlLog="$XmlLog" | Out-Null

    $Summary = Get-Content $Log | Select-String -Pattern "^\s*Errors:\s*(\d+)"

    if (-not $Summary -or [int]$Summary.Matches[0].Groups[1].Value -ne 0)
    {
        throw "The compile of $Model failed: see $Log"
    }
}

function Get-MethodBody([string[]]$Il, [string]$Name)
{
    # The X++ body is the method named with a backtick: the method with the plain name only handles Chain of Command
    $End = -1
    $Marker = "end of method $ClassName::'" + [char]96 + "$Name'"

    for ($Index = 0; $Index -lt $Il.Count; $Index++)
    {
        if ($Il[$Index].Contains($Marker))
        {
            $End = $Index
            break
        }
    }

    if ($End -lt 0)
    {
        throw "Method $Name not found in the IL"
    }

    $Start = $End

    while ($Il[$Start] -notmatch '^\s*\.method ')
    {
        $Start--
    }

    return $Il[$Start..$End]
}

function Test-Result([string]$Title, [bool]$Passed, [string]$Detail)
{
    if ($Passed)
    {
        Write-Host "  ok    $Title"
    }
    else
    {
        Write-Host "  DIFF  $Title`: $Detail"
        $script:Failures++
    }
}

$Failures = 0
$Packages = Get-PackagesFolder
$Ildasm = Get-Ildasm
$ClassFile = Join-Path $Packages "$Model\$Model\AxClass\$ClassName.xml"

New-Item -ItemType Directory -Force -Path $WorkFolder | Out-Null

try
{
    Write-Host "Compiling $ClassName in $Model"
    Write-ProbeClass $ClassFile
    Invoke-ModelCompile $Packages

    $IlFile = Join-Path $WorkFolder "$ClassName.il"
    $Il = $null

    foreach ($Module in Get-ChildItem "$Packages\$Model\bin" -Filter "Dynamics.AX.$Model.*.netmodule")
    {
        if (Test-Path $IlFile) { Remove-Item $IlFile }

        & $Ildasm /nobar "/out=$IlFile" "/item=Dynamics.AX.Application.$ClassName" $Module.FullName | Out-Null

        if ((Test-Path $IlFile) -and (Select-String -Path $IlFile -SimpleMatch "end of method $ClassName::" -Quiet))
        {
            $Il = Get-Content $IlFile
            break
        }
    }

    if (-not $Il)
    {
        throw "$ClassName not found in the compiled modules of $Model"
    }

    Write-Host "What the compiler generates:"

    foreach ($Statement in $Statements)
    {
        $Calls = @(Get-MethodBody $Il $Statement.Name | ForEach-Object {
            if ($_ -match 'PredefinedFunctions::(ContainerPack|AddToContainer|qpeek|qins|qpoke|qdel)\(') { $Matches[1] }
            elseif ($_ -match 'PlusHelper::Plus\(object\[\],') { "PlusRecord" }
        }) -join " "

        Test-Result $Statement.Name ($Calls -ceq $Statement.Expected) "expected '$($Statement.Expected)', found '$Calls'"
    }

    Write-Host "What the kernel functions do:"

    foreach ($Function in $KernelFunctions)
    {
        $FunctionFile = Join-Path $WorkFolder "$($Function.Name).il"
        & $Ildasm /nobar "/out=$FunctionFile" "/item=Microsoft.Dynamics.Ax.Xpp.PredefinedFunctions::$($Function.Name)" "$Packages\bin\Microsoft.Dynamics.AX.Xpp.Support.dll" | Out-Null

        Test-Result $Function.Name (Select-String -Path $FunctionFile -SimpleMatch $Function.Contains -Quiet) "no '$($Function.Contains)' in its IL"
    }

    Write-Host "What the interpreter does:"

    foreach ($Code in $InterpreterCode)
    {
        Test-Result "$($Code.File): $($Code.Contains)" (Select-String -Path (Join-Path $RepoPath $Code.File) -SimpleMatch $Code.Contains -Quiet) "not found"
    }
}
finally
{
    if (Test-Path $ClassFile)
    {
        Remove-Item $ClassFile
        Invoke-ModelCompile $Packages
        Write-Host "Removed $ClassName from $Model"
    }
}

if ($Failures -gt 0)
{
    Write-Host "$Failures difference(s): the interpreter no longer follows the compiler for these statements"
    exit 1
}

Write-Host "The interpreter makes the same calls as the compiled code"
