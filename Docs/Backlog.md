# Open work

What is known to be missing or unverified, as of 2026-11-06. Add to it when you find something, and remove what you finish.

## Before the .NET support is released

- **Build with Microsoft's NuGet packages.** The build action of the repository (`.github/workflows/build.yml`, version 10.0.39) has not run on this code yet. It is the check that the X++ added, such as `SysDaNotExpression` and `SysDaMinusExpression`, exists on that version. A fork needs its workflows enabled once on its Actions page before they run.
- **Other platform versions.** The conformance run was done on 7.0.8199.55 only.
- **Batch.** Scripts that run in batch were not tested with .NET calls.
- **Writing to external systems.** The Azure acceptance test only read: a share and its folders, file lists and downloads.

## Decisions for the maintainer

- **A switch for .NET calls** outside development environments. A script runs as the AOS process, so .NET calls reach its files, the network and its configuration. Today only the **MXT Script executor** role controls it. A parameter on `MXTInterpreterParameters` would need a new field and a database sync.
- **`dotnet test` in `build.yml`.** The workflow builds the DLL but doesn't run the tests.
- **`AppTroubleshooting`** is still a module reference of the descriptor, left over from the removed SQL trace feature. Nothing in the compiled model references it.
- **The model version** in the descriptor wasn't changed for this work.

## Known gaps

- **Macro lines in the editor.** On a line that uses a macro, completion and hints after the macro don't work, because the expanded text has other columns. Running is not affected. A fix needs a map from expanded to original columns in `XppMacroProcessor`, used by the editor's position checks in `XppParser`.
- **More lenient than the compiler.** The interpreter runs the 8 snippets listed in `AcceptedByTheInterpreter` (`ConformanceTests.cs`). Each one taken out of that list needs a parse error that matches the compiler's. It also compares a `System.Double` with a `real`, which the compiler refuses (`Operator '>' cannot be applied to operands of type 'System.Double' and 'real'`): that needs operand types in the parser, and then a case in the file.
- **Debugger getters.** The inspector reads every public property of a .NET object when it pauses. Only `Task.Result` before the task ends and `Lazy.Value` before it is created are skipped (`ClrValues.CanInspect`). Other getters with side effects still run.
- **Late binding of the metadata.** `AosMetadata` finds `Microsoft.Dynamics.AX.Metadata.Storage` and `Microsoft.Dynamics.ApplicationPlatform.Environment` by name, so a change in those APIs shows at run time, as a macro library reported as missing or a warning about references, instead of when the model is compiled.
- **Inference of `+a` and `~a`.** `var x = +a;` has no inferred type, because `XppTypeInferer.VisitUnaryOperation` only handles `-` and `!`.
- **Transactions with the rollback option.** With `EmitGlobalTtsAbort`, a catch inside the script commits the inner levels down to the global transaction instead of aborting them, so the data stays visible until the script ends. Not measured on the compiler: write a case before changing it.

## Ideas

- Measure the conformance file on the next platform version, and on 10.0.39, and keep the differences as cases.
- A C# test fixture for the editor hints with macros, once the column map exists.
