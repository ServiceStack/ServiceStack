import { humanify, padInt, toDate, mapGet, apiValue, isDate, indexOfAny, fromXsdDuration, enc, uniq, omit } from "@servicestack/client"
import { Types } from "./Types"
import { Crud, map } from "./core";
import { MetadataOperationType, MetadataType, MetadataPropertyType, ApiCss, UiInfo, InputInfo } from "../../lib/types"
import { Forms, Meta, CrudApisStateProp } from "../../lib/types"

/*minify:*/

/** @param {Meta} Meta
 *  @param {ApiCss} css 
 *  @param {UiInfo} ui
 *  @return {Forms} */
export function createForms(Meta, css, ui) {
    let { OpsMap, TypesMap, getIcon } = Meta
    let operations = Object.values(OpsMap)
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
    let FloatTypes = 'float,double,decimal'.split(',')
    
    let _id = 0;
    let inputId = input => input && (input.id || `__${input.type||'undefined'}${_id++}`)
    /** @param {number} fields
     * @return {string} */
    let colClass = fields => `col-span-12` + (fields === 2 ? ' sm:col-span-6' : fields === 3 ? ' sm:col-span-4' : fields === 4 ? ' sm:col-span-3' : '')

    /** @param {{namespace:string?,name:string}|string} typeRef
        @return {MetadataType} */
    function getType(typeRef) {
        return !typeRef ? null
            : typeof typeRef == 'string'
                ? TypesMap[typeRef]
                : TypesMap[typeRef.name]
    }

    /** @param {string} typeName */
    function inputType(typeName) {
        if (!typeName) return null
        typeName = Types.unwrap(Types.alias(typeName))
        return InputTypes[typeName]
    }
    /** @param {MetadataPropertyType} prop */
    function inputProp(prop) {
        let id = toCamelCase(prop.name), idLower = id.toLowerCase()
        let propType = Types.unwrap(Types.typeName2(prop.type, prop.genericArgs))
        let input = { id, type:inputType(propType), 'data-type': prop.type }
        /**: allow decimals in number input */
        if (FloatTypes.indexOf(propType) >= 0) input.step = '0.01'
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
            Object.assign(input, omit(prop.input,['id']))
        
        return input
    }
    let pad4 = n => n <= 9999 ? `000${n}`.slice(-4) : n;
    function dateFmt(d) { 
        return pad4(d.getFullYear()) + '-' + padInt(d.getMonth() + 1) + '-' + padInt(d.getDate()) 
    }
    
    /** @param {MetadataType} type
        @return {MetadataPropertyType[]} */
    function typeProperties(type) {
        return Types.typeProperties(TypesMap, type)
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

    /** @param {MetadataType} type 
     * @return {MetadataPropertyType|null} */
    function getPrimaryKey(type) {
        if (!type) return null
        return getPrimaryKeyByProps(type, typeProperties(type)) 
    }

    /** @param {MetadataType} type 
     * @param {MetadataPropertyType[]} props
     * @return {MetadataPropertyType|null} */
    function getPrimaryKeyByProps(type,props) {
        let id = props.find(x => x.name.toLowerCase() === 'id')
        if (id && id.isPrimaryKey) return id
        let pk = props.find(x => x.isPrimaryKey)
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

    // Calc TZOffset: (defaultFormats.assumeUtc ? new Date().getTimezoneOffset() * 1000 * 60 : 0)
    let nowMs = () => new Date().getTime()

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

    let maxFieldLength = map(ui.locode, x => x.maxFieldLength) || 150
    let maxNestedFields = map(ui.locode, x => x.maxNestedFields) || 2
    let maxNestedFieldLength = map(ui.locode, x => x.maxNestedFieldLength) || 30

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
            if (val[0] != null) obj = val[0]
        }
        if (obj == null) return ''
        
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

    let Lookup = {}
    /** @param {*} model
     *  @param {*} id
     *  @param {string} label */
    function lookupLabel(model,id,label) {
        return Lookup[model] && Lookup[model][id] && Lookup[model][id][label] || ''
    }
    /** @param {*} model
     *  @param {*} id
     *  @param {string} label 
     *  @param {*} value */
    function setLookupLabel(model,id,label,value) {
        if (!Lookup[model]) Lookup[model] = {}
        let modelLookup = Lookup[model][id] || (Lookup[model][id] = {})
        modelLookup[label] = value
    }
    /** @param {*} row
     *  @param {MetadataPropertyType} prop
     *  @param {MetadataPropertyType[]} props */
    function refInfo(row, prop, props) {
        let ref = prop.ref
        if (ref) {
            let refIdValue = ref.selfId == null
                ? mapGet(row, prop.name)
                : mapGet(row, ref.selfId)
            let isRefType = typeof refIdValue == 'object'
            if (isRefType) {
                refIdValue = mapGet(refIdValue, ref.refId)
            }
            let queryOp = operations.find(op => Crud.isQuery(op) && op.dataModel.name === ref.model)
            if (queryOp != null) {
                let href = { op:queryOp.request.name, skip:null, edit:null, new:null, $qs: { [ref.refId]: refIdValue } }
                let html = format(mapGet(row,prop.name), prop)
                if (ref.refLabel != null) {
                    let colModel = props.find(x => x.type === ref.model)
                    let modelValue = colModel && mapGet(row, colModel.name)
                    if (modelValue != null) {
                        let label = mapGet(modelValue, ref.refLabel)
                        if (label != null) {
                            html = label
                            setLookupLabel(ref.model, refIdValue, ref.refLabel, label)
                        }
                    } else {
                        let label = lookupLabel(ref.model, refIdValue, ref.refLabel)
                        html = label != null ? label : `${ref.model}: ${html}`
                    }
                }
                return { href, icon: getIcon({ op:queryOp }), html }
            }
        }
        return null
    }
    /** @param {any[]} results
     *  @param {MetadataPropertyType[]} props
     *  @param {() => void} refreshFn */
    function fetchLookupValues(results, props, refreshFn) {
        props.forEach(c => {
            let refLabel = c.ref && c.ref.refLabel
            if (refLabel) {
                let refId = c.ref.refId
                let lookupOp = operations.find(op => Crud.isQuery(op) && op.dataModel.name === c.ref.model)
                if (lookupOp) {
                    let lookupIds = uniq(results.map(x => mapGet(x, c.name)).filter(x => x != null))
                    let modelLookup = Lookup[c.ref.model]
                    if (!modelLookup) Lookup[c.ref.model] = modelLookup = {}
                    let existingIds = []

                    Object.keys(Lookup[c.ref.model]).forEach(pk => {
                        if (mapGet(modelLookup[pk], refLabel) != null) {
                            existingIds.push(pk)
                        }
                    })
                    let newIds = lookupIds.filter(id => existingIds.indexOf(`${id}`) === -1)
                    if (newIds.length === 0) return

                    // /api/QueryEmployees?IdIn=1,2,3&fields=Id,LastName&jsconfig=edv
                    let dataModel = getType({ name:c.ref.model })
                    let isComputed = Types.propHasAttr(c,'Computed') || Types.propHasAttr(Types.getProp(dataModel, refLabel),'Computed')
                    let fields = !isComputed ? [refId,refLabel].join(',') : null
                    let queryArgs = { [c.ref.refId + 'In']:newIds,fields,jsconfig:'edv' }
                    let lookupRequest = createRequest(lookupOp, queryArgs)

                    apiSend(createClient, lookupRequest)
                        .then(r => {
                            if (!r.api.succeeded) return
                            (r.api.response.results || []).forEach(x => {
                                let id = mapGet(x, refId)
                                let val = mapGet(x, refLabel)
                                let modelLookupLabels = modelLookup[id] || (modelLookup[id] = {})
                                modelLookupLabels[refLabel] = val
                            })
                            if (refreshFn) refreshFn()
                        })
                }
            }
        })
    }

    /** @param {MetadataPropertyType} prop
     * @param {string} opName
     * @param {Function} callback
     * @return {CrudApisStateProp}
     */
    function createPropState(prop, opName, callback) {
        let state = createState(opName)
        let viewModel = getType(state.opQuery.viewModel) 
        
        /** @type {CrudApisStateProp} */
        let propState = Object.assign(state, { prop, opName, callback,
            dataModel: getType(state.opQuery.dataModel),
            viewModel,
            viewModelColumns: typeProperties(viewModel),
            createPrefs: () => settings.lookup(opName),
            /** @return {MetadataPropertyType[]} */
            selectedColumns: prefs => []
        })
        
        propState.selectedColumns = prefs => {
            return propState && hasItems(prefs.selectedColumns)
                ? prefs.selectedColumns.map(name => propState.viewModelColumns.find(x => x.name === name))
                : propState.viewModelColumns.filter(x => !!x) || []
        }

        return propState
    }

    let typeofNet = o =>
        typeof o == 'string'
            ? 'String'
            :  typeof o == 'boolean'
                ? "Boolean"
                : typeof o === 'number'
                    ? isFinite(o) && Math.floor(o) === o
                        ? 'Int64'
                        : 'Double'
                    : isDate(o)
                        ? 'DateTime'
                        : Array.isArray(o)
                            ? 'List<object>'
                            : o.constructor && o.constructor.name === 'Object'
                                ? 'Dictionary<String,Object>'
                                : 'Object'
    let ValueTypes = 'String,Boolean,Int64,Double,DateTime'.split(',')
    let isValueType = o => ValueTypes.indexOf(typeofNet(o)) >= 0
    
    /** @param {*} o
     *  @param {MetadataPropertyType} prop */
    function format(o, prop) {
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
    
    /** @param {MetadataPropertyType} prop */
    function supportsProp(prop) {
        if (prop.input && prop.input.type === 'file') return true
        let propType = Types.typeName2(prop.type, prop.genericArgs)
        if (prop.isValueType || prop.isEnum || inputType(propType))
            return true
        if (prop.type === 'List`1') {
            if (inputType(prop.genericArgs[0]))
                return true
            if (map(prop.input, x => x.type === 'file'))
                return true
        }
        console.log('!supportsProp propType', propType, prop.type, prop.genericArgs, map(prop.genericArgs, x => inputType(x[0])), prop.input) /*debug*/
        return false
    }
    let Server = Meta.Server
    
    /** @type {Forms} */
    return {
        Meta,
        Server,
        getId,
        getType,
        inputId,
        colClass,
        inputProp,
        getPrimaryKey,
        getPrimaryKeyByProps,
        typeProperties,
        relativeTime,
        relativeTimeFromMs,
        relativeTimeFromDate,
        Lookup,lookupLabel,refInfo,fetchLookupValues,
        theme,
        formClass: theme.form + (css.form ? ' ' + css.form : ''),
        gridClass: css.fieldset,

        /** @param {MetadataOperationType} op 
         * @return {string} */
        opTitle(op) {
            return op.request.description || humanify(op.request.name).replace(/^Patch/,'Update')
        },
        /** @param {MetadataType} type */
        forAutoForm(type) {
            return field => {
                field.prop = this.getFormProp(field.id, type)
            }
        },
        /** @param {MetadataType} type */
        forCreate(type) {
            return field => {
                field.prop = this.getFormProp(field.id, type)
            }
        },
        /** @param {MetadataType} type */
        forEdit(type) {
            /** @param {InputInfo} input */
            let pk = getPrimaryKey(type)
            if (!pk) return null
            return field => {
                field.prop = this.getFormProp(field.id, type)
                if (field.id.toLowerCase() === pk.name.toLowerCase()) {
                    if (!field.input) field.input = {}
                    field.input.disabled = true
                }
            }
        },
        getFormProp(id, type) {
            let idLower = id && id.toLowerCase()
            let typeProps = id && type && typeProperties(type)
            let prop = typeProps && typeProps.find(x => x.name.toLowerCase() === idLower)
            if (!prop) {
                if (!id.startsWith('__')) console.error(`'${id}' Property not found in ${type && type.name}`)
                return null
            }
            if (typeof settings != 'object' || typeof settings.lookup != 'function') return prop // disable in API Explorer
            if (!prop.ref) {
                let crudRef = map(type.implements, x => x.find(x => Crud.AnyWrite.indexOf(x.name) >= 0))
                let dataModel = map(crudRef && crudRef.genericArgs[0], name => getType({ name }))
                prop.ref = map(dataModel, x => x.properties && map(x.properties.find(p => p.name.toLowerCase() === idLower), p => p.ref))
            }
            if (prop.ref) {
                prop.refInfo = row => {
                    let ret = refInfo(row, prop, typeProps)
                    return ret
                }
                prop.refLookup = callback => {
                    let queryOp = operations.find(op => Crud.isQuery(op) && op.dataModel.name === prop.ref.model)
                    let state = createPropState(prop, queryOp.request.name, callback)
                    state.refresh = () => Object.assign(state, createPropState(prop, queryOp.request.name, callback))
                    store.modalLookup = state
                    transition('modal-lookup', true)
                }
            }
            return prop
        },
        /** @param {InputInfo[]} formLayout
            @param {(args:{id,input:InputInfo,rowClass:string}) => void} [f] */
        
        getGridInputs(formLayout, f) {
            if (!formLayout) return []
            return formLayout.map(input => this.getGridInput(input, f)).filter(x => !!x)
        },
        /** @param {InputInfo} input
            @param {(args:{id:string,input:InputInfo,rowClass:string}) => void} [f] */
        getGridInput(input, f) {
            if (input.ignore) return
            let id = inputId(input)
            if (id.startsWith('__')) console.log(`!id ${id}`, input) /*debug*/
            let field = { id, input, rowClass: input.css && input.css.field || css.field }
            if (input.type === 'hidden' && (field.rowClass||'').indexOf('hidden') === -1) {
                field.rowClass = field.rowClass ? `${field.rowClass} hidden` : 'hidden'
            }
            if (f) f(field)
            return field
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
            let allProps = typeProperties(op.request).filter(supportsProp)
            if (op.ui && op.ui.formLayout) {
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
        /** @param {} dataModel */
        dataModelOps(dataModel) {
            if (!dataModel) return []
            return Server.api.operations.filter(x => Types.equals(x.dataModel, dataModel))
        },
        /** @param {MetadataPropertyType} prop */
        complexProp(prop) {
            let propType = Types.typeName2(prop.type, prop.genericArgs)
            return !(prop.isValueType || prop.isEnum || inputType(propType));
        },
        supportsProp,
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
        format,
        typeofNet,
        isValueType,
    }
}


/**
 * Useful generic collections around Metadata APIs
 * @param app
 * @param appName
 */
export function appObjects(app,appName) {
    let api = app.api
    let CACHE = {}
    /** @type Record<number,string> */
    let HttpErrors = { 401:'Unauthorized', 403:'Forbidden' }
    /** @type Record<string,MetadataOperationType> */
    let OpsMap = {}
    /** @type Record<string,MetadataType> */
    let TypesMap = {}
    /** @type Record<string,MetadataType> */
    let FullTypesMap = {}
    api.operations.forEach(op => {
        OpsMap[op.request.name] = op
        TypesMap[op.request.name] = op.request
        FullTypesMap[Types.key(op.request)] = op.request
        if (op.response) TypesMap[op.response.name] = op.response
        if (op.response) FullTypesMap[Types.key(op.response)] = op.response
    })
    api.types.forEach(type => TypesMap[type.name] = type)
    api.types.forEach(type => FullTypesMap[Types.key(type)] = type)

    let cssName = appName + 'Css'
    api.operations.forEach(op => {
        /** @type {ApiCss} */
        let appCss = op.ui && op.ui[cssName]
        if (appCss) {
            Types.typeProperties(TypesMap, op.request).forEach(prop => {
                if (appCss.field) {
                    if (!prop.input) prop.input = {}
                    if (!prop.input.css) prop.input.css = {}
                    if (!prop.input.css.field) prop.input.css.field = appCss.field
                }
            })
        }
    })

    return { CACHE, HttpErrors, OpsMap, TypesMap, FullTypesMap, }
}

/**
 * Generic functionality around AppMetadata
 * @remarks
 * @param {AppMetadata} app
 * @param {string} appName
 * @return {Meta}
 */
export function createMeta(app,appName) {

    let { CACHE, HttpErrors, OpsMap, TypesMap, FullTypesMap } = appObjects(app, appName)

    /** Find `MetadataOperationType` by API name
     * @param {string} opName */
    function getOp(opName) {
        return OpsMap[opName]
    }

    /** Find `MetadataType` by DTO name
     * @param {{namespace:string?,name:string}|string} typeRef
     * @return {MetadataType} */
    function getType(typeRef) {
        return !typeRef ? null
            : typeof typeRef == 'string'
                ? TypesMap[typeRef]
                : FullTypesMap[Types.key(typeRef)] || TypesMap[typeRef.name]
    }

    /** Check whether a Type is an Enum
     * @param {string} type
     * @return {boolean} */
    function isEnum(type) {
        return type && map(TypesMap[type], x => x.isEnum) === true
    }

    /** Get Enum Values of an Enum Type
     * @param {string} type
     * @return {{key:string,value:string}[]} */
    function enumValues(type) {
        let enumType = type && map(TypesMap[type], x => x.isEnum ? x : null)
        if (!enumType) return []
        if (enumType.enumValues) {
            let ret = []
            for (let i=0; i<enumType.enumNames; i++) {
                ret.push({ key:enumType.enumValues[i], value:enumType.enumNames[i] })
            }
            return ret
        } else {
            return enumType.enumNames.map(x => ({ key:x, value:x }))
        }
    }

    let defaultIcon = app.ui.theme.modelIcon ||
        { svg:`<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24"><g fill="none" stroke="currentColor" stroke-width="1.5"><path d="M5 12v6s0 3 7 3s7-3 7-3v-6"/><path d="M5 6v6s0 3 7 3s7-3 7-3V6"/><path d="M12 3c7 0 7 3 7 3s0 3-7 3s-7-3-7-3s0-3 7-3Z"/></g></svg>` }

    /** Get API Icon
     * @param {{op:MetadataOperationType?,type:MetadataType?}} opt
     * @return {{svg:string}} */
    function getIcon({op,type}) {
        if (op) {
            let img = map(op.request, x => x.icon)
                || map(getType(op.viewModel), x => x.icon)
                || map(getType(op.dataModel), x => x.icon)
            if (img)
                return img
        }
        if (type && type.icon) {
            return type.icon
        }
        return defaultIcon
    }
    
    let qs = queryString(location.search)
    let stateQs = qs.IncludeTypes ? `?IncludeTypes=${qs.IncludeTypes}` : ''

    /** Get Locode URL
     * @param {string} op= */
    function locodeUrl(op) {
        return `/locode/${op || ''}${stateQs}`
    }
    /** Get URL with initial queryString state
     * @param {string} url */
    function urlWithState(url) {
        if (!url) return url
        let alreadyHasState = url.indexOf('IncludeTypes') >= 0
        let isBuiltinUi = url.indexOf('/ui') >= 0 || url.indexOf('/locode') >= 0 || url.indexOf('/admin-ui') >= 0
        if (!isBuiltinUi || alreadyHasState) return url
        return url + (url.indexOf('?') >= 0
            ? (stateQs ? '&' + stateQs.substring(1) : '')
            : stateQs)
    }
    
    let Server = app
    let tags = uniq(app.api.operations.flatMap(op => op.tags)).filter(x => !!x)
    let operations = app.api.operations
    
    return { CACHE, HttpErrors, Server, OpsMap, TypesMap, FullTypesMap, operations, tags, 
             getOp, getType, isEnum, enumValues, getIcon, locodeUrl, urlWithState }
}

/*:minify*/