"use strict";
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
var shared_1 = require("./shared");
var dtos_1 = require("../../dtos");
new vue_1.Vue({
    el: '#app',
    computed: {
        errorSummary: function () {
            return client_1.errorResponseExcept.call(this, 'userName,password');
        },
    },
    methods: {
        submit: function () {
            return __awaiter(this, void 0, void 0, function () {
                var form, response, e_1;
                return __generator(this, function (_a) {
                    switch (_a.label) {
                        case 0:
                            form = this.$refs.form;
                            if (!form.validate()) return [3 /*break*/, 5];
                            _a.label = 1;
                        case 1:
                            _a.trys.push([1, 3, 4, 5]);
                            this.loading = true;
                            return [4 /*yield*/, shared_1.client.post(new dtos_1.Authenticate({
                                    provider: 'credentials',
                                    userName: this.userName,
                                    password: this.password,
                                    rememberMe: this.rememberMe,
                                }))];
                        case 2:
                            response = _a.sent();
                            location.href = CONTINUE;
                            return [3 /*break*/, 5];
                        case 3:
                            e_1 = _a.sent();
                            this.responseStatus = e_1.responseStatus || e_1;
                            return [3 /*break*/, 5];
                        case 4:
                            this.loading = false;
                            form.resetValidation();
                            return [7 /*endfinally*/];
                        case 5: return [2 /*return*/];
                    }
                });
            });
        },
        switchUser: function (email) {
            this.userName = email;
            this.password = 'p@55wOrd';
        },
        errorResponse: client_1.errorResponse
    },
    data: function () { return ({
        loading: false,
        valid: true,
        userName: "",
        password: "",
        rememberMe: true,
        emailRules: shared_1.emailRules, passwordRules: shared_1.passwordRules,
        responseStatus: null
    }); },
});
//# sourceMappingURL=login.js.map