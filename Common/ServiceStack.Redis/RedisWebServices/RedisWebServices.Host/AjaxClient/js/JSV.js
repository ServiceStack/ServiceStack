/**
 * Created by IntelliJ IDEA.
 * User: mythz
 * Date: 16-Jun-2010
 * Time: 00:51:17
 * To change this template use File | Settings | File Templates.
 */

var JSV = {};
/**
 * parses JSV text into a JavaScript type 
 * @param str
 */
JSV.parse = function(str)
{
    if (!str) return str;
    if (str[0] == '{')
    {
        return JSV.parseObject_(str);
    }
    else if (str[0] == '[')
    {
        return JSV.parseArray_(str);
    }
    else
    {
        return JSV.parseString(str);
    }
}

JSV.ESCAPE_CHARS = ['"', ',', '{', '}', '[', ']'];

JSV.parseArray_ = function(str)
{
    var to = [], value = JSV.stripList_(str);
    if (!value) return to;

    if (value[0] == '{')
    {
        var ref = {i:0};
        do
        {
            var itemValue = JSV.eatMapValue_(value, ref);
            to.push(JSV.parse(itemValue));
        } while (++ref.i < value.length);
    }
    else
    {
        for (var ref={i:0}; ref.i < value.length; ref.i++)
        {
            var elementValue = JSV.eatElementValue_(value, ref);
            var listValue = JSV.parseString(elementValue);
            to.push(JSV.parse(listValue));
        }
    }
    return to;
};

JSV.parseObject_ = function(str)
{
    if (str[0] != '{')
    {
        throw "Type definitions should start with a '{', got string starting with: "
            + str.substr(0, str.length < 50 ? str.length : 50);
    }

    var name, obj = {};

    if (str == '{}') return null;
    for (var ref={i:1}, strTypeLength = str.length; ref.i < strTypeLength; ref.i++)
    {
        name = JSV.eatMapKey_(str, ref);
        ref.i++;
        var value = JSV.eatMapValue_(str, ref);
        obj[name]= JSV.parse(value);
    }
    return obj;
}

JSV.eatElementValue_ = function(value, ref)
{
    return JSV.eatUntilCharFound_(value, ref, ',');
}

JSV.containsAny_ = function(str, tests)
{
    if (!is.String(str)) return;
    for (var i = 0, len = tests.length; i < len; i++)
    {
        if (str.indexOf(tests[i]) != -1) return true;
    }
    return false;
};

JSV.toCsvField = function(text)
{
    return !text || JSV.containsAny_(JSV.ESCAPE_CHARS)
        ? text
        : '"' + text.replace('"', '""') + '"';
}

JSV.parseString = JSV.fromCsvField = function(text)
{
    return !text || text[0] != '"'
        ? text
        : text.substr(1, text.length - 2).replace('""', '"');
}

JSV.stripList_ = function(value)
{
    if (!value) return null;
    return value[0] == '['
        ? value.substr(1, value.length - 2)
        : value;
};

/**
 * @param value {string}
 * @param ref {ref int}
 * @param findChar {char}
 */
JSV.eatUntilCharFound_ = function(value, ref, findChar)
{
    var tokenStartPos = ref.i;
    var valueLength = value.length;
    if (value[tokenStartPos] != '"')
    {
        ref.i = value.indexOf(findChar, tokenStartPos);
        if (ref.i == -1) ref.i = valueLength;
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    while (++ref.i < valueLength)
    {
        if (value[ref.i] == '"'
            && (ref.i + 1 >= valueLength || value[ref.i + 1] == findChar))
        {
            ref.i++;
            return value.substr(tokenStartPos, ref.i - tokenStartPos);
        }
    }

    throw "Could not find ending quote";
}

/**
 *
 * @param value {string}
 * @param i {ref int}
 */
JSV.eatMapKey_ = function(value, ref)
{
    var tokenStartPos = ref.i;
    while (value[++ref.i] != ':' && ref.i < value.length) { }
    return value.substr(tokenStartPos, ref.i - tokenStartPos);
}

/**
 *
 * @param value {string}
 * @param ref {ref int}
 */
JSV.eatMapValue_ = function(value, ref)
{
    var tokenStartPos = ref.i;
    var valueLength = value.length;
    if (ref.i == valueLength) return null;

    var valueChar = value[ref.i];

    //If we are at the end, return.
    if (valueChar == ',' || valueChar == '}')
    {
        return null;
    }

    //Is List, i.e. [...]
    var withinQuotes = false;
    if (valueChar == '[')
    {
        var endsToEat = 1;
        while (++ref.i < valueLength && endsToEat > 0)
        {
            valueChar = value[ref.i];
            if (valueChar == '"')
                withinQuotes = !withinQuotes;
            if (withinQuotes)
                continue;
            if (valueChar == '[')
                endsToEat++;
            if (valueChar == ']')
                endsToEat--;
        }
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    //Is Type/Map, i.e. {...}
    if (valueChar == '{')
    {
        var endsToEat = 1;
        while (++ref.i < valueLength && endsToEat > 0)
        {
            valueChar = value[ref.i];

            if (valueChar == '"')
                withinQuotes = !withinQuotes;
            if (withinQuotes)
                continue;
            if (valueChar == '{')
                endsToEat++;
            if (valueChar == '}')
                endsToEat--;
        }
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    //Is Within Quotes, i.e. "..."
    if (valueChar == '"')
    {
        while (++ref.i < valueLength)
        {
            valueChar = value[ref.i];
            if (valueChar != '"') continue;
            var isLiteralQuote = ref.i + 1 < valueLength && value[ref.i + 1] == '"';
            ref.i++; //skip quote
            if (!isLiteralQuote)
                break;
        }
        return value.substr(tokenStartPos, ref.i - tokenStartPos);
    }

    //Is Value
    while (++ref.i < valueLength)
    {
        valueChar = value[ref.i];
        if (valueChar == ',' || valueChar == '}')
            break;
    }

    return value.substr(tokenStartPos, ref.i - tokenStartPos);
}
