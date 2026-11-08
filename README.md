<h1 align="center">MXT - X++ Interpreter (preview)</h1>

 > **_IMPORTANT:_** It is **not recommended** for Production scenarios as this tool allows you to execute code without guarantee.

This tool allows you to write, execute and debug X++ code directly in the browser, without the need to compile or wait for service restart.

## Getting started
1. Clone the repo
2. Execute `Initialize.ps1` script as *Administrator*. This will create a symbolic link into your *LocalPackageDirectory*
3. Build and synchronize the **MXppTools** model
4. Navigate to System Administration > Workspaces > **MXT - X++ Interpreter**

# Interpreter
This tool generates bytecode that is later interpreted directly during execution. The instructions that are interpreted are executed in native code using a series of proxies written in X++. That allows to simulate the execution *almost* exactly as native code would do it, without the need to compile.

![architecture](Assets/simple_arquitecture.png)

### Code editor
The editor will highlight the reserved words and will indicate you which statement is being executed when debugging. It will also tell you if your code has any syntax error.

Some keyboard shortcuts are implemented to easily execute, insert a breakpoint, step over and continue debugging. These can be found as standard shortcuts by Right click > View shortcuts on the editor.
![example](Assets/code_example.png)

The variable inspector will allow you to take a look to the variables that are currently on scope and will let you modify the values for primitive types. Using the immediate window, you will be able to execute X++ expressions while debugging.
![debugger](Assets/variable-inspector.png)

### Script repository
You can save X++ scripts into the built-in repository to later execute them.
![repo](Assets/script_repo.png)

### Running a script from an agent
The form tools of the Dynamics 365 ERP MCP server can't type into the editor, so a separate form, **Run a script from an agent** (`MXTAgentMCPExecute`, also under Links in the workspace), takes the script as a plain field:
1. `form_open_menu_item` with `MXTAgentMCPExecute`
2. `form_set_control_values` with `Script` and the source, or with `ScriptId` and the ID of a saved script, which then runs with its dependencies
3. `form_click_control` with `ExecuteButton`. The messages of the reply hold the infolog, or `Line n, position m: ...` for each parse error.

Execute empties both fields before it runs, so a second click without a new script runs nothing, and it refuses when both are set. The form has the security check and the execution log of the editor, and leaves the editor as it is.

# Use cases
* Execute, modify and save X++ scripts
* Experiment with unknown or new functionality
* Test pieces of your code
* Get metadata information quickly (class and table ids, label texts, enum values...)

## Security
For development environments no additional security is needed. On any other system a user needs the **System administrator** role and the **MXT Script executor** role (`MXTXppInterpreterScriptExecutionLog::checkSecurityRights`).

A script runs as the AOS process, and with .NET calls it can reach everything that process can: files, the network and the configuration of the environment. Give this role only to people who could deploy code to that environment.

## Limitations
* **Class declarations** are not supported
* On a line that uses a macro, the editor can't complete or show hints after the macro, because the columns of the expanded code are different
* A script that runs doesn't always compile in a class: the interpreter accepts a few things the compiler refuses, such as `System.Math::PI`. `ConformanceTests.cs` has the list

# .NET libraries
Scripts can use any .NET type the AOS can load: the framework, the assemblies in its `bin` folder and the references of the installed models. It follows the rules of the X++ compiler.

```cs
using Azure.Storage.Files.Shares;
using Models = Azure.Storage.Files.Shares.Models;

System.Threading.CancellationToken tokenNull;
System.Object objNull;

ShareClient share = new ShareClient(connectionString, 'myshare');
ShareDirectoryClient folder = share.GetRootDirectoryClient().GetSubdirectoryClient('Archive');

var items = folder.GetFilesAndDirectories(objNull, tokenNull);
var enumerator = items.GetEnumerator();

while (enumerator.MoveNext())
{
    Models.ShareFileItem item = enumerator.Current as Models.ShareFileItem;
    info(strFmt('%1 %2', item.Name, item.FileSize));
}
```

| Feature | Example |
| --- | --- |
| Full names and `using` | `System.IO.File::Exists(path)`, `using System.IO;`, `using IO = System.IO;` |
| Overloads, optional parameters, `params` | chosen with the same rules as compiled X++ |
| `byref` | `System.Int32::TryParse('5', byref value);` |
| Generics | `System.Collections.Generic.List<str> names = new System.Collections.Generic.List<str>();` |
| Arrays | `System.String[] a = new System.String[2](); a.SetValue('x', 0);` |
| Typed catch | `System.Exception ex; try { ... } catch (ex) { ... }` |
| `using` statement | `using (var writer = new System.IO.StringWriter()) { ... }` |
| Methods of X++ classes that use .NET types | `ShareClient share = MyClass::getShare();` |

The editor completes namespaces, types and members, shows every overload of a method, and the debugger shows the properties of a .NET object. A call that no overload takes is marked while typing, with the compiler's message: `Method 'IsLetter(System.String)' is not found on type 'System.Char'.`

Things that surprise people, because compiled X++ does them too:
* Type names and namespaces are case sensitive. Member names are not.
* `if (obj)` on a .NET object means *not null*, even when the object is a `Response<bool>` with the value `false`. `boolean b = obj;` does apply its implicit conversion. So `if (file.Exists(token))` is always true: assign it to a `boolean` first.
* A catch block with `Exception::CLRError` or `System.Exception` also catches the X++ errors of the code it calls.
* `strFmt("%1", dateTime)` fails with "Wrong argument type for function" for a `System.DateTime`: assign it to an utcdatetime first.
* `list[0]` and `array[0]` are refused on .NET objects, as the compiler does: use `get_Item`, `GetValue` and `SetValue`.
* A `str` is not a `System.Char`: `System.Char::IsLetter("A")` doesn't compile, `System.Convert::ToChar("A")` does.

# Development
The C# source of the interpreter is in `Projects\XppInterpreter`, and the X++ classes it talks to are in the **MXppTools** model. `Scripts\BuildInterpreter.ps1` builds the DLL, copies it to the package and restarts IIS. Add `-IncludeModel` when X++ code changed, or the C# classes X++ calls did.

## Build dependencies
Every assembly the X++ code names, directly or through a type it uses, has to exist where the model is compiled. A build server has fewer assemblies than a development VM, so a reference that compiles on the VM can break the build of every client. For that reason `XppInterpreter.dll` references only the framework, and code that needs another assembly finds it by name at run time, as `Core\Clr\AosMetadata.cs` does. `BuildDependencyTests` fails when a new reference appears.

## Tests
```
dotnet test Projects\XppInterpreter.Tests
```
They run the real lexer, parser, bytecode generator and interpreter against a fake of the D365 proxies, so they need no environment.

`Conformance\conformance.txt` holds X++ snippets with the value the **real X++ compiler** gave for each one. It is the specification: when the compiler and the documentation disagree, the compiler wins. The snippets that need real D365 types are marked `[D365]` and run only on an environment, with `Scripts\RunConformance.ps1`.

## More
* [Docs/Development.md](Docs/Development.md): setting up a VM, the conformance workflow and the build dependency check
* [Docs/XppSemantics.md](Docs/XppSemantics.md): what the X++ compiler really does, with the case that shows each fact
* [Docs/Backlog.md](Docs/Backlog.md): open work and known gaps

# Build Action
The GitHub build action is based on [FSC-PS for GitHub](https://github.com/fscpscollaborative/fscps).

# Build Status
[![(BUILD)](../../actions/workflows/build.yml/badge.svg)](../../actions/workflows/build.yml)
