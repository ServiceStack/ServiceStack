var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    function adopt(value) { return value instanceof P ? value : new P(function (resolve) { resolve(value); }); }
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : adopt(result.value).then(fulfilled, rejected); }
        step((generator = generator.apply(thisArg, _arguments || [])).next());
    });
};
var __generator = (this && this.__generator) || function (thisArg, body) {
    var _ = { label: 0, sent: function() { if (t[0] & 1) throw t[1]; return t[1]; }, trys: [], ops: [] }, f, y, t, g;
    return g = { next: verb(0), "throw": verb(1), "return": verb(2) }, typeof Symbol === "function" && (g[Symbol.iterator] = function() { return this; }), g;
    function verb(n) { return function (v) { return step([n, v]); }; }
    function step(op) {
        if (f) throw new TypeError("Generator is already executing.");
        while (_) try {
            if (f = 1, y && (t = op[0] & 2 ? y["return"] : op[0] ? y["throw"] || ((t = y["return"]) && t.call(y), 0) : y.next) && !(t = t.call(y, op[1])).done) return t;
            if (y = 0, t) op = [op[0] & 2, t.value];
            switch (op[0]) {
                case 0: case 1: t = op; break;
                case 4: _.label++; return { value: op[1], done: false };
                case 5: _.label++; y = op[1]; op = [0]; continue;
                case 7: op = _.ops.pop(); _.trys.pop(); continue;
                default:
                    if (!(t = _.trys, t = t.length > 0 && t[t.length - 1]) && (op[0] === 6 || op[0] === 2)) { _ = 0; continue; }
                    if (op[0] === 3 && (!t || (op[1] > t[0] && op[1] < t[3]))) { _.label = op[1]; break; }
                    if (op[0] === 6 && _.label < t[1]) { _.label = t[1]; t = op; break; }
                    if (t && _.label < t[2]) { _.label = t[2]; _.ops.push(op); break; }
                    if (t[2]) _.ops.pop();
                    _.trys.pop(); continue;
            }
            op = body.call(thisArg, _);
        } catch (e) { op = [6, e]; y = 0; } finally { f = t = 0; }
        if (op[0] & 5) throw op[1]; return { value: op[0] ? op[1] : void 0, done: true };
    }
};
(function (factory) {
    if (typeof module === "object" && typeof module.exports === "object") {
        var v = factory(require, exports);
        if (v !== undefined) module.exports = v;
    }
    else if (typeof define === "function" && define.amd) {
        define(["require", "exports"], factory);
    }
    else if (typeof window != "undefined") factory(window.require||function(){}, window["@servicestack/desktop"]={});
})(function (require, exports) {
    "use strict";
    Object.defineProperty(exports, "__esModule", { value: true });
    function invokeHostJsonMethod(target, args) {
        return __awaiter(this, void 0, void 0, function () {
            var formData, k, r, body, e_1;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        formData = new FormData();
                        for (k in args) {
                            if (!args.hasOwnProperty(k))
                                continue;
                            formData.append(k, args[k]);
                        }
                        _a.label = 1;
                    case 1:
                        _a.trys.push([1, 4, , 5]);
                        return [4 /*yield*/, fetch("https://host/script", {
                                method: "POST",
                                body: formData
                            })];
                    case 2:
                        r = _a.sent();
                        return [4 /*yield*/, r.text()];
                    case 3:
                        body = _a.sent();
                        if (r.ok)
                            return [2 /*return*/, body.length > 0 ? JSON.parse(body) : null];
                        throw body;
                    case 4:
                        e_1 = _a.sent();
                        throw e_1;
                    case 5: return [2 /*return*/];
                }
            });
        });
    }
    exports.invokeHostJsonMethod = invokeHostJsonMethod;
    function invokeHostTextMethod(target, args) {
        return __awaiter(this, void 0, void 0, function () {
            var formData, k, r, e_2;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        formData = new FormData();
                        for (k in args) {
                            if (!args.hasOwnProperty(k))
                                continue;
                            formData.append(k, args[k]);
                        }
                        _a.label = 1;
                    case 1:
                        _a.trys.push([1, 4, , 5]);
                        return [4 /*yield*/, fetch("https://host/script", {
                                method: "POST",
                                body: formData
                            })];
                    case 2:
                        r = _a.sent();
                        return [4 /*yield*/, r.text()];
                    case 3: return [2 /*return*/, _a.sent()];
                    case 4:
                        e_2 = _a.sent();
                        throw e_2;
                    case 5: return [2 /*return*/];
                }
            });
        });
    }
    exports.invokeHostTextMethod = invokeHostTextMethod;
    function evaluateScript(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostJsonMethod('script', { 'EvaluateScript': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.evaluateScript = evaluateScript;
    function evaluateCode(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostJsonMethod('script', { 'EvaluateCode': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.evaluateCode = evaluateCode;
    function evaluateLisp(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostJsonMethod('script', { 'EvaluateLisp': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.evaluateLisp = evaluateLisp;
    function renderScript(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostTextMethod('script', { 'RenderScript': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.renderScript = renderScript;
    function renderCode(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostTextMethod('script', { 'RenderCode': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.renderCode = renderCode;
    function renderLisp(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostTextMethod('script', { 'RenderLisp': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.renderLisp = renderLisp;
    function evaluateScriptAsync(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostJsonMethod('script', { 'EvaluateScriptAsync': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.evaluateScriptAsync = evaluateScriptAsync;
    function evaluateCodeAsync(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostJsonMethod('script', { 'EvaluateCodeAsync': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.evaluateCodeAsync = evaluateCodeAsync;
    function evaluateLispAsync(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostJsonMethod('script', { 'EvaluateLispAsync': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.evaluateLispAsync = evaluateLispAsync;
    function renderScriptAsync(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostTextMethod('script', { 'RenderScriptAsync': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.renderScriptAsync = renderScriptAsync;
    function renderCodeAsync(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostTextMethod('script', { 'RenderCodeAsync': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.renderCodeAsync = renderCodeAsync;
    function renderLispAsync(scriptSrc) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, invokeHostTextMethod('script', { 'RenderLispAsync': scriptSrc })];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.renderLispAsync = renderLispAsync;
    function quote(text) {
        return '"' + text.replace('"', '\\"') + '"';
    }
    function desktopInfo() {
        return __awaiter(this, void 0, void 0, function () { return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, evaluateCode('desktopInfo')];
                case 1: return [2 /*return*/, (_a.sent())];
            }
        }); });
    }
    exports.desktopInfo = desktopInfo;
    /**
     * Send Window to Foreground
     * @param windowName - The name of the window to send to foreground, supported: browser
     */
    function sendToForeground(windowName) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, evaluateCode('desktopInfo')];
                    case 1: return [2 /*return*/, (_a.sent())];
                }
            });
        });
    }
    exports.sendToForeground = sendToForeground;
    function expandEnvVars(name) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, evaluateCode("expandEnvVars(" + quote(name) + ")")];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.expandEnvVars = expandEnvVars;
    /**
     * Get Clipboard Contents as a UTF-8 string
     */
    function clipboard() {
        return __awaiter(this, void 0, void 0, function () { return __generator(this, function (_a) {
            switch (_a.label) {
                case 0: return [4 /*yield*/, evaluateCode('clipboard')];
                case 1: return [2 /*return*/, _a.sent()];
            }
        }); });
    }
    exports.clipboard = clipboard;
    /**
     * Set the Clipboard Contents with a UTF-8 string
     */
    function setClipboard(contents) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, evaluateCode("setClipboard(" + quote(contents) + ")")];
                    case 1: return [2 /*return*/, _a.sent()];
                }
            });
        });
    }
    exports.setClipboard = setClipboard;
    var OpenFolderFlags;
    (function (OpenFolderFlags) {
        OpenFolderFlags[OpenFolderFlags["AllowMultiSelect"] = 512] = "AllowMultiSelect";
        OpenFolderFlags[OpenFolderFlags["CreatePrompt"] = 8192] = "CreatePrompt";
        OpenFolderFlags[OpenFolderFlags["DontAddToRecent"] = 33554432] = "DontAddToRecent";
        OpenFolderFlags[OpenFolderFlags["EnableHook"] = 32] = "EnableHook";
        OpenFolderFlags[OpenFolderFlags["EnableIncludeNotify"] = 4194304] = "EnableIncludeNotify";
        OpenFolderFlags[OpenFolderFlags["EnableSizing"] = 8388608] = "EnableSizing";
        OpenFolderFlags[OpenFolderFlags["EnableTemplate"] = 64] = "EnableTemplate";
        OpenFolderFlags[OpenFolderFlags["EnableTemplateHandle"] = 128] = "EnableTemplateHandle";
        OpenFolderFlags[OpenFolderFlags["Explorer"] = 524288] = "Explorer";
        OpenFolderFlags[OpenFolderFlags["ExtensionIsDifferent"] = 1024] = "ExtensionIsDifferent";
        OpenFolderFlags[OpenFolderFlags["FileMustExist"] = 4096] = "FileMustExist";
        OpenFolderFlags[OpenFolderFlags["ForceShowHidden"] = 268435456] = "ForceShowHidden";
        OpenFolderFlags[OpenFolderFlags["HideReadOnly"] = 4] = "HideReadOnly";
        OpenFolderFlags[OpenFolderFlags["LongNames"] = 2097152] = "LongNames";
        OpenFolderFlags[OpenFolderFlags["NoChangeDir"] = 8] = "NoChangeDir";
        OpenFolderFlags[OpenFolderFlags["NoDereferenceLinks"] = 1048576] = "NoDereferenceLinks";
        OpenFolderFlags[OpenFolderFlags["NoLongNames"] = 262144] = "NoLongNames";
        OpenFolderFlags[OpenFolderFlags["NoNetworkButton"] = 131072] = "NoNetworkButton";
        OpenFolderFlags[OpenFolderFlags["NoReadOnlyReturn"] = 32768] = "NoReadOnlyReturn";
        OpenFolderFlags[OpenFolderFlags["NoTestFileCreate"] = 65536] = "NoTestFileCreate";
        OpenFolderFlags[OpenFolderFlags["NoValidate"] = 256] = "NoValidate";
        OpenFolderFlags[OpenFolderFlags["OverwritePrompt"] = 2] = "OverwritePrompt";
        OpenFolderFlags[OpenFolderFlags["PathMustExist"] = 2048] = "PathMustExist";
        OpenFolderFlags[OpenFolderFlags["ReadOnly"] = 1] = "ReadOnly";
        OpenFolderFlags[OpenFolderFlags["ShareAware"] = 16384] = "ShareAware";
        OpenFolderFlags[OpenFolderFlags["ShowHelp"] = 16] = "ShowHelp";
    })(OpenFolderFlags = exports.OpenFolderFlags || (exports.OpenFolderFlags = {}));
    function openFolder(options) {
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, evaluateCode("openFolder(" + JSON.stringify(options) + ")")];
                    case 1: return [2 /*return*/, (_a.sent())];
                }
            });
        });
    }
    exports.openFolder = openFolder;
    var MessageBoxType;
    (function (MessageBoxType) {
        MessageBoxType[MessageBoxType["AbortRetryIgnore"] = 2] = "AbortRetryIgnore";
        MessageBoxType[MessageBoxType["CancelTryContinue"] = 6] = "CancelTryContinue";
        MessageBoxType[MessageBoxType["Help"] = 16384] = "Help";
        MessageBoxType[MessageBoxType["Ok"] = 0] = "Ok";
        MessageBoxType[MessageBoxType["OkCancel"] = 1] = "OkCancel";
        MessageBoxType[MessageBoxType["RetryCancel"] = 5] = "RetryCancel";
        MessageBoxType[MessageBoxType["YesNo"] = 4] = "YesNo";
        MessageBoxType[MessageBoxType["YesNoCancel"] = 3] = "YesNoCancel";
        MessageBoxType[MessageBoxType["IconExclamation"] = 48] = "IconExclamation";
        MessageBoxType[MessageBoxType["IconWarning"] = 48] = "IconWarning";
        MessageBoxType[MessageBoxType["IconInformation"] = 64] = "IconInformation";
        MessageBoxType[MessageBoxType["IconQuestion"] = 32] = "IconQuestion";
        MessageBoxType[MessageBoxType["IconStop"] = 16] = "IconStop";
        MessageBoxType[MessageBoxType["DefaultButton1"] = 0] = "DefaultButton1";
        MessageBoxType[MessageBoxType["DefaultButton2"] = 256] = "DefaultButton2";
        MessageBoxType[MessageBoxType["DefaultButton3"] = 512] = "DefaultButton3";
        MessageBoxType[MessageBoxType["DefaultButton4"] = 768] = "DefaultButton4";
        MessageBoxType[MessageBoxType["AppModal"] = 0] = "AppModal";
        MessageBoxType[MessageBoxType["SystemModal"] = 4096] = "SystemModal";
        MessageBoxType[MessageBoxType["TaskModal"] = 8192] = "TaskModal";
        MessageBoxType[MessageBoxType["DefaultDesktopOnly"] = 131072] = "DefaultDesktopOnly";
        MessageBoxType[MessageBoxType["RightJustified"] = 524288] = "RightJustified";
        MessageBoxType[MessageBoxType["RightToLeftReading"] = 1048576] = "RightToLeftReading";
        MessageBoxType[MessageBoxType["SetForeground"] = 65536] = "SetForeground";
        MessageBoxType[MessageBoxType["TopMost"] = 262144] = "TopMost";
        MessageBoxType[MessageBoxType["ServiceNotification"] = 2097152] = "ServiceNotification";
    })(MessageBoxType = exports.MessageBoxType || (exports.MessageBoxType = {}));
    var MessageBoxReturn;
    (function (MessageBoxReturn) {
        MessageBoxReturn[MessageBoxReturn["Abort"] = 3] = "Abort";
        MessageBoxReturn[MessageBoxReturn["Cancel"] = 2] = "Cancel";
        MessageBoxReturn[MessageBoxReturn["Continue"] = 11] = "Continue";
        MessageBoxReturn[MessageBoxReturn["Ignore"] = 5] = "Ignore";
        MessageBoxReturn[MessageBoxReturn["No"] = 7] = "No";
        MessageBoxReturn[MessageBoxReturn["Ok"] = 1] = "Ok";
        MessageBoxReturn[MessageBoxReturn["Retry"] = 4] = "Retry";
        MessageBoxReturn[MessageBoxReturn["TryAgain"] = 10] = "TryAgain";
        MessageBoxReturn[MessageBoxReturn["Yes"] = 6] = "Yes";
    })(MessageBoxReturn = exports.MessageBoxReturn || (exports.MessageBoxReturn = {}));
    /**
     * Refer to Win32 API
     * https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-messagebox
     * @param title
     * @param caption
     * @param type
     */
    function messageBox(title, caption, type) {
        if (caption === void 0) { caption = ""; }
        if (type === void 0) { type = MessageBoxType.Ok; }
        return __awaiter(this, void 0, void 0, function () {
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0: return [4 /*yield*/, evaluateCode("messageBox(" + quote(title) + "," + quote(caption) + "," + type + ")")];
                    case 1: return [2 /*return*/, (_a.sent())];
                }
            });
        });
    }
    exports.messageBox = messageBox;
});
