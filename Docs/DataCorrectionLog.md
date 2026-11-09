# Data correction log

The main reason to run the interpreter in PROD is data correction: someone changed records, and a script has to change them back. A script can save each record to the table `MXTDataCorrectionLog` before it changes or deletes it, and restore it later, with its original RecId. Using the log is optional.

## The table

| Field | What |
|---|---|
| `RefTableId`, `RefRecId` | The record that was saved |
| `RefDataAreaId` | Its company. Empty for a shared table |
| `Name` | Free text that groups the rows of one correction, for example the ticket number |
| `RecData` | The whole record, in a container |
| `RecValueStr1`, `RecValueInt1`, `RecValueDate1`, `RecValueDateTime1` | Extra values for the script to use |

`CreatedBy` and `CreatedDateTime` are filled in automatically. The table is shared, so there is one log for all companies.

## Save a record before you change it

```
CustGroup custGroup;
MXTDataCorrectionLog log;

ttsbegin;
select forupdate custGroup where custGroup.CustGroup == '10';

log = MXTDataCorrectionLog::logRecord(custGroup, "Ticket 1234");
log.RecValueStr1 = custGroup.Name;
log.update();

custGroup.Name = 'New name';
custGroup.update();
ttscommit;
```

The same call goes before a `delete()`. `logRecord` fills in the table, the RecId and the company. To save it yourself:

```
log.RefTableId = custGroup.TableId;
log.RefRecId = custGroup.RecId;
log.Name = "Ticket 1234";
log.RecData = [custGroup];
log.insert();
```

The log row is written in the transaction of the script, so a `ttsabort` removes it together with the change.

## Undo an update

Read the old values from the container, and write back the fields you want:

```
CustGroup custGroup, before;
MXTDataCorrectionLog log;

while select log where log.Name == "Ticket 1234"
    && log.RefTableId == tableNum(CustGroup)
{
    before = conPeek(log.RecData, 1);

    ttsbegin;
    select forupdate custGroup where custGroup.RecId == log.RefRecId;
    custGroup.Name = before.Name;
    custGroup.update();
    ttscommit;
}
```

## Restore a deleted record

```
MXTDataCorrectionLog log;

ttsbegin;
while select log where log.Name == "Ticket 1234"
{
    log.restoreRecord();
}
ttscommit;
```

`restoreRecord` inserts the record in its original company, gives it back its RecId, and returns it, so records that refer to it are valid again. It uses `doInsert`, so the `insert` method of the table doesn't run. `CreatedBy`, `CreatedDateTime` and the modified fields get the values of the restore.

If the `insert` method has to run, do the steps yourself:

```
CustGroup custGroup;

custGroup.data(conPeek(log.RecData, 1));
custGroup.insert();
MXTDataCorrectionLog::restoreRecId(custGroup, log.RefRecId);
```

`restoreRecId` updates `RECID` in SQL, in the transaction of the caller, and reads the record again. It stops with an error if the old RecId already exists in that table, in any company, and needs the same rights as running a script.

## Try a correction first

A switch at the end of the transaction lets you run a script once, read its messages, and save nothing:

```
boolean dryRun = true;

ttsbegin;

// the correction

if (dryRun)
{
    ttsabort;
    warning('Dry run: nothing was saved');
}
else
{
    ttscommit;
}
```

Messages and counters stay after the `ttsabort`, and the rows of the log go back with the changes. Only the database goes back, so a call to another system stays done. Set `dryRun` to `false` for the real run.

## Copy values back from a restored database

After a point-in-time restore to another environment, a script can read the old values from that database and write them to the live table. A record has the same RecId in both, so the RecId connects them.

```
using System.Data.SqlClient;

str connectionString = 'Data Source=localhost;Initial Catalog=AxDB;Integrated Security=True';
str sql = "SELECT RECID, DESIGNNAME, VALUE FROM SYSLASTVALUE WHERE USERID = 'Admin'";
SysLastValue current;
int updated;

using (var connection = new SqlConnection(connectionString))
{
    connection.Open();

    var command = connection.CreateCommand();
    command.CommandText = sql;
    var reader = command.ExecuteReader();

    ttsbegin;

    while (reader.Read())
    {
        RecId recId = reader.GetInt64(0);
        str restoredName = reader.GetString(1);
        container restoredValue = conNull();

        if (!reader.IsDBNull(2))
        {
            System.Byte[] bytes = reader.GetValue(2);
            restoredValue = base64str2con(System.Convert::ToBase64String(bytes));
        }

        select forupdate current
            where current.RecId == recId;

        if (current.RecId && (current.DesignName != restoredName || current.Value != restoredValue))
        {
            MXTDataCorrectionLog::logRecord(current, 'Ticket 1234');

            current.DesignName = restoredName;
            current.Value = restoredValue;
            current.update();
            updated++;
        }
    }

    ttscommit;
}

info(strFmt('%1 records updated', updated));
```

As written it reads the local database, so it finds nothing to change. For a restored database, use its connection string: `Server=<server>;Database=<database>;User Id=<user>;Password=<password>;Encrypt=True`.

**A container field** is a `varbinary` column, in the format of `Global::base64str2con`, so its bytes convert in one line. For a table with a container field `ValueCon`, selected as the third column of the query, the code inside the loop is:

```
container restoredCon = conNull();

if (!reader.IsDBNull(2))
{
    System.Byte[] bytes = reader.GetValue(2);
    restoredCon = base64str2con(System.Convert::ToBase64String(bytes));
}

select forupdate current
    where current.RecId == recId;

current.ValueCon = restoredCon;
current.update();
```

An empty container is `NULL` in SQL, so test `IsDBNull` first. A memo column can be `NULL` too. Nested containers and records come back as they were saved, and `conPeek` reads a record from it.

| Column | Read it with |
|---|---|
| `str`, memo | `GetString` |
| `int`, enum | `GetInt32` |
| `int64`, RecId | `GetInt64` |
| `real` | `GetDecimal` |
| `date`, `utcdatetime` | `GetDateTime` |
| `guid` | `GetGuid` |
| `container` | `GetValue`, then `base64str2con` |

- **Column names** are the field names in capitals. Add `DATAAREAID = 'usmf'` for a company table, and run the script in that company, because `select forupdate` only sees the current one.
- **Network:** the AOS has to reach that SQL server, so its firewall must allow the address of the AOS.
- **Passwords:** the source of every script is saved in the execution log, including a password written in it. Use a temporary login, and don't save the script in the repository with the password.

To find the address to allow, run this on the environment that does the correction:

```
System.Net.WebClient client = new System.Net.WebClient();
str address = client.DownloadString('https://api.ipify.org');
info(strFmt('Public address of this AOS: %1', address));
```

It asks a public service which address the request came from, for the AOS that runs your session, so an environment with several AOS servers can need more than one rule. A SQL server in the same network sees a private address instead.

## Load corrections from an Excel file

A script can ask for an Excel file, find the record of each row by a key, and update it. This sample uses the class `DEVFileReaderExcel` of the `DEVCommon` model in [XppTools](https://github.com/TrudAX/XppTools), so that model has to be installed. The first row of the sheet holds the column names:

| CustGroup | Name |
|---|---|
| 10 | Wholesale customers |
| 20 | Retail customers |

```
FileUploadTemporaryStorageResult upload = File::GetFileFromUser();

if (!upload || !upload.getUploadStatus())
{
    throw error('No file was uploaded');
}

DEVFileReaderExcel reader = new DEVFileReaderExcel();
reader.openFile(upload.openResult());
reader.readHeaderRow();
upload.deleteResult();

CustGroup custGroup;
int updated, notFound;

ttsbegin;

while (reader.readNextRow())
{
    str groupId = reader.getStringByName('CustGroup');
    str newName = reader.getStringByName('Name');

    select forupdate custGroup
        where custGroup.CustGroup == groupId;

    if (!custGroup)
    {
        warning(strFmt('Row %1: customer group %2 not found', reader.getCurRow(), groupId));
        notFound++;
    }
    else if (custGroup.Name != newName)
    {
        MXTDataCorrectionLog::logRecord(custGroup, 'Ticket 1234');

        custGroup.Name = newName;
        custGroup.update();
        updated++;
    }
}

ttscommit;

info(strFmt('%1 records updated, %2 not found', updated, notFound));
```

The dialog uploads the file as soon as it is selected, and `Cancel` ends the script with the error. `openFile` reads the whole sheet into memory, so the temporary file can be deleted straight away. To undo the corrections, use the log name as in [Undo an update](#undo-an-update).

| Column | Read it with |
|---|---|
| `str`, memo | `getStringByName` |
| `int`, `int64` | `getIntByName` |
| `real` | `getRealByName` |
| `date` | `getDateByName` |
| `utcdatetime` | `getDateTimeByName` |
| enum | `getEnumByName('Status', enumNum(CustVendorBlocked))` |

- **One transaction:** an error, such as a column name that is not in the file, stops the script and nothing is saved. A key that isn't found is only a warning, so the other rows are saved.
- **Number keys:** `getStringByName` also reads a key that Excel stores as a number, as `10` and not `10.00`.
- **Empty cells** come back as an empty string, so this sample would clear `Name`. Add a check when an empty cell means "leave it as it is".
- **The end of the data** is the first row with nothing in its first three columns, so a blank row in the middle ends the file.
- **Company:** `select forupdate` only sees the current company, so run the script in the company of the data.
- **It needs a browser:** the file dialog waits for someone to select a file.

## Viewing and cleaning the log

The form is in the **Links** section of the workspace, under **Data correction log**, and shows every row except `RecData`. Its **Clean** button asks how many days to keep and deletes the older rows. To give access to someone who isn't a system administrator, assign the privilege `MXTDataCorrectionLogMaintain`.

A script can remove the rows of one correction when it is finished:

```
delete_from log where log.Name == "Ticket 1234";
```
