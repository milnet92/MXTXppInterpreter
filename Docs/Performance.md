# How fast a script runs

What a script costs next to compiled X++, where the time goes and how to measure a change. The numbers are from 2026-11-11, platform 7.0.8199.55 (10.0.49), a one-box development VM and the Debug build of `XppInterpreter.dll`. They compare runs on the same machine: only the ratios carry over to another one.

## Measuring

`Projects/XppInterpreter.Tests/Benchmark/benchmark.txt` holds snippets, each one a single construct inside a loop. `Scripts\RunBenchmark.ps1` writes every snippet twice into a class of a model of yours, as a compiled method and as a script for `MXTXppInterpreterExecutor::evaluate`, and the class times both:

```
.\Scripts\RunBenchmark.ps1 -Mode Run -Model <model> -AddModuleReferences ContactPerson
    open /?mi=SysClassRunner&cls=DEVXppBenchmark&cmp=USMF
.\Scripts\RunBenchmark.ps1 -Mode Report -Save before
    change the interpreter, deploy it with Scripts\BuildInterpreter.ps1, open the class again
.\Scripts\RunBenchmark.ps1 -Mode Report -CompareWith before
.\Scripts\RunBenchmark.ps1 -Mode Cleanup -Model <model>
```

- **The model** is changed only until `Cleanup`, as with `RunConformance.ps1`: both scripts share `Scripts\ModelRun.ps1`, and a cleanup of either removes the classes of both.
- **Report** waits until the class finishes, shows the best of two runs each way, and marks a case where the compiled and the interpreted result differ.
- **`-Filter b4`** writes only the cases whose name starts with it.
- **The time of a construct** is the time of its case minus the time of `b01_emptyLoop` for the same number of iterations.
- **The C# tests can't measure this:** most of the time was spent in the X++ proxies and in reflection over real tables, which the fake host of the tests doesn't have. `LookupCachingTests` only checks that the number of lookups doesn't grow with the iterations of a loop.

## Before and after the lookups were cached

Milliseconds for the whole case. "Before" is commit 3da5994.

| Case | Compiled | Before | After |
|---|---|---|---|
| `b49_typicalScript`: `while select` over 5,997 sales lines with field reads, an enum and a `Map` | 370 | 10,176 | 508 |
| `b40_fieldRead`: 20,000 reads of `custTable.AccountNum` | 6 | 2,332 | 76 |
| `b43_whileSelectFields`: 9,415 rows, two fields read | 205 | 2,016 | 266 |
| `b08_enumLiteral`: 50,000 times `if (v == NoYes::Yes)` | 0.04 | 1,548 | 133 |
| `b05_strAssign`: 50,000 assignments to a `str` | 0.5 | 596 | 80 |
| `b12_edtDeclarationInLoop`: 20,000 times two string EDT variables | 3 | 782 | 54 |
| `b13_recordDeclarationInLoop`: 20,000 times a table buffer | 10 | 737 | 72 |
| `b26_tableKernelMethod`: 20,000 times `buffer.isTmp()` | 2 | 479 | 66 |
| `b41_fieldWrite`: 20,000 assignments to a field | 11 | 277 | 58 |
| `b28_clrInstanceCall`: 20,000 times `stringBuilder.Append('a')` | 0.1 | 263 | 41 |
| `b46_selectInLoop`: 2,000 times `select firstonly` | 20 | 369 | 110 |
| `b48_findInLoop`: 2,000 times `CustTable::find` | 40 | 293 | 75 |
| `b01_emptyLoop`: 50,000 iterations of a `for` | 0.03 | 103 | 63 |
| `b42_whileSelect`: 9,415 rows, nothing read | 200 | 229 | 212 |

Fetching rows was never the cost: the interpreter runs `select` through the same kernel as compiled code. What a script did with each row was.

## Where the time went

Every one of these ran again for each operation, and now runs once:

| Operation | What it did every time | Cost of each |
|---|---|---|
| Read a field of a record | Looked for a delegate with that name among all the public members of the table class (608 on `CustTable`) | 100 µs, 220 µs on `SalesLine` |
| Enum literal | `SysDictEnum::newName` and `symbol2Enum` | 25 µs |
| Assign a string | `SysDictType::newName`, to know if the type is a string EDT | 9 µs, 17 µs for an EDT |
| Declare a table buffer | Up to four lookups in the data dictionary, to know what the type name is | 35 µs |
| Call a method of a record | `GetMethods()` of the table class (1,634 on `CustTable`), to know if it takes variable arguments | 18 µs |
| Write a field of a record | `SysDictField::newName` | 8 µs |
| Call a .NET method | Chose the overload again among all the methods with that name | 9 µs |
| Read or write a variable | Compared the name with a culture | 0.3 to 0.5 µs |

## The second round: operators and the loop itself

After the lookups, the same day: what a script costs when it touches no data. "Before" is commit 67a5e8a.

| Case | Compiled | Before | After |
|---|---|---|---|
| `b01_emptyLoop`: 50,000 iterations of a `for` | 0.03 | 63 | 34 |
| `b02_intAdd`: 50,000 times `n += i` | 0.03 | 90 | 54 |
| `b08_enumLiteral`: 50,000 times `if (v == NoYes::Yes)` | 0.04 | 133 | 69 |
| `b15_ternary`: 50,000 times `n += (i > 5) ? 1 : 2` | 0.07 | 113 | 63 |
| `b14_localFunction`: 20,000 calls of a local function | 0.07 | 56 | 37 |
| `b29_clrStaticCall`: 20,000 times `System.Math::Abs(-1)` | 0.04 | 53 | 35 |

- **Operators.** `<` was two calls to X++ (`Greater` and `AreEqual`) and `+` was one. `SimpleOperands` computes integers, comparisons of numbers, booleans and values of one enum in C#, and a `switch` tests its cases the same way.
- **Local functions.** Every call runs in a context of its own, which now creates its collections when they are first used.
- **`break` and `continue`** count the scopes they leave without copying the instructions between.
- **The data cases didn't move:** `b49_typicalScript` stays at 505 ms, next to 367 ms compiled. What is left there is the database.

## Rules that keep it fast

- **Ask the metadata once.** What a type or a member is doesn't change while the process runs. `XppProxy` keeps what the X++ proxies answer about a type (`IsXppType`, `IsCommonType`, `IsEnum`, `IsStringEdt`); `ReflectionHelper` and `ClrBinder` keep members by `MemberKey`, also the answer that a type has no such member; `MXTXppCoreCastingProxy::getXppTypeByTypeName` keeps the kind of a type name.
- **Never scan the members of a type for each operation.** A table class has hundreds of fields and more than a thousand methods.
- **A value that can't change is kept by the instruction**, as `StaticLoad` does with an enum literal. A static field is not such a value.
- **The overload of a .NET call** depends only on the types of the arguments, or on the declared type for a null (`ClrConversions.GetCost`). `ClrBinder` keeps it under exactly that, so a change that makes the cost depend on a value has to change the key.
- **An operator is computed in C# only where the result of the compiler is certain.** `SimpleOperands` holds those, and the conformance cases `pN40` to `pN50` hold their edges: integers wrap around, also in an `anytype`. What can fail or has rules of its own stays with the X++ proxies: division by zero has to remain the CLR error it is in compiled code, and strings compare without case.
- **Names are compared with `OrdinalIgnoreCase`**, never with `ToLower()`: with a Turkish culture `"I".ToLower()` is not `"i"`, and a member written in another case was not found.

## What the platform charges, compiled or not

- **Another publisher.** Every X++ method starts with `ManagedSessionState::EnterPublisherScope`. When the publisher of its model is not the one of the code that called it, the kernel opens a scope, and that takes about 20 µs for each call. Microsoft's models count as one publisher and don't pay it. The interpreter belongs to the publisher of `MXppTools`, so a script that calls a class of your own model pays it on every call (`b20` to `b22`), exactly as compiled code of one of your models pays it when it calls another publisher's model.
- **A function called from X++** pays it once for each call of `MXTXppCoreScript`, and nothing more inside the function: the proxies it uses belong to the same publisher. [CallingScripts.md](CallingScripts.md) has what such a call costs.
- **The first run** of a script compiles what it touches, in the interpreter and in the kernel: the benchmark runs each case twice and keeps the best.

## What is left

A loop costs about 0.7 µs for each iteration, some 60 ns for each instruction of the Debug build. A `select` inside a loop costs 54 µs against 10 µs compiled, of which 15 to 20 µs are SysDa itself. Sums of reals, strings and division still go through the X++ proxies. These are in [Backlog.md](Backlog.md), with what was measured for each.
