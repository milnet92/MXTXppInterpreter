# Open work

What is known to be missing or unverified, as of 2026-11-11. Add to it when you find something, and remove what you finish.

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
- **More lenient than the compiler.** The interpreter runs the 10 snippets listed in `AcceptedByTheInterpreter` (`ConformanceTests.cs`). Each one taken out of that list needs a parse error that matches the compiler's. The operand types of `+`, `>` and the other operators are not checked: it computes `"d=" + dateTime` (`pL04`) and compares a `System.Double` with a `real`, which the compiler refuses (`Operator '>' cannot be applied to operands of type 'System.Double' and 'real'`). That needs operand types in the parser, and then a case in the file.
- **Argument checks need known types.** A .NET method or constructor call is checked against the overloads when the script is parsed, with the compiler's message (`Method 'IsLetter(System.String)' is not found on type 'System.Char'.`), but only when the parser knows every argument type. An argument that is an `anytype`, a container, a late-bound object or an expression without inferred type leaves the check to run time.
- **.NET events.** `timer.Elapsed += eventhandler(...)` is refused with "does not have a field named 'Elapsed'". Compiled X++ accepts a handler whose parameters match the delegate exactly (`pL20`); a script has no class method to offer, so it would need a local function as handler.
- **A missing macro or macro library** is reported where the unexpanded `#name` confuses the lexer ("Character not recognized", or a syntax error at the next token), not by name.
- **Macro libraries on environments without source.** `AosMetadata.ReadMacroLibrary` reads the model XML when the environment has it, so a saved library is seen within 5 seconds. Without source (Tier 2 and above) it falls back to the compiled metadata, which the AOS reads once: a new library needs a restart there.
- **Records in containers in the debugger.** A container holds a record packed, as compiled X++ does, so the inspector shows that element as a `byte[]`. `VariableEntryEditor` would need the casting proxy to unpack it.
- **Debugger getters.** The inspector reads every public property of a .NET object when it pauses. Only `Task.Result` before the task ends and `Lazy.Value` before it is created are skipped (`ClrValues.CanInspect`). Other getters with side effects still run.
- **Late binding of the metadata.** `AosMetadata` finds `Microsoft.Dynamics.AX.Metadata.Storage` and `Microsoft.Dynamics.ApplicationPlatform.Environment` by name, so a change in those APIs shows at run time, as a macro library reported as missing or a warning about references, instead of when the model is compiled.
- **Inference of `+a` and `~a`.** `var x = +a;` has no inferred type, because `XppTypeInferer.VisitUnaryOperation` only handles `-` and `!`.
- **Transactions with the rollback option.** With `EmitGlobalTtsAbort`, a catch inside the script commits the inner levels down to the global transaction instead of aborting them, so the data stays visible until the script ends. Not measured on the compiler: write a case before changing it.

## Speed

What [Performance.md](Performance.md) leaves. Measure each one with `Scripts\RunBenchmark.ps1` before and after.

- **Variables by name.** Every read and write looks the name up in a dictionary, scope by scope: 0.3 to 0.5 µs. The bytecode generator knows every variable, so it could give each one a slot.
- **Operators through the proxies.** `<` makes two calls to X++ (`Greater` and `AreEqual`), `mod` takes 1.4 µs. Comparing two values of the same simple type in C# needs a conformance case for each type first.
- **The editor.** A script that runs from the editor always has the debugger attached, so every statement looks for a breakpoint with a LINQ query, also when there is none. The benchmark doesn't measure this path: it runs `evaluate`.
- **A `select` inside a loop** takes 55 µs against 10 µs compiled. The SysDa objects are built again, and every value of the `where` is compiled and interpreted as a small program (`QueryGenerationHelper.ComputeExpression`).
- **A global function** takes 2 µs: `CallGlobalOrPredefinedFunction` looks the name up three times.
- **A field read by name.** `MXTXppReflectionProxy.GetInstanceProperty` calls `getFieldValue(name)`, which takes 1.5 µs on `CustTable` and more on a bigger table; `record.(fieldId)` takes 0.6 µs. Prove first that both return the same .NET type for every kind of field.

## To probe against the compiler

Found by reading the code, not measured. Each one needs a conformance case before anything changes.

- **`buffer = null` on a table that extends another one.** `MXTXppReflectionProxy.IsCommonType` compares the base type with `Common`, which is false for a derived table such as `DirPerson`, so `Store` would set the variable to null instead of clearing the record.
- **No case covers:** a view or a data entity in a `select`, TempDB and InMemory tables (`setTmp`, `linkPhysicalTableInstance`), an extensible enum, an X++ delegate with a local function as handler, and `prmIsDefault` for an optional parameter the script leaves out.
- **The variable editor of the debugger** parses a number with the culture of the user (`VariableEntryEditor`): with a decimal comma, `1.5` is read wrong.

## Ideas

- Measure the conformance file on the next platform version, and keep the differences as cases.
- A C# test fixture for the editor hints with macros, once the column map exists.
- **An API for agents.** The form `MXTAgentMCPExecute` serves the form tools of the ERP MCP server, with the status and the return value of the script in two fields: `MXTXppInterpreterExecutor::execute` returns them in a `MXTXppInterpreterExecutionResult`. A service that takes the source and returns `{ parseErrors, infolog, returnValue }` as JSON would serve any HTTP client, and an OData action on a data entity would make it a tool of the ERP MCP server. Issue 1 of the TrudAX fork compares these options.
