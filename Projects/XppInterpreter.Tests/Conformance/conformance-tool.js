// Runs conformance.txt on a real D365 environment, to compare the interpreter with the real X++ compiler.
//
//   node conformance-tool.js compiled <axClassFolder>     writes the snippets as an X++ class, for the real compiler
//   node conformance-tool.js interpreted <axClassFolder>  writes a class that runs the snippets through the interpreter
//   node conformance-tool.js compare                      shows where the last results differ from conformance.txt
//   node conformance-tool.js update                       writes the last results of a "compiled" run into conformance.txt
//
// Then: compile the model the folder belongs to, restart the AOS and open /?mi=SysClassRunner&cls=DEVXppProbe
// (or DEVXppConformance). The results go to C:\Windows\Temp\DEVXppProbe.txt or DEVXppConformance.txt.
// A snippet that kills the AOS is the last one in the file: mark it with "!!" in conformance.txt.
const fs = require('fs');
const path = require('path');

const GOLDEN = path.join(__dirname, 'conformance.txt');
const COMPILED_RESULTS = 'C:\\Windows\\Temp\\DEVXppProbe.txt';
const INTERPRETED_RESULTS = 'C:\\Windows\\Temp\\DEVXppConformance.txt';

// Same list as ConformanceTests.cs: where the interpreter is right on purpose
const KNOWN_DIFFERENCES = {
    pB82_genericDictionary: '1 true 1',
    pC07_implicitOperatorOnAssign: '2020',
    pB24_utcFromDateTime: '2020-01-02T03:04:05',
};

function readCases() {
    const text = fs.readFileSync(GOLDEN, 'utf8').replace(/\r/g, '');
    const start = text.indexOf('\n### ') + 1;

    return text.slice(start).split(/^### /m).slice(1).map(block => {
        const lines = block.trimEnd().split('\n');
        const title = lines[0].trim();
        const last = lines[lines.length - 1];
        const isResult = last === '=>' || last.startsWith('=> ') || last.startsWith('!! ');

        return {
            name: title.split(' ')[0],
            title,
            code: lines.slice(1, isResult ? -1 : undefined).join('\n'),
            expected: last.startsWith('=>') ? last.slice(3) : null,
            note: last.startsWith('!! ') ? last : null,
        };
    });
}

function readResults(file) {
    const results = new Map();
    let current = null;

    for (const line of fs.readFileSync(file, 'utf8').replace(/\r/g, '').split('\n')) {
        const match = line.match(/^(p[A-Z]\d+_\w+) => (.*)$/);

        if (match) {
            current = match[1];
            results.set(current, match[2]);
        } else if (current && line && line !== 'FINISHED') {
            results.set(current, results.get(current) + '\n' + line);
        }
    }

    return results;
}

const method = (name, source) =>
    `\t\t\t<Method>\n\t\t\t\t<Name>${name}</Name>\n\t\t\t\t<Source><![CDATA[\n${source}\n]]></Source>\n\t\t\t</Method>\n`;

const axClass = (name, declaration, methods) => `<?xml version="1.0" encoding="utf-8"?>
<AxClass xmlns:i="http://www.w3.org/2001/XMLSchema-instance">
\t<Name>${name}</Name>
\t<SourceCode>
\t\t<Declaration><![CDATA[
${declaration}
]]></Declaration>
\t\t<Methods>
${methods}\t\t</Methods>
\t</SourceCode>
</AxClass>`;

const write = (folder, name, content) => {
    fs.writeFileSync(path.join(folder, `${name}.xml`), content.replace(/\n/g, '\r\n'));
    console.log(`written ${name}.xml`);
};

// The helpers the snippets call
function helpers() {
    let methods = '';

    methods += method('nx', `    public static int nx()\n    {\n        counter++;\n        return counter;\n    }`);
    methods += method('make', `    public static DEVXppProbe make()\n    {\n        DEVXppProbe::nx();\n        return new DEVXppProbe();\n    }`);
    methods += method('show', `    public str show(int _value)\n    {\n        return strFmt("arg=%1 counter=%2", _value, counter);\n    }`);
    methods += method('fortyOne', `    public static int fortyOne()\n    {\n        return 41;\n    }`);
    methods += method('num', `    public int num()\n    {\n        return 41;\n    }`);
    methods += method('mapArg', `    public static str mapArg(CustVendTable _map)\n    {\n        return _map.AccountNum;\n    }`);
    methods += method('makeBuilder', `    public static System.Text.StringBuilder makeBuilder()\n    {\n        return new System.Text.StringBuilder('abc');\n    }`);
    methods += method('builderOf', `    public System.Text.StringBuilder builderOf()\n    {\n        return new System.Text.StringBuilder('inst');\n    }`);
    methods += method('readBuilder', `    public static str readBuilder(System.Text.StringBuilder _builder)\n    {\n        return _builder.ToString();\n    }`);
    methods += method('makeArray', `    public static System.String[] makeArray()\n    {\n        System.String[] ret = new System.String[2]();\n        ret.SetValue('y', 1);\n        return ret;\n    }`);
    methods += method('nullBuilder', `    public static System.Text.StringBuilder nullBuilder()\n    {\n        return null;\n    }`);
    methods += method('opt', `    public static str opt(int _number = 5, str _text = 'b')\n    {\n        return strFmt('%1 %2 %3 %4', prmIsDefault(_number), _number, prmIsDefault(_text), _text);\n    }`);
    methods += method('parmValue', `    public int parmValue(int _value = instanceValue)\n    {\n        if (!prmIsDefault(_value))\n        {\n            instanceValue = _value;\n        }\n\n        return instanceValue;\n    }`);

    return methods;
}

const probeDeclaration = `using System.IO;
using IO = System.IO;

public class DEVXppProbe
{
    public static int counter;
    public static int staticValue;
    public static System.Text.StringBuilder builder;
    int instanceValue;
}`;

// Calls every static method of a class and writes the results as it goes: a snippet can kill the AOS
const runnerLoop = (path, call) => `    public static void main(Args _args)
    {
        DictClass dc = new DictClass(classNum(${path.className}));
        str path = @"${path.file}";
        System.Exception ex;

        System.IO.File::WriteAllText(path, "");

        for (int i = 1; i <= dc.staticMethodCnt(); i++)
        {
            str name = dc.staticMethod(i);

            if (strLen(name) > 4 && subStr(name, 1, 1) == 'p' && subStr(name, 3, 2) >= '00' && subStr(name, 3, 2) <= '99')
            {
                str res;
                System.IO.File::AppendAllText(path, name + ' => ');
                try
                {
                    ${call}
                }
                catch (ex)
                {
                    res = 'EXCEPTION: ' + ex.ToString();
                }
                System.IO.File::AppendAllText(path, res + "\\r\\n");
            }
        }

        System.IO.File::AppendAllText(path, "FINISHED\\r\\n");
        info('${path.className} finished');
    }`;

function compiled(folder) {
    let methods = helpers();

    methods += method('main', runnerLoop({ className: 'DEVXppProbe', file: COMPILED_RESULTS }, 'res = dc.callStatic(name);'));

    for (const c of readCases().filter(c => !c.note || !c.note.startsWith('!! REJECTED') && !c.note.startsWith('!! CRASHES') && !c.note.startsWith('!! ABORTS'))) {
        const body = c.code.split('\n').map(l => '        ' + l).join('\n');

        methods += method(c.name, `    public static str ${c.name}()\n    {\n${body}\n    }`);
    }

    write(folder, 'DEVXppProbe', axClass('DEVXppProbe', probeDeclaration, methods));
}

// The same snippet as a script: the source is base64 so nothing needs escaping
function toScript(code) {
    const source = code
        .replace(/(?<![\w:.])counter\b/g, 'DEVXppProbe::counter')
        .replace(/(?<![\w:.])staticValue\b/g, 'DEVXppProbe::staticValue');

    return 'using System.IO; using IO = System.IO;\n' + source;
}

function interpreted(folder) {
    write(folder, 'DEVXppProbe', axClass('DEVXppProbe', probeDeclaration, helpers()));

    let methods = method('main', runnerLoop({ className: 'DEVXppConformance', file: INTERPRETED_RESULTS }, `DEVXppProbe::counter = 0;
                    DEVXppProbe::staticValue = 0;
                    str source = System.Text.Encoding::UTF8.GetString(System.Convert::FromBase64String(dc.callStatic(name)));
                    anytype value = new DictClass(className2Id('MXTXppInterpreterExecutor')).callStatic('evaluate', source);
                    res = strFmt('%1', value);`));

    for (const c of readCases().filter(c => c.expected !== null)) {
        const encoded = Buffer.from(toScript(c.code), 'utf8').toString('base64');

        methods += method(c.name, `    public static str ${c.name}()\n    {\n        return "${encoded}";\n    }`);
    }

    write(folder, 'DEVXppConformance', axClass('DEVXppConformance', 'public class DEVXppConformance\n{\n}', methods));
}

function compare() {
    const results = readResults(INTERPRETED_RESULTS);
    const cases = readCases().filter(c => c.expected !== null);
    const mask = text => text.replace(/(AccountKey|SharedAccessSignature|sig)=[^;&\s]+/gi, '$1=***');
    let different = 0, same = 0, missing = 0;

    // The file can't hold the trailing whitespace of a value, so neither side is compared with it
    for (const c of cases) {
        const expected = (KNOWN_DIFFERENCES[c.name] !== undefined ? KNOWN_DIFFERENCES[c.name] : c.expected).trimEnd();

        if (!results.has(c.name)) {
            missing++;
            console.log(`MISSING ${c.name}`);
        } else if (results.get(c.name).trimEnd() === expected) {
            same++;
        } else {
            different++;
            console.log(`DIFFERENT ${c.title}\n   compiler:    ${mask(expected)}\n   interpreter: ${mask(results.get(c.name)).slice(0, 700)}`);
        }
    }

    console.log(`\n${cases.length} cases: ${same} same, ${different} different, ${missing} missing`);
}

function update() {
    const results = readResults(COMPILED_RESULTS);
    const text = fs.readFileSync(GOLDEN, 'utf8').replace(/\r/g, '');
    // Without the blank lines before the first case: every case below starts with one, and they piled up with each update
    const header = text.slice(0, text.indexOf('\n### ') + 1).trimEnd() + '\n';

    let out = header;

    for (const c of readCases()) {
        out += `\n### ${c.title}\n${c.code}\n`;
        out += c.note && !results.has(c.name) ? `${c.note}\n` : results.has(c.name) ? `=> ${results.get(c.name)}\n` : c.expected !== null ? `=> ${c.expected}\n` : '!! NO RESULT\n';
    }

    fs.writeFileSync(GOLDEN, out.replace(/\n/g, '\r\n'));
    console.log(`${results.size} results read`);
}

const [,, command, folder] = process.argv;

switch (command) {
    case 'compiled': compiled(folder); break;
    case 'interpreted': interpreted(folder); break;
    case 'compare': compare(); break;
    case 'update': update(); break;
    default: console.log('Usage: see the top of this file');
}
