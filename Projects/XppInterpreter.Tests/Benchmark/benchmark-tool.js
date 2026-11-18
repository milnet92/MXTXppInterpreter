// Runs benchmark.txt on a real D365 environment: every snippet compiled by the X++ compiler and through the interpreter.
//
//   node benchmark-tool.js generate <axClassFolder> [filter]   writes the classes DEVXppBenchmark and DEVXppBenchmarkHelper
//   node benchmark-tool.js report [--save file] [--compare file]   shows the last results, and how they changed
//
// Then: compile the model the folder belongs to, restart the AOS and open /?mi=SysClassRunner&cls=DEVXppBenchmark&cmp=USMF.
// The results go to C:\Windows\Temp\DEVXppBenchmark.txt. Scripts\RunBenchmark.ps1 does all of it.
const fs = require('fs');
const path = require('path');

const SNIPPETS = path.join(__dirname, 'benchmark.txt');
const RESULTS = 'C:\\Windows\\Temp\\DEVXppBenchmark.txt';
const RUNS = 2;

function readCases() {
    const text = fs.readFileSync(SNIPPETS, 'utf8').replace(/\r/g, '');

    return text.slice(text.indexOf('\n### ') + 1).split(/^### /m).slice(1).map(block => {
        const lines = block.trimEnd().split('\n');

        return { name: lines[0].trim(), code: lines.slice(1).join('\n') };
    });
}

const method = (name, source) =>
    `\t\t\t<Method>\n\t\t\t\t<Name>${name}</Name>\n\t\t\t\t<Source><![CDATA[\n${source}\n]]></Source>\n\t\t\t</Method>\n`;

const axClass = (name, methods) => `<?xml version="1.0" encoding="utf-8"?>
<AxClass xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
\t<Name>${name}</Name>
\t<SourceCode>
\t\t<Declaration><![CDATA[
public class ${name}
{
}
]]></Declaration>
\t\t<Methods>
${methods}\t\t</Methods>
\t</SourceCode>
</AxClass>`;

const write = (folder, name, content) => {
    fs.writeFileSync(path.join(folder, `${name}.xml`), content.replace(/\n/g, '\r\n'));
    console.log(`written ${name}.xml`);
};

// What the snippets call. It is a class of its own: the class that is running main is not a fair target.
function helper() {
    let methods = '';

    methods += method('one', `    public static int one()\n    {\n        return 1;\n    }`);
    methods += method('addOne', `    public static int addOne(int _value, str _text)\n    {\n        return 1;\n    }`);
    methods += method('two', `    public int two()\n    {\n        return 2;\n    }`);

    return axClass('DEVXppBenchmarkHelper', methods);
}

// Every case runs RUNS times each way and the line is written when it ends, so a case that fails doesn't lose the others.
// A line is: name, the compiled times, the interpreted times, both results. All separated by tabs, times in milliseconds.
const main = `    public static void main(Args _args)
    {
        DictClass dc = new DictClass(classNum(DEVXppBenchmark));
        DictClass executor = new DictClass(className2Id('MXTXppInterpreterExecutor'));
        str path = @"${RESULTS}";
        System.Exception ex;

        System.IO.File::WriteAllText(path, "");

        for (int i = 1; i <= dc.staticMethodCnt(); i++)
        {
            str name = dc.staticMethod(i);

            if (strLen(name) > 4 && subStr(name, 1, 1) == 'b' && subStr(name, 2, 2) >= '00' && subStr(name, 2, 2) <= '99')
            {
                str line = name;

                try
                {
                    str source = System.Text.Encoding::UTF8.GetString(System.Convert::FromBase64String(dc.callStatic('s' + subStr(name, 2, strLen(name)))));
                    str compiledResult, interpretedResult;

                    for (int run = 1; run <= ${RUNS}; run++)
                    {
                        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch::StartNew();
                        compiledResult = dc.callStatic(name);
                        watch.Stop();
                        line += '\\t' + DEVXppBenchmark::milliseconds(watch);
                    }

                    for (int run = 1; run <= ${RUNS}; run++)
                    {
                        System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch::StartNew();
                        anytype value = executor.callStatic('evaluate', source);
                        watch.Stop();
                        interpretedResult = strFmt('%1', value);
                        line += '\\t' + DEVXppBenchmark::milliseconds(watch);
                    }

                    line += '\\t' + compiledResult + '\\t' + interpretedResult;
                }
                catch (ex)
                {
                    line += '\\tEXCEPTION: ' + strReplace(strReplace(ex.ToString(), '\\r', ' '), '\\n', ' ');
                }

                System.IO.File::AppendAllText(path, line + "\\r\\n");
            }
        }

        System.IO.File::AppendAllText(path, "FINISHED\\r\\n");
        info('DEVXppBenchmark finished');
    }`;

const milliseconds = `    private static str milliseconds(System.Diagnostics.Stopwatch _watch)
    {
        int64 ticks = _watch.get_ElapsedTicks();
        int64 frequency = System.Diagnostics.Stopwatch::Frequency;
        real elapsed = ticks;

        return num2Str(elapsed * 1000 / frequency, 0, 3, 1, 0);
    }`;

function generate(folder, filter) {
    const cases = readCases().filter(c => !filter || c.name.startsWith(filter));
    let methods = method('main', main) + method('milliseconds', milliseconds);

    for (const c of cases) {
        const body = c.code.split('\n').map(l => '        ' + l).join('\n');
        const encoded = Buffer.from(c.code, 'utf8').toString('base64');

        methods += method(c.name, `    public static str ${c.name}()\n    {\n${body}\n    }`);

        // The same snippet as a script: the source is base64 so nothing needs escaping
        methods += method('s' + c.name.slice(1), `    public static str s${c.name.slice(1)}()\n    {\n        return "${encoded}";\n    }`);
    }

    write(folder, 'DEVXppBenchmarkHelper', helper());
    write(folder, 'DEVXppBenchmark', axClass('DEVXppBenchmark', methods));
    console.log(`${cases.length} cases`);
}

function readResults(file) {
    const results = new Map();

    for (const line of fs.readFileSync(file, 'utf8').replace(/\r/g, '').split('\n')) {
        const parts = line.split('\t');

        if (parts.length === 2 * RUNS + 3) {
            const times = parts.slice(1, 2 * RUNS + 1).map(Number);

            // The first run of each includes what is compiled or cached on first use, so the best one is compared
            results.set(parts[0], {
                compiled: Math.min(...times.slice(0, RUNS)),
                interpreted: Math.min(...times.slice(RUNS)),
                same: parts[2 * RUNS + 1] === parts[2 * RUNS + 2],
            });
        } else if (parts.length > 1) {
            results.set(parts[0], { error: parts.slice(1).join(' ').slice(0, 300) });
        }
    }

    return results;
}

function report(options) {
    const results = readResults(RESULTS);
    const before = options.compare ? readResults(options.compare) : null;
    const pad = (text, width) => String(text).padStart(width);
    const ms = value => value.toFixed(value < 10 ? 2 : 0);

    console.log(`${'case'.padEnd(32)}${pad('compiled ms', 12)}${pad('interpreted ms', 16)}${pad('times slower', 14)}` + (before ? `${pad('before ms', 12)}${pad('faster', 9)}` : ''));

    for (const [name, result] of results) {
        if (result.error) {
            console.log(`${name.padEnd(32)}  ${result.error}`);
            continue;
        }

        let line = `${name.padEnd(32)}${pad(ms(result.compiled), 12)}${pad(ms(result.interpreted), 16)}${pad((result.interpreted / Math.max(result.compiled, 0.001)).toFixed(0), 14)}`;
        const old = before && before.get(name);

        if (old && !old.error) {
            line += `${pad(ms(old.interpreted), 12)}${pad((old.interpreted / result.interpreted).toFixed(1) + 'x', 9)}`;
        }

        console.log(line + (result.same ? '' : '   DIFFERENT RESULTS'));
    }

    if (options.save) {
        fs.copyFileSync(RESULTS, options.save);
        console.log(`\nsaved as ${options.save}`);
    }
}

const [,, command, ...rest] = process.argv;

switch (command) {
    case 'generate':
        generate(rest[0], rest[1]);
        break;
    case 'report': {
        const option = name => rest.includes(name) ? rest[rest.indexOf(name) + 1] : null;

        report({ save: option('--save'), compare: option('--compare') });
        break;
    }
    default:
        console.log('Usage: see the top of this file');
}
