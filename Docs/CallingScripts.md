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
- **A function uses** its parameters, its own variables and the other functions of the script. A variable declared outside the functions doesn't exist in a call: reading it fails with `Variable total was not declared.`
- **Names** of functions are found in any case, as in X++.

## Arguments and the return value

`call` takes the name of the function and up to 8 arguments. A parameter with a default value can be left out.

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
    boolean exported = MyExportSetup::script(this.TableId).call('isExported', this);

    super();

    if (exported)
    {
        MyExportLog::add(this);
    }
}
```

`MyExportSetup::script` is your own method. It finds the setup record and returns `MXTXppScript::newFromSource(setup.ScriptText)`, or keeps the object in a static `Map` by table: X++ keeps a static field for each session.

## Checking the text when it is saved

```
public boolean validateWrite()
{
    boolean ret = super();

    ret = ret && MXTXppScript::newFromSource(this.ScriptText).validate('isExported');

    return ret;
}
```

`validate` reports each error of the text as `checkFailed` does, in the form `Line 1, position 10: Syntax error...`, and then a missing function. `isValid`, `parseErrors` and `hasFunction` give the same answers without messages. `newFromSource` never throws: a text with errors gives a script whose `call` stops with an error.

## What a call does

- **Transaction:** the function runs in the transaction of the caller. Its own `ttsbegin` and `ttscommit` nest in it, and a `ttsabort` of the caller takes back what the function wrote. Outside a transaction, a function that commits has saved its data.
- **Errors:** an exception of the function reaches the caller. `throw error("...")` is caught with `catch (Exception::Error)`, a warning with `Exception::Warning`, and a .NET exception with `Exception::CLRError`. A missing function, a wrong number of arguments and a text with errors stop the call with an X++ error.
- **No editor settings:** the parameters of the interpreter don't apply. There is no rollback at the end, no limit for loops and no debugger.
- **No log:** a call writes nothing to the execution log.
- **Security:** a call checks no role. The function runs as the user of the calling code, with full X++ and .NET. Whoever can change the text decides what runs in the sessions of other users, so give the table that holds it its own privilege, and remember data entities and data management when you do.

## Speed

Measured on 2026-11-12 from a compiled class of another model: platform 7.0.8199.55 (10.0.49), a one-box development VM, the Debug build of `XppInterpreter.dll`. [Performance.md](Performance.md) says what the statements of a script cost.

| Call | Script | Compiled X++ |
|---|---|---|
| A function that adds 1 to its argument | 21 µs | 0.07 µs |
| `isExported` above, on a changed record | 50 µs | 7 µs |
| `totalAmount` above, outside a transaction | 1.40 ms | 1.21 ms |
| `totalAmount` inside a transaction of the caller | 0.42 ms | not measured |
| `newFromSource` of a text the session already used | 19 µs | |
| The first use of a text in a session: parsing 3 to 5 statements | 0.15 to 0.5 ms | |

- **19 µs of every call is the platform:** it opens a scope when code of one publisher calls a model of another one ([Performance.md](Performance.md), "What the platform charges"). The same call from code of the publisher of `MXppTools` takes 2 µs. `isValid` and `hasFunction` cost those 19 µs too, so don't ask before every call.
- **Keep the script object** where the function is called often: `newFromSource` and `call` are two entries, 40 µs. With the object in a variable or a static field it is one.
- **The cache is for each session,** because X++ keeps static fields for each session: every session parses a text once. It holds 500 texts and is emptied when it is full.
- **Sessions at the same time:** 6 sessions that each called two functions of one text 20,000 times got no wrong answer.

## Not verified

- A call in a batch task.
- Other platform versions than 7.0.8199.55.
