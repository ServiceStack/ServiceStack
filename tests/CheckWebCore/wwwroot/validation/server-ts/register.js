"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
var client_1 = require("@servicestack/client");
client_1.bootstrap(); //converts data-invalid attributes into Bootstrap v4 error messages.
client_1.bindHandlers({
    newUser: function (u) {
        var $ = function (sel) { return document.querySelector(sel); };
        var names = u.split('@');
        $("[name=displayName]").value = client_1.toPascalCase(names[0]) + " " + client_1.toPascalCase(client_1.splitOnFirst(names[1], '.')[0]);
        $("[name=email]").value = u;
        $("[name=password]").value = $("[name=confirmPassword]").value = 'p@55wOrd';
    }
});
//# sourceMappingURL=register.js.map