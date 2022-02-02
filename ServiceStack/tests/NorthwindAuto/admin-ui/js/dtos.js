/*minify:*/
exports.AdminDeleteUser = exports.AdminUpdateUser = exports.AdminCreateUser = exports.AdminQueryUsers = exports.AdminGetUser = exports.AdminDeleteUserResponse = exports.AdminUsersResponse = exports.AdminUserResponse = exports.AdminUserBase = void 0;
var AdminUserBase = /** @class */ (function () {
    function AdminUserBase(init) {
        Object.assign(this, init);
    }
    return AdminUserBase;
}());
var AdminUserResponse = /** @class */ (function () {
    function AdminUserResponse(init) {
        Object.assign(this, init);
    }
    return AdminUserResponse;
}());
exports.AdminUserResponse = AdminUserResponse;

var AdminUsersResponse = /** @class */ (function () {
    function AdminUsersResponse(init) {
        Object.assign(this, init);
    }
    return AdminUsersResponse;
}());
exports.AdminUsersResponse = AdminUsersResponse;

var AdminDeleteUserResponse = /** @class */ (function () {
    function AdminDeleteUserResponse(init) {
        Object.assign(this, init);
    }
    return AdminDeleteUserResponse;
}());
exports.AdminDeleteUserResponse = AdminDeleteUserResponse;

var AdminGetUser = /** @class */ (function () {
    function AdminGetUser(init) {
        Object.assign(this, init);
    }
    AdminGetUser.prototype.createResponse = function () { return new AdminUserResponse(); };
    AdminGetUser.prototype.getTypeName = function () { return 'AdminGetUser'; };
    AdminGetUser.prototype.getMethod = function () { return 'GET'; };
    return AdminGetUser;
}());
exports.AdminGetUser = AdminGetUser;

var AdminQueryUsers = /** @class */ (function () {
    function AdminQueryUsers(init) {
        Object.assign(this, init);
    }
    AdminQueryUsers.prototype.createResponse = function () { return new AdminUsersResponse(); };
    AdminQueryUsers.prototype.getTypeName = function () { return 'AdminQueryUsers'; };
    AdminQueryUsers.prototype.getMethod = function () { return 'GET'; };
    return AdminQueryUsers;
}());
exports.AdminQueryUsers = AdminQueryUsers;

var AdminCreateUser = /** @class */ (function (_super) {
    __extends(AdminCreateUser, _super);
    function AdminCreateUser(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    AdminCreateUser.prototype.createResponse = function () { return new AdminUserResponse(); };
    AdminCreateUser.prototype.getTypeName = function () { return 'AdminCreateUser'; };
    AdminCreateUser.prototype.getMethod = function () { return 'POST'; };
    return AdminCreateUser;
}(AdminUserBase));
exports.AdminCreateUser = AdminCreateUser;

var AdminUpdateUser = /** @class */ (function (_super) {
    __extends(AdminUpdateUser, _super);
    function AdminUpdateUser(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    AdminUpdateUser.prototype.createResponse = function () { return new AdminUserResponse(); };
    AdminUpdateUser.prototype.getTypeName = function () { return 'AdminUpdateUser'; };
    AdminUpdateUser.prototype.getMethod = function () { return 'PUT'; };
    return AdminUpdateUser;
}(AdminUserBase));
exports.AdminUpdateUser = AdminUpdateUser;

var AdminDeleteUser = /** @class */ (function () {
    function AdminDeleteUser(init) {
        Object.assign(this, init);
    }
    AdminDeleteUser.prototype.createResponse = function () { return new AdminDeleteUserResponse(); };
    AdminDeleteUser.prototype.getTypeName = function () { return 'AdminDeleteUser'; };
    AdminDeleteUser.prototype.getMethod = function () { return 'DELETE'; };
    return AdminDeleteUser;
}());
exports.AdminDeleteUser = AdminDeleteUser;
/*:minify*/
