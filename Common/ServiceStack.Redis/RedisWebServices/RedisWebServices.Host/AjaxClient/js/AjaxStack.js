/*
 * jQuery JavaScript Library v1.4.2
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
 * Date: Sat Feb 13 22:33:48 2010 -0500
 */
(function(aO,I){function a0(){if(!ah.isReady){try{M.documentElement.doScroll("left")}catch(c){setTimeout(a0,1);return}ah.ready()}}function E(s,c){c.src?ah.ajax({url:c.src,async:false,dataType:"script"}):ah.globalEval(c.text||c.textContent||c.innerHTML||"");c.parentNode&&c.parentNode.removeChild(c)}function ap(s,c,K,F,G,w){var A=s.length;if(typeof c==="object"){for(var J in c){ap(s,J,c[J],F,G,K)}return s}if(K!==I){F=!w&&F&&ah.isFunction(K);for(J=0;J<A;J++){G(s[J],c,F?K.call(s[J],J,G(s[J],c)):K,w)}return s}return A?G(s[0],c):I}function aF(){return(new Date).getTime()}function ao(){return false}function am(){return true}function aK(s,c,w){w[0].type=s;return ah.event.handle.apply(c,w)}function ag(O){var N,L=[],J=[],K=arguments,F,G,s,A,w,c;G=ah.data(this,"events");if(!(O.liveFired===this||!G||!G.live||O.button&&O.type==="click")){O.liveFired=this;var P=G.live.slice(0);for(A=0;A<P.length;A++){G=P[A];G.origType.replace(az,"")===O.type?J.push(G.selector):P.splice(A--,1)}F=ah(O.target).closest(J,O.currentTarget);w=0;for(c=F.length;w<c;w++){for(A=0;A<P.length;A++){G=P[A];if(F[w].selector===G.selector){s=F[w].elem;J=null;if(G.preType==="mouseenter"||G.preType==="mouseleave"){J=ah(O.relatedTarget).closest(G.selector)[0]}if(!J||J!==s){L.push({elem:s,handleObj:G})}}}}w=0;for(c=L.length;w<c;w++){F=L[w];O.currentTarget=F.elem;O.data=F.handleObj.data;O.handleObj=F.handleObj;if(F.handleObj.origHandler.apply(F.elem,K)===false){N=false;break}}return N}}function z(s,c){return"live."+(s&&s!=="*"?s+".":"")+c.replace(/\./g,"`").replace(/ /g,"&")}function l(c){return !c||!c.parentNode||c.parentNode.nodeType===11}function bj(s,c){var w=0;c.each(function(){if(this.nodeName===(s[w]&&s[w].nodeName)){var G=ah.data(s[w++]),J=ah.data(this,G);if(G=G&&G.events){delete J.handle;J.events={};for(var A in G){for(var F in G[A]){ah.event.add(this,A,G[A][F],G[A][F].data)}}}}})}function a3(s,c,G){var A,F,w;c=c&&c[0]?c[0].ownerDocument||c[0]:M;if(s.length===1&&typeof s[0]==="string"&&s[0].length<512&&c===M&&!aP.test(s[0])&&(ah.support.checkClone||!ak.test(s[0]))){F=true;if(w=ah.fragments[s[0]]){if(w!==1){A=w}}}if(!A){A=c.createDocumentFragment();ah.clean(s,c,A,G)}if(F){ah.fragments[s[0]]=w?A:1}return{fragment:A,cacheable:F}}function aC(s,c){var w={};ah.each(D.concat.apply([],D.slice(0,c)),function(){w[this]=s});return w}function o(c){return"scrollTo" in c&&c.document?c:c.nodeType===9?c.defaultView||c.parentWindow:false}var ah=function(s,c){return new ah.fn.init(s,c)},p=aO.jQuery,d=aO.$,M=aO.document,at,a7=/^[^<]*(<[\w\W]+>)[^>]*$|^#([\w-]+)$/,aT=/^.[^:#\[\.,]*$/,an=/\S/,H=/^(\s|\u00A0)+|(\s|\u00A0)+$/g,q=/^<(\w+)\s*\/?>(?:<\/\1>)?$/,ax=navigator.userAgent,b=false,av=[],aB,a1=Object.prototype.toString,aV=Object.prototype.hasOwnProperty,ay=Array.prototype.push,au=Array.prototype.slice,a6=Array.prototype.indexOf;ah.fn=ah.prototype={init:function(s,c){var A,w;if(!s){return this}if(s.nodeType){this.context=this[0]=s;this.length=1;return this}if(s==="body"&&!c){this.context=M;this[0]=M.body;this.selector="body";this.length=1;return this}if(typeof s==="string"){if((A=a7.exec(s))&&(A[1]||!c)){if(A[1]){w=c?c.ownerDocument||c:M;if(s=q.exec(s)){if(ah.isPlainObject(c)){s=[M.createElement(s[1])];ah.fn.attr.call(s,c,true)}else{s=[w.createElement(s[1])]}}else{s=a3([A[1]],[w]);s=(s.cacheable?s.fragment.cloneNode(true):s.fragment).childNodes}return ah.merge(this,s)}else{if(c=M.getElementById(A[2])){if(c.id!==A[2]){return at.find(s)}this.length=1;this[0]=c}this.context=M;this.selector=s;return this}}else{if(!c&&/^\w+$/.test(s)){this.selector=s;this.context=M;s=M.getElementsByTagName(s);return ah.merge(this,s)}else{return !c||c.jquery?(c||at).find(s):ah(c).find(s)}}}else{if(ah.isFunction(s)){return at.ready(s)}}if(s.selector!==I){this.selector=s.selector;this.context=s.context}return ah.makeArray(s,this)},selector:"",jquery:"1.4.2",length:0,size:function(){return this.length},toArray:function(){return au.call(this,0)},get:function(c){return c==null?this.toArray():c<0?this.slice(c)[0]:this[c]},pushStack:function(s,c,A){var w=ah();ah.isArray(s)?ay.apply(w,s):ah.merge(w,s);w.prevObject=this;w.context=this.context;if(c==="find"){w.selector=this.selector+(this.selector?" ":"")+A}else{if(c){w.selector=this.selector+"."+c+"("+A+")"}}return w},each:function(s,c){return ah.each(this,s,c)},ready:function(c){ah.bindReady();if(ah.isReady){c.call(M,ah)}else{av&&av.push(c)}return this},eq:function(c){return c===-1?this.slice(c):this.slice(c,+c+1)},first:function(){return this.eq(0)},last:function(){return this.eq(-1)},slice:function(){return this.pushStack(au.apply(this,arguments),"slice",au.call(arguments).join(","))},map:function(c){return this.pushStack(ah.map(this,function(s,w){return c.call(s,w,s)}))},end:function(){return this.prevObject||ah(null)},push:ay,sort:[].sort,splice:[].splice};ah.fn.init.prototype=ah.fn;ah.extend=ah.fn.extend=function(){var s=arguments[0]||{},c=1,K=arguments.length,F=false,G,w,A,J;if(typeof s==="boolean"){F=s;s=arguments[1]||{};c=2}if(typeof s!=="object"&&!ah.isFunction(s)){s={}}if(K===c){s=this;--c}for(;c<K;c++){if((G=arguments[c])!=null){for(w in G){A=s[w];J=G[w];if(s!==J){if(F&&J&&(ah.isPlainObject(J)||ah.isArray(J))){A=A&&(ah.isPlainObject(A)||ah.isArray(A))?A:ah.isArray(J)?[]:{};s[w]=ah.extend(F,A,J)}else{if(J!==I){s[w]=J}}}}}}return s};ah.extend({noConflict:function(c){aO.$=d;if(c){aO.jQuery=p}return ah},isReady:false,ready:function(){if(!ah.isReady){if(!M.body){return setTimeout(ah.ready,13)}ah.isReady=true;if(av){for(var s,c=0;s=av[c++];){s.call(M,ah)}av=null}ah.fn.triggerHandler&&ah(M).triggerHandler("ready")}},bindReady:function(){if(!b){b=true;if(M.readyState==="complete"){return ah.ready()}if(M.addEventListener){M.addEventListener("DOMContentLoaded",aB,false);aO.addEventListener("load",ah.ready,false)}else{if(M.attachEvent){M.attachEvent("onreadystatechange",aB);aO.attachEvent("onload",ah.ready);var s=false;try{s=aO.frameElement==null}catch(c){}M.documentElement.doScroll&&s&&a0()}}}},isFunction:function(c){return a1.call(c)==="[object Function]"},isArray:function(c){return a1.call(c)==="[object Array]"},isPlainObject:function(s){if(!s||a1.call(s)!=="[object Object]"||s.nodeType||s.setInterval){return false}if(s.constructor&&!aV.call(s,"constructor")&&!aV.call(s.constructor.prototype,"isPrototypeOf")){return false}var c;for(c in s){}return c===I||aV.call(s,c)},isEmptyObject:function(s){for(var c in s){return false}return true},error:function(c){throw c},parseJSON:function(c){if(typeof c!=="string"||!c){return null}c=ah.trim(c);if(/^[\],:{}\s]*$/.test(c.replace(/\\(?:["\\\/bfnrt]|u[0-9a-fA-F]{4})/g,"@").replace(/"[^"\\\n\r]*"|true|false|null|-?\d+(?:\.\d*)?(?:[eE][+\-]?\d+)?/g,"]").replace(/(?:^|:|,)(?:\s*\[)+/g,""))){return aO.JSON&&aO.JSON.parse?aO.JSON.parse(c):(new Function("return "+c))()}else{ah.error("Invalid JSON: "+c)}},noop:function(){},globalEval:function(s){if(s&&an.test(s)){var c=M.getElementsByTagName("head")[0]||M.documentElement,w=M.createElement("script");w.type="text/javascript";if(ah.support.scriptEval){w.appendChild(M.createTextNode(s))}else{w.text=s}c.insertBefore(w,c.firstChild);c.removeChild(w)}},nodeName:function(s,c){return s.nodeName&&s.nodeName.toUpperCase()===c.toUpperCase()},each:function(s,c,J){var F,G=0,w=s.length,A=w===I||ah.isFunction(s);if(J){if(A){for(F in s){if(c.apply(s[F],J)===false){break}}}else{for(;G<w;){if(c.apply(s[G++],J)===false){break}}}}else{if(A){for(F in s){if(c.call(s[F],F,s[F])===false){break}}}else{for(J=s[0];G<w&&c.call(J,G,J)!==false;J=s[++G]){}}}return s},trim:function(c){return(c||"").replace(H,"")},makeArray:function(s,c){c=c||[];if(s!=null){s.length==null||typeof s==="string"||ah.isFunction(s)||typeof s!=="function"&&s.setInterval?ay.call(c,s):ah.merge(c,s)}return c},inArray:function(s,c){if(c.indexOf){return c.indexOf(s)}for(var A=0,w=c.length;A<w;A++){if(c[A]===s){return A}}return -1},merge:function(s,c){var F=s.length,w=0;if(typeof c.length==="number"){for(var A=c.length;w<A;w++){s[F++]=c[w]}}else{for(;c[w]!==I;){s[F++]=c[w++]}}s.length=F;return s},grep:function(s,c,G){for(var A=[],F=0,w=s.length;F<w;F++){!G!==!c(s[F],F)&&A.push(s[F])}return A},map:function(s,c,J){for(var F=[],G,w=0,A=s.length;w<A;w++){G=c(s[w],w,J);if(G!=null){F[F.length]=G}}return F.concat.apply([],F)},guid:1,proxy:function(s,c,w){if(arguments.length===2){if(typeof c==="string"){w=s;s=w[c];c=I}else{if(c&&!ah.isFunction(c)){w=c;c=I}}}if(!c&&s){c=function(){return s.apply(w||this,arguments)}}if(s){c.guid=s.guid=s.guid||c.guid||ah.guid++}return c},uaMatch:function(c){c=c.toLowerCase();c=/(webkit)[ \/]([\w.]+)/.exec(c)||/(opera)(?:.*version)?[ \/]([\w.]+)/.exec(c)||/(msie) ([\w.]+)/.exec(c)||!/compatible/.test(c)&&/(mozilla)(?:.*? rv:([\w.]+))?/.exec(c)||[];return{browser:c[1]||"",version:c[2]||"0"}},browser:{}});ax=ah.uaMatch(ax);if(ax.browser){ah.browser[ax.browser]=true;ah.browser.version=ax.version}if(ah.browser.webkit){ah.browser.safari=true}if(a6){ah.inArray=function(s,c){return a6.call(c,s)}}at=ah(M);if(M.addEventListener){aB=function(){M.removeEventListener("DOMContentLoaded",aB,false);ah.ready()}}else{if(M.attachEvent){aB=function(){if(M.readyState==="complete"){M.detachEvent("onreadystatechange",aB);ah.ready()}}}}(function(){ah.support={};var L=M.documentElement,K=M.createElement("script"),J=M.createElement("div"),F="script"+aF();J.style.display="none";J.innerHTML="   <link/><table></table><a href='/a' style='color:red;float:left;opacity:.55;'>a</a><input type='checkbox'/>";var G=J.getElementsByTagName("*"),w=J.getElementsByTagName("a")[0];if(!(!G||!G.length||!w)){ah.support={leadingWhitespace:J.firstChild.nodeType===3,tbody:!J.getElementsByTagName("tbody").length,htmlSerialize:!!J.getElementsByTagName("link").length,style:/red/.test(w.getAttribute("style")),hrefNormalized:w.getAttribute("href")==="/a",opacity:/^0.55$/.test(w.style.opacity),cssFloat:!!w.style.cssFloat,checkOn:J.getElementsByTagName("input")[0].value==="on",optSelected:M.createElement("select").appendChild(M.createElement("option")).selected,parentNode:J.removeChild(J.appendChild(M.createElement("div"))).parentNode===null,deleteExpando:true,checkClone:false,scriptEval:false,noCloneEvent:true,boxModel:null};K.type="text/javascript";try{K.appendChild(M.createTextNode("window."+F+"=1;"))}catch(A){}L.insertBefore(K,L.firstChild);if(aO[F]){ah.support.scriptEval=true;delete aO[F]}try{delete K.test}catch(c){ah.support.deleteExpando=false}L.removeChild(K);if(J.attachEvent&&J.fireEvent){J.attachEvent("onclick",function s(){ah.support.noCloneEvent=false;J.detachEvent("onclick",s)});J.cloneNode(true).fireEvent("onclick")}J=M.createElement("div");J.innerHTML="<input type='radio' name='radiotest' checked='checked'/>";L=M.createDocumentFragment();L.appendChild(J.firstChild);ah.support.checkClone=L.cloneNode(true).cloneNode(true).lastChild.checked;ah(function(){var N=M.createElement("div");N.style.width=N.style.paddingLeft="1px";M.body.appendChild(N);ah.boxModel=ah.support.boxModel=N.offsetWidth===2;M.body.removeChild(N).style.display="none"});L=function(N){var P=M.createElement("div");N="on"+N;var O=N in P;if(!O){P.setAttribute(N,"return;");O=typeof P[N]==="function"}return O};ah.support.submitBubbles=L("submit");ah.support.changeBubbles=L("change");L=K=J=G=w=null}})();ah.props={"for":"htmlFor","class":"className",readonly:"readOnly",maxlength:"maxLength",cellspacing:"cellSpacing",rowspan:"rowSpan",colspan:"colSpan",tabindex:"tabIndex",usemap:"useMap",frameborder:"frameBorder"};var aH="jQuery"+aF(),e=0,aS={};ah.extend({cache:{},expando:aH,noData:{embed:true,object:true,applet:true},data:function(s,c,F){if(!(s.nodeName&&ah.noData[s.nodeName.toLowerCase()])){s=s==aO?aS:s;var w=s[aH],A=ah.cache;if(!w&&typeof c==="string"&&F===I){return null}w||(w=++e);if(typeof c==="object"){s[aH]=w;A[w]=ah.extend(true,{},c)}else{if(!A[w]){s[aH]=w;A[w]={}}}s=A[w];if(F!==I){s[c]=F}return typeof c==="string"?s[c]:s}},removeData:function(s,c){if(!(s.nodeName&&ah.noData[s.nodeName.toLowerCase()])){s=s==aO?aS:s;var F=s[aH],w=ah.cache,A=w[F];if(c){if(A){delete A[c];ah.isEmptyObject(A)&&ah.removeData(s)}}else{if(ah.support.deleteExpando){delete s[ah.expando]}else{s.removeAttribute&&s.removeAttribute(ah.expando)}delete w[F]}}}});ah.fn.extend({data:function(s,c){if(typeof s==="undefined"&&this.length){return ah.data(this[0])}else{if(typeof s==="object"){return this.each(function(){ah.data(this,s)})}}var A=s.split(".");A[1]=A[1]?"."+A[1]:"";if(c===I){var w=this.triggerHandler("getData"+A[1]+"!",[A[0]]);if(w===I&&this.length){w=ah.data(this[0],s)}return w===I&&A[1]?this.data(A[0]):w}else{return this.trigger("setData"+A[1]+"!",[A[0],c]).each(function(){ah.data(this,s,c)})}},removeData:function(c){return this.each(function(){ah.removeData(this,c)})}});ah.extend({queue:function(s,c,A){if(s){c=(c||"fx")+"queue";var w=ah.data(s,c);if(!A){return w||[]}if(!w||ah.isArray(A)){w=ah.data(s,c,ah.makeArray(A))}else{w.push(A)}return w}},dequeue:function(s,c){c=c||"fx";var A=ah.queue(s,c),w=A.shift();if(w==="inprogress"){w=A.shift()}if(w){c==="fx"&&A.unshift("inprogress");w.call(s,function(){ah.dequeue(s,c)})}}});ah.fn.extend({queue:function(s,c){if(typeof s!=="string"){c=s;s="fx"}if(c===I){return ah.queue(this[0],s)}return this.each(function(){var w=ah.queue(this,s,c);s==="fx"&&w[0]!=="inprogress"&&ah.dequeue(this,s)})},dequeue:function(c){return this.each(function(){ah.dequeue(this,c)})},delay:function(s,c){s=ah.fx?ah.fx.speeds[s]||s:s;c=c||"fx";return this.queue(c,function(){var w=this;setTimeout(function(){ah.dequeue(w,c)},s)})},clearQueue:function(c){return this.queue(c||"fx",[])}});var be=/[\n\t]/g,U=/\s+/,a8=/\r/g,aM=/href|src|style/,aU=/(button|input)/i,aw=/(button|input|object|select|textarea)/i,S=/^(a|area)$/i,aY=/radio|checkbox/;ah.fn.extend({attr:function(s,c){return ap(this,s,c,true,ah.attr)},removeAttr:function(c){return this.each(function(){ah.attr(this,c,"");this.nodeType===1&&this.removeAttribute(c)})},addClass:function(L){if(ah.isFunction(L)){return this.each(function(O){var N=ah(this);N.addClass(L.call(this,O,N.attr("class")))})}if(L&&typeof L==="string"){for(var K=(L||"").split(U),J=0,F=this.length;J<F;J++){var G=this[J];if(G.nodeType===1){if(G.className){for(var w=" "+G.className+" ",A=G.className,c=0,s=K.length;c<s;c++){if(w.indexOf(" "+K[c]+" ")<0){A+=" "+K[c]}}G.className=ah.trim(A)}else{G.className=L}}}}return this},removeClass:function(s){if(ah.isFunction(s)){return this.each(function(L){var N=ah(this);N.removeClass(s.call(this,L,N.attr("class")))})}if(s&&typeof s==="string"||s===I){for(var c=(s||"").split(U),K=0,F=this.length;K<F;K++){var G=this[K];if(G.nodeType===1&&G.className){if(s){for(var w=(" "+G.className+" ").replace(be," "),A=0,J=c.length;A<J;A++){w=w.replace(" "+c[A]+" "," ")}G.className=ah.trim(w)}else{G.className=""}}}}return this},toggleClass:function(s,c){var A=typeof s,w=typeof c==="boolean";if(ah.isFunction(s)){return this.each(function(G){var F=ah(this);F.toggleClass(s.call(this,G,F.attr("class"),c),c)})}return this.each(function(){if(A==="string"){for(var K,G=0,J=ah(this),L=c,F=s.split(U);K=F[G++];){L=w?L:!J.hasClass(K);J[L?"addClass":"removeClass"](K)}}else{if(A==="undefined"||A==="boolean"){this.className&&ah.data(this,"__className__",this.className);this.className=this.className||s===false?"":ah.data(this,"__className__")||""}}})},hasClass:function(s){s=" "+s+" ";for(var c=0,w=this.length;c<w;c++){if((" "+this[c].className+" ").replace(be," ").indexOf(s)>-1){return true}}return false},val:function(s){if(s===I){var c=this[0];if(c){if(ah.nodeName(c,"option")){return(c.attributes.value||{}).specified?c.value:c.text}if(ah.nodeName(c,"select")){var K=c.selectedIndex,F=[],G=c.options;c=c.type==="select-one";if(K<0){return null}var w=c?K:0;for(K=c?K+1:G.length;w<K;w++){var A=G[w];if(A.selected){s=ah(A).val();if(c){return s}F.push(s)}}return F}if(aY.test(c.type)&&!ah.support.checkOn){return c.getAttribute("value")===null?"on":c.value}return(c.value||"").replace(a8,"")}return I}var J=ah.isFunction(s);return this.each(function(L){var P=ah(this),O=s;if(this.nodeType===1){if(J){O=s.call(this,L,P.val())}if(typeof O==="number"){O+=""}if(ah.isArray(O)&&aY.test(this.type)){this.checked=ah.inArray(P.val(),O)>=0}else{if(ah.nodeName(this,"select")){var N=ah.makeArray(O);ah("option",this).each(function(){this.selected=ah.inArray(ah(this).val(),N)>=0});if(!N.length){this.selectedIndex=-1}}else{this.value=O}}}})}});ah.extend({attrFn:{val:true,css:true,html:true,text:true,data:true,width:true,height:true,offset:true},attr:function(s,c,G,A){if(!s||s.nodeType===3||s.nodeType===8){return I}if(A&&c in ah.attrFn){return ah(s)[c](G)}A=s.nodeType!==1||!ah.isXMLDoc(s);var F=G!==I;c=A&&ah.props[c]||c;if(s.nodeType===1){var w=aM.test(c);if(c in s&&A&&!w){if(F){c==="type"&&aU.test(s.nodeName)&&s.parentNode&&ah.error("type property can't be changed");s[c]=G}if(ah.nodeName(s,"form")&&s.getAttributeNode(c)){return s.getAttributeNode(c).nodeValue}if(c==="tabIndex"){return(c=s.getAttributeNode("tabIndex"))&&c.specified?c.value:aw.test(s.nodeName)||S.test(s.nodeName)&&s.href?0:I}return s[c]}if(!ah.support.style&&A&&c==="style"){if(F){s.style.cssText=""+G}return s.style.cssText}F&&s.setAttribute(c,""+G);s=!ah.support.hrefNormalized&&A&&w?s.getAttribute(c,2):s.getAttribute(c);return s===null?I:s}return ah.style(s,c,G)}});var az=/\.(.*)$/,r=function(c){return c.replace(/[^\w\s\.\|`]/g,function(s){return"\\"+s})};ah.event={add:function(P,O,L,J){if(!(P.nodeType===3||P.nodeType===8)){if(P.setInterval&&P!==aO&&!P.frameElement){P=aO}var K,F;if(L.handler){K=L;L=K.handler}if(!L.guid){L.guid=ah.guid++}if(F=ah.data(P)){var G=F.events=F.events||{},s=F.handle;if(!s){F.handle=s=function(){return typeof ah!=="undefined"&&!ah.event.triggered?ah.event.handle.apply(s.elem,arguments):I}}s.elem=P;O=O.split(" ");for(var A,w=0,c;A=O[w++];){F=K?ah.extend({},K):{handler:L,data:J};if(A.indexOf(".")>-1){c=A.split(".");A=c.shift();F.namespace=c.slice(0).sort().join(".")}else{c=[];F.namespace=""}F.type=A;F.guid=L.guid;var Q=G[A],N=ah.event.special[A]||{};if(!Q){Q=G[A]=[];if(!N.setup||N.setup.call(P,J,c,s)===false){if(P.addEventListener){P.addEventListener(A,s,false)}else{P.attachEvent&&P.attachEvent("on"+A,s)}}}if(N.add){N.add.call(P,F);if(!F.handler.guid){F.handler.guid=L.guid}}Q.push(F);ah.event.global[A]=true}P=null}}},global:{},remove:function(R,Q,O,L){if(!(R.nodeType===3||R.nodeType===8)){var N,J=0,K,A,G,F,c,T,P=ah.data(R),s=P&&P.events;if(P&&s){if(Q&&Q.type){O=Q.handler;Q=Q.type}if(!Q||typeof Q==="string"&&Q.charAt(0)==="."){Q=Q||"";for(N in s){ah.event.remove(R,N+Q)}}else{for(Q=Q.split(" ");N=Q[J++];){F=N;K=N.indexOf(".")<0;A=[];if(!K){A=N.split(".");N=A.shift();G=new RegExp("(^|\\.)"+ah.map(A.slice(0).sort(),r).join("\\.(?:.*\\.)?")+"(\\.|$)")}if(c=s[N]){if(O){F=ah.event.special[N]||{};for(w=L||0;w<c.length;w++){T=c[w];if(O.guid===T.guid){if(K||G.test(T.namespace)){L==null&&c.splice(w--,1);F.remove&&F.remove.call(R,T)}if(L!=null){break}}}if(c.length===0||L!=null&&c.length===1){if(!F.teardown||F.teardown.call(R,A)===false){aG(R,N,P.handle)}delete s[N]}}else{for(var w=0;w<c.length;w++){T=c[w];if(K||G.test(T.namespace)){ah.event.remove(R,F,T.handler,w);c.splice(w--,1)}}}}}if(ah.isEmptyObject(s)){if(Q=P.handle){Q.elem=null}delete P.events;delete P.handle;ah.isEmptyObject(P)&&ah.removeData(R)}}}}},trigger:function(N,L,K,G){var J=N.type||N;if(!G){N=typeof N==="object"?N[aH]?N:ah.extend(ah.Event(J),N):ah.Event(J);if(J.indexOf("!")>=0){N.type=J=J.slice(0,-1);N.exclusive=true}if(!K){N.stopPropagation();ah.event.global[J]&&ah.each(ah.cache,function(){this.events&&this.events[J]&&ah.event.trigger(N,L,this.handle.elem)})}if(!K||K.nodeType===3||K.nodeType===8){return I}N.result=I;N.target=K;L=ah.makeArray(L);L.unshift(N)}N.currentTarget=K;(G=ah.data(K,"handle"))&&G.apply(K,L);G=K.parentNode||K.ownerDocument;try{if(!(K&&K.nodeName&&ah.noData[K.nodeName.toLowerCase()])){if(K["on"+J]&&K["on"+J].apply(K,L)===false){N.result=false}}}catch(A){}if(!N.isPropagationStopped()&&G){ah.event.trigger(N,L,G,true)}else{if(!N.isDefaultPrevented()){G=N.target;var F,c=ah.nodeName(G,"a")&&J==="click",w=ah.event.special[J]||{};if((!w._default||w._default.call(K,N)===false)&&!c&&!(G&&G.nodeName&&ah.noData[G.nodeName.toLowerCase()])){try{if(G[J]){if(F=G["on"+J]){G["on"+J]=null}ah.event.triggered=true;G[J]()}}catch(s){}if(F){G["on"+J]=F}ah.event.triggered=false}}}},handle:function(s){var c,J,F,G;s=arguments[0]=ah.event.fix(s||aO.event);s.currentTarget=this;c=s.type.indexOf(".")<0&&!s.exclusive;if(!c){J=s.type.split(".");s.type=J.shift();F=new RegExp("(^|\\.)"+J.slice(0).sort().join("\\.(?:.*\\.)?")+"(\\.|$)")}G=ah.data(this,"events");J=G[s.type];if(G&&J){J=J.slice(0);G=0;for(var w=J.length;G<w;G++){var A=J[G];if(c||F.test(A.namespace)){s.handler=A.handler;s.data=A.data;s.handleObj=A;A=A.handler.apply(this,arguments);if(A!==I){s.result=A;if(A===false){s.preventDefault();s.stopPropagation()}}if(s.isImmediatePropagationStopped()){break}}}}return s.result},props:"altKey attrChange attrName bubbles button cancelable charCode clientX clientY ctrlKey currentTarget data detail eventPhase fromElement handler keyCode layerX layerY metaKey newValue offsetX offsetY originalTarget pageX pageY prevValue relatedNode relatedTarget screenX screenY shiftKey srcElement target toElement view wheelDelta which".split(" "),fix:function(s){if(s[aH]){return s}var c=s;s=ah.Event(c);for(var A=this.props.length,w;A;){w=this.props[--A];s[w]=c[w]}if(!s.target){s.target=s.srcElement||M}if(s.target.nodeType===3){s.target=s.target.parentNode}if(!s.relatedTarget&&s.fromElement){s.relatedTarget=s.fromElement===s.target?s.toElement:s.fromElement}if(s.pageX==null&&s.clientX!=null){c=M.documentElement;A=M.body;s.pageX=s.clientX+(c&&c.scrollLeft||A&&A.scrollLeft||0)-(c&&c.clientLeft||A&&A.clientLeft||0);s.pageY=s.clientY+(c&&c.scrollTop||A&&A.scrollTop||0)-(c&&c.clientTop||A&&A.clientTop||0)}if(!s.which&&(s.charCode||s.charCode===0?s.charCode:s.keyCode)){s.which=s.charCode||s.keyCode}if(!s.metaKey&&s.ctrlKey){s.metaKey=s.ctrlKey}if(!s.which&&s.button!==I){s.which=s.button&1?1:s.button&2?3:s.button&4?2:0}return s},guid:100000000,proxy:ah.proxy,special:{ready:{setup:ah.bindReady,teardown:ah.noop},live:{add:function(c){ah.event.add(this,c.origType,ah.extend({},c,{handler:ag}))},remove:function(s){var c=true,w=s.origType.replace(az,"");ah.each(ah.data(this,"events").live||[],function(){if(w===this.origType.replace(az,"")){return c=false}});c&&ah.event.remove(this,s.origType,ag)}},beforeunload:{setup:function(s,c,w){if(this.setInterval){this.onbeforeunload=w}return false},teardown:function(s,c){if(this.onbeforeunload===c){this.onbeforeunload=null}}}}};var aG=M.removeEventListener?function(s,c,w){s.removeEventListener(c,w,false)}:function(s,c,w){s.detachEvent("on"+c,w)};ah.Event=function(c){if(!this.preventDefault){return new ah.Event(c)}if(c&&c.type){this.originalEvent=c;this.type=c.type}else{this.type=c}this.timeStamp=aF();this[aH]=true};ah.Event.prototype={preventDefault:function(){this.isDefaultPrevented=am;var c=this.originalEvent;if(c){c.preventDefault&&c.preventDefault();c.returnValue=false}},stopPropagation:function(){this.isPropagationStopped=am;var c=this.originalEvent;if(c){c.stopPropagation&&c.stopPropagation();c.cancelBubble=true}},stopImmediatePropagation:function(){this.isImmediatePropagationStopped=am;this.stopPropagation()},isDefaultPrevented:ao,isPropagationStopped:ao,isImmediatePropagationStopped:ao};var ae=function(s){var c=s.relatedTarget;try{for(;c&&c!==this;){c=c.parentNode}if(c!==this){s.type=s.data;ah.event.handle.apply(this,arguments)}}catch(w){}},x=function(c){c.type=c.data;ah.event.handle.apply(this,arguments)};ah.each({mouseenter:"mouseover",mouseleave:"mouseout"},function(s,c){ah.event.special[s]={setup:function(w){ah.event.add(this,c,w&&w.selector?x:ae,s)},teardown:function(w){ah.event.remove(this,c,w&&w.selector?x:ae)}}});if(!ah.support.submitBubbles){ah.event.special.submit={setup:function(){if(this.nodeName.toLowerCase()!=="form"){ah.event.add(this,"click.specialSubmit",function(s){var c=s.target,w=c.type;if((w==="submit"||w==="image")&&ah(c).closest("form").length){return aK("submit",this,arguments)}});ah.event.add(this,"keypress.specialSubmit",function(s){var c=s.target,w=c.type;if((w==="text"||w==="password")&&ah(c).closest("form").length&&s.keyCode===13){return aK("submit",this,arguments)}})}else{return false}},teardown:function(){ah.event.remove(this,".specialSubmit")}}}if(!ah.support.changeBubbles){var t=/textarea|input|select/i,g,j=function(s){var c=s.type,w=s.value;if(c==="radio"||c==="checkbox"){w=s.checked}else{if(c==="select-multiple"){w=s.selectedIndex>-1?ah.map(s.options,function(A){return A.selected}).join("-"):""}else{if(s.nodeName.toLowerCase()==="select"){w=s.selectedIndex}}}return w},bd=function(s,c){var F=s.target,w,A;if(!(!t.test(F.nodeName)||F.readOnly)){w=ah.data(F,"_change_data");A=j(F);if(s.type!=="focusout"||F.type!=="radio"){ah.data(F,"_change_data",A)}if(!(w===I||A===w)){if(w!=null||A){s.type="change";return ah.event.trigger(s,c,F)}}}};ah.event.special.change={filters:{focusout:bd,click:function(s){var c=s.target,w=c.type;if(w==="radio"||w==="checkbox"||c.nodeName.toLowerCase()==="select"){return bd.call(this,s)}},keydown:function(s){var c=s.target,w=c.type;if(s.keyCode===13&&c.nodeName.toLowerCase()!=="textarea"||s.keyCode===32&&(w==="checkbox"||w==="radio")||w==="select-multiple"){return bd.call(this,s)}},beforeactivate:function(c){c=c.target;ah.data(c,"_change_data",j(c))}},setup:function(){if(this.type==="file"){return false}for(var c in g){ah.event.add(this,c+".specialChange",g[c])}return t.test(this.nodeName)},teardown:function(){ah.event.remove(this,".specialChange");return t.test(this.nodeName)}};g=ah.event.special.change.filters}M.addEventListener&&ah.each({focus:"focusin",blur:"focusout"},function(s,c){function w(A){A=ah.event.fix(A);A.type=c;return ah.event.handle.call(this,A)}ah.event.special[c]={setup:function(){this.addEventListener(s,w,true)},teardown:function(){this.removeEventListener(s,w,true)}}});ah.each(["bind","one"],function(s,c){ah.fn[c]=function(K,F,G){if(typeof K==="object"){for(var w in K){this[c](w,F,K[w],G)}return this}if(ah.isFunction(F)){G=F;F=I}var A=c==="one"?ah.proxy(G,function(L){ah(this).unbind(L,A);return G.apply(this,arguments)}):G;if(K==="unload"&&c!=="one"){this.one(K,F,G)}else{w=0;for(var J=this.length;w<J;w++){ah.event.add(this[w],K,A,F)}}return this}});ah.fn.extend({unbind:function(s,c){if(typeof s==="object"&&!s.preventDefault){for(var A in s){this.unbind(A,s[A])}}else{A=0;for(var w=this.length;A<w;A++){ah.event.remove(this[A],s,c)}}return this},delegate:function(s,c,A,w){return this.live(c,A,w,s)},undelegate:function(s,c,w){return arguments.length===0?this.unbind("live"):this.die(c,null,w,s)},trigger:function(s,c){return this.each(function(){ah.event.trigger(s,c,this)})},triggerHandler:function(s,c){if(this[0]){s=ah.Event(s);s.preventDefault();s.stopPropagation();ah.event.trigger(s,c,this[0]);return s.result}},toggle:function(s){for(var c=arguments,w=1;w<c.length;){ah.proxy(s,c[w++])}return this.click(ah.proxy(s,function(A){var F=(ah.data(this,"lastToggle"+s.guid)||0)%w;ah.data(this,"lastToggle"+s.guid,F+1);A.preventDefault();return c[F].apply(this,arguments)||false}))},hover:function(s,c){return this.mouseenter(s).mouseleave(c||s)}});var bh={focus:"focusin",blur:"focusout",mouseenter:"mouseover",mouseleave:"mouseout"};ah.each(["live","die"],function(s,c){ah.fn[c]=function(O,L,N,J){var K,A=0,G,F,w=J||this.selector,P=J?this:ah(this.context);if(ah.isFunction(L)){N=L;L=I}for(O=(O||"").split(" ");(K=O[A++])!=null;){J=az.exec(K);G="";if(J){G=J[0];K=K.replace(az,"")}if(K==="hover"){O.push("mouseenter"+G,"mouseleave"+G)}else{F=K;if(K==="focus"||K==="blur"){O.push(bh[K]+G);K+=G}else{K=(bh[K]||K)+G}c==="live"?P.each(function(){ah.event.add(this,z(K,w),{data:L,selector:w,handler:N,origType:K,origHandler:N,preType:F})}):P.unbind(z(K,w),N)}}return this}});ah.each("blur focus focusin focusout load resize scroll unload click dblclick mousedown mouseup mousemove mouseover mouseout mouseenter mouseleave change select submit keydown keypress keyup error".split(" "),function(s,c){ah.fn[c]=function(w){return w?this.bind(c,w):this.trigger(c)};if(ah.attrFn){ah.attrFn[c]=true}});aO.attachEvent&&!aO.addEventListener&&aO.attachEvent("onunload",function(){for(var s in ah.cache){if(ah.cache[s].handle){try{ah.event.remove(ah.cache[s].handle.elem)}catch(c){}}}});(function(){function W(ab){for(var aa="",Z,Y=0;ab[Y];Y++){Z=ab[Y];if(Z.nodeType===3||Z.nodeType===4){aa+=Z.nodeValue}else{if(Z.nodeType!==8){aa+=W(Z.childNodes)}}}return aa}function V(bb,ba,ab,aa,Y,Z){Y=0;for(var bm=aa.length;Y<bm;Y++){var bn=aa[Y];if(bn){bn=bn[bb];for(var bl=false;bn;){if(bn.sizcache===ab){bl=aa[bn.sizset];break}if(bn.nodeType===1&&!Z){bn.sizcache=ab;bn.sizset=Y}if(bn.nodeName.toLowerCase()===ba){bl=bn;break}bn=bn[bb]}aa[Y]=bl}}}function T(bb,ba,ab,aa,Y,Z){Y=0;for(var bm=aa.length;Y<bm;Y++){var bn=aa[Y];if(bn){bn=bn[bb];for(var bl=false;bn;){if(bn.sizcache===ab){bl=aa[bn.sizset];break}if(bn.nodeType===1){if(!Z){bn.sizcache=ab;bn.sizset=Y}if(typeof ba!=="string"){if(bn===ba){bl=true;break}}else{if(N.filter(ba,[bn]).length>0){bl=bn;break}}}bn=bn[bb]}aa[Y]=bl}}}var Q=/((?:\((?:\([^()]+\)|[^()]+)+\)|\[(?:\[[^[\]]*\]|['"][^'"]*['"]|[^[\]'"]+)+\]|\\.|[^ >+~,(\[\\]+)+|[>+~])(\s*,\s*)?((?:.|\r|\n)*)/g,R=0,O=Object.prototype.toString,P=false,K=true;[0,0].sort(function(){K=false;return 0});var N=function(bm,bl,ba,ab){ba=ba||[];var Z=bl=bl||M;if(bl.nodeType!==1&&bl.nodeType!==9){return[]}if(!bm||typeof bm!=="string"){return ba}for(var aa=[],br,bs,bo,bb,bq=true,bn=s(bl),bp=bm;(Q.exec(""),br=Q.exec(bp))!==null;){bp=br[3];aa.push(br[1]);if(br[2]){bb=br[3];break}}if(aa.length>1&&G.exec(bm)){if(aa.length===2&&L.relative[aa[0]]){bs=X(aa[0]+aa[1],bl)}else{for(bs=L.relative[aa[0]]?[bl]:N(aa.shift(),bl);aa.length;){bm=aa.shift();if(L.relative[bm]){bm+=aa.shift()}bs=X(bm,bs)}}}else{if(!ab&&aa.length>1&&bl.nodeType===9&&!bn&&L.match.ID.test(aa[0])&&!L.match.ID.test(aa[aa.length-1])){br=N.find(aa.shift(),bl,bn);bl=br.expr?N.filter(br.expr,br.set)[0]:br.set[0]}if(bl){br=ab?{expr:aa.pop(),set:c(ab)}:N.find(aa.pop(),aa.length===1&&(aa[0]==="~"||aa[0]==="+")&&bl.parentNode?bl.parentNode:bl,bn);bs=br.expr?N.filter(br.expr,br.set):br.set;if(aa.length>0){bo=c(bs)}else{bq=false}for(;aa.length;){var Y=aa.pop();br=Y;if(L.relative[Y]){br=aa.pop()}else{Y=""}if(br==null){br=bl}L.relative[Y](bo,br,bn)}}else{bo=[]}}bo||(bo=bs);bo||N.error(Y||bm);if(O.call(bo)==="[object Array]"){if(bq){if(bl&&bl.nodeType===1){for(bm=0;bo[bm]!=null;bm++){if(bo[bm]&&(bo[bm]===true||bo[bm].nodeType===1&&A(bl,bo[bm]))){ba.push(bs[bm])}}}else{for(bm=0;bo[bm]!=null;bm++){bo[bm]&&bo[bm].nodeType===1&&ba.push(bs[bm])}}}else{ba.push.apply(ba,bo)}}else{c(bo,ba)}if(bb){N(bb,Z,ba,ab);N.uniqueSort(ba)}return ba};N.uniqueSort=function(Z){if(J){P=K;Z.sort(J);if(P){for(var Y=1;Y<Z.length;Y++){Z[Y]===Z[Y-1]&&Z.splice(Y--,1)}}}return Z};N.matches=function(Z,Y){return N(Z,null,null,Y)};N.find=function(bb,ba,ab){var aa,Y;if(!bb){return[]}for(var Z=0,bm=L.order.length;Z<bm;Z++){var bn=L.order[Z];if(Y=L.leftMatch[bn].exec(bb)){var bl=Y[1];Y.splice(1,1);if(bl.substr(bl.length-1)!=="\\"){Y[1]=(Y[1]||"").replace(/\\/g,"");aa=L.find[bn](Y,ba,ab);if(aa!=null){bb=bb.replace(L.match[bn],"");break}}}}aa||(aa=ba.getElementsByTagName("*"));return{set:aa,expr:bb}};N.filter=function(bn,bm,bb,ab){for(var Z=bn,aa=[],bt=bm,bu,bq,bl=bm&&bm[0]&&s(bm[0]);bn&&bm.length;){for(var bs in L.filter){if((bu=L.leftMatch[bs].exec(bn))!=null&&bu[2]){var bo=L.filter[bs],br,Y;Y=bu[1];bq=false;bu.splice(1,1);if(Y.substr(Y.length-1)!=="\\"){if(bt===aa){aa=[]}if(L.preFilter[bs]){if(bu=L.preFilter[bs](bu,bt,bb,aa,ab,bl)){if(bu===true){continue}}else{bq=br=true}}if(bu){for(var ba=0;(Y=bt[ba])!=null;ba++){if(Y){br=bo(Y,bu,ba,bt);var bp=ab^!!br;if(bb&&br!=null){if(bp){bq=true}else{bt[ba]=false}}else{if(bp){aa.push(Y);bq=true}}}}}if(br!==I){bb||(bt=aa);bn=bn.replace(L.match[bs],"");if(!bq){return[]}break}}}}if(bn===Z){if(bq==null){N.error(bn)}else{break}}Z=bn}return bt};N.error=function(Y){throw"Syntax error, unrecognized expression: "+Y};var L=N.selectors={order:["ID","NAME","TAG"],match:{ID:/#((?:[\w\u00c0-\uFFFF-]|\\.)+)/,CLASS:/\.((?:[\w\u00c0-\uFFFF-]|\\.)+)/,NAME:/\[name=['"]*((?:[\w\u00c0-\uFFFF-]|\\.)+)['"]*\]/,ATTR:/\[\s*((?:[\w\u00c0-\uFFFF-]|\\.)+)\s*(?:(\S?=)\s*(['"]*)(.*?)\3|)\s*\]/,TAG:/^((?:[\w\u00c0-\uFFFF\*-]|\\.)+)/,CHILD:/:(only|nth|last|first)-child(?:\((even|odd|[\dn+-]*)\))?/,POS:/:(nth|eq|gt|lt|first|last|even|odd)(?:\((\d*)\))?(?=[^-]|$)/,PSEUDO:/:((?:[\w\u00c0-\uFFFF-]|\\.)+)(?:\((['"]?)((?:\([^\)]+\)|[^\(\)]*)+)\2\))?/},leftMatch:{},attrMap:{"class":"className","for":"htmlFor"},attrHandle:{href:function(Y){return Y.getAttribute("href")}},relative:{"+":function(ab,aa){var Z=typeof aa==="string",Y=Z&&!/\W/.test(aa);Z=Z&&!Y;if(Y){aa=aa.toLowerCase()}Y=0;for(var ba=ab.length,bb;Y<ba;Y++){if(bb=ab[Y]){for(;(bb=bb.previousSibling)&&bb.nodeType!==1;){}ab[Y]=Z||bb&&bb.nodeName.toLowerCase()===aa?bb||false:bb===aa}}Z&&N.filter(aa,ab,true)},">":function(ab,aa){var Z=typeof aa==="string";if(Z&&!/\W/.test(aa)){aa=aa.toLowerCase();for(var Y=0,ba=ab.length;Y<ba;Y++){var bb=ab[Y];if(bb){Z=bb.parentNode;ab[Y]=Z.nodeName.toLowerCase()===aa?Z:false}}}else{Y=0;for(ba=ab.length;Y<ba;Y++){if(bb=ab[Y]){ab[Y]=Z?bb.parentNode:bb.parentNode===aa}}Z&&N.filter(aa,ab,true)}},"":function(ab,aa,Z){var Y=R++,ba=T;if(typeof aa==="string"&&!/\W/.test(aa)){var bb=aa=aa.toLowerCase();ba=V}ba("parentNode",aa,Y,ab,bb,Z)},"~":function(ab,aa,Z){var Y=R++,ba=T;if(typeof aa==="string"&&!/\W/.test(aa)){var bb=aa=aa.toLowerCase();ba=V}ba("previousSibling",aa,Y,ab,bb,Z)}},find:{ID:function(aa,Z,Y){if(typeof Z.getElementById!=="undefined"&&!Y){return(aa=Z.getElementById(aa[1]))?[aa]:[]}},NAME:function(ab,aa){if(typeof aa.getElementsByName!=="undefined"){var Z=[];aa=aa.getElementsByName(ab[1]);for(var Y=0,ba=aa.length;Y<ba;Y++){aa[Y].getAttribute("name")===ab[1]&&Z.push(aa[Y])}return Z.length===0?null:Z}},TAG:function(Z,Y){return Y.getElementsByTagName(Z[1])}},preFilter:{CLASS:function(ba,ab,Z,Y,bb,bl){ba=" "+ba[1].replace(/\\/g,"")+" ";if(bl){return ba}bl=0;for(var aa;(aa=ab[bl])!=null;bl++){if(aa){if(bb^(aa.className&&(" "+aa.className+" ").replace(/[\t\n]/g," ").indexOf(ba)>=0)){Z||Y.push(aa)}else{if(Z){ab[bl]=false}}}}return false},ID:function(Y){return Y[1].replace(/\\/g,"")},TAG:function(Y){return Y[1].toLowerCase()},CHILD:function(Z){if(Z[1]==="nth"){var Y=/(-?)(\d*)n((?:\+|-)?\d*)/.exec(Z[2]==="even"&&"2n"||Z[2]==="odd"&&"2n+1"||!/\D/.test(Z[2])&&"0n+"+Z[2]||Z[2]);Z[2]=Y[1]+(Y[2]||1)-0;Z[3]=Y[3]-0}Z[0]=R++;return Z},ATTR:function(ab,aa,Z,Y,ba,bb){aa=ab[1].replace(/\\/g,"");if(!bb&&L.attrMap[aa]){ab[1]=L.attrMap[aa]}if(ab[2]==="~="){ab[4]=" "+ab[4]+" "}return ab},PSEUDO:function(ab,aa,Z,Y,ba){if(ab[1]==="not"){if((Q.exec(ab[3])||"").length>1||/^\w/.test(ab[3])){ab[3]=N(ab[3],null,null,aa)}else{ab=N.filter(ab[3],aa,Z,true^ba);Z||Y.push.apply(Y,ab);return false}}else{if(L.match.POS.test(ab[0])||L.match.CHILD.test(ab[0])){return true}}return ab},POS:function(Y){Y.unshift(true);return Y}},filters:{enabled:function(Y){return Y.disabled===false&&Y.type!=="hidden"},disabled:function(Y){return Y.disabled===true},checked:function(Y){return Y.checked===true},selected:function(Y){return Y.selected===true},parent:function(Y){return !!Y.firstChild},empty:function(Y){return !Y.firstChild},has:function(aa,Z,Y){return !!N(Y[3],aa).length},header:function(Y){return/h\d/i.test(Y.nodeName)},text:function(Y){return"text"===Y.type},radio:function(Y){return"radio"===Y.type},checkbox:function(Y){return"checkbox"===Y.type},file:function(Y){return"file"===Y.type},password:function(Y){return"password"===Y.type},submit:function(Y){return"submit"===Y.type},image:function(Y){return"image"===Y.type},reset:function(Y){return"reset"===Y.type},button:function(Y){return"button"===Y.type||Y.nodeName.toLowerCase()==="button"},input:function(Y){return/input|select|textarea|button/i.test(Y.nodeName)}},setFilters:{first:function(Z,Y){return Y===0},last:function(ab,aa,Z,Y){return aa===Y.length-1},even:function(Z,Y){return Y%2===0},odd:function(Z,Y){return Y%2===1},lt:function(aa,Z,Y){return Z<Y[3]-0},gt:function(aa,Z,Y){return Z>Y[3]-0},nth:function(aa,Z,Y){return Y[3]-0===Z},eq:function(aa,Z,Y){return Y[3]-0===Z}},filter:{PSEUDO:function(ab,aa,Z,Y){var ba=aa[1],bb=L.filters[ba];if(bb){return bb(ab,Z,aa,Y)}else{if(ba==="contains"){return(ab.textContent||ab.innerText||W([ab])||"").indexOf(aa[3])>=0}else{if(ba==="not"){aa=aa[3];Z=0;for(Y=aa.length;Z<Y;Z++){if(aa[Z]===ab){return false}}return true}else{N.error("Syntax error, unrecognized expression: "+ba)}}}},CHILD:function(ba,ab){var Z=ab[1],Y=ba;switch(Z){case"only":case"first":for(;Y=Y.previousSibling;){if(Y.nodeType===1){return false}}if(Z==="first"){return true}Y=ba;case"last":for(;Y=Y.nextSibling;){if(Y.nodeType===1){return false}}return true;case"nth":Z=ab[2];var bb=ab[3];if(Z===1&&bb===0){return true}ab=ab[0];var bl=ba.parentNode;if(bl&&(bl.sizcache!==ab||!ba.nodeIndex)){var aa=0;for(Y=bl.firstChild;Y;Y=Y.nextSibling){if(Y.nodeType===1){Y.nodeIndex=++aa}}bl.sizcache=ab}ba=ba.nodeIndex-bb;return Z===0?ba===0:ba%Z===0&&ba/Z>=0}},ID:function(Z,Y){return Z.nodeType===1&&Z.getAttribute("id")===Y},TAG:function(Z,Y){return Y==="*"&&Z.nodeType===1||Z.nodeName.toLowerCase()===Y},CLASS:function(Z,Y){return(" "+(Z.className||Z.getAttribute("class"))+" ").indexOf(Y)>-1},ATTR:function(ab,aa){var Z=aa[1];ab=L.attrHandle[Z]?L.attrHandle[Z](ab):ab[Z]!=null?ab[Z]:ab.getAttribute(Z);Z=ab+"";var Y=aa[2];aa=aa[4];return ab==null?Y==="!=":Y==="="?Z===aa:Y==="*="?Z.indexOf(aa)>=0:Y==="~="?(" "+Z+" ").indexOf(aa)>=0:!aa?Z&&ab!==false:Y==="!="?Z!==aa:Y==="^="?Z.indexOf(aa)===0:Y==="$="?Z.substr(Z.length-aa.length)===aa:Y==="|="?Z===aa||Z.substr(0,aa.length+1)===aa+"-":false},POS:function(ab,aa,Z,Y){var ba=L.setFilters[aa[2]];if(ba){return ba(ab,Z,aa,Y)}}}},G=L.match.POS;for(var w in L.match){L.match[w]=new RegExp(L.match[w].source+/(?![^\[]*\])(?![^\(]*\))/.source);L.leftMatch[w]=new RegExp(/(^(?:.|\r|\n)*?)/.source+L.match[w].source.replace(/\\(\d+)/g,function(Z,Y){return"\\"+(Y-0+1)}))}var c=function(Z,Y){Z=Array.prototype.slice.call(Z,0);if(Y){Y.push.apply(Y,Z);return Y}return Z};try{Array.prototype.slice.call(M.documentElement.childNodes,0)}catch(F){c=function(ab,aa){aa=aa||[];if(O.call(ab)==="[object Array]"){Array.prototype.push.apply(aa,ab)}else{if(typeof ab.length==="number"){for(var Z=0,Y=ab.length;Z<Y;Z++){aa.push(ab[Z])}}else{for(Z=0;ab[Z];Z++){aa.push(ab[Z])}}}return aa}}var J;if(M.documentElement.compareDocumentPosition){J=function(Z,Y){if(!Z.compareDocumentPosition||!Y.compareDocumentPosition){if(Z==Y){P=true}return Z.compareDocumentPosition?-1:1}Z=Z.compareDocumentPosition(Y)&4?-1:Z===Y?0:1;if(Z===0){P=true}return Z}}else{if("sourceIndex" in M.documentElement){J=function(Z,Y){if(!Z.sourceIndex||!Y.sourceIndex){if(Z==Y){P=true}return Z.sourceIndex?-1:1}Z=Z.sourceIndex-Y.sourceIndex;if(Z===0){P=true}return Z}}else{if(M.createRange){J=function(ab,aa){if(!ab.ownerDocument||!aa.ownerDocument){if(ab==aa){P=true}return ab.ownerDocument?-1:1}var Z=ab.ownerDocument.createRange(),Y=aa.ownerDocument.createRange();Z.setStart(ab,0);Z.setEnd(ab,0);Y.setStart(aa,0);Y.setEnd(aa,0);ab=Z.compareBoundaryPoints(Range.START_TO_END,Y);if(ab===0){P=true}return ab}}}}(function(){var aa=M.createElement("div"),Z="script"+(new Date).getTime();aa.innerHTML="<a name='"+Z+"'/>";var Y=M.documentElement;Y.insertBefore(aa,Y.firstChild);if(M.getElementById(Z)){L.find.ID=function(ab,ba,bb){if(typeof ba.getElementById!=="undefined"&&!bb){return(ba=ba.getElementById(ab[1]))?ba.id===ab[1]||typeof ba.getAttributeNode!=="undefined"&&ba.getAttributeNode("id").nodeValue===ab[1]?[ba]:I:[]}};L.filter.ID=function(ab,ba){var bb=typeof ab.getAttributeNode!=="undefined"&&ab.getAttributeNode("id");return ab.nodeType===1&&bb&&bb.nodeValue===ba}}Y.removeChild(aa);Y=aa=null})();(function(){var Y=M.createElement("div");Y.appendChild(M.createComment(""));if(Y.getElementsByTagName("*").length>0){L.find.TAG=function(ab,aa){aa=aa.getElementsByTagName(ab[1]);if(ab[1]==="*"){ab=[];for(var Z=0;aa[Z];Z++){aa[Z].nodeType===1&&ab.push(aa[Z])}aa=ab}return aa}}Y.innerHTML="<a href='#'></a>";if(Y.firstChild&&typeof Y.firstChild.getAttribute!=="undefined"&&Y.firstChild.getAttribute("href")!=="#"){L.attrHandle.href=function(Z){return Z.getAttribute("href",2)}}Y=null})();M.querySelectorAll&&function(){var aa=N,Z=M.createElement("div");Z.innerHTML="<p class='TEST'></p>";if(!(Z.querySelectorAll&&Z.querySelectorAll(".TEST").length===0)){N=function(ab,bl,bm,ba){bl=bl||M;if(!ba&&bl.nodeType===9&&!s(bl)){try{return c(bl.querySelectorAll(ab),bm)}catch(bb){}}return aa(ab,bl,bm,ba)};for(var Y in aa){N[Y]=aa[Y]}Z=null}}();(function(){var Y=M.createElement("div");Y.innerHTML="<div class='test e'></div><div class='test'></div>";if(!(!Y.getElementsByClassName||Y.getElementsByClassName("e").length===0)){Y.lastChild.className="e";if(Y.getElementsByClassName("e").length!==1){L.order.splice(1,0,"CLASS");L.find.CLASS=function(ab,aa,Z){if(typeof aa.getElementsByClassName!=="undefined"&&!Z){return aa.getElementsByClassName(ab[1])}};Y=null}}})();var A=M.compareDocumentPosition?function(Z,Y){return !!(Z.compareDocumentPosition(Y)&16)}:function(Z,Y){return Z!==Y&&(Z.contains?Z.contains(Y):true)},s=function(Y){return(Y=(Y?Y.ownerDocument||Y:0).documentElement)?Y.nodeName!=="HTML":false},X=function(ab,aa){var Z=[],Y="",ba;for(aa=aa.nodeType?[aa]:aa;ba=L.match.PSEUDO.exec(ab);){Y+=ba[0];ab=ab.replace(L.match.PSEUDO,"")}ab=L.relative[ab]?ab+"*":ab;ba=0;for(var bb=aa.length;ba<bb;ba++){N(ab,aa[ba],Z)}return N.filter(Y,Z)};ah.find=N;ah.expr=N.selectors;ah.expr[":"]=ah.expr.filters;ah.unique=N.uniqueSort;ah.text=W;ah.isXMLDoc=s;ah.contains=A})();var f=/Until$/,a9=/^(?:parents|prevUntil|prevAll)/,aW=/,/;au=Array.prototype.slice;var aL=function(s,c,A){if(ah.isFunction(c)){return ah.grep(s,function(G,F){return !!c.call(G,F,G)===A})}else{if(c.nodeType){return ah.grep(s,function(F){return F===c===A})}else{if(typeof c==="string"){var w=ah.grep(s,function(F){return F.nodeType===1});if(aT.test(c)){return ah.filter(c,w,!A)}else{c=ah.filter(c,w)}}}}return ah.grep(s,function(F){return ah.inArray(F,c)>=0===A})};ah.fn.extend({find:function(s){for(var c=this.pushStack("","find",s),J=0,F=0,G=this.length;F<G;F++){J=c.length;ah.find(s,this[F],c);if(F>0){for(var w=J;w<c.length;w++){for(var A=0;A<J;A++){if(c[A]===c[w]){c.splice(w--,1);break}}}}}return c},has:function(s){var c=ah(s);return this.filter(function(){for(var A=0,w=c.length;A<w;A++){if(ah.contains(this,c[A])){return true}}})},not:function(c){return this.pushStack(aL(this,c,false),"not",c)},filter:function(c){return this.pushStack(aL(this,c,true),"filter",c)},is:function(c){return !!c&&ah.filter(c,this).length>0},closest:function(L,K){if(ah.isArray(L)){var J=[],F=this[0],G,w={},A;if(F&&L.length){G=0;for(var c=L.length;G<c;G++){A=L[G];w[A]||(w[A]=ah.expr.match.POS.test(A)?ah(A,K||this.context):A)}for(;F&&F.ownerDocument&&F!==K;){for(A in w){G=w[A];if(G.jquery?G.index(F)>-1:ah(F).is(G)){J.push({selector:A,elem:F});delete w[A]}}F=F.parentNode}}return J}var s=ah.expr.match.POS.test(L)?ah(L,K||this.context):null;return this.map(function(O,N){for(;N&&N.ownerDocument&&N!==K;){if(s?s.index(N)>-1:ah(N).is(L)){return N}N=N.parentNode}return null})},index:function(c){if(!c||typeof c==="string"){return ah.inArray(this[0],c?ah(c):this.parent().children())}return ah.inArray(c.jquery?c[0]:c,this)},add:function(s,c){s=typeof s==="string"?ah(s,c||this.context):ah.makeArray(s);c=ah.merge(this.get(),s);return this.pushStack(l(s[0])||l(c[0])?c:ah.unique(c))},andSelf:function(){return this.add(this.prevObject)}});ah.each({parent:function(c){return(c=c.parentNode)&&c.nodeType!==11?c:null},parents:function(c){return ah.dir(c,"parentNode")},parentsUntil:function(s,c,w){return ah.dir(s,"parentNode",w)},next:function(c){return ah.nth(c,2,"nextSibling")},prev:function(c){return ah.nth(c,2,"previousSibling")},nextAll:function(c){return ah.dir(c,"nextSibling")},prevAll:function(c){return ah.dir(c,"previousSibling")},nextUntil:function(s,c,w){return ah.dir(s,"nextSibling",w)},prevUntil:function(s,c,w){return ah.dir(s,"previousSibling",w)},siblings:function(c){return ah.sibling(c.parentNode.firstChild,c)},children:function(c){return ah.sibling(c.firstChild)},contents:function(c){return ah.nodeName(c,"iframe")?c.contentDocument||c.contentWindow.document:ah.makeArray(c.childNodes)}},function(s,c){ah.fn[s]=function(F,w){var A=ah.map(this,c,F);f.test(s)||(w=F);if(w&&typeof w==="string"){A=ah.filter(w,A)}A=this.length>1?ah.unique(A):A;if((this.length>1||aW.test(w))&&a9.test(s)){A=A.reverse()}return this.pushStack(A,s,au.call(arguments).join(","))}});ah.extend({filter:function(s,c,w){if(w){s=":not("+s+")"}return ah.find.matches(s,c)},dir:function(s,c,A){var w=[];for(s=s[c];s&&s.nodeType!==9&&(A===I||s.nodeType!==1||!ah(s).is(A));){s.nodeType===1&&w.push(s);s=s[c]}return w},nth:function(s,c,A){c=c||1;for(var w=0;s;s=s[A]){if(s.nodeType===1&&++w===c){break}}return s},sibling:function(s,c){for(var w=[];s;s=s.nextSibling){s.nodeType===1&&s!==c&&w.push(s)}return w}});var ai=/ jQuery\d+="(?:\d+|null)"/g,ar=/^\s+/,B=/(<([\w:]+)[^>]*?)\/>/g,aD=/^(?:area|br|col|embed|hr|img|input|link|meta|param)$/i,m=/<([\w:]+)/,ac=/<tbody/i,u=/<|&#?\w+;/,aP=/<script|<object|<embed|<option|<style/i,ak=/checked\s*(?:[^=]|=\s*.checked.)/i,bk=function(s,c,w){return aD.test(w)?s:c+"></"+w+">"},aJ={option:[1,"<select multiple='multiple'>","</select>"],legend:[1,"<fieldset>","</fieldset>"],thead:[1,"<table>","</table>"],tr:[2,"<table><tbody>","</tbody></table>"],td:[3,"<table><tbody><tr>","</tr></tbody></table>"],col:[2,"<table><tbody></tbody><colgroup>","</colgroup></table>"],area:[1,"<map>","</map>"],_default:[0,"",""]};aJ.optgroup=aJ.option;aJ.tbody=aJ.tfoot=aJ.colgroup=aJ.caption=aJ.thead;aJ.th=aJ.td;if(!ah.support.htmlSerialize){aJ._default=[1,"div<div>","</div>"]}ah.fn.extend({text:function(c){if(ah.isFunction(c)){return this.each(function(s){var w=ah(this);w.text(c.call(this,s,w.text()))})}if(typeof c!=="object"&&c!==I){return this.empty().append((this[0]&&this[0].ownerDocument||M).createTextNode(c))}return ah.text(this)},wrapAll:function(s){if(ah.isFunction(s)){return this.each(function(w){ah(this).wrapAll(s.call(this,w))})}if(this[0]){var c=ah(s,this[0].ownerDocument).eq(0).clone(true);this[0].parentNode&&c.insertBefore(this[0]);c.map(function(){for(var w=this;w.firstChild&&w.firstChild.nodeType===1;){w=w.firstChild}return w}).append(this)}return this},wrapInner:function(c){if(ah.isFunction(c)){return this.each(function(s){ah(this).wrapInner(c.call(this,s))})}return this.each(function(){var s=ah(this),w=s.contents();w.length?w.wrapAll(c):s.append(c)})},wrap:function(c){return this.each(function(){ah(this).wrapAll(c)})},unwrap:function(){return this.parent().each(function(){ah.nodeName(this,"body")||ah(this).replaceWith(this.childNodes)}).end()},append:function(){return this.domManip(arguments,true,function(c){this.nodeType===1&&this.appendChild(c)})},prepend:function(){return this.domManip(arguments,true,function(c){this.nodeType===1&&this.insertBefore(c,this.firstChild)})},before:function(){if(this[0]&&this[0].parentNode){return this.domManip(arguments,false,function(s){this.parentNode.insertBefore(s,this)})}else{if(arguments.length){var c=ah(arguments[0]);c.push.apply(c,this.toArray());return this.pushStack(c,"before",arguments)}}},after:function(){if(this[0]&&this[0].parentNode){return this.domManip(arguments,false,function(s){this.parentNode.insertBefore(s,this.nextSibling)})}else{if(arguments.length){var c=this.pushStack(this,"after",arguments);c.push.apply(c,ah(arguments[0]).toArray());return c}}},remove:function(s,c){for(var A=0,w;(w=this[A])!=null;A++){if(!s||ah.filter(s,[w]).length){if(!c&&w.nodeType===1){ah.cleanData(w.getElementsByTagName("*"));ah.cleanData([w])}w.parentNode&&w.parentNode.removeChild(w)}}return this},empty:function(){for(var s=0,c;(c=this[s])!=null;s++){for(c.nodeType===1&&ah.cleanData(c.getElementsByTagName("*"));c.firstChild;){c.removeChild(c.firstChild)}}return this},clone:function(s){var c=this.map(function(){if(!ah.support.noCloneEvent&&!ah.isXMLDoc(this)){var A=this.outerHTML,w=this.ownerDocument;if(!A){A=w.createElement("div");A.appendChild(this.cloneNode(true));A=A.innerHTML}return ah.clean([A.replace(ai,"").replace(/=([^="'>\s]+\/)>/g,'="$1">').replace(ar,"")],w)[0]}else{return this.cloneNode(true)}});if(s===true){bj(this,c);bj(this.find("*"),c.find("*"))}return c},html:function(s){if(s===I){return this[0]&&this[0].nodeType===1?this[0].innerHTML.replace(ai,""):null}else{if(typeof s==="string"&&!aP.test(s)&&(ah.support.leadingWhitespace||!ar.test(s))&&!aJ[(m.exec(s)||["",""])[1].toLowerCase()]){s=s.replace(B,bk);try{for(var c=0,A=this.length;c<A;c++){if(this[c].nodeType===1){ah.cleanData(this[c].getElementsByTagName("*"));this[c].innerHTML=s}}}catch(w){this.empty().append(s)}}else{ah.isFunction(s)?this.each(function(J){var F=ah(this),G=F.html();F.empty().append(function(){return s.call(this,J,G)})}):this.empty().append(s)}}return this},replaceWith:function(c){if(this[0]&&this[0].parentNode){if(ah.isFunction(c)){return this.each(function(s){var A=ah(this),w=A.html();A.replaceWith(c.call(this,s,w))})}if(typeof c!=="string"){c=ah(c).detach()}return this.each(function(){var s=this.nextSibling,w=this.parentNode;ah(this).remove();s?ah(s).before(c):ah(w).append(c)})}else{return this.pushStack(ah(ah.isFunction(c)?c():c),"replaceWith",c)}},detach:function(c){return this.remove(c,true)},domManip:function(O,N,L){function J(P){return ah.nodeName(P,"table")?P.getElementsByTagName("tbody")[0]||P.appendChild(P.ownerDocument.createElement("tbody")):P}var K,F,G=O[0],s=[],A;if(!ah.support.checkClone&&arguments.length===3&&typeof G==="string"&&ak.test(G)){return this.each(function(){ah(this).domManip(O,N,L,true)})}if(ah.isFunction(G)){return this.each(function(P){var Q=ah(this);O[0]=G.call(this,P,N?Q.html():I);Q.domManip(O,N,L)})}if(this[0]){K=G&&G.parentNode;K=ah.support.parentNode&&K&&K.nodeType===11&&K.childNodes.length===this.length?{fragment:K}:a3(O,this,s);A=K.fragment;if(F=A.childNodes.length===1?(A=A.firstChild):A.firstChild){N=N&&ah.nodeName(F,"tr");for(var w=0,c=this.length;w<c;w++){L.call(N?J(this[w],F):this[w],w>0||K.cacheable||this.length>1?A.cloneNode(true):A)}}s.length&&ah.each(s,E)}return this}});ah.fragments={};ah.each({appendTo:"append",prependTo:"prepend",insertBefore:"before",insertAfter:"after",replaceAll:"replaceWith"},function(s,c){ah.fn[s]=function(J){var F=[];J=ah(J);var G=this.length===1&&this[0].parentNode;if(G&&G.nodeType===11&&G.childNodes.length===1&&J.length===1){J[c](this[0]);return this}else{G=0;for(var w=J.length;G<w;G++){var A=(G>0?this.clone(true):this).get();ah.fn[c].apply(ah(J[G]),A);F=F.concat(A)}return this.pushStack(F,s,J.selector)}}});ah.extend({clean:function(O,N,L,J){N=N||M;if(typeof N.createElement==="undefined"){N=N.ownerDocument||N[0]&&N[0].ownerDocument||M}for(var K=[],F=0,G;(G=O[F])!=null;F++){if(typeof G==="number"){G+=""}if(G){if(typeof G==="string"&&!u.test(G)){G=N.createTextNode(G)}else{if(typeof G==="string"){G=G.replace(B,bk);var s=(m.exec(G)||["",""])[1].toLowerCase(),A=aJ[s]||aJ._default,w=A[0],c=N.createElement("div");for(c.innerHTML=A[1]+G+A[2];w--;){c=c.lastChild}if(!ah.support.tbody){w=ac.test(G);s=s==="table"&&!w?c.firstChild&&c.firstChild.childNodes:A[1]==="<table>"&&!w?c.childNodes:[];for(A=s.length-1;A>=0;--A){ah.nodeName(s[A],"tbody")&&!s[A].childNodes.length&&s[A].parentNode.removeChild(s[A])}}!ah.support.leadingWhitespace&&ar.test(G)&&c.insertBefore(N.createTextNode(ar.exec(G)[0]),c.firstChild);G=c.childNodes}}if(G.nodeType){K.push(G)}else{K=ah.merge(K,G)}}}if(L){for(F=0;K[F];F++){if(J&&ah.nodeName(K[F],"script")&&(!K[F].type||K[F].type.toLowerCase()==="text/javascript")){J.push(K[F].parentNode?K[F].parentNode.removeChild(K[F]):K[F])}else{K[F].nodeType===1&&K.splice.apply(K,[F+1,0].concat(ah.makeArray(K[F].getElementsByTagName("script"))));L.appendChild(K[F])}}}return K},cleanData:function(L){for(var K,J,F=ah.cache,G=ah.event.special,w=ah.support.deleteExpando,A=0,c;(c=L[A])!=null;A++){if(J=c[ah.expando]){K=F[J];if(K.events){for(var s in K.events){G[s]?ah.event.remove(c,s):aG(c,s,K.handle)}}if(w){delete c[ah.expando]}else{c.removeAttribute&&c.removeAttribute(ah.expando)}delete F[J]}}}});var h=/z-?index|font-?weight|opacity|zoom|line-?height/i,a4=/alpha\([^)]*\)/,aQ=/opacity=([^)]*)/,aE=/float/i,ad=/-([a-z])/ig,bf=/([A-Z])/g,aZ=/^-?\d+(?:px)?$/i,aI=/^-?\d/,af={position:"absolute",visibility:"hidden",display:"block"},y=["Left","Right"],k=["Top","Bottom"],bi=M.defaultView&&M.defaultView.getComputedStyle,al=ah.support.cssFloat?"cssFloat":"styleFloat",v=function(s,c){return c.toUpperCase()};ah.fn.css=function(s,c){return ap(this,s,c,true,function(F,w,A){if(A===I){return ah.curCSS(F,w)}if(typeof A==="number"&&!h.test(w)){A+="px"}ah.style(F,w,A)})};ah.extend({style:function(s,c,F){if(!s||s.nodeType===3||s.nodeType===8){return I}if((c==="width"||c==="height")&&parseFloat(F)<0){F=I}var w=s.style||s,A=F!==I;if(!ah.support.opacity&&c==="opacity"){if(A){w.zoom=1;c=parseInt(F,10)+""==="NaN"?"":"alpha(opacity="+F*100+")";s=w.filter||ah.curCSS(s,"filter")||"";w.filter=a4.test(s)?s.replace(a4,c):c}return w.filter&&w.filter.indexOf("opacity=")>=0?parseFloat(aQ.exec(w.filter)[1])/100+"":""}if(aE.test(c)){c=al}c=c.replace(ad,v);if(A){w[c]=F}return w[c]},css:function(s,c,J,F){if(c==="width"||c==="height"){var G,w=c==="width"?y:k;function A(){G=c==="width"?s.offsetWidth:s.offsetHeight;F!=="border"&&ah.each(w,function(){F||(G-=parseFloat(ah.curCSS(s,"padding"+this,true))||0);if(F==="margin"){G+=parseFloat(ah.curCSS(s,"margin"+this,true))||0}else{G-=parseFloat(ah.curCSS(s,"border"+this+"Width",true))||0}})}s.offsetWidth!==0?A():ah.swap(s,af,A);return Math.max(0,Math.round(G))}return ah.curCSS(s,c,J)},curCSS:function(s,c,G){var A,F=s.style;if(!ah.support.opacity&&c==="opacity"&&s.currentStyle){A=aQ.test(s.currentStyle.filter||"")?parseFloat(RegExp.$1)/100+"":"";return A===""?"1":A}if(aE.test(c)){c=al}if(!G&&F&&F[c]){A=F[c]}else{if(bi){if(aE.test(c)){c="float"}c=c.replace(bf,"-$1").toLowerCase();F=s.ownerDocument.defaultView;if(!F){return null}if(s=F.getComputedStyle(s,null)){A=s.getPropertyValue(c)}if(c==="opacity"&&A===""){A="1"}}else{if(s.currentStyle){G=c.replace(ad,v);A=s.currentStyle[c]||s.currentStyle[G];if(!aZ.test(A)&&aI.test(A)){c=F.left;var w=s.runtimeStyle.left;s.runtimeStyle.left=s.currentStyle.left;F.left=G==="fontSize"?"1em":A||0;A=F.pixelLeft+"px";F.left=c;s.runtimeStyle.left=w}}}}return A},swap:function(s,c,F){var w={};for(var A in c){w[A]=s.style[A];s.style[A]=c[A]}F.call(s);for(A in c){s.style[A]=w[A]}}});if(ah.expr&&ah.expr.filters){ah.expr.filters.hidden=function(s){var c=s.offsetWidth,A=s.offsetHeight,w=s.nodeName.toLowerCase()==="tr";return c===0&&A===0&&!w?true:c>0&&A>0&&!w?false:ah.curCSS(s,"display")==="none"};ah.expr.filters.visible=function(c){return !ah.expr.filters.hidden(c)}}var a2=aF(),aN=/<script(.|\s)*?\/script>/gi,aj=/select|textarea/i,C=/color|date|datetime|email|hidden|month|number|password|range|search|tel|text|time|url|week/i,aA=/=\?(&|$)/,i=/\?/,n=/(\?|&)_=.*?(&|$)/,a=/^(\w+:)?\/\/([^\/?#]+)/,a5=/%20/g,aR=ah.fn.load;ah.fn.extend({load:function(s,c,G){if(typeof s!=="string"){return aR.call(this,s)}else{if(!this.length){return this}}var A=s.indexOf(" ");if(A>=0){var F=s.slice(A,s.length);s=s.slice(0,A)}A="GET";if(c){if(ah.isFunction(c)){G=c;c=null}else{if(typeof c==="object"){c=ah.param(c,ah.ajaxSettings.traditional);A="POST"}}}var w=this;ah.ajax({url:s,type:A,dataType:"html",data:c,complete:function(J,K){if(K==="success"||K==="notmodified"){w.html(F?ah("<div />").append(J.responseText.replace(aN,"")).find(F):J.responseText)}G&&w.each(G,[J.responseText,K,J])}});return this},serialize:function(){return ah.param(this.serializeArray())},serializeArray:function(){return this.map(function(){return this.elements?ah.makeArray(this.elements):this}).filter(function(){return this.name&&!this.disabled&&(this.checked||aj.test(this.nodeName)||C.test(this.type))}).map(function(s,c){s=ah(this).val();return s==null?null:ah.isArray(s)?ah.map(s,function(w){return{name:c.name,value:w}}):{name:c.name,value:s}}).get()}});ah.each("ajaxStart ajaxStop ajaxComplete ajaxError ajaxSuccess ajaxSend".split(" "),function(s,c){ah.fn[c]=function(w){return this.bind(c,w)}});ah.extend({get:function(s,c,A,w){if(ah.isFunction(c)){w=w||A;A=c;c=null}return ah.ajax({type:"GET",url:s,data:c,success:A,dataType:w})},getScript:function(s,c){return ah.get(s,null,c,"script")},getJSON:function(s,c,w){return ah.get(s,c,w,"json")},post:function(s,c,A,w){if(ah.isFunction(c)){w=w||A;A=c;c={}}return ah.ajax({type:"POST",url:s,data:c,success:A,dataType:w})},ajaxSetup:function(c){ah.extend(ah.ajaxSettings,c)},ajaxSettings:{url:location.href,global:true,type:"GET",contentType:"application/x-www-form-urlencoded",processData:true,async:true,xhr:aO.XMLHttpRequest&&(aO.location.protocol!=="file:"||!aO.ActiveXObject)?function(){return new aO.XMLHttpRequest}:function(){try{return new aO.ActiveXObject("Microsoft.XMLHTTP")}catch(c){}},accepts:{xml:"application/xml, text/xml",html:"text/html",script:"text/javascript, application/javascript",json:"application/json, text/javascript",text:"text/plain",_default:"*/*"}},lastModified:{},etag:{},ajax:function(aa){function Z(){X.success&&X.success.call(P,K,R,s);X.global&&W("ajaxSuccess",[s,X])}function Y(){X.complete&&X.complete.call(P,s,R);X.global&&W("ajaxComplete",[s,X]);X.global&&!--ah.active&&ah.event.trigger("ajaxStop")}function W(ba,bb){(X.context?ah(X.context):ah.event).trigger(ba,bb)}var X=ah.extend(true,{},ah.ajaxSettings,aa),Q,R,K,P=aa&&aa.context||X,L=X.type.toUpperCase();if(X.data&&X.processData&&typeof X.data!=="string"){X.data=ah.param(X.data,X.traditional)}if(X.dataType==="jsonp"){if(L==="GET"){aA.test(X.url)||(X.url+=(i.test(X.url)?"&":"?")+(X.jsonp||"callback")+"=?")}else{if(!X.data||!aA.test(X.data)){X.data=(X.data?X.data+"&":"")+(X.jsonp||"callback")+"=?"}}X.dataType="json"}if(X.dataType==="json"&&(X.data&&aA.test(X.data)||aA.test(X.url))){Q=X.jsonpCallback||"jsonp"+a2++;if(X.data){X.data=(X.data+"").replace(aA,"="+Q+"$1")}X.url=X.url.replace(aA,"="+Q+"$1");X.dataType="script";aO[Q]=aO[Q]||function(ba){K=ba;Z();Y();aO[Q]=I;try{delete aO[Q]}catch(bb){}c&&c.removeChild(F)}}if(X.dataType==="script"&&X.cache===null){X.cache=false}if(X.cache===false&&L==="GET"){var G=aF(),w=X.url.replace(n,"$1_="+G+"$2");X.url=w+(w===X.url?(i.test(X.url)?"&":"?")+"_="+G:"")}if(X.data&&L==="GET"){X.url+=(i.test(X.url)?"&":"?")+X.data}X.global&&!ah.active++&&ah.event.trigger("ajaxStart");G=(G=a.exec(X.url))&&(G[1]&&G[1]!==location.protocol||G[2]!==location.host);if(X.dataType==="script"&&L==="GET"&&G){var c=M.getElementsByTagName("head")[0]||M.documentElement,F=M.createElement("script");F.src=X.url;if(X.scriptCharset){F.charset=X.scriptCharset}if(!Q){var J=false;F.onload=F.onreadystatechange=function(){if(!J&&(!this.readyState||this.readyState==="loaded"||this.readyState==="complete")){J=true;Z();Y();F.onload=F.onreadystatechange=null;c&&F.parentNode&&c.removeChild(F)}}}c.insertBefore(F,c.firstChild);return I}var A=false,s=X.xhr();if(s){X.username?s.open(L,X.url,X.async,X.username,X.password):s.open(L,X.url,X.async);try{if(X.data||aa&&aa.contentType){s.setRequestHeader("Content-Type",X.contentType)}if(X.ifModified){ah.lastModified[X.url]&&s.setRequestHeader("If-Modified-Since",ah.lastModified[X.url]);ah.etag[X.url]&&s.setRequestHeader("If-None-Match",ah.etag[X.url])}G||s.setRequestHeader("X-Requested-With","XMLHttpRequest");s.setRequestHeader("Accept",X.dataType&&X.accepts[X.dataType]?X.accepts[X.dataType]+", */*":X.accepts._default)}catch(ab){}if(X.beforeSend&&X.beforeSend.call(P,s,X)===false){X.global&&!--ah.active&&ah.event.trigger("ajaxStop");s.abort();return false}X.global&&W("ajaxSend",[s,X]);var V=s.onreadystatechange=function(bb){if(!s||s.readyState===0||bb==="abort"){A||Y();A=true;if(s){s.onreadystatechange=ah.noop}}else{if(!A&&s&&(s.readyState===4||bb==="timeout")){A=true;s.onreadystatechange=ah.noop;R=bb==="timeout"?"timeout":!ah.httpSuccess(s)?"error":X.ifModified&&ah.httpNotModified(s,X.url)?"notmodified":"success";var bl;if(R==="success"){try{K=ah.httpData(s,X.dataType,X)}catch(ba){R="parsererror";bl=ba}}if(R==="success"||R==="notmodified"){Q||Z()}else{ah.handleError(X,s,R,bl)}Y();bb==="timeout"&&s.abort();if(X.async){s=null}}}};try{var T=s.abort;s.abort=function(){s&&T.call(s);V("abort")}}catch(O){}X.async&&X.timeout>0&&setTimeout(function(){s&&!A&&V("timeout")},X.timeout);try{s.send(L==="POST"||L==="PUT"||L==="DELETE"?X.data:null)}catch(N){ah.handleError(X,s,null,N);Y()}X.async||V();return s}},handleError:function(s,c,A,w){if(s.error){s.error.call(s.context||s,c,A,w)}if(s.global){(s.context?ah(s.context):ah.event).trigger("ajaxError",[c,s,w])}},active:0,httpSuccess:function(s){try{return !s.status&&location.protocol==="file:"||s.status>=200&&s.status<300||s.status===304||s.status===1223||s.status===0}catch(c){}return false},httpNotModified:function(s,c){var A=s.getResponseHeader("Last-Modified"),w=s.getResponseHeader("Etag");if(A){ah.lastModified[c]=A}if(w){ah.etag[c]=w}return s.status===304||s.status===0},httpData:function(s,c,F){var w=s.getResponseHeader("content-type")||"",A=c==="xml"||!c&&w.indexOf("xml")>=0;s=A?s.responseXML:s.responseText;A&&s.documentElement.nodeName==="parsererror"&&ah.error("parsererror");if(F&&F.dataFilter){s=F.dataFilter(s,c)}if(typeof s==="string"){if(c==="json"||!c&&w.indexOf("json")>=0){s=ah.parseJSON(s)}else{if(c==="script"||!c&&w.indexOf("javascript")>=0){ah.globalEval(s)}}}return s},param:function(s,c){function G(J,K){if(ah.isArray(K)){ah.each(K,function(L,N){c||/\[\]$/.test(J)?A(J,N):G(J+"["+(typeof N==="object"||ah.isArray(N)?L:"")+"]",N)})}else{!c&&K!=null&&typeof K==="object"?ah.each(K,function(L,N){G(J+"["+L+"]",N)}):A(J,K)}}function A(J,K){K=ah.isFunction(K)?K():K;F[F.length]=encodeURIComponent(J)+"="+encodeURIComponent(K)}var F=[];if(c===I){c=ah.ajaxSettings.traditional}if(ah.isArray(s)||s.jquery){ah.each(s,function(){A(this.name,this.value)})}else{for(var w in s){G(w,s[w])}}return F.join("&").replace(a5,"+")}});var bg={},bc=/toggle|show|hide/,aX=/^([+-]=)?([\d+-.]+)(.*)$/,aq,D=[["height","marginTop","marginBottom","paddingTop","paddingBottom"],["width","marginLeft","marginRight","paddingLeft","paddingRight"],["opacity"]];ah.fn.extend({show:function(s,c){if(s||s===0){return this.animate(aC("show",3),s,c)}else{s=0;for(c=this.length;s<c;s++){var F=ah.data(this[s],"olddisplay");this[s].style.display=F||"";if(ah.css(this[s],"display")==="none"){F=this[s].nodeName;var w;if(bg[F]){w=bg[F]}else{var A=ah("<"+F+" />").appendTo("body");w=A.css("display");if(w==="none"){w="block"}A.remove();bg[F]=w}ah.data(this[s],"olddisplay",w)}}s=0;for(c=this.length;s<c;s++){this[s].style.display=ah.data(this[s],"olddisplay")||""}return this}},hide:function(s,c){if(s||s===0){return this.animate(aC("hide",3),s,c)}else{s=0;for(c=this.length;s<c;s++){var w=ah.data(this[s],"olddisplay");!w&&w!=="none"&&ah.data(this[s],"olddisplay",ah.css(this[s],"display"))}s=0;for(c=this.length;s<c;s++){this[s].style.display="none"}return this}},_toggle:ah.fn.toggle,toggle:function(s,c){var w=typeof s==="boolean";if(ah.isFunction(s)&&ah.isFunction(c)){this._toggle.apply(this,arguments)}else{s==null||w?this.each(function(){var A=w?s:ah(this).is(":hidden");ah(this)[A?"show":"hide"]()}):this.animate(aC("toggle",3),s,c)}return this},fadeTo:function(s,c,w){return this.filter(":hidden").css("opacity",0).show().end().animate({opacity:c},s,w)},animate:function(s,c,F,w){var A=ah.speed(c,F,w);if(ah.isEmptyObject(s)){return this.each(A.complete)}return this[A.queue===false?"each":"queue"](function(){var J=ah.extend({},A),K,L=this.nodeType===1&&ah(this).is(":hidden"),G=this;for(K in s){var N=K.replace(ad,v);if(K!==N){s[N]=s[K];delete s[K];K=N}if(s[K]==="hide"&&L||s[K]==="show"&&!L){return J.complete.call(this)}if((K==="height"||K==="width")&&this.style){J.display=ah.css(this,"display");J.overflow=this.style.overflow}if(ah.isArray(s[K])){(J.specialEasing=J.specialEasing||{})[K]=s[K][1];s[K]=s[K][0]}}if(J.overflow!=null){this.style.overflow="hidden"}J.curAnim=ah.extend({},s);ah.each(s,function(P,O){var T=new ah.fx(G,J,P);if(bc.test(O)){T[O==="toggle"?L?"show":"hide":O](s)}else{var R=aX.exec(O),V=T.cur(true)||0;if(R){O=parseFloat(R[2]);var Q=R[3]||"px";if(Q!=="px"){G.style[P]=(O||1)+Q;V=(O||1)/T.cur(true)*V;G.style[P]=V+Q}if(R[1]){O=(R[1]==="-="?-1:1)*O+V}T.custom(V,O,Q)}else{T.custom(V,O,"")}}});return true})},stop:function(s,c){var w=ah.timers;s&&this.queue([]);this.each(function(){for(var A=w.length-1;A>=0;A--){if(w[A].elem===this){c&&w[A](true);w.splice(A,1)}}});c||this.dequeue();return this}});ah.each({slideDown:aC("show",1),slideUp:aC("hide",1),slideToggle:aC("toggle",1),fadeIn:{opacity:"show"},fadeOut:{opacity:"hide"}},function(s,c){ah.fn[s]=function(A,w){return this.animate(c,A,w)}});ah.extend({speed:function(s,c,A){var w=s&&typeof s==="object"?s:{complete:A||!A&&c||ah.isFunction(s)&&s,duration:s,easing:A&&c||c&&!ah.isFunction(c)&&c};w.duration=ah.fx.off?0:typeof w.duration==="number"?w.duration:ah.fx.speeds[w.duration]||ah.fx.speeds._default;w.old=w.complete;w.complete=function(){w.queue!==false&&ah(this).dequeue();ah.isFunction(w.old)&&w.old.call(this)};return w},easing:{linear:function(s,c,A,w){return A+w*s},swing:function(s,c,A,w){return(-Math.cos(s*Math.PI)/2+0.5)*w+A}},timers:[],fx:function(s,c,w){this.options=c;this.elem=s;this.prop=w;if(!c.orig){c.orig={}}}});ah.fx.prototype={update:function(){this.options.step&&this.options.step.call(this.elem,this.now,this);(ah.fx.step[this.prop]||ah.fx.step._default)(this);if((this.prop==="height"||this.prop==="width")&&this.elem.style){this.elem.style.display="block"}},cur:function(c){if(this.elem[this.prop]!=null&&(!this.elem.style||this.elem.style[this.prop]==null)){return this.elem[this.prop]}return(c=parseFloat(ah.css(this.elem,this.prop,c)))&&c>-10000?c:parseFloat(ah.curCSS(this.elem,this.prop))||0},custom:function(s,c,F){function w(G){return A.step(G)}this.startTime=aF();this.start=s;this.end=c;this.unit=F||this.unit||"px";this.now=this.start;this.pos=this.state=0;var A=this;w.elem=this.elem;if(w()&&ah.timers.push(w)&&!aq){aq=setInterval(ah.fx.tick,13)}},show:function(){this.options.orig[this.prop]=ah.style(this.elem,this.prop);this.options.show=true;this.custom(this.prop==="width"||this.prop==="height"?1:0,this.cur());ah(this.elem).show()},hide:function(){this.options.orig[this.prop]=ah.style(this.elem,this.prop);this.options.hide=true;this.custom(this.cur(),0)},step:function(s){var c=aF(),F=true;if(s||c>=this.options.duration+this.startTime){this.now=this.end;this.pos=this.state=1;this.update();this.options.curAnim[this.prop]=true;for(var w in this.options.curAnim){if(this.options.curAnim[w]!==true){F=false}}if(F){if(this.options.display!=null){this.elem.style.overflow=this.options.overflow;s=ah.data(this.elem,"olddisplay");this.elem.style.display=s?s:this.options.display;if(ah.css(this.elem,"display")==="none"){this.elem.style.display="block"}}this.options.hide&&ah(this.elem).hide();if(this.options.hide||this.options.show){for(var A in this.options.curAnim){ah.style(this.elem,A,this.options.orig[A])}}this.options.complete.call(this.elem)}return false}else{A=c-this.startTime;this.state=A/this.options.duration;s=this.options.easing||(ah.easing.swing?"swing":"linear");this.pos=ah.easing[this.options.specialEasing&&this.options.specialEasing[this.prop]||s](this.state,A,0,1,this.options.duration);this.now=this.start+(this.end-this.start)*this.pos;this.update()}return true}};ah.extend(ah.fx,{tick:function(){for(var s=ah.timers,c=0;c<s.length;c++){s[c]()||s.splice(c--,1)}s.length||ah.fx.stop()},stop:function(){clearInterval(aq);aq=null},speeds:{slow:600,fast:200,_default:400},step:{opacity:function(c){ah.style(c.elem,"opacity",c.now)},_default:function(c){if(c.elem.style&&c.elem.style[c.prop]!=null){c.elem.style[c.prop]=(c.prop==="width"||c.prop==="height"?Math.max(0,c.now):c.now)+c.unit}else{c.elem[c.prop]=c.now}}}});if(ah.expr&&ah.expr.filters){ah.expr.filters.animated=function(c){return ah.grep(ah.timers,function(s){return c===s.elem}).length}}ah.fn.offset="getBoundingClientRect" in M.documentElement?function(s){var c=this[0];if(s){return this.each(function(F){ah.offset.setOffset(this,s,F)})}if(!c||!c.ownerDocument){return null}if(c===c.ownerDocument.body){return ah.offset.bodyOffset(c)}var A=c.getBoundingClientRect(),w=c.ownerDocument;c=w.body;w=w.documentElement;return{top:A.top+(self.pageYOffset||ah.support.boxModel&&w.scrollTop||c.scrollTop)-(w.clientTop||c.clientTop||0),left:A.left+(self.pageXOffset||ah.support.boxModel&&w.scrollLeft||c.scrollLeft)-(w.clientLeft||c.clientLeft||0)}}:function(N){var L=this[0];if(N){return this.each(function(O){ah.offset.setOffset(this,N,O)})}if(!L||!L.ownerDocument){return null}if(L===L.ownerDocument.body){return ah.offset.bodyOffset(L)}ah.offset.initialize();var K=L.offsetParent,G=L,J=L.ownerDocument,A,F=J.documentElement,c=J.body;G=(J=J.defaultView)?J.getComputedStyle(L,null):L.currentStyle;for(var w=L.offsetTop,s=L.offsetLeft;(L=L.parentNode)&&L!==c&&L!==F;){if(ah.offset.supportsFixedPosition&&G.position==="fixed"){break}A=J?J.getComputedStyle(L,null):L.currentStyle;w-=L.scrollTop;s-=L.scrollLeft;if(L===K){w+=L.offsetTop;s+=L.offsetLeft;if(ah.offset.doesNotAddBorder&&!(ah.offset.doesAddBorderForTableAndCells&&/^t(able|d|h)$/i.test(L.nodeName))){w+=parseFloat(A.borderTopWidth)||0;s+=parseFloat(A.borderLeftWidth)||0}G=K;K=L.offsetParent}if(ah.offset.subtractsBorderForOverflowNotVisible&&A.overflow!=="visible"){w+=parseFloat(A.borderTopWidth)||0;s+=parseFloat(A.borderLeftWidth)||0}G=A}if(G.position==="relative"||G.position==="static"){w+=c.offsetTop;s+=c.offsetLeft}if(ah.offset.supportsFixedPosition&&G.position==="fixed"){w+=Math.max(F.scrollTop,c.scrollTop);s+=Math.max(F.scrollLeft,c.scrollLeft)}return{top:w,left:s}};ah.offset={initialize:function(){var s=M.body,c=M.createElement("div"),G,A,F,w=parseFloat(ah.curCSS(s,"marginTop",true))||0;ah.extend(c.style,{position:"absolute",top:0,left:0,margin:0,border:0,width:"1px",height:"1px",visibility:"hidden"});c.innerHTML="<div style='position:absolute;top:0;left:0;margin:0;border:5px solid #000;padding:0;width:1px;height:1px;'><div></div></div><table style='position:absolute;top:0;left:0;margin:0;border:5px solid #000;padding:0;width:1px;height:1px;' cellpadding='0' cellspacing='0'><tr><td></td></tr></table>";s.insertBefore(c,s.firstChild);G=c.firstChild;A=G.firstChild;F=G.nextSibling.firstChild.firstChild;this.doesNotAddBorder=A.offsetTop!==5;this.doesAddBorderForTableAndCells=F.offsetTop===5;A.style.position="fixed";A.style.top="20px";this.supportsFixedPosition=A.offsetTop===20||A.offsetTop===15;A.style.position=A.style.top="";G.style.overflow="hidden";G.style.position="relative";this.subtractsBorderForOverflowNotVisible=A.offsetTop===-5;this.doesNotIncludeMarginInBodyOffset=s.offsetTop!==w;s.removeChild(c);ah.offset.initialize=ah.noop},bodyOffset:function(s){var c=s.offsetTop,w=s.offsetLeft;ah.offset.initialize();if(ah.offset.doesNotIncludeMarginInBodyOffset){c+=parseFloat(ah.curCSS(s,"marginTop",true))||0;w+=parseFloat(ah.curCSS(s,"marginLeft",true))||0}return{top:c,left:w}},setOffset:function(s,c,J){if(/static/.test(ah.curCSS(s,"position"))){s.style.position="relative"}var F=ah(s),G=F.offset(),w=parseInt(ah.curCSS(s,"top",true),10)||0,A=parseInt(ah.curCSS(s,"left",true),10)||0;if(ah.isFunction(c)){c=c.call(s,J,G)}J={top:c.top-G.top+w,left:c.left-G.left+A};"using" in c?c.using.call(s,J):F.css(J)}};ah.fn.extend({position:function(){if(!this[0]){return null}var s=this[0],c=this.offsetParent(),A=this.offset(),w=/^body|html$/i.test(c[0].nodeName)?{top:0,left:0}:c.offset();A.top-=parseFloat(ah.curCSS(s,"marginTop",true))||0;A.left-=parseFloat(ah.curCSS(s,"marginLeft",true))||0;w.top+=parseFloat(ah.curCSS(c[0],"borderTopWidth",true))||0;w.left+=parseFloat(ah.curCSS(c[0],"borderLeftWidth",true))||0;return{top:A.top-w.top,left:A.left-w.left}},offsetParent:function(){return this.map(function(){for(var c=this.offsetParent||M.body;c&&!/^body|html$/i.test(c.nodeName)&&ah.css(c,"position")==="static";){c=c.offsetParent}return c})}});ah.each(["Left","Top"],function(s,c){var w="scroll"+c;ah.fn[w]=function(F){var G=this[0],A;if(!G){return null}if(F!==I){return this.each(function(){if(A=o(this)){A.scrollTo(!s?F:ah(A).scrollLeft(),s?F:ah(A).scrollTop())}else{this[w]=F}})}else{return(A=o(G))?"pageXOffset" in A?A[s?"pageYOffset":"pageXOffset"]:ah.support.boxModel&&A.document.documentElement[w]||A.document.body[w]:G[w]}}});ah.each(["Height","Width"],function(s,c){var w=c.toLowerCase();ah.fn["inner"+c]=function(){return this[0]?ah.css(this[0],w,false,"padding"):null};ah.fn["outer"+c]=function(A){return this[0]?ah.css(this[0],w,false,A?"margin":"border"):null};ah.fn[w]=function(A){var F=this[0];if(!F){return A==null?null:this}if(ah.isFunction(A)){return this.each(function(G){var J=ah(this);J[w](A.call(this,G,J[w]()))})}return"scrollTo" in F&&F.document?F.document.compatMode==="CSS1Compat"&&F.document.documentElement["client"+c]||F.document.body["client"+c]:F.nodeType===9?Math.max(F.documentElement["client"+c],F.body["scroll"+c],F.documentElement["scroll"+c],F.body["offset"+c],F.documentElement["offset"+c]):A===I?ah.css(F,w):this.css(w,typeof A==="string"?A:A+"px")}});aO.jQuery=aO.$=ah})(window);
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
}
A.contains = A.containsValue = function(array, item)
{
	for (var i = 0, len = array.length; i < len; i++) 
		if (array[i] == item) return true;

	return false;
}
A.any = function(array, mapFn)
{
	for (var i = 0, len = array.length; i < len; i++) 
		if (mapFn(array[i])) return true;

	return false;
}
A.all = function(array, mapFn)
{
	for (var i = 0, len = array.length; i < len; i++)
		if (!mapFn(array[i])) return false;

	return true;
}
A.remove = function(array, from, to)
{
	var rest = array.slice((to || from) + 1 || array.length);
	array.length = from < 0 ? array.length + from : from;
	return array.push.apply(array, rest);
}
A.removeItem = function(array, item) {
    var pos = array.indexOf(item);
    if (pos != -1) array.splice(pos, 1);
    return array;
}
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
}
A.isEmpty = function(array)
{
	return !array || array.length == 0;
}
A.each = function(array, fn)
{
	if (!array) return;
	for (var i = 0, len = array.length; i < len; i++)
		fn(array[i]);
}
A.merge = function(a1, a2)
{
	A.each(a2, function(item) { a1.push(item); });
	return a1;
}
A.cat = function()
{
	var all = [];
	for (var i = 0, len = arguments.length; i < arguments.length; i++)
	{
		A.merge(all, arguments[i]);
	}
	return all;
}
A.clone = function(array)
{
	return array.slice();
}
A.sort = function(array, sortFn) {
    array.sort(sortFn);
    return array;
}
A.areEqual = function(array, other) {
    if (!(is.Array(array) && is.Array(other))) return false;
    if (array.length != other.length) return false;

    for (var i = 0; i < array.length; i++) {
        if (!A.containsValue(other, array[i])) return false;
    }
    return true;
}
A.take = function(array, count) {
    var take = array.length < count ? array.length : count;
    var to = array.slice(0, take);
    return to;
}
A.skip = function(array, count) {
    var skip = array.length < count ? array.length : count;
    var to = array.slice(skip);
    return to;
}
A.prepend = function(array, item) {
    array.unshift(item);
}
A.insert = function(array, index, item) {
    var removedItems = array.splice(index, 0, item);
    return array;
}
A.where = function(array, predicateFn) {
    var to = [];    
    for (var i = 0, len = array.length; i < len; i++)
        if (predicateFn(array[i], i)) to.push(array[i]);
    return to;
}

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
O.count = function(obj) 
{
    var i = 0;
    for (var key in obj) i++;
    return i;
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
O.take = function(obj, count) {
    var to = {}, i = 0;
    for (var key in obj) {
        if (i++ < count)
            to[key] = obj[key];
        else break;
    }
    return to;
}
O.skip = function(obj, count) {
    var to = {}, i = 0;
    for (var key in obj) {
        if (i++ >= count)
            to[key] = obj[key];
    }
    return to;
}
O.where = function(obj, predicateFn) {
    var to = {};
    for (var key in obj)
        if (predicateFn(key, obj[key])) to[key] = obj[key];
    return to;
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
