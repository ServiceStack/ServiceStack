"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var client_1 = require("@servicestack/client");
exports.client = new client_1.JsonServiceClient();
exports.nameRules = [
    function (v) { return !!v || 'Name is required'; },
];
exports.emailRules = [
    function (v) { return !!v || 'E-mail is required'; },
    function (v) { return /^\w+([\.-]?\w+)*@\w+([\.-]?\w+)*(\.\w{2,3})+$/.test(v) || 'E-mail must be valid'; }
];
exports.passwordRules = [
    function (v) { return !!v || 'Password is required'; },
    function (v) { return v.length > 6 || 'Password must be grater than 6 characters'; }
];
exports.confirmPasswordRules = [
    function (v) { return !!v || 'Password is required'; }
];
//# sourceMappingURL=shared.js.map