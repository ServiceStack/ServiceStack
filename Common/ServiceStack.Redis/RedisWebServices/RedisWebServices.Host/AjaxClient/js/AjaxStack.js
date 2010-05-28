/*
 * jQuery JavaScript Library v1.4.1
 * http://jquery.com/
 *
 * Copyright 2010, John Resig
 * Dual licensed under the MIT or GPL Version 2 licenses.
 * http://jquery.org/license
 *
 * Includes Sizzle.js
 * http://sizzlejs.com/
 * Copyright 2010, The Dojo Foundation
 * Released under the MIT, BSD, and GPL Licenses.
 *
 * Date: Mon Jan 25 19:43:33 2010 -0500
 */
(function(H,M){function bc(){if(!ah.isReady){try{R.documentElement.doScroll("left")}catch(c){setTimeout(bc,1);return}ah.ready()}}function bg(r,c){c.src?ah.ajax({url:c.src,async:false,dataType:"script"}):ah.globalEval(c.text||c.textContent||c.innerHTML||"");c.parentNode&&c.parentNode.removeChild(c)}function ap(r,c,J,F,G,z){var v=r.length;if(typeof c==="object"){for(var K in c){ap(r,K,c[K],F,G,J)}return r}if(J!==M){F=!z&&F&&ah.isFunction(J);for(K=0;K<v;K++){G(r[K],c,F?J.call(r[K],K,G(r[K],c)):J,z)}return r}return v?G(r[0],c):null}function aF(){return(new Date).getTime()}function ao(){return false}function am(){return true}function aY(r,c,v){v[0].type=r;return ah.event.handle.apply(c,v)}function aK(O){var L,K=[],G=[],J=arguments,F,z,r,c,v,P,N=ah.extend({},ah.data(this,"events").live);if(!(O.button&&O.type==="click")){for(c in N){z=N[c];if(z.live===O.type||z.altLive&&ah.inArray(O.type,z.altLive)>-1){F=z.data;F.beforeFilter&&F.beforeFilter[O.type]&&!F.beforeFilter[O.type](O)||G.push(z.selector)}else{delete N[c]}}F=ah(O.target).closest(G,O.currentTarget);v=0;for(P=F.length;v<P;v++){for(c in N){z=N[c];r=F[v].elem;G=null;if(F[v].selector===z.selector){if(z.live==="mouseenter"||z.live==="mouseleave"){G=ah(O.relatedTarget).closest(z.selector)[0]}if(!G||G!==r){K.push({elem:r,fn:z})}}}}v=0;for(P=K.length;v<P;v++){F=K[v];O.currentTarget=F.elem;O.data=F.fn.data;if(F.fn.apply(F.elem,J)===false){L=false;break}}return L}}function ag(r,c){return"live."+(r?r+".":"")+c.replace(/\./g,"`").replace(/ /g,"&")}function A(c){return !c||!c.parentNode||c.parentNode.nodeType===11}function l(r,c){var v=0;c.each(function(){if(this.nodeName===(r[v]&&r[v].nodeName)){var G=ah.data(r[v++]),J=ah.data(this,G);if(G=G&&G.events){delete J.handle;J.events={};for(var F in G){for(var z in G[F]){ah.event.add(this,F,G[F][z],G[F][z].data)}}}}})}function bf(r,c,G){var z,F,v;if(r.length===1&&typeof r[0]==="string"&&r[0].length<512&&r[0].indexOf("<option")<0&&(ah.support.checkClone||!a2.test(r[0]))){F=true;if(v=ah.fragments[r[0]]){if(v!==1){z=v}}}if(!z){c=c&&c[0]?c[0].ownerDocument||c[0]:R;z=c.createDocumentFragment();ah.clean(r,c,z,G)}if(F){ah.fragments[r[0]]=v?z:1}return{fragment:z,cacheable:F}}function aC(r,c){var v={};ah.each(aO.concat.apply([],aO.slice(0,c)),function(){v[this]=r});return v}function ak(c){return"scrollTo" in c&&c.document?c:c.nodeType===9?c.defaultView||c.parentWindow:false}var ah=function(r,c){return new ah.fn.init(r,c)},a3=H.jQuery,aP=H.$,R=H.document,au,al=/^[^<]*(<[\w\W]+>)[^>]*$|^#([\w-]+)$/,E=/^.[^:#\[\.,]*$/,p=/\S/,d=/^(\s|\u00A0)+|(\s|\u00A0)+$/g,a5=/^<(\w+)\s*\/?>(?:<\/\1>)?$/,az=navigator.userAgent,D=false,ax=[],aB,a0=Object.prototype.toString,aT=Object.prototype.hasOwnProperty,ay=Array.prototype.push,av=Array.prototype.slice,o=Array.prototype.indexOf;ah.fn=ah.prototype={init:function(r,c){var z,v;if(!r){return this}if(r.nodeType){this.context=this[0]=r;this.length=1;return this}if(typeof r==="string"){if((z=al.exec(r))&&(z[1]||!c)){if(z[1]){v=c?c.ownerDocument||c:R;if(r=a5.exec(r)){if(ah.isPlainObject(c)){r=[R.createElement(r[1])];ah.fn.attr.call(r,c,true)}else{r=[v.createElement(r[1])]}}else{r=bf([z[1]],[v]);r=(r.cacheable?r.fragment.cloneNode(true):r.fragment).childNodes}}else{if(c=R.getElementById(z[2])){if(c.id!==z[2]){return au.find(r)}this.length=1;this[0]=c}this.context=R;this.selector=r;return this}}else{if(!c&&/^\w+$/.test(r)){this.selector=r;this.context=R;r=R.getElementsByTagName(r)}else{return !c||c.jquery?(c||au).find(r):ah(c).find(r)}}}else{if(ah.isFunction(r)){return au.ready(r)}}if(r.selector!==M){this.selector=r.selector;this.context=r.context}return ah.isArray(r)?this.setArray(r):ah.makeArray(r,this)},selector:"",jquery:"1.4.1",length:0,size:function(){return this.length},toArray:function(){return av.call(this,0)},get:function(c){return c==null?this.toArray():c<0?this.slice(c)[0]:this[c]},pushStack:function(r,c,v){r=ah(r||null);r.prevObject=this;r.context=this.context;if(c==="find"){r.selector=this.selector+(this.selector?" ":"")+v}else{if(c){r.selector=this.selector+"."+c+"("+v+")"}}return r},setArray:function(c){this.length=0;ay.apply(this,c);return this},each:function(r,c){return ah.each(this,r,c)},ready:function(c){ah.bindReady();if(ah.isReady){c.call(R,ah)}else{ax&&ax.push(c)}return this},eq:function(c){return c===-1?this.slice(c):this.slice(c,+c+1)},first:function(){return this.eq(0)},last:function(){return this.eq(-1)},slice:function(){return this.pushStack(av.apply(this,arguments),"slice",av.call(arguments).join(","))},map:function(c){return this.pushStack(ah.map(this,function(r,v){return c.call(r,v,r)}))},end:function(){return this.prevObject||ah(null)},push:ay,sort:[].sort,splice:[].splice};ah.fn.init.prototype=ah.fn;ah.extend=ah.fn.extend=function(){var r=arguments[0]||{},c=1,J=arguments.length,F=false,G,z,v,K;if(typeof r==="boolean"){F=r;r=arguments[1]||{};c=2}if(typeof r!=="object"&&!ah.isFunction(r)){r={}}if(J===c){r=this;--c}for(;c<J;c++){if((G=arguments[c])!=null){for(z in G){v=r[z];K=G[z];if(r!==K){if(F&&K&&(ah.isPlainObject(K)||ah.isArray(K))){v=v&&(ah.isPlainObject(v)||ah.isArray(v))?v:ah.isArray(K)?[]:{};r[z]=ah.extend(F,v,K)}else{if(K!==M){r[z]=K}}}}}}return r};ah.extend({noConflict:function(c){H.$=aP;if(c){H.jQuery=a3}return ah},isReady:false,ready:function(){if(!ah.isReady){if(!R.body){return setTimeout(ah.ready,13)}ah.isReady=true;if(ax){for(var r,c=0;r=ax[c++];){r.call(R,ah)}ax=null}ah.fn.triggerHandler&&ah(R).triggerHandler("ready")}},bindReady:function(){if(!D){D=true;if(R.readyState==="complete"){return ah.ready()}if(R.addEventListener){R.addEventListener("DOMContentLoaded",aB,false);H.addEventListener("load",ah.ready,false)}else{if(R.attachEvent){R.attachEvent("onreadystatechange",aB);H.attachEvent("onload",ah.ready);var r=false;try{r=H.frameElement==null}catch(c){}R.documentElement.doScroll&&r&&bc()}}}},isFunction:function(c){return a0.call(c)==="[object Function]"},isArray:function(c){return a0.call(c)==="[object Array]"},isPlainObject:function(r){if(!r||a0.call(r)!=="[object Object]"||r.nodeType||r.setInterval){return false}if(r.constructor&&!aT.call(r,"constructor")&&!aT.call(r.constructor.prototype,"isPrototypeOf")){return false}var c;for(c in r){}return c===M||aT.call(r,c)},isEmptyObject:function(r){for(var c in r){return false}return true},error:function(c){throw c},parseJSON:function(c){if(typeof c!=="string"||!c){return null}if(/^[\],:{}\s]*$/.test(c.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g,"@").replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g,"]").replace(/(?:^|:|,)(?:\s*\[)+/g,""))){return H.JSON&&H.JSON.parse?H.JSON.parse(c):(new Function("return "+c))()}else{ah.error("Invalid JSON: "+c)}},noop:function(){},globalEval:function(r){if(r&&p.test(r)){var c=R.getElementsByTagName("head")[0]||R.documentElement,v=R.createElement("script");v.type="text/javascript";if(ah.support.scriptEval){v.appendChild(R.createTextNode(r))}else{v.text=r}c.insertBefore(v,c.firstChild);c.removeChild(v)}},nodeName:function(r,c){return r.nodeName&&r.nodeName.toUpperCase()===c.toUpperCase()},each:function(r,c,J){var F,G=0,z=r.length,v=z===M||ah.isFunction(r);if(J){if(v){for(F in r){if(c.apply(r[F],J)===false){break}}}else{for(;G<z;){if(c.apply(r[G++],J)===false){break}}}}else{if(v){for(F in r){if(c.call(r[F],F,r[F])===false){break}}}else{for(J=r[0];G<z&&c.call(J,G,J)!==false;J=r[++G]){}}}return r},trim:function(c){return(c||"").replace(d,"")},makeArray:function(r,c){c=c||[];if(r!=null){r.length==null||typeof r==="string"||ah.isFunction(r)||typeof r!=="function"&&r.setInterval?ay.call(c,r):ah.merge(c,r)}return c},inArray:function(r,c){if(c.indexOf){return c.indexOf(r)}for(var z=0,v=c.length;z<v;z++){if(c[z]===r){return z}}return -1},merge:function(r,c){var F=r.length,v=0;if(typeof c.length==="number"){for(var z=c.length;v<z;v++){r[F++]=c[v]}}else{for(;c[v]!==M;){r[F++]=c[v++]}}r.length=F;return r},grep:function(r,c,G){for(var z=[],F=0,v=r.length;F<v;F++){!G!==!c(r[F],F)&&z.push(r[F])}return z},map:function(r,c,J){for(var F=[],G,z=0,v=r.length;z<v;z++){G=c(r[z],z,J);if(G!=null){F[F.length]=G}}return F.concat.apply([],F)},guid:1,proxy:function(r,c,v){if(arguments.length===2){if(typeof c==="string"){v=r;r=v[c];c=M}else{if(c&&!ah.isFunction(c)){v=c;c=M}}}if(!c&&r){c=function(){return r.apply(v||this,arguments)}}if(r){c.guid=r.guid=r.guid||c.guid||ah.guid++}return c},uaMatch:function(c){c=c.toLowerCase();c=/(webkit)[ \/]([\w.]+)/.exec(c)||/(opera)(?:.*version)?[ \/]([\w.]+)/.exec(c)||/(msie) ([\w.]+)/.exec(c)||!/compatible/.test(c)&&/(mozilla)(?:.*? rv:([\w.]+))?/.exec(c)||[];return{browser:c[1]||"",version:c[2]||"0"}},browser:{}});az=ah.uaMatch(az);if(az.browser){ah.browser[az.browser]=true;ah.browser.version=az.version}if(ah.browser.webkit){ah.browser.safari=true}if(o){ah.inArray=function(r,c){return o.call(c,r)}}au=ah(R);if(R.addEventListener){aB=function(){R.removeEventListener("DOMContentLoaded",aB,false);ah.ready()}}else{if(R.attachEvent){aB=function(){if(R.readyState==="complete"){R.detachEvent("onreadystatechange",aB);ah.ready()}}}}(function(){ah.support={};var r=R.documentElement,c=R.createElement("script"),J=R.createElement("div"),F="script"+aF();J.style.display="none";J.innerHTML="   <link/><table></table><a href='/a' style='color:red;float:left;opacity:.55;'>a</a><input type='checkbox'/>";var G=J.getElementsByTagName("*"),z=J.getElementsByTagName("a")[0];if(!(!G||!G.length||!z)){ah.support={leadingWhitespace:J.firstChild.nodeType===3,tbody:!J.getElementsByTagName("tbody").length,htmlSerialize:!!J.getElementsByTagName("link").length,style:/red/.test(z.getAttribute("style")),hrefNormalized:z.getAttribute("href")==="/a",opacity:/^0.55$/.test(z.style.opacity),cssFloat:!!z.style.cssFloat,checkOn:J.getElementsByTagName("input")[0].value==="on",optSelected:R.createElement("select").appendChild(R.createElement("option")).selected,checkClone:false,scriptEval:false,noCloneEvent:true,boxModel:null};c.type="text/javascript";try{c.appendChild(R.createTextNode("window."+F+"=1;"))}catch(v){}r.insertBefore(c,r.firstChild);if(H[F]){ah.support.scriptEval=true;delete H[F]}r.removeChild(c);if(J.attachEvent&&J.fireEvent){J.attachEvent("onclick",function K(){ah.support.noCloneEvent=false;J.detachEvent("onclick",K)});J.cloneNode(true).fireEvent("onclick")}J=R.createElement("div");J.innerHTML="<input type='radio' name='radiotest' checked='checked'/>";r=R.createDocumentFragment();r.appendChild(J.firstChild);ah.support.checkClone=r.cloneNode(true).cloneNode(true).lastChild.checked;ah(function(){var L=R.createElement("div");L.style.width=L.style.paddingLeft="1px";R.body.appendChild(L);ah.boxModel=ah.support.boxModel=L.offsetWidth===2;R.body.removeChild(L).style.display="none"});r=function(O){var N=R.createElement("div");O="on"+O;var L=O in N;if(!L){N.setAttribute(O,"return;");L=typeof N[O]==="function"}return L};ah.support.submitBubbles=r("submit");ah.support.changeBubbles=r("change");r=c=J=G=z=null}})();ah.props={"for":"htmlFor","class":"className",readonly:"readOnly",maxlength:"maxLength",cellspacing:"cellSpacing",rowspan:"rowSpan",colspan:"colSpan",tabindex:"tabIndex",usemap:"useMap",frameborder:"frameBorder"};var aH="jQuery"+aF(),aR=0,b={},an={};ah.extend({cache:{},expando:aH,noData:{embed:true,object:true,applet:true},data:function(r,c,F){if(!(r.nodeName&&ah.noData[r.nodeName.toLowerCase()])){r=r==H?b:r;var v=r[aH],z=ah.cache;if(!c&&!v){return null}v||(v=++aR);if(typeof c==="object"){r[aH]=v;z=z[v]=ah.extend(true,{},c)}else{z=z[v]?z[v]:typeof F==="undefined"?an:(z[v]={})}if(F!==M){r[aH]=v;z[c]=F}return typeof c==="string"?z[c]:z}},removeData:function(r,c){if(!(r.nodeName&&ah.noData[r.nodeName.toLowerCase()])){r=r==H?b:r;var G=r[aH],z=ah.cache,F=z[G];if(c){if(F){delete F[c];ah.isEmptyObject(F)&&ah.removeData(r)}}else{try{delete r[aH]}catch(v){r.removeAttribute&&r.removeAttribute(aH)}delete z[G]}}}});ah.fn.extend({data:function(r,c){if(typeof r==="undefined"&&this.length){return ah.data(this[0])}else{if(typeof r==="object"){return this.each(function(){ah.data(this,r)})}}var z=r.split(".");z[1]=z[1]?"."+z[1]:"";if(c===M){var v=this.triggerHandler("getData"+z[1]+"!",[z[0]]);if(v===M&&this.length){v=ah.data(this[0],r)}return v===M&&z[1]?this.data(z[0]):v}else{return this.trigger("setData"+z[1]+"!",[z[0],c]).each(function(){ah.data(this,r,c)})}},removeData:function(c){return this.each(function(){ah.removeData(this,c)})}});ah.extend({queue:function(r,c,z){if(r){c=(c||"fx")+"queue";var v=ah.data(r,c);if(!z){return v||[]}if(!v||ah.isArray(z)){v=ah.data(r,c,ah.makeArray(z))}else{v.push(z)}return v}},dequeue:function(r,c){c=c||"fx";var z=ah.queue(r,c),v=z.shift();if(v==="inprogress"){v=z.shift()}if(v){c==="fx"&&z.unshift("inprogress");v.call(r,function(){ah.dequeue(r,c)})}}});ah.fn.extend({queue:function(r,c){if(typeof r!=="string"){c=r;r="fx"}if(c===M){return ah.queue(this[0],r)}return this.each(function(){var v=ah.queue(this,r,c);r==="fx"&&v[0]!=="inprogress"&&ah.dequeue(this,r)})},dequeue:function(c){return this.each(function(){ah.dequeue(this,c)})},delay:function(r,c){r=ah.fx?ah.fx.speeds[r]||r:r;c=c||"fx";return this.queue(c,function(){var v=this;setTimeout(function(){ah.dequeue(v,c)},r)})},clearQueue:function(c){return this.queue(c||"fx",[])}});var a4=/[\n\t]/g,ac=/\s+/,I=/\r/g,q=/href|src|style/,e=/(button|input)/i,a6=/(button|input|object|select|textarea)/i,aM=/^(a|area)$/i,aQ=/radio|checkbox/;ah.fn.extend({attr:function(r,c){return ap(this,r,c,true,ah.attr)},removeAttr:function(c){return this.each(function(){ah.attr(this,c,"");this.nodeType===1&&this.removeAttribute(c)})},addClass:function(r){if(ah.isFunction(r)){return this.each(function(N){var L=ah(this);L.addClass(r.call(this,N,L.attr("class")))})}if(r&&typeof r==="string"){for(var c=(r||"").split(ac),J=0,F=this.length;J<F;J++){var G=this[J];if(G.nodeType===1){if(G.className){for(var z=" "+G.className+" ",v=0,K=c.length;v<K;v++){if(z.indexOf(" "+c[v]+" ")<0){G.className+=" "+c[v]}}}else{G.className=r}}}}return this},removeClass:function(r){if(ah.isFunction(r)){return this.each(function(N){var L=ah(this);L.removeClass(r.call(this,N,L.attr("class")))})}if(r&&typeof r==="string"||r===M){for(var c=(r||"").split(ac),J=0,F=this.length;J<F;J++){var G=this[J];if(G.nodeType===1&&G.className){if(r){for(var z=(" "+G.className+" ").replace(a4," "),v=0,K=c.length;v<K;v++){z=z.replace(" "+c[v]+" "," ")}G.className=z.substring(1,z.length-1)}else{G.className=""}}}}return this},toggleClass:function(r,c){var z=typeof r,v=typeof c==="boolean";if(ah.isFunction(r)){return this.each(function(G){var F=ah(this);F.toggleClass(r.call(this,G,F.attr("class"),c),c)})}return this.each(function(){if(z==="string"){for(var J,G=0,F=ah(this),L=c,K=r.split(ac);J=K[G++];){L=v?L:!F.hasClass(J);F[L?"addClass":"removeClass"](J)}}else{if(z==="undefined"||z==="boolean"){this.className&&ah.data(this,"__className__",this.className);this.className=this.className||r===false?"":ah.data(this,"__className__")||""}}})},hasClass:function(r){r=" "+r+" ";for(var c=0,v=this.length;c<v;c++){if((" "+this[c].className+" ").replace(a4," ").indexOf(r)>-1){return true}}return false},val:function(r){if(r===M){var c=this[0];if(c){if(ah.nodeName(c,"option")){return(c.attributes.value||{}).specified?c.value:c.text}if(ah.nodeName(c,"select")){var J=c.selectedIndex,F=[],G=c.options;c=c.type==="select-one";if(J<0){return null}var z=c?J:0;for(J=c?J+1:G.length;z<J;z++){var v=G[z];if(v.selected){r=ah(v).val();if(c){return r}F.push(r)}}return F}if(aQ.test(c.type)&&!ah.support.checkOn){return c.getAttribute("value")===null?"on":c.value}return(c.value||"").replace(I,"")}return M}var K=ah.isFunction(r);return this.each(function(P){var N=ah(this),O=r;if(this.nodeType===1){if(K){O=r.call(this,P,N.val())}if(typeof O==="number"){O+=""}if(ah.isArray(O)&&aQ.test(this.type)){this.checked=ah.inArray(N.val(),O)>=0}else{if(ah.nodeName(this,"select")){var L=ah.makeArray(O);ah("option",this).each(function(){this.selected=ah.inArray(ah(this).val(),L)>=0});if(!L.length){this.selectedIndex=-1}}else{this.value=O}}}})}});ah.extend({attrFn:{val:true,css:true,html:true,text:true,data:true,width:true,height:true,offset:true},attr:function(r,c,G,z){if(!r||r.nodeType===3||r.nodeType===8){return M}if(z&&c in ah.attrFn){return ah(r)[c](G)}z=r.nodeType!==1||!ah.isXMLDoc(r);var F=G!==M;c=z&&ah.props[c]||c;if(r.nodeType===1){var v=q.test(c);if(c in r&&z&&!v){if(F){c==="type"&&e.test(r.nodeName)&&r.parentNode&&ah.error("type property can't be changed");r[c]=G}if(ah.nodeName(r,"form")&&r.getAttributeNode(c)){return r.getAttributeNode(c).nodeValue}if(c==="tabIndex"){return(c=r.getAttributeNode("tabIndex"))&&c.specified?c.value:a6.test(r.nodeName)||aM.test(r.nodeName)&&r.href?0:M}return r[c]}if(!ah.support.style&&z&&c==="style"){if(F){r.style.cssText=""+G}return r.style.cssText}F&&r.setAttribute(c,""+G);r=!ah.support.hrefNormalized&&z&&v?r.getAttribute(c,2):r.getAttribute(c);return r===null?M:r}return ah.style(r,c,G)}});var aS=function(c){return c.replace(/[^\w\s\.\|`]/g,function(r){return"\\"+r})};ah.event={add:function(O,L,K,G){if(!(O.nodeType===3||O.nodeType===8)){if(O.setInterval&&O!==H&&!O.frameElement){O=H}if(!K.guid){K.guid=ah.guid++}if(G!==M){K=ah.proxy(K);K.data=G}var J=ah.data(O,"events")||ah.data(O,"events",{}),F=ah.data(O,"handle"),z;if(!F){z=function(){return typeof ah!=="undefined"&&!ah.event.triggered?ah.event.handle.apply(z.elem,arguments):M};F=ah.data(O,"handle",z)}if(F){F.elem=O;L=L.split(/\s+/);for(var r,c=0;r=L[c++];){var v=r.split(".");r=v.shift();if(c>1){K=ah.proxy(K);if(G!==M){K.data=G}}K.type=v.slice(0).sort().join(".");var P=J[r],N=this.special[r]||{};if(!P){P=J[r]={};if(!N.setup||N.setup.call(O,G,v,K)===false){if(O.addEventListener){O.addEventListener(r,F,false)}else{O.attachEvent&&O.attachEvent("on"+r,F)}}}if(N.add){if((v=N.add.call(O,K,G,v,P))&&ah.isFunction(v)){v.guid=v.guid||K.guid;v.data=v.data||K.data;v.type=v.type||K.type;K=v}}P[K.guid]=K;this.global[r]=true}O=null}}},global:{},remove:function(P,N,L){if(!(P.nodeType===3||P.nodeType===8)){var J=ah.data(P,"events"),K,G,F;if(J){if(N===M||typeof N==="string"&&N.charAt(0)==="."){for(G in J){this.remove(P,G+(N||""))}}else{if(N.type){L=N.handler;N=N.type}N=N.split(/\s+/);for(var v=0;G=N[v++];){var r=G.split(".");G=r.shift();var z=!r.length,Q=ah.map(r.slice(0).sort(),aS);Q=new RegExp("(^|\\.)"+Q.join("\\.(?:.*\\.)?")+"(\\.|$)");var O=this.special[G]||{};if(J[G]){if(L){F=J[G][L.guid];delete J[G][L.guid]}else{for(var c in J[G]){if(z||Q.test(J[G][c].type)){delete J[G][c]}}}O.remove&&O.remove.call(P,r,F);for(K in J[G]){break}if(!K){if(!O.teardown||O.teardown.call(P,r)===false){if(P.removeEventListener){P.removeEventListener(G,ah.data(P,"handle"),false)}else{P.detachEvent&&P.detachEvent("on"+G,ah.data(P,"handle"))}}K=null;delete J[G]}}}}for(K in J){break}if(!K){if(c=ah.data(P,"handle")){c.elem=null}ah.removeData(P,"events");ah.removeData(P,"handle")}}}},trigger:function(r,c,J,F){var G=r.type||r;if(!F){r=typeof r==="object"?r[aH]?r:ah.extend(ah.Event(G),r):ah.Event(G);if(G.indexOf("!")>=0){r.type=G=G.slice(0,-1);r.exclusive=true}if(!J){r.stopPropagation();this.global[G]&&ah.each(ah.cache,function(){this.events&&this.events[G]&&ah.event.trigger(r,c,this.handle.elem)})}if(!J||J.nodeType===3||J.nodeType===8){return M}r.result=M;r.target=J;c=ah.makeArray(c);c.unshift(r)}r.currentTarget=J;(F=ah.data(J,"handle"))&&F.apply(J,c);F=J.parentNode||J.ownerDocument;try{if(!(J&&J.nodeName&&ah.noData[J.nodeName.toLowerCase()])){if(J["on"+G]&&J["on"+G].apply(J,c)===false){r.result=false}}}catch(z){}if(!r.isPropagationStopped()&&F){ah.event.trigger(r,c,F,true)}else{if(!r.isDefaultPrevented()){J=r.target;var v;if(!(ah.nodeName(J,"a")&&G==="click")&&!(J&&J.nodeName&&ah.noData[J.nodeName.toLowerCase()])){try{if(J[G]){if(v=J["on"+G]){J["on"+G]=null}this.triggered=true;J[G]()}}catch(K){}if(v){J["on"+G]=v}this.triggered=false}}}},handle:function(r){var c,G;r=arguments[0]=ah.event.fix(r||H.event);r.currentTarget=this;G=r.type.split(".");r.type=G.shift();c=!G.length&&!r.exclusive;var z=new RegExp("(^|\\.)"+G.slice(0).sort().join("\\.(?:.*\\.)?")+"(\\.|$)");G=(ah.data(this,"events")||{})[r.type];for(var F in G){var v=G[F];if(c||z.test(v.type)){r.handler=v;r.data=v.data;v=v.apply(this,arguments);if(v!==M){r.result=v;if(v===false){r.preventDefault();r.stopPropagation()}}if(r.isImmediatePropagationStopped()){break}}}return r.result},props:"altKey attrChange attrName bubbles button cancelable charCode clientX clientY ctrlKey currentTarget data detail eventPhase fromElement handler keyCode layerX layerY metaKey newValue offsetX offsetY originalTarget pageX pageY prevValue relatedNode relatedTarget screenX screenY shiftKey srcElement target toElement view wheelDelta which".split(" "),fix:function(r){if(r[aH]){return r}var c=r;r=ah.Event(c);for(var z=this.props.length,v;z;){v=this.props[--z];r[v]=c[v]}if(!r.target){r.target=r.srcElement||R}if(r.target.nodeType===3){r.target=r.target.parentNode}if(!r.relatedTarget&&r.fromElement){r.relatedTarget=r.fromElement===r.target?r.toElement:r.fromElement}if(r.pageX==null&&r.clientX!=null){c=R.documentElement;z=R.body;r.pageX=r.clientX+(c&&c.scrollLeft||z&&z.scrollLeft||0)-(c&&c.clientLeft||z&&z.clientLeft||0);r.pageY=r.clientY+(c&&c.scrollTop||z&&z.scrollTop||0)-(c&&c.clientTop||z&&z.clientTop||0)}if(!r.which&&(r.charCode||r.charCode===0?r.charCode:r.keyCode)){r.which=r.charCode||r.keyCode}if(!r.metaKey&&r.ctrlKey){r.metaKey=r.ctrlKey}if(!r.which&&r.button!==M){r.which=r.button&1?1:r.button&2?3:r.button&4?2:0}return r},guid:100000000,proxy:ah.proxy,special:{ready:{setup:ah.bindReady,teardown:ah.noop},live:{add:function(r,c){ah.extend(r,c||{});r.guid+=c.selector+c.live;c.liveProxy=r;ah.event.add(this,c.live,aK,c)},remove:function(r){if(r.length){var c=0,v=new RegExp("(^|\\.)"+r[0]+"(\\.|$)");ah.each(ah.data(this,"events").live||{},function(){v.test(this.type)&&c++});c<1&&ah.event.remove(this,r[0],aK)}},special:{}},beforeunload:{setup:function(r,c,v){if(this.setInterval){this.onbeforeunload=v}return false},teardown:function(r,c){if(this.onbeforeunload===c){this.onbeforeunload=null}}}}};ah.Event=function(c){if(!this.preventDefault){return new ah.Event(c)}if(c&&c.type){this.originalEvent=c;this.type=c.type}else{this.type=c}this.timeStamp=aF();this[aH]=true};ah.Event.prototype={preventDefault:function(){this.isDefaultPrevented=am;var c=this.originalEvent;if(c){c.preventDefault&&c.preventDefault();c.returnValue=false}},stopPropagation:function(){this.isPropagationStopped=am;var c=this.originalEvent;if(c){c.stopPropagation&&c.stopPropagation();c.cancelBubble=true}},stopImmediatePropagation:function(){this.isImmediatePropagationStopped=am;this.stopPropagation()},isDefaultPrevented:ao,isPropagationStopped:ao,isImmediatePropagationStopped:ao};var a8=function(r){for(var c=r.relatedTarget;c&&c!==this;){try{c=c.parentNode}catch(v){break}}if(c!==this){r.type=r.data;ah.event.handle.apply(this,arguments)}},aW=function(c){c.type=c.data;ah.event.handle.apply(this,arguments)};ah.each({mouseenter:"mouseover",mouseleave:"mouseout"},function(r,c){ah.event.special[r]={setup:function(v){ah.event.add(this,c,v&&v.selector?aW:a8,r)},teardown:function(v){ah.event.remove(this,c,v&&v.selector?aW:a8)}}});if(!ah.support.submitBubbles){ah.event.special.submit={setup:function(r,c,v){if(this.nodeName.toLowerCase()!=="form"){ah.event.add(this,"click.specialSubmit."+v.guid,function(F){var G=F.target,z=G.type;if((z==="submit"||z==="image")&&ah(G).closest("form").length){return aY("submit",this,arguments)}});ah.event.add(this,"keypress.specialSubmit."+v.guid,function(F){var G=F.target,z=G.type;if((z==="text"||z==="password")&&ah(G).closest("form").length&&F.keyCode===13){return aY("submit",this,arguments)}})}else{return false}},remove:function(r,c){ah.event.remove(this,"click.specialSubmit"+(c?"."+c.guid:""));ah.event.remove(this,"keypress.specialSubmit"+(c?"."+c.guid:""))}}}if(!ah.support.changeBubbles){var t=/textarea|input|select/i;function aG(r){var c=r.type,v=r.value;if(c==="radio"||c==="checkbox"){v=r.checked}else{if(c==="select-multiple"){v=r.selectedIndex>-1?ah.map(r.options,function(z){return z.selected}).join("-"):""}else{if(r.nodeName.toLowerCase()==="select"){v=r.selectedIndex}}}return v}function g(r,c){var F=r.target,v,z;if(!(!t.test(F.nodeName)||F.readOnly)){v=ah.data(F,"_change_data");z=aG(F);if(r.type!=="focusout"||F.type!=="radio"){ah.data(F,"_change_data",z)}if(!(v===M||z===v)){if(v!=null||z){r.type="change";return ah.event.trigger(r,c,F)}}}}ah.event.special.change={filters:{focusout:g,click:function(r){var c=r.target,v=c.type;if(v==="radio"||v==="checkbox"||c.nodeName.toLowerCase()==="select"){return g.call(this,r)}},keydown:function(r){var c=r.target,v=c.type;if(r.keyCode===13&&c.nodeName.toLowerCase()!=="textarea"||r.keyCode===32&&(v==="checkbox"||v==="radio")||v==="select-multiple"){return g.call(this,r)}},beforeactivate:function(c){c=c.target;c.nodeName.toLowerCase()==="input"&&c.type==="radio"&&ah.data(c,"_change_data",aG(c))}},setup:function(r,c,z){for(var v in at){ah.event.add(this,v+".specialChange."+z.guid,at[v])}return t.test(this.nodeName)},remove:function(r,c){for(var v in at){ah.event.remove(this,v+".specialChange"+(c?"."+c.guid:""),at[v])}return t.test(this.nodeName)}};var at=ah.event.special.change.filters}R.addEventListener&&ah.each({focus:"focusin",blur:"focusout"},function(r,c){function v(z){z=ah.event.fix(z);z.type=c;return ah.event.handle.call(this,z)}ah.event.special[c]={setup:function(){this.addEventListener(r,v,true)},teardown:function(){this.removeEventListener(r,v,true)}}});ah.each(["bind","one"],function(r,c){ah.fn[c]=function(J,F,G){if(typeof J==="object"){for(var z in J){this[c](z,F,J[z],G)}return this}if(ah.isFunction(F)){G=F;F=M}var v=c==="one"?ah.proxy(G,function(K){ah(this).unbind(K,v);return G.apply(this,arguments)}):G;return J==="unload"&&c!=="one"?this.one(J,F,G):this.each(function(){ah.event.add(this,J,v,F)})}});ah.fn.extend({unbind:function(r,c){if(typeof r==="object"&&!r.preventDefault){for(var v in r){this.unbind(v,r[v])}return this}return this.each(function(){ah.event.remove(this,r,c)})},trigger:function(r,c){return this.each(function(){ah.event.trigger(r,c,this)})},triggerHandler:function(r,c){if(this[0]){r=ah.Event(r);r.preventDefault();r.stopPropagation();ah.event.trigger(r,c,this[0]);return r.result}},toggle:function(r){for(var c=arguments,v=1;v<c.length;){ah.proxy(r,c[v++])}return this.click(ah.proxy(r,function(z){var F=(ah.data(this,"lastToggle"+r.guid)||0)%v;ah.data(this,"lastToggle"+r.guid,F+1);z.preventDefault();return c[F].apply(this,arguments)||false}))},hover:function(r,c){return this.mouseenter(r).mouseleave(c||r)}});ah.each(["live","die"],function(r,c){ah.fn[c]=function(J,F,G){var z,v=0;if(ah.isFunction(F)){G=F;F=M}for(J=(J||"").split(/\s+/);(z=J[v++])!=null;){z=z==="focus"?"focusin":z==="blur"?"focusout":z==="hover"?J.push("mouseleave")&&"mouseenter":z;c==="live"?ah(this.context).bind(ag(z,this.selector),{data:F,selector:this.selector,live:z},G):ah(this.context).unbind(ag(z,this.selector),G?{guid:G.guid+this.selector+z}:null)}return this}});ah.each("blur focus focusin focusout load resize scroll unload click dblclick mousedown mouseup mousemove mouseover mouseout mouseenter mouseleave change select submit keydown keypress keyup error".split(" "),function(r,c){ah.fn[c]=function(v){return v?this.bind(c,v):this.trigger(c)};if(ah.attrFn){ah.attrFn[c]=true}});H.attachEvent&&!H.addEventListener&&H.attachEvent("onunload",function(){for(var r in ah.cache){if(ah.cache[r].handle){try{ah.event.remove(ah.cache[r].handle.elem)}catch(c){}}}});(function(){function X(ab){for(var aa="",Z,Y=0;ab[Y];Y++){Z=ab[Y];if(Z.nodeType===3||Z.nodeType===4){aa+=Z.nodeValue}else{if(Z.nodeType!==8){aa+=X(Z.childNodes)}}}return aa}function W(bb,ba,ab,aa,Y,Z){Y=0;for(var bi=aa.length;Y<bi;Y++){var bj=aa[Y];if(bj){bj=bj[bb];for(var bh=false;bj;){if(bj.sizcache===ab){bh=aa[bj.sizset];break}if(bj.nodeType===1&&!Z){bj.sizcache=ab;bj.sizset=Y}if(bj.nodeName.toLowerCase()===ba){bh=bj;break}bj=bj[bb]}aa[Y]=bh}}}function V(bb,ba,ab,aa,Y,Z){Y=0;for(var bi=aa.length;Y<bi;Y++){var bj=aa[Y];if(bj){bj=bj[bb];for(var bh=false;bj;){if(bj.sizcache===ab){bh=aa[bj.sizset];break}if(bj.nodeType===1){if(!Z){bj.sizcache=ab;bj.sizset=Y}if(typeof ba!=="string"){if(bj===ba){bh=true;break}}else{if(K.filter(ba,[bj]).length>0){bh=bj;break}}}bj=bj[bb]}aa[Y]=bh}}}var S=/((?:\((?:\([^()]+\)|[^()]+)+\)|\[(?:\[[^[\]]*\]|['"][^'"]*['"]|[^[\]'"]+)+\]|\\.|[^ >+~,(\[\\]+)+|[>+~])(\s*,\s*)?((?:.|\r|\n)*)/g,T=0,Q=Object.prototype.toString,P=false,N=true;[0,0].sort(function(){N=false;return 0});var K=function(bi,bh,ba,ab){ba=ba||[];var Z=bh=bh||R;if(bh.nodeType!==1&&bh.nodeType!==9){return[]}if(!bi||typeof bi!=="string"){return ba}for(var aa=[],bn,bo,bk,bb,bm=true,bj=r(bh),bl=bi;(S.exec(""),bn=S.exec(bl))!==null;){bl=bn[3];aa.push(bn[1]);if(bn[2]){bb=bn[3];break}}if(aa.length>1&&z.exec(bi)){if(aa.length===2&&O.relative[aa[0]]){bo=L(aa[0]+aa[1],bh)}else{for(bo=O.relative[aa[0]]?[bh]:K(aa.shift(),bh);aa.length;){bi=aa.shift();if(O.relative[bi]){bi+=aa.shift()}bo=L(bi,bo)}}}else{if(!ab&&aa.length>1&&bh.nodeType===9&&!bj&&O.match.ID.test(aa[0])&&!O.match.ID.test(aa[aa.length-1])){bn=K.find(aa.shift(),bh,bj);bh=bn.expr?K.filter(bn.expr,bn.set)[0]:bn.set[0]}if(bh){bn=ab?{expr:aa.pop(),set:J(ab)}:K.find(aa.pop(),aa.length===1&&(aa[0]==="~"||aa[0]==="+")&&bh.parentNode?bh.parentNode:bh,bj);bo=bn.expr?K.filter(bn.expr,bn.set):bn.set;if(aa.length>0){bk=J(bo)}else{bm=false}for(;aa.length;){var Y=aa.pop();bn=Y;if(O.relative[Y]){bn=aa.pop()}else{Y=""}if(bn==null){bn=bh}O.relative[Y](bk,bn,bj)}}else{bk=[]}}bk||(bk=bo);bk||K.error(Y||bi);if(Q.call(bk)==="[object Array]"){if(bm){if(bh&&bh.nodeType===1){for(bi=0;bk[bi]!=null;bi++){if(bk[bi]&&(bk[bi]===true||bk[bi].nodeType===1&&v(bh,bk[bi]))){ba.push(bo[bi])}}}else{for(bi=0;bk[bi]!=null;bi++){bk[bi]&&bk[bi].nodeType===1&&ba.push(bo[bi])}}}else{ba.push.apply(ba,bk)}}else{J(bk,ba)}if(bb){K(bb,Z,ba,ab);K.uniqueSort(ba)}return ba};K.uniqueSort=function(Z){if(F){P=N;Z.sort(F);if(P){for(var Y=1;Y<Z.length;Y++){Z[Y]===Z[Y-1]&&Z.splice(Y--,1)}}}return Z};K.matches=function(Z,Y){return K(Z,null,null,Y)};K.find=function(bb,ba,ab){var aa,Y;if(!bb){return[]}for(var Z=0,bi=O.order.length;Z<bi;Z++){var bj=O.order[Z];if(Y=O.leftMatch[bj].exec(bb)){var bh=Y[1];Y.splice(1,1);if(bh.substr(bh.length-1)!=="\\"){Y[1]=(Y[1]||"").replace(/\\/g,"");aa=O.find[bj](Y,ba,ab);if(aa!=null){bb=bb.replace(O.match[bj],"");break}}}}aa||(aa=ba.getElementsByTagName("*"));return{set:aa,expr:bb}};K.filter=function(bk,bj,bh,bb){for(var Z=bk,ab=[],bp=bj,bq,bm,bi=bj&&bj[0]&&r(bj[0]);bk&&bj.length;){for(var bo in O.filter){if((bq=O.leftMatch[bo].exec(bk))!=null&&bq[2]){var bl=O.filter[bo],bn,Y;Y=bq[1];bm=false;bq.splice(1,1);if(Y.substr(Y.length-1)!=="\\"){if(bp===ab){ab=[]}if(O.preFilter[bo]){if(bq=O.preFilter[bo](bq,bp,bh,ab,bb,bi)){if(bq===true){continue}}else{bm=bn=true}}if(bq){for(var ba=0;(Y=bp[ba])!=null;ba++){if(Y){bn=bl(Y,bq,ba,bp);var aa=bb^!!bn;if(bh&&bn!=null){if(aa){bm=true}else{bp[ba]=false}}else{if(aa){ab.push(Y);bm=true}}}}}if(bn!==M){bh||(bp=ab);bk=bk.replace(O.match[bo],"");if(!bm){return[]}break}}}}if(bk===Z){if(bm==null){K.error(bk)}else{break}}Z=bk}return bp};K.error=function(Y){throw"Syntax error, unrecognized expression: "+Y};var O=K.selectors={order:["ID","NAME","TAG"],match:{ID:/#((?:[\w\u00c0-\uFFFF-]|\\.)+)/,CLASS:/\.((?:[\w\u00c0-\uFFFF-]|\\.)+)/,NAME:/\[name=['"]*((?:[\w\u00c0-\uFFFF-]|\\.)+)['"]*\]/,ATTR:/\[\s*((?:[\w\u00c0-\uFFFF-]|\\.)+)\s*(?:(\S?=)\s*(['"]*)(.*?)\3|)\s*\]/,TAG:/^((?:[\w\u00c0-\uFFFF\*-]|\\.)+)/,CHILD:/:(only|nth|last|first)-child(?:\((even|odd|[\dn+-]*)\))?/,POS:/:(nth|eq|gt|lt|first|last|even|odd)(?:\((\d*)\))?(?=[^-]|$)/,PSEUDO:/:((?:[\w\u00c0-\uFFFF-]|\\.)+)(?:\((['"]?)((?:\([^\)]+\)|[^\(\)]*)+)\2\))?/},leftMatch:{},attrMap:{"class":"className","for":"htmlFor"},attrHandle:{href:function(Y){return Y.getAttribute("href")}},relative:{"+":function(ab,aa){var Z=typeof aa==="string",Y=Z&&!/\W/.test(aa);Z=Z&&!Y;if(Y){aa=aa.toLowerCase()}Y=0;for(var ba=ab.length,bb;Y<ba;Y++){if(bb=ab[Y]){for(;(bb=bb.previousSibling)&&bb.nodeType!==1;){}ab[Y]=Z||bb&&bb.nodeName.toLowerCase()===aa?bb||false:bb===aa}}Z&&K.filter(aa,ab,true)},">":function(ab,aa){var Z=typeof aa==="string";if(Z&&!/\W/.test(aa)){aa=aa.toLowerCase();for(var Y=0,ba=ab.length;Y<ba;Y++){var bb=ab[Y];if(bb){Z=bb.parentNode;ab[Y]=Z.nodeName.toLowerCase()===aa?Z:false}}}else{Y=0;for(ba=ab.length;Y<ba;Y++){if(bb=ab[Y]){ab[Y]=Z?bb.parentNode:bb.parentNode===aa}}Z&&K.filter(aa,ab,true)}},"":function(ab,aa,Z){var Y=T++,ba=V;if(typeof aa==="string"&&!/\W/.test(aa)){var bb=aa=aa.toLowerCase();ba=W}ba("parentNode",aa,Y,ab,bb,Z)},"~":function(ab,aa,Z){var Y=T++,ba=V;if(typeof aa==="string"&&!/\W/.test(aa)){var bb=aa=aa.toLowerCase();ba=W}ba("previousSibling",aa,Y,ab,bb,Z)}},find:{ID:function(aa,Z,Y){if(typeof Z.getElementById!=="undefined"&&!Y){return(aa=Z.getElementById(aa[1]))?[aa]:[]}},NAME:function(ab,aa){if(typeof aa.getElementsByName!=="undefined"){var Z=[];aa=aa.getElementsByName(ab[1]);for(var Y=0,ba=aa.length;Y<ba;Y++){aa[Y].getAttribute("name")===ab[1]&&Z.push(aa[Y])}return Z.length===0?null:Z}},TAG:function(Z,Y){return Y.getElementsByTagName(Z[1])}},preFilter:{CLASS:function(ba,ab,Z,Y,bb,bh){ba=" "+ba[1].replace(/\\/g,"")+" ";if(bh){return ba}bh=0;for(var aa;(aa=ab[bh])!=null;bh++){if(aa){if(bb^(aa.className&&(" "+aa.className+" ").replace(/[\t\n]/g," ").indexOf(ba)>=0)){Z||Y.push(aa)}else{if(Z){ab[bh]=false}}}}return false},ID:function(Y){return Y[1].replace(/\\/g,"")},TAG:function(Y){return Y[1].toLowerCase()},CHILD:function(Z){if(Z[1]==="nth"){var Y=/(-?)(\d*)n((?:\+|-)?\d*)/.exec(Z[2]==="even"&&"2n"||Z[2]==="odd"&&"2n+1"||!/\D/.test(Z[2])&&"0n+"+Z[2]||Z[2]);Z[2]=Y[1]+(Y[2]||1)-0;Z[3]=Y[3]-0}Z[0]=T++;return Z},ATTR:function(ab,aa,Z,Y,ba,bb){aa=ab[1].replace(/\\/g,"");if(!bb&&O.attrMap[aa]){ab[1]=O.attrMap[aa]}if(ab[2]==="~="){ab[4]=" "+ab[4]+" "}return ab},PSEUDO:function(ab,aa,Z,Y,ba){if(ab[1]==="not"){if((S.exec(ab[3])||"").length>1||/^\w/.test(ab[3])){ab[3]=K(ab[3],null,null,aa)}else{ab=K.filter(ab[3],aa,Z,true^ba);Z||Y.push.apply(Y,ab);return false}}else{if(O.match.POS.test(ab[0])||O.match.CHILD.test(ab[0])){return true}}return ab},POS:function(Y){Y.unshift(true);return Y}},filters:{enabled:function(Y){return Y.disabled===false&&Y.type!=="hidden"},disabled:function(Y){return Y.disabled===true},checked:function(Y){return Y.checked===true},selected:function(Y){return Y.selected===true},parent:function(Y){return !!Y.firstChild},empty:function(Y){return !Y.firstChild},has:function(aa,Z,Y){return !!K(Y[3],aa).length},header:function(Y){return/h\d/i.test(Y.nodeName)},text:function(Y){return"text"===Y.type},radio:function(Y){return"radio"===Y.type},checkbox:function(Y){return"checkbox"===Y.type},file:function(Y){return"file"===Y.type},password:function(Y){return"password"===Y.type},submit:function(Y){return"submit"===Y.type},image:function(Y){return"image"===Y.type},reset:function(Y){return"reset"===Y.type},button:function(Y){return"button"===Y.type||Y.nodeName.toLowerCase()==="button"},input:function(Y){return/input|select|textarea|button/i.test(Y.nodeName)}},setFilters:{first:function(Z,Y){return Y===0},last:function(ab,aa,Z,Y){return aa===Y.length-1},even:function(Z,Y){return Y%2===0},odd:function(Z,Y){return Y%2===1},lt:function(aa,Z,Y){return Z<Y[3]-0},gt:function(aa,Z,Y){return Z>Y[3]-0},nth:function(aa,Z,Y){return Y[3]-0===Z},eq:function(aa,Z,Y){return Y[3]-0===Z}},filter:{PSEUDO:function(ab,aa,Z,Y){var ba=aa[1],bb=O.filters[ba];if(bb){return bb(ab,Z,aa,Y)}else{if(ba==="contains"){return(ab.textContent||ab.innerText||X([ab])||"").indexOf(aa[3])>=0}else{if(ba==="not"){aa=aa[3];Z=0;for(Y=aa.length;Z<Y;Z++){if(aa[Z]===ab){return false}}return true}else{K.error("Syntax error, unrecognized expression: "+ba)}}}},CHILD:function(ba,ab){var Z=ab[1],Y=ba;switch(Z){case"only":case"first":for(;Y=Y.previousSibling;){if(Y.nodeType===1){return false}}if(Z==="first"){return true}Y=ba;case"last":for(;Y=Y.nextSibling;){if(Y.nodeType===1){return false}}return true;case"nth":Z=ab[2];var bb=ab[3];if(Z===1&&bb===0){return true}ab=ab[0];var bh=ba.parentNode;if(bh&&(bh.sizcache!==ab||!ba.nodeIndex)){var aa=0;for(Y=bh.firstChild;Y;Y=Y.nextSibling){if(Y.nodeType===1){Y.nodeIndex=++aa}}bh.sizcache=ab}ba=ba.nodeIndex-bb;return Z===0?ba===0:ba%Z===0&&ba/Z>=0}},ID:function(Z,Y){return Z.nodeType===1&&Z.getAttribute("id")===Y},TAG:function(Z,Y){return Y==="*"&&Z.nodeType===1||Z.nodeName.toLowerCase()===Y},CLASS:function(Z,Y){return(" "+(Z.className||Z.getAttribute("class"))+" ").indexOf(Y)>-1},ATTR:function(ab,aa){var Z=aa[1];ab=O.attrHandle[Z]?O.attrHandle[Z](ab):ab[Z]!=null?ab[Z]:ab.getAttribute(Z);Z=ab+"";var Y=aa[2];aa=aa[4];return ab==null?Y==="!=":Y==="="?Z===aa:Y==="*="?Z.indexOf(aa)>=0:Y==="~="?(" "+Z+" ").indexOf(aa)>=0:!aa?Z&&ab!==false:Y==="!="?Z!==aa:Y==="^="?Z.indexOf(aa)===0:Y==="$="?Z.substr(Z.length-aa.length)===aa:Y==="|="?Z===aa||Z.substr(0,aa.length+1)===aa+"-":false},POS:function(ab,aa,Z,Y){var ba=O.setFilters[aa[2]];if(ba){return ba(ab,Z,aa,Y)}}}},z=O.match.POS;for(var c in O.match){O.match[c]=new RegExp(O.match[c].source+/(?![^\[]*\])(?![^\(]*\))/.source);O.leftMatch[c]=new RegExp(/(^(?:.|\r|\n)*?)/.source+O.match[c].source.replace(/\\(\d+)/g,function(Z,Y){return"\\"+(Y-0+1)}))}var J=function(Z,Y){Z=Array.prototype.slice.call(Z,0);if(Y){Y.push.apply(Y,Z);return Y}return Z};try{Array.prototype.slice.call(R.documentElement.childNodes,0)}catch(G){J=function(ab,aa){aa=aa||[];if(Q.call(ab)==="[object Array]"){Array.prototype.push.apply(aa,ab)}else{if(typeof ab.length==="number"){for(var Z=0,Y=ab.length;Z<Y;Z++){aa.push(ab[Z])}}else{for(Z=0;ab[Z];Z++){aa.push(ab[Z])}}}return aa}}var F;if(R.documentElement.compareDocumentPosition){F=function(Z,Y){if(!Z.compareDocumentPosition||!Y.compareDocumentPosition){if(Z==Y){P=true}return Z.compareDocumentPosition?-1:1}Z=Z.compareDocumentPosition(Y)&4?-1:Z===Y?0:1;if(Z===0){P=true}return Z}}else{if("sourceIndex" in R.documentElement){F=function(Z,Y){if(!Z.sourceIndex||!Y.sourceIndex){if(Z==Y){P=true}return Z.sourceIndex?-1:1}Z=Z.sourceIndex-Y.sourceIndex;if(Z===0){P=true}return Z}}else{if(R.createRange){F=function(ab,aa){if(!ab.ownerDocument||!aa.ownerDocument){if(ab==aa){P=true}return ab.ownerDocument?-1:1}var Z=ab.ownerDocument.createRange(),Y=aa.ownerDocument.createRange();Z.setStart(ab,0);Z.setEnd(ab,0);Y.setStart(aa,0);Y.setEnd(aa,0);ab=Z.compareBoundaryPoints(Range.START_TO_END,Y);if(ab===0){P=true}return ab}}}}(function(){var aa=R.createElement("div"),Z="script"+(new Date).getTime();aa.innerHTML="<a name='"+Z+"'/>";var Y=R.documentElement;Y.insertBefore(aa,Y.firstChild);if(R.getElementById(Z)){O.find.ID=function(ab,ba,bb){if(typeof ba.getElementById!=="undefined"&&!bb){return(ba=ba.getElementById(ab[1]))?ba.id===ab[1]||typeof ba.getAttributeNode!=="undefined"&&ba.getAttributeNode("id").nodeValue===ab[1]?[ba]:M:[]}};O.filter.ID=function(ab,ba){var bb=typeof ab.getAttributeNode!=="undefined"&&ab.getAttributeNode("id");return ab.nodeType===1&&bb&&bb.nodeValue===ba}}Y.removeChild(aa);Y=aa=null})();(function(){var Y=R.createElement("div");Y.appendChild(R.createComment(""));if(Y.getElementsByTagName("*").length>0){O.find.TAG=function(ab,aa){aa=aa.getElementsByTagName(ab[1]);if(ab[1]==="*"){ab=[];for(var Z=0;aa[Z];Z++){aa[Z].nodeType===1&&ab.push(aa[Z])}aa=ab}return aa}}Y.innerHTML="<a href='#'></a>";if(Y.firstChild&&typeof Y.firstChild.getAttribute!=="undefined"&&Y.firstChild.getAttribute("href")!=="#"){O.attrHandle.href=function(Z){return Z.getAttribute("href",2)}}Y=null})();R.querySelectorAll&&function(){var aa=K,Z=R.createElement("div");Z.innerHTML="<p class='TEST'></p>";if(!(Z.querySelectorAll&&Z.querySelectorAll(".TEST").length===0)){K=function(ab,bh,bi,ba){bh=bh||R;if(!ba&&bh.nodeType===9&&!r(bh)){try{return J(bh.querySelectorAll(ab),bi)}catch(bb){}}return aa(ab,bh,bi,ba)};for(var Y in aa){K[Y]=aa[Y]}Z=null}}();(function(){var Y=R.createElement("div");Y.innerHTML="<div class='test e'></div><div class='test'></div>";if(!(!Y.getElementsByClassName||Y.getElementsByClassName("e").length===0)){Y.lastChild.className="e";if(Y.getElementsByClassName("e").length!==1){O.order.splice(1,0,"CLASS");O.find.CLASS=function(ab,aa,Z){if(typeof aa.getElementsByClassName!=="undefined"&&!Z){return aa.getElementsByClassName(ab[1])}};Y=null}}})();var v=R.compareDocumentPosition?function(Z,Y){return Z.compareDocumentPosition(Y)&16}:function(Z,Y){return Z!==Y&&(Z.contains?Z.contains(Y):true)},r=function(Y){return(Y=(Y?Y.ownerDocument||Y:0).documentElement)?Y.nodeName!=="HTML":false},L=function(ab,aa){var Z=[],Y="",ba;for(aa=aa.nodeType?[aa]:aa;ba=O.match.PSEUDO.exec(ab);){Y+=ba[0];ab=ab.replace(O.match.PSEUDO,"")}ab=O.relative[ab]?ab+"*":ab;ba=0;for(var bb=aa.length;ba<bb;ba++){K(ab,aa[ba],Z)}return K.filter(Y,Z)};ah.find=K;ah.expr=K.selectors;ah.expr[":"]=ah.expr.filters;ah.unique=K.uniqueSort;ah.getText=X;ah.isXMLDoc=r;ah.contains=v})();var aw=/Until$/,U=/^(?:parents|prevUntil|prevAll)/,s=/,/;av=Array.prototype.slice;var x=function(r,c,z){if(ah.isFunction(c)){return ah.grep(r,function(G,F){return !!c.call(G,F,G)===z})}else{if(c.nodeType){return ah.grep(r,function(F){return F===c===z})}else{if(typeof c==="string"){var v=ah.grep(r,function(F){return F.nodeType===1});if(E.test(c)){return ah.filter(c,v,!z)}else{c=ah.filter(c,v)}}}}return ah.grep(r,function(F){return ah.inArray(F,c)>=0===z})};ah.fn.extend({find:function(r){for(var c=this.pushStack("","find",r),J=0,F=0,G=this.length;F<G;F++){J=c.length;ah.find(r,this[F],c);if(F>0){for(var z=J;z<c.length;z++){for(var v=0;v<J;v++){if(c[v]===c[z]){c.splice(z--,1);break}}}}}return c},has:function(r){var c=ah(r);return this.filter(function(){for(var z=0,v=c.length;z<v;z++){if(ah.contains(this,c[z])){return true}}})},not:function(c){return this.pushStack(x(this,c,false),"not",c)},filter:function(c){return this.pushStack(x(this,c,true),"filter",c)},is:function(c){return !!c&&ah.filter(c,this).length>0},closest:function(L,K){if(ah.isArray(L)){var J=[],F=this[0],G,z={},v;if(F&&L.length){G=0;for(var r=L.length;G<r;G++){v=L[G];z[v]||(z[v]=ah.expr.match.POS.test(v)?ah(v,K||this.context):v)}for(;F&&F.ownerDocument&&F!==K;){for(v in z){G=z[v];if(G.jquery?G.index(F)>-1:ah(F).is(G)){J.push({selector:v,elem:F});delete z[v]}}F=F.parentNode}}return J}var c=ah.expr.match.POS.test(L)?ah(L,K||this.context):null;return this.map(function(N,O){for(;O&&O.ownerDocument&&O!==K;){if(c?c.index(O)>-1:ah(O).is(L)){return O}O=O.parentNode}return null})},index:function(c){if(!c||typeof c==="string"){return ah.inArray(this[0],c?ah(c):this.parent().children())}return ah.inArray(c.jquery?c[0]:c,this)},add:function(r,c){r=typeof r==="string"?ah(r,c||this.context):ah.makeArray(r);c=ah.merge(this.get(),r);return this.pushStack(A(r[0])||A(c[0])?c:ah.unique(c))},andSelf:function(){return this.add(this.prevObject)}});ah.each({parent:function(c){return(c=c.parentNode)&&c.nodeType!==11?c:null},parents:function(c){return ah.dir(c,"parentNode")},parentsUntil:function(r,c,v){return ah.dir(r,"parentNode",v)},next:function(c){return ah.nth(c,2,"nextSibling")},prev:function(c){return ah.nth(c,2,"previousSibling")},nextAll:function(c){return ah.dir(c,"nextSibling")},prevAll:function(c){return ah.dir(c,"previousSibling")},nextUntil:function(r,c,v){return ah.dir(r,"nextSibling",v)},prevUntil:function(r,c,v){return ah.dir(r,"previousSibling",v)},siblings:function(c){return ah.sibling(c.parentNode.firstChild,c)},children:function(c){return ah.sibling(c.firstChild)},contents:function(c){return ah.nodeName(c,"iframe")?c.contentDocument||c.contentWindow.document:ah.makeArray(c.childNodes)}},function(r,c){ah.fn[r]=function(F,v){var z=ah.map(this,c,F);aw.test(r)||(v=F);if(v&&typeof v==="string"){z=ah.filter(v,z)}z=this.length>1?ah.unique(z):z;if((this.length>1||s.test(v))&&U.test(r)){z=z.reverse()}return this.pushStack(z,r,av.call(arguments).join(","))}});ah.extend({filter:function(r,c,v){if(v){r=":not("+r+")"}return ah.find.matches(r,c)},dir:function(r,c,z){var v=[];for(r=r[c];r&&r.nodeType!==9&&(z===M||r.nodeType!==1||!ah(r).is(z));){r.nodeType===1&&v.push(r);r=r[c]}return v},nth:function(r,c,z){c=c||1;for(var v=0;r;r=r[z]){if(r.nodeType===1&&++v===c){break}}return r},sibling:function(r,c){for(var v=[];r;r=r.nextSibling){r.nodeType===1&&r!==c&&v.push(r)}return v}});var j=/ jQuery\d+="(?:\d+|null)"/g,ar=/^\s+/,bd=/(<([\w:]+)[^>]*?)\/>/g,f=/^(?:area|br|col|embed|hr|img|input|link|meta|param)$/i,aZ=/<([\w:]+)/,a7=/<tbody/i,aU=/<|&\w+;/,a2=/checked\s*(?:[^=]|=\s*.checked.)/i,aL=function(r,c,v){return f.test(v)?r:c+"></"+v+">"},aJ={option:[1,"<select multiple='multiple'>","</select>"],legend:[1,"<fieldset>","</fieldset>"],thead:[1,"<table>","</table>"],tr:[2,"<table><tbody>","</tbody></table>"],td:[3,"<table><tbody><tr>","</tr></tbody></table>"],col:[2,"<table><tbody></tbody><colgroup>","</colgroup></table>"],area:[1,"<map>","</map>"],_default:[0,"",""]};aJ.optgroup=aJ.option;aJ.tbody=aJ.tfoot=aJ.colgroup=aJ.caption=aJ.thead;aJ.th=aJ.td;if(!ah.support.htmlSerialize){aJ._default=[1,"div<div>","</div>"]}ah.fn.extend({text:function(c){if(ah.isFunction(c)){return this.each(function(r){var v=ah(this);v.text(c.call(this,r,v.text()))})}if(typeof c!=="object"&&c!==M){return this.empty().append((this[0]&&this[0].ownerDocument||R).createTextNode(c))}return ah.getText(this)},wrapAll:function(r){if(ah.isFunction(r)){return this.each(function(v){ah(this).wrapAll(r.call(this,v))})}if(this[0]){var c=ah(r,this[0].ownerDocument).eq(0).clone(true);this[0].parentNode&&c.insertBefore(this[0]);c.map(function(){for(var v=this;v.firstChild&&v.firstChild.nodeType===1;){v=v.firstChild}return v}).append(this)}return this},wrapInner:function(c){if(ah.isFunction(c)){return this.each(function(r){ah(this).wrapInner(c.call(this,r))})}return this.each(function(){var r=ah(this),v=r.contents();v.length?v.wrapAll(c):r.append(c)})},wrap:function(c){return this.each(function(){ah(this).wrapAll(c)})},unwrap:function(){return this.parent().each(function(){ah.nodeName(this,"body")||ah(this).replaceWith(this.childNodes)}).end()},append:function(){return this.domManip(arguments,true,function(c){this.nodeType===1&&this.appendChild(c)})},prepend:function(){return this.domManip(arguments,true,function(c){this.nodeType===1&&this.insertBefore(c,this.firstChild)})},before:function(){if(this[0]&&this[0].parentNode){return this.domManip(arguments,false,function(r){this.parentNode.insertBefore(r,this)})}else{if(arguments.length){var c=ah(arguments[0]);c.push.apply(c,this.toArray());return this.pushStack(c,"before",arguments)}}},after:function(){if(this[0]&&this[0].parentNode){return this.domManip(arguments,false,function(r){this.parentNode.insertBefore(r,this.nextSibling)})}else{if(arguments.length){var c=this.pushStack(this,"after",arguments);c.push.apply(c,ah(arguments[0]).toArray());return c}}},clone:function(r){var c=this.map(function(){if(!ah.support.noCloneEvent&&!ah.isXMLDoc(this)){var z=this.outerHTML,v=this.ownerDocument;if(!z){z=v.createElement("div");z.appendChild(this.cloneNode(true));z=z.innerHTML}return ah.clean([z.replace(j,"").replace(ar,"")],v)[0]}else{return this.cloneNode(true)}});if(r===true){l(this,c);l(this.find("*"),c.find("*"))}return c},html:function(r){if(r===M){return this[0]&&this[0].nodeType===1?this[0].innerHTML.replace(j,""):null}else{if(typeof r==="string"&&!/<script/i.test(r)&&(ah.support.leadingWhitespace||!ar.test(r))&&!aJ[(aZ.exec(r)||["",""])[1].toLowerCase()]){r=r.replace(bd,aL);try{for(var c=0,z=this.length;c<z;c++){if(this[c].nodeType===1){ah.cleanData(this[c].getElementsByTagName("*"));this[c].innerHTML=r}}}catch(v){this.empty().append(r)}}else{ah.isFunction(r)?this.each(function(J){var G=ah(this),F=G.html();G.empty().append(function(){return r.call(this,J,F)})}):this.empty().append(r)}}return this},replaceWith:function(c){if(this[0]&&this[0].parentNode){if(ah.isFunction(c)){return this.each(function(r){var z=ah(this),v=z.html();z.replaceWith(c.call(this,r,v))})}else{c=ah(c).detach()}return this.each(function(){var r=this.nextSibling,v=this.parentNode;ah(this).remove();r?ah(r).before(c):ah(v).append(c)})}else{return this.pushStack(ah(ah.isFunction(c)?c():c),"replaceWith",c)}},detach:function(c){return this.remove(c,true)},domManip:function(N,L,K){function G(O){return ah.nodeName(O,"table")?O.getElementsByTagName("tbody")[0]||O.appendChild(O.ownerDocument.createElement("tbody")):O}var J,F,z=N[0],r=[];if(!ah.support.checkClone&&arguments.length===3&&typeof z==="string"&&a2.test(z)){return this.each(function(){ah(this).domManip(N,L,K,true)})}if(ah.isFunction(z)){return this.each(function(P){var O=ah(this);N[0]=z.call(this,P,L?O.html():M);O.domManip(N,L,K)})}if(this[0]){J=N[0]&&N[0].parentNode&&N[0].parentNode.nodeType===11?{fragment:N[0].parentNode}:bf(N,this,r);if(F=J.fragment.firstChild){L=L&&ah.nodeName(F,"tr");for(var c=0,v=this.length;c<v;c++){K.call(L?G(this[c],F):this[c],J.cacheable||this.length>1||c>0?J.fragment.cloneNode(true):J.fragment)}}r&&ah.each(r,bg)}return this}});ah.fragments={};ah.each({appendTo:"append",prependTo:"prepend",insertBefore:"before",insertAfter:"after",replaceAll:"replaceWith"},function(r,c){ah.fn[r]=function(J){var F=[];J=ah(J);for(var G=0,z=J.length;G<z;G++){var v=(G>0?this.clone(true):this).get();ah.fn[c].apply(ah(J[G]),v);F=F.concat(v)}return this.pushStack(F,r,J.selector)}});ah.each({remove:function(r,c){if(!r||ah.filter(r,[this]).length){if(!c&&this.nodeType===1){ah.cleanData(this.getElementsByTagName("*"));ah.cleanData([this])}this.parentNode&&this.parentNode.removeChild(this)}},empty:function(){for(this.nodeType===1&&ah.cleanData(this.getElementsByTagName("*"));this.firstChild;){this.removeChild(this.firstChild)}}},function(r,c){ah.fn[r]=function(){return this.each(c,arguments)}});ah.extend({clean:function(r,c,F,v){c=c||R;if(typeof c.createElement==="undefined"){c=c.ownerDocument||c[0]&&c[0].ownerDocument||R}var z=[];ah.each(r,function(K,J){if(typeof J==="number"){J+=""}if(J){if(typeof J==="string"&&!aU.test(J)){J=c.createTextNode(J)}else{if(typeof J==="string"){J=J.replace(bd,aL);var N=(aZ.exec(J)||["",""])[1].toLowerCase(),L=aJ[N]||aJ._default,G=L[0];K=c.createElement("div");for(K.innerHTML=L[1]+J+L[2];G--;){K=K.lastChild}if(!ah.support.tbody){G=a7.test(J);N=N==="table"&&!G?K.firstChild&&K.firstChild.childNodes:L[1]==="<table>"&&!G?K.childNodes:[];for(L=N.length-1;L>=0;--L){ah.nodeName(N[L],"tbody")&&!N[L].childNodes.length&&N[L].parentNode.removeChild(N[L])}}!ah.support.leadingWhitespace&&ar.test(J)&&K.insertBefore(c.createTextNode(ar.exec(J)[0]),K.firstChild);J=ah.makeArray(K.childNodes)}}if(J.nodeType){z.push(J)}else{z=ah.merge(z,J)}}});if(F){for(r=0;z[r];r++){if(v&&ah.nodeName(z[r],"script")&&(!z[r].type||z[r].type.toLowerCase()==="text/javascript")){v.push(z[r].parentNode?z[r].parentNode.removeChild(z[r]):z[r])}else{z[r].nodeType===1&&z.splice.apply(z,[r+1,0].concat(ah.makeArray(z[r].getElementsByTagName("script"))));F.appendChild(z[r])}}}return z},cleanData:function(r){for(var c=0,v;(v=r[c])!=null;c++){ah.event.remove(v);ah.removeData(v)}}});var aD=/z-?index|font-?weight|opacity|zoom|line-?height/i,ai=/alpha\([^)]*\)/,B=/opacity=([^)]*)/,aV=/float/i,aE=/-([a-z])/ig,ad=/([A-Z])/g,u=/^-?\d+(?:px)?$/i,h=/^-?\d/,a9={position:"absolute",visibility:"hidden",display:"block"},aX=["Left","Right"],aI=["Top","Bottom"],af=R.defaultView&&R.defaultView.getComputedStyle,m=ah.support.cssFloat?"cssFloat":"styleFloat",ae=function(r,c){return c.toUpperCase()};ah.fn.css=function(r,c){return ap(this,r,c,true,function(F,v,z){if(z===M){return ah.curCSS(F,v)}if(typeof z==="number"&&!aD.test(v)){z+="px"}ah.style(F,v,z)})};ah.extend({style:function(r,c,F){if(!r||r.nodeType===3||r.nodeType===8){return M}if((c==="width"||c==="height")&&parseFloat(F)<0){F=M}var v=r.style||r,z=F!==M;if(!ah.support.opacity&&c==="opacity"){if(z){v.zoom=1;c=parseInt(F,10)+""==="NaN"?"":"alpha(opacity="+F*100+")";r=v.filter||ah.curCSS(r,"filter")||"";v.filter=ai.test(r)?r.replace(ai,c):c}return v.filter&&v.filter.indexOf("opacity=")>=0?parseFloat(B.exec(v.filter)[1])/100+"":""}if(aV.test(c)){c=m}c=c.replace(aE,ae);if(z){v[c]=F}return v[c]},css:function(r,c,J,F){if(c==="width"||c==="height"){var G,z=c==="width"?aX:aI;function v(){G=c==="width"?r.offsetWidth:r.offsetHeight;F!=="border"&&ah.each(z,function(){F||(G-=parseFloat(ah.curCSS(r,"padding"+this,true))||0);if(F==="margin"){G+=parseFloat(ah.curCSS(r,"margin"+this,true))||0}else{G-=parseFloat(ah.curCSS(r,"border"+this+"Width",true))||0}})}r.offsetWidth!==0?v():ah.swap(r,a9,v);return Math.max(0,Math.round(G))}return ah.curCSS(r,c,J)},curCSS:function(r,c,G){var z,F=r.style;if(!ah.support.opacity&&c==="opacity"&&r.currentStyle){z=B.test(r.currentStyle.filter||"")?parseFloat(RegExp.$1)/100+"":"";return z===""?"1":z}if(aV.test(c)){c=m}if(!G&&F&&F[c]){z=F[c]}else{if(af){if(aV.test(c)){c="float"}c=c.replace(ad,"-$1").toLowerCase();F=r.ownerDocument.defaultView;if(!F){return null}if(r=F.getComputedStyle(r,null)){z=r.getPropertyValue(c)}if(c==="opacity"&&z===""){z="1"}}else{if(r.currentStyle){G=c.replace(aE,ae);z=r.currentStyle[c]||r.currentStyle[G];if(!u.test(z)&&h.test(z)){c=F.left;var v=r.runtimeStyle.left;r.runtimeStyle.left=r.currentStyle.left;F.left=G==="fontSize"?"1em":z||0;z=F.pixelLeft+"px";F.left=c;r.runtimeStyle.left=v}}}}return z},swap:function(r,c,F){var v={};for(var z in c){v[z]=r.style[z];r.style[z]=c[z]}F.call(r);for(z in c){r.style[z]=v[z]}}});if(ah.expr&&ah.expr.filters){ah.expr.filters.hidden=function(r){var c=r.offsetWidth,z=r.offsetHeight,v=r.nodeName.toLowerCase()==="tr";return c===0&&z===0&&!v?true:c>0&&z>0&&!v?false:ah.curCSS(r,"display")==="none"};ah.expr.filters.visible=function(c){return !ah.expr.filters.hidden(c)}}var y=aF(),k=/<script(.|\s)*?\/script>/gi,be=/select|textarea/i,a1=/color|date|datetime|email|hidden|month|number|password|range|search|tel|text|time|url|week/i,aA=/=\?(&|$)/,w=/\?/,aN=/(\?|&)_=.*?(&|$)/,aj=/^(\w+:)?\/\/([^\/?#]+)/,C=/%20/g;ah.fn.extend({_load:ah.fn.load,load:function(r,c,G){if(typeof r!=="string"){return this._load(r)}else{if(!this.length){return this}}var z=r.indexOf(" ");if(z>=0){var F=r.slice(z,r.length);r=r.slice(0,z)}z="GET";if(c){if(ah.isFunction(c)){G=c;c=null}else{if(typeof c==="object"){c=ah.param(c,ah.ajaxSettings.traditional);z="POST"}}}var v=this;ah.ajax({url:r,type:z,dataType:"html",data:c,complete:function(J,K){if(K==="success"||K==="notmodified"){v.html(F?ah("<div />").append(J.responseText.replace(k,"")).find(F):J.responseText)}G&&v.each(G,[J.responseText,K,J])}});return this},serialize:function(){return ah.param(this.serializeArray())},serializeArray:function(){return this.map(function(){return this.elements?ah.makeArray(this.elements):this}).filter(function(){return this.name&&!this.disabled&&(this.checked||be.test(this.nodeName)||a1.test(this.type))}).map(function(r,c){r=ah(this).val();return r==null?null:ah.isArray(r)?ah.map(r,function(v){return{name:c.name,value:v}}):{name:c.name,value:r}}).get()}});ah.each("ajaxStart ajaxStop ajaxComplete ajaxError ajaxSuccess ajaxSend".split(" "),function(r,c){ah.fn[c]=function(v){return this.bind(c,v)}});ah.extend({get:function(r,c,z,v){if(ah.isFunction(c)){v=v||z;z=c;c=null}return ah.ajax({type:"GET",url:r,data:c,success:z,dataType:v})},getScript:function(r,c){return ah.get(r,null,c,"script")},getJSON:function(r,c,v){return ah.get(r,c,v,"json")},post:function(r,c,z,v){if(ah.isFunction(c)){v=v||z;z=c;c={}}return ah.ajax({type:"POST",url:r,data:c,success:z,dataType:v})},ajaxSetup:function(c){ah.extend(ah.ajaxSettings,c)},ajaxSettings:{url:location.href,global:true,type:"GET",contentType:"application/x-www-form-urlencoded",processData:true,async:true,xhr:H.XMLHttpRequest&&(H.location.protocol!=="file:"||!H.ActiveXObject)?function(){return new H.XMLHttpRequest}:function(){try{return new H.ActiveXObject("Microsoft.XMLHTTP")}catch(c){}},accepts:{xml:"application/xml, text/xml",html:"text/html",script:"text/javascript, application/javascript",json:"application/json, text/javascript",text:"text/plain",_default:"*/*"}},lastModified:{},etag:{},ajax:function(ab){function aa(){Y.success&&Y.success.call(K,N,S,r);Y.global&&X("ajaxSuccess",[r,Y])}function Z(){Y.complete&&Y.complete.call(K,r,S);Y.global&&X("ajaxComplete",[r,Y]);Y.global&&!--ah.active&&ah.event.trigger("ajaxStop")}function X(ba,bb){(Y.context?ah(Y.context):ah.event).trigger(ba,bb)}var Y=ah.extend(true,{},ah.ajaxSettings,ab),T,S,N,K=ab&&ab.context||Y,O=Y.type.toUpperCase();if(Y.data&&Y.processData&&typeof Y.data!=="string"){Y.data=ah.param(Y.data,Y.traditional)}if(Y.dataType==="jsonp"){if(O==="GET"){aA.test(Y.url)||(Y.url+=(w.test(Y.url)?"&":"?")+(Y.jsonp||"callback")+"=?")}else{if(!Y.data||!aA.test(Y.data)){Y.data=(Y.data?Y.data+"&":"")+(Y.jsonp||"callback")+"=?"}}Y.dataType="json"}if(Y.dataType==="json"&&(Y.data&&aA.test(Y.data)||aA.test(Y.url))){T=Y.jsonpCallback||"jsonp"+y++;if(Y.data){Y.data=(Y.data+"").replace(aA,"="+T+"$1")}Y.url=Y.url.replace(aA,"="+T+"$1");Y.dataType="script";H[T]=H[T]||function(ba){N=ba;aa();Z();H[T]=M;try{delete H[T]}catch(bb){}J&&J.removeChild(G)}}if(Y.dataType==="script"&&Y.cache===null){Y.cache=false}if(Y.cache===false&&O==="GET"){var z=aF(),c=Y.url.replace(aN,"$1_="+z+"$2");Y.url=c+(c===Y.url?(w.test(Y.url)?"&":"?")+"_="+z:"")}if(Y.data&&O==="GET"){Y.url+=(w.test(Y.url)?"&":"?")+Y.data}Y.global&&!ah.active++&&ah.event.trigger("ajaxStart");z=(z=aj.exec(Y.url))&&(z[1]&&z[1]!==location.protocol||z[2]!==location.host);if(Y.dataType==="script"&&O==="GET"&&z){var J=R.getElementsByTagName("head")[0]||R.documentElement,G=R.createElement("script");G.src=Y.url;if(Y.scriptCharset){G.charset=Y.scriptCharset}if(!T){var F=false;G.onload=G.onreadystatechange=function(){if(!F&&(!this.readyState||this.readyState==="loaded"||this.readyState==="complete")){F=true;aa();Z();G.onload=G.onreadystatechange=null;J&&G.parentNode&&J.removeChild(G)}}}J.insertBefore(G,J.firstChild);return M}var v=false,r=Y.xhr();if(r){Y.username?r.open(O,Y.url,Y.async,Y.username,Y.password):r.open(O,Y.url,Y.async);try{if(Y.data||ab&&ab.contentType){r.setRequestHeader("Content-Type",Y.contentType)}if(Y.ifModified){ah.lastModified[Y.url]&&r.setRequestHeader("If-Modified-Since",ah.lastModified[Y.url]);ah.etag[Y.url]&&r.setRequestHeader("If-None-Match",ah.etag[Y.url])}z||r.setRequestHeader("X-Requested-With","XMLHttpRequest");r.setRequestHeader("Accept",Y.dataType&&Y.accepts[Y.dataType]?Y.accepts[Y.dataType]+", */*":Y.accepts._default)}catch(L){}if(Y.beforeSend&&Y.beforeSend.call(K,r,Y)===false){Y.global&&!--ah.active&&ah.event.trigger("ajaxStop");r.abort();return false}Y.global&&X("ajaxSend",[r,Y]);var W=r.onreadystatechange=function(bb){if(!r||r.readyState===0||bb==="abort"){v||Z();v=true;if(r){r.onreadystatechange=ah.noop}}else{if(!v&&r&&(r.readyState===4||bb==="timeout")){v=true;r.onreadystatechange=ah.noop;S=bb==="timeout"?"timeout":!ah.httpSuccess(r)?"error":Y.ifModified&&ah.httpNotModified(r,Y.url)?"notmodified":"success";var bh;if(S==="success"){try{N=ah.httpData(r,Y.dataType,Y)}catch(ba){S="parsererror";bh=ba}}if(S==="success"||S==="notmodified"){T||aa()}else{ah.handleError(Y,r,S,bh)}Z();bb==="timeout"&&r.abort();if(Y.async){r=null}}}};try{var V=r.abort;r.abort=function(){r&&V.call(r);W("abort")}}catch(Q){}Y.async&&Y.timeout>0&&setTimeout(function(){r&&!v&&W("timeout")},Y.timeout);try{r.send(O==="POST"||O==="PUT"||O==="DELETE"?Y.data:null)}catch(P){ah.handleError(Y,r,null,P);Z()}Y.async||W();return r}},handleError:function(r,c,z,v){if(r.error){r.error.call(r.context||r,c,z,v)}if(r.global){(r.context?ah(r.context):ah.event).trigger("ajaxError",[c,r,v])}},active:0,httpSuccess:function(r){try{return !r.status&&location.protocol==="file:"||r.status>=200&&r.status<300||r.status===304||r.status===1223||r.status===0}catch(c){}return false},httpNotModified:function(r,c){var z=r.getResponseHeader("Last-Modified"),v=r.getResponseHeader("Etag");if(z){ah.lastModified[c]=z}if(v){ah.etag[c]=v}return r.status===304||r.status===0},httpData:function(r,c,F){var v=r.getResponseHeader("content-type")||"",z=c==="xml"||!c&&v.indexOf("xml")>=0;r=z?r.responseXML:r.responseText;z&&r.documentElement.nodeName==="parsererror"&&ah.error("parsererror");if(F&&F.dataFilter){r=F.dataFilter(r,c)}if(typeof r==="string"){if(c==="json"||!c&&v.indexOf("json")>=0){r=ah.parseJSON(r)}else{if(c==="script"||!c&&v.indexOf("javascript")>=0){ah.globalEval(r)}}}return r},param:function(r,c){function G(J,K){if(ah.isArray(K)){ah.each(K,function(N,L){c?z(J,L):G(J+"["+(typeof L==="object"||ah.isArray(L)?N:"")+"]",L)})}else{!c&&K!=null&&typeof K==="object"?ah.each(K,function(N,L){G(J+"["+N+"]",L)}):z(J,K)}}function z(J,K){K=ah.isFunction(K)?K():K;F[F.length]=encodeURIComponent(J)+"="+encodeURIComponent(K)}var F=[];if(c===M){c=ah.ajaxSettings.traditional}if(ah.isArray(r)||r.jquery){ah.each(r,function(){z(this.name,this.value)})}else{for(var v in r){G(v,r[v])}}return F.join("&").replace(C,"+")}});var i={},n=/toggle|show|hide/,a=/^([+-]=)?([\d+-.]+)(.*)$/,aq,aO=[["height","marginTop","marginBottom","paddingTop","paddingBottom"],["width","marginLeft","marginRight","paddingLeft","paddingRight"],["opacity"]];ah.fn.extend({show:function(r,c){if(r||r===0){return this.animate(aC("show",3),r,c)}else{r=0;for(c=this.length;r<c;r++){var F=ah.data(this[r],"olddisplay");this[r].style.display=F||"";if(ah.css(this[r],"display")==="none"){F=this[r].nodeName;var v;if(i[F]){v=i[F]}else{var z=ah("<"+F+" />").appendTo("body");v=z.css("display");if(v==="none"){v="block"}z.remove();i[F]=v}ah.data(this[r],"olddisplay",v)}}r=0;for(c=this.length;r<c;r++){this[r].style.display=ah.data(this[r],"olddisplay")||""}return this}},hide:function(r,c){if(r||r===0){return this.animate(aC("hide",3),r,c)}else{r=0;for(c=this.length;r<c;r++){var v=ah.data(this[r],"olddisplay");!v&&v!=="none"&&ah.data(this[r],"olddisplay",ah.css(this[r],"display"))}r=0;for(c=this.length;r<c;r++){this[r].style.display="none"}return this}},_toggle:ah.fn.toggle,toggle:function(r,c){var v=typeof r==="boolean";if(ah.isFunction(r)&&ah.isFunction(c)){this._toggle.apply(this,arguments)}else{r==null||v?this.each(function(){var z=v?r:ah(this).is(":hidden");ah(this)[z?"show":"hide"]()}):this.animate(aC("toggle",3),r,c)}return this},fadeTo:function(r,c,v){return this.filter(":hidden").css("opacity",0).show().end().animate({opacity:c},r,v)},animate:function(r,c,F,v){var z=ah.speed(c,F,v);if(ah.isEmptyObject(r)){return this.each(z.complete)}return this[z.queue===false?"each":"queue"](function(){var K=ah.extend({},z),J,N=this.nodeType===1&&ah(this).is(":hidden"),L=this;for(J in r){var G=J.replace(aE,ae);if(J!==G){r[G]=r[J];delete r[J];J=G}if(r[J]==="hide"&&N||r[J]==="show"&&!N){return K.complete.call(this)}if((J==="height"||J==="width")&&this.style){K.display=ah.css(this,"display");K.overflow=this.style.overflow}if(ah.isArray(r[J])){(K.specialEasing=K.specialEasing||{})[J]=r[J][1];r[J]=r[J][0]}}if(K.overflow!=null){this.style.overflow="hidden"}K.curAnim=ah.extend({},r);ah.each(r,function(Q,P){var O=new ah.fx(L,K,Q);if(n.test(P)){O[P==="toggle"?N?"show":"hide":P](r)}else{var V=a.exec(P),T=O.cur(true)||0;if(V){P=parseFloat(V[2]);var S=V[3]||"px";if(S!=="px"){L.style[Q]=(P||1)+S;T=(P||1)/O.cur(true)*T;L.style[Q]=T+S}if(V[1]){P=(V[1]==="-="?-1:1)*P+T}O.custom(T,P,S)}else{O.custom(T,P,"")}}});return true})},stop:function(r,c){var v=ah.timers;r&&this.queue([]);this.each(function(){for(var z=v.length-1;z>=0;z--){if(v[z].elem===this){c&&v[z](true);v.splice(z,1)}}});c||this.dequeue();return this}});ah.each({slideDown:aC("show",1),slideUp:aC("hide",1),slideToggle:aC("toggle",1),fadeIn:{opacity:"show"},fadeOut:{opacity:"hide"}},function(r,c){ah.fn[r]=function(z,v){return this.animate(c,z,v)}});ah.extend({speed:function(r,c,z){var v=r&&typeof r==="object"?r:{complete:z||!z&&c||ah.isFunction(r)&&r,duration:r,easing:z&&c||c&&!ah.isFunction(c)&&c};v.duration=ah.fx.off?0:typeof v.duration==="number"?v.duration:ah.fx.speeds[v.duration]||ah.fx.speeds._default;v.old=v.complete;v.complete=function(){v.queue!==false&&ah(this).dequeue();ah.isFunction(v.old)&&v.old.call(this)};return v},easing:{linear:function(r,c,z,v){return z+v*r},swing:function(r,c,z,v){return(-Math.cos(r*Math.PI)/2+0.5)*v+z}},timers:[],fx:function(r,c,v){this.options=c;this.elem=r;this.prop=v;if(!c.orig){c.orig={}}}});ah.fx.prototype={update:function(){this.options.step&&this.options.step.call(this.elem,this.now,this);(ah.fx.step[this.prop]||ah.fx.step._default)(this);if((this.prop==="height"||this.prop==="width")&&this.elem.style){this.elem.style.display="block"}},cur:function(c){if(this.elem[this.prop]!=null&&(!this.elem.style||this.elem.style[this.prop]==null)){return this.elem[this.prop]}return(c=parseFloat(ah.css(this.elem,this.prop,c)))&&c>-10000?c:parseFloat(ah.curCSS(this.elem,this.prop))||0},custom:function(r,c,F){function v(G){return z.step(G)}this.startTime=aF();this.start=r;this.end=c;this.unit=F||this.unit||"px";this.now=this.start;this.pos=this.state=0;var z=this;v.elem=this.elem;if(v()&&ah.timers.push(v)&&!aq){aq=setInterval(ah.fx.tick,13)}},show:function(){this.options.orig[this.prop]=ah.style(this.elem,this.prop);this.options.show=true;this.custom(this.prop==="width"||this.prop==="height"?1:0,this.cur());ah(this.elem).show()},hide:function(){this.options.orig[this.prop]=ah.style(this.elem,this.prop);this.options.hide=true;this.custom(this.cur(),0)},step:function(r){var c=aF(),F=true;if(r||c>=this.options.duration+this.startTime){this.now=this.end;this.pos=this.state=1;this.update();this.options.curAnim[this.prop]=true;for(var v in this.options.curAnim){if(this.options.curAnim[v]!==true){F=false}}if(F){if(this.options.display!=null){this.elem.style.overflow=this.options.overflow;r=ah.data(this.elem,"olddisplay");this.elem.style.display=r?r:this.options.display;if(ah.css(this.elem,"display")==="none"){this.elem.style.display="block"}}this.options.hide&&ah(this.elem).hide();if(this.options.hide||this.options.show){for(var z in this.options.curAnim){ah.style(this.elem,z,this.options.orig[z])}}this.options.complete.call(this.elem)}return false}else{z=c-this.startTime;this.state=z/this.options.duration;r=this.options.easing||(ah.easing.swing?"swing":"linear");this.pos=ah.easing[this.options.specialEasing&&this.options.specialEasing[this.prop]||r](this.state,z,0,1,this.options.duration);this.now=this.start+(this.end-this.start)*this.pos;this.update()}return true}};ah.extend(ah.fx,{tick:function(){for(var r=ah.timers,c=0;c<r.length;c++){r[c]()||r.splice(c--,1)}r.length||ah.fx.stop()},stop:function(){clearInterval(aq);aq=null},speeds:{slow:600,fast:200,_default:400},step:{opacity:function(c){ah.style(c.elem,"opacity",c.now)},_default:function(c){if(c.elem.style&&c.elem.style[c.prop]!=null){c.elem.style[c.prop]=(c.prop==="width"||c.prop==="height"?Math.max(0,c.now):c.now)+c.unit}else{c.elem[c.prop]=c.now}}}});if(ah.expr&&ah.expr.filters){ah.expr.filters.animated=function(c){return ah.grep(ah.timers,function(r){return c===r.elem}).length}}ah.fn.offset="getBoundingClientRect" in R.documentElement?function(r){var c=this[0];if(r){return this.each(function(F){ah.offset.setOffset(this,r,F)})}if(!c||!c.ownerDocument){return null}if(c===c.ownerDocument.body){return ah.offset.bodyOffset(c)}var z=c.getBoundingClientRect(),v=c.ownerDocument;c=v.body;v=v.documentElement;return{top:z.top+(self.pageYOffset||ah.support.boxModel&&v.scrollTop||c.scrollTop)-(v.clientTop||c.clientTop||0),left:z.left+(self.pageXOffset||ah.support.boxModel&&v.scrollLeft||c.scrollLeft)-(v.clientLeft||c.clientLeft||0)}}:function(N){var L=this[0];if(N){return this.each(function(O){ah.offset.setOffset(this,N,O)})}if(!L||!L.ownerDocument){return null}if(L===L.ownerDocument.body){return ah.offset.bodyOffset(L)}ah.offset.initialize();var K=L.offsetParent,G=L,J=L.ownerDocument,F,z=J.documentElement,r=J.body;G=(J=J.defaultView)?J.getComputedStyle(L,null):L.currentStyle;for(var c=L.offsetTop,v=L.offsetLeft;(L=L.parentNode)&&L!==r&&L!==z;){if(ah.offset.supportsFixedPosition&&G.position==="fixed"){break}F=J?J.getComputedStyle(L,null):L.currentStyle;c-=L.scrollTop;v-=L.scrollLeft;if(L===K){c+=L.offsetTop;v+=L.offsetLeft;if(ah.offset.doesNotAddBorder&&!(ah.offset.doesAddBorderForTableAndCells&&/^t(able|d|h)$/i.test(L.nodeName))){c+=parseFloat(F.borderTopWidth)||0;v+=parseFloat(F.borderLeftWidth)||0}G=K;K=L.offsetParent}if(ah.offset.subtractsBorderForOverflowNotVisible&&F.overflow!=="visible"){c+=parseFloat(F.borderTopWidth)||0;v+=parseFloat(F.borderLeftWidth)||0}G=F}if(G.position==="relative"||G.position==="static"){c+=r.offsetTop;v+=r.offsetLeft}if(ah.offset.supportsFixedPosition&&G.position==="fixed"){c+=Math.max(z.scrollTop,r.scrollTop);v+=Math.max(z.scrollLeft,r.scrollLeft)}return{top:c,left:v}};ah.offset={initialize:function(){var r=R.body,c=R.createElement("div"),G,z,F,v=parseFloat(ah.curCSS(r,"marginTop",true))||0;ah.extend(c.style,{position:"absolute",top:0,left:0,margin:0,border:0,width:"1px",height:"1px",visibility:"hidden"});c.innerHTML="<div style='position:absolute;top:0;left:0;margin:0;border:5px solid #000;padding:0;width:1px;height:1px;'><div></div></div><table style='position:absolute;top:0;left:0;margin:0;border:5px solid #000;padding:0;width:1px;height:1px;' cellpadding='0' cellspacing='0'><tr><td></td></tr></table>";r.insertBefore(c,r.firstChild);G=c.firstChild;z=G.firstChild;F=G.nextSibling.firstChild.firstChild;this.doesNotAddBorder=z.offsetTop!==5;this.doesAddBorderForTableAndCells=F.offsetTop===5;z.style.position="fixed";z.style.top="20px";this.supportsFixedPosition=z.offsetTop===20||z.offsetTop===15;z.style.position=z.style.top="";G.style.overflow="hidden";G.style.position="relative";this.subtractsBorderForOverflowNotVisible=z.offsetTop===-5;this.doesNotIncludeMarginInBodyOffset=r.offsetTop!==v;r.removeChild(c);ah.offset.initialize=ah.noop},bodyOffset:function(r){var c=r.offsetTop,v=r.offsetLeft;ah.offset.initialize();if(ah.offset.doesNotIncludeMarginInBodyOffset){c+=parseFloat(ah.curCSS(r,"marginTop",true))||0;v+=parseFloat(ah.curCSS(r,"marginLeft",true))||0}return{top:c,left:v}},setOffset:function(r,c,J){if(/static/.test(ah.curCSS(r,"position"))){r.style.position="relative"}var F=ah(r),G=F.offset(),z=parseInt(ah.curCSS(r,"top",true),10)||0,v=parseInt(ah.curCSS(r,"left",true),10)||0;if(ah.isFunction(c)){c=c.call(r,J,G)}J={top:c.top-G.top+z,left:c.left-G.left+v};"using" in c?c.using.call(r,J):F.css(J)}};ah.fn.extend({position:function(){if(!this[0]){return null}var r=this[0],c=this.offsetParent(),z=this.offset(),v=/^body|html$/i.test(c[0].nodeName)?{top:0,left:0}:c.offset();z.top-=parseFloat(ah.curCSS(r,"marginTop",true))||0;z.left-=parseFloat(ah.curCSS(r,"marginLeft",true))||0;v.top+=parseFloat(ah.curCSS(c[0],"borderTopWidth",true))||0;v.left+=parseFloat(ah.curCSS(c[0],"borderLeftWidth",true))||0;return{top:z.top-v.top,left:z.left-v.left}},offsetParent:function(){return this.map(function(){for(var c=this.offsetParent||R.body;c&&!/^body|html$/i.test(c.nodeName)&&ah.css(c,"position")==="static";){c=c.offsetParent}return c})}});ah.each(["Left","Top"],function(r,c){var v="scroll"+c;ah.fn[v]=function(F){var G=this[0],z;if(!G){return null}if(F!==M){return this.each(function(){if(z=ak(this)){z.scrollTo(!r?F:ah(z).scrollLeft(),r?F:ah(z).scrollTop())}else{this[v]=F}})}else{return(z=ak(G))?"pageXOffset" in z?z[r?"pageYOffset":"pageXOffset"]:ah.support.boxModel&&z.document.documentElement[v]||z.document.body[v]:G[v]}}});ah.each(["Height","Width"],function(r,c){var v=c.toLowerCase();ah.fn["inner"+c]=function(){return this[0]?ah.css(this[0],v,false,"padding"):null};ah.fn["outer"+c]=function(z){return this[0]?ah.css(this[0],v,false,z?"margin":"border"):null};ah.fn[v]=function(z){var F=this[0];if(!F){return z==null?null:this}if(ah.isFunction(z)){return this.each(function(J){var G=ah(this);G[v](z.call(this,J,G[v]()))})}return"scrollTo" in F&&F.document?F.document.compatMode==="CSS1Compat"&&F.document.documentElement["client"+c]||F.document.body["client"+c]:F.nodeType===9?Math.max(F.documentElement["client"+c],F.body["scroll"+c],F.documentElement["scroll"+c],F.body["offset"+c],F.documentElement["offset"+c]):z===M?ah.css(F,v):this.css(v,typeof z==="string"?z:z+"px")}});H.jQuery=H.$=ah})(window);
(function($){function toIntegersAtLease(n){return n<10?"0"+n:n}Date.prototype.toJSON=function(date){return this.getUTCFullYear()+"-"+toIntegersAtLease(this.getUTCMonth())+"-"+toIntegersAtLease(this.getUTCDate())};var escapeable=/["\\\x00-\x1f\x7f-\x9f]/g;var meta={"\b":"\\b","\t":"\\t","\n":"\\n","\f":"\\f","\r":"\\r",'"':'\\"',"\\":"\\\\"};$.quoteString=function(string){if(escapeable.test(string)){return'"'+string.replace(escapeable,function(a){var c=meta[a];if(typeof c==="string"){return c}c=a.charCodeAt();return"\\u00"+Math.floor(c/16).toString(16)+(c%16).toString(16)})+'"'}return'"'+string+'"'};$.toJSON=function(o,compact){var type=typeof(o);if(type=="undefined"){return"undefined"}else{if(type=="number"||type=="boolean"){return o+""}else{if(o===null){return"null"}}}if(type=="string"){return $.quoteString(o)}if(type=="object"&&typeof o.toJSON=="function"){return o.toJSON(compact)}if(type!="function"&&typeof(o.length)=="number"){var ret=[];for(var i=0;i<o.length;i++){ret.push($.toJSON(o[i],compact))}if(compact){return"["+ret.join(",")+"]"}else{return"["+ret.join(", ")+"]"}}if(type=="function"){throw new TypeError("Unable to convert object of type 'function' to json.")}var ret=[];for(var k in o){var name;type=typeof(k);if(type=="number"){name='"'+k+'"'}else{if(type=="string"){name=$.quoteString(k)}else{continue}}var val=$.toJSON(o[k],compact);if(typeof(val)!="string"){continue}if(compact){ret.push(name+":"+val)}else{ret.push(name+": "+val)}}return"{"+ret.join(", ")+"}"};$.compactJSON=function(o){return $.toJSON(o,true)};$.evalJSON=function(src){return eval("("+src+")")};$.secureEvalJSON=function(src){var filtered=src;filtered=filtered.replace(/\\["\\\/bfnrtu]/g,"@");filtered=filtered.replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g,"]");filtered=filtered.replace(/(?:^|:|,)(?:\s*\[)+/g,"");if(/^[\],:{}\s]*$/.test(filtered)){return eval("("+src+")")}else{throw new SyntaxError("Error parsing JSON, source is not valid.")}}})(jQuery);

(function(){/**
* Class.js
* 
* @author Demis Bellot
* @version 1.0
* 
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Class() { }
Class.typeMap = {};
Class.$type = "AjaxStack.Class";
Class.getType = function() { return this.$type; }
Class.getTypeClassName = function()
{
	var parts = this.$type.split('.');
	return parts[parts.length - 1];
}
Class.prototype.getType = function()
{
	return this.constructor.$type;
}
Class.prototype.getTypeClassName = function()
{
	return this.constructor.getTypeClassName();
}
Class.mapAdd = function(array, key)
{
	array.push(key);
	array[key] = true;
	return array;
}
Class.registerType = function(type, ctor)
{
	if (!type) return;
	Class.typeMap[type] = ctor;
	var nsTypes = type.split('.'), ns = window, nsType;
	for (var i = 0; i < nsTypes.length; i++)
	{
		var nsType = nsTypes[i];
		if (!ns[nsType]) ns[nsType] = i < nsTypes.length - 1 ? {} : ctor;
		ns = ns[nsType];
	}
}
Class.prototype.getBaseTypes = function(types)
{
	var base = this, types = types || [];
	do {
		base = base.constructor.$base;
		if (base) Class.mapAdd(types, base.getType());
	} while (base);
	return types;
}
Class.prototype.getBaseTypesAndSelf = function()
{
	return this.getBaseTypes(Class.mapAdd([], this.getType()));
}
Class.prototype.isTypeOf = function(typeName)
{
	return this.getBaseTypesAndSelf()[typeName] ? true : false;
}
Class.getConstructor = function(typeName)
{
	return Class.typeMap[typeName];
}
Class.create = function(typeName, ctorArgs)
{
	var ctor = Class.typeMap[typeName];
	function F() {
		ctor.apply(this, ctorArgs);
	}
	F.prototype = ctor.prototype;
	return new F();
}
Class.inherit = function(subClass, baseClass)
{
	function F() { }
	F.prototype = baseClass.prototype;
	subClass.$base = baseClass.prototype;
	subClass.$baseConstructor = baseClass;
	subClass.prototype = new F();
	subClass.prototype.constructor = subClass;
}
Function.prototype.extend = function(a, options, members)
{
	Class.inherit(this, a);
	options = options || {};
	Class.registerType(options.type, this);
	this.$type = options.type || Class.$type;
	this.getType = Class.getType;

	if (members)
	{
		if (typeof (members) === 'function')
			members(this.prototype);
		else
			for (var a in members) this.prototype[a] = members[a];
	}

	return this.prototype;
};

var is = {
	Null: function(a)
	{
		return a === null;
	},
	Undefined: function(a)
	{
		return a === undefined;
	},
	Empty: function(a)
	{
		return (a === null || a === undefined || a === "");
	},
	Function: function(a)
	{
		return (typeof (a) === 'function') ? a.constructor.toString().match(/Function/) !== null : false;
	},
	String: function(a)
	{
		if (a === null || a === undefined || a.type) return false;
		return (typeof (a) === 'string') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/string/i) !== null : false;
	},
	Array: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'object') ? a.constructor.toString().match(/array/i) !== null || a.length !== undefined : false;
	},
	Boolean: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'boolean') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/boolean/i) !== null : false;
	},
	Date: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'date') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/date/i) !== null : false;
	},
	Number: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return (typeof (a) === 'number') ? true : (typeof (a) === 'object') ? a.constructor.toString().match(/Number/) !== null : false;
	},
	Numeric: function(a)
	{
		return (a - 0) == a && a.length > 0;
	},
	Digit: function(a)
	{
		if (is.Empty(a)) return false;
		var s = a.toString();
		return (s.length == 1 && (s >= "0") && (s <= "9"));
	},
	Integer: function(a)
	{
		if (is.Empty(a)) return false;
		var s = a.toString();
		if (s[0] == '-') s = s.substr(1);
		for (i = 0, len = s.length; i < len; i++)
			if (!(s.charAt(i) >= "0" && s.charAt(i) <= "9")) return false;
		return true;
	},
	UnsignedInteger: function(a)
	{
		return is.isInteger(a) && parseInt(a) >= 0;
	},
	IntegerInRange: function(a, min, max)
	{
		if (!is.Integer(a)) return false;
		var val = parseInt(a);
		return val >= min && val <= max;
	},
	Object: function(a)
	{
		return (typeof (a) === 'object') ? a.constructor.toString().match(/object/i) !== null : false;
	},
	RegExp: function(a)
	{
		return (typeof (a) === 'function') ? a.constructor.toString().match(/regexp/i) !== null : false;
	},
	ValueType: function(a)
	{
		if (is.Empty(a) || a.type) return false;
		return is.String(a) || is.Date(a) || is.Number(a) || is.Boolean(a);
	},
	Class: function(a)
	{
		if (is.Empty(a)) return false;
		return a.constructor && a.constructor.$type ? true : false;
	},
	TypeOf: function(a, type)
	{
		return is.Class(a) && a.isTypeOf(type) ? true : false;
	}
};
var 
// Will speed up references to window, and allows munging its name.
window = this,
// Will speed up references to undefined, and allows munging its name.
undefined,
// Map over ajaxstack in case of overwrite
_AjaxStack = window.AjaxStack,

AjaxStack = window.A$ = window.AjaxStack = {};

//@requires Class to be defined first.
var staticMap =
{
	Class: Class, is: is, ArrayExt: ArrayExt, Environment: Environment, FormExt: FormExt,
	Html: Html, jQueryExt: jQueryExt, Key: Key, ObjectExt: ObjectExt, StringExt: StringExt,
	Reflection: Reflection, Quirks: Quirks, Script: Script, ResultEvent: ResultEvent, 
	Urn: Urn, Dto: Dto
};
for (var type in staticMap) Class.registerType("AjaxStack." + type, staticMap[type]);

AjaxStack.sys = sys = {
	'import': function(jsNamespace, intoScope)
	{
		var jsTypes = is.String(jsNamespace) ? window[jsNamespace] : jsNamespace;
		intoScope = intoScope || window;
		for (var type in jsTypes)
		{
			intoScope[type] = jsTypes[type];
		}
	},
	create: function(nsType, ctorArgs)
	{
		var ctor = Class.getConstructor(nsType);
		if (!ctor) return null;
		return new ctor(ctorArgs);
	},
	createFactoryFn: function(ctor)
	{
		return function() { return new ctor(); }
	}
};


function ASObject()
{
	if (!this.constructor.log)
	{
		this.constructor.log = new Logger(this.getType());
	}
	this.log = this.constructor.log;
}
ASObject.extend(Class, { type: 'AjaxStack.ASObject' },
	function(p)
	{
		var ctor = p.constructor;
		p.getFactoryFn = function() { return sys.createFactoryFn(this.constructor); }
	}
);


/// <reference path="../../release/jsApp.js"/>

function Quirks() { }
//If the table has a border ff includes it in the width
Quirks.getTableWidth = function(tableIdSelector)
{
	var tableWidth = $(tableIdSelector).width();
	if (E.browser.chrome || E.browser.msie)
	{
		return tableWidth + 2;
	}
	return tableWidth;
};

Quirks.forceBlur = function()
{
	//when you select a different link chrome doesn't fire the blur event
	if (E.browser.webkit)
	{
		$("INPUT").blur();
	}
};

//WARNING: does not work in old versions of safari and chrome, but is a standard
Quirks.inputHasFocus = function()
{
	return document.activeElement && document.activeElement.tagName.toLowerCase() == "input";
};

Quirks.callLater = function(fn, delay)
{
	setTimeout(function()
	{
		fn();
	}, delay || 100);
};

Quirks.disableScrolling = function(e)
{
	if (E.browser.chrome)
	{
		e.stopPropagation();
		e.stopImmediatePropagation();
	}
}
/**
* Cache.js
* 
* @author Demis Bellot
* @version 1.0
* @requires Class.js, StringExt.js, ArrayExt.js 
* 
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Cache(options, type)
{
	Cache.$baseConstructor.call(this);
	
	this.options =
	{
		size: 0,
		include: [],
		exclude: []
	};
	O.merge(this.options, options);

	this.log.debug("CACHE: init(): " + this.toString());

	this.count = 0;
	this.keys = [];
	this.data = {};
}
Cache.extend(ASObject, { type: "AjaxStack.Cache" }, {
	toString: function()
	{
		return S.toString(this.options.size, this.options.include, this.options.exclude);
	},

	getNextIndex: function()
	{
		return this.count++ % this.options.size;
	},

	isValidKey: function(key)
	{
		if (this.options.size == 0) return false;

		//ensure key is valid
		var include = this.options.include;
		if (!A.isEmpty(include)
			&& !A.any(include, function(i, s)
			{
				return S.startsWith(key, s);
			})
		) return false;

		//ensure key is not invalid
		var exclude = this.options.exclude;
		if (!A.isEmpty(exclude)
			&& !A.all(exclude, function(i, s)
			{
				return !S.startsWith(key, s);
			})
		) return false;

		return true;
	},

	add: function(key, value)
	{
		if (!this.isValidKey(key)) return;

		//this.log.debug("CACHE: adding valid key: " + key + ", ex: " + this.toString());

		if (this.data[key])
		{
			this.data[key] = value;
			return;
		}
		var i = this.getNextIndex();

		this.remove(i);

		this.keys[i] = key;

		this.data[key] = value;
	},

	remove: function(i)
	{
		var key = this.keys[i];
		if (!key) return;

		this.keys[i] = null;
		delete this.data[key];
	},

	getValue: function(key)
	{
		if (!this.isValidKey(key)) return;

		var value = this.data[key];

		this.log.debug("CACHE: getValue(): " + key + ": " + (value ? "HIT" : "MISS"));
		return value;
	}
}); 
/**
* Dto.js
* 
* @author Demis Bellot
* @version 1.0
* @requires Class.js
* 
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Dto() { }

Dto.CHAR_A = "A".charCodeAt(0);
Dto.CHAR_Z = "Z".charCodeAt(0);
Dto.MULTI_FIELD_SEPERATOR = ";";

Dto.parseGuid = function(guid)
{
	return guid.toLowerCase().replace(/-/g, "");
}
Dto.parseBool = function(aBool)
{
	if (is.Boolean(aBool)) return aBool;
	if (is.String(aBool)) return aBool.toString().toLowerCase() == "true";
	return aBool;
}
Dto.parseArrayOfId = function(array)
{
	return array || [];
}
Dto.parseArrayOfKvp = function(kvps)
{
	if (!kvps) return {};
	
	var result = {};
	for (var i = 0, len = kvps.length; i < len; i++)
	{
		var kvp = kvps[i];
		result[kvp['K']] = kvp['V'];
	}
	return result;
}
Dto.toGetArray = function(array)
{
	return array ? array.join(",") : [];
}
Dto.toPostArray = function(array)
{
	return array || [];
}
Dto.toUtcDate = function(date)
{
	return date.getUTCFullYear()
		+ '-' + S.lpad(date.getUTCMonth() + 1, 2)
		+ '-' + S.lpad(date.getUTCDate(), 2)
		+ 'T' + S.lpad(date.getUTCHours(), 2)
		+ ':' + S.lpad(date.getUTCMinutes(), 2)
		+ ':' + S.lpad(date.getUTCSeconds(), 2)
		+ 'Z';
}
Dto.toJsonDate = function(date)
{
	return "\/Date(" + date.getTime() + "-0000)\/";
}
Dto.toNumber = function(obj)
{
	return isNaN(obj) ? 0 : obj;
}
Dto.toDto = function(obj)
{
	if (!obj || is.Function(obj)) return null;

	//If they have defined their own toDto method use that instead
	if (obj.toDto) return obj.toDto();

	if (is.Array(obj))
	{
		var arrayOfDto = [];
		for (var i in obj)
		{
			arrayOfDto.push(Dto.toDto(obj[i]));
		}
		return arrayOfDto;
	}

	if (is.String(obj) || is.Boolean(obj)) return obj;

	if (is.Number(obj)) return Dto.toNumber(obj);

	if (is.Date(obj)) return Dto.toDate(obj);

	return Dto.cloneObjectWithPascalFields(obj);
}
Dto.cloneObjectWithPascalFields = function(obj)
{
	var clone = {};
	for (var fieldName in obj)
	{
		var firstChar = fieldName.charCodeAt(0);
		var startsWithCapital = firstChar >= Dto.CHAR_A && firstChar <= Dto.CHAR_Z;
		if (startsWithCapital)
		{
			clone[fieldName] = Dto.toDto(obj[fieldName]);
		}
	}
	return clone;
}
Dto.parseKeyValuePairs = function(oKvps)
{
	var results = {};
	for (var i in oKvps)
	{
		var result = objResults[i];
		var name = result.K;
		var value = result.V;
		results[name] = value;
	}
	return results;
}





/**
* JsonServiceClient.js
* 
* @author Demis Bellot
* @version 1.0
* @requires jQuery.js, jquery-json.js
* 
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function JsonServiceClient(baseUri, type)
{
	JsonServiceClient.$baseConstructor.call(this);

	//should similar to "http://localhost/Service.Host.WebService/Public/Json/SyncReply/"
	this.baseSyncReplyUri = Path.combine(baseUri, "Json/SyncReply");
	this.baseAsyncOneWayUri = Path.combine(baseUri, "Json/AsyncOneWay");

	if (!JSN.cache)
	{
		this.log.debug("CACHE: JSON: " + this.baseSyncReplyUri);
		JSN.setCacheOptions({ size: 0 });
	}
}
var JSN = JsonServiceClient;
JSN.extend(ASObject, { type: "AjaxStack.JsonServiceClient" }, {

	send: function(webMethod, request, onSuccess, onError, ajaxOptions)
	{
		var startCallTime = new Date();
		var requestUrl = Path.combine(this.baseSyncReplyUri, webMethod);
		var id = JsonServiceClient.id++;

		var canCache = !ajaxOptions || ajaxOptions.type != "POST";
		var cacheKey = S.toString(webMethod, request);

		if (canCache)
		{
			var response = JSN.cache.getValue(cacheKey);
			if (response)
			{
				JSN.log.debug("loaded from cache: " + webMethod);
				if (onSuccess) onSuccess(new ResultEvent(response));
				return;
			}
		}

		var options = {
			type: "GET",
			url: requestUrl,
			data: request,
			dataType: "json",
			success: function(response)
			{
				var endCallTime = new Date();
				var callDuration = endCallTime.getTime() - startCallTime.getTime();
				if (!response)
				{
					JSN.log.warn(webMethod + ": returned empty response. time taken: " + callDuration + " ms");
					if (onSuccess) onSuccess(null);
					return;
				}
				else
				{
					JSN.log.debug(webMethod + ": server response time : " + callDuration + " ms");
				}

				if (isResponseSuccessful(response.ResponseStatus, onError))
				{
					if (canCache) JSN.cache.add(cacheKey, response);

					if (onSuccess) onSuccess(new ResultEvent(response));
					JSN.onSuccess({ id: id, webMethod: webMethod, request: request,
						response: response, durationMs: callDuration
					});
				}
				else
				{
					JSN.onError({ id: id, webMethod: webMethod, request: request,
						error: OperationResult.Parse(response.ResponseStatus),
						durationMs: callDuration
					});
				}
			},
			error: function(xhr, desc, exceptionobj)
			{
				var endCallTime = new Date();
				var callDuration = endCallTime.getTime() - startCallTime.getTime();

				//alert("ajax request error: " + xhr.responseText);
				try
				{
					JSN.log.error(xhr.responseText);
					if (onError) onError(xhr.responseText);
				}
				catch (e) { }
				JSN.onError({ id: id, webMethod: webMethod, request: request,
					error: xhr.responseText, durationMs: callDuration
				});
			}
		};

		O.merge(options, ajaxOptions);
		//$.extend(options, ajaxOptions);

		var ajax = $.ajax(options);
	},

	//Sends a HTTP 'GET' request on the QueryString
	getFromService: function(webMethod, request, onSuccess, onError)
	{
		this.send(webMethod, request, onSuccess, onError);
	},

	//Sends a HTTP 'POST' request as key value pair formData
	postFormDataToService: function(webMethod, request, onSuccess, onError)
	{
		this.send(webMethod, request, onSuccess, onError, { type: "POST" });
	},

	//Sends a HTTP 'POST' request as JSON
	postToService: function(webMethod, request, onSuccess, onError)
	{
		var jsonRequest = $.compactJSON(request);
		//$.dump({ request:request, json: jsonRequest });
		this.send(webMethod, jsonRequest, onSuccess, onError,
			{ type: "POST", processData: false, contentType: "application/json; charset=utf-8" });
	}
});

JSN.UNDEFINED_NUMBER = 0;
JSN.id = 0;
JSN.onError = function(args) { };
JSN.onSuccess = function(args) { };
JSN.setCacheOptions = function(options)
{
	JSN.cache = new Cache(options);
}

function isResponseSuccessful(responseStatus, onError)
{
	//if there is no responseStatus then there is no way to work out if it was an error.
	if (!responseStatus) return true;
	
	var result = OperationResult.Parse(responseStatus);
	if (!result.isSuccess)
	{
		if (onError == null)
		{
			JSN.log.error("isResponseSuccessful: result.isSuccess == false: " + result.errorCode);
			return;
		}
		var errorEvent = new ResponseErrorEvent(result);
		onError(errorEvent);
		return false;
	}
	return true;
}

function OperationResult()
{
	this.isSuccess = false;
	this.message = "";
	this.errorCode = "";
	this.stackTrace = "";
	this.fieldErrors = [];
	this.fieldErrorMap = {};
}
OperationResult.prototype.toString = function()
{
	return this.errorCode + ": " + this.message;
}
OperationResult.prototype.getFieldErrors = function()
{
	return this.fieldErrors;
}
OperationResult.prototype.getFieldErrorMap = function()
{
	return this.fieldErrorMap;
}
OperationResult.Parse = function(responseStatus)
{
	var result = new OperationResult();
	result.isSuccess = is.Empty(responseStatus.ErrorCode);
	result.errorCode = responseStatus.ErrorCode;
	result.message = responseStatus.Message;
	result.errorMessage = responseStatus.ErrorMessage;
	result.stackTrace = responseStatus.StackTrace;

	A.each(responseStatus.FieldErrors, function(objError)
	{
		var error = new FieldError(objError.ErrorCode, objError.FieldName, objError.ErrorMessage);
		result.fieldErrors.push(error);

		if (!is.Empty(error.fieldName))
		{
			result.fieldErrorMap[error.fieldName] = error;
		}
	});

	return result;
}

function ResponseErrorEvent(result)
{
	this.result = result;
}

function FieldError(errorCode, fieldName, errorMessage)
{
	this.errorCode = errorCode;
	this.fieldName = fieldName;
	this.errorMessage = errorMessage || '';
}
/// <reference path="../../release/jsApp.js"/>

function NavigationController(app, controllerTypes)
{
	NavigationController.$baseConstructor.call(this);

	this.controllerTypes = controllerTypes || [];

	this.app = app;
	this.currentPath = null;
	this.masterController = null;
	this.backHistory = [];
	this.forwardHistory = [];
}
NavigationController.extend(ASObject, { type: 'AjaxStack.NavigationController' },
	function(p)
	{
		p.getControllers = function()
		{
			if (!this.masterController) return [];
			var all = [this.masterController];
			if (this.masterController.getControllers)
			{
				A.merge(all, this.masterController.getControllers());
			}
			return all;
		}

		p.getCurrentPath = function()
		{
			return this.currentPath;
		}

		p.getBackHistory = function()
		{
			return this.backHistory;
		}

		p.getForwardHistory = function()
		{
			return this.forwardHistory;
		}

		p.registerPath = function(path, fromHistory)
		{
			if (!path) return;
			if (!fromHistory)
			{
				this.backHistory.push(path);
				this.forwardHistory = [];
			}
		}

		p.loadPath = function(path, fromHistory)
		{
			this.log.debug("NAV: this.loadPath(): " + path);
			if (!path) return;

			this.registerPath(this.currentPath, fromHistory);

			this.currentPath = path = Path.getPath(path);

			var controllerCtor = this.masterController
				? this.masterController.constructor.getController(path.getName())
				: null;

			if (controllerCtor)
			{
				this.masterController.loadPath(path);
			}
			else
			{
				var ctor = null;
				for (var i = 0, len = this.controllerTypes.length; i < len; i++)
				{
					var controllerType = this.controllerTypes[i];
					if (controllerType.canHandlePath(path))
					{
						this.log.debug("Loading masterController: " + controllerType.getType() + " with path: " + path);
						this.masterController = new controllerType(this.app);
						this.masterController.loadPath(path); //calls onLoadBody
						break;
					}
				}
				if (!ctor)
				{
					this.log.debug("Could not find masterController for path: " + path);
				}
			}

			this.app.dispatchEvent("onAfterLoadPath", this, path);
		}

		p.loadBody = function(controller, bodyPageHtml)
		{
			if (!this.masterController || this.masterController.getTagId()) return;
			$(this.masterController.getTagId()).html(bodyPageHtml);
		}

		//potentially called by <tag />
		p.gotoPath = function(path)
		{
			var selectedHref = path.toHref();
			location.href = selectedHref;

			$(".nav-link").each(function(i)
			{
				if ($(this).attr("href") == selectedHref)
					$(this).addClass("selected");
				else
					$(this).removeClass("selected");
			});

			var nav = AppBase.getNav();

			nav.log.debug("NAV: executing .nav-link: " + path);

			Quirks.forceBlur();

			//Skip loading already loaded page.
			if (nav.masterController && path.equals(nav.masterController.getPath()))
			{
				nav.log.debug("NAV: skipping already loaded path: " + path + " == " + nav.masterController.getPath());
				return;
			}

			nav.loadPath(path);
		}

		//called by <tag />
		p.handlerClickNavLink = function(e)
		{
			e.stopPropagation();
			var selectedHref = $(this).attr("href");
			AppBase.getNav().gotoPath(Path.getPath(selectedHref));
		}

		//called by <tag />
		p.handlerClickActionLink = function(e)
		{
			//we don't want action-links to change the url
			e.stopPropagation();

			var nav = AppBase.getNav();
			var path = Path.getPath($(this).attr("href"));
			nav.log.debug("NAV: executing .action-link: " + path);

			var allControllers = A.cat(
				AppBase.getInstance().getControllers(),
				AppBase.getInstance().getContexts()
			);
			for (var i = 0, len = allControllers.length; i < len; i++)
			{
				var controller = allControllers[i];
				if (controller[path.getName()])
				{
					nav.log.debug("controller .action-link found: " + path.getName() + " on controller: " + controller);

					controller[path.getName()](this, path.getArgs());
				}
			}
		}

		p.onAfterContentLoaded = function(source, args)
		{
			this.log.debug("NAV: onAfterContentLoaded(): " + args.id);
			var rootSel = args.id ? args.id + " " : "";

			$(rootSel + ".nav-link").unbind("click").click(this.handlerClickNavLink);

			//register .action-links to => controller[A#href]([args]) if exists;
			$(rootSel + ".action-link").unbind("click").click(this.handlerClickActionLink);
		}

		p.actionNavBack = function(source, args)
		{
			if (this.backHistory.length == 0) return;

			var popPath = this.backHistory.pop();
			if (this.currentPath)
			{
				this.forwardHistory.push(this.currentPath);
			}
			this.log.debug("actionNavBack: " + popPath);
			this.loadPath(popPath, true);
		}

		p.actionNavForward = function(source, args)
		{
			if (this.forwardHistory.length == 0) return;

			var popPath = this.forwardHistory.pop();
			this.backHistory.push(popPath);

			this.log.debug("actionNavForward: " + popPath);
			this.loadPath(popPath, true);
		}

	}
);

/// <reference path="../../release/jsApp.js"/>

/* Base Controller for all UiControllers */

function UiController(app, tagId)
{
	UiController.$baseConstructor.call(this);

	this.app = app;
	this.tagId = tagId;
	this.path = null;
	this.prevPath = null;
	this.nextPath = null;
	this.controllers = [];

//	UiController.registerControllers(UiController, [this.constructor]);
}
UiController.extend(ASObject, { type: 'AjaxStack.UiController' },
	function(p)
	{
		p.getApp = function() { return this.app; }

		p.getPath = function() { return this.path; }

		p.setNavPath = function(prevPath, nextPath)
		{
			this.prevPath = prevPath;
			this.nextPath = nextPath;
		}

		p.getTagId = function() { return this.tagId; }

		p.getTitle = function() { return "Untitled"; }

		p.getBreadcrumbs = function() { return []; }

		p.getName = function()
		{
			return this.constructor.getName
				? this.constructor.getName() : this.constructor.getType();
		}

		p.getControllers = function()
		{
			var all = [];
			for (var i = 0, len = this.controllers.length; i < len; i++)
			{
				var controller = this.controllers[i];
				all.push(controller);
				A.merge(all, controller.getControllers());
			}
			return all;
		}

		p.onAfterRender = function() { }

		p.render = function(html, tagId)
		{
			tagId = tagId || this.getTagId();
			if (!tagId) return;
			$(tagId).html(html);

			this.onAfterRender();
			this.dispatchEvent("onAfterContentLoaded", this, { id: tagId });
		}

		p.dispatchEvent = function(eventName, source, args)
		{
			this.getApp().dispatchEvent(eventName, source, args);
		}

		p.loadPath = function(path)
		{
			if (!path) return;
			this.path = path;

			if (AppBase.getAppPages())
			{
				this.log.warn("AppPages not set, skipping.");

				var pageFn = AppBase.getAppPages().pages[this.path.getName()];
				if (pageFn)
				{
					this.log.debug("Rendering html for path: " + this.path.getName());
					this.render(pageFn());
					return;
				}
			}
			this.log.debug("Could not find html for path: " + this.path.getName());
			this.render();
		}
	}
);
UiController.controllerMap = {};
UiController.log = new Logger(UiController.getType());
UiController.getController = function(name)
{
	return this.controllerMap[name];
}
UiController.registerControllers = function(masterCtor, controllers, options)
{
	if (!masterCtor.controllerMap)
	{
		masterCtor.controllerMap = {};
		masterCtor.getController = UiController.getController;
	}
	for (var i = 0, len = controllers.length; i < len; i++)
	{
		var c = controllers[i];
		var name = c.getName ? c.getName() : c.getType();
		masterCtor.controllerMap[name] = c;
	}

	options = options || {};
	masterCtor.canHandlePath = options.canHandlePathFn || UiController.handleRegisteredPaths;
}

//return not-null regardless of name to indicate it can handle all paths
UiController.wildcard = new UiController();
UiController.handleRegisteredPaths = function(path)
{
	return this.getController(path.getName()) != null;
}
UiController.handleAnyPath = function(path)
{
	return true;
}
UiController.render = function(tagId, html)
{
	if (!tagId) return;
	$(tagId).html(html);
	AppBase.getInstance().dispatchEvent("onAfterContentLoaded", this, { id: tagId });
}
/// <reference path="../../release/jsApp.js"/>

function ContextBase(app, gateway)
{
	this.app = app;
	this.gateway = gateway;
	this.cacheBlackList = [];
}
ContextBase.Version = 100;
ContextBase.extend(ASObject, { type: 'AjaxStack.ContextBase' },
	function(p)
	{
		p.dispatchEvent = function(eventName, source, args)
		{
			this.app.dispatchEvent(eventName, source, args);
		}
		p.getCacheBlackList = function()
		{
			return this.cacheBlackList;
		}
		p.setCacheBlackList = function()
		{
			this.cacheBlackList = arguments;
		}
		p.createRequest = function(withArgs)
		{
			var request = { Version: this.constructor.Version || ContextBase.Version };
			if (withArgs)
			{
				O.merge(request, withArgs);
			}
			return request;
		}
	}
);

/**
* ArrayExt.js
* 
* @author Demis Bellot
* @version 1.0
* 
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function ArrayExt() { }
var A = ArrayExt;
A.convertAll = function(array, convertFn)
{
	var to = [];
	for (var i = 0, len = array.length; i < len; i++)
		to[i] = convertFn(array[i]);
	return to;
};
A.containsValue = function(array, item)
{
	for (var i = 0, len = array.length; i < len; i++) 
		if (array[i] == item) return true;

	return false;
};
A.any = function(array, mapFn)
{
	for (var i = 0, len = array.length; i < len; i++) 
		if (mapFn(array[i])) return true;

	return false;
};
A.all = function(array, mapFn)
{
	for (var i = 0, len = array.length; i < len; i++)
		if (!mapFn(array[i])) return false;

	return true;
};
A.remove = function(array, from, to)
{
	var rest = array.slice((to || from) + 1 || array.length);
	array.length = from < 0 ? array.length + from : from;
	return array.push.apply(array, rest);
};
A.join = function(array, on)
{
	var s = "";
	on = on || ",";
	for (var i = 0, len = array.length; i < len; i++)
	{
		if (s) s += on;
		s += array[i];
	}
	return s;
};
A.isEmpty = function(array)
{
	return !array || array.length == 0;
};
A.each = function(array, fn)
{
	if (!array) return;
	for (var i = 0, len = array.length; i < len; i++)
		fn(array[i]);
};
A.merge = function(a1, a2)
{
	A.each(a2, function(item) { a1.push(item); });
	return a1;
};
A.cat = function()
{
	var all = [];
	for (var i = 0, len = arguments.length; i < arguments.length; i++)
	{
		A.merge(all, arguments[i]);
	}
	return all;
};
A.clone = function(array)
{
	return array.slice();
};
A.sort = function(array, sortFn) {
    array.sort(sortFn);
    return array;
};
A.areEqual = function(array, other) {
    if (!(is.Array(array) && is.Array(other))) return false;
    if (array.length != other.length) return false;

    for (var i = 0; i < array.length; i++) {
        if (!A.containsValue(other, array[i])) return false;
    }
    return true;
};
A.take = function(array, count) {
    var take = array.length < count ? array.length : count;
    var to = [];
    for (var i = 0, len = take; i < len; i++)
        to.push(array[i]);

    return to;
};
A.skip = function(array, count) {
    var skip = array.length < count ? array.length : count;
    var to = [];
    for (var i = skip, len = array.length; i < len; i++)
        to.push(array[i]);

    return to;
};
A.insert = function(array, index, item) {
    if (index >= 0) {
        var a = array.slice(), b = a.splice(index);
        array[index] = item;
        return a.concat(b);
    }
};

function Environment() { }
var E = Environment;
E.getBrowserNameWithVersion = function()
{
	if (E.browser.msie)
	{
		if (document.getElementById && document.compatMode && !window.XMLHttpRequest) return "msie6";
		if (document.getElementById && document.compatMode && window.XMLHttpRequest && !document.documentMode) return "msie7";
		if (document.getElementById && document.compatMode && window.XMLHttpRequest && document.documentMode) return "msie8";
	}
	return E.getBrowserName() + parseInt(navigator.appVersion);
};

var userAgent = navigator.userAgent.toLowerCase();
E.browser = {
	chrome: /chrome/.test(userAgent),
	safari: /webkit/.test(userAgent) && !/chrome/.test(userAgent),
	opera: /opera/.test(userAgent),
	msie: /msie/.test(userAgent) && !/opera/.test(userAgent),
	firefox: /mozilla/.test(userAgent) && !/(compatible|webkit)/.test(userAgent)
};
E.browser.webkit = E.browser.chrome || E.browser.safari;

E.getHostName = Path.getHostName;

E.getBrowserName = function()
{
	if (E.browser.chrome) return "chrome";
	if (E.browser.safari) return "safari";
	if (E.browser.opera) return "opera";
	if (E.browser.msie) return "msie";
	if (E.browser.firefox) return "firefox";
};
E.browser[E.getBrowserNameWithVersion()] = true; //msie8 = true;

E.resNames = ["res-low", "res-1280", "res-high"];
E.getResolutionName = function()
{
	var w = $(window).width();
	if (w <= 1024) return E.resNames[0];
	if (w <= 1280) return E.resNames[1];
	if (w > 1280) return E.resNames[2];
	return "res-low";
};
E.getValueByResolutionWidth = function(low, medium, high)
{
	var w = $(window).width();
	if (w <= 1024) return low;
	if (w <= 1280) return medium;
	if (w > 1280) return high;
	return low;
};
E.getValueByResolutionHeight = function(low, med800, med1050, high)
{
	var windowHeight = $(window).height();
	//header size
	var unavailableResolution = 181;
	if (E.browser.firefox) unavailableResolution = 181;
	if (E.browser.safari) unavailableResolution = 91;
	if (E.browser.chrome) unavailableResolution = 99;
	if (E.browser.msie8) unavailableResolution = 131;

	//alert(windowHeight + ":" + (768 - windowHeight) + ":" + (768 - unavailableResolution));
	if (windowHeight <= 768 - unavailableResolution) return low;
	if (windowHeight <= 800 - unavailableResolution) return med800;
	if (windowHeight <= 1050 - unavailableResolution) return med1050;
	if (windowHeight > 1050 - unavailableResolution) return high;
	return low;
};

/// <reference path="../release/ajaxstack-core.js"/>
/// <reference path="../release/jsApi-vsdoc.js"/>

function FormExt()
{
}
var F = FormExt;
F.eachField = function(fieldSels, validateFn)
{
	if (is.String(fieldSels)) fieldSels = [fieldSels];

	var gValid = true;
	A.each(fieldSels, function(field)
	{
		var jq = $(field);
		jq.each(function(i)
		{
			var valid = validateFn($(this));
			gValid = gValid && valid;
		});
	});

	return gValid;
}
F.validateRequired = function(fieldSels)
{
	return F.eachField(fieldSels, function(jq)
	{
		var val = S.trim(jq.val());
		if (!val)
		{
			JQ.addAfter(jq, { 'class': 'error' }, "* required");
			return false;
		}
		return true;
	});
}
F.validateEmail = function(fieldSels)
{
	return F.eachField(fieldSels, function(jq)
	{
		var val = S.trim(jq.val());
		if (!val)
		{
			JQ.addAfter(jq, { 'class': 'error' }, "* required");
			return false;
		}

		var regEx = /^([A-Za-z0-9_\-\.])+\@([A-Za-z0-9_\-\.])+\.([A-Za-z]{2,4})$/;
		if (regEx.test(val) == false)
		{
			JQ.addAfter(jq, { 'class': 'error' }, "* invalid email");
			return false;
		}

		return true;
	});
}
F.validateTextDate = function(fieldSels)
{
	return F.eachField(fieldSels, function(jq)
	{
		var inputs = jq.children('INPUT');
		if (inputs.length != 3)
		{
			JQ.addAfter(jq, { 'class': 'error' }, "* invalid control");
			return false;
		}

		if (F.parseTextDate(jq) == null)
		{
			JQ.addAfter(jq, { 'class': 'error' }, "* invalid date");
			return false;
		}

		return true;
	});
}
F.parseTextDate = function(jq)
{
	var inputs = jq.children('INPUT');
	
	var dd = $(inputs.get(0)).val(),
		mm = $(inputs.get(1)).val(),
		yyyy = $(inputs.get(2)).val();

	if (!is.IntegerInRange(parseInt(dd, 10), 1, 31)
			|| !is.IntegerInRange(parseInt(mm, 10), 1, 12)
			|| !is.IntegerInRange(parseInt(yyyy, 10), 1000, 9999))
	{
		return null;
	}

	return new Date(yyyy, mm, dd);
}
F.validate = function(formIdSel)
{
	$(formIdSel + " .error").remove();
	
	var valid = true;
	if (!F.validateRequired(formIdSel + " .required")) valid = false;
	if (!F.validateEmail(formIdSel + " .required-email")) valid = false;
	if (!F.validateTextDate(formIdSel + " .required-textdate")) valid = false;
	return valid;
}
F.bindResponseError = function(formIdSel, responseError)
{
	var fieldErrorMap = responseError.getFieldErrorMap();

	var boundAtLeastOne = false;
	$(formIdSel + " :input").each(function(i)
	{
		var fieldName = $(this).attr('name');
		var fieldError = fieldErrorMap[fieldName];
		if (fieldError)
		{
			boundAtLeastOne = true;
			JQ.addAfter($(this), { 'class': 'error' }, "* " + fieldError.errorMessage);
		}
	});

	if (!boundAtLeastOne)
	{
		$(formIdSel + ' .message').html(H.span({ 'class': 'error' }, responseError.errorMessage));
	}

	return boundAtLeastOne;
}
F.bindForm = function(formIdSel, options)
{
	options = options || {};

	if (options.submitFn)
	{
		if (options.submitButton)
		{
			$(options.submitButton).click(options.submitFn);
		}

		$(formIdSel + " INPUT").each(function(i)
		{
			var fieldType = $(this).attr('type');
			if (fieldType.toLowerCase() == "text" || fieldType.toLowerCase() == "password")
			{
				$(this).keypress(function(e)
				{
					if (Key.isEnterKey(e.which))
					{
						if (options.submitButton) 
							$(options.submitButton).click();
						else 
							options.submitFn();
					}
				});
			}
		});
	}
}



/**
* Html.js
*  
* @author Demis Bellot
* @version 1.0
* @requires Class.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Html() { }
var H = Html;

H.tag = function(tagName, attrs, innerHtml)
{
	attrs = attrs || {};

	//If the only arg is a string, its actually the content
	if (is.ValueType(attrs) && !innerHtml)
	{
		innerHtml = attrs;
		attrs = null;
	}
	else if (attrs['dataSource'])
	{
		//every tag also supports a dataSource/onItemDataBound templating if supplied
		innerHtml = innerHtml || "";
		var dataSource = attrs['dataSource'];
		for (var i in dataSource)
		{
			innerHtml += attrs['onItemDataBound']
				? attrs['onItemDataBound'](dataSource[i])
				: dataSource[i].toString();
		}
		delete attrs['dataSource'];
		if (attrs['onItemDataBound']) delete attrs['onItemDataBound'];
	}

	var html = "<" + tagName;
	var attrStr = S.createAttr(attrs);
	if (attrStr) html += " " + attrStr;

	html += !innerHtml
		? "/>"
		: ">" + innerHtml + "</" + tagName + ">";

	return html;
};

H.tagNames = "span,div,p,label,a,b,i,em,code,pre,ul,ol,li,dl,dt,dd,h1,h2,h3,h4,h5,h6,br,img,input,button,form,fieldset,iframe,abbr,acronym,defn".split(',');
for (var i in H.tagNames)
{
	(function(tagName)
	{
		Html[tagName] = function(attrs, innerHtml)
		{
			return H.tag(tagName, attrs, innerHtml);
		};
	})(H.tagNames[i]);
}

H.inputText = function(attrs)
{
	attrs['type'] = "text";
	return H.tag("input", attrs);
};
H.inputCheckbox = function(attrs)
{
	attrs['type'] = "checkbox";
	return H.tag("input", attrs);
};
H.textArea = function(attrs)
{
	var value = attrs['value'];
	delete attrs['value'];
	return H.tag("textarea", attrs, value);
};

/**
* HtmlExt.js
* 
* @author Demis Bellot
* @version 1.0
* @requires Class.js, StringExt.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

var H = H || {};
H.createTable = function(oRows, tableOptions)
{
	tableOptions = tableOptions || {};
	var theadHtml = "";
	var trHtml = "";

	for (var i in oRows)
	{
		var oData = oRows[i];
		if (!theadHtml)
		{
			for (var key in tableOptions.showFields)
			{
				theadHtml += "<th>" + tableOptions.showFields[key] + "</th>";
			}
		}
		var rowClassName = i % 2 == 0 ? "odd" : "even";
		if (tableOptions.rowClass)
		{
			rowClassName += is.Function(tableOptions.rowClass)
				? " " + tableOptions.rowClass(oData)
				: " " + tableOptions.rowClass;
		}
		trHtml += "<tr class='" + rowClassName + "'>";

		for (var key in tableOptions.showFields)
		{
			var label = tableOptions.showFields[key];
			var val = is.Function(oData[key]) ? oData[key]() : oData[key];
			if (!val)
			{
				val = "";
			}
			if (tableOptions.maxFieldLength && val.indexOf("<") == -1)
			{
				val = S.cropText(val, tableOptions.maxFieldLength);
			}
			trHtml += "<td>" + val + "</td>";
		}

		trHtml += "</tr>";
	}
	var tableHtml = "<table class='" + tableOptions.tableClassName + "'><thead><tr>" + theadHtml + "</tr></thead><tbody>" + trHtml + "</tbody></table>";
	return tableHtml;
}

H.navLink = function(attrs, innerHtml)
{
	return H.actionLink(attrs, innerHtml, 'nav-link');
}

H.actionLink = function(attrs, innerHtml, linkClass)
{
	linkClass = linkClass || 'action-link';
	var tag = "span";
	if (attrs['tag'])
	{
		tag = attrs['tag'];
		delete attrs['tag'];
	}
	if (attrs['action'])
	{
		var action = attrs['action'];
		if (is.TypeOf(action, Path.getType())) action = action.toUrl();
		attrs['href'] = is.Array(action) ? H.pathInfoHash(action) : '#' + action;
		delete attrs['action'];
	}
	attrs['class'] = attrs['class'] ? attrs['class'] + ' ' + linkClass : linkClass;
	return H.tag(tag, attrs, innerHtml);
}

H.safeAttr = function(attrValue)
{
	return attrValue.toString().replace('"', '\\"');
}

H.pathInfo = function(parts)
{
	var attr = "";
	for (i in parts)
	{
		if (attr) attr += "/";
		attr += H.safeAttr(parts[i]);
	}
	return attr;
}

H.pathInfoHash = function(parts)
{
	return '#' + H.pathInfo(parts);
}


/// <reference path="../release/ajaxstack-core.js"/>
/// <reference path="../release/jsApi-vsdoc.js"/>

function jQueryExt()
{
}
var JQ = jQueryExt;
JQ.addAfter = function(jq, attr, html)
{
	attr = attr || {};
	var id = attr['id'], className = attr['class'], next = jq.next();

	//skip if this element already exists
	if (next)
	{
		if (id && next.attr('id') == id) return;
		if (className && next.attr('class') == className) return;
	}

	jq.parent().append(H.span(attr, html));
}
JQ.removeAfter = function(jq, attr)
{
	attr = attr || {};
	var id = attr['id'], className = attr['class'], next = jq.next();

	if (!next) return;

	if (id && next.attr('id') == id) next.remove();
	else if (className && next.attr('class') == className) next.remove();
	else next.remove();
}
JQ.disable = function(jq)
{
	jq.attr('disabled', true);
	jq.blur();
}
JQ.enable = function(jq)
{
	jq.attr('disabled', false);
}

/**
* Key.js
* 
* @author Demis Bellot
* @version 1.0
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Key() { }
Key.isRenameKey = function(key) { return key == Key.f2 || key == Key.enter; };
Key.isEnterKey = function(key) { return key == Key.enter; };
Key.isEscapeKey = function(key) { return key == Key.escape; };
Key.isSpaceKey = function(key) { return key == Key.space; };
Key.isUpKey = function(key) { return key == Key.up; };
Key.isDownKey = function(key) { return key == Key.down; };
Key.isDeleteKey = function(key) { return key == Key.$delete; };
Key.escape = 27;
Key.f2 = 113;
Key.enter = 13;
Key.space = 32;
Key.left = 37;
Key.up = 38;
Key.right = 39;
Key.down = 40;
Key.$delete = 46;

Key.processKey = function(key, onEnterFn, onCancelFn)
{
	if (Key.isRenameKey(key))
	{
		onEnterFn();
		return false;
	}
	else if (Key.isEscapeKey(key))
	{
		onCancelFn();
		return false;
	}
	return true;
};


/**
* Logger.js
* 
* @author Demis Bellot
* @version 1.0
* @requres StringExt.js, ArrayExt.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Logger(category)
{
	this.category = category;
}
Logger.extend(ASObject, { type: 'AjaxStack.Logger' }, {
	debug: function(message)
	{
		this.log("DEBUG", message);
	},

	info: function(message)
	{
		this.log("INFO", message);
	},

	warn: function(message)
	{
		this.log("WARN", message);
	},

	error: function(message)
	{
		Logger.errors++;
		this.log("ERROR", message);
	},

	fatal: function(message)
	{
		this.log("FATAL", message);
	},

	log: function(level, message)
	{
		if (!window.console) return;
		if (Logger.logLevels.length > 0 && !A.containsValue(Logger.logLevels, level)) return;
		if (Logger.logCategories.length > 0 && !A.containsValue(Logger.logCategories, this.category)) return;
		if (Logger.logAfterErrorNo && Logger.errors < Logger.logAfterErrorNo) return;
		if (Logger.logMessagesStartingWith.length > 0)
		{
			var messageStartsWithAFilter = A.any(Logger.logMessagesStartingWith,
				function(key, value)
				{
					return S.startsWith(message, value);
				});
			if (!messageStartsWithAFilter) return;
		}
		window.console.log(level + ": " + message);
	}

});

Logger.logCategories = [];
Logger.logLevels = [];
Logger.logMessagesStartingWith = [];
//Logger.logAfterErrorNo = 1;
Logger.errors = 0;

/**
* ObjectExt.js
* 
* @author Demis Bellot
* @version 1.0
* @requires Class.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function ObjectExt() { }
var O = ObjectExt;
O.keys = function(obj)
{
	var keys = [];
	for (var key in obj) keys.push(key);
	return keys;
}
O.values = function(obj)
{
	var values = [];
	for (var key in obj) values.push(obj[key]);
	return values;
}
O.containsKey = function(obj, item)
{
	for (var key in obj) if (key == item) return true;
	return false;
}
O.containsValue = function(obj, item)
{
	for (var key in obj) if (obj[key] == item) return true;
	return false;
}
O.any = function(obj, mapFn)
{
	for (var key in obj) if (mapFn(key, obj[key])) return true;
	return false;
}
O.all = function(obj, mapFn)
{
	for (var key in obj) if (!mapFn(key, obj[key])) return false;
	return true;
}
O.isEmpty = function(o1)
{
	if (!o1) return true;
	for (var a in o1) return false;
	return true;
}
O.each = function(o, fn)
{
	for (var i in o) fn(i, o[i]);
}
O.merge = function(dst, src)
{
	for (var k in src) dst[k] = src[k];
	return dst;
}
O.clone = function(obj)
{
	var to = {};
	for (var key in obj) to[key] = obj[key];
	return to;
}
O.factoryFn = function()
{
	return {};
}
O.areEqual = function(obj, other) {
    if (!(is.Object(obj) && is.Object(other))) return false;

    for (var k in obj) {
        if (obj[k] != other[k]) return false;
    }
    for (var k in other) {
        if (obj[k] != other[k]) return false;
    }
    return true;
}
/**
* Path.js
* 
* @author Demis Bellot
* @version 1.0
* @requres Class.js, StringExt.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

//#{name}/{arg1/arg2}?{op1=1&op2=2}
function Path(name, args, options)
{
	this.name = name;
	this.options = options || {};
	this.args = args || [];
	if (!is.Array(this.args)) this.args = [this.args];
}
Path.extend(ASObject, { type: "AjaxStack.Path" }, {
	toUrl: function()
	{
		var str = this.name;
		if (this.args.length > 0)
		{
			str += '/' + this.args.join('/');
		}
		var qs = "";
		for (var key in this.options)
		{
			qs += qs ? '&' : '?';
			qs += key + "=" + encodeURIComponent(this.options[key]);
		}
		return str + qs;
	},
	
	//IE has problems overrideing toString(), use toUrl() instead
	toString: function()
	{
		return this.toUrl();
	},

	getName: function()
	{
		return this.name;
	},

	getArgs: function()
	{
		return this.args;
	},

	getFirstArg: function()
	{
		return this.args.length > 0 ? this.args[0] : null;
	},

	setFirstArg: function(id)
	{
		return this.args[0] = id;
	},

	getOptions: function()
	{
		return this.options;
	},

	toHref: function()
	{
		return '#' + this.toUrl();
	},

	equals: function(other)
	{
		if (!other) return false;
		var otherHref = other.toHref ? other.toHref() : other.toString()
		return this.toHref() == otherHref;
	},

	nameEquals: function(otherPath)
	{
		if (!otherPath) return false;
		otherPath = Path.getPath(otherPath);
		return this.getName() == otherPath.getName();
	}
});

Path.getFirstArg = function(href)
{
	var path = Path.parse(href);
	return path ? path.getFirstArg() : null;
}
Path.getPath = function(path)
{
	if (is.TypeOf(path, Path.getType())) return path;
	return Path.parse(path);
}
Path.parse = function(href)
{
	if (!href) return null;
	if (href.indexOf('#') == -1) href = '#' + href;
	return Path.parseHref(href);
}
Path.parseHref = function(href)
{
	var href = href || window.location.href;
	var nameStartPos = href.indexOf('#');
	if (nameStartPos > -1)
	{
		var pathInfo = href.toString().substr(nameStartPos + 1);

		var options = {};
		var qsPos = pathInfo.indexOf("?");
		if (qsPos != -1)
		{
			var qs = pathInfo.substr(qsPos);
			pathInfo = pathInfo.substring(0, qsPos);
			options = Path.parseQueryString(qs);
		}

		var parts = pathInfo.split("/");
		var args = [];
		for (var i = 1; i < parts.length; i++)
		{
			args.push(unescape(parts[i]));
		}
		var path = new Path(parts[0], args, options);
		return path;
	}
	return null;
}
Path.getHostName = function(href)
{
	href = S.eat(href || window.location.href, "://");
	return href.substring(0, href.indexOf('/'));
}
Path.parseQueryString = function(href)
{
	var qString = S.eat(href || window.location.href, '?');
	if (!qString) return {};
	var qMap = {};
	var kvps = qString.split('&');
	for (var i = 0; i < kvps.length; i++)
	{
		var kvp = kvps[i].split('=');
		qMap[kvp[0]] = kvp.length == 1 ? true : kvp[1];
	}
	return qMap;
}
Path.combine = function() {
    var paths = "";
    for (var i = 0, len = arguments.length; i < arguments.length; i++) {
        
        if (paths.length > 0)
            paths += "/";

        paths += S.rtrim(arguments[i], '/');
    }
    return paths;
}
/**
* Reflection.js
* 
* @author Demis Bellot
* @version 1.0
* @requres StringExt.js, ArrayExt.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Reflection() { }
var R = Reflection;
R.log = new Logger("Reflection");

R.convertAll = function(objects, factoryFn, fields)
{
	if (!objects) return [];
	var to = [];
	for (var i = 0, len = objects.length; i < len; i++)
	{
		var item = R.convert(objects[i], factoryFn, fields);
		to.push(item);
	}
	return to;
};

R.convert = function(from, factoryFn, fields)
{
	if (!from) return null;
	return R.populate(factoryFn(), from, fields);
};

R.populate = function(to, from, fields)
{
	try
	{
		if (fields)
		{
			for (var i = 0, len = fields.length; i < len; i++)
			{
				var field = fields[i];
				if (!from.hasOwnProperty(field))
				{
					R.log.error("R.populate: Field '" + field + "' does not exist!");
					continue;
				}
				R.setField(from, to, field);
			}
		}
		else
		{
			for (var field in from)
			{
				R.setField(from, to, field);
			}
		}
	}
	catch (e)
	{
		R.log.error("R.populate: convert(), message = " + e.message);
	}
	return to;
};

R.getField = function(obj, field)
{
	var CHAR_A = "A".charCodeAt(0);
	var CHAR_Z = "Z".charCodeAt(0);
	var CHAR_a = "a".charCodeAt(0);
	var CHAR_z = "z".charCodeAt(0);

	if (obj.hasOwnProperty(field))
	{
		return field;
	}

	var firstChar = field.charCodeAt(0);
	var startsWithUpper = firstChar >= CHAR_A && firstChar <= CHAR_Z;
	var startsWithLower = firstChar >= CHAR_a && firstChar <= CHAR_z;

	var altFieldName = "";
	if (startsWithUpper)
	{
		altFieldName = field.charAt(0).toLowerCase() + field.substr(1);
	}
	else if (startsWithLower)
	{
		altFieldName = field.charAt(0).toUpperCase() + field.substr(1);
	}

	return obj.hasOwnProperty(altFieldName) ? altFieldName : null;
};

R.setField = function(from, to, fromField)
{
	//Skip reserved vars prefixed with '$'
	if (!fromField || S.startsWith(fromField, "$")) return;

	//Skip functions
	var fromFieldValue = from[fromField];
	if (is.Function(fromFieldValue)) return;

	var toField = R.getField(to, fromField);

	if (toField == null)
	{
		//if the field doesn't exist on 'to' and its not a 'valueType' ignore
		if (!is.ValueType(fromFieldValue)) return;
		toField = fromField;
	}


	if (fromFieldValue == undefined)
	{
		to[toField] = undefined;
	}
	else if (fromFieldValue == null)
	{
		to[toField] = null;
	}
	else
	{
		try
		{
			to[toField] = fromFieldValue;
		}
		catch (e)
		{
			R.log.error("R.setField: Error on '" + to + "'[" + toField + "] = " + fromFieldValue);
			//Can happen when the fields are not the same type.
			//TODO: find more effiecient way to find if a field is a date
			if (is.Date(to[toField]))
			{
				to[toField] = Dto.parseDate(fromFieldValue);
			}
		}
	}
};

//Converts dates from JSON and XML to a Date
R.parseDate = function(dateObject)
{
	if (!dateObject) return null;
	if (is.Date(dateObject)) return dateObject;
	var dateStr = dateObject.toString();

	//Parse Date From String
	if (is.String(dateObject))
	{
		var dateMilliSecondsIndex = dateStr.indexOf(".");
		var isDatabaseDate = dateMilliSecondsIndex != -1;
		if (isDatabaseDate)
		{
			//Need to convert '2008-01-25 06:14:23.223' to '2008/01/25 06:14:23 GMT-0000'
			dateStr = dateStr.substr(0, dateMilliSecondsIndex);
			dateStr = dateStr.replace(/-/g, "/");
			dateStr += " GMT-0000";
			dateValue = new Date(Date.parse(dateStr));
			return dateValue;
		}
	}

	var dateValue;

	//Parse Date from WCF Json Date Value: '\/Date(-2208960000000-0800)\/'
	var isWcfJsonDate = /\/Date(.*)\//.test(dateStr);
	if (isWcfJsonDate)
	{
		dateStr = dateStr.match(/Date\((.*?)\)/)[1];
		dateValue = new Date(parseInt(dateStr));
		return dateValue;
	}
	dateValue = new Date(dateObject);
	return dateValue;
};

//converts 2005-12-25T00:00:00 to 2005/12/25 00:00:00 so it can be correctly parsed 
//by the Date() constructor
R.parseDateFromXml = function(dateObject)
{
	var dateStr = dateObject.toString();
	if (!dateStr || dateStr == "" || dateStr == null)
	{
		return new Date(0);
	}
	dateStr = dateStr.replace(/-/g, "/");
	dateStr = dateStr.replace("T", " ");
	dateStr = dateStr.replace(/\..*/, "");
	dateStr += " GMT-0000";
	return new Date(dateStr);
};

R.dateToWcfJsonDate = function(date)
{
	return "\/Date(" + date.getTime() + "-0000)\/";
};



function ResultEvent(result)
{
	this.result = result;
	this.errorCode = null;
	this.isSuccess = is.Empty(this.errorCode);
	this.errorMessage = null;
}
var RE = ResultEvent;
ResultEvent.createError = function(errorCode, errorMessage, result)
{
	var r = new ResultEvent(result);
	r.errorCode = errorCode || 'Error';
	r.isSuccess = false;
	r.errorMessage = errorMessage;
	return r;
}
ResultEvent.prototype =
{
	getResult: function() { return this.result; },
	success: function() { return is.Empty(this.errorCode); },
	getErrorCode: function() { return this.errorCode; },
	getErrorMessage: function() { return this.errorMessage; }
};

/**
* Script.js
* 
* @author Demis Bellot
* @version 1.0
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Script() { }
var scriptId = 1;
Script.append = function(url)
{
	var element = document.createElement("script");
	element.id = "ASScript" + scriptId++;
	element.src = url;
	document.body.appendChild(element);
};

/**
* StringExt.js
* 
* @author Demis Bellot
* @version 1.0
* @requires Class.js
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function StringExt() { } 
var S = StringExt;
S.OVERFLOW_SUFFIX = "..";
S.ESCAPE_CHARS_REGEX = new RegExp("([{}\(\)\^$&.\*\?\/\+\|\[\\\\]|\]|\-)", "g");
S.upperCaseFirstChar = function(fieldName)
{
	if (!fieldName) return fieldName;
	return fieldName.charAt(0).toUpperCase() + fieldName.substr(1);
};
S.escapeRegexChars = function(s)
{
	var newString = s.replace(S.ESCAPE_CHARS_REGEX, "\\$1");
	return newString;
};
S.startsWith = function(text, startsWith)
{
	if (!text || !startsWith) return false;
	if (startsWith.length > text.length) return false;
	return text.substring(0, startsWith.length) == startsWith;
};
S.startsWithAny = function(text, startsWithArray)
{
	for (var i = 0; i < startsWithArray.length; i++)
	{
		if (S.startsWith(text, startsWithArray[i])) return true;
	}
	return false;
};
S.escapeHtml = function(str)
{
	if (!str) return str;
	if (!is.String(str)) str = str.toString();
	str = str.replace(/&/g, "&amp;");
	str = str.replace(/</g, "&lt;");
	str = str.replace(/>/g, "&gt;");
	return str;
};
S.cropText = function(text, lineLength, testLength)
{
	testLength = isNaN(testLength) ? text.length : testLength;
	return testLength <= lineLength ? text : text.substr(0, lineLength) + S.OVERFLOW_SUFFIX;
};
S.chop = function(text, length)
{
	length = length || 1;
	return text.substring(0, text.length - length);
};
S.equals = function(to, from)
{
	if (!to || !from) return false;
	to = is.String(to) ? to : to.toString();
	from = is.String(from) ? from : from.toString();
	return to == from;
};
S.pad = function(text, padLen, padChar, lpad)
{
	var padChar = padChar || "0";
	text = text.toString();
	var s = text;
	for (var i = text.length; i < padLen; i++)
	{
		s = lpad 
			? padChar + s 
			: s + padChar;
	}
	return s;
}
S.padLeft = function(text, padLen, padChar)
{
	return S.pad(text, padLen, padChar, true);
}
S.padRight = function(text, padLen, padChar)
{
	return S.pad(text, padLen, padChar, false);
}
S.lpad = S.padLeft;
S.rpad = S.padRight;
S.createAttr = function(kvp)
{
	var attrName, text = "";
	for (attrName in kvp)
	{
		if (text) text += " ";
		var attrValue = kvp[attrName] || "";
		text += attrName + '="' + attrValue.toString().replace('"', '\\"') + '"';
	}
	return text;
};
S.eat = function(text, until, greedy)
{
	var pos = greedy ? text.lastIndexOf(until) : text.indexOf(until);
	return (pos != -1) ? text.substring(pos + until.length) : null;
};
S.cat = function()
{
	var all = "";
	for (var i = 0, len = arguments.length; i < arguments.length; i++)
	{
		all += arguments[i];
	}
	return all;
}
S.toString = function()
{
	if (arguments.length == 0 || !arguments[0]) return null;

	var s = "";
	for (var i = 0; i < arguments.length; i++)
	{
		var arg = arguments[i];

		if (s) s += "/";

		if (is.String(arg)) s += arg;
		else if (is.ValueType(arg)) s += arg.toString();
		else if (is.Array(arg)) s += '[' + A.join(arg, ",") + ']';
		else
		{
			var o = "";
			for (var name in arg)
			{
				if (o) o += ",";
				o += name + ":" + S.safeString(arg[name]);
			}
			s += '{' + o + '}';
		}
	}
	return s;
}
S.safeString = function(str)
{
	if (!str) return str;
	if (S.containsAny(str, ['[', ']', '{', '}', ',']))
	{
		return '"' + str + '"';
	}
	return str;
}
S.trim = function(str, chars)
{
	return S.ltrim(S.rtrim(str, chars), chars);
}
S.ltrim = function(str, chars)
{
	chars = chars || "\\s";
	return str.replace(new RegExp("^[" + chars + "]+", "g"), "");
}
S.rtrim = function(str, chars)
{
	chars = chars || "\\s";
	return str.replace(new RegExp("[" + chars + "]+$", "g"), "");
}
S.contains = function(str, test)
{
    if (!is.String(str)) return;
    return str.indexOf(test) != -1;
}
S.containsAny = function(str, tests)
{
    if (!is.String(str)) return;
	for (var i = 0, len = tests.length; i < len; i++)
	{
		if (str.indexOf(tests[i]) != -1) return true;
	}
	return false;
}
S.r4 = function()
{
	return (((1 + Math.random()) * 0x10000) | 0).toString(16).substring(1);
}
S.createGuid = function()
{
	return (S.r4() + S.r4() + "-" + S.r4() + "-" + S.r4() + "-" + S.r4() + "-" + S.r4() + S.r4() + S.r4());
}
S.strip = function(text, match)
{
	return text.replace(match, "");
}

/**
* Urn.js
* 
* @author Demis Bellot
* @version 1.0
*
* Copyright 2009, Demis Bellot
* http://code.google.com/p/ajaxstack/
*/

function Urn(resource, name, id)
{
	this.resource = resource;
	this.name = id ? name : null;
	this.id = id ? id : name;
}
Urn.log = new Logger("Urn");
p = Urn.prototype;
p.toUrn = function()
{
	return this.name
		? "urn:" + this.resource + ":" + this.name + ":" + this.id
		: "urn:" + this.resource + ":" + this.id;
};
p.getResource = function()
{
	return this.resource;
};
p.getName = function()
{
	return this.name;
};
p.getId = function()
{
	return this.id;
};
Urn.parse = function(urnStr)
{
	if (!urnStr) return null;

	var parts = urnStr.split(":");

	if (parts.length < 3 || parts.length > 4)
	{		
		Urn.log.error("Invalid Urn: " + urnStr);
		return null;
	}

	return parts.length > 2
		? new Urn(parts[1], parts[2], parts[3])
		: new Urn(parts[1], parts[2]);
};
Urn.getResource = function(urn)
{
	var urn = Urn.parse(urn);
	return urn ? urn.resource : null;
};
Urn.getName = function(urn)
{
	var urn = Urn.parse(urn);
	return urn ? urn.name : null;
};
Urn.getId = function(urn)
{
	var urn = Urn.parse(urn);
	return urn ? urn.id : null;
};
Urn.getIdNumber = function(urn)
{
	return parseInt(Urn.getId(urn));
};


function AppBase(tagId, appPages, hostControllers)
{
	AppBase.$baseConstructor.call(this);

	this.tagId = tagId;
	this.config = new Config();
	this.nav = new NavigationController(this, hostControllers);
	this.contexts = [];
	this.controllers = [this.nav];
	this.appPages = appPages;

	if (AppBase.instance)
	{
		this.log.warn('More than one instance of AppBase detected.');
	}
	AppBase.instance = this;

	var $this = this;
	JSN.onSuccess = function(args)
	{
		$this.dispatchEvent("onGatewaySuccess", $this, args);
	};
	JSN.onError = function(args)
	{
		$this.dispatchEvent("onGatewayError", $this, args);
	};
}
p = AppBase.extend(ASObject, { type: 'AjaxStack.AppBase' });
AppBase.getInstance = function()
{
	return AppBase.instance;
}
AppBase.getNav = function()
{
	return AppBase.getInstance().getNav();
}
AppBase.getAppPages = function()
{
	return AppBase.getInstance().getAppPages();
}
AppBase.getPage = function(pageName, args)
{
	return AppBase.getInstance().getPage(pageName, args);
}

p.getTagId = function() { return this.tagId; }
p.getNav = function() { return this.nav; }
p.getConfig = function() { return this.config; }
p.getAppPages = function() { return this.appPages; }
p.getPage = function(pageName, args) { return this.appPages.pages[pageName](args); }
p.getContexts = function() { return this.contexts; }
p.getControllers = function()
{
	var all = [];
	for (var i = 0, len = this.controllers.length; i < len; i++)
	{
		var c = this.controllers[i];
		all.push(c);
		A.merge(all, c.getControllers());
	}
	return all;
}

p.navigateTo = function(path)
{
	var $this = this;
	$(function()
	{
		$this.nav.loadPath(path);
	});
}

//dispatch an event to all contexts and controllers
p.dispatchEvent = function(eventName, source, args)
{
	var allContexts = this.getContexts();
	var allControllers = this.getControllers();

	this.log.debug("dispatchEvent: '" + eventName + "' to " + allContexts.length + " Contexts and " + allControllers.length + " controllers");

	for (var i = 0, len = allContexts.length; i < len; i++)
	{
		this.fireEvent(allContexts[i], eventName, source, args);
	}

	for (var i = 0, len = allControllers.length; i < len; i++)
	{
		this.fireEvent(allControllers[i], eventName, source, args);
	}
}

p.fireEvent = function(target, eventName, source, args)
{
	if (target[eventName])
	{
		this.log.debug("fireEvent(): " + eventName + " on " + target);
		target[eventName](source, args);
	}
};



function Config()
{
	this.hostName = Path.getHostName();
	O.merge(this, Path.parseQueryString());
}

})();if (window.onAjaxStackScriptLoad) window.onAjaxStackScriptLoad();
