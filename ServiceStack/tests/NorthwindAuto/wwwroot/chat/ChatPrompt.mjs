import { ref, nextTick, inject, unref } from 'vue'
import { useRouter } from 'vue-router'
import { lastRightPart } from '@servicestack/client'
import { deepClone, fileToDataUri, fileToBase64, addCopyButtons, toModelInfo, tokenCost } from './utils.mjs'

const imageExts = 'png,webp,jpg,jpeg,gif,bmp,svg,tiff,ico'.split(',')
const audioExts = 'mp3,wav,ogg,flac,m4a,opus,webm'.split(',')

export function useChatPrompt() {
    const messageText = ref('')
    const attachedFiles = ref([])
    const isGenerating = ref(false)
    const errorStatus = ref(null)
    const hasImage = () => attachedFiles.value.some(f => imageExts.includes(lastRightPart(f.name, '.')))
    const hasAudio = () => attachedFiles.value.some(f => audioExts.includes(lastRightPart(f.name, '.')))
    const hasFile = () => attachedFiles.value.length > 0
    // const hasText = () => !hasImage() && !hasAudio() && !hasFile()

    function reset() {
        // Ensure initial state is ready to accept input
        isGenerating.value = false
        attachedFiles.value = []
        messageText.value = ''
    }

    return {
        messageText,
        attachedFiles,
        errorStatus,
        isGenerating,
        get generating() {
            return isGenerating.value
        },
        hasImage,
        hasAudio,
        hasFile,
        // hasText,
        reset,
    }
}

export default {
    template:`
    <div class="mx-auto max-w-3xl">
        <SettingsDialog :isOpen="showSettings" @close="showSettings = false" />
        <div class="flex space-x-2">
            <!-- Attach (+) button and Settings button -->
            <div class="mt-1.5 flex flex-col space-y-1 items-center">
                <div>
                    <button type="button"
                            @click="triggerFilePicker"
                            :disabled="isGenerating || !model"
                            class="size-8 flex items-center justify-center rounded-md border border-gray-300 text-gray-600 hover:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed"
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
                        :disabled="isGenerating || !model"
                        class="size-8 flex items-center justify-center rounded-md border border-gray-300 text-gray-600 hover:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed">
                        <svg class="size-4 text-gray-600 disabled:text-gray-400" xmlns="http://www.w3.org/2000/svg" fill="currentColor" viewBox="0 0 256 256"><path d="M40,88H73a32,32,0,0,0,62,0h81a8,8,0,0,0,0-16H135a32,32,0,0,0-62,0H40a8,8,0,0,0,0,16Zm64-24A16,16,0,1,1,88,80,16,16,0,0,1,104,64ZM216,168H199a32,32,0,0,0-62,0H40a8,8,0,0,0,0,16h97a32,32,0,0,0,62,0h17a8,8,0,0,0,0-16Zm-48,24a16,16,0,1,1,16-16A16,16,0,0,1,168,192Z"></path></svg>
                    </button>
                </div>
            </div>

            <div class="flex-1">
                <div class="relative">
                    <textarea
                        ref="messageInput"
                        v-model="messageText"
                        @keydown.enter.exact.prevent="sendMessage"
                        @keydown.enter.shift.exact="addNewLine"
                        placeholder="Type your message... (Enter to send, Shift+Enter for new line)"
                        rows="3"
                        class="block w-full rounded-md border border-gray-300 px-3 py-2 pr-12 text-sm placeholder-gray-500 focus:border-blue-500 focus:outline-none focus:ring-1 focus:ring-blue-500"
                        :disabled="isGenerating || !model"
                    ></textarea>
                    <button title="Send (Enter)" type="button"
                        @click="sendMessage"
                        :disabled="!messageText.trim() || isGenerating || !model"
                        class="absolute bottom-2 right-2 size-8 flex items-center justify-center rounded-md border border-gray-300 text-gray-600 hover:bg-gray-50 disabled:text-gray-400 disabled:cursor-not-allowed disabled:border-gray-200 transition-colors">
                        <svg v-if="isGenerating" class="size-5 animate-spin" fill="none" viewBox="0 0 24 24">
                            <circle class="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" stroke-width="4"></circle>
                            <path class="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                        </svg>
                        <svg v-else class="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-linecap="round" stroke-linejoin="round" stroke-width="2"><path stroke-dasharray="20" stroke-dashoffset="20" d="M12 21l0 -17.5"><animate fill="freeze" attributeName="stroke-dashoffset" dur="0.2s" values="20;0"/></path><path stroke-dasharray="12" stroke-dashoffset="12" d="M12 3l7 7M12 3l-7 7"><animate fill="freeze" attributeName="stroke-dashoffset" begin="0.2s" dur="0.2s" values="12;0"/></path></g></svg>
                    </button>
                </div>

                <!-- Attached files preview -->
                <div v-if="attachedFiles.length" class="mt-2 flex flex-wrap gap-2">
                    <div v-for="(f,i) in attachedFiles" :key="i" class="flex items-center gap-2 px-2 py-1 rounded-md border border-gray-300 text-xs text-gray-700 bg-gray-50">
                        <span class="truncate max-w-48" :title="f.name">{{ f.name }}</span>
                        <button type="button" class="text-gray-500 hover:text-gray-700" @click="removeAttachment(i)" title="Remove Attachment">
                            <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                        </button>
                    </div>
                </div>

                <div v-if="!model" class="mt-2 text-sm text-red-600">
                    Please select a model
                </div>
            </div>
        </div>
    </div>    
    `,
    props: {
        model: {
            type: String,
            default: ''
        },
        systemPrompt: {
            type: String,
            default: ''
        }
    },
    setup(props) {
        const ai = inject('ai')
        const chatSettings = inject('chatSettings')
        const router = useRouter()
        const config = inject('config')
        const chatPrompt = inject('chatPrompt')
        const {
            messageText,
            attachedFiles,
            isGenerating,
            errorStatus,
            hasImage,
            hasAudio,
            hasFile
        } = chatPrompt
        const threads = inject('threads')
        const {
            currentThread,
        } = threads

        const fileInput = ref(null)
        const showSettings = ref(false)
        const { applySettings } = chatSettings

        // File attachments (+) handlers
        const triggerFilePicker = () => {
            if (fileInput.value) fileInput.value.click()
        }
        const onFilesSelected = (e) => {
            const files = Array.from(e.target?.files || [])
            if (files.length) attachedFiles.value.push(...files)
            // allow re-selecting the same file
            if (fileInput.value) fileInput.value.value = ''

            if (!messageText.value.trim()) {
                if (hasImage()) {
                    messageText.value = getTextContent(config.defaults.image)
                } else if (hasAudio()) {
                    messageText.value = getTextContent(config.defaults.audio)
                } else {
                    messageText.value = getTextContent(config.defaults.file)
                }
            }
        }
        const removeAttachment = (i) => {
            attachedFiles.value.splice(i, 1)
        }

        function createChatRequest() {
            if (hasImage()) {
                return deepClone(config.defaults.image)
            }
            if (hasAudio()) {
                return deepClone(config.defaults.audio)
            }
            if (attachedFiles.value.length) {
                return deepClone(config.defaults.file)
            }
            const text = deepClone(config.defaults.text)
            return text
        }

        function getTextContent(chat) {
            const textMessage = chat.messages.find(m =>
                m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'text'))
            return textMessage?.content.find(c => c.type === 'text')?.text || ''
        }

        // Send message
        const sendMessage = async () => {
            if (!messageText.value.trim() || isGenerating.value || !props.model) return

            // Clear any existing error message
            errorStatus.value = null

            let message = messageText.value.trim()
            if (attachedFiles.value.length) {
                const names = attachedFiles.value.map(f => f.name).join(', ')
                const mediaType = imageExts.some(ext => names.includes(ext))
                    ? '🖼️'
                    : audioExts.some(ext => names.includes(ext))
                        ? '🔉'
                        : '📎'
                message += `\n\n[${mediaType} ${names}]`
            }
            messageText.value = ''

            try {
                let threadId

                // Create thread if none exists
                if (!currentThread.value) {
                    const newThread = await threads.createThread('New Chat', props.model, props.systemPrompt)
                    threadId = newThread.id
                    // Navigate to the new thread URL
                    router.push(`${ai.base}/c/${newThread.id}`)
                } else {
                    threadId = currentThread.value.id
                    // Update the existing thread's model and systemPrompt to match current selection
                    await threads.updateThread(threadId, {
                        model: props.model.id,
                        info: toModelInfo(props.model),
                        systemPrompt: props.systemPrompt
                    })
                }

                // Get the thread to check for duplicates
                let thread = await threads.getThread(threadId)
                const lastMessage = thread.messages[thread.messages.length - 1]
                const isDuplicate = lastMessage && lastMessage.role === 'user' && lastMessage.content === message

                // Add user message only if it's not a duplicate
                if (!isDuplicate) {
                    await threads.addMessageToThread(threadId, {
                        role: 'user',
                        content: message
                    })
                    // Reload thread after adding message
                    thread = await threads.getThread(threadId)
                }

                isGenerating.value = true
                const messages = [...thread.messages]

                // Add system prompt if present
                if (props.systemPrompt?.trim()) {
                    messages.unshift({
                        role: 'system',
                        content: [
                            { type: 'text', text: props.systemPrompt }
                        ]
                    })
                }

                const chatRequest = createChatRequest()
                chatRequest.model = props.model.id

                // Apply user settings
                applySettings(chatRequest)

                console.debug('chatRequest', chatRequest, hasImage(), hasAudio(), attachedFiles.value.length, attachedFiles.value)

                function setContentText(chatRequest, text) {
                    // Replace text message
                    const textImage = chatRequest.messages.find(m =>
                        m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'text'))
                    for (const c of textImage.content) {
                        if (c.type === 'text') {
                            c.text = text
                        }
                    }
                }

                if (hasImage()) {
                    const imageMessage = chatRequest.messages.find(m =>
                        m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'image_url'))
                    console.debug('hasImage', chatRequest, imageMessage)
                    if (imageMessage) {
                        const imgs = []
                        let imagePart = deepClone(imageMessage.content.find(c => c.type === 'image_url'))
                        for (const f of attachedFiles.value) {
                            if (imageExts.includes(lastRightPart(f.name, '.'))) {
                                imagePart.image_url.url = await fileToDataUri(f)
                            }
                            imgs.push(imagePart)
                        }
                        imageMessage.content = imageMessage.content.filter(c => c.type !== 'image_url')
                        imageMessage.content = [...imgs, ...imageMessage.content]
                        setContentText(chatRequest, message)
                    }

                } else if (hasAudio()) {
                    console.debug('hasAudio', chatRequest)
                    const audioMessage = chatRequest.messages.find(m =>
                        m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'input_audio'))
                    if (audioMessage) {
                        const audios = []
                        let audioPart = deepClone(audioMessage.content.find(c => c.type === 'input_audio'))
                        for (const f of attachedFiles.value) {
                            if (audioExts.includes(lastRightPart(f.name, '.'))) {
                                audioPart.input_audio.data = await fileToBase64(f)
                            }
                            audios.push(audioPart)
                        }
                        audioMessage.content = audioMessage.content.filter(c => c.type !== 'input_audio')
                        audioMessage.content = [...audios, ...audioMessage.content]
                        setContentText(chatRequest, message)
                    }
                } else if (attachedFiles.value.length) {
                    console.debug('hasFile', chatRequest)
                    const fileMessage = chatRequest.messages.find(m =>
                        m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'file'))
                    if (fileMessage) {
                        const files = []
                        let filePart = deepClone(fileMessage.content.find(c => c.type === 'file'))
                        for (const f of attachedFiles.value) {
                            filePart.file.file_data = await fileToDataUri(f)
                            filePart.file.filename = f.name
                            files.push(filePart)
                        }
                        fileMessage.content = fileMessage.content.filter(c => c.type !== 'file')
                        fileMessage.content = [...files, ...fileMessage.content]
                        setContentText(chatRequest, message)
                    }

                } else {
                    console.debug('hasText', chatRequest)
                    // Chat template message needs to be empty
                    chatRequest.messages = []
                    messages.forEach(m => chatRequest.messages.push({
                        role: m.role,
                        content: typeof m.content === 'string'
                            ? [{ type: 'text', text: m.content }]
                            : m.content
                    }))
                }

                // Send to API
                console.debug('chatRequest', chatRequest)
                const startTime = Date.now()
                const response = await ai.post('/v1/chat/completions', {
                    body: JSON.stringify(chatRequest)
                })

                let result = null
                if (!response.ok) {
                    errorStatus.value = {
                        errorCode: `HTTP ${response.status} ${response.statusText}`,
                        message: null,
                        stackTrace: null
                    }
                    let errorBody = null
                    try {
                        errorBody = await response.text()
                        if (errorBody) {
                            // Try to parse as JSON for better formatting
                            try {
                                const errorJson = JSON.parse(errorBody)
                                const status = errorJson?.responseStatus
                                if (status) {
                                    errorStatus.value.errorCode += ` ${status.errorCode}`
                                    errorStatus.value.message = status.message
                                    errorStatus.value.stackTrace = status.stackTrace
                                } else {
                                    errorStatus.value.stackTrace = JSON.stringify(errorJson, null, 2)
                                }
                            } catch (e) {
                            }
                        }
                    } catch (e) {
                        // If we can't read the response body, just use the status
                    }
                } else {
                    try {
                        result = await response.json()
                        console.debug('chatResponse', JSON.stringify(result, null, 2))
                    } catch (e) {
                        errorStatus.value = {
                            errorCode: 'Error',
                            message: e.message,
                            stackTrace: null
                        }
                    }
                }

                if (result?.error) {
                    errorStatus.value ??= {
                        errorCode: 'Error',
                    }
                    errorStatus.value.message = result.error
                } 
                
                if (!errorStatus.value) {
                    // Add assistant response (save entire message including reasoning)
                    const assistantMessage = result.choices?.[0]?.message

                    const usage = result.usage
                    if (usage) {
                        if (result.metadata?.pricing) {
                            const [ input, output ] = result.metadata.pricing.split('/')
                            usage.duration = result.metadata.duration ?? (Date.now() - startTime)
                            usage.input = input
                            usage.output = output
                            usage.tokens = usage.completion_tokens
                            usage.price = usage.output
                            usage.cost = tokenCost(usage.prompt_tokens * parseFloat(input) + usage.completion_tokens * parseFloat(output))
                        }
                        await threads.logRequest(threadId, props.model, chatRequest, result)
                    }
                    await threads.addMessageToThread(threadId, assistantMessage, usage)

                    nextTick(addCopyButtons)

                    attachedFiles.value = []
                    // Error will be cleared when user sends next message (no auto-timeout)
                }
            } finally {
                isGenerating.value = false
            }
        }

        const addNewLine = () => {
            // Enter key already adds new line
            //messageText.value += '\n'
        }

        return {
            isGenerating,
            attachedFiles,
            messageText,
            fileInput,
            showSettings,
            triggerFilePicker,
            onFilesSelected,
            removeAttachment,
            sendMessage,
            addNewLine,
        }
    }
}