import { leftPart, mapGet } from "@servicestack/client"
/*minify:*/

/** @typedef {{namespace:string,name:string}} TypeRef 
    @typedef {{name:string,genericArgs:string[]}} MetaType */

export let Types = (function() {
    let NumTypesMap = {
        Byte: 'byte',
        Int16: 'short',
        Int32: 'int',
        Int64: 'long',
        UInt16: 'ushort',
        Unt32: 'uint',
        UInt64: 'ulong',
        Single: 'float',
        Double: 'double',
        Decimal: 'decimal',
    }
    let NumTypes = [ ...Object.keys(NumTypesMap), ...Object.values(NumTypesMap) ]
    let Aliases = {
        String: 'string',
        Boolean: 'bool',
        ...NumTypesMap,
    }
    /** @param {string} type */
    function alias(type) {
        return Aliases[type] || type
    }
    /** @param {string} name
     @param {string[]} genericArgs */
    function typeName2(name, genericArgs) {
        if (!name) return ''
        if (!genericArgs)
            genericArgs = []
        if (name === 'Nullable`1')
            return alias(genericArgs[0]) + '?'
        if (name.endsWith('[]'))
            return `List<${alias(name.substring(0,name.length-2))}>`
                ;if (genericArgs.length === 0)
            return alias(name)
        return leftPart(alias(name), '`') + '<' + genericArgs.join(',') + '>'
    }
    /** @param {MetaType} metaType */
    function typeName(metaType) { return metaType && typeName2(metaType.name, metaType.genericArgs) }
    /** @param {string} type */
    function unwrap(type) { return type && type.endsWith('?') ? type.substring(0,type.length-1) : type }
    /** @param {string} type */
    function isNumber(type) { return type && NumTypes.indexOf(type) >= 0 }
    /** @param {string} type */
    function isString(type) { return type && type.toLowerCase() === 'string' }
    /** @param {string} type */
    function isArray(type) { return type.startsWith('List<') || type.endsWith('[]') }
    /** @param {TypeRef} typeRef */
    function key(typeRef) {
        return !typeRef ? null : (typeRef.namespace || '') + '.' + typeRef.name
    }
    /** @param {TypeRef} a
        @param {TypeRef} b */
    function equals(a,b) {
        return (a && b) && a.name === b.name && ((!a.namespace || !b.namespace) || a.namespace === b.namespace)
    }
    /** @param {string} type
        @param {*} value */
    function formatValue(type,value) {
        if (!type) return value
        type = unwrap(type)
        return isNumber(type) || type === 'Boolean'
            ? value
            : isArray(type)
                ? `[${value}]`
                : `'${value}'`
    }
    
    return ({ alias, unwrap, typeName2, isNumber, isString, isArray, typeName, formatValue, key, equals, })
})()

/*:minify*/
