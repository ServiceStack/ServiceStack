
import{createApp,reactive}from"vue"
import{createBus,map,each,leftPart,on,queryString,apiValue,mapGet,isDate,padInt,}from"@servicestack/client"
import{useMetadata,useFormatters}from"@servicestack/vue"
const{typeOfRef}=useMetadata()
export class App{app
events=createBus()
Providers={}
Plugins=[]
Directives={}
Props={}
OnStart=[]
Components={}
provides(providers){Object.keys(providers).forEach(k=>this.Providers[k]=providers[k])}
components(components){Object.keys(components).forEach(k=>this.Components[k]=components[k])}
component(name,c){if(c){this.Components[name]=c}
return this.Components[name]}
directive(name,f){this.Directives[name]=f}
use(plugin){this.Plugins.push(plugin)}
build(component,props){const app=this.app=createApp(component,props)
this.Plugins.forEach(plugin=>app.use(plugin))
Object.keys(this.Providers).forEach(name=>{app.provide(name,this.Providers[name])})
Object.keys(this.Components).forEach(name=>{app.component(name,this.Components[name])})
Object.keys(this.Directives).forEach(name=>{app.directive(name,this.Directives[name])})
return app}
onStart(f){this.OnStart.push(f)}
start(){this.OnStart.forEach(f=>f(this))}
subscribe(type,callback){return this.events.subscribe(type,callback)}
unsubscribe(sub){if(sub){sub.unsubscribe()}}}
export function usePageRoutes(app,{page,queryKeys,handlers,extend}){if(typeof page!='string'||page==='')
throw new Error('page is required')
if(typeof queryKeys=='undefined'||!queryKeys.length)
throw new Error('Array of queryKeys is required')
let allKeys=[page,...queryKeys]
function getPage(){return leftPart(location.href,'?').substring(document.baseURI.length)}
function state(store){return each(allKeys,(o,key)=>store[key]?o[key]=store[key]:null)}
let publish=(name,args)=>{events.publish('route:'+name,args)
events.publish('route:nav',args)}
let events=app.events
let store={page,queryKeys,...each(allKeys,(o,x)=>o[x]=''),start(){window.addEventListener('popstate',(event)=>{this.set({[page]:getPage(),...event.state})
publish('init',state(this))})
console.log('routes.start()',page,getPage())
this.set({[page]:getPage(),...(location.search?queryString(location.search):{})})
publish('init',state(this))},set(args){if(typeof args['$page']!='undefined'){this[page]=args[page]=args['$page']}
if(args['$clear']){allKeys.forEach(k=>this[k]=args[k]!=null?args[k]:'')}else{Object.keys(args).forEach(k=>{if(allKeys.indexOf(k)>=0){this[k]=args[k]}})}},get state(){return state(this)},to(args){this.set(args)
let cleanArgs=state(this)
if(typeof args.$on=='function')args.$on(cleanArgs)
let href=args.$qs?this.href({$qs:args.$qs}):this.href(null)
history.pushState(cleanArgs,this[page],href)
publish('to',cleanArgs)},href(args){if(args&&typeof args['$page']!='undefined')args[page]=args['$page']
let s=args?Object.assign({},state(this),args):state(this)
let path=s[page]||''
let qsArgs=queryKeys.filter(k=>s[k]).map(k=>`${encodeURIComponent(k)}=${encodeURIComponent(s[k])}`)
let $qs=args&&typeof args['$qs']=='object'?args['$qs']:null
if($qs){qsArgs=[...qsArgs,...Object.keys($qs).map(k=>`${encodeURIComponent(k)}=${encodeURIComponent($qs[k])}`)]}
let qs=qsArgs.join('&')
return path+(qs?'?'+qs:'')},...extend}
store=reactive(store)
app.directive('href',function(el,binding){el.href=store.href(binding)
el.onclick=e=>{e.preventDefault()
store.to(binding.value)}})
if(handlers){let init=handlers.init&&handlers.init.bind(store)
if(init)
events.subscribe('route:init',args=>init(args))
let to=handlers.to&&handlers.to.bind(store)
if(to)
events.subscribe('route:to',args=>to(args))
let nav=handlers.nav&&handlers.nav.bind(store)
if(nav)
events.subscribe('route:nav',args=>nav(args))}
app.onStart(app=>store.start())
return store}
export function useBreakpoints(app,options){if(!options)options={}
let{resolutions,handlers}=options
if(!resolutions)resolutions={'2xl':1536,xl:1280,lg:1024,md:768,sm:640}
let sizes=Object.keys(resolutions)
let previous={}
let events=app.events
let store={get previous(){return previous},get current(){return each(sizes,(o,res)=>o[res]=this[res])},snap(){let w=document.body.clientWidth
let current=each(sizes,(o,res)=>o[res]=w>resolutions[res])
let changed=false
sizes.forEach(res=>{if(current[res]!==this[res]){this[res]=current[res]
changed=true}})
if(changed){previous=current
events.publish('breakpoint:change',this)}},}
store=reactive(store)
on(window,{resize:()=>store.snap()})
if(handlers&&handlers.change)
events.subscribe('breakpoint:change',args=>handlers.change(args))
app.onStart(app=>store.snap())
return store}
export function setBodyClass(obj){let bodyCls=document.body.classList
Object.keys(obj).forEach(name=>{if(obj[name]){bodyCls.add(name)
bodyCls.remove(`no${name}`)}else{bodyCls.remove(name)
bodyCls.add(`no${name}`)}})}
export function setFavIcon(icon,defaultSrc){setFavIconSrc(icon.uri||defaultSrc)}
function setFavIconSrc(src){let link=document.querySelector("link[rel~='icon']")
if(!link){link=document.createElement('link')
link.rel='icon'
document.querySelector('head').appendChild(link)}
link.href=src}
const SORT_METHODS=['GET','POST','PATCH','PUT','DELETE']
function opSortName(op){let group=map(op.dataModel,x=>x.name)||map(op.request.inherits,x=>x.genericArgs&&x.genericArgs[0])
let sort1=group?group+map(SORT_METHODS.indexOf(op.method||'ANY'),x=>x===-1?'':x.toString()):'z'
return sort1+`_`+op.request.name}
export function sortOps(ops){ops.sort((a,b)=>opSortName(a).localeCompare(opSortName(b)))
return ops}
let defaultIcon=globalThis.Server.ui.theme.modelIcon||{svg:`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-width="1.5"><path d="M5 12v6s0 3 7 3s7-3 7-3v-6"/><path d="M5 6v6s0 3 7 3s7-3 7-3V6"/><path d="M12 3c7 0 7 3 7 3s0 3-7 3s-7-3-7-3s0-3 7-3Z"/></g></svg>`}
export function getIcon({op,type}){if(op){let img=op.request.icon||typeOfRef(op.viewModel)?.icon||typeOfRef(op.dataModel)?.icon
if(img)
return img}
if(type&&type.icon){return type.icon}
return defaultIcon}
export function hasItems(obj){return!obj?false:typeof obj==='object'?Object.keys(obj).length>0:obj.length}
export function indentJson(o,space=4){return useFormatters().indentJson(o,space)}
export function prettyJson(o){return useFormatters().prettyJson(o)}
export function scrub(o){return useFormatters().scrub(o)}
export function mapGetForInput(o,id){let ret=apiValue(mapGet(o,id))
return isDate(ret)?`${ret.getFullYear()}-${padInt(ret.getMonth() + 1)}-${padInt(ret.getDate())}`:ret}
export const parseJsv=(()=>{function jsvParse(jsv){if(!jsv)return jsv;if(jsv[0]==='{')
return jsvParseObject(jsv);else if(jsv[0]==='[')
return jsvParseArray(jsv);else
return jsvParseString(jsv);}
function jsvParseObject(s){if(s[0]!=='{')
throw"Type definitions should start with a '{', got string starting with: "
+s.substr(0,s.length<50?s.length:50);let k,obj={};if(s==='{}')return null;for(let ref={i:1},len=s.length;ref.i<len;ref.i++){k=jsvEatMapKey(s,ref);ref.i++;let v=jsvEatMapValue(s,ref);obj[k]=jsvParse(v);}
return obj;}
function jsvParseString(s){return!s||s[0]!=='"'?s:s.substr(1,s.length-2).replace(/""/g,'"');}
function jsvEatMapKey(s,ref){let pos=ref.i;while(s[++ref.i]!==':'&&ref.i<s.length){}
return s.substr(pos,ref.i-pos);}
function jsvEatMapValue(s,ref){let tokPos=ref.i;let sLen=s.length;if(ref.i===sLen)return null;let c=s[ref.i];if(c===','||c==='}')
return null;let inQ=false;if(c==='['){let endsToEat=1;while(++ref.i<sLen&&endsToEat>0){c=s[ref.i];if(c==='"')
inQ=!inQ;if(inQ)
continue;if(c==='[')
endsToEat++;if(c===']')
endsToEat--;}
return s.substr(tokPos,ref.i-tokPos);}
if(c==='{'){let endsToEat=1;while(++ref.i<sLen&&endsToEat>0){c=s[ref.i];if(c==='"')
inQ=!inQ;if(inQ)
continue;if(c==='{')
endsToEat++;if(c==='}')
endsToEat--;}
return s.substr(tokPos,ref.i-tokPos);}
if(c==='"'){while(++ref.i<sLen){c=s[ref.i];if(c!=='"')continue;let isQuote=ref.i+1<sLen&&s[ref.i+1]==='"';ref.i++;if(!isQuote)break;}
return s.substr(tokPos,ref.i-tokPos);}
while(++ref.i<sLen){c=s[ref.i];if(c===','||c==='}')break;}
return s.substr(tokPos,ref.i-tokPos);}
function jsvParseArray(str){let to=[],s=jsvStripList(str);if(!s)return to;if(s[0]==='{'){let ref={i:0};do{let v=jsvEatMapValue(s,ref);to.push(jsvParse(v));}while(++ref.i<s.length);}else{for(let ref={i:0};ref.i<s.length;ref.i++){let v=jsvEatUntil(s,ref,',');to.push(jsvParse(v));}}
return to;}
function jsvStripList(s){if(!s)return null;return s[0]==='['?s.substr(1,s.length-2):s;}
function jsvEatUntil(s,ref,findChar){let tokPos=ref.i;let sLen=s.length;if(s[tokPos]!=='"'){ref.i=s.indexOf(findChar,tokPos);if(ref.i===-1)ref.i=sLen;return s.substr(tokPos,ref.i-tokPos);}
while(++ref.i<sLen){if(s[ref.i]==='"'){if(ref.i+1>=sLen)
return s.substr(tokPos,++ref.i-tokPos);if(s[ref.i+1]==='"')
ref.i++;else if(s[ref.i+1]===findChar)
return s.substr(tokPos,++ref.i-tokPos);}}
throw"Could not find ending quote";}
return jsvParse})()
