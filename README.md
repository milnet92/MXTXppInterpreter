<h1 align="center">MXT - X++ Interpreter (preview)</h1>

 > **_IMPORTANT:_** It is **not recommended** for Production escenarios as this tool allows you to execute code without guarantee.

This tool allows you to write, execute and debug X++ code directly in the browser, without the need to compile or wait for service restart.

## Getting started
1. Clone the repo
2. Build the **MXppTools** package included and apply it to your environment
3. Synchronize the database
4. Navigate to System Administration > Workspaces > **MXT - X++ Interpreter**

# Interpreter
This tool generates bytecode that is later interpreted directly during execution. The instructions that are interpreted are executed in native code using a series of proxies written in X++. That allows to simulate the execution *almost* exactly as native code would do it, without the need to compile.

![arquitecture](Assets/simple_arquitecture.png)

### Code editor
<p>The editor will highlight the reserved words and will indicate you which statement is being executed when debugging. It will also tell you if your code has any syntax error.</p>

<p>Some keyboard shortcuts are implemented to easly execute, insert a breakpoint, step over and continue debugging. These can be found as standard shorcuts by Right click > View shorcuts on the editor.</p>
![example](Assets/code_example.png)

<p>The variable inspector will allow you to take a look to the variables that are currently on scope and will let you modify the values for primitive types.</p>
![debugger](Assets/debugger.png)

### Script repository
<p>You can save X++ scripts into the built-in repository to later execute them.</p>
![repo](Assets/script_repo.png)

# Use cases
* Execute, modify and save X++ scripts
* Experiment with unknown or new functionality
* Test pieces of your code
* Get metada information quickly (class and table ids, label texts, enum values...)

## Limitations
* **Macros** usage and declarations are not supported
* **Class declarations** are not supported
* **retry** statement is not implemented
* **.NET namespaces** cannot be referenced
