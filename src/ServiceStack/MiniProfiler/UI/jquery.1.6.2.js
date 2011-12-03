window.jQuery = 
window['$'] = window['jquip'] = (function(){
    var win = window,   
    queryShimCdn = "http://cdnjs.cloudflare.com/ajax/libs/sizzle/1.4.4/sizzle.min.js",
    queryEngines = function(){ return win["Sizzle"] || win["qwery"]; },     
        doc = document, docEl = doc.documentElement, 
        scriptFns=[], load=[], sLoaded,
        runtil = /Until$/, rmultiselector = /,/,
        rparentsprev = /^(?:parents|prevUntil|prevAll)/,
        rtagname = /<([\w:]+)/,
        rclass = /[\n\t\r]/g,
        rspace = /\s+/,
        rdigit = /\d/,
        rnotwhite = /\S/,
        rReturn = /\r\n/g,
        rsingleTag = /^<(\w+)\s*\/?>(?:<\/\1>)?$/,
        rCRLF = /\r?\n/g,
        rselectTextarea = /^(?:select|textarea)/i,
        rinput = /^(?:color|date|datetime|datetime-local|email|hidden|month|number|password|range|search|tel|text|time|url|week)$/i,
        strim = String.prototype.trim, trim,
        trimLeft = /^\s+/,
        trimRight = /\s+$/,
        contains, sortOrder,
        guaranteedUnique = { children: true, contents: true, next: true, prev: true },
        toString = Object.prototype.toString,
        class2type = {},
        hasDup = false, baseHasDup = true,
        wrapMap = {
            option: [1, "<select multiple='multiple'>", "</select>"],
            legend: [1, "<fieldset>", "</fieldset>"],
            thead: [1, "<table>", "</table>"],
            tr: [2, "<table><tbody>", "</tbody></table>"],
            td: [3, "<table><tbody><tr>", "</tr></tbody></table>"],
            col: [2, "<table><tbody></tbody><colgroup>", "</colgroup></table>"],
            area: [1, "<map>", "</map>"],
            _default: [0, "", ""] },
        rComplexQuery = /[,\s.#\[>+]/, emptyArr = [],
        breaker = {},
        ArrayProto = Array.prototype, ObjProto = Object.prototype,
        hasOwn = ObjProto.hasOwnProperty,
        slice = ArrayProto.slice,
        push = ArrayProto.push,
        indexOf = ArrayProto.indexOf,
        nativeForEach = ArrayProto.forEach,
        nativeFilter = ArrayProto.filter,
        nativeIndexOf = ArrayProto.indexOf;

    if (rnotwhite.test("\xA0")){
        trimLeft = /^[\s\xA0]+/;
        trimRight = /[\s\xA0]+$/;
    }

    /**
     * @constructor
     * @param {Object|Element|string|Array.<string>} sel
     * @param {Object=} ctx
     */
    function J(sel, ctx){
        var ret;
        for(var i = 0, l = ctors.length; i < l; i++)
            if (ctors[i].apply(this, arguments)) return this;

        if (!sel) return this;
        if (isF(sel)){
            if (sLoaded) sel();
            else scriptFns.push(sel);
            return this;
        } else if (isA(sel)) return this['make'](sel);
        if (sel.nodeType || isWin(sel)) return this['make']([sel]);
        if (sel == "body" && !ctx && doc.body) {
            this['context'] = sel['context'];
            this[0] = doc.body;
            this.length = 1;
            this['selector'] = sel;
            return this;
        }
        if (sel['selector'] !== undefined) {
            this['context'] = sel['context'];
            this['selector'] = sel['selector'];
            return this['make'](sel);
        }
        sel = isS(sel) && sel.charAt(0) === "<"
            ? (ret = rsingleTag.exec(sel))
                ? (sel = [doc.createElement(ret[1])]) && isPlainObj(ctx)
                    ? $["fn"]["attr"].call(sel, ctx) && sel
                    : sel
                : htmlFrag(sel).childNodes
            : $$(sel, ctx);
        return this['make'](sel);
    }

    var ctors=[], plugins={}, jquid=0, _cache={_id:0}, _display = {}, p;
    function $(sel, ctx){
        return new J(sel, ctx);
    }

    p = J.prototype = $.prototype = $['fn'] = {
        constructor: $,
        'selector': "",
        'length': 0,
        dm: function(args, tbl, cb){
            var value = args[0], parent, frag, first, i;
            if (value){
                if (this[0]) {
                    if (!(frag = value.nodeType === 3 && value)){
                        parent = value && value.parentNode;
                        frag = parent && parent.nodeType === 11 && parent.childNodes.length === this.length
                            ? parent
                            : htmlFrag(value);
                        first = frag.firstChild;
                        if (frag.childNodes.length === 1) frag = first;
                        if (!first) return this;
                    }
                    for (i=0, l=this.length; i<l; i++)
                        cb.call(this[i],frag);
                }
            }
            return this;
        },
        /**
         * @param {Object} els
         * @param {string=} name
         * @param {string=} selector
         * */
        ps: function(els, name, selector){
            var ret = this.constructor();
            if (isA(els)) push.apply(ret, els);
            else merge(ret, els);
            ret.prevObject = this;
            ret.context = this.context;
            if (name === "find")
                ret.selector = this['selector'] + (this['selector'] ? " " : "") + selector;
            else if (name)
                ret.selector = this['selector'] + "." + name + "(" + selector + ")";
            return ret;
        }
    };

    p['make'] = function(els){
        make(this, els);
        return this;
    };
    p['toArray'] = function() {
        return slice.call(this, 0);
    };
    p['get'] = function(num){
        return num == null
            ? this['toArray']()
            : (num < 0 ? this[this.length + num] : this[num]);
    };
    p['add'] = function(sel, ctx){
        var set = typeof sel == "string"
            ? $(sel, ctx)
            : makeArray(sel && sel.nodeType ? [sel] : sel),
            all = merge(this.get(), set);
        return this.ps(detached(set[0]) || detached(all[0]) ? all : unique(all));
    };
    function detached(el) {
        return !el || !el.parentNode || el.parentNode.nodeType == 11;
    }
    p['each'] = function(fn){
            if (!isF(fn)) return this;
            for(var i = 0, l = this.length; i < l; i++)
                fn.call(this[i], i, this[i]);
            return this;
    };
    p['attr'] = function(name, val){
        var el = this[0];
        return (isS(name) && val === undefined)
            ? attr(el, name)
            : this['each'](function(idx){
                var nt = this.nodeType;
                if (nt !== 3 && nt !== 8 && nt !== 2){
                    if (isO(name)) for(var k in name)
                        if (val === null)
                            this.removeAttribute(name);
                        else
                            this.setAttribute(k, name[k]);
                    else this.setAttribute(name, isF(val) ? val.call(this, idx, this.getAttribute(name)) : val);
            }
        });
    };
    p['removeAttr'] = function(name){
        return this['each'](function(){
            this.removeAttribute(name);
        });
    };
    p['data'] = function(name, setVal){
        return data(this[0], name, setVal);
    };
    p['append'] = function(){
        return this.dm(arguments, true, function(el){
            if (this.nodeType === 1)
                this.appendChild(el);
        });
    };
    p['prepend'] = function(){
        return this.dm(arguments, true, function(el){
            if (this.nodeType === 1)
                this.insertBefore(el, this.firstChild);
        });
    };
    p['before'] = function(){
        return this.dm(arguments, false, function(el){
            this.parentNode.insertBefore(el, this);
        });
    };
    p['after'] = function(){
        if (this[0] && this[0].parentNode){
            return this.dm(arguments, false, function(el){
                this.parentNode.insertBefore(el, this.nextSibling);
            });
        }
        return this;
    };
    p['hide'] = function(){
        return this['each'](function(){
            cache(this, "display", this.style.display);
            this.style.display = "none";
        });
    };
    p['show'] = function(){
        return this['each'](function(){
            this.style.display = cache(this, "display") || display(this.tagName);
        });
    };
    p['toggle'] = function(){
        return this['each'](function(){
            this.style.display = $['Expr']['hidden'](this)
                ? cache(this, "display") || display(this.tagName)
                : (cache(this, "display", this.style.display), "none");
        });
    };
    p['eq'] = function(i){
        return i === -1 ? this.slice(i) : this.slice(i, +i + 1);
    };
    p['first'] = function(){
        return this['eq'](0);
    };
    p['last'] = function(){
        return this['eq'](-1);
    };
    p['slice'] = function(){
        return this.ps(slice.apply(this, arguments),
            "slice", slice.call(arguments).join(","));
    };
    p['map'] = function(cb) {
        return this.ps(map(this, function(el, i) {
            return cb.call(el, i, el);
        }));
    };
    p['find'] = function(sel){
        var self = this, i, l;
        if (!isS(sel)){
            return $(sel).filter(function(){
                for(i = 0, l = self.length; i < l; i++)
                    if (contains(self[i], this)) return true;
            });
        }
        var ret = this.ps("", "find", sel), len, n, r;
        for(i=0, l=this.length; i<l; i++){
            len = ret.length;
            merge(ret, $(sel, this[i]));
            if (i == 0){
                for(n = len; n < ret.length; n++)
                    for(r = 0; r < len; r++)
                        if (ret[r] === ret[n]){
                            ret.splice(n--, 1);
                            break;
                        }
            }
        }
        return ret;
    };
    p['not'] = function(sel){
        return this.ps(winnow(this, sel, false), "not", sel);
    };
    p['filter'] = function(sel){
        return this.ps(winnow(this, sel, true), "filter", sel);
    };
    p['indexOf'] = function(val){
        return _indexOf(this, val);
    };
    p['is'] = function(sel){
        return this.length > 0 && $(this[0]).filter(sel).length > 0;
    };
    p['remove'] = function(){
        for(var i = 0, el; (el = this[i]) != null; i++) if (el.parentNode) el.parentNode.removeChild(el);
        return this;
    };
    p['closest'] = function(sel, ctx) {
        var ret=[], i;
        for (i=0, l=this.length; i<l; i++){
            cur = this[i];
            while (cur){
                if (filter(sel, [cur]).length>0){
                    ret.push(cur);
                    break;
                }else{
                    cur = cur.parentNode;
                    if (!cur || !cur.ownerDocument || cur === ctx|| cur.nodeType === 11)
                        break;
                }
            }
        }
        ret = ret.length > 1 ? unique(ret) : ret;
        return this.ps(ret, "closest", sel);
    };
    p['val'] = function(setVal){
        if (setVal == null) return (this[0] && this[0].value) || "";
        return this['each'](function(){
            this.value = setVal;
        });
    };
    p['html'] = function(setHtml){
        if (setHtml == null) return (this[0] && this[0].innerHTML) || "";
        return this['each'](function(){
            this.innerHTML = setHtml;
        });
    };
    p['text'] = function(val){
        var el = this[0], nt;
        return typeof val == "undefined"
            ? (el && (nt = el.nodeType)
                ? ((nt === 1 || nt === 9)
                    ? (typeof el.textContent == "string" ? el.textContent : el.innerText.replace(rReturn, ''))
                    : (nt === 3 || nt === 4) ? el.nodeValue : null)
                : null)
            : this['empty']()['append']((el && el.ownerDocument || doc).createTextNode(val));
    };
    p['empty'] = function(){
        for(var i = 0, el; (el = this[i]) != null; i++)
            while (el.firstChild)
                el.removeChild(el.firstChild);
        return this;
    };
    p['addClass'] = function(val){
        var cls, i, l, el, setClass, c, cl;
        if (isF(val))
            return this['each'](function(j){
                $(this)['addClass'](val.call(this, j, this.className));
            });
        if (val && isS(val)){
            cls = val.split(rspace);
            for(i = 0, l = this.length; i < l; i++){
                el = this[i];
                if (el && el.nodeType === 1){
                    if (!el.className && cls.length === 1)
                        el.className = val;
                    else {
                        setClass = " " + el.className + " ";
                        for(c = 0, cl = cls.length; c < cl; c++){
                            if (!~setClass.indexOf(" " + cls[c] + " "))
                                setClass += cls[c] + " ";
                        }
                        el.className = trim(setClass);
                    }
                }
            }
        }
        return this;
    };
    p['removeClass'] = function(val){
        var clss, i, l, el, cls, c, cl;
        if (isF(val)) return this['each'](function(j){
            $(this)['removeClass'](val.call(this, j, this.className));
        });
        if ((val && isS(val)) || val === undefined){
            clss = (val || "").split(rspace);
            for(i = 0, l = this.length; i < l; i++){
                el = this[i];
                if (el.nodeType === 1 && el.className){
                    if (val){
                        cls = (" " + el.className + " ").replace(rclass, " ");
                        for(c = 0, cl = clss.length; c < cl; c++)
                            cls = cls.replace(" " + clss[c] + " ", " ");
                        el.className = trim(cls);
                    }
                    else el.className = "";
                }
            }
        }
        return this;
    };
    p['hasClass'] = function(sel){
        return hasClass(this, sel);
    };
    p['fadeIn'] = function(){
        this['each'](function(){
            $(this)['show']();
        });
    };
    p['fadeOut'] = function(){
        this['each'](function(){
            $(this)['hide']();
        });
    };
    p['serializeArray'] = function() {
        return this['map'](function(){
            return this.elements ? makeArray(this.elements) : this;
        }).filter(function(){
            return this.name && !this.disabled &&
                (this.checked || rselectTextarea.test(this.nodeName) || rinput.test(this.type));
        }).map(function(i, el){
            var val = $(this)['val']();
            return val == null || isA(val)
                ? map(val, function(val){
                    return { name: el.name, value: val.replace(rCRLF, "\r\n") };
                  })
                : { name: el.name, value: val.replace(rCRLF, "\r\n") };
        }).get();
    };

    $['Expr'] = {
        'hidden': function(el){
            return el.offsetWidth === 0 || el.offsetHeight == 0
                || (($["css"] && $["css"](el,"display") || el.style.display) === "none");
        },
        'visible': function(el) {
            return !$['Expr']['hidden'](el);
        }
    };

    function winnow(els, sel, keep){
        sel = sel || 0;
        if (isF(sel))
            return grep(els, function(el, i){
                return !!sel.call(el, i, el) === keep;
            });
        else if (sel.nodeType)
            return grep(els, function(el){
                return (el === sel) === keep;
            });
        else if (isS(sel)) {
            var expr = sel.charAt(0) == ":" && $['Expr'][sel.substring(1)];
            return grep(els, function(el) {
                return expr
                    ? expr(el)
                    : el.parentNode && _indexOf($$(sel, el.parentNode), el) >= 0
            });
        }
        return grep(els, function(el) {
            return (_indexOf(sel, el) >= 0) === keep;
        });
    }
    function cache(el, name, val)
    {
        var id = $['data'](el,"_J");
        if (typeof val === "undefined")
            return id && _cache[id] && _cache[id][name];
        if (!id) $['data'](el,"_J", (id=++_cache.id));
        return (_cache[id] || (_cache[id]={}))[name] = val;
    }
    function display(tag) {
        if (!_display[tag]) {
            var el = $("<" + tag + ">")['appendTo'](doc.body),
                d = ($['css'] && $['css'](el[0], "display")) || el[0].style.display;
            el.remove();
            _display[tag] = d;
        }
        return _display[tag];
    }
    function make(arr, els){
        arr.length = (els && els.length || 0);
        if (arr.length == 0) return arr;
        for(var i = 0, l = els.length; i < l; i++)
            arr[i] = els[i];
        return arr;
    }
    function hasClass(els, cls){
        var cls = " " + cls + " ";
        for(var i = 0, l = els.length; i < l; i++)
            if (eqClass(els[i], cls))
                return true;
        return false;
    } $['hasClass'] = hasClass;
    function eqClass(el, cls){
        return el.nodeType === 1 && (" " + el.className + " ").replace(rclass, " ").indexOf(cls) > -1;
    }
    function walk(fn, ctx, ret){
        ctx = ctx || doc;
        ret = ret || [];
        if (ctx.nodeType == 1)
            if (fn(ctx)) ret.push(ctx);
        var els = ctx.childNodes;
        for(var i = 0, l = els.length; i < l; i++){
            var subEl = els[i];
            if (subEl.nodeType == 1)
                walk(fn, subEl, ret);
        }
        return ret;
    } $['walk'] = walk;

    /**
     * @param {string} html
     * @param {Object=} ctx
     * @param {Object=} qry
     * */
    function $$(sel, ctx, qry){
        if (sel && isS(sel)){
            if (ctx instanceof $) ctx = ctx[0];
            ctx = ctx || doc;
            qry = qry || $['query'];
            var firstChar = sel.charAt(0), arg = sel.substring(1), complex = rComplexQuery.test(arg), el;
            try{
                if (complex)
                    return slice.call(qry(sel, ctx));
                return complex
                    ? slice.call(qry(sel, ctx))
                    : (firstChar == "#"
                        ? ((el = doc.getElementById(arg)) ? [el] : emptyArr)
                        : makeArray(firstChar == "."
                            ? (ctx.getElementsByClassName ? ctx.getElementsByClassName(arg) : qry(sel, ctx))
                            : ctx.getElementsByTagName(sel))
                    );
            }catch(e){
                warn(e);
            }
        }
        return sel.nodeType == 1 || sel.nodeType == 9 ? [sel] : emptyArr;
    } $['$$'] = $$;

    $['setQuery'] = function(qry){
        $['query'] = function(sel, ctx){
            return $$(sel, ctx, (qry || function(sel, ctx){ return ctx.querySelectorAll(sel); }));
        };
    };

    var useQuery = queryEngines();
    $['setQuery'](useQuery || function(sel, ctx){
        return doc.querySelectorAll ? makeArray((ctx || doc).querySelectorAll(sel)) : [];
    });

    function loadScript(url, cb, async){
        var h = doc.head || doc.getElementsByTagName('head')[0] || docEl,
            s = doc.createElement('script'), rs;
        if (async) s.async = "async";
        s.onreadystatechange = function () {
            if (!(rs = s.readyState) || rs == "loaded" || rs == "complete"){
                s.onload = s.onreadystatechange = null;
                if (h && s.parentNode)
                    h.removeChild(s);
                s = undefined;
                if (cb) cb();
            }
        };
        s.onload = cb;
        s.src = url;
        h.insertBefore(s, h.firstChild);
    } $['loadScript'] = loadScript;

    /** @param {...string} var_args */
    function warn(var_args){ win.console && win.console.warn(arguments) }

    $['each'] = function(o, cb, args){
        var k, i = 0, l = o.length, isObj = l === undefined || isF(o);
        if (args){
            if (isObj) {
                for(k in o)
                    if (cb.apply(o[k], args) === false) break;
            } else
                for(; i < l;)
                    if (cb.apply(o[i++], args) === false) break;
        } else {
            if (isObj) {
                for(k in o)
                    if (cb.call(o[k], k, o[k]) === false) break;
            }
            else
                for(; i < l;)
                    if (cb.call(o[i], i, o[i++]) === false) break;
        }
        return o;
    };
    function _each(o, fn, ctx){
        if (o == null) return;
        if (nativeForEach && o.forEach === nativeForEach)
            o.forEach(fn, ctx);
        else if (o.length === +o.length){
            for(var i = 0, l = o.length; i < l; i++)
                if (i in o && fn.call(ctx, o[i], i, o) === breaker) return;
        } else {
            for(var key in o)
                if (hasOwn.call(o, key))
                    if (fn.call(ctx, o[key], key, o) === breaker) return;
        }
    } $['_each'] = _each;
    function attr(el, name) {
        return (el && el.nodeName === 'INPUT' && el.type === 'text' && name === 'value')
            ? el.value
            : (el ? (el.getAttribute(name) || (name in el ? el[name] : undefined)) : null);
    }
    var rfilter = [ //[ID, TAG, CLASS, ATTR]
        /#((?:[\w\u00c0-\uFFFF\-]|\\.)+)/,
        /^((?:[\w\u00c0-\uFFFF\*\-]|\\.)+)/,
        /\.((?:[\w\u00c0-\uFFFF\-]|\\.)+)/,
        /\[\s*((?:[\w\u00c0-\uFFFF\-]|\\.)+)\s*(?:(\S?=)\s*(?:(['"])(.*?)\3|(#?(?:[\w\u00c0-\uFFFF\-]|\\.)*)|)|)\s*\]/
    ];
    function filter(sel, els) {
        var ret = [], i, j, l, el, m;
        for (i = 0, l = rfilter.length; i < l; i++)
            if (m = rfilter[i].exec(sel)) break;
        if (i < rfilter.length){
            for (j = 0; (el = els[j]); j++)
                if ((i == 0 && m[1] == el.id)
                   || (i == 1 && eqSI(m[1], el.tagName))
                   || (i == 2 && eqClass(el, m[1]))
                   || (i == 3 && m[2] == attr(el, m[1])))
                    ret.push(el);
        }
        else
            warn(sel + " not supported");
        return ret;
    } $['filter'] = filter;
    function _indexOf(arr, val){
        if (arr == null) return -1;
        var i, l;
        if (nativeIndexOf && arr.indexOf === nativeIndexOf) return arr.indexOf(val);
        for(i = 0, l = arr.length; i < l; i++) if (arr[i] === val) return i;
        return -1;
    } $['_indexOf'] = _indexOf;
    $['_defaults'] = function(obj){
        _each(slice.call(arguments, 1), function(o){
            for(var k in o)
                if (obj[k] == null) obj[k] = o[k];
        });
        return obj;
    };
    function _filter(o, fn, ctx){
        var ret = [];
        if (o == null) return ret;
        if (nativeFilter && o.filter === nativeFilter) return o.filter(fn, ctx);
        _each(o, function(val, i, arr){
            if (fn.call(ctx, val, i, arr)) ret[ret.length] = val;
        });
        return ret;
    } $['_filter'] = _filter;
    $['proxy'] = function(fn, ctx){
        if (typeof ctx == "string"){
            var tmp = fn[ctx];
            ctx = fn;
            fn = tmp;
        }
        if (isF(fn)){
            var args = slice.call(arguments, 2),
                proxy = function(){
                    return fn.apply(ctx, args.concat(slice.call(arguments))); };
            proxy.guid = fn.guid = fn.guid || proxy.guid || jquid++;
            return proxy;
        }
    };
    function dir(el, dir, until){
        var matched = [], cur = el[dir];
        while (cur && cur.nodeType !== 9 && (until === undefined || cur.nodeType !== 1 || !$(cur).is(until))){
            if (cur.nodeType === 1) matched.push(cur);
            cur = cur[dir];
        }
        return matched;
    } $['dir'] = dir;
    function nth(cur, res, dir){
        res = res || 1;
        var num = 0;
        for(; cur; cur = cur[dir])
            if (cur.nodeType === 1 && ++num === res) break;
        return cur;
    } $['nth'] = nth;
    function sibling(n, el){
        var r = [];
        for(; n; n = n.nextSibling) if (n.nodeType === 1 && n !== el) r.push(n);
        return r;
    } $['sibling'] = sibling;

    function grep(els, cb, inv){
        var ret = [], retVal;
        inv = !!inv;
        for(var i=0, l=els.length; i<l; i++){
            retVal = !!cb(els[i], i);
            if (inv !== retVal)
                ret.push(els[i]);
        }
        return ret;
    } $['grep'] = grep;
    /**
     * @param {Object} els
     * @param {function} cb
     * @param {Object=} arg
     * */
    function map(els, cb, arg){
        var value, key, ret = [], i = 0, length = els.length,
            isArray = els instanceof $
                || typeof length == "number"
                && ((length > 0 && els[0] && els[length - 1]) || length === 0 || isA(els));
        if (isArray){
            for(; i < length; i++){
                value = cb(els[i], i, arg);
                if (value != null)
                    ret[ret.length] = value;
            }
        } else {
            for(key in els){
                value = cb(els[key], key, arg);
                if (value != null)
                    ret[ret.length] = value;
            }
        }
        return ret.concat.apply([], ret);
    } $['map'] = map;
    function data(el, name, setVal){
        if (!el) return {};
        if (name && setVal){
            el.setAttribute(name, setVal);
            return null;
        }
        var o = {};
        _each(attrs(el), function(val, aName){
            if (aName.indexOf("data-") !== 0 || !val) return;
            o[aName.substr("data-".length)] = val;
        });
        if (isS(name)) return o[name];
        return o;
    } $['data'] = data;
    function attrs(el){
        var o = {};
        for(var i = 0, elAttrs = el.attributes, len = elAttrs.length; i < len; i++)
            o[elAttrs.item(i).nodeName] = elAttrs.item(i).nodeValue;
        return o;
    } $['attrs'] = attrs;
    function eqSI(str1, str2){
        return !str1 || !str2 ? str1 == str2 : str1.toLowerCase() === str2.toLowerCase();
    } $['eqSI'] = eqSI;
    $['trim'] = trim = strim
        ? function(text){ return text == null ? "" : strim.call(text); }
        : function(text){ return text == null ? "" : text.toString().replace(trimLeft, "").replace(trimRight, ""); };
    $['indexOf'] = $['inArray'] = function(el, arr){
        if (!arr) return -1;
        if (indexOf) return indexOf.call(arr, el);
        for(var i = 0, length = arr.length; i < length; i++)
            if (arr[i] === el)
                return i;
        return -1;
    };
    _each("Boolean Number String Function Array Date RegExp Object".split(" "), function(name){
        class2type["[object " + name + "]"] = name.toLowerCase();
        return this;
    });

    function typeOf(o){ return o == null ? String(o) : class2type[toString.call(o)] || "object"; } $['type'] = typeOf;
    function isS(o){ return typeof o == "string"; }
    function isO(o){ return typeof o == "object"; }
    function isF(o){ return typeof o == "function" || typeOf(o) === "function"; } $['isFunction'] = isF;
    function isA(o){ return typeOf(o) === "array"; } $['isArray'] = Array.isArray || isA;
    function isAL(o){ return !isS(o) && typeof o.length == 'number' }
    function isWin(o){ return o && typeof o == "object" && "setInterval" in o; } $['isWindow'] = isWin;
    function isNan(obj){ return obj == null || !rdigit.test(obj) || isNaN(obj); } $['isNaN'] = isNan;
    function isPlainObj(o){
        if (!o || typeOf(o) !== "object" || o.nodeType || isWin(o)) return false;
        try{
            if (o.constructor && !hasOwn.call(o, "constructor") && !hasOwn.call(o.constructor.prototype, "isPrototypeOf"))
                return false;
        }catch(e){
            return false;
        }
        var key;
        for(key in o){}
        return key === undefined || hasOwn.call(o, key);
    }
    function merge(a1, a2){
        var i = a1.length, j = 0;
        if (typeof a2.length == "number")
            for(var l = a2.length; j < l; j++)
                a1[i++] = a2[j];
        else
            while (a2[j] !== undefined)
                a1[i++] = a2[j++];
        a1.length = i;
        return a1;
    } $['merge'] = merge;
    function extend(){
        var opt, name, src, copy, copyIsArr, clone, args = arguments,
            dst = args[0] || {}, i = 1, aLen = args.length, deep = false;
        if (typeof dst == "boolean"){ deep = dst; dst = args[1] || {}; i = 2; }
        if (typeof dst != "object" && !isF(dst)) dst = {};
        if (aLen === i){ dst = this; --i; }
        for(; i < aLen; i++){
            if ((opt = args[i]) != null){
                for(name in opt){
                    src = dst[name];
                    copy = opt[name];
                    if (dst === copy) continue;
                    if (deep && copy && (isPlainObj(copy) || (copyIsArr = isA(copy)))){
                        if (copyIsArr){
                            copyIsArr = false;
                            clone = src && isA(src) ? src : [];
                        } else
                            clone = src && isPlainObj(src) ? src : {};
                        dst[name] = extend(deep, clone, copy);
                    } else if (copy !== undefined)
                        dst[name] = copy;
                }
            }
        }
        return dst;
    } $['extend'] = $['fn']['extend'] = extend;
    function makeArray(arr, res){
        var ret = res || [];
        if (arr != null){
            var type = typeOf(arr);
            if (arr.length == null || type == "string" || type == "function" || type === "regexp" || isWin(arr))
                push.call(ret, arr);
            else
                merge(ret, arr);
        }
        return ret;
    } $['makeArray'] = makeArray;
    /**
     * @param {string} html
     * @param {Object=} ctx
     * @param {Object=} frag
     * */
    function htmlFrag(html, ctx, frag){
        ctx = ((ctx || doc) || ctx.ownerDocument || ctx[0] && ctx[0].ownerDocument || doc);
        frag = frag || ctx.createDocumentFragment();
        if (isAL(html))
            return clean(html, ctx, frag) && frag;
        var div = fragDiv(html);
        while (div.firstChild)
            frag.appendChild(div.firstChild);
        return frag;
    } $['htmlFrag'] = htmlFrag;
    /**
     * @param {string} html
     * @param {Object=} ctx
     * */
    function fragDiv(html, ctx){
        var div = (ctx||doc).createElement('div'),
            tag = (rtagname.exec(html) || ["", ""])[1].toLowerCase(),
            wrap = wrapMap[tag] || wrapMap._default,
            depth = wrap[0];
        div.innerHTML = wrap[1] + html + wrap[2];
        while (depth--)
            div = div.lastChild;
        return div;
    }
    function clean(els, ctx, frag){
        var ret=[],i,el;
        for (i=0; (el=els[i])!=null; i++){
            if (isS(el))
                el = fragDiv(el, ctx);
            if (el.nodeType)
                ret.push(el);
            else
                ret = merge(ret, el);
        }
        if (frag)
            for (i=0; i<ret.length; i++)
                if (ret[i].nodeType)
                    frag.appendChild(ret[i]);
        return ret;
    }
    var sibChk = function(a, b, ret){
        if (a === b) return ret;
        var cur = a.nextSibling;
        while (cur){
            if (cur === b) return -1;
            cur = cur.nextSibling;
        }
        return 1;
    };
    contains = $['contains'] = docEl.contains
        ? function(a, b){
            return a !== b && (a.contains ? a.contains(b) : true); }
        : function(){ return false };
    sortOrder = docEl.compareDocumentPosition
        ? (contains = function(a, b){ return !!(a.compareDocumentPosition(b) & 16); }) //assigning contains
          && function(a, b){
            if (a === b){ hasDup = true; return 0; }
            if (!a.compareDocumentPosition || !b.compareDocumentPosition)
                return a.compareDocumentPosition ? -1 : 1;
            return a.compareDocumentPosition(b) & 4 ? -1 : 1;
          }
        : function(a, b){
            if (a === b){ hasDup = true; return 0; }
            else if (a.sourceIndex && b.sourceIndex) return a.sourceIndex - b.sourceIndex;
            var al, bl, ap = [], bp = [], aup = a.parentNode, bup = b.parentNode, cur = aup;
            if (aup === bup) return sibChk(a, b);
            else if (!aup) return -1;
            else if (!bup) return 1;
            while (cur){ ap.unshift(cur); cur = cur.parentNode; }
            cur = bup;
            while (cur){ bp.unshift(cur); cur = cur.parentNode; }
            al = ap.length;
            bl = bp.length;
            for(var i = 0; i < al && i < bl; i++)
                if (ap[i] !== bp[i]) return sibChk(ap[i], bp[i]);
            return i === al ? sibChk(a, bp[i], -1) : sibChk(ap[i], b, 1);
         };
    function unique(els){
        if (sortOrder){
            hasDup = baseHasDup;
            els.sort(sortOrder);
            if (hasDup)
                for(var i = 1; i < els.length; i++)
                    if (els[i] === els[i - 1]) els.splice(i--, 1);
        }
        return els;
    } $['unique'] = unique;
    _each({
        'parent': function(el){ var parent = el.parentNode; return parent && parent.nodeType !== 11 ? parent : null; },
        'parents': function(el){ return dir(el, "parentNode"); },
        'parentsUntil': function(el, i, until){ return dir(el, "parentNode", until); },
        'next': function(el){ return nth(el, 2, "nextSibling"); },
        'prev': function(el){ return nth(el, 2, "previousSibling"); },
        'nextAll': function(el){ return dir(el, "nextSibling"); },
        'prevAll': function(el){ return dir(el, "previousSibling"); },
        'nextUntil': function(el, i, until){ return dir(el, "nextSibling", until); },
        'prevUntil': function(el, i, until){ return dir(el, "previousSibling", until); },
        'siblings': function(el){ return sibling(el['parentNode']['firstChild'], el); },
        'children': function(el){ return sibling(el['firstChild']); },
        'contents': function(el){
            return el['nodeName'] === "iframe" ? el['contentDocument'] || el['contentWindow']['document '] : makeArray(el['childNodes']);
        }
    }, function(fn, name){
        $['fn'][name] = function(until, sel){
            var ret = map(this, fn, until), args = slice.call(arguments);
            if (!runtil.test(name)) sel = until;
            if (typeof sel == "string") 
                ret = filter(sel, ret);
            ret = this.length > 1 && !guaranteedUnique[name] ? unique(ret) : ret;
            if ((this.length > 1 || rmultiselector.test(sel)) && rparentsprev.test(name)) ret = ret.reverse();
            return this.ps(ret, name, args.join(","));
        };
    });
    _each({
        'appendTo': "append",
        'prependTo': "prepend",
        'insertBefore': "before",
        'insertAfter': "after"
    }, function(orig, name) {
        $['fn'][name] = function(sel){
            var ret = [], to = $(sel), i, els,
                p = this.length === 1 && this[0].parentNode;
            if (p && p.nodeType === 11 && p.childNodes.length === 1 && to.length === 1) {
                to[orig](this[0]);
                return this;
            }else{
                for(i=0, l=to.length; i<l; i++){
                    els = (i > 0 ? this.clone(true) : this).get();
                    $(to[i])[orig](els);
                    ret = ret.concat(els);
                }
                return this.ps(ret, name, to['selector']);
            }
        };
    });

    function boxmodel(){
        if (!doc.body) return null; //in HEAD
        var d = doc.createElement('div');
        doc.body.appendChild(d);
        d.style.width = '20px';
        d.style.padding = '10px';
        var w = d.offsetWidth;
        doc.body.removeChild(d);
        return w == 40;
    }

    (function(){
        var div = document.createElement("div");
        div.style.display = "none";
        div.innerHTML = "   <link/><table></table><a href='/a' style='color:red;float:left;opacity:.55;'>a</a><input type='checkbox'/>";
        var a = div.getElementsByTagName("a")[0];
        $['support'] = {
            boxModel: null,
            opacity: /^0.55$/.test(a.style.opacity),
            cssFloat: !!a.style.cssFloat
        };

        var rwebkit = /(webkit)[ \/]([\w.]+)/,
            ropera = /(opera)(?:.*version)?[ \/]([\w.]+)/,
            rmsie = /(msie) ([\w.]+)/,
            rmozilla = /(mozilla)(?:.*? rv:([\w.]+))?/,
            ua = navigator.userAgent.toLowerCase(),
            match = rwebkit.exec(ua)
                 || ropera.exec( ua )
                 || rmsie.exec( ua )
                 || ua.indexOf("compatible") < 0 && rmozilla.exec( ua ) || [],
            b;
        b = $['browser'] = { version: match[2] || "0" };
        b[match[1] || ""] = true;
    })();
    $['scriptsLoaded'] = function(cb) {
        if (isF(cb)) scriptFns.push(cb);
    }
    function loadAsync(url, cb){
        load.push({url:url,cb:cb});
    }; $['loadAsync'] = loadAsync;

    if (!useQuery && !doc.querySelectorAll) 
        loadAsync(queryShimCdn, function(){
            $['setQuery'](queryEngines());
        });

    function fireSL(){
        _each(scriptFns, function(cb){
            cb();
        });
        sLoaded = true;
    }

    $['init'] = false;
    $['onload'] = function(){
        if (!$['init'])
        try {
            $['support']['boxModel'] = boxmodel();
            var cbs = 0;
            _each(load, function(o){
                cbs++;
                loadScript(o.url, function(){
                    try { if (o.cb) o.cb(); } catch(e){}
                    if (!--cbs)fireSL();
                });
            });
            $['init'] = true;
            if (!cbs)fireSL();
        } catch(e){
            warn(e);
        }
    };
    if (doc['body'] && !$['init']) 
        setTimeout($['onload'],1); //let plugins loadAsync

    $['hook'] = function(fn){
        ctors.push(fn);
    };
    $['plug'] = function(meta, fn){
        var name = isS(meta) ? meta : meta['name'];
        fn = isF(meta) ? meta : fn;
        if (!isF(fn)) throw "Plugin fn required";
        if (name && fn) plugins[name] = fn;
        fn($);
    };

    return $;
})();
$['plug']("ajax", function ($) {
    var xhrs = [
           function () { return new XMLHttpRequest(); },
           function () { return new ActiveXObject("Microsoft.XMLHTTP"); },
           function () { return new ActiveXObject("MSXML2.XMLHTTP.3.0"); },
           function () { return new ActiveXObject("MSXML2.XMLHTTP"); }
        ],
        _xhrf = null;
    function _xhr() {
        if (_xhrf != null) return _xhrf();
        for (var i = 0, l = xhrs.length; i < l; i++) {
            try {
                var f = xhrs[i], req = f();
                if (req != null) {
                    _xhrf = f;
                    return req;
                }
            } catch (e){}
        }
        return function () { };
    } $['xhr'] = _xhr;
    function _xhrResp(xhr, dataType) {
        dataType = dataType || xhr.getResponseHeader("Content-Type").split(";")[0];
        switch (dataType) {
            case "text/xml":
                return xhr.responseXML;
            case "json":
            case "text/json":
            case "application/json":
            case "text/javascript":
            case "application/javascript":
            case "application/x-javascript":
                return window.JSON ? window.JSON['parse'](xhr.responseText) : eval(xhr.responseText);
            default:
                return xhr.responseText;
        }
    } $['_xhrResp'] = _xhrResp;
    $['formData'] = function formData(o) {
        var kvps = [], regEx = /%20/g;
        for (var k in o) kvps.push(encodeURIComponent(k).replace(regEx, "+") + "=" + encodeURIComponent(o[k].toString()).replace(regEx, "+"));
        return kvps.join('&');
    };
    $['each']("ajaxStart ajaxStop ajaxComplete ajaxError ajaxSuccess ajaxSend".split(" "), function(i,o){
        $['fn'][o] = function(f){
            return this['bind'](o, f);
        };
    });

    function ajax(o) {
        var xhr = _xhr(), timer, n = 0;
        o = $['_defaults'](o, { 'userAgent': "XMLHttpRequest", 'lang': "en", 'type': "GET", 'data': null, 'contentType': "application/x-www-form-urlencoded", 'dataType': "application/json" });
        if (o.timeout) timer = setTimeout(function () { xhr.abort(); if (o.timeoutFn) o.timeoutFn(o.url); }, o.timeout);
        var cbCtx = $(o['context'] || document), evtCtx = cbCtx;
        xhr.onreadystatechange = function() {
            if (xhr.readyState == 4){
                if (timer) clearTimeout(timer);
                if (xhr.status < 300){
                    var res = _xhrResp(xhr, o.dataType);
                    if (o['success'])
                        o['success'](res);
                    evtCtx['trigger']("ajaxSuccess", [xhr, res, o]);
                }
                else {
                    if (o.error)
                        o.error(xhr, xhr.status, xhr.statusText);
                    evtCtx['trigger'](cbCtx, "ajaxError", [xhr, xhr.statusText, o]);
                }
                if (o['complete'])
                    o['complete'](xhr, xhr.statusText);
                evtCtx['trigger'](cbCtx, "ajaxComplete", [xhr, o]);
            }
            else if (o['progress']) o['progress'](++n);
        };
        var url = o['url'], data = null;
        var isPost = o['type'] == "POST" || o['type'] == "PUT";
        if (!isPost && o['data']) url += "?" + formData(o['data']);
        xhr.open(o['type'], url);

        if (isPost) {
            var isJson = o['dataType'].indexOf("json") >= 0;
            data = isJson ? JSON.stringify(o['data']) : formData(o['data']);
            xhr.setRequestHeader("Content-Type", isJson ? "application/json" : "application/x-www-form-urlencoded");
        }
        xhr.send(data);
    } $['ajax'] = ajax;
    $['getJSON'] = function (url, data, success, error) {
        if ($['isFunction'](data)){
            error = success;
            success = data;
            data = null;
        }
        ajax({'url': url, 'data': data, 'success': success, 'dataType': 'json'});
    };
    $['get'] = function (url, data, success, dataType) {
        if ($['isFunction'](data)) {
            dataType = success;
            success = data;
            data = null;
        }
        ajax({'url': url, 'type': "GET", 'data': data, 'success': success, 'dataType': dataType || "text/plain"});
    };
    $['post'] = function (url, data, success, dataType) {
        if ($['isFunction'](data)) {
            dataType = success;
            success = data;
            data = null;
        }
        ajax({'url': url, 'type': "POST", 'data': data, 'success': success, 'dataType': dataType || "text/plain"});
    };

    if (!window.JSON)
        $['loadAsync']("http://ajax.cdnjs.com/ajax/libs/json2/20110223/json2.js");

    //TODO $.getScript
});
$['plug']("css", function ($) {
    var doc = document,
        docEl = doc.documentElement,
        ralpha = /alpha\([^)]*\)/i,
        ropacity = /opacity=([^)]*)/,
        rdashAlpha = /-([a-z])/ig,
        rupper = /([A-Z])/g,
        rnumpx = /^-?\d+(?:px)?$/i,
        rnum = /^-?\d/,
        rroot = /^(?:body|html)$/i,
        cssShow = { position: "absolute", visibility: "hidden", display: "block" },
        cssWidth = [ "Left", "Right" ],
        cssHeight = [ "Top", "Bottom" ],
        curCSS,
        getComputedStyle,
        currentStyle,
        fcamelCase = function (all, l) { return l.toUpperCase(); };
    
    $['cssHooks'] = {
        'opacity': {
            'get': function (el, comp) {
                if (!comp) return el.style.opacity;
                var ret = curCSS(el, "opacity", "opacity");
                return ret === "" ? "1" : ret;
            }
        }
    };
    $['_each'](["height", "width"], function(k) {
        $['cssHooks'][k] = {
            get: function(el, comp, extra) {
                var val;
                if (comp) {
                    if (el.offsetWidth !== 0)
                        return getWH(el, k, extra);

                    swap(el, cssShow, function() {
                        val = getWH( el, k, extra );
                    });
                    return val;
                }
            },
            set: function(el, val) {
                if (rnumpx.test(val)) {
                    val = parseFloat( val );

                    if (val >= 0)
                        return val + "px";
                } else
                    return val;
            }
        };
    });
    function getWH(el, name, extra) {
        var val = name === "width" ? el.offsetWidth : el.offsetHeight,
            which = name === "width" ? cssWidth : cssHeight;
        if (val > 0) {
            if (extra !== "border") {
                $['each']( which, function() {
                    if ( !extra )
                        val -= parseFloat(css(el, "padding" + this) ) || 0;
                    if ( extra === "margin" )
                        val += parseFloat(css(el, extra + this) ) || 0;
                    else
                        val -= parseFloat(css(el, "border" + this + "Width") ) || 0;
                });
            }
            return val + "px";
        }
        return "";
    }

    if (!$['support']['opacity']) {
        $['support']['opacity'] = {
            get: function (el, computed) {
                return ropacity.test((computed && el.currentStyle ? el.currentStyle.filter : el.style.filter) || "")
                    ? (parseFloat(RegExp.$1) / 100) + "" 
                    : computed ? "1" : "";
            },
            set: function (el, value) {
                var s = el.style;
                s.zoom = 1;
                var opacity = $['isNaN'](value) ? "" : "alpha(opacity=" + value * 100 + ")", filter = s.filter || "";
                s.filter = ralpha.test(filter) ?
                filter.replace(ralpha, opacity) :
                s.filter + ' ' + opacity;
            }
        };
    }        
    if (doc.defaultView && doc.defaultView.getComputedStyle) {
        getComputedStyle = function (el, newName, name) {
            var ret, defaultView, computedStyle;
            name = name.replace(rupper, "-$1").toLowerCase();
            if (!(defaultView = el.ownerDocument.defaultView)) return undefined;
            if ((computedStyle = defaultView.getComputedStyle(el, null))) {
                ret = computedStyle.getPropertyValue(name);
                if (ret === "" && !$['contains'](el.ownerDocument.documentElement, el))
                    ret = $['style'](el, name);
            }
            return ret;
        };
    }
    if (doc.documentElement.currentStyle) {
        currentStyle = function (el, name) {
            var left,
                ret = el.currentStyle && el.currentStyle[name],
                rsLeft = el.runtimeStyle && el.runtimeStyle[name],
                style = el.style;
            if (!rnumpx.test(ret) && rnum.test(ret)) {
                left = style.left;
                if (rsLeft) el.runtimeStyle.left = el.currentStyle.left;
                style.left = name === "fontSize" ? "1em" : (ret || 0);
                ret = style.pixelLeft + "px";
                style.left = left;
                if (rsLeft) el.runtimeStyle.left = rsLeft;
            }
            return ret === "" ? "auto" : ret;
        };
    }
    curCSS = getComputedStyle || currentStyle;

    $['fn']['css'] = function (name, value) {
        if (arguments.length === 2 && value === undefined) return this;

        return access(this, name, value, true, function (el, name, value) {
            return value !== undefined ? style(el, name, value) : css(el, name);
        });
    };
    $['cssNumber'] = { "zIndex": true, "fontWeight": true, "opacity": true, "zoom": true, "lineHeight": true };
    $['cssProps'] = { "float": $['support']['cssFloat'] ? "cssFloat" : "styleFloat" };
    function style(el, name, value, extra) {
        if (!el || el.nodeType === 3 || el.nodeType === 8 || !el.style) return;
        var ret, origName = camelCase(name), style = el.style, hooks = $['cssHooks'][origName];
        name = $['cssProps'][origName] || origName;
        if (value !== undefined) {
            if (typeof value === "number" && isNaN(value) || value == null) return;
            if (typeof value === "number" && !$['cssNumber'][origName]) value += "px";
            if (!hooks || !("set" in hooks) || (value = hooks.set(el, value)) !== undefined) {
                try {
                    style[name] = value;
                } catch (e) { }
            }
        } else {
            if (hooks && "get" in hooks && (ret = hooks.get(el, false, extra)) !== undefined)
                return ret;
            return style[name];
        }
    } $['style'] = style;
    function css(el, name, extra) {
        var ret, origName = camelCase(name), hooks = $['cssHooks'][origName];
        name = $['cssProps'][origName] || origName;
        if (hooks && "get" in hooks && (ret = hooks.get(el, true, extra)) !== undefined) return ret;
        else if (curCSS) return curCSS(el, name, origName);
    }$['css'] = css;
    function swap(el, opt, cb) {
        var old = {},k;
        for (var k in opt) {
            old[k] = el.style[k];
            el.style[k] = opt[k];
        }
        cb.call(el);
        for (k in opt) el.style[k] = old[k];
    }$['swap'] = swap;
    function camelCase(s) { return s.replace(rdashAlpha, fcamelCase); } $['camelCase'] = camelCase;
    function access(els, key, value, exec, fn, pass) {
        var l = els.length;
        if (typeof key === "object") {
            for (var k in key) {
                access(els, k, key[k], exec, fn, value);
            }
            return els;
        }
        if (value !== undefined) {
            exec = !pass && exec && $['isFunction'](value);
            for (var i = 0; i < l; i++)
                fn(els[i], key, exec ? value.call(els[i], i, fn(els[i], key)) : value, pass);
            return els;
        }
        return l ? fn(els[0], key) : undefined;
    }

    var init, noMarginBodyOff, subBorderForOverflow, suppFixedPos, noAddBorder, noAddBorderForTables,
        initialize = function() {
            if (init) return;
            var body = doc.body, c = doc.createElement("div"), iDiv, cDiv , table, td, bodyMarginTop = parseFloat(css(body, "marginTop")) || 0,
                html = "<div style='position:absolute;top:0;left:0;margin:0;border:5px solid #000;padding:0;width:1px;height:1px;'><div></div></div><table style='position:absolute;top:0;left:0;margin:0;border:5px solid #000;padding:0;width:1px;height:1px;' cellpadding='0' cellspacing='0'><tr><td></td></tr></table>";
            $['extend'](c.style, { position: "absolute", top: 0, left: 0, margin: 0, border: 0, width: "1px", height: "1px", visibility: "hidden" });
            c.innerHTML = html;
            body.insertBefore(c, body.firstChild);
            iDiv = c.firstChild;
            cDiv = iDiv.firstChild;
            td = iDiv.nextSibling.firstChild.firstChild;
            noAddBorder = (cDiv .offsetTop !== 5);
            noAddBorderForTables = (td.offsetTop === 5);
            cDiv .style.position = "fixed";
            cDiv .style.top = "20px";
            suppFixedPos = (cDiv .offsetTop === 20 || cDiv .offsetTop === 15);
            cDiv .style.position = cDiv .style.top = "";
            iDiv.style.overflow = "hidden";
            iDiv.style.position = "relative";
            subBorderForOverflow = (cDiv .offsetTop === -5);
            noMarginBodyOff = (body.offsetTop !== bodyMarginTop);
            body.removeChild(c);
            init = true;
        },
        bodyOffset = function(body){
            var top = body.offsetTop, left = body.offsetLeft;
            initialize();
            if (noMarginBodyOff){
                top  += parseFloat( css(body, "marginTop") ) || 0;
                left += parseFloat( css(body, "marginLeft") ) || 0;
            }
            return { top: top, left: left };
        };

    $['fn']['offset'] = function(){
        var el = this[0], box;
        if (!el || !el.ownerDocument) return null;
        if (el === el.ownerDocument.body) return bodyOffset(el);
        try {
            box = el.getBoundingClientRect();
        } catch(e) {}
        if (!box || !$['contains'](docEl, el))
            return box ? { top: box.top, left: box.left } : { top: 0, left: 0 };
        var body = doc.body,
            win = getWin(doc),
            clientTop  = docEl.clientTop  || body.clientTop  || 0,
            clientLeft = docEl.clientLeft || body.clientLeft || 0,
            scrollTop  = win['pageYOffset'] || $['support']['boxModel'] && docEl.scrollTop  || body.scrollTop,
            scrollLeft = win['pageXOffset'] || $['support']['boxModel'] && docEl.scrollLeft || body.scrollLeft,
            top  = box.top + scrollTop - clientTop,
            left = box.left + scrollLeft - clientLeft;
        return { top: top, left: left };
    };
    $['fn']['position'] = function() {
        if (!this[0]) return null;
        var el = this[0],
        offPar = this['offsetParent'](),
        off = this['offset'](),
        parOff = rroot.test(offPar[0].nodeName) ? { top: 0, left: 0 } : offPar['offset']();
        off.top -= parseFloat(css(el, "marginTop")) || 0;
        off.left -= parseFloat(css(el, "marginLeft")) || 0;
        parOff.top += parseFloat(css(offPar[0], "borderTopWidth")) || 0;
        parOff.left += parseFloat(css(offPar[0], "borderLeftWidth")) || 0;
        return { top: off.top - parOff.top, left: off.left - parOff.left };
    };
    $['fn']['offsetParent'] = function(){
        return this['map'](function(){
            var op = this.offsetParent || doc.body;
            while (op && (!rroot.test(op.nodeName) && css(op,"position") === "static"))
                op = op.offsetParent;
            return op;
        });
    };

    $['_each'](["Height", "Width"], function (name, i) {
        var type = name.toLowerCase();
        $['fn']["inner" + name] = function () {
            var el = this[0];
            return el && el.style ? parseFloat(css(el, type, "padding")) : null;
        };
        $['fn']["outer" + name] = function (margin) {
            var el = this[0];
            return el && el.style ? parseFloat(css(el, type, margin ? "margin" : "border")) : null;
        };
        $['fn'][type] = function (size) {
            var el = this[0];
            if (!el) return size == null ? null : this;
            if ($['isFunction'](size))
                return this.each(function (i) {
                    var self = $(this);
                    self[type](size.call(this, i, self[type]()));
                });
            if ($['isWindow'](el)) {
                var docElemProp = el.document.documentElement["client" + name], body = el.document.body;
                return el.document.compatMode === "CSS1Compat" && docElemProp || body && body["client" + name] || docElemProp;
            } else if (el.nodeType === 9) {
                return Math.max(
                    el.documentElement["client" + name],
                    el.body["scroll" + name], el.documentElement["scroll" + name],
                    el.body["offset" + name], el.documentElement["offset" + name]);
            } else if (size === undefined) {
                var orig = css(el, type),
                    ret = parseFloat(orig);
                return $['isNaN'](ret) ? orig : ret;
            }
            else return this['css'](type, typeof size === "string" ? size : size + "px");
        };
    });

    function getWin(el) { return $['isWindow'](el) ? el : el.nodeType === 9 ? el.defaultView || el.parentWindow : false; }

    $['_each'](["Left", "Top"], function (name, i) {
        var method = "scroll" + name;
        $['fn'][method] = function (val) {
            var el, win;
            if (val === undefined) {
                el = this[0];
                if (!el) return null;
                win = getWin(el);
                return win ? ("pageXOffset" in win)
                    ? win[i ? "pageYOffset" : "pageXOffset"]
                    : $['support']['boxModel'] && win.document.documentElement[method] || win.document.body[method] : el[method];
            }
            return this.each(function() {
                win = getWin(this);
                if (win)
                    win.scrollTo(!i ? val : $(win).scrollLeft(), i ? val : $(win).scrollTop());
                else
                    this[method] = val;
            });
        };
    });

});
$['plug']("custom", function($){
    var win=window, doc=document, qsMap = {}, 
        vars = win.location.search.substring(1).split("&");

    for (var i = 0; i < vars.length; i++) {
        var kvp = vars[i].split("=");
        qsMap[kvp[0]] = unescape(kvp[1]);
    }
    $['queryString'] = function (name) { return qsMap[name]; };
    var Key = $['Key'] = function (keyCode) { this.keyCode = keyCode; };
    Key.namedKeys = {
        Backspace: 8, Tab: 9, Enter: 13, Shift: 16, Ctrl: 17, Alt: 18, Pause: 19, Capslock: 20, Escape: 27, PageUp: 33, 
        PageDown: 34, End: 35, Home: 36, LeftArrow: 37, UpArrow: 38, RightArrow: 39, DownArrow: 40, Insert: 45, Delete: 46
    };
    $['_each'](Key.namedKeys, function (val, key) {
        var keyCode = val;
        Key.prototype['is' + key] = function () { return this.keyCode === keyCode; };
    });
    $.key = function (e) {
        e = e || window.event;
        return new Key(e.keyCode || e.which);
    };
    $['cancelEvent'] = function (e) {
        if (!e) e = window.event;
        e.cancelBubble = true;
        e.returnValue = false;
        if (e.stopPropagation) {
            e.stopPropagation();
            e.preventDefault();
        }
        return false;
    };
    $['templateSettings'] = {
      evaluate    : /<%([\s\S]+?)%>/g,
      interpolate : /<%=([\s\S]+?)%>/g,
      escape      : /<%-([\s\S]+?)%>/g
    };
    $['_template'] = function(str, data) {
        var c  = $['templateSettings'];
        var tmpl = 'var __p=[],print=function(){__p.push.apply(__p,arguments);};' +
          'with(obj||{}){__p.push(\'' +
          str.replace(/\\/g, '\\\\')
             .replace(/'/g, "\\'")
             .replace(c.escape, function(match, code) {
               return "',_.escape(" + code.replace(/\\'/g, "'") + "),'";
             })
             .replace(c.interpolate, function(match, code) {
               return "'," + code.replace(/\\'/g, "'") + ",'";
             })
             .replace(c.evaluate || null, function(match, code) {
               return "');" + code.replace(/\\'/g, "'")
                                  .replace(/[\r\n\t]/g, ' ') + ";__p.push('";
             })
             .replace(/\r/g, '\\r')
             .replace(/\n/g, '\\n')
             .replace(/\t/g, '\\t')
             + "');}return __p.join('');";
        var func = new Function('obj', '$', tmpl);
        return data ? func(data, $) : function(data) { return func(data, $) };
    };
});
$['plug']("docready", function ($) {
    var win = window, doc = document, DOMContentLoaded, readyBound, readyList = [], isReady = false, readyWait = 1;        
    $['hook'](function (sel, ctx) {
        if (typeof sel == "function") {
            this['ready'](sel);
            return true;
        }
    });
    function doScrollCheck() {
        if (isReady) return;
        try {
            doc.documentElement.doScroll("left");
        } catch (e) {
            setTimeout(doScrollCheck, 1);
            return;
        }
        ready();
    }
    function ready(wait) {
        if (wait === true) readyWait--;
        if (!readyWait || (wait !== true && !isReady)) {
            if (!doc.body) return setTimeout(ready, 1);
            isReady = true;
            if (wait !== true && --readyWait > 0) return;
            if (readyList) {
                var fn, i = 0, ready = readyList;
                readyList = null;
                while ((fn = ready[i++])) fn.call(doc, $);
                if ($['fn']['trigger']) $(doc)['trigger']("ready")['unbind']("ready");
            }
        }
    } $['ready'] = ready;
    DOMContentLoaded = doc.addEventListener
        ? function () {
            doc.removeEventListener("DOMContentLoaded", DOMContentLoaded, false);
            ready(); }
        : function () {
            if (doc.readyState === "complete") {
                doc.detachEvent("onreadystatechange", DOMContentLoaded);
                ready();
            }
        };
    $['bindReady'] = function() {
        if (readyBound) return;
        readyBound = true;
        if (doc.readyState === "complete") return setTimeout(ready, 1);

        if (doc.addEventListener) {
            doc.addEventListener("DOMContentLoaded", DOMContentLoaded, false);
            win.addEventListener("load", ready, false);
        } else if (doc.attachEvent) {
            doc.attachEvent("onreadystatechange", DOMContentLoaded);
            win.attachEvent("onload", ready);
            var toplevel = false;
            try { toplevel = window.frameElement == null; } catch (e) { }
            if (doc.documentElement.doScroll && toplevel) doScrollCheck();
        }
    };

    $['fn']['ready'] = function (fn) {
        $['bindReady']();
         if (isReady) fn.call(doc, $);
         else if (readyList) readyList.push(fn);
         return this;
     };

    if (!$['init']) $(document)['ready']($['onload']);
});
$['plug']("events", function($){
    var doc = document, handlers = {}, _jquid = 1;
    function jquid(el){
        return el._jquid || (el._jquid = _jquid++);
    }
    function bind(o, type, fn){
        if (o.addEventListener)
            o.addEventListener(type, fn, false);
        else {
            o['e' + type + fn] = fn;
            o[type + fn] = function(){
                o['e' + type + fn](window.event);
            };
            o.attachEvent('on' + type, o[type + fn]);
        }
    } $['bind'] = bind;
    function unbind(o, type, fn){
        if (o.removeEventListener)
            o.removeEventListener(type, fn, false);
        else {
            o.detachEvent('on' + type, o[type + fn]);
            o[type + fn] = null;
        }
    } $['unbind'] = unbind;
    function parseEvt(evt){
        var parts = ('' + evt).split('.');
        return {e: parts[0], ns: parts.slice(1).sort().join(' ')};
    }
    function matcherFor(ns){
        return new RegExp('(?:^| )' + ns.replace(' ', ' .* ?') + '(?: |$)');
    }
    function findHdls(el, evt, fn, sel){
        evt = parseEvt(evt);
        if (evt.ns) var m = matcherFor(evt.ns);
        return $['_filter'](handlers[jquid(el)] || [], function(hdl){
            return hdl
                && (!evt.e  || hdl.e == evt.e)
                && (!evt.ns || m.test(hdl.ns))
                && (!fn     || hdl.fn == fn)
                && (!sel    || hdl.sel == sel);
        });
    }
    function addEvt(el, evts, fn, sel, delegate){
        var id = jquid(el), set = (handlers[id] || (handlers[id] = []));
        $['_each'](evts.split(/\s/), function(evt){
            var handler = $['extend'](parseEvt(evt), {fn: fn, sel: sel, del: delegate, i: set.length});
            set.push(handler);
            bind(el, handler.e, delegate || fn);
        });
        el = null;
    }
    function remEvt(el, evts, fn, sel){
        var id = jquid(el);
        $['_each']((evts || '').split(/\s/), function(evt){
            $['_each'](findHdls(el, evt, fn, sel), function(hdl){
                delete handlers[id][hdl.i];
                unbind(el, hdl.e, hdl.del || hdl.fn);
            });
        });
    }
    var evtMethods = ['preventDefault', 'stopImmediatePropagation', 'stopPropagation'];
    function createProxy(evt){
        var proxy = $['extend']({originalEvent: evt}, evt);
        $['_each'](evtMethods, function(key){
            proxy[key] = function(){
                return evt[key].apply(evt, arguments);
            };
        });
        return proxy;
    }
    var p = $['fn'];
    $['_each'](("blur focus focusin focusout load resize scroll unload click dblclick " +
        "mousedown mouseup mousemove mouseover mouseout mouseenter mouseleave " +
        "change select submit keydown keypress keyup error").split(" "),
        function(name){
            p[name] = function(fn, data){
                return arguments.length > 0 ? this['bind'](name, fn, data) : this['trigger'](name);
            };
        }
    );
    p['bind'] = function(type, cb){
        return this['each'](function(){
            addEvt(this, type, cb);
        });
    };
    p['unbind'] = function(type, cb){
        return this['each'](function(){
             remEvt(this, type, cb);
        });
    };
    p['one'] = function(evt, cb){
        return this['each'](function(){
            var self = this;
            addEvt(this, evt, function wrapper(){
                cb();
                 remEvt(self, evt, arguments.callee);
            });
        });
    };
    p['delegate'] = function(sel, evt, cb){
        return this['each'](function(i, el){
            addEvt(el, evt, cb, sel, function(e){
                var target = e.target, nodes = $['$$'](sel, el);
                while (target && nodes.indexOf(target) < 0)
                    target = target.parentNode;
                if (target && !(target === el) && !(target === document)){
                    cb.call(target, $['extend'](createProxy(e||window.event), {
                        currentTarget: target, liveFired: el
                    }));
                }
            });
        });
    };
    p['undelegate'] = function(sel, evt, cb){
        return this['each'](function(){
             remEvt(this, evt, cb, sel);
        });
    };
    p['live'] = function(evt, cb){
        $(doc.body)['delegate'](this['selector'], evt, cb);
        return this;
    };
    p['die'] = function(evt, cb){
        $(doc.body)['undelegate'](this['selector'], evt, cb);
        return this;
    };
    p['trigger'] = function (evt) {
        return this['each'](function () {
            if ((evt == "click" || evt == "blur" || evt == "focus") && this[evt])
                return this[evt]();
            if (doc.createEvent) {
                var e = doc.createEvent('Events');
                this.dispatchEvent(e, e.initEvent(evt, true, true));
            } else if (this.fireEvent)
                try {
                    if (evt !== "ready") {
                        this.fireEvent("on" + evt);
                    }
                } catch (e) { }
        });
    };
    if (!$['init']) $(window)['bind']("load",$['onload']);
});