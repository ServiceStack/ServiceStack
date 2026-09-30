export const languages = {
    python: {
        name: 'Python',
        mime: 'text/x-python',
        default: 'print("Hello, Python!")\n',
        tool: 'run_python',
    },
    javascript: {
        name: 'JavaScript',
        mime: 'text/javascript',
        default: 'console.log("Hello, JavaScript!");\n',
        tool: 'run_javascript',
    },
    typescript: {
        name: 'TypeScript',
        mime: 'text/typescript',
        default: 'const msg: string = "Hello, TypeScript!";\nconsole.log(msg);\n',
        tool: 'run_typescript',
    },
    csharp: {
        name: 'C#',
        mime: 'text/x-csharp',
        default: 'Console.WriteLine("Hello, C#!");\n',
        tool: 'run_csharp',
    },
    json: {
        name: 'JSON',
        mime: 'application/json',
        // not runnable - this tab generates a form UI and typed classes from the document instead
        default: JSON.stringify({
            name: 'Acme Widgets',
            founded: 2019,
            active: true,
            contact: { email: 'hi@acme.example', phone: '+61 2 5555 0100' },
            products: [
                { sku: 'W-100', title: 'Widget', price: 19.95, tags: ['popular'] },
                { sku: 'W-200', title: 'Widget Pro', price: 49.5, tags: [] },
            ],
        }, null, 2) + '\n',
        tool: null,
    },
}
