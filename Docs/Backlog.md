# Open work

What is known to be missing or unverified, as of 2026-11-12. Add to it when you find something, and remove what you finish.

## Before the .NET support is released

- **Other platform versions.** The conformance run was done on 7.0.8199.55 (10.0.49) only. The build action compiles the model on **10.0.48** (platform 7.0.7996.33), the oldest version the tool supports, with the platform assemblies of a build server: run 42430981050 on the fork passed with 0 errors. 10.0.39 is not a target: its platform has no `SysDaNotExpression`, and Microsoft supports the two latest versions only.
- **Batch.** Scripts that run in batch were not tested with .NET calls.
- **Writing to external systems.** The Azure acceptance test only read: a share and its folders, file lists and downloads.

## Decisions for the maintainer

- **A switch for .NET calls** outside development environments. A script runs as the AOS process, so .NET calls reach its files, the network and its configuration. Today only the **MXT Script executor** role controls it. A parameter on `MXTInterpreterParameters` would need a new field and a database sync.
- **`dotnet test` in `build.yml`.** The workflow builds the DLL but doesn't run the tests.
- **`AppTroubleshooting`** is still a module reference of the descriptor, left over from the removed SQL trace feature. Nothing in the compiled model references it.
- **The model version** in the descriptor wasn't changed for this work.

## Known gaps

- **Macro lines in the editor.** On a line that uses a macro, completion and hints after the macro don't work, because the expanded text has other columns. Running is not affected. A fix needs a map from expanded to original columns in `XppMacroProcessor`, used by the editor's position checks in `XppParser`.
- **More lenient than the compiler.** The interpreter runs the 10 snippets listed in `AcceptedByTheInterpreter` (`ConformanceTests.cs`). Each one taken out of that list needs a parse error that matches the compiler's. The operand types of `+`, `>` and the other operators are not checked: it computes `"d=" + dateTime` (`pL04`) and compares a `System.Double` with a `real`, which the compiler refuses (`Operator '>' cannot be applied to operands of type 'System.Double' and 'real'`). That needs operand types in the parser, and then a case in the file. An extensible enum compared with `>` is computed too (`pN24`).
- **Argument checks need known types.** A .NET method or constructor call is checked against the overloads when the script is parsed, with the compiler's message (`Method 'IsLetter(System.String)' is not found on type 'System.Char'.`), but only when the parser knows every argument type. An argument that is an `anytype`, a container, a late-bound object or an expression without inferred type leaves the check to run time.
- **.NET events.** `timer.Elapsed += eventhandler(...)` is refused with "does not have a field named 'Elapsed'". Compiled X++ accepts a handler whose parameters match the delegate exactly (`pL20`); a script has no class method to offer, so it would need a local function as handler.
- **A missing macro or macro library** is reported where the unexpanded `#name` confuses the lexer ("Character not recognized", or a syntax error at the next token), not by name.
- **Macro libraries on environments without source.** `AosMetadata.ReadMacroLibrary` reads the model XML when the environment has it, so a saved library is seen within 5 seconds. Without source (Tier 2 and above) it falls back to the compiled metadata, which the AOS reads once: a new library needs a restart there.
- **Records in containers in the debugger.** A container holds a record packed, as compiled X++ does, so the inspector shows that element as a `byte[]`. `VariableEntryEditor` would need the casting proxy to unpack it.
- **Debugger getters.** The inspector reads every public property of a .NET object when it pauses. Only `Task.Result` before the task ends and `Lazy.Value` before it is created are skipped (`ClrValues.CanInspect`). Other getters with side effects still run.
- **Late binding of the metadata.** `AosMetadata` finds `Microsoft.Dynamics.AX.Metadata.Storage` and `Microsoft.Dynamics.ApplicationPlatform.Environment` by name, so a change in those APIs shows at run time, as a macro library reported as missing or a warning about references, instead of when the model is compiled.
- **Inference of `+a` and `~a`.** `var x = +a;` has no inferred type, because `XppTypeInferer.VisitUnaryOperation` only handles `-` and `!`.
- **Transactions with the rollback option.** With `EmitGlobalTtsAbort`, a catch inside the script commits the inner levels down to the global transaction instead of aborting them, so the data stays visible until the script ends. Not measured on the compiler: write a case before changing it.

## Calling a script from X++

What [CallingScripts.md](CallingScripts.md) leaves.

- **A variable of the script in a function.** The statements outside the functions don't run in a call, so a function that reads such a variable fails at run time with the .NET error `Variable total was not declared.` The parser sees the declaration and reports nothing. `CompiledScript` would have to find those reads when it compiles the text, so that `validate` reports them when the text is saved. As a .NET error it passes a `catch (Exception::Error)` of the caller.
- **A wrong argument** is found by the conversion: a record of another table is an `InvalidCastException`, and a `str` for an `int` parameter becomes 0. Both reach the caller as they are, without the name of the function or the parameter.
- **A cache for the AOS.** The compiled scripts are kept for each session, because the X++ proxies are (`XppSessionStaticVariableHelper` holds every static field of an X++ class). The syntax tree and the instructions don't hold a proxy, and `CompiledScriptTests` calls one function from several threads, so they could be shared. Check first what the parser reads for the session: the value of a label, the company.
- **The statements cost twice as much in a call.** `_record.orig().Name != _record.Name` takes 13 µs in a loop of a script, 31 µs in a function called from a compiled class, and 7 µs compiled. In the call, `orig()` takes 18 µs instead of 8, a field read 3 to 5 µs instead of 2.4, and `isTmp()` 3 µs instead of 1. Why was not found: the instructions are the same, the entry into the model is paid once, and passing the record costs nothing. A profiler on the AOS would show it.

## Speed

What [Performance.md](Performance.md) leaves. Measure each one with `Scripts\RunBenchmark.ps1` before and after.

- **The instructions themselves.** A loop costs 0.7 µs for each iteration, about 60 ns for each instruction of the Debug build: the loop of `XppInterpreter.Interpret`, the stack of the scope, and one lookup of each variable by name. A slot for each variable was not tried: creating the dictionary of a scope only with its first variable, which removes the lookups in the scopes a read walks through, changed nothing, so the lookup is not what costs.
- **Operators on other types.** `SimpleOperands` computes integers, comparisons of numbers, booleans and values of one enum. A sum of reals (0.25 µs), a comparison of strings (0.4 µs), `/`, `div` and `mod` (1.4 µs) still go to the proxies. Each needs conformance cases for its edges first: an exception has to stay the CLR error it is in compiled code (`pN51` to `pN53`).
- **The editor.** A script that runs from the editor has the debugger attached and a limit for its loops: a `Debug` instruction for each statement and a counter for each iteration, 0.15 to 0.4 µs for each iteration with the fake host of the tests. The benchmark doesn't measure this path: it runs `evaluate`.
- **A `select` inside a loop** takes 54 µs against 10 µs compiled. The same SysDa statement written in compiled X++ takes 15 to 20 µs, of which building the objects is 1 µs, so that part belongs to SysDa. The other 30 µs are the many small calls between `MXTXppQueryGenerator` and the syntax tree: the hints, the selection fields, the visitor of the `where`. None stands out. Keeping the SysDa objects of a statement between two executions would need to know that its buffers and values are still the same.
- **A global function** takes 2 µs: `CallGlobalOrPredefinedFunction` looks the name up three times.
- **A field read by name** takes 2 µs. `record.(fieldId)` is faster, but not the same: on `CustTable`, `SalesLine`, `InventTrans`, `DirPerson` and `SalesTable` every field gave the same value both ways except the system field `SysDataStateCode`, so the read stays by name.

## To probe against the compiler

Found by reading the code, not measured.

- **An X++ delegate with a local function as handler.** Compiled X++ can't take a local function, so it needs a test on an AOS instead of a conformance case.
- **The variable editor of the debugger** parses a number with the culture of the user (`VariableEntryEditor`): with a decimal comma, `1.5` is read wrong.

## Ideas

- Measure the conformance file on the next platform version, and keep the differences as cases.
- A C# test fixture for the editor hints with macros, once the column map exists.
- **An API for agents.** The form `MXTAgentMCPExecute` serves the form tools of the ERP MCP server, with the status and the return value of the script in two fields: `MXTXppInterpreterExecutor::execute` returns them in a `MXTXppInterpreterExecutionResult`. A service that takes the source and returns `{ parseErrors, infolog, returnValue }` as JSON would serve any HTTP client, and an OData action on a data entity would make it a tool of the ERP MCP server. Issue 1 of the TrudAX fork compares these options.
