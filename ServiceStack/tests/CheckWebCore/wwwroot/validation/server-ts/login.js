"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var client_1 = require("@servicestack/client");
client_1.bootstrap(); //converts data-invalid attributes into Bootstrap v4 error messages.
client_1.bindHandlers({
    switchUser: function (u) {
        document.querySelector("[name=userName]").value = u;
        document.querySelector("[name=password]").value = 'p@55wOrd';
    }
});
//# sourceMappingURL=login.js.map