# Developing the interpreter

How to set up an environment, change the interpreter and prove a change against the real X++ compiler. What the compiler does is in [XppSemantics.md](XppSemantics.md), and the open work is in [Backlog.md](Backlog.md).

## What is where

| Folder | What |
|---|---|
| `Projects/XppInterpreter/Lexer` | Tokens, literals and the macro processor (`XppMacroProcessor.cs`) |
| `Projects/XppInterpreter/Parser` | The syntax tree, type inference (`Metadata/XppTypeInferer.cs`) and editor hints |
| `Projects/XppInterpreter/Interpreter` | Bytecode generation and execution. Exits through `finally` blocks: `PendingExit.cs`, `FinallyBlock.cs` |
| `Projects/XppInterpreter/Core` | Scopes, exception handlers and transaction rules (`ExceptionHandler.cs`) |
| `Projects/XppInterpreter/Core/Clr` | .NET calls: type resolution, overloads, conversions, the AOS metadata |
| `Metadata/MXppTools` | The X++ package, with `XppInterpreter.dll` in its `bin` and two models |
| `Metadata/MXppTools/MXTCore` | The model a script needs to run: the nine `MXTXppCore*` classes the interpreter calls, and the reference to the DLL |
| `Metadata/MXppTools/MXppTools` | The model of the editor: forms, the debugger, the execution log and the web resources |
| `Projects/XppInterpreter.Tests` | C# tests, the conformance file and the benchmark, each with its tool |
| `Scripts` | Build, conformance, benchmark and reference scripts |

The interpreter never calls D365 directly. Everything goes through the proxy interfaces in `Interpreter/Proxy`, which the X++ classes implement on an AOS and `Tests/Infrastructure/FakeXppHost.cs` implements in the tests.

In the X++ proxies, `value != null` is false for an empty record, because X++ treats it as null: test the type with `IsInstanceOfType` instead.

## The core classes

Nine classes named `MXTXppCore*` are all the X++ code a script needs to run. They are the model `MXTCore`, with the `AxReference` of `XppInterpreter.dll`, and use no table, label or form. The editor, the debugger and the execution log are the model `MXppTools`, which takes the proxy from `MXTXppCoreScript` (`sharedProxy`, `newProxy`).

Both models are in the package `MXppTools`, so they compile into one assembly, need no module reference between them, and an environment gets one package. The descriptor of each model is in `Metadata/MXppTools/Descriptor`.

`MXTCore` is a model of its own so that a project can move it into its own package ([CallingScripts.md](CallingScripts.md), "Moving the core into your own package"), and compare its files with this repository afterwards. **Keep the publisher of `MXppTools` the same as the publisher of `MXTCore`.** The compiler writes the publisher of a model's descriptor into every method of it, and a call into a model of another publisher costs 20 µs, also inside one package. With two publishers, a statement of a script run from the editor takes 22 to 33 µs instead of 2 to 3.

| Class | What it holds |
|---|---|
| `MXTXppCoreScript` | What other code calls ([CallingScripts.md](CallingScripts.md)), and the creation of the proxy |
| `MXTXppCoreIntrinsicFunctionProvider` | The compile-time functions, such as `tableNum` and `fieldStr`, and the checks of their arguments |
| `MXTXppCorePredefinedFunctions` | The predefined functions that reflection can't call, such as `strFmt` and `conPeek`. Every static method of it is a function of a script |
| `MXTXppCoreBinaryOperationProxy` | The binary operators and the negation |
| `MXTXppCoreCastingProxy` | Conversions, arrays, `is` and `as`, and the .NET types of the X++ base types |
| `MXTXppCoreDataAccessProxy` | Transactions, `changecompany`, `unchecked`, `next` and `flush`, and the exceptions |
| `MXTXppCoreReflectionProxy` | Calls, properties, fields, enums, labels, macro libraries and the syntax of a method |
| `MXTXppCoreQueryGenerator` | The SysDa statement of a `select` and of the set-based statements. The same class is the object that creates a generator for each statement, and the one that reads the rows of a select |
| `MXTXppCoreQueryExpressionGenerator` | The `where` and `setting` expressions of those statements |

Their messages are texts in the code, not labels, and the classes are public, because after such a move another package calls them. Keep the editor's classes out of them: `MXTCore` must compile without the model `MXppTools`, which the compiler doesn't check while both are in one package. The Visual Studio project `Projects/MXT/MXTFO` is for development and holds the editor model only: a project belongs to one model, so open the core classes from the Application Explorer.

## Setting up a development VM

1. Clone the repository and close Visual Studio.
2. Replace the package with a junction to the repository, so the AOS, Visual Studio and git see the same files:
   ```
   mklink /J C:\AOSService\PackagesLocalDirectory\MXppTools <repository>\Metadata\MXppTools
   ```
   Move any old `MXppTools` folder away first. File Explorer windows on it must be closed too.
3. Build and deploy, as Administrator. This builds the DLL in Debug, compiles the package (both models) and its labels, copies the web resources and restarts IIS:
   ```
   .\Scripts\BuildInterpreter.ps1 -IncludeModel
   ```
4. Open `/?mi=MXTScriptEditor`.

Tools: Visual Studio 2022 (its MSBuild), the .NET SDK with the .NET Framework 4.7.2 targeting pack for the tests, Node.js for the conformance tool, and the Windows SDK's .NET Framework tools for `ildasm`.

## The everyday loop

1. Change the C# code and run the tests. They need no environment:
   ```
   dotnet test Projects\XppInterpreter.Tests
   ```
2. Deploy with `Scripts\BuildInterpreter.ps1`. Add `-IncludeModel` when X++ code changed, or when the C# classes the X++ code calls did.
3. Try it in the editor.

The committed `Metadata/MXppTools/bin/XppInterpreter.dll` is a Debug build, and the build action replaces it with its own Release build. Commit a rebuilt DLL on its own, after the source it comes from, so the source can be reviewed without it.

## Deciding what X++ does

The real compiler decides, not the documentation, which has been wrong more than once. `Projects/XppInterpreter.Tests/Conformance/conformance.txt` holds the snippets and what the compiler did with each:

```
### pK04_declUsesPrevious
int a = 1, b = a + 1;
return int2Str(b);
=> 2
```

- The snippet is the body of a static method that returns a `str`.
- `[D365]` after the name marks a snippet that needs real D365 types. Only the AOS run covers it, and the C# tests skip it.
- The last line is the result: `=> value`, or a note: `!! REJECTED: <compiler message>`, `!! CRASHES ...` or `!! ABORTS ...`.
- Never write an expected value by hand: write `=> ?` and let the compiler fill it in.
- Data differs between environments, so a case that reads tables compares two queries, or counts, rather than printing a value. The `pM` cases insert their own rows in a transaction that ends with `ttsabort`, so they print values.
- **The SQL a statement produces** is the sharpest comparison for data statements: `select generateonly ...` and `buffer.getSQLStatement()`, with the partition and company replaced (`pM54` onwards). Both sides generate their SQL through the same kernel, so a different text is a different translation, even when the rows happen to agree.
- A statement that ends the class runner with an internal error (`pM53`) stops the run: mark it `!! ABORTS` and start again.

To check a new behaviour, add the case and run it on an AOS:

```
.\Scripts\RunConformance.ps1 -Mode Compiled -Model <model> -AddModuleReferences ContactPerson
    open /?mi=SysClassRunner&cls=DEVXppProbe
.\Scripts\RunConformance.ps1 -Mode Update
.\Scripts\RunConformance.ps1 -Mode Interpreted -Model <model>
    open /?mi=SysClassRunner&cls=DEVXppConformance
.\Scripts\RunConformance.ps1 -Mode Compare
.\Scripts\RunConformance.ps1 -Mode Cleanup -Model <model>
```

- **The model:** use one of your own that references ApplicationSuite. The script writes the classes `DEVXppProbe` and `DEVXppConformance` there, and `-AddModuleReferences` adds what the D365 cases need. `Cleanup` removes the classes, restores the descriptor and compiles the model again.
- **Update and Compare** wait until the class finishes, so you can start them right after opening the page.
- **When the compiler refuses a case**, the compile fails with the method name and message: write `!! REJECTED: <message>` as its last line and compile again.
- **When the compiler itself crashes**, the log shows `Unspecified Fatal Error` with the method: mark it `!! CRASHES the X++ compiler`.
- **When a case ends the AOS process**, the run stops and the results end with that case: mark it `!! CRASHES the AOS when compiled` and run again.

After `Compare`, every case has to match, except the ones listed in `ConformanceTests.cs`:
- `KnownDifferences`: compiled X++ loses a value and the interpreter keeps it.
- `AcceptedByTheInterpreter`: snippets the compiler refuses and the interpreter runs.

`ConformanceTests` runs every case without `[D365]` in the C# tests, so a fix stays fixed.

Write the C# test for a fix as well, in the file of its area, and give the expected value from the conformance case. The editor, the debugger and the AOS-only paths can only be tried on the AOS.

### Reading the IL

When the result of a case doesn't explain why the interpreter differs, compile the statement and read what the compiler made of it. `ildasm` on the model's `.netmodule` shows the kernel calls: the X++ body is the method whose name starts with a backtick, because the method with the plain name only handles Chain of Command.

`Scripts\CheckContainerIL.ps1 -Model <model>` does this for the statements that put a value in a container or read one back. It compiles them in a temporary class, disassembles them, compares the calls with a list, and checks that the interpreter makes the same calls. It exits with 1 when something differs. Add a statement to its list when the interpreter starts to follow another piece of generated code.

## Measuring speed

A change in the instructions, the scopes or the proxies can make every operation of a script slower without failing a test. `Scripts\RunBenchmark.ps1` times the snippets of `Projects/XppInterpreter.Tests/Benchmark/benchmark.txt` compiled and interpreted on an AOS, and compares two runs: keep one before the change with `-Mode Report -Save before`, and compare after it with `-CompareWith before`. [Performance.md](Performance.md) has the workflow, what each operation costs and the rules that keep it that way.

## Build dependencies

Every assembly that the X++ code names, directly or through a type it uses, has to exist where the model is compiled. A build server has fewer assemblies than a development VM: the SQL trace feature once broke builds with `Microsoft.Dynamics.AX.Services.Tracing.*`.

- `XppInterpreter.dll` references only `mscorlib`, `System` and `System.Core`. Code that needs another assembly finds it by name at run time, as `Core/Clr/AosMetadata.cs` does.
- `BuildDependencyTests` fails when the DLL or the X++ source gets a new reference.
- `Scripts/CompareBuildReferences.ps1 -BaseRef upstream/main` compiles the model of that commit in a temporary folder and compares what both compiled models and both DLLs reference. Run it before a pull request that touches X++ or the DLL's references.

## Trying it in the editor

The tests can't reach the editor, so after a change there check by hand:
- Completion after a namespace, a type and a variable, and the hint of an overloaded method while typing its arguments.
- A script with a macro library (`#macrolib.AOT` and `#TablesPath`), and one with a type from a model reference, such as `Renci.SshNet.Common.SshException`, run right after an IIS restart.
- A macro library saved in Visual Studio is used by a script within 5 seconds, without a build or a restart: `AosMetadata` reads the model XML when the environment has it, and `MXTXppCoreReflectionProxy` keeps the answer for 5 seconds because the editor asks on every change.
- The debugger: a breakpoint, the inspector on a .NET object, the immediate window and stepping.

## Before pushing

- **Private data:** scripts used for testing can hold connection strings and names of real systems. Keep them out of the repository, and scan the files rather than a diff: `grep` on `git diff --text` stops matching lines when the diff contains a binary file, so it reports nothing.
- **Connection strings:** never print them in logs or in issues. `conformance-tool.js` masks `AccountKey`, `SharedAccessSignature` and `sig` in what it shows.
- **Review comments**, including automatic ones, are claims to test: run the snippet compiled and interpreted before changing anything.
