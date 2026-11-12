# Calling a script from X++

Compiled X++ can call a function of a script, pass it records, objects and values, and use what it returns. The script is a text, for example a field of a setup table, so the logic can change without a deployment. The class is `MXTXppScript`.

```
MXTXppScript script = MXTXppScript::newFromSource(setupTable.ScriptText);
Amount       amount = script.call('totalAmount', salesTable);
```

## The script

The script declares functions, and X++ calls one of them by name:

```
real totalAmount(SalesTable _salesTable)
{
    SalesLine salesLine;

    select sum(LineAmount) from salesLine
        where salesLine.SalesId == _salesTable.SalesId;

    return salesLine.LineAmount;
}

// Runs only in the editor, as the test of the function
info(strFmt("%1", totalAmount(SalesTable::find('000002'))));
```

- **The statements outside the functions** never run in a call. In the editor they run, so they are the place for test code.
- **A function uses** its parameters, its own variables and the other functions of the script. A variable declared outside the functions doesn't exist in a call. `call` refuses a function that uses one, itself or through a function it calls, and `validate` reports it when the text is saved.
- **Names** of functions are found in any case, as in X++.

## Arguments and the return value

`call` takes the name of the function and up to 8 arguments. A parameter with a default value can be left out. `MXTXppScript::callFromSource(text, 'totalAmount', salesTable)` does the same without the object.

| Passed | What the function gets |
|---|---|
| A record, or an object such as a `Map` or a class instance | The one of the caller. What the function changes is changed for the caller |
| `int`, `int64`, `real`, `str`, `boolean`, `date`, `utcdatetime`, `guid`, an enum, a container | A copy. The caller keeps its value |

A function that fills a record of the caller:

```
void setDelivery(SalesTable _salesTable)
{
    _salesTable.DeliveryName = "AAA";
}
```

```
script.call('setDelivery', salesTable);
// salesTable.DeliveryName is now "AAA". Nothing is written to the database: the function didn't call update
```

- **The return value** is converted to the return type of the function, so `real total() { return 1; }` gives a `real`. The types of the table above, records and objects all come back. A `void` function gives nothing to assign.
- **Several results:** return a container, or pass a record or an object that the function fills.
- **An argument is converted** to the type of its parameter as an assignment would: an `int` for a `real` parameter becomes a `real`.
- **A parameter of a table type** takes only that table. Another record stops the call with the .NET error `Cannot convert from type 'CustGroup' to type 'SalesTable'.` A `Common` parameter takes any record, and the parser then knows only the fields of `Common`, such as `RecId`.

## A rule kept in setup

A setup table holds a script for each table, and the `update` method of the table asks it whether the record is exported:

```
boolean isExported(CustGroup _record)
{
    if (_record.orig().Name != _record.Name)
    {
        return true;
    }

    return false;
}
```

```
public void update()
{
    MyExportSetup setup = MyExportSetup::find(this.TableId);
    boolean       exported;

    if (setup.ScriptText)
    {
        exported = MXTXppScript::callFromSource(setup.ScriptText, 'isExported', this);
    }

    super();

    if (exported)
    {
        MyExportLog::add(this);
    }
}
```

`MyExportSetup` and `MyExportLog` are your own tables. `callFromSource` is `newFromSource` and `call` in one step: it takes the text, the name of the function and the same arguments.

## Checking the text when it is saved

```
public boolean validateWrite()
{
    boolean ret = super();

    ret = ret && MXTXppScript::newFromSource(this.ScriptText).validate('isExported');

    return ret;
}
```

`validate` reports as `checkFailed` does:

- **The errors of the text,** in the form `Line 1, position 10: Syntax error...`
- **A missing function:** `The script has no function 'isExported'.`
- **A function that can't be called:** `The function 'isExported' uses total, declared outside the functions of the script. In a call those variables don't exist.` It follows the functions that the function calls.

Without the name of a function, `validate` only checks the text. `isValid`, `parseErrors` and `hasFunction` give the same answers without messages. `newFromSource` never throws: a text with errors gives a script whose `call` stops with an error.

## What a call does

- **Transaction:** the function runs in the transaction of the caller. Its own `ttsbegin` and `ttscommit` nest in it, and a `ttsabort` of the caller takes back what the function wrote. Outside a transaction, a function that commits has saved its data.
- **Errors:** an exception of the function reaches the caller as the same statement in a compiled method would: see [Exceptions](#exceptions). A missing function, a wrong number of arguments and a text with errors stop the call with an X++ error.
- **No editor settings:** the parameters of the interpreter don't apply. There is no rollback at the end, no limit for loops and no debugger.
- **No log:** a call writes nothing to the execution log.
- **Security:** a call checks no role. The function runs as the user of the calling code, with full X++ and .NET. Whoever can change the text decides what runs in the sessions of other users, so give the table that holds it its own privilege, and remember data entities and data management when you do.

## Exceptions

A `throw` in a function behaves as the same `throw` in a compiled method that the caller called. This was checked on the AOS with 26 cases, each one run twice: with compiled X++ that throws, and with a script function that throws. The catch block that ran, the transaction level, the rows in the database, the messages and the company were the same in all of them.

| The function | In the caller |
|---|---|
| `throw error("...")`, no transaction | `catch (Exception::Error)` runs, with one message in the infolog |
| Throws inside a transaction of the caller | The transaction is aborted. The catch outside the transaction runs at level 0, and what the caller and the function inserted is gone |
| Throws, and the `try` of the caller is inside its transaction | That catch is skipped. The catch outside the transaction runs |
| Opens a transaction itself and throws | The transaction is aborted |
| Has its own `try` and `catch`, called outside a transaction | The catch of the function runs |
| Has its own `try` and `catch`, called inside a transaction | The catch of the function is skipped, as in a compiled method, and the catch of the caller outside the transaction runs |
| `throw Exception::UpdateConflict` | `catch (Exception::UpdateConflict)` runs. When its `try` is inside the transaction, in the caller or in the function, the transaction stays open |
| A duplicate key in `insert()` | `Exception::DuplicateKeyException`, which a catch inside the transaction gets |
| A .NET exception, such as `System.Int32::Parse("x")` | `catch (Exception::CLRError)` runs and the transaction is aborted. `CLRInterop::getLastException()` holds the `FormatException` as inner exception |
| Throws inside `changecompany` | The company is the one of the caller again |
| Calls a compiled method that throws | The exception of that method, with its one message |
| Has a `finally` block | It runs before the caller gets the exception |
| `throw warning("...")` | `catch (Exception::Warning)` |
| `ttsabort` | The transaction of the caller is aborted, and the call returns |

`retry` in the catch of the caller calls the function again, and a call after an exception runs as the first one.

**What `call` refuses is an X++ error:** a missing function, a wrong number of arguments, a text with errors, a function that uses a variable declared outside the functions. `catch (Exception::Error)` gets it, before any statement of the function ran.

**An error of the interpreter itself is a .NET exception.** An argument of a type that can't be converted stops the call with one. `catch (Exception::Error)` doesn't get it: `catch (Exception::CLRError)` or a catch without a type does. A transaction of the caller is aborted as with any other exception.

## Speed

Measured on 2026-11-12 from a compiled class of another model: platform 7.0.8199.55 (10.0.49), a one-box development VM, the Debug build of `XppInterpreter.dll`. [Performance.md](Performance.md) says what the statements of a script cost.

| Call | Script | Compiled X++ |
|---|---|---|
| A function that adds 1 to its argument, `call` on a kept object | 21 µs | 0.07 µs |
| The same with `callFromSource` | 23 µs | |
| The same with `newFromSource(...).call(...)` | 43 µs | |
| `isExported` above, on a changed record | 53 µs | 7 µs |
| `totalAmount` above, outside a transaction | 1.40 ms | 1.21 ms |
| `totalAmount` inside a transaction of the caller | 0.42 ms | not measured |
| `newFromSource` of a text the session already used | 19 µs | |
| The first use of a text in a session: parsing 3 to 5 statements | 0.15 to 0.5 ms | |

- **19 µs of every call is the platform:** it opens a scope when code of one publisher calls a model of another one ([Performance.md](Performance.md), "What the platform charges"). The same call from code of the publisher of `MXppTools` takes 2 µs. `isValid` and `hasFunction` cost those 19 µs too, so don't ask before every call.
- **One entry for each call:** `newFromSource(...).call(...)` enters the model twice. Keep the object, or use `callFromSource`, which finds the text in the cache and calls in one entry.
- **Where the 53 µs of `isExported` go:** 19 µs for the entry and 2 µs for the call. `orig()` takes 6 µs compiled, because it creates a record, and 18 µs in the function. A field read takes 3 to 5 µs.
- **The cache is for each session,** because X++ keeps static fields for each session: every session parses a text once. It holds 500 texts and is emptied when it is full.
- **Sessions at the same time:** 6 sessions that each called two functions of one text 20,000 times got no wrong answer.
- **Batch:** a batch task ran the same tests with the same results and the same times: 21 µs for the function that adds 1, 1.40 ms for `totalAmount`.

## Not verified

- Other platform versions than 7.0.8199.55.
