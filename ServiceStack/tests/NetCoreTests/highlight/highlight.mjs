// highlight.mjs
import hljs from 'highlight.js/lib/core';

// ✅ Import only the languages you need
import bash from 'highlight.js/lib/languages/bash';
import c from 'highlight.js/lib/languages/c';
import csharp from 'highlight.js/lib/languages/csharp';
import css from 'highlight.js/lib/languages/css';
import dart from 'highlight.js/lib/languages/dart';
import fsharp from 'highlight.js/lib/languages/fsharp';
import go from 'highlight.js/lib/languages/go';
import java from 'highlight.js/lib/languages/java';
import javascript from 'highlight.js/lib/languages/javascript';
import json from 'highlight.js/lib/languages/json';
import kotlin from 'highlight.js/lib/languages/kotlin';
import lisp from 'highlight.js/lib/languages/lisp';
import markdown from 'highlight.js/lib/languages/markdown';
import php from 'highlight.js/lib/languages/php';
import plaintext from 'highlight.js/lib/languages/plaintext';
import python from 'highlight.js/lib/languages/python';
import ruby from 'highlight.js/lib/languages/ruby';
import rust from 'highlight.js/lib/languages/rust';
import shell from 'highlight.js/lib/languages/shell';
import sql from 'highlight.js/lib/languages/sql';
import swift from 'highlight.js/lib/languages/swift';
import typescript from 'highlight.js/lib/languages/typescript';
import vbnet from 'highlight.js/lib/languages/vbnet';
import yaml from 'highlight.js/lib/languages/yaml';
import xml from 'highlight.js/lib/languages/xml';

// Zig isn't included in the Highlight.js language bundle.
const zig = hljs => ({
    name: 'Zig',
    aliases: ['zig'],
    keywords: {
        keyword: 'addrspace align allowzero and anyframe anytype asm async await break callconv catch comptime const continue defer else enum errdefer error export extern fn for if inline linksection noalias noinline nosuspend opaque or orelse packed pub resume return struct suspend switch test threadlocal try union unreachable usingnamespace var volatile while',
        type: 'bool void noreturn type anyerror anyopaque comptime_int comptime_float f16 f32 f64 f80 f128 i8 u8 i16 u16 i32 u32 i64 u64 i128 u128 isize usize c_char c_short c_ushort c_int c_uint c_long c_ulong c_longlong c_ulonglong c_longdouble',
        literal: 'true false null undefined',
    },
    contains: [
        hljs.COMMENT('//', '$', { contains: [{ scope: 'doctag', begin: '//[!/]' }] }),
        { scope: 'string', begin: /\\\\/, end: /$/ },
        hljs.QUOTE_STRING_MODE,
        { scope: 'string', begin: /'/, end: /'/, illegal: /\n/, contains: [hljs.BACKSLASH_ESCAPE] },
        { scope: 'built_in', begin: /@[a-zA-Z_]\w*/ },
        hljs.C_NUMBER_MODE,
        { scope: 'title.function', begin: /\bfn\s+/, end: /[\s(]/, excludeBegin: true, excludeEnd: true },
        { scope: 'type', begin: /\b[A-Z]\w*/, relevance: 0 },
    ],
});

// ✅ Register them
hljs.registerLanguage('bash', bash);
hljs.registerLanguage('c', c);
hljs.registerLanguage('csharp', csharp);
hljs.registerLanguage('css', css);
hljs.registerLanguage('dart', dart);
hljs.registerLanguage('fsharp', fsharp);
hljs.registerLanguage('go', go);
hljs.registerLanguage('java', java);
hljs.registerLanguage('javascript', javascript);
hljs.registerLanguage('json', json);
hljs.registerLanguage('kotlin', kotlin);
hljs.registerLanguage('lisp', lisp);
hljs.registerLanguage('markdown', markdown);
hljs.registerLanguage('php', php);
hljs.registerLanguage('plaintext', plaintext);
hljs.registerLanguage('python', python);
hljs.registerLanguage('ruby', ruby);
hljs.registerLanguage('rust', rust);
hljs.registerLanguage('shell', shell);
hljs.registerLanguage('sql', sql);
hljs.registerLanguage('swift', swift);
hljs.registerLanguage('typescript', typescript);
hljs.registerLanguage('vbnet', vbnet);
hljs.registerLanguage('yaml', yaml);
hljs.registerLanguage('xml', xml);
hljs.registerLanguage('zig', zig);

// ✅ Optionally: import a theme if bundling CSS
// import 'highlight.js/styles/github-dark.css';

export default hljs;
