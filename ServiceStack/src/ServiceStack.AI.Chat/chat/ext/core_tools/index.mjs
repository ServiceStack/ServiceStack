import { ref, reactive, computed, onMounted, watch, inject, nextTick } from "vue"
import { ApiResult, createErrorStatus } from "@servicestack/client"
import { JsonSchemaForm } from "/ui/components/JsonSchemaForm.mjs"
import { generateTypes } from "/ui/components/jsonTypes.mjs"

let ext

// the JSON tab keeps one document, plus the schema generated from it, in localStorage. Typed classes are
// regenerated on demand instead - they're instant to produce, so there's nothing worth storing or saving.
const JSON_NAME = 'data.json'

const ARTIFACTS = [
    { id: 'json', label: 'Code', file: JSON_NAME, mime: 'application/json' },
    { id: 'ui', label: 'Schema', file: 'data.ui.json', mime: 'application/json', gen: 'schema' },
    { id: 'cs', label: 'C#', file: 'data.cs', mime: 'text/x-csharp', gen: 'types', language: 'csharp' },
    { id: 'py', label: 'Python', file: 'data.py', mime: 'text/x-python', gen: 'types', language: 'python' },
    { id: 'ts', label: 'TS', file: 'data.ts', mime: 'text/typescript', gen: 'types', language: 'typescript' },
    { id: 'js', label: 'JS', file: 'data.js', mime: 'text/javascript', gen: 'types', language: 'javascript' },
]
const artifactKey = id => (id === 'json' ? 'llms.tools.json' : `llms.tools.json.${id}`)
const isTypes = id => ARTIFACTS.some(a => a.id === id && a.gen === 'types')

// joined segmented button group, same as the pdf designer's sub toolbar
const BTN_GROUP =
    'inline-flex rounded-md shadow-sm overflow-hidden border border-gray-300 dark:border-gray-600 ' +
    'divide-x divide-gray-200 dark:divide-gray-700'
const BTN_ON = 'bg-indigo-600 text-white'
const BTN_OFF = 'bg-white dark:bg-gray-900 text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-800'
const BTN_NEW = 'bg-white dark:bg-gray-900 text-gray-400 dark:text-gray-500 hover:bg-gray-50 dark:hover:bg-gray-800'

const languages = {
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

const CodePage = {
    template: `
        <div class="flex flex-col h-full w-full">
            <component :is="'style'">
                .CodeMirror { height: 100% !important; }
            </component>
            <!-- Toolbar -->
            <div class="flex items-center justify-between p-2 border-b shrink-0" :class="[$styles.bgPage, $styles.chromeBorder]">
                <div class="flex items-center space-x-1">
                    <button v-for="lang in Object.keys(languages)" :key="lang" type="button" @click="language = lang" 
                        class="px-2.5 py-1 rounded-full text-xs font-medium border transition-colors select-none capitalize"
                        :class="language === lang 
                            ? $styles.tagButtonActive 
                            : $styles.tagButton">
                        {{ languages[lang].name }}
                    </button>
                </div>
                <div v-if="isJson" class="flex items-center gap-2">
                    <span v-if="genError" class="px-2 py-1 text-xs rounded-md border border-red-200 dark:border-red-800 bg-red-50 dark:bg-red-900/30 text-red-800 dark:text-red-200">{{ genError }}</span>
                </div>
                <div v-else class="flex items-center space-x-2">
                    <button type="button" @click="toggleOutput" class="p-1 rounded" :class="[$styles.mutedIcon,$styles.mutedIconHover]" :title="showOutput ? 'Hide Output' : 'Show Output'">
                        <svg v-if="showOutput" xmlns="http://www.w3.org/2000/svg" class="size-5" viewBox="0 0 24 24"><path fill="currentColor" d="M21 3H3c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2m0 16H3v-3h18zm0-5H3V5h18z"/></svg>
                        <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-5" viewBox="0 0 24 24"><path fill="currentColor" d="M21 3H3c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2m0 16H3V5h18z"/></svg>
                    </button>
                    <button @click="runCode" type="button" :disabled="loading" class="px-4 py-1.5 flex items-center shadow-sm transition-colors" :class="$styles.primaryButton">
                        <svg v-if="loading" class="animate-spin -ml-1 mr-2 h-4 w-4" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        <span v-else>Run</span>
                        <svg v-if="!loading" class="ml-1 size-5" fill="none" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M19.266 13.516a1.917 1.917 0 0 0 0-3.032A35.8 35.8 0 0 0 9.35 5.068l-.653-.232c-1.248-.443-2.567.401-2.736 1.69a42.5 42.5 0 0 0 0 10.948c.17 1.289 1.488 2.133 2.736 1.69l.653-.232a35.8 35.8 0 0 0 9.916-5.416"/></svg>
                    </button>
                </div>
            </div>

            <!-- Main Content -->
            <div class="flex-1 flex flex-col min-h-0">
                <!-- views of the JSON document: its source, the generated form, and each generated artifact -->
                <div v-if="isJson" class="flex items-center gap-2 px-2 py-1 border-b shrink-0 overflow-x-auto" :class="[$styles.chromeBorder, $styles.bgSidebar]">
                    <div :class="btnGroup">
                        <button v-for="v in dataViews" :key="v.id" type="button" @click="selectView(v.id)"
                            class="px-3 py-1 text-xs font-medium" :class="view === v.id ? btnOn : btnOff">
                            {{ v.label }}
                        </button>
                    </div>
                    <div :class="btnGroup">
                        <button v-for="a in generatable" :key="a.id" type="button" @click="selectView(a.id)"
                            :disabled="!!genBusy" :title="(artifacts[a.id] && !isTypes(a.id) ? 'Open ' : 'Generate ') + a.file"
                            class="px-3 py-1 text-xs font-medium inline-flex items-center gap-1 disabled:opacity-40"
                            :class="view === a.id ? btnOn : (artifacts[a.id] || isTypes(a.id) ? btnOff : btnNew)">
                            <svg v-if="genBusy === a.id" class="animate-spin size-3 text-blue-500" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z"></path>
                            </svg>
                            <span v-else-if="!artifacts[a.id] && !isTypes(a.id)" class="text-xs">+</span>
                            {{ a.label }}
                        </button>
                    </div>
                    <div class="flex-1"></div>
                    <span class="text-xs truncate" :class="$styles.muted">{{ viewFile }}</span>
                    <button v-if="!showForm" type="button" @click="copyEditor" class="px-2 py-1 text-xs inline-flex items-center gap-1 shrink-0" :class="$styles.secondaryButton" title="Copy to clipboard">
                        <svg xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                            <rect x="9" y="9" width="13" height="13" rx="2"/><path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"/>
                        </svg>
                        {{ copied ? 'Copied' : 'Copy' }}
                    </button>
                </div>

                <!-- Code Editor -->
                <div class="flex-1 overflow-hidden relative">
                    <!-- The div CodeMirror attaches to. We use absolute positioning to ensure it takes full space of parent -->
                    <div v-show="!showForm" ref="refInput" class="absolute inset-0 h-full w-full text-base"></div>
                    <div v-if="showForm" class="absolute inset-0 overflow-y-auto p-4" :class="$styles.bgInput">
                        <div v-if="!schema" class="h-full flex flex-col items-center justify-center gap-3 text-center">
                            <p class="text-xs max-w-sm" :class="$styles.muted">
                                No <span class="font-mono">data.ui.json</span> yet. Generate a JSON Schema for this document
                                and it will be rendered as a form.
                            </p>
                            <button type="button" @click="generate(schemaArtifact)" class="px-3 py-1.5 text-xs" :class="$styles.primaryButton">Generate form schema</button>
                        </div>
                        <p v-else-if="formError" class="text-xs" :class="$styles.muted">{{ formError }}</p>
                        <JsonSchemaForm v-else :schema="schema" :data="formData" :status="formStatus" @change="onFormChange" />
                    </div>
                </div>

                <!-- Output Pane -->
                <div v-if="showOutput && !isJson" class="h-1/3 min-h-[150px] border-t border-gray-200 dark:border-gray-700 bg-gray-50 dark:bg-gray-900 flex flex-col font-mono text-sm overflow-hidden shrink-0 shadow-[0_-4px_6px_-1px_rgba(0,0,0,0.1)] z-10">
                    <div class="px-2 py-1 bg-gray-100 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700 text-xs font-semibold text-gray-500 uppercase flex justify-between items-center select-none">
                        <span>Output</span>
                        <div class="flex items-center">
                            <span v-if="resultStatus" class="mr-2 px-2 py-0.5 rounded text-[10px]" :class="resultStatusColor">{{ resultStatus }}</span>
                            <button @click="showOutput=false" type="button" class="hover:text-gray-700 dark:hover:text-gray-300">
                                <svg xmlns="http://www.w3.org/2000/svg" class="size-4" viewBox="0 0 24 24"><path fill="currentColor" d="M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z"/></svg>
                            </button>
                        </div>
                    </div>
                     <div class="flex-1 overflow-auto p-2 whitespace-pre-wrap font-mono relative">
                        <div v-if="loading" class="absolute inset-0 bg-white/50 dark:bg-gray-900/50 flex items-center justify-center z-10 transition-opacity">
                             <div class="animate-pulse text-blue-500">Executing...</div>
                        </div>
                        <div v-if="!stdout && !stderr && !resultStatus && !loading" class="text-gray-400 italic p-4 text-center">
                            Press CTRL+ENTER or click Run to execute code.
                        </div>
                        <div v-if="stdout" class="text-gray-800 dark:text-gray-300">{{ stdout }}</div>
                        <div v-if="stderr" class="text-red-600 dark:text-red-400 mt-2 border-t border-red-200 dark:border-red-900 pt-2">{{ stderr }}</div>
                    </div>
                </div>
            </div>
        </div>
    `,
    setup() {
        let cm
        const refInput = ref()
        const language = ref(localStorage.getItem('llms.tools.lastLanguage') || 'python')
        const code = ref(localStorage.getItem(`llms.tools.${language.value}`) || '')
        const stdout = ref('')
        const stderr = ref('')
        const loading = ref(false)
        const resultStatus = ref('')
        const resultStatusColor = ref('')
        const showOutput = ref(true)

        // --- JSON tab: one document, plus a form schema and typed classes generated from it -----------
        const ctx = inject('ctx')
        const isJson = computed(() => language.value === 'json')
        const VIEW_IDS = ['code', 'form', ...ARTIFACTS.filter(a => a.gen).map(a => a.id)]
        const stored = localStorage.getItem('llms.tools.json.view')
        const view = ref(VIEW_IDS.includes(stored) ? stored : 'code')
        /** which artifact the editor is showing (the form has no editor) */
        const artifact = computed(() => (view.value === 'code' || view.value === 'form' ? 'json' : view.value))
        const artifacts = reactive(
            Object.fromEntries(ARTIFACTS.map(a => [a.id, isTypes(a.id) ? null : (localStorage.getItem(artifactKey(a.id)) ?? null)])),
        )
        // typed classes used to be cached here - drop any left from an earlier version
        ARTIFACTS.filter(a => isTypes(a.id)).forEach(a => localStorage.removeItem(artifactKey(a.id)))
        const genBusy = ref('')
        const genError = ref('')
        const formError = ref('')
        const formData = ref(null)
        const formStatus = ref(null)
        let formSource = null

        const dataViews = [
            { id: 'code', label: 'Code' },
            { id: 'form', label: 'Form' },
        ]
        const generatable = computed(() => ARTIFACTS.filter(a => a.gen))
        const viewFile = computed(() => ARTIFACTS.find(a => a.id === artifact.value)?.file ?? JSON_NAME)
        const schemaArtifact = ARTIFACTS.find(a => a.id === 'ui')
        const schema = computed(() => {
            try {
                return artifacts.ui ? JSON.parse(artifacts.ui) : null
            } catch {
                return null
            }
        })
        const showForm = computed(() => isJson.value && view.value === 'form')

        const copied = ref(false)
        let copiedTimer = null

        async function copyEditor() {
            const text = cm?.getValue() ?? ''
            try {
                await navigator.clipboard.writeText(text)
            } catch {
                // clipboard is blocked outside a secure context - fall back to a temporary selection
                const el = document.createElement('textarea')
                el.value = text
                document.body.appendChild(el)
                el.select()
                document.execCommand('copy')
                el.remove()
            }
            copied.value = true
            clearTimeout(copiedTimer)
            copiedTimer = setTimeout(() => (copied.value = false), 1500)
        }

        function jsonDoc() {
            return artifacts.json ?? localStorage.getItem('llms.tools.json') ?? languages.json.default
        }

        function setView(next) {
            const previous = artifact.value
            if (cm && previous && view.value !== 'form' && !isTypes(previous)) {
                // keep whatever is in the editor before switching away
                artifacts[previous] = cm.getValue()
                localStorage.setItem(artifactKey(previous), cm.getValue())
            }
            view.value = next
            localStorage.setItem('llms.tools.json.view', next)
            if (next === 'form') {
                loadForm()
                return
            }
            nextTick(() => {
                if (!cm) return
                const meta = ARTIFACTS.find(a => a.id === artifact.value)
                cm.setOption('mode', meta.mime)
                cm.setOption('readOnly', isTypes(artifact.value))
                cm.setValue(artifacts[artifact.value] ?? (artifact.value === 'json' ? jsonDoc() : ''))
                cm.refresh()
            })
        }

        /** typed classes regenerate on every click; the schema costs a model call, so it's kept */
        async function selectView(id) {
            const target = ARTIFACTS.find(a => a.id === id)
            if (!target?.gen || (artifacts[id] && !isTypes(id))) return setView(id)
            await generate(target)
        }

        function loadForm() {
            formError.value = ''
            const raw = jsonDoc()
            try {
                formData.value = JSON.parse(raw || '{}')
                formSource = raw
            } catch (e) {
                formData.value = null
                formError.value = `This document isn't valid JSON yet - fix it in the Code view. (${e.message})`
            }
        }

        /** form edits write back to the document and localStorage, so both views stay in step */
        function onFormChange() {
            const json = JSON.stringify(formData.value, null, 2) + '\n'
            formSource = json
            artifacts.json = json
            localStorage.setItem('llms.tools.json', json)
            if (artifact.value === 'json' && cm) {
                code.value = json
                cm.setValue(json)
            }
        }

        async function generate(target) {
            if (genBusy.value) return
            genError.value = ''
            const content = artifact.value === 'json' && cm ? cm.getValue() : jsonDoc()
            genBusy.value = target.id
            try {
                let generated
                if (target.gen === 'types') {
                    // deterministic and local - the schema, when generated, sharpens the output
                    generated = generateTypes({
                        name: JSON_NAME,
                        json: content || '{}',
                        schema: artifacts.ui || undefined,
                        language: target.language,
                    }).content
                } else {
                    const model = ctx?.state?.selectedModel
                    if (!model) {
                        genError.value = 'Select a model first'
                        return
                    }
                    const api = await ext.postJson('/schema', { name: JSON_NAME, model, content })
                    if (api.error) {
                        genError.value = api.error.message ?? 'Generation failed'
                        return
                    }
                    generated = api.response.content
                }
                artifacts[target.id] = generated
                if (!isTypes(target.id)) localStorage.setItem(artifactKey(target.id), generated)
                setView(target.id)
            } catch (e) {
                genError.value = `${e.message ?? e}`
            } finally {
                genBusy.value = ''
            }
        }

        const loadCode = (lang) => {
            const saved = localStorage.getItem(`llms.tools.${lang}`)
            // Default snippets if empty
            if (!saved || Object.values(languages).some(l => l.default.trim() === saved.trim())) {
                return languages[lang].default
            }
            return saved
        }

        // Initial load
        code.value = loadCode(language.value)

        watch(language, (newLang, oldLang) => {
            // Save old language code
            if (oldLang && cm) {
                const currentContent = cm.getValue()
                localStorage.setItem(`llms.tools.${oldLang}`, currentContent)
            }
            localStorage.setItem('llms.tools.lastLanguage', newLang)

            // Load new language code
            code.value = loadCode(newLang)
            if (cm) {
                cm.setValue(code.value)
                cm.setOption('mode', languages[newLang].mime)
            }

            // Clear output on language switch
            stdout.value = ''
            stderr.value = ''
            resultStatus.value = ''
            genError.value = ''
            if (newLang === 'json') {
                if (view.value === 'form') loadForm()
                else if (isTypes(view.value)) selectView(view.value)
                else setView(view.value)
            }
        })

        function setError(status) {
            if (!status) return
            if (typeof status == 'string') {
                status = {
                    message: status,
                    errorCode: 'Error'
                }
            }
            stderr.value = status.message
            resultStatus.value = status.errorCode || 'Error'
            resultStatusColor.value = 'text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-900'
        }

        const toggleOutput = () => {
            showOutput.value = !showOutput.value
            nextTick(() => {
                if (cm) cm.refresh()
            })
        }

        const runCode = async () => {
            if (loading.value) return

            if (!showOutput.value) {
                showOutput.value = true
                nextTick(() => {
                    if (cm) cm.refresh()
                })
            }

            // Save before run
            if (cm) {
                code.value = cm.getValue()
            }
            localStorage.setItem(`llms.tools.${language.value}`, code.value)

            loading.value = true
            stdout.value = ''
            stderr.value = ''
            resultStatus.value = ''
            let api

            try {
                const res = await ext.post(`/code/${language.value}/run`, {
                    body: code.value
                })
                if (!res.ok) {
                    api = new ApiResult({ error: createErrorStatus(`HTTP ${res.status} ${res.statusText}`) })
                } else {
                    const response = await res.json()
                    api = new ApiResult({ response })
                }
            } catch (e) {
                api = new ApiResult({ error: createErrorStatus(e.message) })
            }

            if (api.response) {
                const result = api.response
                stdout.value = result.stdout || ''
                stderr.value = result.stderr || ''

                if (result.returncode === 0) {
                    resultStatus.value = 'Success'
                    resultStatusColor.value = 'text-green-600 bg-green-100 dark:text-green-400 dark:bg-green-900'
                } else {
                    resultStatus.value = `Exit Code: ${result.returncode}`
                    resultStatusColor.value = 'text-red-600 bg-red-100 dark:text-red-400 dark:bg-red-900'
                }
            }
            else if (api.error) {
                setError(api.error)
            }

            loading.value = false
        }

        onMounted(() => {
            // Ensure CodeMirror is global
            if (typeof CodeMirror === 'undefined') {
                console.error('CodeMirror is not loaded')
                return
            }

            cm = CodeMirror(refInput.value, {
                lineNumbers: true,
                styleActiveLine: true,
                matchBrackets: true,
                mode: languages[language.value].mime,
                theme: 'ctp-mocha', // using the theme from existing code
                value: code.value,
                extraKeys: {
                    "Ctrl-Enter": () => !isJson.value && runCode(),
                    "Cmd-Enter": () => !isJson.value && runCode(), // Mac support
                },
                tabSize: 4,
                indentUnit: 4,
                lineWrapping: false, // Code editors usually don't wrap by default, but customizable
            })

            cm.on('change', () => {
                code.value = cm.getValue()
                if (isJson.value) {
                    if (isTypes(artifact.value)) return
                    artifacts[artifact.value] = code.value
                    localStorage.setItem(artifactKey(artifact.value), code.value)
                } else {
                    localStorage.setItem(`llms.tools.${language.value}`, code.value)
                }
            })

            // Fix layout issues when resizing
            window.addEventListener('resize', () => {
                cm.refresh()
            })

            // a restored view has nothing cached behind it - rebuild whatever it was showing
            if (isJson.value) {
                if (view.value === 'form') loadForm()
                else if (isTypes(view.value)) selectView(view.value)
                else setView(view.value)
            }
        })

        return {
            languages,
            refInput,
            stdout,
            stderr,
            loading,
            resultStatus,
            resultStatusColor,
            language,
            code,
            showOutput,
            toggleOutput,
            runCode,
            isJson, view, setView, selectView, dataViews, artifact, artifacts, generatable, schemaArtifact, viewFile,
            isTypes, copyEditor, copied,
            schema, showForm, formData, formError, formStatus, onFormChange,
            genBusy, genError, generate,
            btnGroup: BTN_GROUP, btnOn: BTN_ON, btnOff: BTN_OFF, btnNew: BTN_NEW,
        }
    }
}

const CalcPage = {
    template: `
        <div class="flex flex-col h-full w-full text-base" :class="[$styles.bgPage]">
            <!-- Header/Input Area -->
            <div class="p-4 shrink-0 border-b" :class="[$styles.bgPage, $styles.chromeBorder]">
                <div class="max-w-3xl mx-auto w-full">
                    <form @submit.prevent="calculate" class="relative">
                        <input
                            ref="inputRef"
                            v-model="expression"
                            type="text"
                            placeholder="Type an expression (e.g. 1 + 2 * 3) and press Enter"
                            class="w-full px-4 py-3 pr-12 rounded-lg shadow-sm transition-all"
                            :class="[$styles.bgInput, $styles.textInput, $styles.borderInput]"
                            :disabled="loading"
                            autofocus
                        />
                        <button
                            type="submit"
                            :disabled="loading || !expression.trim()"
                            class="absolute right-2 top-1/2 -translate-y-1/2 p-2 rounded-md transition-colors disabled:cursor-not-allowed"
                            :class="[$styles.mutedIcon]"
                            title="Calculate"
                        >
                            <svg v-if="loading" class="animate-spin size-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                                <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                                <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                            </svg>
                            <svg v-else class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" d="M16 14H8m8-4H8"/><circle cx="12" cy="12" r="10"/></g></svg>
                        </button>
                    </form>
                    <div v-if="error" class="mt-2 text-sm text-red-600 dark:text-red-400">
                        {{ error }}
                    </div>
                </div>
            </div>

            <!-- History List -->
            <div class="flex-1 overflow-auto p-4">
                <div class="max-w-3xl mx-auto w-full space-y-3">
                    <div v-if="history.length === 0" class="text-center py-10 italic" :class="$styles.muted">
                        No calculation history.
                    </div>
                    
                    <div v-for="(item, index) in history" :key="index" class="group rounded-lg p-4 shadow-sm hover:shadow-md transition-all" :class="[$styles.card]">
                        <div class="flex items-center justify-between gap-4">
                            <div class="flex-1 space-y-1 min-w-0">
                                <!-- Expression -->
                                <div class="flex items-center gap-2 group/expr cursor-pointer" @click="useResult(item.expression, item, 'expr')">
                                    <span 
                                        class="font-mono group-hover/expr:text-blue-600 dark:group-hover/expr:text-blue-400 transition-colors select-none"
                                        :class="[$styles.muted]"
                                        title="Click to copy & use"
                                    >
                                        {{ item.expression }} =
                                    </span>
                                    <button 
                                        type="button"
                                        class="opacity-0 group-hover/expr:opacity-100 p-1 text-gray-400 group-hover/expr:text-blue-500 transition-opacity"
                                        title="Copy expression"
                                    >
                                        <svg v-if="item.copiedExpr" class="size-3.5 text-green-600 dark:text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg>
                                        <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-3.5" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg>
                                    </button>
                                </div>
                                
                                <!-- Answer -->
                                <div class="flex items-center gap-2 group/ans cursor-pointer" @click="useResult(item.answer, item, 'ans')">
                                    <span 
                                        class="font-mono text-xl font-semibold text-gray-900 dark:text-white group-hover/ans:text-blue-600 dark:group-hover/ans:text-blue-400 transition-colors break-all"
                                        title="Click to copy & use"
                                    >
                                        {{ item.answer }}
                                    </span>
                                    <button 
                                        type="button"
                                        class="opacity-0 group-hover/ans:opacity-100 p-1 text-gray-400 group-hover/ans:text-blue-500 transition-opacity"
                                        title="Copy answer"
                                    >
                                        <svg v-if="item.copiedAns" class="size-4 text-green-600 dark:text-green-500" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="m9.55 18l-5.7-5.7l1.425-1.425L9.55 15.15l9.175-9.175L20.15 7.4z"/></svg>
                                        <svg v-else xmlns="http://www.w3.org/2000/svg" class="size-4" viewBox="0 0 24 24"><path fill="currentColor" d="M16 1H4c-1.1 0-2 .9-2 2v14h2V3h12zm3 4H8c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h11c1.1 0 2-.9 2-2V7c0-1.1-.9-2-2-2m0 16H8V7h11z"/></svg>
                                    </button>
                                </div>
                            </div>
                            
                            <!-- Delete Button -->
                            <button 
                                type="button"
                                @click="remove(index)"
                                class="opacity-0 group-hover:opacity-100 p-2 text-gray-400 hover:text-red-500 hover:bg-red-50 dark:hover:bg-red-900/30 rounded-md transition-all"
                                title="Delete"
                            >
                                <svg class="size-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"></path></svg>
                            </button>
                        </div>
                    </div>

                    <div v-if="history.length" class="flex justify-center pt-4">
                        <button 
                            type="button"
                            @click="clearAll"
                            class="text-sm"
                            :class="[$styles.highlighted, $styles.linkHover]"
                        >
                            clear all history
                        </button>
                    </div>
                    
                    <!-- Features (Operators & Functions) -->
                    <div v-if="features.operators?.length || features.functions?.length" class="mt-12 mb-6 space-y-4">
                        <!-- Numbers -->
                        <div>
                            <div class="flex flex-wrap gap-2">
                                <button 
                                    v-for="num in features.numbers" 
                                    :key="num"
                                    type="button"
                                    @click="insert(num)"
                                    class="px-3 py-1 rounded text-sm font-mono transition-colors"
                                    :class="[$styles.secondaryButton]"
                                    :title="'insert number ' + num"
                                >
                                    {{ num }}
                                </button>
                                <span class="px-1 py-1" :class="[$styles.icon]">|</span>
                                <button 
                                    v-for="c in features.constants" 
                                    :key="c"
                                    type="button"
                                    @click="insert(c)"
                                    class="px-3 py-1 rounded text-sm font-mono transition-colors"
                                    :class="[$styles.secondaryButton]"
                                    :title="'insert constant ' + c"
                                >
                                    {{ c }}
                                </button>
                            </div>
                        </div>

                        <!-- Operators -->
                        <div v-if="features.operators?.length">
                            <div class="flex flex-wrap gap-2">
                                <button 
                                    v-for="op in features.operators" 
                                    :key="op"
                                    type="button"
                                    @click="insert(op)"
                                    class="px-3 py-1 rounded text-sm font-mono transition-colors"
                                    :class="[$styles.secondaryButton]"
                                    :title="'insert operator ' + op"
                                >
                                    {{ op }}
                                </button>
                            </div>
                        </div>

                        <!-- Functions -->
                        <div v-if="features.functions?.length">
                            <h3 class="text-xs font-semibold uppercase tracking-wider mb-2" :class="[$styles.muted]">Functions</h3>
                            <div class="flex flex-wrap gap-2">
                                <button 
                                    v-for="func in features.functions" 
                                    :key="func"
                                    type="button"
                                    @click="wrapWithFunction(func)"
                                    class="px-3 py-1 rounded text-sm font-mono transition-colors"
                                    :class="[$styles.secondaryButton]"
                                    :title="'use function ' + func"
                                >
                                    {{ func }}
                                </button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const expression = ref('')
        const history = ref([])
        const loading = ref(false)
        const error = ref('')
        const inputRef = ref()
        const features = ref({ functions: [] })

        // Load history from localStorage
        try {
            const saved = localStorage.getItem('llms.tools.calc.history')
            if (saved) {
                history.value = JSON.parse(saved)
            }
        } catch (e) {
            console.error('Failed to load history', e)
        }

        function setExpr(result) {
            if (Array.isArray(result)) {
                expression.value = JSON.stringify(result)
            } else {
                expression.value = String(result)
            }
        }

        const saveHistory = () => {
            localStorage.setItem('llms.tools.calc.history', JSON.stringify(history.value))
        }

        const calculate = async () => {
            if (!expression.value.trim() || loading.value) return

            loading.value = true
            error.value = ''
            const expr = expression.value

            const res = await ext.post('/calc', {
                body: expr
            })

            const api = await ext.createJsonResult(res)

            if (api.response) {
                // Add to history (newest first)
                history.value.unshift({
                    expression: expr,
                    answer: api.response.result,
                    timestamp: Date.now()
                })

                // Keep history size reasonable
                if (history.value.length > 50) {
                    history.value = history.value.slice(0, 50)
                }

                saveHistory()
                setExpr(api.response.result)
            } else {
                error.value = api.error.message
            }

            loading.value = false
            // Refocus input and move cursor to end
            nextTick(() => {
                if (inputRef.value) {
                    inputRef.value.focus()
                    const len = inputRef.value.value.length
                    inputRef.value.setSelectionRange(len, len)
                }
            })
        }

        const populate = (result) => {
            setExpr(result)
            inputRef.value?.focus()
        }

        const insert = (text) => {
            expression.value += String(text)
            inputRef.value?.focus()
        }

        const wrapWithFunction = (fn) => {
            const input = inputRef.value
            if (!input) return

            const start = input.selectionStart
            const end = input.selectionEnd
            const val = expression.value

            if (start !== end) {
                // Wrap selection
                const selected = val.substring(start, end)
                const before = val.substring(0, start)
                const after = val.substring(end)
                expression.value = `${before}${fn}(${selected})${after}`

                nextTick(() => {
                    input.focus()
                    // Position cursor after the closing parenthesis
                    const newPos = start + fn.length + 1 + selected.length + 1
                    input.setSelectionRange(newPos, newPos)
                })
            } else if (val) {
                // Wrap entire expression
                expression.value = `${fn}(${val})`
                nextTick(() => {
                    input.focus()
                    // Position cursor at end
                    const len = expression.value.length
                    input.setSelectionRange(len, len)
                })
            } else {
                // Just insert empty function
                expression.value = `${fn}()`
                nextTick(() => {
                    input.focus()
                    // Position cursor inside parentheses
                    const pos = fn.length + 1
                    input.setSelectionRange(pos, pos)
                })
            }
        }

        const copy = (text) => {
            navigator.clipboard.writeText(String(text))
        }

        const useResult = (text, item, type) => {
            populate(text)
            const str = String(text)
            copy(str)

            // Set temporary success state
            if (type === 'expr') item.copiedExpr = true
            else if (type === 'ans') item.copiedAns = true

            setTimeout(() => {
                if (type === 'expr') item.copiedExpr = false
                else if (type === 'ans') item.copiedAns = false
            }, 2000)

            ctx.toast('Copied to clipboard')
        }

        const remove = (index) => {
            history.value.splice(index, 1)
            saveHistory()
        }

        const clearAll = () => {
            if (confirm('Are you sure you want to clear all history?')) {
                history.value = []
                saveHistory()
            }
        }

        onMounted(async () => {
            const api = await ext.getJson('/calc')
            features.value = api.response
            console.log(features.value)
        })

        return {
            expression,
            history,
            loading,
            error,
            inputRef,
            calculate,
            useResult,
            remove,
            clearAll,
            features,
            insert,
            wrapWithFunction,
        }
    }
}

export default {
    install(ctx) {
        ext = ctx.scope('core_tools')

        // shared component, also used by the pdf designer - register it here so /code works standalone
        ctx.components({ JsonSchemaForm })

        const LANGUAGE_TOOLS = Object.values(languages).map(x => x.tool).filter(Boolean)
        ctx.setLeftIcons({
            code: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/code')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M0 0h24v24H0z"/><path fill="currentColor" d="M14.486 3.143a1 1 0 0 1 .692 1.233l-4.43 15.788a1 1 0 0 1-1.926-.54l4.43-15.788a1 1 0 0 1 1.234-.693M7.207 7.05a1 1 0 0 1 0 1.414L3.672 12l3.535 3.535a1 1 0 1 1-1.414 1.415L1.55 12.707a1 1 0 0 1 0-1.414L5.793 7.05a1 1 0 0 1 1.414 0m9.586 1.414a1 1 0 1 1 1.414-1.414l4.243 4.243a1 1 0 0 1 0 1.414l-4.243 4.243a1 1 0 0 1-1.414-1.415L20.328 12z"/></g></svg>`,
                    setup() {
                    }
                },
                isVisible() {
                    return LANGUAGE_TOOLS.some(tool => ctx.state.tool?.groups?.core_tools?.includes(tool))
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/code')
                },
                title: 'Run Code',
            },
            calc: {
                component: {
                    template: `<svg @click="$ctx.togglePath('/calc')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 14 14"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="1"><path d="M11.5.5h-9a1 1 0 0 0-1 1v11a1 1 0 0 0 1 1h9a1 1 0 0 0 1-1v-11a1 1 0 0 0-1-1m-10 5h11"/><path d="M4.25 8.5a.25.25 0 0 1 0-.5m0 .5a.25.25 0 0 0 0-.5M7 8.5A.25.25 0 0 1 7 8m0 .5A.25.25 0 0 0 7 8m2.75.5a.25.25 0 0 1 0-.5m0 .5a.25.25 0 0 0 0-.5m-5.5 3a.25.25 0 1 1 0-.5m0 .5a.25.25 0 1 0 0-.5M7 11a.25.25 0 1 1 0-.5m0 .5a.25.25 0 1 0 0-.5m2.75.5a.25.25 0 1 1 0-.5m0 .5a.25.25 0 1 0 0-.5M10 3H9"/></g></svg>`,
                    setup() {
                    }
                },
                isVisible() {
                    return ctx.state.tool?.groups?.core_tools?.includes('calc')
                },
                isActive({ path }) {
                    return ctx.matchesPath(path, '/calc')
                },
                title: 'Calculator',
            }
        })

        ctx.routes.push({ path: '/code', component: CodePage, meta: { title: 'Run Code' } })
        ctx.routes.push({ path: '/calc', component: CalcPage, meta: { title: 'Calculator' } })
    }
}
