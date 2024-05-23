<h1 align="center">MXT - X++ Interpreter (preview)</h1>

This tool allows you to write, execute and debug X++ code directly in the browser, without the need to compile or wait for service restart.

## Getting started
1. Download and install the Deployable Package from the releases. You can also clone the repo and compile the model by yourself.
2. Navigate to **https://[YOU_ENVIRONMENT]/?mi=MXTXppInterpreterRunner** and start writing!

# Motivation
Every time we want to execute a small piece of code for whatever reason, we need to modify the source, compile and wait for all the required services to restart. This, depending on the VM configuration adds up to lots of wasted minutes just... waiting.

# Interpreter
This tool generates bytecode that is later interpreted directly during execution. The instructions that are interpreted are executed in native code using a series of proxies written in X++. That allows to simulate the execution *almost* exactly as native code would do it, without the need to compile.

<p align="center"><img src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/d4d0eff0-0320-43f7-a2d8-0300b601e84a"</img></p>

### Code editor
<p>The editor will highlight the reserved words and will indicate you which statement is being executed when debugging. It will also tell you if your code has any syntax error.</p>

<p>Some keyboard shortcuts are implemented to easly execute, insert a breakpoint, step over and continue debugging. These can be found as standard shorcuts by Right click > View shorcuts on the editor.</p>
<img width="400" src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/7440d562-db83-4972-b07b-97b9acdd05c9"/>

<p>The variable inspector will allow you to take a look to the variables that are currently on scope and will let you modify the values for primitive types.</p>
<img width="700" src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/6d4e47ea-1593-4aa3-8afc-b4b6cfb5e6d3"/>

# Use cases
* Execute, modify and save X++ scripts
* Experiment with unknown or new functionality
* Test pieces of your code
* Get metada information quickly (class and table ids, label texts, enum values...)

## Limitations
* **Macros** usage and declarations are not supported
* **Function declarations** and **class declarations** are not supported
* **try**, **catch** and **finally** statements are not implemented
* **.NET namespaces** cannot be referenced

 > **_IMPORTANT:_** It is **not recommended** for Production escenarios as this tool allows you to execute *any code* on the environment.
