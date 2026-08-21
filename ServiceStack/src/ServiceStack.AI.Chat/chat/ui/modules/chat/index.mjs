import { ref, watch, computed, nextTick, inject, onMounted, onUnmounted } from 'vue'
import { $$, createElement, lastRightPart, ApiResult, createErrorStatus } from "@servicestack/client"
import SettingsDialog, { useSettings } from './SettingsDialog.mjs'
import {
    ChatBody, ErrorBubble, LightboxImage, TypeText, TypeImage, TypeAudio, TypeFile, ViewType, ViewTypes,
    ViewToolTypes, TextViewer, ToolCall, ToolArguments, ToolOutput, MessageUsage, MessageReasoning,
    CompactThreadButton, UserAvatar, AgentAvatar, CodeBlock, JsonPreview,
} from './ChatBody.mjs'
import { AppContext } from '../../ctx.mjs'

const imageExts = 'png,webp,jpg,jpeg,gif,bmp,svg,tiff,ico'.split(',')
const audioExts = 'mp3,wav,ogg,flac,m4a,opus,webm'.split(',')

/* Example image generation request: https://openrouter.ai/docs/guides/overview/multimodal/image-generation
{
    "model": "google/gemini-2.5-flash-image-preview",
    "messages": [
        {
            "role": "user",
            "content": "Create a picture of a nano banana dish in a fancy restaurant with a Gemini theme"
        }
    ],
    "modalities": ["image", "text"],
    "image_config": {
        "aspect_ratio": "16:9"
    }
}
Example response:
{
  "choices": [
    {
      "message": {
        "role": "assistant",
        "content": "I've generated a beautiful sunset image for you.",
        "images": [
          {
            "type": "image_url",
            "image_url": {
              "url": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
            }
          }
        ]
      }
    }
  ]
}
*/
const imageAspectRatios = {
    '1024×1024': '1:1',
    '832×1248': '2:3',
    '1248×832': '3:2',
    '864×1184': '3:4',
    '1184×864': '4:3',
    '896×1152': '4:5',
    '1152×896': '5:4',
    '768×1344': '9:16',
    '1344×768': '16:9',
    '1536×672': '21:9',
}
// Reverse lookup
const imageRatioSizes = Object.entries(imageAspectRatios).reduce((acc, [key, value]) => {
    acc[value] = key
    return acc
}, {})

const svg = {
    clipboard: `<svg class="w-6 h-6" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none"><path d="M8 5H6a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2v-1M8 5a2 2 0 0 0 2 2h2a2 2 0 0 0 2-2M8 5a2 2 0 0 1 2-2h2a2 2 0 0 1 2 2m0 0h2a2 2 0 0 1 2 2v3m2 4H10m0 0l3-3m-3 3l3 3" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"></path></g></svg>`,
    check: `<svg class="w-6 h-6 text-green-500" fill="none" stroke="currentColor" viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 13l4 4L19 7"></path></svg>`,
}

function copyBlock(btn) {
    // console.log('copyBlock',btn)
    const label = btn.previousElementSibling
    const code = btn.parentElement.nextElementSibling
    label.classList.remove('hidden')
    label.innerHTML = 'copied'
    btn.classList.add('border-gray-600', 'bg-gray-700')
    btn.classList.remove('border-gray-700')
    btn.innerHTML = svg.check
    navigator.clipboard.writeText(code.innerText)
    setTimeout(() => {
        label.classList.add('hidden')
        label.innerHTML = ''
        btn.innerHTML = svg.clipboard
        btn.classList.remove('border-gray-600', 'bg-gray-700')
        btn.classList.add('border-gray-700')
    }, 2000)
}

function addCopyButtonToCodeBlocks(sel) {
    globalThis.copyBlock ??= copyBlock
    //console.log('addCopyButtonToCodeBlocks', sel, [...$$(sel)].length)

    $$(sel).forEach(code => {
        let pre = code.parentElement;
        if (pre.classList.contains('group')) return
        pre.classList.add('relative', 'group')

        const div = createElement('div', { attrs: { className: 'opacity-0 group-hover:opacity-100 transition-opacity duration-100 flex absolute right-2 -mt-1 select-none' } })
        const label = createElement('div', { attrs: { className: 'hidden font-sans p-1 px-2 mr-1 rounded-md border border-gray-600 bg-gray-700 text-gray-400' } })
        const btn = createElement('button', {
            attrs: {
                type: 'button',
                className: 'p-1 rounded-md border block text-gray-500 hover:text-gray-400 border-gray-700 hover:border-gray-600',
                onclick: 'copyBlock(this)'
            }
        })
        btn.innerHTML = svg.clipboard
        div.appendChild(label)
        div.appendChild(btn)
        pre.insertBefore(div, code)
    })
}

export function addCopyButtons() {
    addCopyButtonToCodeBlocks('.prose pre>code')
}

export function useChatPrompt(ctx) {
    const messageText = ref('')
    const promptHistory = ref([])
    const attachedFiles = ref([])
    const hasImage = () => attachedFiles.value.some(f => imageExts.includes(lastRightPart(f.name, '.')))
    const hasAudio = () => attachedFiles.value.some(f => audioExts.includes(lastRightPart(f.name, '.')))
    const hasFile = () => attachedFiles.value.length > 0

    const editingMessage = ref(null)

    function reset() {
        // Ensure initial state is ready to accept input
        attachedFiles.value = []
        messageText.value = ''
        editingMessage.value = null
    }

    const settings = useSettings()

    function getModel(name) {
        return ctx.state.models.find(x => x.name === name) ?? ctx.state.models.find(x => x.id === name)
    }

    function getSelectedModel() {
        const candidates = [ctx.state.selectedModel, ctx.state.config.defaults.text.model]
        const ret = candidates.map(name => name && getModel(name)).find(x => !!x)
        if (!ret) {
            // Try to find a model in the latest threads
            for (const thread in ctx.threads.threads) {
                const model = thread.model && getModel(thread.model)
                if (model) return model
            }
        }
        return ret
    }

    function setSelectedModel(model) {
        if (!model) {
            console.log('setSelectedModel(null)')
            return
        }
        ctx.setState({
            selectedModel: model.name,
            selectedVoice: model.defaults?.voice || undefined
        })
        ctx.setPrefs({
            model: model.name
        })
    }

    function getProviderForModel(model) {
        return getModel(model)?.provider
    }

    const canGenerateImage = model => {
        return model?.modalities?.output?.includes('image')
    }
    const canGenerateAudio = model => {
        return model?.modalities?.output?.includes('audio')
    }
    const getVoices = model => model?.voices ?? []

    function applySettings(request) {
        settings.applySettings(request)
    }

    function createContent({ text, files }) {
        let content = []

        // Add Text Block
        if (text) {
            content.push({ type: 'text', text })
        }

        // Add Attachment Blocks
        if (Array.isArray(files)) {
            for (const f of files) {
                const ext = lastRightPart(f.name, '.')
                if (imageExts.includes(ext)) {
                    content.push({ type: 'image_url', image_url: { url: f.url } })
                } else if (audioExts.includes(ext)) {
                    content.push({ type: 'input_audio', input_audio: { data: f.url, format: ext } })
                } else {
                    content.push({ type: 'file', file: { file_data: f.url, filename: f.name } })
                }
            }
        }
        return content
    }

    function createRequest({ model, text, files, systemPrompt, aspectRatio, voice }) {
        // Construct API Request from History
        const request = {
            model: model.name,
            messages: [],
            metadata: {}
        }

        // Apply user settings
        applySettings(request)

        if (systemPrompt) {
            request.messages = request.messages.filter(m => m.role !== 'system')
            request.messages.unshift({
                role: 'system',
                content: systemPrompt
            })
        }

        if (canGenerateImage(model)) {
            request.image_config = {
                aspect_ratio: aspectRatio || imageAspectRatios[ctx.state.selectedAspectRatio] || '1:1'
            }
            request.modalities = ["image", "text"]
        }
        else if (canGenerateAudio(model)) {
            request.modalities = ["audio", "text"]
        }

        if (voice) {
            request.metadata['voice'] = voice
        }

        if (text) {
            const content = createContent({ text, files })
            request.messages.push({
                role: 'user',
                content
            })
        }

        return request
    }

    async function completion({ request, thread, model, controller, redirect }) {
        try {
            let error
            if (!model) {
                if (request.model) {
                    model = getModel(request.model)
                } else {
                    model = getModel(request.model) ?? getSelectedModel()
                }
            }

            if (!model) {
                return ctx.createErrorResult({ message: `Model ${request.model || ''} not found`, errorCode: 'NotFound' })
            }

            if (!thread) {
                const title = getTextContent(request) || 'New Chat'
                thread = await ctx.threads.startNewThread({ title, redirect })
            }

            const ctxRequest = ctx.createChatContext({ request, thread, model })
            ctx.chatRequestFilters.forEach(f => f(ctxRequest))
            ctx.completeChatContext(ctxRequest)

            // Send to API
            const startTime = Date.now()
            const res = await ctx.post('/v1/chat/completions', {
                body: JSON.stringify(request),
                signal: controller?.signal
            })

            let response = null
            if (!res.ok) {
                error = ctx.createErrorStatus({ message: `HTTP ${res.status} ${res.statusText}` })
                let errorBody = null
                try {
                    errorBody = await res.text()
                    if (errorBody) {
                        // Try to parse as JSON for better formatting
                        try {
                            const errorJson = JSON.parse(errorBody)
                            const status = errorJson?.responseStatus
                            if (status) {
                                error.errorCode += ` ${status.errorCode}`
                                error.message = status.message
                                error.stackTrace = status.stackTrace
                            } else {
                                error.stackTrace = JSON.stringify(errorJson, null, 2)
                            }
                        } catch (e) {
                        }
                    }
                } catch (e) {
                    // If we can't read the response body, just use the status
                }
            } else {
                try {
                    response = await res.json()
                    const ctxResponse = {
                        response,
                        thread,
                    }
                    ctx.chatResponseFilters.forEach(f => f(ctxResponse))
                    console.debug('completion.response', JSON.stringify(response, null, 2))
                } catch (e) {
                    error = createErrorStatus(e.message)
                }
            }

            if (response?.error) {
                error ??= createErrorStatus()
                error.message = response.error
            }

            if (error) {
                ctx.chatErrorFilters.forEach(f => f(error))
                return new ApiResult({ error })
            }

            if (!error) {
                // Add tool history messages if any
                if (response.tool_history && Array.isArray(response.tool_history)) {
                    for (const msg of response.tool_history) {
                        if (msg.role === 'assistant') {
                            msg.model = model.name // tag with model
                        }
                    }
                }

                nextTick(addCopyButtons)

                return new ApiResult({ response })
            }
        } catch (e) {
            console.log('completion.error', e)
            return new ApiResult({ error: createErrorStatus(e.message, 'ChatFailed') })
        }
    }
    function getTextContent(chat) {
        const textMessage = chat.messages.find(m =>
            m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'text'))
        return textMessage?.content.find(c => c.type === 'text')?.text || ''
    }
    function getAnswer(response) {
        const textMessage = response.choices?.[0]?.message
        return textMessage?.content || ''
    }
    function selectAspectRatio(ratio) {
        const selectedAspectRatio = imageRatioSizes[ratio] || '1024×1024'
        console.log(`selectAspectRatio(${ratio})`, selectedAspectRatio)
        ctx.setState({ selectedAspectRatio })
    }

    async function sendUserMessage(text, { model, target, redirect = true } = {}) {
        ctx.clearError()

        if (!model) {
            model = getSelectedModel()
        }

        let content = createContent({ text, files: attachedFiles.value })

        let thread

        // Create thread if none exists
        if (target === "new" || !ctx.threads.currentThread.value) {
            thread = await ctx.threads.startNewThread({ redirect })
        } else {
            thread = ctx.threads.currentThread.value
        }

        let threadId = thread.id
        let messages = thread.messages || []
        if (!threadId) {
            console.error('No thread ID found', thread, ctx.threads.currentThread.value)
            return
        }

        // Handle Editing / Redo Logic
        // truncate tells the server this request intends to rewrite history; without it
        // a shrinking write is refused so an in-flight request can't erase the thread
        let truncate = false
        const editingMsg = editingMessage.value
        if (editingMsg) {
            let messageIndex = messages.findIndex(m => m.timestamp === editingMsg)
            if (messageIndex == -1) {
                messageIndex = messages.findLastIndex(m => m.role === 'user')
            }
            console.log('Editing message', editingMsg, messageIndex, messages)

            if (messageIndex >= 0) {
                messages[messageIndex].content = content
                // Truncate messages to only include up to the edited message
                messages.length = messageIndex + 1
                truncate = true
            } else {
                messages.push({
                    timestamp: new Date().valueOf(),
                    role: 'user',
                    content,
                })
            }
        } else {
            // Regular Send Logic
            const lastMessage = messages[messages.length - 1]

            // Check duplicate based on text content extracted from potential array
            const getLastText = (msgContent) => {
                if (typeof msgContent === 'string') return msgContent
                if (Array.isArray(msgContent)) return msgContent.find(c => c.type === 'text')?.text || ''
                return ''
            }
            const newText = text // content[0].text
            const lastText = lastMessage && lastMessage.role === 'user' ? getLastText(lastMessage.content) : null
            const isDuplicate = lastText === newText

            // Add user message only if it's not a duplicate
            // Note: We are saving the FULL STRUCTURED CONTENT array here
            if (!isDuplicate) {
                messages.push({
                    timestamp: new Date().valueOf(),
                    role: 'user',
                    content,
                })
            }
        }

        if (text === 'retry') {
            let lastMessage = messages[messages.length - 1]
            if (lastMessage.role === 'user' && lastMessage.content[0].text === 'retry') {
                messages.pop()
                // Also remove assistant message before it
                lastMessage = messages[messages.length - 1]
                if (lastMessage.role === 'assistant') {
                    messages.pop()
                }
                truncate = true
            }
        }

        const request = createRequest({ model })
        if (truncate) {
            request.truncate = true
        }

        // Add Thread History
        messages.forEach(m => {
            request.messages.push(m)
        })

        // Update Thread Title if not set or is default
        if (!thread.title || thread.title === 'New Chat' || request.title === 'New Chat') {
            request.title = text.length > 100
                ? text.slice(0, 100) + '...'
                : text
            console.debug(`changing thread title from '${thread.title}' to '${request.title}'`)
        } else {
            console.debug(`thread title is '${thread.title}'`, request.title)
        }

        const api = await ctx.threads.queueChat({ request, thread, model })
        if (api.response) {
            // success
            thread = api.response
            completeChat(thread)
        } else {
            ctx.setError(api.error)
        }
    }

    function completeChat(thread) {
        editingMessage.value = null
        attachedFiles.value = []
        ctx.threads.replaceThread(thread)
    }

    return {
        completion,
        createContent,
        createRequest,
        applySettings,
        promptHistory,
        messageText,
        attachedFiles,
        editingMessage,
        hasImage,
        hasAudio,
        hasFile,
        reset,
        settings,
        addCopyButtons,
        getModel,
        getSelectedModel,
        setSelectedModel,
        getProviderForModel,
        canGenerateImage,
        canGenerateAudio,
        getVoices,
        getTextContent,
        getAnswer,
        selectAspectRatio,
        sendUserMessage,
        completeChat,
    }
}

const VoiceInput = {
    template: `
        <button v-if="$state.config.extensions.includes('voice')" type="button" 
            ref="voiceBtn"
            @click="toggleRecording"
            :class="['absolute bottom-12 right-2 size-8 flex items-center justify-center rounded-full hover:shadow transition-colors',
                isRecording 
                    ? $styles.voiceButtonRecording
                    : isProcessing
                        ? $styles.voiceButtonProcessing
                        : $styles.voiceButtonDefault
            ]"
            :title="isProcessing ? 'Processing...' : 'Record voice (Alt+D)'"
        >
            <svg v-if="isProcessing" class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M21 12a9 9 0 1 1-6.219-8.56"></path>
            </svg>
            <svg v-else class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                <path d="M12 1a3 3 0 0 0-3 3v8a3 3 0 0 0 6 0V4a3 3 0 0 0-3-3z"></path>
                <path d="M19 10v2a7 7 0 0 1-14 0v-2"></path>
                <line x1="12" y1="19" x2="12" y2="23"></line>
                <line x1="8" y1="23" x2="16" y2="23"></line>
            </svg>
        </button>
    `,
    setup() {
        const ctx = inject('ctx')
        const voiceBtn = ref(null)
        const isRecording = ref(false)

        let mediaRecorder = null
        let audioChunks = []

        const isProcessing = ref(false)

        const stopRecording = () => {
            if (mediaRecorder && isRecording.value) {
                isProcessing.value = true
                // stop() only *queues* the flush of buffered audio, so the stream tracks
                // must not be stopped here - tearing the source down first races the flush
                // and yields an empty recording (a bare container header, no audio).
                // They're released in onstop below, once the data has been delivered.
                mediaRecorder.stop()
                isRecording.value = false
            }
        }

        // Convert a recording to 16 kHz mono 16-bit WAV using the browser's own decoder.
        // Every transcription API accepts WAV, whereas the webm/opus browsers record is
        // rejected by some (Mistral: "Audio input could not be decoded"), and this avoids
        // needing ffmpeg installed on the server. Returns null if the browser can't do it,
        // in which case the original recording is sent unchanged.
        const toWavBlob = async (blob) => {
            const OfflineCtx = window.OfflineAudioContext || window.webkitOfflineAudioContext
            if (!OfflineCtx || !blob?.size) return null
            const rate = 16000
            try {
                // Decode with an offline context: a real AudioContext would open the audio
                // output device, and its close() is async, so it can still be tearing down
                // when the next recording calls getUserMedia - which shows up as a delay
                // before recording starts. An offline context touches no hardware.
                const decoder = new OfflineCtx(1, 1, rate)
                const encoded = await blob.arrayBuffer()
                const decoded = await new Promise((resolve, reject) => {
                    // Safari's older implementation is callback-only
                    const p = decoder.decodeAudioData(encoded, resolve, reject)
                    if (p?.then) p.then(resolve, reject)
                })
                const frames = Math.max(1, Math.ceil(decoded.duration * rate))
                // rendering into a 1-channel context downmixes and resamples in one step
                const offline = new OfflineCtx(1, frames, rate)
                const source = offline.createBufferSource()
                source.buffer = decoded
                source.connect(offline.destination)
                source.start()
                const samples = (await offline.startRendering()).getChannelData(0)

                const bytes = new ArrayBuffer(44 + samples.length * 2)
                const view = new DataView(bytes)
                const ascii = (offset, text) => {
                    for (let i = 0; i < text.length; i++) view.setUint8(offset + i, text.charCodeAt(i))
                }
                ascii(0, 'RIFF')
                view.setUint32(4, 36 + samples.length * 2, true)
                ascii(8, 'WAVE')
                ascii(12, 'fmt ')
                view.setUint32(16, 16, true)        // PCM header size
                view.setUint16(20, 1, true)         // format = PCM
                view.setUint16(22, 1, true)         // mono
                view.setUint32(24, rate, true)
                view.setUint32(28, rate * 2, true)  // byte rate
                view.setUint16(32, 2, true)         // block align
                view.setUint16(34, 16, true)        // bits per sample
                ascii(36, 'data')
                view.setUint32(40, samples.length * 2, true)
                for (let i = 0, offset = 44; i < samples.length; i++, offset += 2) {
                    const s = Math.max(-1, Math.min(1, samples[i]))
                    view.setInt16(offset, s < 0 ? s * 0x8000 : s * 0x7fff, true)
                }
                return new Blob([bytes], { type: 'audio/wav' })
            } catch (e) {
                console.debug('WAV conversion unavailable, sending the original recording', e)
                return null
            }
        }

        const startRecording = async () => {
            if (isProcessing.value) return
            try {
                const requestedAt = performance.now()
                const stream = await navigator.mediaDevices.getUserMedia({ audio: true })
                console.debug(`microphone acquired in ${Math.round(performance.now() - requestedAt)}ms`)
                recordingStartTime.value = Date.now()
                mediaRecorder = new MediaRecorder(stream)
                audioChunks = []
                mediaRecorder.ondataavailable = event => {
                    audioChunks.push(event.data)
                }
                mediaRecorder.onstop = async () => {
                    // safe to release the microphone now the recorder has flushed
                    mediaRecorder.stream.getTracks().forEach(track => track.stop())

                    // Ignore recordings less than 1 second - likely unintentional
                    const recordingDuration = Date.now() - recordingStartTime.value
                    if (recordingDuration < 1000) {
                        console.debug(`Recording too short (${recordingDuration}ms), ignoring`)
                        isProcessing.value = false
                        return
                    }
                    // MediaRecorder's default differs by browser (webm/opus in Chrome,
                    // mp4/aac in Safari) so take the type it actually produced
                    const recordedType = mediaRecorder.mimeType || 'audio/webm'
                    const recordedExt = recordedType.includes('mp4') ? 'mp4'
                        : recordedType.includes('ogg') ? 'ogg' : 'webm'
                    let audioBlob = new Blob(audioChunks, { type: recordedType })
                    let fileName = `voice-${Date.now()}.${recordedExt}`

                    // A recording of at least a second that's this small contains no audio
                    // frames, only a container header - uploading it just yields a confusing
                    // "could not be decoded" error from the transcription provider.
                    if (audioBlob.size < 1024) {
                        console.debug(`Recording contained no audio (${audioBlob.size} bytes)`)
                        ctx.setError({ errorCode: 'Error',
                            message: 'No audio was captured - check your microphone is not muted, then try again' })
                        isProcessing.value = false
                        return
                    }

                    const convertedAt = performance.now()
                    const wavBlob = await toWavBlob(audioBlob)
                    console.debug(`converted ${audioBlob.size}b ${recordedType} to wav in ` +
                        `${Math.round(performance.now() - convertedAt)}ms`)
                    if (wavBlob) {
                        audioBlob = wavBlob
                        fileName = `voice-${Date.now()}.wav`
                    }

                    const audioFile = new File([audioBlob], fileName, { type: audioBlob.type })
                    const formData = new FormData()
                    formData.append('file', audioFile)
                    const res = await ctx.postForm('/transcribe', {
                        body: formData
                    })
                    const api = await ctx.createJsonResult(res)
                    if (api.response) {
                        if (ctx.chat.messageText.value) {
                            const lastChar = ctx.chat.messageText.value.slice(-1)
                            ctx.chat.messageText.value += (lastChar === ' ' || lastChar === '\n' ? '' : ' ') + api.response.text
                        } else {
                            ctx.chat.messageText.value = api.response.text || ''
                        }
                        document.getElementById('messageText')?.focus()
                    } else {
                        ctx.setError(api.error)
                    }
                    isProcessing.value = false
                }
                mediaRecorder.start()
                isRecording.value = true
                // Focus the voice button to prevent textarea keyboard interference
                voiceBtn.value?.focus()
            } catch (err) {
                ctx.setError({ message: "Error accessing microphone: " + err.message })
                isProcessing.value = false
            }
        }

        const toggleRecording = () => {
            if (isProcessing.value) return
            if (isRecording.value) {
                stopRecording()
            } else {
                startRecording()
            }
        }

        const triggeredByKey = ref(false)
        const recordingStartTime = ref(0)

        const onWindowKeyDown = (e) => {
            if (e.altKey && (e.key === 'd' || e.code === 'KeyD') && !e.repeat) {
                e.preventDefault()

                if (isProcessing.value) return

                // Already triggered by key, ignore until key is released
                if (triggeredByKey.value) return

                if (!isRecording.value) {
                    // Start Recording (Press)
                    triggeredByKey.value = true
                    recordingStartTime.value = Date.now()
                    startRecording()
                } else if (!triggeredByKey.value) {
                    // Already recording but NOT by holding key (i.e. was toggled on)
                    // Treat this press as a Toggle OFF command
                    stopRecording()
                }
            }
        }

        const onWindowKeyUp = (e) => {
            if (triggeredByKey.value && (e.key === 'd' || e.code === 'KeyD' || e.key === 'Alt')) {
                // If either key is released, check duration
                // We use a small threshold to distinguish between a tap (toggle) and a hold (PTT)
                const duration = Date.now() - recordingStartTime.value
                if (duration < 500) {
                    // Short press -> Treated as Toggle ON. 
                    // Do NOT stop recording.
                    // Clear triggeredByKey so subsequent key events treat it as toggled state.
                    triggeredByKey.value = false
                } else {
                    // Long press -> Treated as Push-to-Talk.
                    // Stop recording immediately on release.
                    stopRecording()
                    triggeredByKey.value = false
                }
            }
        }

        onMounted(() => {
            document.addEventListener('keydown', onWindowKeyDown)
            document.addEventListener('keyup', onWindowKeyUp)
        })

        onUnmounted(() => {
            document.removeEventListener('keydown', onWindowKeyDown)
            document.removeEventListener('keyup', onWindowKeyUp)
        })

        return {
            voiceBtn,
            isProcessing,
            isRecording,
            toggleRecording,
        }
    }
}

const ChatPrompt = {
    template: `
    <div class="mx-auto max-w-3xl">
        <SettingsDialog :isOpen="showSettings" @close="showSettings = false" />
        <div class="flex space-x-2">
            <!-- Attach (+) button and Settings button -->
            <div class="mt-1.5 flex flex-col space-y-1 items-center">
                <div>
                    <button type="button"
                            @click="triggerFilePicker"
                            :disabled="$threads.isWatchingThread.value || !model"
                            class="size-8 flex items-center justify-center rounded-md disabled:cursor-not-allowed"
                            :class="$styles.chatButton"
                            title="Attach image or audio">
                        <svg class="size-5" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 256 256">
                            <path d="M224,128a8,8,0,0,1-8,8H136v80a8,8,0,0,1-16,0V136H40a8,8,0,0,1,0-16h80V40a8,8,0,0,1,16,0v80h80A8,8,0,0,1,224,128Z"></path>
                        </svg>
                    </button>
                    <!-- Hidden file input -->
                    <input ref="fileInput" type="file" multiple @change="onFilesSelected"
                        class="hidden" accept="image/*,audio/*,.pdf,.doc,.docx,.xml,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                        />
                </div>
                <div>
                    <button type="button" title="Settings" @click="showSettings = true"
                        :disabled="$threads.watchingThread || !model"
                        class="size-8 flex items-center justify-center rounded-md disabled:cursor-not-allowed"
                        :class="$styles.chatButton">
                        <svg class="size-4" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 256 256"><path d="M40,88H73a32,32,0,0,0,62,0h81a8,8,0,0,0,0-16H135a32,32,0,0,0-62,0H40a8,8,0,0,0,0,16Zm64-24A16,16,0,1,1,88,80,16,16,0,0,1,104,64ZM216,168H199a32,32,0,0,0-62,0H40a8,8,0,0,0,0,16h97a32,32,0,0,0,62,0h17a8,8,0,0,0,0-16Zm-48,24a16,16,0,1,1,16-16A16,16,0,0,1,168,192Z"></path></svg>
                    </button>
                </div>
            </div>

            <div class="flex-1">
                <div class="relative">
                    <textarea id="messageText"
                        ref="refMessage"
                        v-model="messageText"
                        @keydown="onKeyDown"
                        @keydown.enter.exact.prevent="sendMessage"
                        @keydown.enter.shift.exact="addNewLine"
                        @paste="onPaste"
                        @dragover="onDragOver"
                        @dragleave="onDragLeave"
                        @drop="onDrop"
                        placeholder="Type message... (Enter to send, Shift+Enter for new line, drag & drop or paste files)"
                        :class="[
                            'h-22 block w-full rounded-md border px-3 py-2 pr-12 text-sm focus:outline-none focus:ring-1 ' + $styles.textInput + ' ' + $styles.bgInput,
                            isDragging
                                ? $styles.draggingInput
                                : $styles.borderInput
                        ]"
                        :disabled="$threads.watchingThread || !model"
                    ></textarea>
                    <VoiceInput />
                    <button v-if="!$threads.watchingThread" title="Send (Enter)" type="button"
                        @click="sendMessage"
                        :disabled="!messageText.trim() || $threads.watchingThread || !model"
                        class="absolute bottom-2 right-2 size-8 flex items-center justify-center rounded-md"
                        :class="$styles.chatButton">
                        <svg class="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path stroke-dasharray="20" stroke-dashoffset="20" d="M12 21l0 -17.5"><animate fill="freeze" attributeName="stroke-dashoffset" dur="0.2s" values="20;0"/></path><path stroke-dasharray="12" stroke-dashoffset="12" d="M12 3l7 7M12 3l-7 7"><animate fill="freeze" attributeName="stroke-dashoffset" begin="0.2s" dur="0.2s" values="12;0"/></path></g></svg>
                    </button>
                    <button v-else title="Cancel request" type="button"
                        @click="$threads.cancelThread()"
                        class="absolute bottom-2 right-2 size-8 flex items-center justify-center rounded-md border border-red-300 dark:border-red-600 text-red-600 dark:text-red-400 hover:bg-red-50 dark:hover:bg-red-900/30 transition-colors">
                        <svg class="size-5" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
                            <rect x="3" y="3" width="18" height="18" rx="2" ry="2"></rect>
                        </svg>
                    </button>
                </div>

                <!-- Attachments & Image Options -->
                <div class="mt-2 flex justify-between items-start gap-2">
                    <div class="flex flex-wrap gap-2">
                        <div v-for="(f,i) in $chat.attachedFiles.value" :key="i" class="flex items-center gap-2 px-2 py-1 text-xs rounded-md border" :class="[$styles.tagLabel]">
                            <span class="truncate max-w-48" :title="f.name">{{ f.name }}</span>
                            <button type="button" :class="[$styles.icon, $styles.iconHover]" @click="removeAttachment(i)" title="Remove Attachment">
                                <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                            </button>
                        </div>
                    </div>

                    <div class="flex gap-x-2">
                        <!-- Image Aspect Ratio Selector -->
                        <div v-if="$chat.canGenerateImage(model)">
                            <select name="aspect_ratio" v-model="$state.selectedAspectRatio" 
                                    class="block w-full rounded-md pl-2 pr-6 py-1 text-xs" :class="[$styles.textInput, $styles.bgInput, $styles.borderInput]">
                                <option v-for="(ratio, size) in imageAspectRatios" :key="size" :value="size">
                                    {{ ratio }}
                                </option>
                            </select>
                        </div>

                        <!-- Voice Selector -->
                        <div v-if="$chat.getVoices(model).length">
                            <select name="voice" v-model="$state.selectedVoice" 
                                    class="block w-full rounded-md pl-2 pr-6 py-1 text-xs" :class="[$styles.textInput, $styles.bgInput, $styles.borderInput]">
                                <option v-for="(voice, idx) in $chat.getVoices(model)" :key="idx" :value="voice">
                                    {{ voice }}
                                </option>
                            </select>
                        </div>
                    </div>
                </div>

                <div v-if="!model" class="mt-2 text-sm text-red-600 dark:text-red-400">
                    Please select a model
                </div>
            </div>
        </div>
    </div>    
    `,
    props: {
        model: {
            type: Object,
            default: null
        }
    },
    setup(props) {
        const ctx = inject('ctx')
        const config = ctx.state.config
        const {
            messageText,
            promptHistory,
            hasImage,
            hasAudio,
            hasFile,
            getTextContent,
            sendUserMessage,
        } = ctx.chat

        const fileInput = ref(null)
        const refMessage = ref(null)
        const showSettings = ref(false)
        const historyIndex = ref(-1)
        const isNavigatingHistory = ref(false)

        const uploadFiles = async (files) => {
            if (files.length) {
                // Upload files immediately
                const uploadedFiles = await Promise.all(files.map(async f => {
                    try {
                        const response = await ctx.ai.uploadFile(f)
                        const metadata = {
                            url: response.url,
                            name: f.name,
                            size: response.size,
                            type: f.type,
                            width: response.width,
                            height: response.height,
                            threadId: ctx.threads.currentThread.value?.id,
                            created: Date.now()
                        }

                        return {
                            ...metadata,
                            file: f // Keep original file for preview/fallback if needed
                        }
                    } catch (error) {
                        ctx.setError({
                            errorCode: 'Upload Failed',
                            message: `Failed to upload ${f.name}: ${error.message}`
                        })
                        return null
                    }
                }))

                ctx.chat.attachedFiles.value.push(...uploadedFiles.filter(f => f))
            }
        }

        // File attachments (+) handlers
        const triggerFilePicker = () => {
            if (fileInput.value) fileInput.value.click()
        }
        const onFilesSelected = async (e, { populateImagePrompt = true } = {}) => {
            const files = Array.from(e.target?.files || [])
            await uploadFiles(files)


            // allow re-selecting the same file
            if (fileInput.value) fileInput.value.value = ''

            if (!messageText.value?.trim()) {
                if (hasImage()) {
                    if (populateImagePrompt) {
                        messageText.value = getTextContent(config.defaults.image)
                    }
                } else if (hasAudio()) {
                    messageText.value = getTextContent(config.defaults.audio)
                } else {
                    messageText.value = getTextContent(config.defaults.file)
                }
            }
        }
        const removeAttachment = (i) => {
            ctx.chat.attachedFiles.value.splice(i, 1)
        }

        // Handle paste events for clipboard images, audio, and files
        const onPaste = async (e) => {
            // Use the paste event's clipboardData directly (works best for paste events)
            const items = e.clipboardData?.items
            if (!items) return

            const files = []

            // Check all clipboard items
            for (let i = 0; i < items.length; i++) {
                const item = items[i]

                // Handle files (images, audio, etc.)
                if (item.kind === 'file') {
                    const file = item.getAsFile()
                    if (file) {
                        // Generate a better filename based on type
                        let filename = file.name
                        if (!filename || filename === 'image.png' || filename === 'blob') {
                            const ext = file.type.split('/')[1] || 'png'
                            const timestamp = new Date().getTime()
                            if (file.type.startsWith('image/')) {
                                filename = `pasted-image-${timestamp}.${ext}`
                            } else if (file.type.startsWith('audio/')) {
                                filename = `pasted-audio-${timestamp}.${ext}`
                            } else {
                                filename = `pasted-file-${timestamp}.${ext}`
                            }
                            // Create a new File object with the better name
                            files.push(new File([file], filename, { type: file.type }))
                        } else {
                            files.push(file)
                        }
                    }
                }
            }

            if (files.length > 0) {
                e.preventDefault()
                // Reuse the same logic as onFilesSelected for consistency
                const event = { target: { files: files } }
                const threadIsEmpty = !(ctx.threads.currentThread.value?.messages?.length)
                const pastedImage = files.some(file => file.type?.startsWith('image/'))
                await onFilesSelected(event, {
                    populateImagePrompt: !pastedImage || threadIsEmpty,
                })
            }
        }

        // Handle drag and drop events
        const isDragging = ref(false)

        const onDragOver = (e) => {
            e.preventDefault()
            e.stopPropagation()
            isDragging.value = true
        }

        const onDragLeave = (e) => {
            e.preventDefault()
            e.stopPropagation()
            isDragging.value = false
        }

        const onDrop = async (e) => {
            e.preventDefault()
            e.stopPropagation()
            isDragging.value = false

            const files = Array.from(e.dataTransfer?.files || [])
            if (files.length > 0) {
                // Reuse the same logic as onFilesSelected for consistency
                const event = { target: { files: files } }
                await onFilesSelected(event)
            }
        }

        // Send message
        const sendMessage = async () => {
            if (!messageText.value?.trim() && !hasImage() && !hasAudio() && !hasFile()) return
            if (ctx.threads.isWatchingThread.value || !props.model) return

            // 1. Construct Structured Content (Text + Attachments)
            let text = messageText.value.trim()

            if (text) {
                const idx = promptHistory.value.indexOf(text)
                if (idx !== -1) {
                    promptHistory.value.splice(idx, 1)
                }
                promptHistory.value.push(text)
            }

            messageText.value = ''

            await sendUserMessage(text, { model: props.model })

            // Restore focus to the textarea
            nextTick(() => {
                refMessage.value?.focus()
            })
        }

        const addNewLine = () => {
            // Enter key already adds new line
            //messageText.value += '\n'
        }

        const onKeyDown = (e) => {
            if (e.key === 'ArrowUp') {
                if (refMessage.value.selectionStart === 0 && refMessage.value.selectionEnd === 0) {
                    if (promptHistory.value.length > 0) {
                        e.preventDefault()
                        if (historyIndex.value === -1) {
                            historyIndex.value = promptHistory.value.length - 1
                        } else {
                            historyIndex.value = Math.max(0, historyIndex.value - 1)
                        }
                        isNavigatingHistory.value = true
                        messageText.value = promptHistory.value[historyIndex.value]
                        nextTick(() => {
                            refMessage.value.setSelectionRange(0, 0)
                        })
                    }
                }
            } else if (e.key === 'ArrowDown') {
                if (historyIndex.value !== -1) {
                    e.preventDefault()
                    if (historyIndex.value < promptHistory.value.length - 1) {
                        historyIndex.value++
                        isNavigatingHistory.value = true
                        messageText.value = promptHistory.value[historyIndex.value]
                    } else {
                        historyIndex.value = -1
                        isNavigatingHistory.value = true
                        messageText.value = ''
                    }
                    nextTick(() => {
                        refMessage.value.setSelectionRange(0, 0)
                    })
                }
            }
        }

        watch(messageText, (newValue) => {
            if (!isNavigatingHistory.value) {
                historyIndex.value = -1
            }
            isNavigatingHistory.value = false
        })

        watch(() => ctx.state.selectedAspectRatio, newValue => {
            ctx.setPrefs({ aspectRatio: newValue })
        })

        watch(() => ctx.layout.path, newValue => {
            if (newValue === '/' || newValue.startsWith('/c/')) {
                nextTick(() => {
                    refMessage.value?.focus()
                })
            }
        })

        return {
            messageText,
            fileInput,
            refMessage,
            showSettings,
            isDragging,
            triggerFilePicker,
            onFilesSelected,
            onPaste,
            onDragOver,
            onDragLeave,
            onDrop,
            removeAttachment,
            sendMessage,
            addNewLine,
            onKeyDown,
            imageAspectRatios,
            sendUserMessage,
        }
    }
}

const ThemeButton = {
    template: `
        <div :class="['rounded-xl overflow-hidden border-2 transition-all duration-200 hover:scale-[1.02] hover:shadow-lg', preview.bgBody, preview.chromeBorder]" :title="JSON.stringify(preview, undefined, 2)">
            <button type="button" :key="id" @click="$emit('select', id)"
                class="flex w-full text-left">
                
                <div class="w-14 flex items-center justify-center border-r"
                    :class="[preview.bgSidebar, preview.icon, preview.chromeBorder]">
                    <svg xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M17.5 12a1.5 1.5 0 0 1-1.5-1.5A1.5 1.5 0 0 1 17.5 9a1.5 1.5 0 0 1 1.5 1.5a1.5 1.5 0 0 1-1.5 1.5m-3-4A1.5 1.5 0 0 1 13 6.5A1.5 1.5 0 0 1 14.5 5A1.5 1.5 0 0 1 16 6.5A1.5 1.5 0 0 1 14.5 8m-5 0A1.5 1.5 0 0 1 8 6.5A1.5 1.5 0 0 1 9.5 5A1.5 1.5 0 0 1 11 6.5A1.5 1.5 0 0 1 9.5 8m-3 4A1.5 1.5 0 0 1 5 10.5A1.5 1.5 0 0 1 6.5 9A1.5 1.5 0 0 1 8 10.5A1.5 1.5 0 0 1 6.5 12M12 3a9 9 0 0 0-9 9a9 9 0 0 0 9 9a1.5 1.5 0 0 0 1.5-1.5c0-.39-.15-.74-.39-1c-.23-.27-.38-.62-.38-1a1.5 1.5 0 0 1 1.5-1.5H16a5 5 0 0 0 5-5c0-4.42-4.03-8-9-8"/></svg>
                </div>
                
                <div class="flex-1 flex items-center px-4 py-3 text-sm font-medium"
                    :class="[preview.bgBody, preview.heading]">
                    {{ name }}
                </div>
            </button>
        </div>
    `,
    props: {
        id: String,
        theme: Object
    },
    setup(props) {
        const ctx = inject('ctx')
        const name = computed(() => ctx.utils.idToName(props.id))
        const preview = computed(() => props.theme.preview)

        return {
            name,
            preview,
        }
    }
}

const ThemeSelector = {
    template: `
    <div v-if="$state.themes" class="relative w-64 text-left select-none" ref="menuContainer">
        <button type="button" @click.stop="toggleMenu"
            class="flex w-full items-center justify-between rounded-lg px-4 py-2 border shadow-sm transition-colors"
            :class="[$styles.dropdownButton, $styles.chromeBorder]">
            <span class="flex items-center">
                <svg class="mr-2 h-5 w-5" :class="$styles.icon" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><path fill="currentColor" d="M17.5 12a1.5 1.5 0 0 1-1.5-1.5A1.5 1.5 0 0 1 17.5 9a1.5 1.5 0 0 1 1.5 1.5a1.5 1.5 0 0 1-1.5 1.5m-3-4A1.5 1.5 0 0 1 13 6.5A1.5 1.5 0 0 1 14.5 5A1.5 1.5 0 0 1 16 6.5A1.5 1.5 0 0 1 14.5 8m-5 0A1.5 1.5 0 0 1 8 6.5A1.5 1.5 0 0 1 9.5 5A1.5 1.5 0 0 1 11 6.5A1.5 1.5 0 0 1 9.5 8m-3 4A1.5 1.5 0 0 1 5 10.5A1.5 1.5 0 0 1 6.5 9A1.5 1.5 0 0 1 8 10.5A1.5 1.5 0 0 1 6.5 12M12 3a9 9 0 0 0-9 9a9 9 0 0 0 9 9a1.5 1.5 0 0 0 1.5-1.5c0-.39-.15-.74-.39-1c-.23-.27-.38-.62-.38-1a1.5 1.5 0 0 1 1.5-1.5H16a5 5 0 0 0 5-5c0-4.42-4.03-8-9-8"/></svg>
                <span class="font-medium" :class="$styles.heading">{{ $utils.idToName($ctx.selectedTheme) || 'Select Theme' }}</span>
            </span>
            <svg class="h-5 w-5 opacity-70" :class="$styles.icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20" fill="currentColor">
                <path fill-rule="evenodd" d="M5.293 7.293a1 1 0 011.414 0L10 10.586l3.293-3.293a1 1 0 111.414 1.414l-4 4a1 1 0 01-1.414 0l-4-4a1 1 0 010-1.414z" clip-rule="evenodd" />
            </svg>
        </button>

        <div v-if="showMenu"
            @click.stop
            class="absolute right-[-9rem] z-50 mt-2 w-[34rem] origin-top-right rounded-lg focus:outline-none"
            role="menu" aria-orientation="vertical" tabindex="-1">
            
            <div class="max-h-96 overflow-y-auto w-full p-4 bg-gray-100/90 dark:bg-gray-800/90 backdrop-blur-md rounded-xl shadow-2xl border border-gray-200 dark:border-gray-700">
                <div class="grid grid-cols-2 gap-6 w-full">
                    <!-- Light Themes Column -->
                    <div class="flex flex-col space-y-3">
                        <div class="text-xs font-bold tracking-wider uppercase px-1" :class="$styles.muted">Light Themes</div>
                        <template v-for="(theme, id) in lightThemes" :key="id">
                            <ThemeButton :id="id" :theme="theme" @select="selectTheme" />
                        </template>
                    </div>

                    <!-- Dark Themes Column -->
                    <div class="flex flex-col space-y-3">
                        <div class="text-xs font-bold tracking-wider uppercase px-1" :class="$styles.muted">Dark Themes</div>
                        <template v-for="(theme, id) in darkThemes" :key="id">
                            <ThemeButton :id="id" :theme="theme" @select="selectTheme" />
                        </template>
                    </div>
                </div>
            </div>    
            
        </div>
    </div>
    `,
    setup() {
        const ctx = inject('ctx')
        const showMenu = ref(false)
        const menuContainer = ref(null)
        const fullThemes = computed(() => ctx.resolveThemes(ctx.state.themes) || {})

        const lightThemes = computed(() => {
            const themes = {}
            const sortedEntries = Object.entries(fullThemes.value).sort((a, b) => {
                const idA = a[0]
                const idB = b[0]
                if (idA === 'light') return -1
                if (idB === 'light') return 1

                const nameA = (ctx.utils.idToName(idA) || '').toLowerCase()
                const nameB = (ctx.utils.idToName(idB) || '').toLowerCase()
                return nameA.localeCompare(nameB)
            })
            for (const [id, theme] of sortedEntries) {
                if (theme.vars.colorScheme !== 'dark') {
                    themes[id] = theme
                }
            }
            return themes
        })

        const darkThemes = computed(() => {
            const themes = {}
            const sortedEntries = Object.entries(fullThemes.value).sort((a, b) => {
                const idA = a[0]
                const idB = b[0]
                if (idA === 'dark') return -1
                if (idB === 'dark') return 1

                const nameA = (ctx.utils.idToName(idA) || '').toLowerCase()
                const nameB = (ctx.utils.idToName(idB) || '').toLowerCase()
                return nameA.localeCompare(nameB)
            })
            for (const [id, theme] of sortedEntries) {
                if (theme.vars.colorScheme === 'dark') {
                    themes[id] = theme
                }
            }
            return themes
        })

        function toggleMenu() {
            showMenu.value = !showMenu.value
        }

        function selectTheme(id) {
            ctx.selectTheme(id)
            showMenu.value = false
        }

        const handleClickOutside = (event) => {
            if (showMenu.value && menuContainer.value && !menuContainer.value.contains(event.target)) {
                showMenu.value = false
            }
        }

        onMounted(() => {
            document.addEventListener('click', handleClickOutside)
        })

        onUnmounted(() => {
            document.removeEventListener('click', handleClickOutside)
        })

        return {
            showMenu,
            menuContainer,
            lightThemes,
            darkThemes,
            toggleMenu,
            selectTheme
        }
    }
}

const HomeTools = {
    template: `
        <div class="mt-4 flex space-x-3 justify-center items-center">
            <ThemeSelector />
        </div>
    `,
}

const ThreadHeader = {
    template: `
    <div v-if="showComponents.length" class="flex items-center justify-center gap-2">
        <div v-for="component in showComponents">
            <component :is="component" :thread="thread" />
        </div>
    </div>
    `,
    props: { thread: Object },
    setup(props) {
        const ctx = inject('ctx')
        const showComponents = computed(() => {
            const args = { thread: props.thread }
            return Object.values(ctx.threadHeaderComponents).filter(def => def.show(args)).map(def => def.component)
        })
        return {
            showComponents,
        }
    }
}

const ThreadFooter = {
    template: `
    <div v-if="showComponents.length">
        <div v-for="component in showComponents">
            <component :is="component" :thread="thread" />
        </div>
    </div>
    `,
    props: { thread: Object },
    setup(props) {
        const ctx = inject('ctx')
        const showComponents = computed(() => {
            const args = { thread: props.thread }
            return Object.values(ctx.threadFooterComponents).filter(def => def.show(args)).map(def => def.component)
        })
        return {
            showComponents,
        }
    }
}

const MessageFooter = {
    template: `
    <div v-if="showComponents.length">
        <div v-for="component in showComponents">
            <component :is="component" :thread="thread" :message="message" />
        </div>
    </div>
    `,
    props: { thread: Object, message: Object },
    setup(props) {
        const ctx = inject('ctx')
        const showComponents = computed(() => {
            const args = { thread: props.thread, message: props.message }
            return Object.values(ctx.messageFooterComponents).filter(def => def.show(args)).map(def => def.component)
        })
        return {
            showComponents,
        }
    }
}

const ThreadModel = {
    template: `
    <span @click="$chat.setSelectedModel({ name: thread.model})" 
        class="flex items-center cursor-pointer px-1.5 py-0.5 text-xs rounded transition-colors" :class="[$styles.tagLabel, $styles.tagLabelHover]">
        <ProviderIcon class="size-4 mr-1" :provider="$chat.getProviderForModel(thread.model)" />
        {{thread.model}}
    </span>
    `,
    props: { thread: Object },
}

const ThreadTools = {
    template: `
    <div class="text-sm flex items-center gap-1 flex items-center px-1.5 py-0.5 text-xs rounded cursor-help" :class="[$styles.tagLabel]" :title="title">
        <svg class="size-4" :class="[$styles.icon]" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M7 10h3V7L6.5 3.5a6 6 0 0 1 8 8l6 6a2 2 0 0 1-3 3l-6-6a6 6 0 0 1-8-8z"/></svg>
        <span v-if="toolFns.length==1">{{toolFns[0]?.function?.name || ''}}</span>
        <span v-else-if="toolFns.length>1">{{toolFns.length}} Tools</span>
    </div>
    `,
    props: { thread: Object },
    setup(props) {
        const toolFns = computed(() => (props.thread?.tools || []).filter(x => x && x.type === 'function'))
        const title = computed(() => toolFns.value.length == 1
            ? toolFns.value[0]?.function?.name || ''
            : toolFns.value.length > 1
                ? toolFns.value.map(x => x?.function?.name || '').join('\n')
                : '')
        return {
            toolFns,
            title,
        }
    }
}

export default {
    /**@param {AppContext} ctx */
    install(ctx) {
        const Home = ChatBody
        ctx.components({
            SettingsDialog,
            ChatPrompt,
            VoiceInput,
            ErrorBubble,

            ChatBody,
            MessageUsage,
            MessageReasoning,
            LightboxImage,
            TypeText,
            TypeImage,
            TypeAudio,
            TypeFile,
            ViewType,
            ViewTypes,
            ViewToolTypes,
            TextViewer,
            ToolCall,
            ToolArguments,
            ToolOutput,
            CompactThreadButton,

            HomeTools,
            Home,
            ThemeSelector,
            ThemeButton,
            ThreadHeader,
            ThreadFooter,
            MessageFooter,
            UserAvatar,
            AgentAvatar,
            CodeBlock,
            JsonPreview,
        })
        ctx.setGlobals({
            chat: useChatPrompt(ctx)
        })

        ctx.setLeftIcons({
            chat: {
                component: {
                    template: `<svg @click="$ctx.togglePath($ctx.layout.path?.startsWith('/c/') ? $ctx.layout.path : '/')" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16"><path fill="currentColor" d="M8 2.19c3.13 0 5.68 2.25 5.68 5s-2.55 5-5.68 5a5.7 5.7 0 0 1-1.89-.29l-.75-.26l-.56.56a14 14 0 0 1-2 1.55a.13.13 0 0 1-.07 0v-.06a6.58 6.58 0 0 0 .15-4.29a5.25 5.25 0 0 1-.55-2.16c0-2.77 2.55-5 5.68-5M8 .94c-3.83 0-6.93 2.81-6.93 6.27a6.4 6.4 0 0 0 .64 2.64a5.53 5.53 0 0 1-.18 3.48a1.32 1.32 0 0 0 2 1.5a15 15 0 0 0 2.16-1.71a6.8 6.8 0 0 0 2.31.36c3.83 0 6.93-2.81 6.93-6.27S11.83.94 8 .94"/><ellipse cx="5.2" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/><ellipse cx="10.8" cy="7.7" fill="currentColor" rx=".8" ry=".75"/></svg>`,
                },
                isActive({ path }) {
                    return path === '/' || ctx.matchesPath(path, '/c/*')
                }
            }
        })

        const title = 'Chat'
        ctx.setState({
            title
        })

        const meta = { title }
        ctx.routes.push(...[
            { path: '/', component: Home, meta },
            { path: '/c/:id', component: ChatBody, meta },
        ])

        ctx.setThreadHeaders({
            model: {
                component: ThreadModel,
                show({ thread }) { return thread.model }
            },
            tools: {
                component: ThreadTools,
                show({ thread }) { return (thread.tools || []).filter(x => x.type === 'function').length }
            }
        })

        const prefs = ctx.getPrefs()
        if (prefs.model) {
            ctx.state.selectedModel = prefs.model
        }
        ctx.setState({
            selectedAspectRatio: prefs.aspectRatio || '1:1',
        })
    }
}
