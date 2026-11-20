# Calling scripts from outside D365FO: MXppToolsGatewayScript

A small service on a development environment that runs X++ script text from outside the browser: a whole text, or a function of a text with arguments. The caller gets the infolog (info, warning and error messages, also those of standard code the script calls) and the value of `return`.

It is a separate package (`Metadata/MXppToolsGatewayScript`) that uses only the public class `MXTXppCoreScript` of MXppTools. It needs no `InternalsVisibleTo` entry in the descriptor of MXppTools.

**This package depends on #81** (`MXTXppCoreScript`, see `Docs/CallingScripts.md`). It does not compile without it.

## Setup

1. Register an app in Entra ID and create a client secret.
2. In D365FO, System administration > Setup > Microsoft Entra ID applications, add the client id and map it to a user. The user needs the privilege **MXGSExecuteXpp** and, for logging, the rights of the script editor (system administrator on a development environment).
3. Build the packages `MXppTools` and `MXppToolsGatewayScript`, and restart IIS so that the AOS loads the new assemblies.

## The request

```
POST https://<aos-host>/api/services/MXGSServiceGroup/MXGSService/execute
Authorization: Bearer <token>
Content-Type: application/json
```

The token is an Entra client-credentials token for the resource `https://<aos-host>`:

```
POST https://login.microsoftonline.com/<tenant>/oauth2/v2.0/token
grant_type=client_credentials&client_id=<client id>&client_secret=<secret>&scope=https://<aos-host>/.default
```

On a one-box environment the host name may point to another machine in DNS. Connect to the IP address and keep the host name in the `Host` header and for TLS, for example with `curl --resolve <aos-host>:443:<ip> -k ...`.

Body, all fields except `Code` are optional:

| Field | Meaning |
|---|---|
| `Code` | The script text |
| `FunctionName` | A function of the text to call. Empty: the whole text runs as the body of a function |
| `Arguments` | List of strings, up to 8, converted to the parameter types of the function |
| `DryRun` | `true`: the call runs between `ttsbegin` and `ttsabort`, nothing is committed |
| `NoLog` | `true`: no row in `MXTXppInterpreterScriptExecutionLog`. Logging is the default and runs the role check of the script editor |

Response:

| Field | Meaning |
|---|---|
| `Success` | `true` when the call ran to its end |
| `Error` | Short text when it did not |
| `Messages` | The infolog in order, each with `Type` (`Info`, `Warning`, `Error`, `ParseError`) and `Text` |
| `Result` | What the function or the text returned, as text |

## Examples

### Call a function with arguments

Script text:

```
str describe(str _prefix, str _name)
{
    info("describe called for " + _name);
    warning("this is a warning");
    return _prefix + ": " + _name;
}
```

Request body:

```json
{
  "_request": {
    "Code": "str describe(str _prefix, str _name)\n{\n    info(\"describe called for \" + _name);\n    warning(\"this is a warning\");\n    return _prefix + \": \" + _name;\n}\n",
    "FunctionName": "describe",
    "Arguments": [ "Hello", "World" ],
    "DryRun": true,
    "NoLog": true
  }
}
```

Response:

```json
{
  "Success": true,
  "Error": "",
  "Messages": [
    { "Type": "Info", "Text": "describe called for World" },
    { "Type": "Warning", "Text": "this is a warning" }
  ],
  "Result": "Hello: World"
}
```

### Run a whole text

Script text, with a standard table method that posts its own message:

```
CustGroup custGroup;
info("whole text runs as a function body");
custGroup.validateWrite();
return "finished";
```

Request body:

```json
{
  "_request": {
    "Code": "CustGroup custGroup;\ninfo(\"whole text runs as a function body\");\ncustGroup.validateWrite();\nreturn \"finished\";\n",
    "DryRun": true
  }
}
```

Response:

```json
{
  "Success": true,
  "Error": "",
  "Messages": [
    { "Type": "Info", "Text": "whole text runs as a function body" },
    { "Type": "Warning", "Text": "Field 'Customer group' must be filled in." }
  ],
  "Result": "finished"
}
```

### An error in the script

`FunctionName` is `failing`, the text is `str failing() { info("before the error"); throw error("something went wrong"); }`:

```json
{
  "Success": false,
  "Error": "X++ error exception (see messages).",
  "Messages": [
    { "Type": "Info", "Text": "before the error" },
    { "Type": "Error", "Text": "something went wrong" }
  ],
  "Result": ""
}
```

### A syntax error

```json
{
  "Success": false,
  "Error": "Parse failed.",
  "Messages": [
    { "Type": "ParseError", "Text": "Line 3, position 9: Syntax error. Token ; [Semicolon] was not expected." }
  ],
  "Result": ""
}
```

(The line is 2 higher than in the file when the whole text runs, see the limits.)

### From PowerShell

```powershell
$aos = "https://<aos-host>"; $tenant = "<tenant>"; $clientId = "<client id>"; $secret = "<secret>"
$token = (Invoke-RestMethod -Method Post -Uri "https://login.microsoftonline.com/$tenant/oauth2/v2.0/token" -Body @{
    grant_type = 'client_credentials'; client_id = $clientId; client_secret = $secret; scope = "$aos/.default" }).access_token

$body = @{ _request = @{ Code = 'info("hi"); return "x";'; DryRun = $true } } | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri "$aos/api/services/MXGSServiceGroup/MXGSService/execute" `
    -Headers @{ Authorization = "Bearer $token" } -ContentType 'application/json' -Body $body
```

## Limits

- **Development environments only.** Whoever has `MXGSExecuteXpp` can run any X++ and .NET code as the mapped user, and a call with `NoLog` leaves no trace in the execution log.
- No loop limit: the interpreter parameters of the editor don't apply to a call of `MXTXppCoreScript`.
- Arguments are strings. Records and objects can't be passed from outside.
- When the whole text runs, it is wrapped in a function, so the line numbers of parse errors are 2 higher than in the file. With `FunctionName` the lines are right.