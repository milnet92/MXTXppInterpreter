<h1 align="center">(MXT) X++ Interpreter</h1>

This tool allows you to write, execute and debug X++ code directly in the browser, without the need to compile or wait for service restart.

## Getting started
1. Download and install the Deployable Package from the releases. You can also clone the repo and compile the model by yourself.
2. Navigate to https://[YOU_ENVIRONMENT]/?mi=MXTXppInterpreterRunner and start writing!

# Motivation
Every time we want to test a small piece of code, we need to modify the source, compile and wait for all the required services to restart. This, depending on the VM configuration adds up to lots of wasted minutes just... waiting.

# Interpreter
This tool generates bytecode that is later interpreted directly during execution. The instructions that are interpreted are executed in native code using a series of proxies written in X++. That allows to simulate the execution *almost* exactly as native code would do it, without the need to compile.
<p align="center"><img src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/b3c06d47-7695-4d55-983c-f090ea44b511"/></p>

### Code editor
<p>The editor will highlight the reserved words and will indicate you which statement is being executed when debugging. It will also tell you if your code has any syntax error.</p>
<p>Some keyboard shortcuts are implemented to easly execute, insert a breakpoint, step over and continue debugging. These can be found as standard shorcuts by Right click > View shorcuts on the editor.</p>
<img src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/df039e8f-0c22-4aaa-9e78-d551327194d4" width="700"/>

<p>The variable inspector will allow you to take a look to the variables that are currently on scope and will let you modify the values for primitive types.</p>
<img src="https://github.com/milnet92/MXTXppInterpreter/assets/10449294/e05ca5f6-056b-4cb9-ad70-91fc61ace814" width="800"/>

# Use cases
* Execute and modify scripts
* Experiment with unknown or new functionallity
* Test pieces of your code rapidly

## Limitations
* Macros usage and declarations are not supported
* Function and class declarations are not supported
* *try*, *catch*, *finally* statements are not implemented

 > **_IMPORTANT:_** It is **not recommended** for Production escenarios as this tool allows you to execute *any code* on the environment.
