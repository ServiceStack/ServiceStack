import { ref, watch } from "vue"
import { lastRightPart, rightPart } from "@servicestack/client"

const config = {
    defaults: {
        "text": {
            "model": "kimi-k2",
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "text",
                            "text": ""
                        }
                    ]
                }
            ]
        },
        "image": {
            "model": "qwen2.5vl",
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "image_url",
                            "image_url": {
                                "url": ""
                            }
                        },
                        {
                            "type": "text",
                            "text": "Describe the key features of the input image"
                        }
                    ]
                }
            ]
        },
        "audio": {
            "model": "gpt-4o-audio-preview",
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "input_audio",
                            "input_audio": {
                                "data": "",
                                "format": "mp3"
                            }
                        },
                        {
                            "type": "text",
                            "text": "Please transcribe and summarize this audio file"
                        }
                    ]
                }
            ]
        },
        "file": {
            "model": "qwen2.5vl",
            "messages": [
                {
                    "role": "user",
                    "content": [
                        {
                            "type": "file",
                            "file": {
                                "filename": "",
                                "file_data": ""
                            }
                        },
                        {
                            "type": "text",
                            "text": "Please summarize this document"
                        }
                    ]
                }
            ]
        }
    }
}
const imageExts = 'png,webp,jpg,jpeg,gif,bmp,svg,tiff,ico'.split(',')
const audioExts = 'mp3,wav,ogg,flac,m4a,opus,webm'.split(',')

function deepClone(obj) {
    return JSON.parse(JSON.stringify(obj))
}

function fileToBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.readAsDataURL(file) //= "data:…;base64,…"
        reader.onload  = () => {
            resolve(rightPart(reader.result, ',')) // strip prefix
        }
        reader.onerror = err => reject(err)
    })
}

function fileToDataUri(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader()
        reader.readAsDataURL(file) //= "data:…;base64,…"
        reader.onload  = () => resolve(reader.result)
        reader.onerror = err => reject(err)
    })
}

const SystemPrompt = {
    template:`
        <div class="relative">
            <CloseButton class="-mt-4 -mr-4" @close="$emit('done')" buttonClass="" title="Close System Prompt" />
            <div class="mb-2">
                <label class="block text-sm font-medium text-gray-700 mb-2">
                    System Prompt
                </label>
                <TextareaInput
                    :value="modelValue" @input="$emit('update:modelValue', $event.target.value)"
                    placeholder="Enter a system prompt to guide AI's behavior..."
                    rows="6"
                ></TextareaInput>
            </div>
        </div>
    `,
    emits:['update:modelValue','done'],
    props: {
        modelValue: String,
    },
    setup() {
    }
}

export const ChatMessages = {
    components: { 
        SystemPrompt,
    },
    template: `
    <div>
        <MarkdownInput :label="$attrs.label" v-model="userPrompt" hide="image">
          <template #header>
            <SystemPrompt v-if="showSystemPrompt" v-model="systemPrompt" @done="showSystemPrompt=false" />
          </template>
          <template #toolbarbuttons="{ instance, textarea }">
            <!-- Attach Files -->
            <button type="button" @click="triggerFilePicker" title="Attach image, audio or documents"
                class="pl-4 border-l border-gray-200">
                <svg class="size-5 cursor-pointer select-none text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="currentColor" d="M18 15.75q0 2.6-1.825 4.425T11.75 22t-4.425-1.825T5.5 15.75V6.5q0-1.875 1.313-3.187T10 2t3.188 1.313T14.5 6.5v8.75q0 1.15-.8 1.95t-1.95.8t-1.95-.8t-.8-1.95V6h2v9.25q0 .325.213.538t.537.212t.538-.213t.212-.537V6.5q-.025-1.05-.737-1.775T10 4t-1.775.725T7.5 6.5v9.25q-.025 1.775 1.225 3.013T11.75 20q1.75 0 2.975-1.237T16 15.75V6h2z"/></svg>
            </button>

            <!-- Hidden file input -->
            <input ref="fileInput" type="file" multiple @change="onFilesSelected"
                class="hidden" accept="image/*,audio/*,.pdf,.doc,.docx,.xml,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                />

            <!-- Show System Prompt -->
            <button type="button" @click="showSystemPrompt=!showSystemPrompt" title="Show System Prompt">
                <svg class="size-5 cursor-pointer select-none text-gray-700 dark:text-gray-300 hover:text-indigo-600 dark:hover:text-indigo-400" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path fill="none" stroke="currentColor" stroke-width="2" d="M1 19h22V1H1zm4 4h14zm3 0h8v-4H8zM7.757 5.757l2.122 2.122zM9 10H6zm.879 2.121l-2.122 2.122zM12 13v3zm2.121-.879l2.122 2.122zM18 10h-3zm-1.757-4.243l-2.122 2.122zM12 7V4zm0 0a3 3 0 1 0 0 6a3 3 0 0 0 0-6Z"/></svg>
            </button>
          </template>
        </MarkdownInput>
        <!-- Attached files preview -->
        <div v-if="attachedFiles.length" class="flex flex-wrap gap-x-2">
            <div v-for="(f,i) in attachedFiles" :key="i" class="flex items-center gap-2 px-2 py-1 rounded-md border border-gray-300 text-xs text-gray-700 bg-gray-50">
                <span class="truncate max-w-48" :title="f.name">{{ f.name }}</span>
                <button type="button" class="text-gray-500 hover:text-gray-700" @click="removeAttachment(i)" title="Remove Attachment">
                    <svg class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><line x1="18" y1="6" x2="6" y2="18"></line><line x1="6" y1="6" x2="18" y2="18"></line></svg>
                </button>
            </div>
        </div>
        <input type="hidden" :name="$attrs.id" :value="JSON.stringify(modelValue)" />
    </div>
    `,
    emits:['update:modelValue'],
    props: ['modelValue'],
    setup(props, { emit }) {
        const systemPrompt = ref('')
        const userPrompt = ref('')
        const showSystemPrompt = ref(false)
        const attachedFiles = ref([])
        const fileInput = ref()

        const hasImage = () => attachedFiles.value.some(f => imageExts.includes(lastRightPart(f.name, '.')))
        const hasAudio = () => attachedFiles.value.some(f => audioExts.includes(lastRightPart(f.name, '.')))
        const hasFile = () => attachedFiles.value.length > 0
        
        watch(() => [systemPrompt.value, userPrompt.value], async () => {
            console.log('watch', systemPrompt.value, userPrompt.value)
            
            const chat = await createChatMessages(userPrompt.value)

            if (systemPrompt.value.trim()) {
                chat.messages.unshift({
                    role: 'system',
                    content: [
                        { type: 'text', text: systemPrompt.value }
                    ],
                })
            }
            console.log('msgs', chat.messages)
            emit('update:modelValue', chat.messages)
        })

        const triggerFilePicker = () => {
            if (fileInput.value) fileInput.value.click()
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
        
        async function createChatMessages(message) {
            const chatRequest = createChatRequest()
            const messages = chatRequest.messages
            
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
                setContentText(chatRequest, message)
            }
            
            return chatRequest
        }

        function getTextContent(chat) {
            const textMessage = chat.messages.find(m =>
                m.role === 'user' && Array.isArray(m.content) && m.content.some(c => c.type === 'text'))
            return textMessage?.content.find(c => c.type === 'text')?.text || ''
        }

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

        const onFilesSelected = (e) => {
            const files = Array.from(e.target?.files || [])
            if (files.length) attachedFiles.value.push(...files)
            // allow re-selecting the same file
            if (fileInput.value) fileInput.value.value = ''

            if (!userPrompt.value?.trim()) {
                if (hasImage()) {
                    userPrompt.value = getTextContent(config.defaults.image)
                } else if (hasAudio()) {
                    userPrompt.value = getTextContent(config.defaults.audio)
                } else {
                    userPrompt.value = getTextContent(config.defaults.file)
                }
            }
        }
        const removeAttachment = (i) => {
            attachedFiles.value.splice(i, 1)
        }
        
        return {
            fileInput,
            showSystemPrompt,
            systemPrompt,
            userPrompt,
            attachedFiles,
            triggerFilePicker,
            onFilesSelected,
            removeAttachment,
        }
    }
}
export default ChatMessages