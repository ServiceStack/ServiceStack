// Copyright (c) Service Stack LLC. All Rights Reserved.
// License: https://raw.github.com/ServiceStack/ServiceStack/master/license.txt


var fs = require('fs');
var path = require('path');
var sax = require("sax");

var log = console.log;
log("Generating .Signed.csprojs...");

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
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLiteV45/ServiceStack.OrmLiteV45.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServer/ServiceStack.OrmLite.SqlServer.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.SqlServerV45/ServiceStack.OrmLite.SqlServerV45.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.Oracle/ServiceStack.OrmLite.Oracle.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.Sqlite.Windows/ServiceStack.OrmLite.Sqlite.Windows.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.MySql/ServiceStack.OrmLite.MySql.csproj',
    '../../ServiceStack.OrmLite/src/ServiceStack.OrmLite.MySqlV45/ServiceStack.OrmLite.MySqlV45.csproj',
    '../src/ServiceStack.Common/ServiceStack.Common.csproj',
    '../src/ServiceStack.Client/ServiceStack.Client.csproj',
    '../src/ServiceStack.Server/ServiceStack.Server.csproj',
    '../src/ServiceStack.ServerV45/ServiceStack.ServerV45.csproj',
    '../src/ServiceStack.Razor/ServiceStack.Razor.csproj',
    '../src/ServiceStack.ProtoBuf/ServiceStack.ProtoBuf.csproj',
    '../src/ServiceStack.Api.Swagger/ServiceStack.Api.Swagger.csproj',
    '../src/ServiceStack/ServiceStack.csproj'
];
var SIGN_REPLACE_TEXTS = {
    '<HintPath>..\\..\\lib\\ServiceStack.Text.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.Text.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.Common.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.Common.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.Redis.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.Redis.dll</HintPath>',
    '<HintPath>..\\..\\lib\\ServiceStack.OrmLite.dll</HintPath>': '<HintPath>..\\..\\lib\\signed\\ServiceStack.OrmLite.dll</HintPath>',
    '<HintPath>..\\..\\lib\\net45\\ServiceStack.OrmLite.dll</HintPath>': '<HintPath>..\\..\\lib\\net45\\signed\\ServiceStack.OrmLite.dll</HintPath>',
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


log("Generating Custom build .csprojs...");

var CUSTOM_TEMPLATES = [{
    Code: 'PCL',
    Path: '../src/Templates/PclTemplate.csproj',
    ProjectGuid: '{BF5EEDF0-594C-4660-AFF5-AF3423F3602D}',
    RootNamespace: 'PclTemplate',
    AssemblyName: 'PclTemplate',
},
{
    Code: 'SL5',
    Path: '../src/Templates/SilverlightTemplate.csproj',
    ProjectGuid: '{12B8CB9F-E397-4B5F-89AF-B6998296BFE6}',
    RootNamespace: 'SilverlightTemplate',
    AssemblyName: 'SilverlightTemplate',
}
/*,{
    Code: 'Android',
    Path: '../src/Templates/AndroidTemplate.csproj',
    ProjectGuid: '{BEA92E9F-00B1-4923-BD81-7F3A9CA24408}',
    RootNamespace: 'AndroidTemplate',
    AssemblyName: 'AndroidTemplate',
},
{
    Code: 'AndroidIndie',
    Path: '../src/Templates/AndroidIndieTemplate.csproj',
    ProjectGuid: '{BEA92E9F-00B1-4923-BD81-7F3A9CA24408}',
    RootNamespace: 'AndroidIndieTemplate',
    AssemblyName: 'AndroidIndieTemplate',
}*/
];

var CUSTOM_PROJS = [
//{
//    Path: '../src/ServiceStack.Interfaces/ServiceStack.Interfaces.csproj',
//    ReplaceElements: {
//        ProjectGuid: '{42E1C8C0-A163-44CC-92B1-8F416F2C0B01}',
//        RootNamespace: 'ServiceStack',
//        AssemblyName: 'ServiceStack.Interfaces',
//    },
//    ReplaceTemplate: function (code, tmpl) {
//        var replaceTexts = {
//            '<MinimumVisualStudioVersion>11.0</MinimumVisualStudioVersion>': '<MinimumVisualStudioVersion>10.0</MinimumVisualStudioVersion>',
//            '<TargetFrameworkVersion>v4.5</TargetFrameworkVersion>': '<TargetFrameworkVersion>v4.0</TargetFrameworkVersion>',
//            '<TargetFrameworkProfile>Profile7</TargetFrameworkProfile>': '<TargetFrameworkProfile>Profile136</TargetFrameworkProfile>'
//        };
//        if (code == 'PCL') {
//            for (var needle in replaceTexts) {
//                var replaceText = replaceTexts[needle];
//                tmpl = tmpl.replace(needle, replaceText);
//            }
//        }
//        return tmpl;
//    }
//},
{
    Path: '../../ServiceStack.Text/src/ServiceStack.Text/ServiceStack.Text.csproj',
    ReplaceElements: {
        ProjectGuid: '{579B3FDB-CDAD-44E1-8417-885C38E49A0E}',
        RootNamespace: 'ServiceStack.Text',
        AssemblyName: 'ServiceStack.Text',
    }
},
{
    Path: '../src/ServiceStack.Client/ServiceStack.Client.csproj',
    ReplaceElements: {
        ProjectGuid: '{42E1C8C0-A163-44CC-92B1-8F416F2C0B01}',
        RootNamespace: 'ServiceStack',
        AssemblyName: 'ServiceStack.Client'
    },
    ReplaceTexts: {
        '<!--ItemGroup,ProjectReference-->': [
            '<ItemGroup>',
            '  <ProjectReference Include="..\\ServiceStack.Interfaces\\ServiceStack.Interfaces.csproj">',
            '    <Project>{55942102-033A-4DA8-A6AF-1DB7B2F34A2D}</Project>',
            '    <Name>ServiceStack.Interfaces</Name>',
            '  </ProjectReference>',
            '</ItemGroup>',
            '<ItemGroup>',
            '  <Reference Include="ServiceStack.Text">',
            '    <HintPath>..\\..\\lib\\ServiceStack.Text.dll</HintPath>',
            '  </Reference>',
            '</ItemGroup>'
        ].join('\n')
    },
    ReplaceTemplate: function (code, tmpl) {
        if (code == 'SL5') {
            //tmpl = tmpl.replace(/ServiceStack.Interfaces.SL5/g, 'ServiceStack.Interfaces');
            tmpl = tmpl.replace('<HintPath>..\\..\\lib\\ServiceStack.Text.dll</HintPath>', '<HintPath>..\\..\\lib\\sl5\\ServiceStack.Text.dll</HintPath>');
        }
        else if (code == 'PCL') {
            tmpl = tmpl.replace('<HintPath>..\\..\\lib\\ServiceStack.Text.dll</HintPath>', '<HintPath>..\\..\\lib\\pcl\\ServiceStack.Text.dll</HintPath>');
        }
        return tmpl;
    }
}];

var CUSTOM_MERGE_FRAGMENTS = [
    ["ItemGroup", "Compile"],
    ["ItemGroup", "Content"],
    ["ItemGroup", "None"]
];

CUSTOM_TEMPLATES.forEach(function(tmpl) {
    log(tmpl.Path);
    var originalTemplateXml = readTextFile(tmpl.Path);
    log(originalTemplateXml);

    CUSTOM_PROJS.forEach(function (proj) {
        var tmplXml = originalTemplateXml;

        Object.keys(proj.ReplaceElements || {}).forEach(function (elName) {
            var from = '<' + elName + '>' + tmpl[elName] + '</' + elName + '>';
            var to = '<' + elName + '>' + proj.ReplaceElements[elName] + '</' + elName + '>';
            log('\nReplaceElements(' + from + ',' + to + ')\n');
            tmplXml = tmplXml.replace(from, to);
        });

        Object.keys(proj.ReplaceTexts || {}).forEach(function (from) {
            var code = tmpl.getCode ? tmpl.getCode(proj) : tmpl.Code;
            var to = proj.ReplaceTexts[from].replace(/\$Code/g, code);
            log('\nReplaceTexts(' + from + ',' + to + ')\n');
            tmplXml = tmplXml.replace(new RegExp(from,'g'), to);
        });

        var xml = readTextFile(proj.Path);
        log(proj.Path);
        //log(xml);

        CUSTOM_MERGE_FRAGMENTS.forEach(function (seekCombo) {
            var strict = false;
            var parser = sax.parser(strict);

            var matches = [];
            var root;
            var found = false;
            var comboFound = [];

            parser.onopentag = function (el) {
                var tag = seekCombo[0] || '';
                var tagToMatch = tag.toUpperCase();

                if (el.name == tagToMatch) {

                    var isRoot = comboFound.length == 0;
                    if (isRoot) {
                        log("found open root <" + tag + "> at: ", this.line + ":" + this.column + ", " + this.startTagPosition);
                        root = { line: this.line, column: this.column, startTagPosition: this.startTagPosition };
                    }

                    comboFound.unshift(seekCombo.shift());

                    if (seekCombo.length == 0 && !found) {
                        log("found open match <" + tag + "> at: ", this.line + ":" + this.column + ", " + this.startTagPosition);
                        found = true;
                    }
                }
            };

            parser.onclosetag = function (name) {
                if (comboFound.length > 0) {
                    var tag = comboFound[0];
                    var tagToMatch = tag.toUpperCase();

                    if (name == tagToMatch) {

                        seekCombo.unshift(comboFound.shift());

                        if (comboFound.length == 0 && found) {
                            log("found close </" + tag + "> at: ", this.line + ":" + this.column + ", " + this.startTagPosition);
                            var match = { start: root, end: { line: this.line, column: this.column, startTagPosition: this.startTagPosition } };
                            matches.push(match);
                            found = false;

                            var startPos = match.start.startTagPosition + match.start.column;
                            var endPos = match.end.startTagPosition - 1;
                            var fragment = xml.substring(startPos, endPos);
                            log("fragment for " + seekCombo);
                            log(match);
                            //log(fragment);
                            var placeholder = "<!--" + seekCombo + "-->";
                            var withFragment = "<" + seekCombo[0] + ">\n " + fragment + "</" + seekCombo[0] + ">";
                            tmplXml = tmplXml.replace(placeholder, withFragment);
                        }
                    }
                }
            };

            parser.write(xml);
        });
        
        if (proj.ReplaceTemplate) {
            tmplXml = proj.ReplaceTemplate(tmpl.Code, tmplXml);
        }

        var customProjPath = path.join(path.dirname(proj.Path), path.basename(proj.Path).replace(".csproj", "." + tmpl.Code + ".csproj"));
        log("\nwriting transformedXml to: " + customProjPath);
        log(tmplXml);

        fs.writeFileSync(customProjPath, tmplXml);
    });
});