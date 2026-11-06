# What the X++ compiler really does

Each fact was measured with the real compiler on platform 7.0.8199.55. The case named after it in `Projects/XppInterpreter.Tests/Conformance/conformance.txt` shows it, so it can be checked again on another version. Several of them contradict the documentation.

## Operators and expressions

| Fact | Case |
|---|---|
| `&&` and `\|\|` have the **same** precedence and run from left to right: `t \|\| f && f` is false | `pA01` |
| Unary minus applies to the next operand only: `-1 + 2` is 1 | `pA02` |
| `div` is integer division, `mod` keeps the sign of the left operand | `pA03` |
| Arguments, container elements and binary operands run from left to right, and the caller before its arguments | `pA15` to `pA18` |
| `a < b == true` needs parentheses | `pA45` |
| Only `+=`, `-=`, `*=` and `/=` exist. `\|=`, `&=`, `^=`, `<<=`, `>>=` and `%=` are refused | `pF40` to `pF42`, `pH12` to `pH16` |
| `a = b = c` and `a++` inside an expression are refused | `pG53`, `pG54` |
| `like` works outside queries too | `pE07` |
| A real needs digits on both sides of the point: `1e3` and `.5` are refused, `1.5e2` and `1.5e-2` are valid | `pF24`, `pF99`, `pH10`, `pH11` |
| `date - date` is an int, and a date plus an int is a date | `pI02`, `pI03` |
| An utcdatetime literal is written `2020-01-02T03:04:05` | `pF18` |
| A real assigned to an int is truncated | `pA21` |

## Declarations

| Fact | Case |
|---|---|
| `int a = 1, b = a + 1;` is valid: a declaration can use the one before it | `pK04`, `pK05` |
| `int a[2], c, b[3];` mixes arrays and variables. `b = a` makes both names share one array, and `a[0] = 0` clears it | `pH93`, `pF38`, `pF36` |
| A local `const` needs a constant value: a literal, an enum value or another const. It can't be assigned, and local functions can use it | `pI07` to `pI14` |
| A variable can't have the name of one in an outer block | `pG48` |
| `appl`, `infolog` and `classFactory` exist without a declaration and can't be shadowed | `pI15` to `pI17` |
| A local function can't be called before its declaration | `pI33` |
| `default` must be the last part of a `switch` | `pK48` |

## Strings, labels and macros

| Fact | Case |
|---|---|
| A verbatim string ends at the first delimiter: `@"a""b"` is refused, `@"a\"` is `a\` | `pK40` to `pK43` |
| A string that holds only a label id is translated (`"@SYS1"`, `'@SYS1'`, `@"@SYS1"`). `"@SYS1 x"` isn't, and an unknown label stays as written | `pH53` to `pH58` |
| Macros are replaced neither in strings nor in comments, and their names are not case sensitive | `pH19`, `pH30`, `pH28` |
| A macro is text: `%1` is replaced everywhere in its body, also inside strings, and becomes empty text without an argument, even without parentheses | `pH26`, `pI65`, `pI68`, `pH34` |
| Macro arguments are split at commas outside nested parentheses and strings | `pI66`, `pI67` |

## Control flow and exceptions

| Fact | Case |
|---|---|
| `finally` runs on `return`, `break`, `continue` and on exceptions | `pA30`, `pD18` to `pD23` |
| A `try` inside a `finally` keeps what the block is holding: the pending `return`, or the exception on its way out | `pK20` to `pK30` |
| An exception that leaves a `finally` block replaces its pending `return` | `pK29` |
| A `throw` inside `finally` is refused, also in a nested `try`, but a call that throws is valid | `pK46`, `pK47`, `pK24` |
| A loop inside a `finally` block can use `break` | `pK25` |
| `return;` in a function with a return type, and a value returned from a void function, are refused | `pK44`, `pK45` |
| A `try` inside a transaction catches only `UpdateConflict` and `DuplicateKeyException`. A bare `catch`, `Deadlock`, `Error`, `CLRError` and typed catches skip to the code outside the transaction, and `finally` still runs | `pH43` to `pH52`, `pI50` to `pI58` |
| A caught conflict inside a transaction restores the level the `try` started at, and a catch at level 0 aborts everything | `pI53`, `pI54` |
| `xSession::currentRetryCount()` keeps its value after a call to a method with its own `try` | `pK35` to `pK37` |
| `catch (Exception::CLRError)` gets .NET exceptions, and `CLRInterop::getLastException()` returns them inside a `TargetInvocationException` | `pB30`, `pB79` |
| A catch variable of type `System.Exception` also catches X++ errors | `pB80` |

## .NET types

| Fact | Case |
|---|---|
| Type names and namespaces are case sensitive, member names are not | `pB74`, `pB72`, `pB73` |
| `if (obj)` on a .NET object only tests that it isn't null, even for a `Response<bool>` whose value is false. `boolean b = obj;` applies its implicit conversion: that was measured with Azure, so the case isn't in the file | `pB41` |
| An implicit operator applies to an argument, but not when a `DateTime` is assigned to a `DateTimeOffset` variable | `pC08`, `pC07` |
| A typed catch is `System.Exception e; ... catch (e)` | `pB75` to `pB78` |
| Generic types are valid (`List<str>`), generic methods can't be called, and `using` doesn't apply to generic types | `pB81` to `pB88` |
| A .NET array is created with `new System.String[n]()` | `pB13`, `pB70` |
| Const fields of referenced assemblies can't be read: `System.Math::PI` is refused | `pB91` |
| `var` can't name a nested generic struct, such as `List<T>.Enumerator` | `pC09`, `pC40` |
| `System.Double` values are compared and computed as doubles. Comparing one with a `real` is refused, which the interpreter doesn't check yet | `pK10` to `pK15` |
| The argument types choose the overload: `var n = System.Math::Abs(-1);` is an int | `pK01` to `pK03` |

## Records and queries

| Fact | Case |
|---|---|
| `buf.(fieldId)` works, also in a `where`, and so does `!buf.(fieldId)` | `pI84` to `pI86`, `pJ20`, `pK50`, `pK51` |
| `buf.Map::method()` calls a table map method, also in a `where` | `pI76` to `pI80` |
| `orig()` and `data()` are typed as the table, not as `Common` | `pH99` |
| `select nofetch` leaves the record empty until `next` | `pH70`, `pJ17`, `pJ18` |

## Things that end the AOS or the compiler

These run in the interpreter, but compiled code fails, so the cases have no value to compare:

| What | Case |
|---|---|
| A nested .NET type used through a variable ends the AOS process | `pB22` |
| An uninitialized `Nullable` passed to a method ends the class runner | `pC03` |
| `-a` on a `System.Double` ends the AOS process | `pK13` |
| `retry` in a `try` with `finally`, inside a `catch`, crashes the compiler | `pK31` |

## Where the interpreter differs on purpose

- **Values compiled X++ loses** (`KnownDifferences` in `ConformanceTests.cs`): a generic dictionary declared with `var` (`pB82`), an implicit operator on assignment (`pC07`), and a `DateTime` assigned to an utcdatetime (`pB24`). The interpreter keeps the value.
- **Code the compiler refuses and the interpreter runs** (`AcceptedByTheInterpreter`): 8 cases, such as `System.Math::PI` and `var` with a nested enumerator. A script that runs doesn't always compile in a class.
