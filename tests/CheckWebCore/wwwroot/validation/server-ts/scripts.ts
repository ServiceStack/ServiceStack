export interface ICreateElementOptions {
  insertAfter?:Element|null
}

const keyAliases:{ [index:string]: string } = {className: 'class', htmlFor: 'for'};
export function createElement(tagName:string, options?:ICreateElementOptions, attrs?:any) {
  const el = document.createElement(tagName);
  if (attrs) {
    for (const key in attrs) {
      el.setAttribute(keyAliases[key] || key, attrs[key]);
    }
  }
  if (options && options.insertAfter) {
    options.insertAfter.parentNode!.insertBefore(el, options.insertAfter.nextSibling);
  }
  return el;
}
function showInvalidInputs() {
  // @ts-ignore
  const $this:HTMLInputElement = this;
  let errorMsg: string|null = $this.getAttribute('data-invalid');
  if (errorMsg) {
    const isCheck = $this.type === "checkbox" || $this.type === "radio";
    const elFormCheck = isCheck ? parent($this.parentElement, 'form-check') : null;
    if (!isCheck)
      addClass($this, 'is-invalid');
    else
      addClass(elFormCheck || $this.parentElement, 'is-invalid form-control');

    const elNext = $this.nextElementSibling;
    const elLast = elNext && (elNext.getAttribute('for') === $this.id || elNext.tagName === "SMALL")
        ? (isCheck ? elFormCheck || elNext.parentElement : elNext)
        : $this;
    const elError = elLast != null && elLast.nextElementSibling && hasClass(elLast.nextElementSibling, 'invalid-feedback')
        ? elLast.nextElementSibling
        : createElement("div", {insertAfter: elLast}, {className: 'invalid-feedback'});
    elError.innerHTML = errorMsg;
  }
}
function parent(el:Element|HTMLElement|null,cls:string):Element {
  while (el != null && !hasClass(el,cls))
    el = el.parentElement;
  return el as Element;
}

const hasClass = (el:Element|HTMLElement|null, cls:string|null) =>
    (" " + el!.className + " ").replace(/[\n\t\r]/g, " ").indexOf(" " + cls + " ") > -1;
const addClass = (el:Element|HTMLElement|null, cls:string|null) =>
    !hasClass(el, cls) ? el!.className = (el!.className + " " + cls).trim() : null;
const removeClass = (el:Element|HTMLElement|null, cls:string|null) =>
    hasClass(el, cls) ? el!.className = (" " + el!.className + " ").replace(el!.className,"").trim() : null;

// init generic behavior to bootstrap elements
export function bootstrap(el?:Element) {
  const els = (el || document).querySelectorAll('[data-invalid]'); 
  for (let i=0; i<els.length; i++) {
    showInvalidInputs.call(els[i]);
  }
}
// polyfill IE9+
if (!Element.prototype.matches) {
  Element.prototype.matches = (Element.prototype as any).msMatchesSelector ||
      Element.prototype.webkitMatchesSelector;
}
if (!Element.prototype.closest) {
  Element.prototype.closest = function(s:string) {
    let el:Element = this;
    do {
      if (el.matches(s)) return el;
      el = el.parentElement || el.parentNode as Element;
    } while (el !== null && el.nodeType === 1);
    return null;
  };
}

export const bindHandlers = (handlers:any) => {
  document.querySelectorAll<HTMLElement>('[data-click]').forEach((selected, i, all) => {
    selected.addEventListener('click', function(evt) {
      const el = evt.target as Element;
      const attr = el.getAttribute('data-click') ||
          el.closest('[data-click]')!.getAttribute('data-click');
      if (!attr) return;

      let pos = attr.indexOf(':');
      if (pos >= 0) {
        const cmd = attr.substring(0, pos);
        const data = attr.substring(pos + 1);
        const fn = handlers[cmd];
        if (fn) {
          fn.apply(evt.target, data.split(','));
        }
      } else {
        const fn = handlers[attr];
        if (fn) {
          fn.apply(evt.target, [].slice.call(arguments));
        }
      }
    });
  });  
};

interface IAjaxFormOptions {
  type?:string,
  validate?:Function,
  onSubmitDisable?:string,
  success?:Function,
  error?:Function,
  complete?:Function,
}

export function bootstrapForm (form:HTMLFormElement|null, options:IAjaxFormOptions) {
  if (!form) return;
  form.onsubmit = function (evt) {
    evt.preventDefault();
    options.type = "bootstrap-v4";
    return ajaxSubmit(form, options);
  }
}

function clearErrors(f: HTMLFormElement) {
  removeClass(f,'has-errors');
  f.querySelectorAll('.error-summary').forEach(el => {
    el.innerHTML = "";
    (el as HTMLElement).style.display = "none";
  });
  f.querySelectorAll('[data-validation-summary]').forEach(el => {
    el.innerHTML = "";
  });
  f.querySelectorAll('.error').forEach(el => removeClass(el,'error'));
  f.querySelectorAll('.form-check.is-invalid [data-invalid]').forEach(el => {
    el.removeAttribute('data-invalid');
  });
  f.querySelectorAll('.form-check.is-invalid').forEach(el => removeClass(el,'form-control'));
  f.querySelectorAll('.is-invalid').forEach(el => {
    removeClass(el, 'is-invalid');
    el.removeAttribute('data-invalid');
  });
  f.querySelectorAll('.is-valid').forEach(el => removeClass(el,'is-valid'));
}

export function ajaxSubmit(form:HTMLFormElement,options:IAjaxFormOptions={}) {
  clearErrors(form);
  addClass(form, 'loading');
  const disableSel = options.onSubmitDisable == null
    ? "[type=submit]"
    : options.onSubmitDisable;
  if (disableSel != null && disableSel != "") {
    form.querySelectorAll(disableSel).forEach(el => {
      el.setAttribute('disabled','disabled');
    });
  }
  //ajax -> fetch
}

