// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


var fs = require('fs');
var path = require('path');
var sax = require("sax");

console.log("hello, world!");
var log = console.log;

function stripBOM(content) {
    if (content.charCodeAt(0) === 0xFEFF) {
        content = content.slice(1);
    }
    return content;
}

String.prototype.lines = function() {
    return this.replace(/\r/g, '').split('\n');
};

var SIGN_PROJS = [
    '../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj',
    '../../ServiceStack.Redis/src/ServiceStack.Redis/ServiceStack.Redis.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite/ServiceStack.OrmLite.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServer/ServiceStack.OrmLite.SqlServer.csproj',
    '../src/ServiceStack.Common/ServiceStack.Common.csproj',
    '../src/ServiceStack.Client/ServiceStack.Client.csproj',
    '../src/ServiceStack.Server/ServiceStack.Server.csproj',
    '../src/ServiceStack.Razor/ServiceStack.Razor.csproj',
    '../src/ServiceStack/ServiceStack.csproj'
];
var SIGN_REPLACE_TEXTS = {
    '<HintPath>..\\..\\lib\\ServiceStack.Text.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.Text.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.Common.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.Common.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.Redis.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.Redis.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.OrmLite.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.OrmLite.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.OrmLite.SqlServer.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.OrmLite.SqlServer.dll</HintPath>',
    '<ProjectReference Include="..\\ServiceStack.Common\\ServiceStack.Common.csproj">': '<ProjectReference Include="..\\ServiceStack.Common\\ServiceStack.Common.Signed.csproj">',
    '<ProjectReference Include="..\\ServiceStack.Client\\ServiceStack.Client.csproj">': '<ProjectReference Include="..\\ServiceStack.Client\\ServiceStack.Client.Signed.csproj">',
    '<ProjectReference Include="..\\ServiceStack\\ServiceStack.csproj">': '<ProjectReference Include="..\\ServiceStack\\ServiceStack.Signed.csproj">'
};

var injectSignedElements = [
    {
        PropertyGroup: {
            SignAssembly: "true",
        }
    },
    {
        PropertyGroup: {
            AssemblyOriginatorKeyFile: "servicestack-sn.pfx"
        }
    }
];

var readTextFile = function (path) {
    return stripBOM(fs.readFileSync(path, { encoding: 'UTF-8' }));
};

var mergeElements = function (xml, els) {
    els.forEach(function(elSeek) {
        Object.keys(elSeek).forEach(function(tag) {
            var tagToMatch = tag.toUpperCase();
            
            var strict = false;
            var parser = sax.parser(strict);

            var matches = [];
            var open = { line: 0, coloumn: 0, startTagPosition: 0 };

            parser.onopentag = function (el) {
                if (el.name == tagToMatch) {
                    log("found <" + tag + "> at: ", this.line + ":" + this.column);
                    open = { line: this.line, column: this.column, startTagPosition: this.startTagPosition };
                }
            };
            parser.onclosetag = function (name) {
                if (name == tagToMatch) {
                    log("found </" + tag + "> at: ", this.line + ":" + this.column);
                    matches.push({ start: open, end: { line: this.line, column: this.column, startTagPosition: this.startTagPosition } });
                }
            };

            parser.write(xml);

            log("results for " + tag);
            log(matches);

            var props = elSeek[tag];
            var out = [];
            var lastPos = 0;
            var found = false;

            matches.forEach(function(match) {
                var startPos = match.start.startTagPosition + match.start.column;
                var endPos = match.end.startTagPosition - 1;

                out.push(xml.substring(lastPos, startPos));

                var fragment = xml.substring(startPos, endPos);
                
                if (!found) {
                    var missing = [];
                    Object.keys(props).forEach(function (elName) {
                        var elValue = props[elName];
                        var seekTag = "<" + elName + ">";
                        var seekEndTag = "</" + elName + ">";
                        var injectXml = seekTag + elValue + seekEndTag;

                        var startPos;
                        if ((startPos = fragment.indexOf(seekTag)) >= 0) {
                            var endPos = fragment.indexOf(seekEndTag) + seekEndTag.length;
                            if (endPos == -1)
                                throw "Couldn't find matching end tag: " + seekEndTag;

                            fragment = fragment.substring(0, startPos)
                                + injectXml
                                + fragment.substring(endPos);

                            found = true;
                        } else {
                            missing.push(injectXml);
                        }
                    });
                    
                    //If one was found, add the missing at the end fragment
                    if (found) {
                        missing.forEach(function(injectXml) {
                            fragment += "  " + injectXml + "\n";
                        });
                        fragment += "  ";
                    }
                }

                out.push(fragment);

                lastPos = endPos;
            });

            //If we can't merge, append
            if (!found) {
                //Close open end tag first, then open new tag:
                out.push("</" + tag + ">\n  ");
                out.push("<" + tag + ">\n");
                Object.keys(props).forEach(function (elName) {
                    var injectXml = "    <" + elName + ">" + props[elName] + "</" + elName + ">\n";
                    out.push(injectXml);
                });
                //remaining fragment starts with closing tag
            }

            out.push("  ");
            out.push(xml.substring(lastPos));

            xml = out.join('');
        });
    });

    return xml;
};

SIGN_PROJS.forEach(function(proj) {
    log("file: " + proj);
    var xml = readTextFile(proj);
    log(xml);

    var transformedXml = mergeElements(xml, injectSignedElements);

    Object.keys(SIGN_REPLACE_TEXTS).forEach(function (find) {
        log("replacing: " + find + ", with: " + SIGN_REPLACE_TEXTS[find]);
        transformedXml = transformedXml.replace(find, SIGN_REPLACE_TEXTS[find]);
    });

    var signedProjPath = path.join(path.dirname(proj), path.basename(proj).replace(".csproj", ".Signed.csproj"));
    log("writing transformedXml to: " + signedProjPath);
    log(transformedXml);

    fs.writeFileSync(signedProjPath, transformedXml);
});
