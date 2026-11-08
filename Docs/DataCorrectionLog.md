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

## Viewing and cleaning the log

The form is in the **Links** section of the workspace, under **Data correction log**, and shows every row except `RecData`. Its **Clean** button asks how many days to keep and deletes the older rows. To give access to someone who isn't a system administrator, assign the privilege `MXTDataCorrectionLogMaintain`.

A script can remove the rows of one correction when it is finished:

```
delete_from log where log.Name == "Ticket 1234";
```
