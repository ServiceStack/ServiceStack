"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var keyAliases = { className: 'class', htmlFor: 'for' };
function createElement(tagName, options, attrs) {
    var el = document.createElement(tagName);
    if (attrs) {
        for (var key in attrs) {
            el.setAttribute(keyAliases[key] || key, attrs[key]);
        }
    }
    if (options && options.insertAfter) {
        options.insertAfter.parentNode.insertBefore(el, options.insertAfter.nextSibling);
    }
    return el;
}
exports.createElement = createElement;
function showInvalidInputs() {
    // @ts-ignore
    var $this = this;
    var errorMsg = $this.getAttribute('data-invalid');
    if (errorMsg) {
        var isCheck = $this.type === "checkbox" || $this.type === "radio";
        var elFormCheck = isCheck ? parent($this.parentElement, 'form-check') : null;
        if (!isCheck)
            addClass($this, 'is-invalid');
        else
            addClass(elFormCheck || $this.parentElement, 'is-invalid form-control');
        var elNext = $this.nextElementSibling;
        var elLast = elNext && (elNext.getAttribute('for') === $this.id || elNext.tagName === "SMALL")
            ? (isCheck ? elFormCheck || elNext.parentElement : elNext)
            : $this;
        var elError = elLast != null && elLast.nextElementSibling && hasClass(elLast.nextElementSibling, 'invalid-feedback')
            ? elLast.nextElementSibling
            : createElement("div", { insertAfter: elLast }, { className: 'invalid-feedback' });
        elError.innerHTML = errorMsg;
    }
}
function parent(el, cls) {
    while (el != null && !hasClass(el, cls))
        el = el.parentElement;
    return el;
}
var hasClass = function (el, cls) {
    return (" " + el.className + " ").replace(/[\n\t\r]/g, " ").indexOf(" " + cls + " ") > -1;
};
var addClass = function (el, cls) {
    return !hasClass(el, cls) ? el.className = (el.className + " " + cls).trim() : null;
};
var removeClass = function (el, cls) {
    return hasClass(el, cls) ? el.className = (" " + el.className + " ").replace(el.className, "").trim() : null;
};
// init generic behavior to bootstrap elements
function bootstrap(el) {
    var els = (el || document).querySelectorAll('[data-invalid]');
    for (var i = 0; i < els.length; i++) {
        showInvalidInputs.call(els[i]);
    }
}
exports.bootstrap = bootstrap;
// polyfill IE9+
if (!Element.prototype.matches) {
    Element.prototype.matches = Element.prototype.msMatchesSelector ||
        Element.prototype.webkitMatchesSelector;
}
if (!Element.prototype.closest) {
    Element.prototype.closest = function (s) {
        var el = this;
        do {
            if (el.matches(s))
                return el;
            el = el.parentElement || el.parentNode;
        } while (el !== null && el.nodeType === 1);
        return null;
    };
}
exports.bindHandlers = function (handlers) {
    document.querySelectorAll('[data-click]').forEach(function (selected, i, all) {
        selected.addEventListener('click', function (evt) {
            var el = evt.target;
            var attr = el.getAttribute('data-click') ||
                el.closest('[data-click]').getAttribute('data-click');
            if (!attr)
                return;
            var pos = attr.indexOf(':');
            if (pos >= 0) {
                var cmd = attr.substring(0, pos);
                var data = attr.substring(pos + 1);
                var fn = handlers[cmd];
                if (fn) {
                    fn.apply(evt.target, data.split(','));
                }
            }
            else {
                var fn = handlers[attr];
                if (fn) {
                    fn.apply(evt.target, [].slice.call(arguments));
                }
            }
        });
    });
};
function bootstrapForm(form, options) {
    if (!form)
        return;
    form.onsubmit = function (evt) {
        evt.preventDefault();
        options.type = "bootstrap-v4";
        return ajaxSubmit(form, options);
    };
}
exports.bootstrapForm = bootstrapForm;
function clearErrors(f) {
    removeClass(f, 'has-errors');
    f.querySelectorAll('.error-summary').forEach(function (el) {
        el.innerHTML = "";
        el.style.display = "none";
    });
    f.querySelectorAll('[data-validation-summary]').forEach(function (el) {
        el.innerHTML = "";
    });
    f.querySelectorAll('.error').forEach(function (el) { return removeClass(el, 'error'); });
    f.querySelectorAll('.form-check.is-invalid [data-invalid]').forEach(function (el) {
        el.removeAttribute('data-invalid');
    });
    f.querySelectorAll('.form-check.is-invalid').forEach(function (el) { return removeClass(el, 'form-control'); });
    f.querySelectorAll('.is-invalid').forEach(function (el) {
        removeClass(el, 'is-invalid');
        el.removeAttribute('data-invalid');
    });
    f.querySelectorAll('.is-valid').forEach(function (el) { return removeClass(el, 'is-valid'); });
}
function ajaxSubmit(form, options) {
    if (options === void 0) { options = {}; }
    clearErrors(form);
    addClass(form, 'loading');
    var disableSel = options.onSubmitDisable == null
        ? "[type=submit]"
        : options.onSubmitDisable;
    if (disableSel != null && disableSel != "") {
        form.querySelectorAll(disableSel).forEach(function (el) {
            el.setAttribute('disabled', 'disabled');
        });
    }
    //ajax -> fetch
}
exports.ajaxSubmit = ajaxSubmit;
