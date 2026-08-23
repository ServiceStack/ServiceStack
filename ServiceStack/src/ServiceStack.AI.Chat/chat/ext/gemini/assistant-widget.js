const script = typeof SCRIPT !== 'undefined' ? SCRIPT : document.currentScript;
const overrides = script ? script.dataset : {};
const allowed = {
    theme: ['auto', 'light', 'dark', 'nord', 'matrix', 'soft-pink'],
    position: ['bottom-left', 'bottom-right'],
    icon: ['sparkles', 'chat', 'help'],
};
const appearance = { ...CONFIG.appearance };
appearance.colors = { ...(CONFIG.appearance.colors || {}) };
appearance.fonts = { ...(CONFIG.appearance.fonts || {}) };
appearance.button = { size:50, iconSize:26, background:'', iconColor:'#ffffff', borderColor:'', borderWidth:0, borderRadius:50, shadow:'medium', iconDataUri:'', ...(CONFIG.appearance.button || {}) };
for (const key of Object.keys(allowed)) {
    if (allowed[key].includes(overrides[key])) appearance[key] = overrides[key];
}
const accentOverride = /^#[0-9a-f]{6}$/i.test(overrides.accent || '') ? overrides.accent : '';
const launch = { openMode:'', keyboardShortcut:false, ...(CONFIG.launch || {}) };
if (!['', 'page-load', 'page-bottom'].includes(launch.openMode)) launch.openMode = '';
launch.keyboardShortcut = Boolean(launch.keyboardShortcut);
const markdownParser = typeof MARKDOWN !== 'undefined' && typeof MARKDOWN?.parse === 'function' ? MARKDOWN : null;

const storageKey = `gemini-assistant:${CONFIG.assistantId}`;
let stored = {};
try { stored = JSON.parse(localStorage.getItem(storageKey) || '{}') || {}; } catch {}
const newSessionId = () => crypto.randomUUID ? crypto.randomUUID() :
    `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;
let sessionId = stored.sessionId || newSessionId();
let messages = Array.isArray(stored.messages) ? stored.messages.slice(-30) : [];

const host = document.createElement('div');
host.style.cssText = 'all:initial';
host.setAttribute('data-gemini-assistant', CONFIG.assistantId);
host.dataset.position = appearance.position;
host.dataset.theme = appearance.theme;
const colorNames = ['accent-bg','panel-bg','conversation-bg','assistant-bg','assistant-border','user-bg','user-border','primary-text','muted-text','assistant-text','user-text','link-text','error-text','warning-text','panel-border','focus-border'];
const colorScheme = globalThis.matchMedia?.('(prefers-color-scheme: dark)');
function applyThemeColors() {
    colorNames.forEach(name => host.style.removeProperty(`--${name}`));
    host.style.removeProperty('--font-family');
    const theme = appearance.theme === 'auto' ? (colorScheme?.matches ? 'dark' : 'light') : appearance.theme;
    const colors = appearance.colors[theme] || {};
    colorNames.forEach(name => { if (/^#[0-9a-f]{6}$/i.test(colors[name] || '')) host.style.setProperty(`--${name}`, colors[name]); });
    if (appearance.fonts[theme]) host.style.setProperty('--font-family', appearance.fonts[theme]);
    if (accentOverride) host.style.setProperty('--accent-bg', accentOverride);
}
applyThemeColors();
if (appearance.theme === 'auto') colorScheme?.addEventListener?.('change', applyThemeColors);
document.body.appendChild(host);
const shadow = host.attachShadow({ mode: 'open' });

shadow.innerHTML = `<style>
:host{all:initial;--accent-bg:#2563eb;--panel-bg:#fff;--conversation-bg:#f8fafc;--assistant-bg:#fff;--assistant-border:#dbe2ea;--user-bg:#e8f0ff;--user-border:#bfdbfe;--primary-text:#172033;--muted-text:#64748b;--assistant-text:#172033;--user-text:#172033;--link-text:#2563eb;--error-text:#dc2626;--warning-text:#d97706;--panel-border:#dbe2ea;--focus-border:#93c5fd;--font-family:Inter,ui-sans-serif,system-ui,-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;--shadow:0 20px 55px rgba(15,23,42,.24);font-family:var(--font-family);color:var(--primary-text)}
:host([hidden]){display:none}
.root,.root *,.root *::before,.root *::after{box-sizing:border-box}.root{position:fixed;z-index:2147483000;bottom:20px;right:20px;display:flex;flex-direction:column;align-items:flex-end;gap:12px;pointer-events:none;font-family:var(--font-family);font-size:16px;font-style:normal;font-weight:400;line-height:1.45;color:var(--primary-text);text-align:left;text-transform:none;letter-spacing:normal;word-spacing:normal}.root button,.root textarea{font-family:var(--font-family);font-style:normal;letter-spacing:normal;text-transform:none}.root h2,.root p,.root form{padding:0}
:host([data-position="bottom-left"]) .root{right:auto;left:20px;align-items:flex-start}
.launcher{width:50px;height:50px;border:0;border-radius:50%;display:grid;place-items:center;pointer-events:auto;background:var(--accent-bg);color:#fff;box-shadow:0 10px 30px rgba(15,23,42,.28);cursor:pointer;overflow:hidden;transition:filter .16s ease}.launcher:hover{filter:brightness(.94)}.launcher:focus-visible,.close:focus-visible,.maximize:focus-visible,.clear:focus-visible,.send:focus-visible,.suggestion:focus-visible,textarea:focus-visible{outline:3px solid var(--focus-border);outline-offset:2px}.launcher svg,.launcher img{display:block;width:26px;height:26px}.launcher img{object-fit:contain}
.panel{width:min(390px,calc(100vw - 28px));height:min(610px,calc(100vh - 110px));display:grid;grid-template-rows:auto 1fr auto;visibility:hidden;opacity:0;pointer-events:none;transform:translateY(18px) scale(.94);transform-origin:bottom right;transition:opacity .18s ease,transform .22s cubic-bezier(.2,.8,.2,1),visibility 0s linear .22s;background:var(--panel-bg);border:1px solid var(--panel-border);border-radius:18px;box-shadow:var(--shadow);overflow:hidden}.panel.open{visibility:visible;opacity:1;pointer-events:auto;transform:translateY(0) scale(1);transition-delay:0s}:host([data-position="bottom-left"]) .panel{transform-origin:bottom left}.panel.compact{width:min(350px,calc(100vw - 28px));height:min(510px,calc(100vh - 110px))}.panel.maximized,.panel.compact.maximized{position:fixed;inset:16px;width:auto;height:auto;max-width:none;max-height:none;border-radius:14px;z-index:2147483001}
.header{display:flex;gap:12px;align-items:flex-start;padding:16px 16px 14px;background:var(--accent-bg);color:#fff}.heading{min-width:0;flex:1;cursor:pointer}.title-row{display:flex;align-items:center;gap:4px}.title{font-size:16px;font-weight:700;margin:0}.description{font-size:12px;opacity:.84;margin:3px 0 0}.maximize,.close{width:30px;height:30px;border:0;background:transparent;color:inherit;padding:5px;border-radius:6px;cursor:pointer;display:grid;place-items:center;line-height:1}.maximize{opacity:0;pointer-events:none;transition:opacity .15s ease}.header:hover .maximize,.header:focus-within .maximize{opacity:1;pointer-events:auto}.maximize:hover,.close:hover{background:rgba(255,255,255,.14)}.maximize svg{width:17px;height:17px}.close svg{width:18px;height:18px}
.body{overflow-y:auto;padding:16px;background:var(--conversation-bg)}.message{display:flex;margin:0 0 12px}.message.user{justify-content:flex-end}.bubble{max-width:88%;padding:10px 12px;border-radius:14px;background:var(--assistant-bg);color:var(--assistant-text);border:1px solid var(--assistant-border);font-size:14px;overflow-wrap:anywhere}.bubble.plaintext{white-space:pre-wrap}.user .bubble,.welcome .bubble{background:var(--user-bg);color:var(--user-text);border-color:var(--user-border)}.message.error .bubble{background:color-mix(in srgb,var(--error-text),transparent 86%);border-color:var(--error-text)}.bubble p{margin:0 0 8px}.bubble p:last-child{margin-bottom:0}.bubble h1,.bubble h2,.bubble h3,.bubble h4,.bubble h5,.bubble h6{font:inherit;font-weight:700;line-height:1.3;margin:12px 0 6px}.bubble h1:first-child,.bubble h2:first-child,.bubble h3:first-child{margin-top:0}.bubble h1{font-size:1.3em}.bubble h2{font-size:1.2em}.bubble h3{font-size:1.1em}.bubble ul,.bubble ol{margin:7px 0;padding-left:22px}.bubble li{margin:3px 0}.bubble blockquote{margin:8px 0;padding:2px 0 2px 10px;border-left:3px solid var(--panel-border);color:var(--muted-text)}.bubble hr{border:0;border-top:1px solid var(--panel-border);margin:10px 0}.bubble a{color:var(--link-text);text-decoration:none}.bubble a:hover{text-decoration:underline}.bubble pre{white-space:pre-wrap;overflow-wrap:anywhere;background:#111827;color:#f8fafc;padding:10px;border-radius:8px;margin:8px 0;font:12px/1.5 ui-monospace,SFMono-Regular,Menlo,monospace}.bubble code{font:12px ui-monospace,SFMono-Regular,Menlo,monospace;background:color-mix(in srgb,var(--panel-border),transparent 45%);padding:1px 4px;border-radius:4px}.bubble pre code{background:transparent;padding:0}.bubble table{display:block;max-width:100%;overflow-x:auto;border-collapse:collapse;margin:8px 0;font-size:12px}.bubble th,.bubble td{border:1px solid var(--panel-border);padding:4px 6px;text-align:left}.bubble input[type="checkbox"]{margin:0 5px 0 0}.sources{margin-top:9px;padding-top:8px;border-top:1px solid var(--panel-border);display:grid;gap:5px}.sources a,.sources span{font-size:13px}.sources a{color:var(--link-text);text-decoration:none}.sources a:hover{text-decoration:underline}.typing{color:var(--muted-text);font-size:13px}.suggestions{display:flex;flex-wrap:wrap;gap:7px;margin:2px 0 14px}.suggestion{border:1px solid var(--panel-border);background:var(--panel-bg);color:var(--primary-text);border-radius:14px;padding:7px 10px;font-size:12px;font-weight:400;line-height:1.45;cursor:pointer;text-align:left}.suggestion:hover{border-color:var(--link-text);color:var(--link-text)}
.thread-actions{display:flex;justify-content:center;margin:2px 0 4px}.clear{border:0;background:transparent;color:var(--muted-text);padding:2px 4px;cursor:pointer;font-size:11px;text-decoration:none}.clear:hover{text-decoration:underline}.composer{padding:11px 12px 9px;background:var(--panel-bg);border-top:1px solid var(--panel-border)}.form{display:flex;align-items:flex-end;gap:8px;margin:0}textarea{box-sizing:border-box;min-height:40px;max-height:112px;resize:none;flex:1;border:1px solid var(--panel-border);border-radius:12px;background:var(--panel-bg);color:var(--primary-text);padding:9px 10px;font-size:14px;font-weight:400;line-height:1.4}.send{width:40px;height:40px;flex:0 0 40px;border:0;border-radius:11px;background:var(--accent-bg);color:#fff;cursor:pointer;font-size:18px;font-weight:400;line-height:1}.send:disabled{opacity:.45;cursor:not-allowed}.notice{margin:7px 2px 0;color:var(--muted-text);font-size:10px;font-weight:400;line-height:1.45;text-align:center}
:host([data-theme="dark"]){--accent-bg:#2563eb;--panel-bg:#0f172a;--conversation-bg:#111827;--assistant-bg:#1f2937;--assistant-border:#374151;--assistant-text:#f3f4f6;--user-bg:#1d4ed8;--user-border:#3b82f6;--user-text:#ffffff;--primary-text:#f3f4f6;--muted-text:#9ca3af;--panel-border:#334155;--link-text:#60a5fa;--focus-border:#93c5fd;--error-text:#f87171;--warning-text:#fbbf24;--shadow:0 20px 60px rgba(0,0,0,.55)}
:host([data-theme="nord"]){--accent-bg:#5e81ac;--panel-bg:#2e3440;--conversation-bg:#2e3440;--assistant-bg:#4c566a;--assistant-border:#434c5e;--assistant-text:#eceff4;--user-bg:#5e81ac;--user-border:#81a1c1;--user-text:#eceff4;--primary-text:#eceff4;--muted-text:#d8dee9;--panel-border:#4c566a;--link-text:#8fbcbb;--focus-border:#81a1c1;--error-text:#bf616a;--warning-text:#ebcb8b;--shadow:0 20px 60px rgba(46,52,64,.62)}
:host([data-theme="matrix"]){--accent-bg:#0d542b;--panel-bg:#000000;--conversation-bg:#020a04;--assistant-bg:#000000;--assistant-border:#008236;--assistant-text:#86efac;--user-bg:#052e16;--user-border:#166534;--user-text:#4ade80;--primary-text:#4ade80;--muted-text:#15803d;--panel-border:#166534;--link-text:#4ade80;--focus-border:#22c55e;--error-text:#f87171;--warning-text:#facc15;--font-family:ui-monospace,SFMono-Regular,Menlo,Monaco,Consolas,monospace;--shadow:0 20px 60px rgba(0,0,0,.75)}
:host([data-theme="matrix"]) .panel{box-shadow:0 0 28px rgba(34,197,94,.16),var(--shadow)}
:host([data-theme="soft-pink"]){--accent-bg:#ec4899;--panel-bg:#ffffff;--conversation-bg:#fdf2f8;--assistant-bg:#fce7f3;--assistant-border:#f9a8d4;--assistant-text:#831843;--user-bg:#f1f5f9;--user-border:#cbd5e1;--user-text:#1e293b;--primary-text:#831843;--muted-text:#9d174d;--panel-border:#fbcfe8;--link-text:#ec4899;--focus-border:#f472b6;--error-text:#e11d48;--warning-text:#d97706;--shadow:0 20px 55px rgba(131,24,67,.20)}
@media(prefers-color-scheme:dark){:host([data-theme="auto"]){--accent-bg:#2563eb;--panel-bg:#0f172a;--conversation-bg:#111827;--assistant-bg:#1f2937;--assistant-border:#374151;--assistant-text:#f3f4f6;--user-bg:#1d4ed8;--user-border:#3b82f6;--user-text:#ffffff;--primary-text:#f3f4f6;--muted-text:#9ca3af;--panel-border:#334155;--link-text:#60a5fa;--focus-border:#93c5fd;--error-text:#f87171;--warning-text:#fbbf24;--shadow:0 20px 60px rgba(0,0,0,.55)}}
@media(max-width:520px){.root{bottom:12px;right:12px}:host([data-position="bottom-left"]) .root{left:12px}.panel,.panel.compact{width:calc(100vw - 24px);height:min(680px,calc(100vh - 88px))}.panel.maximized,.panel.compact.maximized{inset:0;width:100vw;height:100vh;border-radius:0}}
@media(prefers-reduced-motion:reduce){.launcher,.body,.panel{transition:none;scroll-behavior:auto}}
</style>
<div class="root">
  <section class="panel ${appearance.panelSize === 'compact' ? 'compact' : ''}" role="dialog" aria-label="${escapeAttr(CONFIG.title)}" aria-hidden="true">
    <header class="header"><div class="heading"><div class="title-row"><h2 class="title"></h2><button class="maximize" type="button" aria-label="Maximize assistant" title="Maximize"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M8 3H3v5M16 3h5v5M8 21H3v-5M16 21h5v-5"/></svg></button></div><p class="description"></p></div><button class="close" type="button" aria-label="Close assistant"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round"><path d="M6 6l12 12M18 6 6 18"/></svg></button></header>
    <main class="body" aria-live="polite"><div class="messages"></div><div class="thread-actions"><button class="clear" type="button" title="Clear this conversation and start a new one">clear</button></div></main>
    <footer class="composer"><form class="form"><textarea rows="1" maxlength="8000" aria-label="Message" placeholder="Ask a question…"></textarea><button class="send" type="submit" aria-label="Send message">➤</button></form><p class="notice"></p></footer>
  </section>
  <button class="launcher" type="button" aria-label="Open assistant" aria-expanded="false"></button>
</div>`;

function escapeAttr(value) { return String(value || '').replace(/[&<>"']/g, ''); }
function safeSourceUrl(value) {
    const url = String(value || '').trim();
    return /^(https?:\/\/|\/)/i.test(url) ? url : '';
}
function sourceTitle(source) {
    const title = String(source.title || 'Source');
    const sourceUrl = String(source.url || '').trim();
    if (/^https?:\/\//i.test(sourceUrl)) {
        try {
            const url = new URL(sourceUrl);
            if (url.pathname === '/' && !url.search && !url.hash) return url.host;
        } catch {}
    }
    const match = title.match(/^(.*)\.[^./]+$/);
    if (!match) return title;
    const path = sourceUrl.split(/[?#]/, 1)[0].replace(/\/+$/, '');
    let lastSegment = path.slice(path.lastIndexOf('/') + 1);
    try { lastSegment = decodeURIComponent(lastSegment); } catch {}
    return lastSegment === match[1] ? match[1] : title;
}
const panel = shadow.querySelector('.panel');
const launcher = shadow.querySelector('.launcher');
const closeButton = shadow.querySelector('.close');
const heading = shadow.querySelector('.heading');
const maximizeButton = shadow.querySelector('.maximize');
const clearButton = shadow.querySelector('.clear');
const body = shadow.querySelector('.body');
const messageList = shadow.querySelector('.messages');
const form = shadow.querySelector('.form');
const input = shadow.querySelector('textarea');
const send = shadow.querySelector('.send');
const notice = shadow.querySelector('.notice');
shadow.querySelector('.title').textContent = CONFIG.title;
shadow.querySelector('.description').textContent = CONFIG.description;
notice.textContent = CONFIG.notice || '';
notice.hidden = !notice.textContent;
const icons = {
    sparkles: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none" /><path fill="currentColor" d="m21.45 11.11l-3-1.5l-2.68-1.34l-.03-.03l-1.34-2.68l-1.5-3c-.34-.68-1.45-.68-1.79 0l-1.5 3l-1.34 2.68l-.03.03l-2.68 1.34l-3 1.5c-.34.17-.55.52-.55.89s.21.72.55.89l3 1.5l2.68 1.34l.03.03l1.34 2.68l1.5 3c.17.34.52.55.89.55s.72-.21.89-.55l1.5-3l1.34-2.68l.03-.03l2.68-1.34l3-1.5c.34-.17.55-.52.55-.89s-.21-.72-.55-.89Z" /></svg>`,
    chat: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none" /><path fill="currentColor" d="M12 3c5.5 0 10 3.58 10 8s-4.5 8-10 8c-1.24 0-2.43-.18-3.53-.5C5.55 21 2 21 2 21c2.33-2.33 2.7-3.9 2.75-4.5C3.05 15.07 2 13.13 2 11c0-4.42 4.5-8 10-8" /></svg>`,
    help: `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><path d="M0 0h24v24H0z" fill="none" /><path fill="currentColor" d="M10 19h3v3h-3zm2-17c5.35.22 7.68 5.62 4.5 9.67c-.83 1-2.17 1.66-2.83 2.5C13 15 13 16 13 17h-3c0-1.67 0-3.08.67-4.08c.66-1 2-1.59 2.83-2.25C15.92 8.43 15.32 5.26 12 5a3 3 0 0 0-3 3H6a6 6 0 0 1 6-6" /></svg>`,
};
const launcherConfig = appearance.button;
const launcherSize = Math.min(Math.max(Number(launcherConfig.size) || 50, 40), 96);
const launcherIconSize = Math.min(Math.max(Number(launcherConfig.iconSize) || 26, 16), 72);
const launcherShadows = { none:'none', subtle:'0 4px 12px rgba(15,23,42,.16)', medium:'0 10px 30px rgba(15,23,42,.28)', strong:'0 16px 42px rgba(15,23,42,.4)' };
Object.assign(launcher.style, {
    width: `${launcherSize}px`, height: `${launcherSize}px`,
    background: /^#[0-9a-f]{6}$/i.test(launcherConfig.background || '') ? launcherConfig.background : 'var(--accent-bg)',
    color: /^#[0-9a-f]{6}$/i.test(launcherConfig.iconColor || '') ? launcherConfig.iconColor : '#ffffff',
    borderStyle: 'solid', borderWidth: `${Math.min(Math.max(Number(launcherConfig.borderWidth) || 0, 0), 8)}px`,
    borderColor: /^#[0-9a-f]{6}$/i.test(launcherConfig.borderColor || '') ? launcherConfig.borderColor : 'var(--panel-border)',
    borderRadius: `${Math.min(Math.max(Number(launcherConfig.borderRadius) || 0, 0), 50)}%`,
    boxShadow: launcherShadows[launcherConfig.shadow] || launcherShadows.medium,
});
if (/^data:image\/(?:png|jpeg|gif|webp|svg\+xml)(?:;charset=[^;,]+)?(?:;base64)?,/i.test(launcherConfig.iconDataUri || '')) {
    const image = document.createElement('img'); image.src = launcherConfig.iconDataUri; image.alt = '';
    image.style.width = `${launcherIconSize}px`; image.style.height = `${launcherIconSize}px`; launcher.append(image);
} else {
    launcher.innerHTML = icons[appearance.icon] || icons.sparkles;
    const icon = launcher.querySelector('svg'); icon.style.width = `${launcherIconSize}px`; icon.style.height = `${launcherIconSize}px`;
}

function save() {
    try { localStorage.setItem(storageKey, JSON.stringify({ sessionId, messages: messages.slice(-30) })); } catch {}
}
const markdownTags = new Set(['p','br','strong','em','code','pre','ul','ol','li','blockquote',
    'h1','h2','h3','h4','h5','h6','hr','a','table','thead','tbody','tr','th','td','del','input']);
function safeMarkdownUrl(value) {
    const url = String(value || '').trim();
    if (!url || /^(?:javascript|data|vbscript):/i.test(url)) return '';
    return url;
}
function sanitizedMarkdown(html) {
    const template = document.createElement('template');
    template.innerHTML = html;
    for (const element of [...template.content.querySelectorAll('*')]) {
        const tag = element.tagName.toLowerCase();
        if (!markdownTags.has(tag)) {
            element.replaceWith(document.createTextNode(element.textContent || ''));
            continue;
        }
        const href = tag === 'a' ? safeMarkdownUrl(element.getAttribute('href')) : '';
        const title = tag === 'a' ? element.getAttribute('title') : '';
        const checked = tag === 'input' && element.hasAttribute('checked');
        for (const attribute of [...element.attributes]) element.removeAttribute(attribute.name);
        if (tag === 'a' && href) {
            element.setAttribute('href', href);
            if (title) element.setAttribute('title', title);
            if (!href.startsWith('#')) {
                element.setAttribute('target', '_blank');
                element.setAttribute('rel', 'noopener noreferrer');
            }
        } else if (tag === 'input') {
            element.setAttribute('type', 'checkbox');
            element.disabled = true;
            element.checked = checked;
        }
    }
    return template.content;
}
function renderPlainText(container, text) {
    container.classList.add('plaintext');
    container.textContent = text;
}
function renderMarkdown(container, text) {
    const source = String(text || '');
    container.replaceChildren();
    container.classList.remove('plaintext');
    if (!markdownParser) return renderPlainText(container, source);
    try {
        const html = markdownParser.parse(source, { gfm: true });
        if (typeof html !== 'string') throw new Error('Markdown renderer returned an invalid result');
        container.append(sanitizedMarkdown(html));
    } catch (error) {
        console.warn('Gemini Assistant could not render Markdown; using plain text.', error);
        renderPlainText(container, source);
    }
}
function addMessage(message) {
    const row = document.createElement('div'); row.className = `message ${message.role}${message.welcome ? ' welcome' : ''}${message.error ? ' error' : ''}`;
    const bubble = document.createElement('div'); bubble.className = 'bubble'; renderMarkdown(bubble, message.content);
    if (message.citations && message.citations.length) {
        const sources = document.createElement('div'); sources.className = 'sources';
        message.citations.forEach((source, i) => {
            const url = safeSourceUrl(source.url);
            const item = document.createElement(url ? 'a' : 'span');
            item.textContent = `${i + 1}. ${sourceTitle(source)}`;
            if (url) { item.href = url; item.target = '_blank'; item.rel = 'noopener noreferrer'; }
            sources.append(item);
        });
        bubble.append(sources);
    }
    row.append(bubble); messageList.append(row);
    return bubble;
}
let scrollFrame = 0;
function scrollToBottom() {
    if (scrollFrame) return;
    scrollFrame = requestAnimationFrame(() => {
        scrollFrame = 0;
        body.scrollTop = body.scrollHeight;
    });
}
function render() {
    messageList.replaceChildren();
    if (!messages.length) {
        addMessage({ role: 'assistant', content: CONFIG.welcome, welcome: true });
        if (CONFIG.suggestions && CONFIG.suggestions.length) {
            const suggestions = document.createElement('div'); suggestions.className = 'suggestions';
            CONFIG.suggestions.forEach(text => {
                const button = document.createElement('button'); button.type = 'button'; button.className = 'suggestion'; button.textContent = text;
                button.addEventListener('click', () => submitMessage(text)); suggestions.append(button);
            });
            messageList.append(suggestions);
        }
    } else messages.forEach(addMessage);
    clearButton.hidden = !messages.some(message =>
        message.role === 'assistant' && String(message.content || '').trim());
    scrollToBottom();
}
const maximizeIcon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 28 28">
        <path d="M0 0h28v28H0z" fill="none" />
        <path fill="#fff" d="M8.776 18.168a.75.75 0 0 1 1.056 1.056l-.052.056l-4.22 4.22h2.69a.75.75 0 0 1 0 1.5h-4.5a.75.75 0 0 1-.75-.75v-4.5a.75.75 0 0 1 1.5 0v2.69l4.22-4.22zm9.444.052a.75.75 0 0 1 1.004-.052l.056.052l4.22 4.22v-2.69a.75.75 0 0 1 1.5 0v4.5a.75.75 0 0 1-.75.75h-4.5a.75.75 0 0 1 0-1.5h2.69l-4.22-4.22l-.052-.056a.75.75 0 0 1 .052-1.004M8.25 3a.75.75 0 0 1 0 1.5H5.56l4.22 4.22l.052.056a.75.75 0 0 1-1.056 1.056L8.72 9.78L4.5 5.56v2.69a.75.75 0 0 1-1.5 0v-4.5A.75.75 0 0 1 3.75 3zm16 0a.75.75 0 0 1 .75.75v4.5a.75.75 0 0 1-1.5 0V5.56l-4.22 4.22l-.056.052a.75.75 0 0 1-1.056-1.056l.052-.056l4.22-4.22h-2.69a.75.75 0 0 1 0-1.5z" />
    </svg>`;
const restoreIcon = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
        <path d="M0 0h24v24H0z" fill="none" />
        <path fill="currentColor" d="M8.5 3.75a.75.75 0 0 0-1.5 0v2.5a.75.75 0 0 1-.75.75h-2.5a.75.75 0 0 0 0 1.5h2.5A2.25 2.25 0 0 0 8.5 6.25zm0 16.5a.75.75 0 0 1-1.5 0v-2.5a.75.75 0 0 0-.75-.75h-2.5a.75.75 0 0 1 0-1.5h2.5a2.25 2.25 0 0 1 2.25 2.25zM16.25 3a.75.75 0 0 0-.75.75v2.5a2.25 2.25 0 0 0 2.25 2.25h2.5a.75.75 0 0 0 0-1.5h-2.5a.75.75 0 0 1-.75-.75v-2.5a.75.75 0 0 0-.75-.75m-.75 17.25a.75.75 0 0 0 1.5 0v-2.5a.75.75 0 0 1 .75-.75h2.5a.75.75 0 0 0 0-1.5h-2.5a2.25 2.25 0 0 0-2.25 2.25z" />
    </svg>`;
function setMaximized(maximized) {
    panel.classList.toggle('maximized', maximized);
    maximizeButton.innerHTML = maximized ? restoreIcon : maximizeIcon;
    maximizeButton.setAttribute('aria-label', maximized ? 'Restore assistant window' : 'Maximize assistant');
    maximizeButton.title = maximized ? 'Restore' : 'Maximize';
}
function setOpen(open) {
    panel.classList.toggle('open', open); panel.setAttribute('aria-hidden', String(!open));
    launcher.setAttribute('aria-expanded', String(open)); launcher.setAttribute('aria-label', open ? 'Close assistant' : 'Open assistant');
    if (!open) setMaximized(false);
    if (open) setTimeout(() => input.focus(), 0);
}
function clearConversation() {
    if (!messages.length) return;
    messages = []; sessionId = newSessionId(); input.value = ''; save(); render(); input.focus();
}
async function submitMessage(value) {
    const message = String(value || input.value || '').trim();
    if (!message || send.disabled) return;
    input.value = ''; messages.push({ role: 'user', content: message }); save(); render();
    const typing = document.createElement('div'); typing.className = 'typing'; typing.textContent = 'Thinking…'; messageList.append(typing); scrollToBottom();
    send.disabled = true; input.disabled = true;
    let reply = null;
    try {
        const response = await fetch(CONFIG.chatUrl, {
            method: 'POST', headers: { 'Content-Type': 'text/plain;charset=UTF-8' },
            body: JSON.stringify({ sessionId, message, pageUrl: location.href, stream: true }),
        });
        if (!response.ok) {
            const data = await response.json();
            throw new Error(data?.error?.message || data?.message || 'The assistant could not answer.');
        }
        reply = { role: 'assistant', content: '', citations: [] };
        messages.push(reply);
        let replyBubble = null;
        const reader = response.body.getReader(), decoder = new TextDecoder();
        let buffer = '';
        while (true) {
            const { value: chunk, done } = await reader.read();
            buffer += decoder.decode(chunk || new Uint8Array(), { stream: !done });
            const lines = buffer.split('\n'); buffer = done ? '' : lines.pop();
            for (const line of lines) {
                if (!line.trim()) continue;
                const event = JSON.parse(line);
                if (event.error) throw new Error(event.error);
                if (event.delta) {
                    reply.content += event.delta;
                    if (!replyBubble) {
                        typing.remove();
                        replyBubble = addMessage(reply);
                    } else {
                        replyBubble.replaceChildren();
                        renderMarkdown(replyBubble, reply.content);
                    }
                    scrollToBottom();
                }
                if (event.citations) reply.citations = event.citations;
            }
            if (done) break;
        }
        if (!reply.content) throw new Error('The assistant returned an empty response.');
    } catch (error) {
        if (reply) messages = messages.filter(x => x !== reply);
        messages.push({ role: 'assistant', content: error.message || 'The assistant could not answer right now.', error: true });
    } finally {
        send.disabled = false; input.disabled = false; save(); render(); input.focus();
    }
}
launcher.addEventListener('click', () => setOpen(!panel.classList.contains('open')));
closeButton.addEventListener('click', () => setOpen(false));
heading.addEventListener('click', () => setMaximized(!panel.classList.contains('maximized')));
clearButton.addEventListener('click', clearConversation);
form.addEventListener('submit', event => { event.preventDefault(); submitMessage(); });
input.addEventListener('keydown', event => {
    if (event.key === 'Enter' && !event.shiftKey) { event.preventDefault(); submitMessage(); }
});
input.addEventListener('input', () => { input.style.height = 'auto'; input.style.height = `${Math.min(input.scrollHeight, 112)}px`; });
shadow.addEventListener('keydown', event => { if (event.key === 'Escape') panel.classList.contains('maximized') ? setMaximized(false) : setOpen(false); event.stopPropagation(); });
shadow.addEventListener('keyup', event => event.stopPropagation());
shadow.addEventListener('keypress', event => event.stopPropagation());
function pageIsAtBottom() {
    const doc = document.scrollingElement || document.documentElement;
    return doc.scrollTop + doc.clientHeight >= doc.scrollHeight - 2;
}
if (launch.openMode === 'page-load') {
    requestAnimationFrame(() => setOpen(true));
}
if (launch.openMode === 'page-bottom') {
    const openAtBottom = () => {
        if (!pageIsAtBottom()) return;
        setOpen(true);
        window.removeEventListener('scroll', openAtBottom);
        window.removeEventListener('resize', openAtBottom);
    };
    window.addEventListener('scroll', openAtBottom, { passive:true });
    window.addEventListener('resize', openAtBottom);
    requestAnimationFrame(openAtBottom);
}
if (launch.keyboardShortcut) {
    document.addEventListener('keydown', event => {
        if (event.repeat || event.altKey || String(event.key).toLowerCase() !== 'k' || (!event.ctrlKey && !event.metaKey)) return;
        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        setOpen(true);
    }, true);
}
render();
if (overrides.open === 'true') setOpen(true);
