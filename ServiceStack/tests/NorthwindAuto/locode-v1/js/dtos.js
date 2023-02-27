exports.QueryResponse = exports.QueryBase = exports.QueryDb = exports.GetCrudEvents = void 0

var QueryResponse = /** @class */ (function () {
    function QueryResponse(init) {
        Object.assign(this, init);
    }
    return QueryResponse;
}());
exports.QueryResponse = QueryResponse;

var QueryBase = /** @class */ (function () {
    function QueryBase(init) {
        Object.assign(this, init);
    }
    return QueryBase;
}());
exports.QueryBase = QueryBase;

var QueryDb = /** @class */ (function (_super) {
    __extends(QueryDb, _super);
    function QueryDb(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return QueryDb;
}(QueryBase));
exports.QueryBase = QueryBase

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
exports.GetCrudEvents = GetCrudEvents 
