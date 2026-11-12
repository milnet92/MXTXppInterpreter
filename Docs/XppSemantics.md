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
| An int or an int64 that overflows wraps around, also inside an expression and in an `anytype`; an int with an int64 gives an int64 | `pN40` to `pN46` |
| Division by zero, with `/`, `div` and `mod`, is a CLR error: `catch (Exception::CLRError)` gets it, `Exception::Error` and `Exception::Numeric` don't | `pN51` to `pN53` |
| The count of a shift wraps at the size of the value: `1 << 32` is 1, and `1 << 31` is the smallest int | `pN54` |
| Strings compare without case, but a trailing blank counts: `'abc' == 'ABC'`, and `'a '` is not `'a'` | `pN55` |
| An extensible enum compares only with `==` and `!=`: `>` is refused. `enum2Str` gives its label, and its number is the one of the value | `pN20` to `pN24` |
| The `u` suffix is an unsigned 32-bit literal, not an int64: `5u` is an int, `3000000000u` is the negative int with the same bits, `4294967301u` is an int64 | `pL32`, `pL35`, `pL37`, `pL40` |

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
| `prmIsDefault` is true for a parameter the caller leaves out, and false when it is passed, also with the default value; `strFmt` prints it as Yes or No | `pN30`, `pN31` |

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
| `strFmt` refuses a `System.DateTime` ("Wrong argument type for function"), also `d.Date`, and the error is not an `Exception::Error`: assign it to an utcdatetime first | `pL01` to `pL03` |
| `strFmt` prints a .NET object with `ToString()`, a .NET enum as its number | `pL06` to `pL09`, `pL11` |
| A `str` is not a `System.Char`, in either direction: `System.Char c = 'A'`, `System.Char::IsLetter("A")` and `str s = text.get_Chars(1)` are refused. `System.Convert::ToChar` converts | `pL10`, `pL36`, `pL38` |
| `[index]` is only for X++ arrays, also on a .NET list, array or string: use `get_Item`, `GetValue` and `SetValue` | `pL15` to `pL17`, `pL26` |
| A nested generic struct can't be named at all (`List<int>.Enumerator`); a nested enum can (`System.Environment.SpecialFolder`) | `pL18`, `pL19` |
| `+=` on a .NET event takes an `eventhandler` whose parameters match the delegate exactly | `pL20` |
| A member can't be read directly on a static property: `Thread::CurrentThread.CurrentCulture` is refused, a variable in between works | `pL30`, `pL31` |
| An utcdatetime literal becomes a `DateTime` with `Kind` Utc. A `date`, or an utcdatetime from `DateTimeUtil::newDateTime`, becomes Unspecified | `pL25`, `pL33`, `pL34` |
| Boxed in a `System.Object`: `5u` is an `Int32`, a date a `Date`, `NoYes::Yes` the enum, a container an `Object[]`, an X++ object its `Microsoft.Dynamics.Ax.Xpp` type | `pL12` to `pL14` |

## Records and queries

| Fact | Case |
|---|---|
| `buf.(fieldId)` works, also in a `where`, and so does `!buf.(fieldId)` | `pI84` to `pI86`, `pJ20`, `pK50`, `pK51` |
| `buf.Map::method()` calls a table map method, also in a `where` | `pI76` to `pI80` |
| `orig()` and `data()` are typed as the table, not as `Common` | `pH99` |
| A record in a container is packed when it goes in (`ContainerPack`) and unpacked when it is read, so the container keeps a copy, and it can be saved to a container field; `container += record` uses a separate overload | `pM88` to `pM95`, `Scripts\CheckContainerIL.ps1` |
| `buffer = null` clears the record, and a null passed for a table parameter is an empty record | `pN01`, `pN02`, `pN07` |
| A table that extends another one (`DirPerson`) has the fields of its base table, in any case of the name, goes where the base table is expected, and its SQL reads the base table with `INSTANCERELATIONTYPE IN (...)` | `pN03` to `pN09` |
| A TempDB buffer shares its rows with `linkPhysicalTableInstance`; a `setTmp` buffer is apart from the table and takes `forupdate`, `doUpdate` and `order by` | `pN10` to `pN13` |
| A view and a data entity are selected like a table | `pN15` to `pN18` |
| `select nofetch` leaves the record empty until `next` | `pH70`, `pJ17`, `pJ18` |
| A `join` without `where` is a cross join, and a `where` on the first table that names a table joined later compares with the value the buffer holds, not with the joined row (`T1.PAYMTERMID=?`) | `pM22`, `pM43`, `pM59` |
| `where a \|\| b && c` is `(a \|\| b) && c`: same precedence, left to right, also in SQL | `pM06`, `pM57` |
| `exists join` and `notexists join` leave the joined buffer empty; `outer join` leaves it empty for an unmatched row, and a condition on the joined table is part of the `ON` | `pM02` to `pM05`, `pM56`, `pM64`, `pM65` |
| `index Name` and `index hint Name` both order by the index fields, after `PARTITION` and `DATAAREAID`; with `reverse` only the index fields, descending; an `index` on a joined table is ignored, and an explicit `order by` wins | `pM71`, `pM78` to `pM83` |
| `div` in a `where` is the SQL division of the two integers (`NUMOFDAYS/?`) | `pM52`, `pM54` |
| `validTimeState(asOf)` compares `VALIDFROM<=? AND VALIDTO>=?`; `validTimeState(from, to)` is `NOT (VALIDTO<? OR VALIDFROM>?)`; the dates keep their type (`date` or `utcdatetime`) | `pM33`, `pM69`, `pM84` to `pM87` |
| Aggregates on no rows give 0; `avg` of integers is an integer, of reals a real; `maxof`/`minof` work on strings | `pM11` to `pM13`, `pM39` |
| String comparison in SQL ignores case and trailing blanks (`'alpha' == 'Alpha'`, `'Alpha ' == 'Alpha'`); `like` is case-insensitive, `_` and `%` are literal characters, `*` and `?` the wildcards | `pM14`, `pM15`, `pM66` |
| A field alone in a `where` is `> ''` for a string, `<> 0` for a number and `= 0` with `!`; an enum compares with its number | `pM16`, `pM17`, `pM67` |
| A `select` of fields leaves the other fields of the buffer empty, `RecId` included on a join (`T1.CUSTGROUP,T1.RECID,T2.NUMOFDAYS,T2.RECID`) | `pM24`, `pM50`, `pM62` |
| `update_recordset ... setting f = joined.Field`, `delete_from ... notexists join` and `insert_recordset ... group by` / `exists join` work; `RowCount()` is the affected rows, 0 when none | `pM25` to `pM28`, `pM47` to `pM49` |
| `update()` without `forupdate` is an error; `select forupdate` then `update()` and `delete()` work | `pM29`, `pM30` |
| A record of a table hierarchy selected into a buffer of the base table `is` the derived table the row has (`InstanceRelationType`) | `pM32` |
| A variable in a `where` is read when the statement starts, not on every row of a `while select` | `pM31` |
| A select expression inside a `where` is refused: `Table 'pt' is not found` | `pM44` |

## Things that end the AOS or the compiler

These run in the interpreter, but compiled code fails, so the cases have no value to compare:

| What | Case |
|---|---|
| A nested .NET type used through a variable ends the AOS process | `pB22` |
| An uninitialized `Nullable` passed to a method ends the class runner | `pC03` |
| `-a` on a `System.Double` ends the AOS process | `pK13` |
| `retry` in a `try` with `finally`, inside a `catch`, crashes the compiler | `pK31` |
| `mod` in a `where` clause ends with "Internal error number 173 in script", compiled and interpreted alike: the kernel has no SQL for it | `pM53` |

## Where the interpreter differs on purpose

- **Values compiled X++ loses** (`KnownDifferences` in `ConformanceTests.cs`): a generic dictionary declared with `var` (`pB82`), an implicit operator on assignment (`pC07`), and a `DateTime` assigned to an utcdatetime (`pB24`). The interpreter keeps the value.
- **Code the compiler refuses and the interpreter runs** (`AcceptedByTheInterpreter`): 10 cases, such as `System.Math::PI`, `var` with a nested enumerator, `"d=" + dateTime` and `Thread::CurrentThread.CurrentCulture`. A script that runs doesn't always compile in a class.
