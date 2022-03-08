import { humanify, padInt, toDate, mapGet, apiValue, isDate, indexOfAny, fromXsdDuration, enc, apiValueFmt } from "@servicestack/client"
import { Types } from "./Types"
import { Forms } from "../../ui/js/appInit"
import { Crud } from "./core";
/*minify:*/

/** @typedef {{namespace:string,name:string}} TypeRef
    @typedef {{name:string,genericArgs:string[]}} MetaType */

/** @param {{[op:string]:MetadataType}} TypesMap
 *  @param {ApiCss} css 
 *  @param {UiInfo} ui */
export function createForms(TypesMap, css, ui) {
    let { theme, defaultFormats } = ui
    if (!defaultFormats) defaultFormats = {}
    if (!defaultFormats.locale) {
        defaultFormats.locale = map(navigator.languages, x => x[0]) || navigator.language || 'en'
    }
    let useType = type => (acc,x) => { acc[x]=type; return acc }
    let InputTypes = {
        bool: 'checkbox',
        ...'DateTime,DateTimeOffset,DateOnly'.split(',').reduce(useType('date'), {}),
        ...'TimeSpan,TimeOnly'.split(',').reduce(useType('time'), {}),
        ...'byte,short,int,long,ushort,uint,ulong,float,double,decimal'.split(',').reduce(useType('number'), {}),
        ...'string,Guid,Uri'.split(',').reduce(useType('text'), {}),
    }
    
    let _id = 0;
    let inputId = input => input && (input.id || `__${input.type||'undefined'}${_id++}`)
    let colClass = fields => `col-span-12` + (fields === 2 ? ' sm:col-span-6' : fields === 3 ? ' sm:col-span-4' : fields === 4 ? ' sm:col-span-3' : '')

    function inputType(typeName) {
        if (!typeName) return null
        typeName = Types.unwrap(Types.alias(typeName))
        return InputTypes[typeName]
    }
    function inputProp(prop) {
        let id = toCamelCase(prop.name), idLower = id.toLowerCase()
        let propType = Types.unwrap(Types.typeName2(prop.type, prop.genericArgs))
        let input = { id, type:inputType(propType), 'data-type': prop.type }
        if (prop.genericArgs) input['data-args'] = prop.genericArgs.join(',')
        let type = TypesMap[propType]
        if (type && type.isEnum) {
            input.type = 'select'
            if (type.enumValues || type.enumDescriptions) {
                input.allowableEntries = []
                for (let i=0; i<type.enumNames.length; i++) {
                    let key = map(type.enumValues, x => x[i]) || type.enumNames[i]
                    let value = map(type.enumDescriptions, x => x[i]) || type.enumNames[i]
                    input.allowableEntries.push({ key, value })
                }
            } else {
                input.allowableValues = type.enumNames
            }
        } else if (idLower.indexOf('password') >= 0) {
            input.type = 'password'
        } else if (idLower === 'email') {
            input.type = 'email'
        } else if (idLower.endsWith('url')) {
            input.type = 'url'
        }
        if (prop.input)
            Object.assign(input, prop.input)
        return input
    }
    let pad4 = n => n <= 9999 ? `000${n}`.slice(-4) : n;
    function dateFmt(d) { 
        return pad4(d.getFullYear()) + '-' + padInt(d.getMonth() + 1) + '-' + padInt(d.getDate()) 
    }
    
    /** @param {MetadataType} type
        @return {MetadataPropertyType[]} */
    function typeProperties(type) {
        if (!type) return []
        let props = []
        let existing = {}
        let addProps = xs => xs.forEach(p => {
            if (existing[p.name]) return
            existing[p.name] = 1
            props.push(p)
        })
        
        while (type) {
            if (type.properties) addProps(type.properties)
            type = type.inherits ? TypesMap[type.inherits.name] : null
        }
        return props.map(prop => prop.type.endsWith('[]')
            ? {...prop, type:'List`1', genericArgs:[prop.type.substring(0,prop.type.length-2)] }
            : prop)
    }

    /** @param {MetadataType} type */
    function isCrud(type) {
        return map(type.inherits, x => Crud.AnyRead.indexOf(x.name) >= 0) ||
            map(type.implements, x => x.some(iFace => Crud.AnyWrite.indexOf(iFace.name) >= 0))
    }

    /** @param {MetadataType} type */
    function crudModel(type) {
        return map(type.inherits, x => Crud.AnyRead.indexOf(x.name) >= 0) 
            ? type.inherits.genericArgs[0]
            : map(map(type.implements, x => x.find(iFace => Crud.AnyWrite.indexOf(iFace.name) >= 0)), x => x.genericArgs[0])
    }

    /** @param {MetadataType} type */
    function getPrimaryKey(type) {
        if (!type) return null
        let typeProps = typeProperties(type)
        let id = typeProps.find(x => x.name.toLowerCase() === 'id')
        if (id && id.isPrimaryKey) return id
        let pk = typeProps.find(x => x.isPrimaryKey)
        let ret = pk || id
        if (!ret) {
            let crudType = crudModel(type)
            if (crudType) {
                return getPrimaryKey(getType({ name: crudType }))
            }
            console.error(`Primary Key not found in ${type.name}`)
        } 
        return ret || null
    }

    /** @param {MetadataType} type 
     *  @param {*} row */
    function getId(type,row) { return map(getPrimaryKey(type), pk => mapGet(row, pk.name)) }
    
    let nowMs = () => new Date().getTime() + (defaultFormats.assumeUtc ? new Date().getTimezoneOffset() * 1000 * 60 : 0)

    let DateChars = ['/','T',':','-']
    /** @param {string|Date|number} val */
    function toRelativeNumber(val) {
        if (val == null) return NaN
        if (typeof val == 'number')
            return val
        if (isDate(val))
            return val.getTime() - nowMs()
        if (typeof val === 'string') {
            let num = Number(val)
            if (!isNaN(num))
                return num
            if (val[0] === 'P' || val.startsWith('-P'))
                return fromXsdDuration(val) * 1000 * -1
            if (indexOfAny(val, DateChars) >= 0)
                return toDate(val).getTime() - nowMs()
        }
        return NaN
    }
    
    let defaultRtf = new Intl.RelativeTimeFormat(defaultFormats.locale, {})
    let year =  24 * 60 * 60 * 1000 * 365
    let units = {
        year,
        month : year/12,
        day   : 24 * 60 * 60 * 1000,
        hour  : 60 * 60 * 1000,
        minute: 60 * 1000,
        second: 1000
    }
    /** @param {number} elapsedMs
     *  @param {Intl.RelativeTimeFormat} [rtf] */
    function relativeTimeFromMs(elapsedMs,rtf) {
        for (let u in units) {
            if (Math.abs(elapsedMs) > units[u] || u === 'second')
                return (rtf || defaultRtf).format(Math.round(elapsedMs/units[u]), u)
        }
    }
    /** @param {string|Date|number} val
     *  @param {Intl.RelativeTimeFormat} [rtf] */
    function relativeTime(val,rtf) {
        let num = toRelativeNumber(val)
        if (!isNaN(num))
            return relativeTimeFromMs(num,rtf)
        console.error(`Cannot convert ${val}:${typeof val} to relativeTime`)
        return ''
    }
    /** @param {Date} d
     *  @param {Date} [from] */
    let relativeTimeFromDate = (d,from) => 
        relativeTimeFromMs(d.getTime()-(from ? from.getTime() : nowMs()))
    
    let Formatters = {}
    /**  @param {FormatInfo} format */
    function formatter(format) {
        let { method, options } = format
        let key = `${method}(${options})`
        let f = Formatters[key]
        if (typeof f == 'function') return f
        let loc = format.locale || defaultFormats.locale
        if (method.startsWith('Intl.')) {
            let intlExpr = `return new ${method}('${loc}',${options||'undefined'})`
            try {
                let intlFn = Function(intlExpr)()
                f = method === 'Intl.DateTimeFormat'
                    ? val => intlFn.format(toDate(val))
                    : method === 'Intl.NumberFormat'
                        ? val => intlFn.format(Number(val))
                        : method === 'Intl.RelativeTimeFormat'
                            ? val => relativeTime(val,intlFn)
                            : val => intlFn.format(val)
                return Formatters[key] = f
            } catch(e) {
                console.error(`Invalid format: ${intlExpr}`,e)
            }
        } else {
            let fmt = require(method)
            if (typeof fmt == 'function') {
                let opt = options != null
                    ? Function("return " + options)()
                    : undefined
                f = val => fmt(val,opt,loc)
                return Formatters[key] = f
            }
            console.error(`No '${method}' function exists`)
        }
    }
    let useDateFmt = defaultFormats.date 
        ? formatter(defaultFormats.date)
        : dateFmt
    let useNumberFmt = defaultFormats.number
        ? formatter(defaultFormats.number)
        : v => v

    let maxFieldLength = map(ui.query, x => x.maxFieldLength) || 150
    let maxNestedFields = map(ui.query, x => x.maxNestedFields) || 2
    let maxNestedFieldLength = map(ui.query, x => x.maxNestedFieldLength) || 30

    /** @param {string} s
     *  @param {number} len */
    function trunc(s, len) { return s.length > len ? s.substring(0,len) + '...' : s }

    /** @param {string} s */
    function scrubStr(s) {
        return s.substring(0, 6) === '/Date('
            ? dateFmt(toDate(s))
            : s
    }

    /** @param {*} o */
    function scrub(o) {
        if (o == null) return null
        if (typeof o == 'string') return scrubStr(o)
        if (Types.isPrimitive(o)) return o
        if (Array.isArray(o)) return o.map(scrub)
        if (typeof o == 'object') {
            let to = {}
            Object.keys(o).forEach(k => {
                to[k] = scrub(o[k])
            })
            return to
        }
        return o
    }
    
    function displayObj(val) {
        return JSON.stringify(scrub(val), null, 4).replace(/"/g,'')
    }
    
    function formatObject(val) {
        let obj = val
        if (Array.isArray(val)) {
            if (Types.isPrimitive(val[0])) {
                return obj.join(',')
            }
            obj = val[0]
        }
        
        let keys = Object.keys(obj)
        let sb = []
        for (let i=0; i<Math.min(maxNestedFields,keys.length); i++) {
            let k = keys[i]
            let val = `${obj[k]}`
            sb.push(`<b class="font-medium">${k}</b>: ${enc(trunc(scrubStr(val),maxNestedFieldLength))}`)
        }
        if (keys.length > 2) sb.push('...')
        return '<span title="' + enc(displayObj(val)) + '">{ ' + sb.join(', ') + ' }</span>'
    }

    return {
        getId,
        inputId,
        colClass,
        inputProp,
        getPrimaryKey,
        typeProperties,
        relativeTime,
        relativeTimeFromMs,
        relativeTimeFromDate,
        theme,
        formClass: theme.form + (css.form ? ' ' + css.form : ''),
        gridClass: css.fieldset,
        /** @param {MetadataType} type */
        forEdit(type) {
            /** @param {InputInfo} input */
            let pk = getPrimaryKey(type)
            if (!pk) return null
            return field => {
                if (field.id.toLowerCase() === pk.name.toLowerCase()) {
                    if (!field.input) field.input = {}
                    field.input.disabled = true
                }
            }
        },
        /** @param {InputInfo[]} formLayout
            @param {({id:string,input:InputInfo,rowClass:string}) => void} [f] */
        getGridInputs(formLayout, f) {
            let to = []
            if (formLayout) {
                formLayout.forEach(input => {
                    if (input.ignore) return
                    let id = inputId(input)
                    if (id.startsWith('__')) console.log(`!id ${id}`, input) /*debug*/
                    let field = { id, input, rowClass: input.css && input.css.field || css.field }
                    if (input.type === 'hidden' && (field.rowClass||'').indexOf('hidden') === -1) {
                        field.rowClass = field.rowClass ? `${field.rowClass} hidden` : 'hidden'
                    }
                    if (f) f(field)
                    to.push(field)
                })
            }
            return to
        },
        getFieldError(error, id) { return error && error.errors &&
            map(error.errors.find(x => x.fieldName.toLowerCase() === id.toLowerCase()), x => x.message)
        },
        kvpValues(input) {
            return input.allowableEntries || (input.allowableValues||[]).map(x => ({ key:x, value:x }))
        },
        useLabel(input) {
            return input.label != null ? input.label : humanify(input.id)
        },
        usePlaceholder(input) {
            return input.placeholder || ''
        },
        isRequired(input) {
            return input.required || false
        },

        /** @param {MetadataOperationType} op */
        resolveFormLayout(op) {
            if (!op) return null
            let allProps = typeProperties(op.request).filter(Forms.supportsProp)
            if (op.ui.formLayout) {
                let allPropsMap = allProps.reduce((acc,x) => { acc[x.name] = x; return acc }, {})
                let ret = op.ui.formLayout.map(input => ({ ...inputProp(allPropsMap[input.id]), ...input }) )
                return ret
            }
            let inputProps = allProps.map(inputProp)
            let fullWidthTypes = ['textarea','divider']
            /** @param {InputInfo} input */
            let configureCss = input => {
                if (input && (fullWidthTypes.indexOf(input.type) >= 0 || (input['data-type'] === 'List`1' && input.type !== 'file'))) {
                    if (!input.css) input.css = {}
                    if (!input.css.field) input.css.field = `col-span-12`
                }
                return input
            }
            let pagingStart = inputProps.findIndex(x => x.id.toLowerCase() === 'skip')
            if (pagingStart >= 0) inputProps.splice(pagingStart, 0, { id:`__divider${pagingStart}`, type:'divider' })
            let formLayout = inputProps.map(configureCss)
            return formLayout
        },
        formValues(form) {
            let obj = {}
            Array.from(form.elements).forEach(el => {
                if (!el.id || el.value == null || el.value === '') return
                let dataType = el.getAttribute('data-type')
                let dataArgs = (el.getAttribute('data-args') || '').split(','), dataArg = dataArgs[0]
                let value = el.type === 'checkbox'
                    ? el.checked
                    : el.value
                if (Types.isNumber(dataType) || (dataType === 'Nullable`1' && Types.isNumber(dataArg))) {
                    value = Number(value)
                } else if (dataType === 'List`1') {
                    value = value.split(',').map(x => Types.isNumber(dataArg)
                        ? Number(x)
                        : x)
                }
                obj[el.id] = value
            })
            return obj
        },
        /** @param {Element} form
         *  @param {MetadataOperationType} op */
        formData(form,op) {
            let formData = new FormData(form)
            Array.from(form.elements).forEach(e => {
                if (e.type === 'file') {
                    let file = formData.get(e.name)
                    if (file.size === 0) {
                        formData.delete(e.name)
                    }
                }
            })
            return formData
        },
        groupTypes(allTypes) {
            let allTypesMap = {}
            let groups = []
            allTypes.forEach(type => {
                if (allTypesMap[type.name]) return
                let group = []
                let addTypeDef = typeDef => {
                    if (!typeDef || allTypesMap[typeDef.name]) return
                    allTypesMap[typeDef.name] = typeDef
                    group.push({ type: typeDef, typeName: Types.typeName(typeDef) })
                }
                let addTypeRef = typeRef => {
                    if (!typeRef || allTypesMap[typeRef.name]) return
                    let typeDef = TypesMap[typeRef.name]
                    allTypesMap[typeDef.name] = typeDef
                    group.push({ type: typeDef, typeName: Types.typeName(typeRef) })
                    return typeDef
                }
                addTypeDef(type)
                if (type.inherits) {
                    let subType = addTypeRef(type.inherits)
                    while (subType) {
                        subType = subType.inherits ? addTypeRef(subType.inherits) : null
                    }
                }
                groups.push(group)
            })
            return groups
        },
        supportsProp(prop) {
            let propType = Types.typeName2(prop.type, prop.genericArgs)
            if (prop.isValueType || prop.isEnum || inputType(propType))
                return true
            if (prop.type === 'List`1') {
                if (inputType(prop.genericArgs[0]))
                    return true
                if (map(prop.input, x => x.type === 'file'))
                    return true
            }
            console.log('!supportsProp', 'propType', propType, prop.type, prop.genericArgs, map(prop.genericArgs, x => inputType(x[0]))) /*debug*/
            return false
        },
        populateModel(model, formLayout) {
            if (!model || !formLayout) return null
            formLayout.forEach(input => {
                if (typeof model[input.id] == 'undefined') {
                    model[input.id] = null
                }
            })
            return model
        },
        apiValue(o) {
            if (o == null) return ''
            if (typeof o == 'string')
                return o.substring(0, 6) === '/Date('
                    ? dateFmt(toDate(o))
                    : o.trim()
            return o
        },
        /** @param {*} o
         *  @param {MetadataPropertyType} prop */
        format(o, prop) {
            if (o == null) return ''
            let val = apiValue(o)
            let { format } = prop
            let f = format && formatter(format) 
            if (typeof f != 'function') {
                f = v => isDate(v) 
                    ? useDateFmt(v) 
                    : typeof v == 'number'
                        ? useNumberFmt(v)
                        : v
            }
            let ret = f(val)
            if (typeof ret == 'object') {
                return formatObject(ret)
            }
            return typeof ret == 'string' && ret[0] !== '<'
                ? ret.length > maxFieldLength
                    ? `<span title="${enc(ret)}">${enc(trunc(ret,maxFieldLength))}</span>`
                    : enc(ret)
                : `${ret}`
        }
    }
}

/*:minify*/