/*minify:*/
let Types = (function() {
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
    function alias(type) {
        return Aliases[type] || type
    }
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
    function typeName(metaType) { return metaType && typeName2(metaType.name, metaType.genericArgs) }
    function unwrap(type) { return type && type.endsWith('?') ? type.substring(0,type.length-1) : type }
    function isNumber(type) { return type && NumTypes.indexOf(type) >= 0 }
    function isString(type) { return type && type.toLowerCase() === 'string' }
    function isArray(type) { return type.startsWith('List<') || type.endsWith('[]') }
    function key(typeRef) {
        return !typeRef ? null : (typeRef.namespace || '') + '.' + typeRef.name
    }
    function equals(a,b) {
        return (a && b) && a.name === b.name && ((!a.namespace || !b.namespace) || a.namespace === b.namespace)
    }
    function formatValue(type,value) {
        if (!type) return value
        type = unwrap(type)
        return isNumber(type) || type === 'Boolean'
            ? value
            : isArray(type)
                ? `[${value}]`
                : `'${value}'`
    }
    let Primitives = ['string','number','symbol','boolean']
    function isPrimitive (value) { return Primitives.indexOf(typeof value) >= 0 }
    function propHasAttr(p, attr) {
        return p && p.attributes && p.attributes.some(x => x.name === attr) 
    }
    function getProp(type, name) {
        let nameLower = name.toLowerCase()
        return type && type.properties && type.properties.find(p => p.name.toLowerCase() === nameLower)
    }
    return ({ alias, unwrap, typeName2, isNumber, isString, isArray, typeName, formatValue, key, equals, isPrimitive, propHasAttr, getProp, })
})()
/*:minify*/
