"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var client_1 = require("@servicestack/client");
var form = document.querySelector("form");
client_1.bootstrapForm(form, {
    success: function () {
        location.href = '/validation/server-ts/contacts/';
    }
});
//# sourceMappingURL=edit.js.map