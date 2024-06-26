<h1 align="center">MXT - X++ Interpreter (preview)</h1>

 > **_IMPORTANT:_** It is **not recommended** for Production escenarios as this tool allows you to execute code without guarantee.

This tool allows you to write, execute and debug X++ code directly in the browser, without the need to compile or wait for service restart.

## Getting started
1. Clone the repo
2. Build the **MXppTools** package included and apply it to your environment
3. Navigate to https://[YOU_ENVIRONMENT]/?mi=**MXTXppInterpreterRunner** and start writing your code

# Interpreter
This tool generates bytecode that is later interpreted directly during execution. The instructions that are interpreted are executed in native code using a series of proxies written in X++. That allows to simulate the execution *almost* exactly as native code would do it, without the need to compile.

<p align="center"><img src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/d4d0eff0-0320-43f7-a2d8-0300b601e84a"</img></p>

### Code editor
<p>The editor will highlight the reserved words and will indicate you which statement is being executed when debugging. It will also tell you if your code has any syntax error.</p>

<p>Some keyboard shortcuts are implemented to easly execute, insert a breakpoint, step over and continue debugging. These can be found as standard shorcuts by Right click > View shorcuts on the editor.</p>
<img width="400" src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/7440d562-db83-4972-b07b-97b9acdd05c9"/>

<p>The variable inspector will allow you to take a look to the variables that are currently on scope and will let you modify the values for primitive types.</p>
<img width="700" src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/66b815ea-169a-4366-a1f0-1cac12b39fa7"/>

### Script repository
<p>You can save X++ scripts into the built-in repository to later execute them.</p>
<img width="400" src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/dab3be52-5c99-4b57-932d-d298771793c3"/>

# Use cases
* Execute, modify and save X++ scripts
* Experiment with unknown or new functionality
* Test pieces of your code
* Get metada information quickly (class and table ids, label texts, enum values...)

## Limitations
* **Macros** usage and declarations are not supported
* **Class declarations** are not supported
* **try**, **catch** and **finally** statements are not implemented
* **.NET namespaces** cannot be referenced
