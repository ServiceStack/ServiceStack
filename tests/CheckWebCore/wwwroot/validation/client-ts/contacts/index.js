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
var client_1 = require("@servicestack/client");
var dtos_1 = require("../../../dtos");
var client = new client_1.JsonServiceClient();
var form = document.querySelector("form");
client_1.bootstrapForm(form, {
    success: function (r) {
        form.reset();
        CONTACTS.push(r.result);
        render();
    }
});
client_1.bindHandlers({
    deleteContact: function (id) {
        return __awaiter(this, void 0, void 0, function () {
            var response;
            return __generator(this, function (_a) {
                switch (_a.label) {
                    case 0:
                        if (!confirm('Are you sure?'))
                            return [2 /*return*/];
                        return [4 /*yield*/, client.delete(new dtos_1.DeleteContact({ id: id }))];
                    case 1:
                        _a.sent();
                        return [4 /*yield*/, client.get(new dtos_1.GetContacts())];
                    case 2:
                        response = _a.sent();
                        CONTACTS = response.results;
                        render();
                        return [2 /*return*/];
                }
            });
        });
    }
});
var contactRow = function (contact) {
    return "<tr style=\"background:" + contact.color + "\">\n        <td>" + contact.title + " " + contact.name + " (" + contact.age + ")</td>\n        <td><a href=\"/validation/client-ts/contacts/" + contact.id + "/edit\">edit</a></td>\n        <td><button class=\"btn btn-sm btn-primary\" data-click=\"deleteContact:" + contact.id + "\">delete</button></td>\n    </tr>";
};
function render() {
    var sb = "";
    if (CONTACTS.length > 0) {
        for (var i = 0; i < CONTACTS.length; i++) {
            sb += contactRow(CONTACTS[i]);
        }
    }
    else {
        sb = "<tr><td>There are no contacts.</td></tr>";
    }
    document.querySelector("#results").innerHTML = "<tbody>" + sb + "</tbody>";
}
render();
//# sourceMappingURL=index.js.map