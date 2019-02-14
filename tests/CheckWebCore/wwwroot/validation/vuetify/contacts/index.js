"use strict";
var __assign = (this && this.__assign) || function () {
    __assign = Object.assign || function(t) {
        for (var s, i = 1, n = arguments.length; i < n; i++) {
            s = arguments[i];
            for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
                t[p] = s[p];
        }
        return t;
    };
    return __assign.apply(this, arguments);
};
var __awaiter = (this && this.__awaiter) || function (thisArg, _arguments, P, generator) {
    return new (P || (P = Promise))(function (resolve, reject) {
        function fulfilled(value) { try { step(generator.next(value)); } catch (e) { reject(e); } }
        function rejected(value) { try { step(generator["throw"](value)); } catch (e) { reject(e); } }
        function step(result) { result.done ? resolve(result.value) : new P(function (resolve) { resolve(result.value); }).then(fulfilled, rejected); }
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
Object.defineProperty(exports, "__esModule", { value: true });
var vue_1 = require("vue");
var client_1 = require("@servicestack/client");
var shared_1 = require("../shared");
var dtos_1 = require("../../../dtos");
new vue_1.Vue({
    el: '#app',
    computed: {
        heading: function () {
            return this.update ? 'Edit new Contact' : 'Add new Contact';
        },
        action: function () {
            return this.update ? 'Update Contact' : 'Add Contact';
        },
        errorSummary: function () {
            return client_1.errorResponseExcept.call(this, 'title,name,color,filmGenres,age,agree');
        },
    },
    methods: {
        submit: function () {
            return __awaiter(this, void 0, void 0, function () {
                var form, request, e_1;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            form = this.$refs.form;
                            if (!form.validate()) return [3 /*break*/, 10];
                            _a.label = 1;
                        case 1:
                            _a.trys.push([1, 6, 7, 8]);
                            this.loading = true;
                            request = {
                                title: this.title,
                                name: this.name,
                                color: this.color,
                                filmGenres: this.filmGenres,
                                age: this.age,
                            };
                            if (!this.update) return [3 /*break*/, 3];
                            return [4 /*yield*/, shared_1.client.post(new dtos_1.UpdateContact(__assign({}, request, { id: this.id })))];
                        case 2:
                            _a.sent();
                            return [3 /*break*/, 5];
                        case 3: return [4 /*yield*/, shared_1.client.post(new dtos_1.CreateContact(__assign({}, request, { agree: this.agree })))];
                        case 4:
                            _a.sent();
                            _a.label = 5;
                        case 5:
                            this.update = false;
                            form.reset();
                            return [3 /*break*/, 8];
                        case 6:
                            e_1 = _a.sent();
                            this.responseStatus = e_1.responseStatus || e_1;
                            return [3 /*break*/, 8];
                        case 7:
                            this.loading = false;
                            form.resetValidation();
                            return [7 /*endfinally*/];
                        case 8: return [4 /*yield*/, this.refresh()];
                        case 9:
                            _a.sent();
                            _a.label = 10;
                        case 10: return [2 /*return*/];
                    }
                });
            });
        },
        refresh: function () {
            return __awaiter(this, void 0, void 0, function () {
                var _a;
                return __generator(this, function (_b) {
                    switch (_b.label) {
                        case 0:
                            _a = this;
                            return [4 /*yield*/, shared_1.client.get(new dtos_1.GetContacts())];
                        case 1:
                            _a.contacts = (_b.sent()).results;
                            return [2 /*return*/];
                    }
                });
            });
        },
        reset: function () {
            this.$refs.form.reset();
        },
        cancel: function () {
            this.reset();
            this.update = false;
        },
        edit: function (id) {
            return __awaiter(this, void 0, void 0, function () {
                var contact;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            this.update = true;
                            return [4 /*yield*/, shared_1.client.get(new dtos_1.GetContact({ id: id }))];
                        case 1:
                            contact = (_a.sent()).result;
                            console.log(contact);
                            Object.assign(this, contact);
                            return [2 /*return*/];
                    }
                });
            });
        },
        remove: function (id) {
            return __awaiter(this, void 0, void 0, function () {
                var response;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            if (!confirm('Are you sure?'))
                                return [2 /*return*/];
                            return [4 /*yield*/, shared_1.client.delete(new dtos_1.DeleteContact({ id: id }))];
                        case 1:
                            _a.sent();
                            return [4 /*yield*/, shared_1.client.get(new dtos_1.GetContacts())];
                        case 2:
                            response = _a.sent();
                            return [4 /*yield*/, this.refresh()];
                        case 3:
                            _a.sent();
                            return [2 /*return*/];
                    }
                });
            });
        },
        errorResponse: client_1.errorResponse
    },
    data: function () { return (__assign({ loading: false, valid: true, update: false }, DATA, { id: 0, title: "", name: "", color: "", filmGenres: [], age: 13, agree: false, nameRules: shared_1.nameRules, responseStatus: null })); },
});
//# sourceMappingURL=index.js.map