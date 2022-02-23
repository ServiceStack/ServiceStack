import { QueryDb, QueryResponse } from "../../lib/types"

var GetCrudEvents = /** @class */ (function (_super) {
    __extends(GetCrudEvents, _super)
    function GetCrudEvents(init) {
        let _this = _super.call(this, init) || this;
        Object.assign(_this, init)
        return _this
    }
    GetCrudEvents.prototype.createResponse = function () { return new QueryResponse() }
    GetCrudEvents.prototype.getTypeName = function () { return 'GetCrudEvents' }
    GetCrudEvents.prototype.getMethod = function () { return 'GET' }
    return GetCrudEvents
}(QueryDb))
