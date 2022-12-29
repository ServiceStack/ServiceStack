"use strict";
/* Options:
Date: 2019-02-11 10:08:04
Version: 5.41
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:55799

//GlobalNamespace:
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion:
//AddDescriptionAsComments: True
//IncludeTypes:
//ExcludeTypes:
//DefaultImports:
*/
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
var QueryBase = /** @class */ (function () {
    function QueryBase(init) {
        Object.assign(this, init);
    }
    return QueryBase;
}());
exports.QueryBase = QueryBase;
var QueryData = /** @class */ (function (_super) {
    __extends(QueryData, _super);
    function QueryData(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return QueryData;
}(QueryBase));
exports.QueryData = QueryData;
var RequestLogEntry = /** @class */ (function () {
    function RequestLogEntry(init) {
        Object.assign(this, init);
    }
    return RequestLogEntry;
}());
exports.RequestLogEntry = RequestLogEntry;
// @DataContract
var ResponseError = /** @class */ (function () {
    function ResponseError(init) {
        Object.assign(this, init);
    }
    return ResponseError;
}());
exports.ResponseError = ResponseError;
// @DataContract
var ResponseStatus = /** @class */ (function () {
    function ResponseStatus(init) {
        Object.assign(this, init);
    }
    return ResponseStatus;
}());
exports.ResponseStatus = ResponseStatus;
var QueryDb_1 = /** @class */ (function (_super) {
    __extends(QueryDb_1, _super);
    function QueryDb_1(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return QueryDb_1;
}(QueryBase));
exports.QueryDb_1 = QueryDb_1;
var Rockstar = /** @class */ (function () {
    function Rockstar(init) {
        Object.assign(this, init);
    }
    return Rockstar;
}());
exports.Rockstar = Rockstar;
var ArrayElementInDictionary = /** @class */ (function () {
    function ArrayElementInDictionary(init) {
        Object.assign(this, init);
    }
    return ArrayElementInDictionary;
}());
exports.ArrayElementInDictionary = ArrayElementInDictionary;
var ObjectDesign = /** @class */ (function () {
    function ObjectDesign(init) {
        Object.assign(this, init);
    }
    return ObjectDesign;
}());
exports.ObjectDesign = ObjectDesign;
// @DataContract
var AuthUserSession = /** @class */ (function () {
    function AuthUserSession(init) {
        Object.assign(this, init);
    }
    return AuthUserSession;
}());
exports.AuthUserSession = AuthUserSession;
var MetadataTestNestedChild = /** @class */ (function () {
    function MetadataTestNestedChild(init) {
        Object.assign(this, init);
    }
    return MetadataTestNestedChild;
}());
exports.MetadataTestNestedChild = MetadataTestNestedChild;
var MetadataTestChild = /** @class */ (function () {
    function MetadataTestChild(init) {
        Object.assign(this, init);
    }
    return MetadataTestChild;
}());
exports.MetadataTestChild = MetadataTestChild;
var MenuItemExampleItem = /** @class */ (function () {
    function MenuItemExampleItem(init) {
        Object.assign(this, init);
    }
    return MenuItemExampleItem;
}());
exports.MenuItemExampleItem = MenuItemExampleItem;
var MenuItemExample = /** @class */ (function () {
    function MenuItemExample(init) {
        Object.assign(this, init);
    }
    return MenuItemExample;
}());
exports.MenuItemExample = MenuItemExample;
// @DataContract
var MenuExample = /** @class */ (function () {
    function MenuExample(init) {
        Object.assign(this, init);
    }
    return MenuExample;
}());
exports.MenuExample = MenuExample;
var MetadataTypeName = /** @class */ (function () {
    function MetadataTypeName(init) {
        Object.assign(this, init);
    }
    return MetadataTypeName;
}());
exports.MetadataTypeName = MetadataTypeName;
var MetadataRoute = /** @class */ (function () {
    function MetadataRoute(init) {
        Object.assign(this, init);
    }
    return MetadataRoute;
}());
exports.MetadataRoute = MetadataRoute;
var MetadataDataContract = /** @class */ (function () {
    function MetadataDataContract(init) {
        Object.assign(this, init);
    }
    return MetadataDataContract;
}());
exports.MetadataDataContract = MetadataDataContract;
var MetadataDataMember = /** @class */ (function () {
    function MetadataDataMember(init) {
        Object.assign(this, init);
    }
    return MetadataDataMember;
}());
exports.MetadataDataMember = MetadataDataMember;
var MetadataAttribute = /** @class */ (function () {
    function MetadataAttribute(init) {
        Object.assign(this, init);
    }
    return MetadataAttribute;
}());
exports.MetadataAttribute = MetadataAttribute;
var MetadataPropertyType = /** @class */ (function () {
    function MetadataPropertyType(init) {
        Object.assign(this, init);
    }
    return MetadataPropertyType;
}());
exports.MetadataPropertyType = MetadataPropertyType;
var MetadataType = /** @class */ (function () {
    function MetadataType(init) {
        Object.assign(this, init);
    }
    return MetadataType;
}());
exports.MetadataType = MetadataType;
var AutoQueryConvention = /** @class */ (function () {
    function AutoQueryConvention(init) {
        Object.assign(this, init);
    }
    return AutoQueryConvention;
}());
exports.AutoQueryConvention = AutoQueryConvention;
var AutoQueryViewerConfig = /** @class */ (function () {
    function AutoQueryViewerConfig(init) {
        Object.assign(this, init);
    }
    return AutoQueryViewerConfig;
}());
exports.AutoQueryViewerConfig = AutoQueryViewerConfig;
var AutoQueryViewerUserInfo = /** @class */ (function () {
    function AutoQueryViewerUserInfo(init) {
        Object.assign(this, init);
    }
    return AutoQueryViewerUserInfo;
}());
exports.AutoQueryViewerUserInfo = AutoQueryViewerUserInfo;
var AutoQueryOperation = /** @class */ (function () {
    function AutoQueryOperation(init) {
        Object.assign(this, init);
    }
    return AutoQueryOperation;
}());
exports.AutoQueryOperation = AutoQueryOperation;
var RecursiveNode = /** @class */ (function () {
    function RecursiveNode(init) {
        Object.assign(this, init);
    }
    RecursiveNode.prototype.createResponse = function () { return new RecursiveNode(); };
    RecursiveNode.prototype.getTypeName = function () { return 'RecursiveNode'; };
    return RecursiveNode;
}());
exports.RecursiveNode = RecursiveNode;
var NativeTypesTestService = /** @class */ (function () {
    function NativeTypesTestService(init) {
        Object.assign(this, init);
    }
    return NativeTypesTestService;
}());
exports.NativeTypesTestService = NativeTypesTestService;
var NestedClass = /** @class */ (function () {
    function NestedClass(init) {
        Object.assign(this, init);
    }
    return NestedClass;
}());
exports.NestedClass = NestedClass;
var ListResult = /** @class */ (function () {
    function ListResult(init) {
        Object.assign(this, init);
    }
    return ListResult;
}());
exports.ListResult = ListResult;
var OnlyInReturnListArg = /** @class */ (function () {
    function OnlyInReturnListArg(init) {
        Object.assign(this, init);
    }
    return OnlyInReturnListArg;
}());
exports.OnlyInReturnListArg = OnlyInReturnListArg;
var ArrayResult = /** @class */ (function () {
    function ArrayResult(init) {
        Object.assign(this, init);
    }
    return ArrayResult;
}());
exports.ArrayResult = ArrayResult;
var EnumType;
(function (EnumType) {
    EnumType["Value1"] = "Value1";
    EnumType["Value2"] = "Value2";
})(EnumType = exports.EnumType || (exports.EnumType = {}));
var EnumWithValues;
(function (EnumWithValues) {
    EnumWithValues["Value1"] = "1";
    EnumWithValues["Value2"] = "2";
})(EnumWithValues = exports.EnumWithValues || (exports.EnumWithValues = {}));
// @Flags()
var EnumFlags;
(function (EnumFlags) {
    EnumFlags[EnumFlags["Value0"] = 0] = "Value0";
    EnumFlags[EnumFlags["Value1"] = 1] = "Value1";
    EnumFlags[EnumFlags["Value2"] = 2] = "Value2";
    EnumFlags[EnumFlags["Value3"] = 3] = "Value3";
    EnumFlags[EnumFlags["Value123"] = 3] = "Value123";
})(EnumFlags = exports.EnumFlags || (exports.EnumFlags = {}));
var EnumStyle;
(function (EnumStyle) {
    EnumStyle["lower"] = "lower";
    EnumStyle["UPPER"] = "UPPER";
    EnumStyle["PascalCase"] = "PascalCase";
    EnumStyle["camelCase"] = "camelCase";
    EnumStyle["camelUPPER"] = "camelUPPER";
    EnumStyle["PascalUPPER"] = "PascalUPPER";
})(EnumStyle = exports.EnumStyle || (exports.EnumStyle = {}));
var Poco = /** @class */ (function () {
    function Poco(init) {
        Object.assign(this, init);
    }
    return Poco;
}());
exports.Poco = Poco;
var AllCollectionTypes = /** @class */ (function () {
    function AllCollectionTypes(init) {
        Object.assign(this, init);
    }
    return AllCollectionTypes;
}());
exports.AllCollectionTypes = AllCollectionTypes;
var KeyValuePair = /** @class */ (function () {
    function KeyValuePair(init) {
        Object.assign(this, init);
    }
    return KeyValuePair;
}());
exports.KeyValuePair = KeyValuePair;
var SubType = /** @class */ (function () {
    function SubType(init) {
        Object.assign(this, init);
    }
    return SubType;
}());
exports.SubType = SubType;
var HelloBase = /** @class */ (function () {
    function HelloBase(init) {
        Object.assign(this, init);
    }
    return HelloBase;
}());
exports.HelloBase = HelloBase;
var HelloResponseBase = /** @class */ (function () {
    function HelloResponseBase(init) {
        Object.assign(this, init);
    }
    return HelloResponseBase;
}());
exports.HelloResponseBase = HelloResponseBase;
var HelloBase_1 = /** @class */ (function () {
    function HelloBase_1(init) {
        Object.assign(this, init);
    }
    return HelloBase_1;
}());
exports.HelloBase_1 = HelloBase_1;
var Item = /** @class */ (function () {
    function Item(init) {
        Object.assign(this, init);
    }
    return Item;
}());
exports.Item = Item;
var InheritedItem = /** @class */ (function () {
    function InheritedItem(init) {
        Object.assign(this, init);
    }
    return InheritedItem;
}());
exports.InheritedItem = InheritedItem;
var HelloWithReturnResponse = /** @class */ (function () {
    function HelloWithReturnResponse(init) {
        Object.assign(this, init);
    }
    return HelloWithReturnResponse;
}());
exports.HelloWithReturnResponse = HelloWithReturnResponse;
var HelloType = /** @class */ (function () {
    function HelloType(init) {
        Object.assign(this, init);
    }
    return HelloType;
}());
exports.HelloType = HelloType;
var EmptyClass = /** @class */ (function () {
    function EmptyClass(init) {
        Object.assign(this, init);
    }
    return EmptyClass;
}());
exports.EmptyClass = EmptyClass;
var TypeB = /** @class */ (function () {
    function TypeB(init) {
        Object.assign(this, init);
    }
    return TypeB;
}());
exports.TypeB = TypeB;
var TypeA = /** @class */ (function () {
    function TypeA(init) {
        Object.assign(this, init);
    }
    return TypeA;
}());
exports.TypeA = TypeA;
var InnerType = /** @class */ (function () {
    function InnerType(init) {
        Object.assign(this, init);
    }
    return InnerType;
}());
exports.InnerType = InnerType;
var InnerEnum;
(function (InnerEnum) {
    InnerEnum["Foo"] = "Foo";
    InnerEnum["Bar"] = "Bar";
    InnerEnum["Baz"] = "Baz";
})(InnerEnum = exports.InnerEnum || (exports.InnerEnum = {}));
var InnerTypeItem = /** @class */ (function () {
    function InnerTypeItem(init) {
        Object.assign(this, init);
    }
    return InnerTypeItem;
}());
exports.InnerTypeItem = InnerTypeItem;
var DayOfWeek;
(function (DayOfWeek) {
    DayOfWeek["Sunday"] = "Sunday";
    DayOfWeek["Monday"] = "Monday";
    DayOfWeek["Tuesday"] = "Tuesday";
    DayOfWeek["Wednesday"] = "Wednesday";
    DayOfWeek["Thursday"] = "Thursday";
    DayOfWeek["Friday"] = "Friday";
    DayOfWeek["Saturday"] = "Saturday";
})(DayOfWeek = exports.DayOfWeek || (exports.DayOfWeek = {}));
// @DataContract
var ShortDays;
(function (ShortDays) {
    ShortDays["Monday"] = "MON";
    ShortDays["Tuesday"] = "TUE";
    ShortDays["Wednesday"] = "WED";
    ShortDays["Thursday"] = "THU";
    ShortDays["Friday"] = "FRI";
    ShortDays["Saturday"] = "SAT";
    ShortDays["Sunday"] = "SUN";
})(ShortDays = exports.ShortDays || (exports.ShortDays = {}));
// @DataContract
var ScopeType;
(function (ScopeType) {
    ScopeType["Global"] = "1";
    ScopeType["Sale"] = "2";
})(ScopeType = exports.ScopeType || (exports.ScopeType = {}));
var Tuple_2 = /** @class */ (function () {
    function Tuple_2(init) {
        Object.assign(this, init);
    }
    return Tuple_2;
}());
exports.Tuple_2 = Tuple_2;
var Tuple_3 = /** @class */ (function () {
    function Tuple_3(init) {
        Object.assign(this, init);
    }
    return Tuple_3;
}());
exports.Tuple_3 = Tuple_3;
// @Flags()
var CacheControl;
(function (CacheControl) {
    CacheControl[CacheControl["None"] = 0] = "None";
    CacheControl[CacheControl["Public"] = 1] = "Public";
    CacheControl[CacheControl["Private"] = 2] = "Private";
    CacheControl[CacheControl["MustRevalidate"] = 4] = "MustRevalidate";
    CacheControl[CacheControl["NoCache"] = 8] = "NoCache";
    CacheControl[CacheControl["NoStore"] = 16] = "NoStore";
    CacheControl[CacheControl["NoTransform"] = 32] = "NoTransform";
    CacheControl[CacheControl["ProxyRevalidate"] = 64] = "ProxyRevalidate";
})(CacheControl = exports.CacheControl || (exports.CacheControl = {}));
var MyColor;
(function (MyColor) {
    MyColor["Red"] = "Red";
    MyColor["Green"] = "Green";
    MyColor["Blue"] = "Blue";
})(MyColor = exports.MyColor || (exports.MyColor = {}));
var SwaggerNestedModel = /** @class */ (function () {
    function SwaggerNestedModel(init) {
        Object.assign(this, init);
    }
    return SwaggerNestedModel;
}());
exports.SwaggerNestedModel = SwaggerNestedModel;
var SwaggerNestedModel2 = /** @class */ (function () {
    function SwaggerNestedModel2(init) {
        Object.assign(this, init);
    }
    return SwaggerNestedModel2;
}());
exports.SwaggerNestedModel2 = SwaggerNestedModel2;
var MyEnum;
(function (MyEnum) {
    MyEnum["A"] = "A";
    MyEnum["B"] = "B";
    MyEnum["C"] = "C";
})(MyEnum = exports.MyEnum || (exports.MyEnum = {}));
// @DataContract
var UserApiKey = /** @class */ (function () {
    function UserApiKey(init) {
        Object.assign(this, init);
    }
    return UserApiKey;
}());
exports.UserApiKey = UserApiKey;
var PgRockstar = /** @class */ (function (_super) {
    __extends(PgRockstar, _super);
    function PgRockstar(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return PgRockstar;
}(Rockstar));
exports.PgRockstar = PgRockstar;
var QueryDb_2 = /** @class */ (function (_super) {
    __extends(QueryDb_2, _super);
    function QueryDb_2(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return QueryDb_2;
}(QueryBase));
exports.QueryDb_2 = QueryDb_2;
var CustomRockstar = /** @class */ (function () {
    function CustomRockstar(init) {
        Object.assign(this, init);
    }
    return CustomRockstar;
}());
exports.CustomRockstar = CustomRockstar;
var Movie = /** @class */ (function () {
    function Movie(init) {
        Object.assign(this, init);
    }
    return Movie;
}());
exports.Movie = Movie;
var RockstarAlbum = /** @class */ (function () {
    function RockstarAlbum(init) {
        Object.assign(this, init);
    }
    return RockstarAlbum;
}());
exports.RockstarAlbum = RockstarAlbum;
var RockstarReference = /** @class */ (function () {
    function RockstarReference(init) {
        Object.assign(this, init);
    }
    return RockstarReference;
}());
exports.RockstarReference = RockstarReference;
var OnlyDefinedInGenericType = /** @class */ (function () {
    function OnlyDefinedInGenericType(init) {
        Object.assign(this, init);
    }
    return OnlyDefinedInGenericType;
}());
exports.OnlyDefinedInGenericType = OnlyDefinedInGenericType;
var OnlyDefinedInGenericTypeFrom = /** @class */ (function () {
    function OnlyDefinedInGenericTypeFrom(init) {
        Object.assign(this, init);
    }
    return OnlyDefinedInGenericTypeFrom;
}());
exports.OnlyDefinedInGenericTypeFrom = OnlyDefinedInGenericTypeFrom;
var OnlyDefinedInGenericTypeInto = /** @class */ (function () {
    function OnlyDefinedInGenericTypeInto(init) {
        Object.assign(this, init);
    }
    return OnlyDefinedInGenericTypeInto;
}());
exports.OnlyDefinedInGenericTypeInto = OnlyDefinedInGenericTypeInto;
var TypesGroup = /** @class */ (function () {
    function TypesGroup(init) {
        Object.assign(this, init);
    }
    return TypesGroup;
}());
exports.TypesGroup = TypesGroup;
// @DataContract
var QueryResponse = /** @class */ (function () {
    function QueryResponse(init) {
        Object.assign(this, init);
    }
    return QueryResponse;
}());
exports.QueryResponse = QueryResponse;
var ChangeRequestResponse = /** @class */ (function () {
    function ChangeRequestResponse(init) {
        Object.assign(this, init);
    }
    return ChangeRequestResponse;
}());
exports.ChangeRequestResponse = ChangeRequestResponse;
var DiscoverTypes = /** @class */ (function () {
    function DiscoverTypes(init) {
        Object.assign(this, init);
    }
    DiscoverTypes.prototype.createResponse = function () { return new DiscoverTypes(); };
    DiscoverTypes.prototype.getTypeName = function () { return 'DiscoverTypes'; };
    return DiscoverTypes;
}());
exports.DiscoverTypes = DiscoverTypes;
var CustomHttpErrorResponse = /** @class */ (function () {
    function CustomHttpErrorResponse(init) {
        Object.assign(this, init);
    }
    return CustomHttpErrorResponse;
}());
exports.CustomHttpErrorResponse = CustomHttpErrorResponse;
// @Route("/alwaysthrows")
var AlwaysThrows = /** @class */ (function () {
    function AlwaysThrows(init) {
        Object.assign(this, init);
    }
    AlwaysThrows.prototype.createResponse = function () { return new AlwaysThrows(); };
    AlwaysThrows.prototype.getTypeName = function () { return 'AlwaysThrows'; };
    return AlwaysThrows;
}());
exports.AlwaysThrows = AlwaysThrows;
// @Route("/alwaysthrowsfilterattribute")
var AlwaysThrowsFilterAttribute = /** @class */ (function () {
    function AlwaysThrowsFilterAttribute(init) {
        Object.assign(this, init);
    }
    AlwaysThrowsFilterAttribute.prototype.createResponse = function () { return new AlwaysThrowsFilterAttribute(); };
    AlwaysThrowsFilterAttribute.prototype.getTypeName = function () { return 'AlwaysThrowsFilterAttribute'; };
    return AlwaysThrowsFilterAttribute;
}());
exports.AlwaysThrowsFilterAttribute = AlwaysThrowsFilterAttribute;
// @Route("/alwaysthrowsglobalfilter")
var AlwaysThrowsGlobalFilter = /** @class */ (function () {
    function AlwaysThrowsGlobalFilter(init) {
        Object.assign(this, init);
    }
    AlwaysThrowsGlobalFilter.prototype.createResponse = function () { return new AlwaysThrowsGlobalFilter(); };
    AlwaysThrowsGlobalFilter.prototype.getTypeName = function () { return 'AlwaysThrowsGlobalFilter'; };
    return AlwaysThrowsGlobalFilter;
}());
exports.AlwaysThrowsGlobalFilter = AlwaysThrowsGlobalFilter;
var CustomFieldHttpErrorResponse = /** @class */ (function () {
    function CustomFieldHttpErrorResponse(init) {
        Object.assign(this, init);
    }
    return CustomFieldHttpErrorResponse;
}());
exports.CustomFieldHttpErrorResponse = CustomFieldHttpErrorResponse;
var NoRepeatResponse = /** @class */ (function () {
    function NoRepeatResponse(init) {
        Object.assign(this, init);
    }
    return NoRepeatResponse;
}());
exports.NoRepeatResponse = NoRepeatResponse;
var BatchThrowsResponse = /** @class */ (function () {
    function BatchThrowsResponse(init) {
        Object.assign(this, init);
    }
    return BatchThrowsResponse;
}());
exports.BatchThrowsResponse = BatchThrowsResponse;
var ObjectDesignResponse = /** @class */ (function () {
    function ObjectDesignResponse(init) {
        Object.assign(this, init);
    }
    return ObjectDesignResponse;
}());
exports.ObjectDesignResponse = ObjectDesignResponse;
var CreateJwtResponse = /** @class */ (function () {
    function CreateJwtResponse(init) {
        Object.assign(this, init);
    }
    return CreateJwtResponse;
}());
exports.CreateJwtResponse = CreateJwtResponse;
var CreateRefreshJwtResponse = /** @class */ (function () {
    function CreateRefreshJwtResponse(init) {
        Object.assign(this, init);
    }
    return CreateRefreshJwtResponse;
}());
exports.CreateRefreshJwtResponse = CreateRefreshJwtResponse;
var MetadataTestResponse = /** @class */ (function () {
    function MetadataTestResponse(init) {
        Object.assign(this, init);
    }
    return MetadataTestResponse;
}());
exports.MetadataTestResponse = MetadataTestResponse;
// @DataContract
var GetExampleResponse = /** @class */ (function () {
    function GetExampleResponse(init) {
        Object.assign(this, init);
    }
    return GetExampleResponse;
}());
exports.GetExampleResponse = GetExampleResponse;
var AutoQueryMetadataResponse = /** @class */ (function () {
    function AutoQueryMetadataResponse(init) {
        Object.assign(this, init);
    }
    return AutoQueryMetadataResponse;
}());
exports.AutoQueryMetadataResponse = AutoQueryMetadataResponse;
var TestAttributeExport = /** @class */ (function () {
    function TestAttributeExport(init) {
        Object.assign(this, init);
    }
    TestAttributeExport.prototype.createResponse = function () { return new TestAttributeExport(); };
    TestAttributeExport.prototype.getTypeName = function () { return 'TestAttributeExport'; };
    return TestAttributeExport;
}());
exports.TestAttributeExport = TestAttributeExport;
// @DataContract
var HelloACodeGenTestResponse = /** @class */ (function () {
    function HelloACodeGenTestResponse(init) {
        Object.assign(this, init);
    }
    return HelloACodeGenTestResponse;
}());
exports.HelloACodeGenTestResponse = HelloACodeGenTestResponse;
var HelloResponse = /** @class */ (function () {
    function HelloResponse(init) {
        Object.assign(this, init);
    }
    return HelloResponse;
}());
exports.HelloResponse = HelloResponse;
/**
 * Description on HelloAllResponse type
 */
// @DataContract
var HelloAnnotatedResponse = /** @class */ (function () {
    function HelloAnnotatedResponse(init) {
        Object.assign(this, init);
    }
    return HelloAnnotatedResponse;
}());
exports.HelloAnnotatedResponse = HelloAnnotatedResponse;
var HelloList = /** @class */ (function () {
    function HelloList(init) {
        Object.assign(this, init);
    }
    HelloList.prototype.createResponse = function () { return new Array(); };
    HelloList.prototype.getTypeName = function () { return 'HelloList'; };
    return HelloList;
}());
exports.HelloList = HelloList;
var HelloArray = /** @class */ (function () {
    function HelloArray(init) {
        Object.assign(this, init);
    }
    HelloArray.prototype.createResponse = function () { return new Array(); };
    HelloArray.prototype.getTypeName = function () { return 'HelloArray'; };
    return HelloArray;
}());
exports.HelloArray = HelloArray;
var HelloExistingResponse = /** @class */ (function () {
    function HelloExistingResponse(init) {
        Object.assign(this, init);
    }
    return HelloExistingResponse;
}());
exports.HelloExistingResponse = HelloExistingResponse;
var AllTypes = /** @class */ (function () {
    function AllTypes(init) {
        Object.assign(this, init);
    }
    AllTypes.prototype.createResponse = function () { return new AllTypes(); };
    AllTypes.prototype.getTypeName = function () { return 'AllTypes'; };
    return AllTypes;
}());
exports.AllTypes = AllTypes;
var HelloAllTypesResponse = /** @class */ (function () {
    function HelloAllTypesResponse(init) {
        Object.assign(this, init);
    }
    return HelloAllTypesResponse;
}());
exports.HelloAllTypesResponse = HelloAllTypesResponse;
// @DataContract
var HelloWithDataContractResponse = /** @class */ (function () {
    function HelloWithDataContractResponse(init) {
        Object.assign(this, init);
    }
    return HelloWithDataContractResponse;
}());
exports.HelloWithDataContractResponse = HelloWithDataContractResponse;
/**
 * Description on HelloWithDescriptionResponse type
 */
var HelloWithDescriptionResponse = /** @class */ (function () {
    function HelloWithDescriptionResponse(init) {
        Object.assign(this, init);
    }
    return HelloWithDescriptionResponse;
}());
exports.HelloWithDescriptionResponse = HelloWithDescriptionResponse;
var HelloWithInheritanceResponse = /** @class */ (function (_super) {
    __extends(HelloWithInheritanceResponse, _super);
    function HelloWithInheritanceResponse(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return HelloWithInheritanceResponse;
}(HelloResponseBase));
exports.HelloWithInheritanceResponse = HelloWithInheritanceResponse;
var HelloWithAlternateReturnResponse = /** @class */ (function (_super) {
    __extends(HelloWithAlternateReturnResponse, _super);
    function HelloWithAlternateReturnResponse(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return HelloWithAlternateReturnResponse;
}(HelloWithReturnResponse));
exports.HelloWithAlternateReturnResponse = HelloWithAlternateReturnResponse;
var HelloWithRouteResponse = /** @class */ (function () {
    function HelloWithRouteResponse(init) {
        Object.assign(this, init);
    }
    return HelloWithRouteResponse;
}());
exports.HelloWithRouteResponse = HelloWithRouteResponse;
var HelloWithTypeResponse = /** @class */ (function () {
    function HelloWithTypeResponse(init) {
        Object.assign(this, init);
    }
    return HelloWithTypeResponse;
}());
exports.HelloWithTypeResponse = HelloWithTypeResponse;
var HelloStruct = /** @class */ (function () {
    function HelloStruct(init) {
        Object.assign(this, init);
    }
    HelloStruct.prototype.createResponse = function () { return new HelloStruct(); };
    HelloStruct.prototype.getTypeName = function () { return 'HelloStruct'; };
    return HelloStruct;
}());
exports.HelloStruct = HelloStruct;
var HelloSessionResponse = /** @class */ (function () {
    function HelloSessionResponse(init) {
        Object.assign(this, init);
    }
    return HelloSessionResponse;
}());
exports.HelloSessionResponse = HelloSessionResponse;
var HelloImplementsInterface = /** @class */ (function () {
    function HelloImplementsInterface(init) {
        Object.assign(this, init);
    }
    HelloImplementsInterface.prototype.createResponse = function () { return new HelloImplementsInterface(); };
    HelloImplementsInterface.prototype.getTypeName = function () { return 'HelloImplementsInterface'; };
    return HelloImplementsInterface;
}());
exports.HelloImplementsInterface = HelloImplementsInterface;
var Request1Response = /** @class */ (function () {
    function Request1Response(init) {
        Object.assign(this, init);
    }
    return Request1Response;
}());
exports.Request1Response = Request1Response;
var Request2Response = /** @class */ (function () {
    function Request2Response(init) {
        Object.assign(this, init);
    }
    return Request2Response;
}());
exports.Request2Response = Request2Response;
var HelloInnerTypesResponse = /** @class */ (function () {
    function HelloInnerTypesResponse(init) {
        Object.assign(this, init);
    }
    return HelloInnerTypesResponse;
}());
exports.HelloInnerTypesResponse = HelloInnerTypesResponse;
var CustomUserSession = /** @class */ (function (_super) {
    __extends(CustomUserSession, _super);
    function CustomUserSession(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return CustomUserSession;
}(AuthUserSession));
exports.CustomUserSession = CustomUserSession;
// @DataContract
var QueryResponseTemplate = /** @class */ (function () {
    function QueryResponseTemplate(init) {
        Object.assign(this, init);
    }
    return QueryResponseTemplate;
}());
exports.QueryResponseTemplate = QueryResponseTemplate;
var HelloVerbResponse = /** @class */ (function () {
    function HelloVerbResponse(init) {
        Object.assign(this, init);
    }
    return HelloVerbResponse;
}());
exports.HelloVerbResponse = HelloVerbResponse;
var EnumResponse = /** @class */ (function () {
    function EnumResponse(init) {
        Object.assign(this, init);
    }
    return EnumResponse;
}());
exports.EnumResponse = EnumResponse;
var ExcludeTestNested = /** @class */ (function () {
    function ExcludeTestNested(init) {
        Object.assign(this, init);
    }
    return ExcludeTestNested;
}());
exports.ExcludeTestNested = ExcludeTestNested;
var RestrictLocalhost = /** @class */ (function () {
    function RestrictLocalhost(init) {
        Object.assign(this, init);
    }
    RestrictLocalhost.prototype.createResponse = function () { return new RestrictLocalhost(); };
    RestrictLocalhost.prototype.getTypeName = function () { return 'RestrictLocalhost'; };
    return RestrictLocalhost;
}());
exports.RestrictLocalhost = RestrictLocalhost;
var RestrictInternal = /** @class */ (function () {
    function RestrictInternal(init) {
        Object.assign(this, init);
    }
    RestrictInternal.prototype.createResponse = function () { return new RestrictInternal(); };
    RestrictInternal.prototype.getTypeName = function () { return 'RestrictInternal'; };
    return RestrictInternal;
}());
exports.RestrictInternal = RestrictInternal;
var HelloTuple = /** @class */ (function () {
    function HelloTuple(init) {
        Object.assign(this, init);
    }
    HelloTuple.prototype.createResponse = function () { return new HelloTuple(); };
    HelloTuple.prototype.getTypeName = function () { return 'HelloTuple'; };
    return HelloTuple;
}());
exports.HelloTuple = HelloTuple;
var HelloAuthenticatedResponse = /** @class */ (function () {
    function HelloAuthenticatedResponse(init) {
        Object.assign(this, init);
    }
    return HelloAuthenticatedResponse;
}());
exports.HelloAuthenticatedResponse = HelloAuthenticatedResponse;
var Echo = /** @class */ (function () {
    function Echo(init) {
        Object.assign(this, init);
    }
    return Echo;
}());
exports.Echo = Echo;
var ThrowHttpErrorResponse = /** @class */ (function () {
    function ThrowHttpErrorResponse(init) {
        Object.assign(this, init);
    }
    return ThrowHttpErrorResponse;
}());
exports.ThrowHttpErrorResponse = ThrowHttpErrorResponse;
var ThrowTypeResponse = /** @class */ (function () {
    function ThrowTypeResponse(init) {
        Object.assign(this, init);
    }
    return ThrowTypeResponse;
}());
exports.ThrowTypeResponse = ThrowTypeResponse;
var ThrowValidationResponse = /** @class */ (function () {
    function ThrowValidationResponse(init) {
        Object.assign(this, init);
    }
    return ThrowValidationResponse;
}());
exports.ThrowValidationResponse = ThrowValidationResponse;
var acsprofileResponse = /** @class */ (function () {
    function acsprofileResponse(init) {
        Object.assign(this, init);
    }
    return acsprofileResponse;
}());
exports.acsprofileResponse = acsprofileResponse;
var ReturnedDto = /** @class */ (function () {
    function ReturnedDto(init) {
        Object.assign(this, init);
    }
    return ReturnedDto;
}());
exports.ReturnedDto = ReturnedDto;
// @Route("/matchroute/html")
var MatchesHtml = /** @class */ (function () {
    function MatchesHtml(init) {
        Object.assign(this, init);
    }
    MatchesHtml.prototype.createResponse = function () { return new MatchesHtml(); };
    MatchesHtml.prototype.getTypeName = function () { return 'MatchesHtml'; };
    return MatchesHtml;
}());
exports.MatchesHtml = MatchesHtml;
// @Route("/matchroute/json")
var MatchesJson = /** @class */ (function () {
    function MatchesJson(init) {
        Object.assign(this, init);
    }
    MatchesJson.prototype.createResponse = function () { return new MatchesJson(); };
    MatchesJson.prototype.getTypeName = function () { return 'MatchesJson'; };
    return MatchesJson;
}());
exports.MatchesJson = MatchesJson;
var TimestampData = /** @class */ (function () {
    function TimestampData(init) {
        Object.assign(this, init);
    }
    return TimestampData;
}());
exports.TimestampData = TimestampData;
// @Route("/test/html")
var TestHtml = /** @class */ (function () {
    function TestHtml(init) {
        Object.assign(this, init);
    }
    TestHtml.prototype.createResponse = function () { return new TestHtml(); };
    TestHtml.prototype.getTypeName = function () { return 'TestHtml'; };
    return TestHtml;
}());
exports.TestHtml = TestHtml;
var ViewResponse = /** @class */ (function () {
    function ViewResponse(init) {
        Object.assign(this, init);
    }
    return ViewResponse;
}());
exports.ViewResponse = ViewResponse;
// @Route("/swagger/model")
var SwaggerModel = /** @class */ (function () {
    function SwaggerModel(init) {
        Object.assign(this, init);
    }
    SwaggerModel.prototype.createResponse = function () { return new SwaggerModel(); };
    SwaggerModel.prototype.getTypeName = function () { return 'SwaggerModel'; };
    return SwaggerModel;
}());
exports.SwaggerModel = SwaggerModel;
// @Route("/plain-dto")
var PlainDto = /** @class */ (function () {
    function PlainDto(init) {
        Object.assign(this, init);
    }
    PlainDto.prototype.createResponse = function () { return new PlainDto(); };
    PlainDto.prototype.getTypeName = function () { return 'PlainDto'; };
    return PlainDto;
}());
exports.PlainDto = PlainDto;
// @Route("/httpresult-dto")
var HttpResultDto = /** @class */ (function () {
    function HttpResultDto(init) {
        Object.assign(this, init);
    }
    HttpResultDto.prototype.createResponse = function () { return new HttpResultDto(); };
    HttpResultDto.prototype.getTypeName = function () { return 'HttpResultDto'; };
    return HttpResultDto;
}());
exports.HttpResultDto = HttpResultDto;
// @Route("/restrict/mq")
var TestMqRestriction = /** @class */ (function () {
    function TestMqRestriction(init) {
        Object.assign(this, init);
    }
    TestMqRestriction.prototype.createResponse = function () { return new TestMqRestriction(); };
    TestMqRestriction.prototype.getTypeName = function () { return 'TestMqRestriction'; };
    return TestMqRestriction;
}());
exports.TestMqRestriction = TestMqRestriction;
// @Route("/set-cache")
var SetCache = /** @class */ (function () {
    function SetCache(init) {
        Object.assign(this, init);
    }
    SetCache.prototype.createResponse = function () { return new SetCache(); };
    SetCache.prototype.getTypeName = function () { return 'SetCache'; };
    return SetCache;
}());
exports.SetCache = SetCache;
var SwaggerComplexResponse = /** @class */ (function () {
    function SwaggerComplexResponse(init) {
        Object.assign(this, init);
    }
    return SwaggerComplexResponse;
}());
exports.SwaggerComplexResponse = SwaggerComplexResponse;
/**
 * Api GET All
 */
// @Route("/swaggerexamples", "GET")
// @Api(Description="Api GET All")
var GetSwaggerExamples = /** @class */ (function () {
    function GetSwaggerExamples(init) {
        Object.assign(this, init);
    }
    GetSwaggerExamples.prototype.createResponse = function () { return new GetSwaggerExamples(); };
    GetSwaggerExamples.prototype.getTypeName = function () { return 'GetSwaggerExamples'; };
    return GetSwaggerExamples;
}());
exports.GetSwaggerExamples = GetSwaggerExamples;
/**
 * Api GET Id
 */
// @Route("/swaggerexamples/{Id}", "GET")
// @Api(Description="Api GET Id")
var GetSwaggerExample = /** @class */ (function () {
    function GetSwaggerExample(init) {
        Object.assign(this, init);
    }
    GetSwaggerExample.prototype.createResponse = function () { return new GetSwaggerExample(); };
    GetSwaggerExample.prototype.getTypeName = function () { return 'GetSwaggerExample'; };
    return GetSwaggerExample;
}());
exports.GetSwaggerExample = GetSwaggerExample;
/**
 * Api POST
 */
// @Route("/swaggerexamples", "POST")
// @Api(Description="Api POST")
var PostSwaggerExamples = /** @class */ (function () {
    function PostSwaggerExamples(init) {
        Object.assign(this, init);
    }
    PostSwaggerExamples.prototype.createResponse = function () { return new PostSwaggerExamples(); };
    PostSwaggerExamples.prototype.getTypeName = function () { return 'PostSwaggerExamples'; };
    return PostSwaggerExamples;
}());
exports.PostSwaggerExamples = PostSwaggerExamples;
/**
 * Api PUT Id
 */
// @Route("/swaggerexamples/{Id}", "PUT")
// @Api(Description="Api PUT Id")
var PutSwaggerExample = /** @class */ (function () {
    function PutSwaggerExample(init) {
        Object.assign(this, init);
    }
    PutSwaggerExample.prototype.createResponse = function () { return new PutSwaggerExample(); };
    PutSwaggerExample.prototype.getTypeName = function () { return 'PutSwaggerExample'; };
    return PutSwaggerExample;
}());
exports.PutSwaggerExample = PutSwaggerExample;
// @Route("/lists", "GET")
var GetLists = /** @class */ (function () {
    function GetLists(init) {
        Object.assign(this, init);
    }
    GetLists.prototype.createResponse = function () { return new GetLists(); };
    GetLists.prototype.getTypeName = function () { return 'GetLists'; };
    return GetLists;
}());
exports.GetLists = GetLists;
// @DataContract
var AuthenticateResponse = /** @class */ (function () {
    function AuthenticateResponse(init) {
        Object.assign(this, init);
    }
    return AuthenticateResponse;
}());
exports.AuthenticateResponse = AuthenticateResponse;
// @DataContract
var AssignRolesResponse = /** @class */ (function () {
    function AssignRolesResponse(init) {
        Object.assign(this, init);
    }
    return AssignRolesResponse;
}());
exports.AssignRolesResponse = AssignRolesResponse;
// @DataContract
var UnAssignRolesResponse = /** @class */ (function () {
    function UnAssignRolesResponse(init) {
        Object.assign(this, init);
    }
    return UnAssignRolesResponse;
}());
exports.UnAssignRolesResponse = UnAssignRolesResponse;
// @DataContract
var ConvertSessionToTokenResponse = /** @class */ (function () {
    function ConvertSessionToTokenResponse(init) {
        Object.assign(this, init);
    }
    return ConvertSessionToTokenResponse;
}());
exports.ConvertSessionToTokenResponse = ConvertSessionToTokenResponse;
// @DataContract
var GetAccessTokenResponse = /** @class */ (function () {
    function GetAccessTokenResponse(init) {
        Object.assign(this, init);
    }
    return GetAccessTokenResponse;
}());
exports.GetAccessTokenResponse = GetAccessTokenResponse;
// @DataContract
var GetApiKeysResponse = /** @class */ (function () {
    function GetApiKeysResponse(init) {
        Object.assign(this, init);
    }
    return GetApiKeysResponse;
}());
exports.GetApiKeysResponse = GetApiKeysResponse;
// @DataContract
var RegenerateApiKeysResponse = /** @class */ (function () {
    function RegenerateApiKeysResponse(init) {
        Object.assign(this, init);
    }
    return RegenerateApiKeysResponse;
}());
exports.RegenerateApiKeysResponse = RegenerateApiKeysResponse;
// @DataContract
var RegisterResponse = /** @class */ (function () {
    function RegisterResponse(init) {
        Object.assign(this, init);
    }
    return RegisterResponse;
}());
exports.RegisterResponse = RegisterResponse;
// @Route("/anontype")
var AnonType = /** @class */ (function () {
    function AnonType(init) {
        Object.assign(this, init);
    }
    return AnonType;
}());
exports.AnonType = AnonType;
// @Route("/query/requestlogs")
// @Route("/query/requestlogs/{Date}")
var QueryRequestLogs = /** @class */ (function (_super) {
    __extends(QueryRequestLogs, _super);
    function QueryRequestLogs(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRequestLogs.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRequestLogs.prototype.getTypeName = function () { return 'QueryRequestLogs'; };
    return QueryRequestLogs;
}(QueryData));
exports.QueryRequestLogs = QueryRequestLogs;
// @AutoQueryViewer(Name="Today\'s Logs", Title="Logs from Today")
var TodayLogs = /** @class */ (function (_super) {
    __extends(TodayLogs, _super);
    function TodayLogs(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    TodayLogs.prototype.createResponse = function () { return new QueryResponse(); };
    TodayLogs.prototype.getTypeName = function () { return 'TodayLogs'; };
    return TodayLogs;
}(QueryData));
exports.TodayLogs = TodayLogs;
var TodayErrorLogs = /** @class */ (function (_super) {
    __extends(TodayErrorLogs, _super);
    function TodayErrorLogs(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    TodayErrorLogs.prototype.createResponse = function () { return new QueryResponse(); };
    TodayErrorLogs.prototype.getTypeName = function () { return 'TodayErrorLogs'; };
    return TodayErrorLogs;
}(QueryData));
exports.TodayErrorLogs = TodayErrorLogs;
var YesterdayLogs = /** @class */ (function (_super) {
    __extends(YesterdayLogs, _super);
    function YesterdayLogs(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    YesterdayLogs.prototype.createResponse = function () { return new QueryResponse(); };
    YesterdayLogs.prototype.getTypeName = function () { return 'YesterdayLogs'; };
    return YesterdayLogs;
}(QueryData));
exports.YesterdayLogs = YesterdayLogs;
var YesterdayErrorLogs = /** @class */ (function (_super) {
    __extends(YesterdayErrorLogs, _super);
    function YesterdayErrorLogs(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    YesterdayErrorLogs.prototype.createResponse = function () { return new QueryResponse(); };
    YesterdayErrorLogs.prototype.getTypeName = function () { return 'YesterdayErrorLogs'; };
    return YesterdayErrorLogs;
}(QueryData));
exports.YesterdayErrorLogs = YesterdayErrorLogs;
// @Route("/query/rockstars")
var QueryRockstars = /** @class */ (function (_super) {
    __extends(QueryRockstars, _super);
    function QueryRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstars.prototype.getTypeName = function () { return 'QueryRockstars'; };
    return QueryRockstars;
}(QueryDb_1));
exports.QueryRockstars = QueryRockstars;
// @Route("/query/rockstars/cached")
var QueryRockstarsCached = /** @class */ (function (_super) {
    __extends(QueryRockstarsCached, _super);
    function QueryRockstarsCached(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarsCached.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsCached.prototype.getTypeName = function () { return 'QueryRockstarsCached'; };
    return QueryRockstarsCached;
}(QueryDb_1));
exports.QueryRockstarsCached = QueryRockstarsCached;
// @Route("/changerequest/{Id}")
var ChangeRequest = /** @class */ (function () {
    function ChangeRequest(init) {
        Object.assign(this, init);
    }
    ChangeRequest.prototype.createResponse = function () { return new ChangeRequestResponse(); };
    ChangeRequest.prototype.getTypeName = function () { return 'ChangeRequest'; };
    return ChangeRequest;
}());
exports.ChangeRequest = ChangeRequest;
// @Route("/compress/{Path*}")
var CompressFile = /** @class */ (function () {
    function CompressFile(init) {
        Object.assign(this, init);
    }
    return CompressFile;
}());
exports.CompressFile = CompressFile;
// @Route("/Routing/LeadPost.aspx")
var LegacyLeadPost = /** @class */ (function () {
    function LegacyLeadPost(init) {
        Object.assign(this, init);
    }
    return LegacyLeadPost;
}());
exports.LegacyLeadPost = LegacyLeadPost;
// @Route("/info/{Id}")
var Info = /** @class */ (function () {
    function Info(init) {
        Object.assign(this, init);
    }
    return Info;
}());
exports.Info = Info;
var CustomHttpError = /** @class */ (function () {
    function CustomHttpError(init) {
        Object.assign(this, init);
    }
    CustomHttpError.prototype.createResponse = function () { return new CustomHttpErrorResponse(); };
    CustomHttpError.prototype.getTypeName = function () { return 'CustomHttpError'; };
    return CustomHttpError;
}());
exports.CustomHttpError = CustomHttpError;
var CustomFieldHttpError = /** @class */ (function () {
    function CustomFieldHttpError(init) {
        Object.assign(this, init);
    }
    CustomFieldHttpError.prototype.createResponse = function () { return new CustomFieldHttpErrorResponse(); };
    CustomFieldHttpError.prototype.getTypeName = function () { return 'CustomFieldHttpError'; };
    return CustomFieldHttpError;
}());
exports.CustomFieldHttpError = CustomFieldHttpError;
var FallbackRoute = /** @class */ (function () {
    function FallbackRoute(init) {
        Object.assign(this, init);
    }
    return FallbackRoute;
}());
exports.FallbackRoute = FallbackRoute;
var NoRepeat = /** @class */ (function () {
    function NoRepeat(init) {
        Object.assign(this, init);
    }
    NoRepeat.prototype.createResponse = function () { return new NoRepeatResponse(); };
    NoRepeat.prototype.getTypeName = function () { return 'NoRepeat'; };
    return NoRepeat;
}());
exports.NoRepeat = NoRepeat;
var BatchThrows = /** @class */ (function () {
    function BatchThrows(init) {
        Object.assign(this, init);
    }
    BatchThrows.prototype.createResponse = function () { return new BatchThrowsResponse(); };
    BatchThrows.prototype.getTypeName = function () { return 'BatchThrows'; };
    return BatchThrows;
}());
exports.BatchThrows = BatchThrows;
var BatchThrowsAsync = /** @class */ (function () {
    function BatchThrowsAsync(init) {
        Object.assign(this, init);
    }
    BatchThrowsAsync.prototype.createResponse = function () { return new BatchThrowsResponse(); };
    BatchThrowsAsync.prototype.getTypeName = function () { return 'BatchThrowsAsync'; };
    return BatchThrowsAsync;
}());
exports.BatchThrowsAsync = BatchThrowsAsync;
// @Route("/code/object", "GET")
var ObjectId = /** @class */ (function () {
    function ObjectId(init) {
        Object.assign(this, init);
    }
    ObjectId.prototype.createResponse = function () { return new ObjectDesignResponse(); };
    ObjectId.prototype.getTypeName = function () { return 'ObjectId'; };
    return ObjectId;
}());
exports.ObjectId = ObjectId;
// @Route("/jwt")
var CreateJwt = /** @class */ (function (_super) {
    __extends(CreateJwt, _super);
    function CreateJwt(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    CreateJwt.prototype.createResponse = function () { return new CreateJwtResponse(); };
    CreateJwt.prototype.getTypeName = function () { return 'CreateJwt'; };
    return CreateJwt;
}(AuthUserSession));
exports.CreateJwt = CreateJwt;
// @Route("/jwt-refresh")
var CreateRefreshJwt = /** @class */ (function () {
    function CreateRefreshJwt(init) {
        Object.assign(this, init);
    }
    CreateRefreshJwt.prototype.createResponse = function () { return new CreateRefreshJwtResponse(); };
    CreateRefreshJwt.prototype.getTypeName = function () { return 'CreateRefreshJwt'; };
    return CreateRefreshJwt;
}());
exports.CreateRefreshJwt = CreateRefreshJwt;
var MetadataTest = /** @class */ (function () {
    function MetadataTest(init) {
        Object.assign(this, init);
    }
    MetadataTest.prototype.createResponse = function () { return new MetadataTestResponse(); };
    MetadataTest.prototype.getTypeName = function () { return 'MetadataTest'; };
    return MetadataTest;
}());
exports.MetadataTest = MetadataTest;
// @Route("/example", "GET")
// @DataContract
var GetExample = /** @class */ (function () {
    function GetExample(init) {
        Object.assign(this, init);
    }
    GetExample.prototype.createResponse = function () { return new GetExampleResponse(); };
    GetExample.prototype.getTypeName = function () { return 'GetExample'; };
    return GetExample;
}());
exports.GetExample = GetExample;
var MetadataRequest = /** @class */ (function () {
    function MetadataRequest(init) {
        Object.assign(this, init);
    }
    MetadataRequest.prototype.createResponse = function () { return new AutoQueryMetadataResponse(); };
    MetadataRequest.prototype.getTypeName = function () { return 'MetadataRequest'; };
    return MetadataRequest;
}());
exports.MetadataRequest = MetadataRequest;
var ExcludeMetadataProperty = /** @class */ (function () {
    function ExcludeMetadataProperty(init) {
        Object.assign(this, init);
    }
    return ExcludeMetadataProperty;
}());
exports.ExcludeMetadataProperty = ExcludeMetadataProperty;
// @Route("/namedconnection")
var NamedConnection = /** @class */ (function () {
    function NamedConnection(init) {
        Object.assign(this, init);
    }
    return NamedConnection;
}());
exports.NamedConnection = NamedConnection;
/**
 * Description for HelloACodeGenTest
 */
var HelloACodeGenTest = /** @class */ (function () {
    function HelloACodeGenTest(init) {
        Object.assign(this, init);
    }
    HelloACodeGenTest.prototype.createResponse = function () { return new HelloACodeGenTestResponse(); };
    HelloACodeGenTest.prototype.getTypeName = function () { return 'HelloACodeGenTest'; };
    return HelloACodeGenTest;
}());
exports.HelloACodeGenTest = HelloACodeGenTest;
var HelloInService = /** @class */ (function () {
    function HelloInService(init) {
        Object.assign(this, init);
    }
    HelloInService.prototype.createResponse = function () { return new HelloResponse(); };
    HelloInService.prototype.getTypeName = function () { return 'NativeTypesTestService.HelloInService'; };
    return HelloInService;
}());
exports.HelloInService = HelloInService;
// @Route("/hello")
// @Route("/hello/{Name}")
var Hello = /** @class */ (function () {
    function Hello(init) {
        Object.assign(this, init);
    }
    Hello.prototype.createResponse = function () { return new HelloResponse(); };
    Hello.prototype.getTypeName = function () { return 'Hello'; };
    return Hello;
}());
exports.Hello = Hello;
/**
 * Description on HelloAll type
 */
// @DataContract
var HelloAnnotated = /** @class */ (function () {
    function HelloAnnotated(init) {
        Object.assign(this, init);
    }
    HelloAnnotated.prototype.createResponse = function () { return new HelloAnnotatedResponse(); };
    HelloAnnotated.prototype.getTypeName = function () { return 'HelloAnnotated'; };
    return HelloAnnotated;
}());
exports.HelloAnnotated = HelloAnnotated;
var HelloWithNestedClass = /** @class */ (function () {
    function HelloWithNestedClass(init) {
        Object.assign(this, init);
    }
    HelloWithNestedClass.prototype.createResponse = function () { return new HelloResponse(); };
    HelloWithNestedClass.prototype.getTypeName = function () { return 'HelloWithNestedClass'; };
    return HelloWithNestedClass;
}());
exports.HelloWithNestedClass = HelloWithNestedClass;
var HelloReturnList = /** @class */ (function () {
    function HelloReturnList(init) {
        Object.assign(this, init);
    }
    HelloReturnList.prototype.createResponse = function () { return new Array(); };
    HelloReturnList.prototype.getTypeName = function () { return 'HelloReturnList'; };
    return HelloReturnList;
}());
exports.HelloReturnList = HelloReturnList;
var HelloExisting = /** @class */ (function () {
    function HelloExisting(init) {
        Object.assign(this, init);
    }
    HelloExisting.prototype.createResponse = function () { return new HelloExistingResponse(); };
    HelloExisting.prototype.getTypeName = function () { return 'HelloExisting'; };
    return HelloExisting;
}());
exports.HelloExisting = HelloExisting;
var HelloWithEnum = /** @class */ (function () {
    function HelloWithEnum(init) {
        Object.assign(this, init);
    }
    return HelloWithEnum;
}());
exports.HelloWithEnum = HelloWithEnum;
var RestrictedAttributes = /** @class */ (function () {
    function RestrictedAttributes(init) {
        Object.assign(this, init);
    }
    return RestrictedAttributes;
}());
exports.RestrictedAttributes = RestrictedAttributes;
/**
 * AllowedAttributes Description
 */
// @Route("/allowed-attributes", "GET")
// @Api(Description="AllowedAttributes Description")
// @ApiResponse(Description="Your request was not understood", StatusCode=400)
// @DataContract
var AllowedAttributes = /** @class */ (function () {
    function AllowedAttributes(init) {
        Object.assign(this, init);
    }
    return AllowedAttributes;
}());
exports.AllowedAttributes = AllowedAttributes;
/**
 * Multi Line Class
 */
// @Api(Description="Multi \r\nLine \r\nClass")
var HelloAttributeStringTest = /** @class */ (function () {
    function HelloAttributeStringTest(init) {
        Object.assign(this, init);
    }
    return HelloAttributeStringTest;
}());
exports.HelloAttributeStringTest = HelloAttributeStringTest;
var HelloAllTypes = /** @class */ (function () {
    function HelloAllTypes(init) {
        Object.assign(this, init);
    }
    HelloAllTypes.prototype.createResponse = function () { return new HelloAllTypesResponse(); };
    HelloAllTypes.prototype.getTypeName = function () { return 'HelloAllTypes'; };
    return HelloAllTypes;
}());
exports.HelloAllTypes = HelloAllTypes;
var HelloString = /** @class */ (function () {
    function HelloString(init) {
        Object.assign(this, init);
    }
    HelloString.prototype.createResponse = function () { return ''; };
    HelloString.prototype.getTypeName = function () { return 'HelloString'; };
    return HelloString;
}());
exports.HelloString = HelloString;
var HelloVoid = /** @class */ (function () {
    function HelloVoid(init) {
        Object.assign(this, init);
    }
    HelloVoid.prototype.createResponse = function () { };
    HelloVoid.prototype.getTypeName = function () { return 'HelloVoid'; };
    return HelloVoid;
}());
exports.HelloVoid = HelloVoid;
// @DataContract
var HelloWithDataContract = /** @class */ (function () {
    function HelloWithDataContract(init) {
        Object.assign(this, init);
    }
    HelloWithDataContract.prototype.createResponse = function () { return new HelloWithDataContractResponse(); };
    HelloWithDataContract.prototype.getTypeName = function () { return 'HelloWithDataContract'; };
    return HelloWithDataContract;
}());
exports.HelloWithDataContract = HelloWithDataContract;
/**
 * Description on HelloWithDescription type
 */
var HelloWithDescription = /** @class */ (function () {
    function HelloWithDescription(init) {
        Object.assign(this, init);
    }
    HelloWithDescription.prototype.createResponse = function () { return new HelloWithDescriptionResponse(); };
    HelloWithDescription.prototype.getTypeName = function () { return 'HelloWithDescription'; };
    return HelloWithDescription;
}());
exports.HelloWithDescription = HelloWithDescription;
var HelloWithInheritance = /** @class */ (function (_super) {
    __extends(HelloWithInheritance, _super);
    function HelloWithInheritance(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    HelloWithInheritance.prototype.createResponse = function () { return new HelloWithInheritanceResponse(); };
    HelloWithInheritance.prototype.getTypeName = function () { return 'HelloWithInheritance'; };
    return HelloWithInheritance;
}(HelloBase));
exports.HelloWithInheritance = HelloWithInheritance;
var HelloWithGenericInheritance = /** @class */ (function (_super) {
    __extends(HelloWithGenericInheritance, _super);
    function HelloWithGenericInheritance(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return HelloWithGenericInheritance;
}(HelloBase_1));
exports.HelloWithGenericInheritance = HelloWithGenericInheritance;
var HelloWithGenericInheritance2 = /** @class */ (function (_super) {
    __extends(HelloWithGenericInheritance2, _super);
    function HelloWithGenericInheritance2(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return HelloWithGenericInheritance2;
}(HelloBase_1));
exports.HelloWithGenericInheritance2 = HelloWithGenericInheritance2;
var HelloWithNestedInheritance = /** @class */ (function (_super) {
    __extends(HelloWithNestedInheritance, _super);
    function HelloWithNestedInheritance(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    return HelloWithNestedInheritance;
}(HelloBase_1));
exports.HelloWithNestedInheritance = HelloWithNestedInheritance;
var HelloWithListInheritance = /** @class */ (function (_super) {
    __extends(HelloWithListInheritance, _super);
    function HelloWithListInheritance(init) {
        var _this = _super.call(this) || this;
        Object.assign(_this, init);
        return _this;
    }
    return HelloWithListInheritance;
}(Array));
exports.HelloWithListInheritance = HelloWithListInheritance;
var HelloWithReturn = /** @class */ (function () {
    function HelloWithReturn(init) {
        Object.assign(this, init);
    }
    HelloWithReturn.prototype.createResponse = function () { return new HelloWithAlternateReturnResponse(); };
    HelloWithReturn.prototype.getTypeName = function () { return 'HelloWithReturn'; };
    return HelloWithReturn;
}());
exports.HelloWithReturn = HelloWithReturn;
// @Route("/helloroute")
var HelloWithRoute = /** @class */ (function () {
    function HelloWithRoute(init) {
        Object.assign(this, init);
    }
    HelloWithRoute.prototype.createResponse = function () { return new HelloWithRouteResponse(); };
    HelloWithRoute.prototype.getTypeName = function () { return 'HelloWithRoute'; };
    return HelloWithRoute;
}());
exports.HelloWithRoute = HelloWithRoute;
var HelloWithType = /** @class */ (function () {
    function HelloWithType(init) {
        Object.assign(this, init);
    }
    HelloWithType.prototype.createResponse = function () { return new HelloWithTypeResponse(); };
    HelloWithType.prototype.getTypeName = function () { return 'HelloWithType'; };
    return HelloWithType;
}());
exports.HelloWithType = HelloWithType;
var HelloSession = /** @class */ (function () {
    function HelloSession(init) {
        Object.assign(this, init);
    }
    HelloSession.prototype.createResponse = function () { return new HelloSessionResponse(); };
    HelloSession.prototype.getTypeName = function () { return 'HelloSession'; };
    return HelloSession;
}());
exports.HelloSession = HelloSession;
var HelloInterface = /** @class */ (function () {
    function HelloInterface(init) {
        Object.assign(this, init);
    }
    return HelloInterface;
}());
exports.HelloInterface = HelloInterface;
var Request1 = /** @class */ (function () {
    function Request1(init) {
        Object.assign(this, init);
    }
    Request1.prototype.createResponse = function () { return new Request1Response(); };
    Request1.prototype.getTypeName = function () { return 'Request1'; };
    return Request1;
}());
exports.Request1 = Request1;
var Request2 = /** @class */ (function () {
    function Request2(init) {
        Object.assign(this, init);
    }
    Request2.prototype.createResponse = function () { return new Request2Response(); };
    Request2.prototype.getTypeName = function () { return 'Request2'; };
    return Request2;
}());
exports.Request2 = Request2;
var HelloInnerTypes = /** @class */ (function () {
    function HelloInnerTypes(init) {
        Object.assign(this, init);
    }
    HelloInnerTypes.prototype.createResponse = function () { return new HelloInnerTypesResponse(); };
    HelloInnerTypes.prototype.getTypeName = function () { return 'HelloInnerTypes'; };
    return HelloInnerTypes;
}());
exports.HelloInnerTypes = HelloInnerTypes;
var GetUserSession = /** @class */ (function () {
    function GetUserSession(init) {
        Object.assign(this, init);
    }
    GetUserSession.prototype.createResponse = function () { return new CustomUserSession(); };
    GetUserSession.prototype.getTypeName = function () { return 'GetUserSession'; };
    return GetUserSession;
}());
exports.GetUserSession = GetUserSession;
var QueryTemplate = /** @class */ (function () {
    function QueryTemplate(init) {
        Object.assign(this, init);
    }
    QueryTemplate.prototype.createResponse = function () { return new QueryResponseTemplate(); };
    QueryTemplate.prototype.getTypeName = function () { return 'QueryTemplate'; };
    return QueryTemplate;
}());
exports.QueryTemplate = QueryTemplate;
var HelloReserved = /** @class */ (function () {
    function HelloReserved(init) {
        Object.assign(this, init);
    }
    return HelloReserved;
}());
exports.HelloReserved = HelloReserved;
var HelloDictionary = /** @class */ (function () {
    function HelloDictionary(init) {
        Object.assign(this, init);
    }
    HelloDictionary.prototype.createResponse = function () { return {}; };
    HelloDictionary.prototype.getTypeName = function () { return 'HelloDictionary'; };
    return HelloDictionary;
}());
exports.HelloDictionary = HelloDictionary;
var HelloBuiltin = /** @class */ (function () {
    function HelloBuiltin(init) {
        Object.assign(this, init);
    }
    return HelloBuiltin;
}());
exports.HelloBuiltin = HelloBuiltin;
var HelloGet = /** @class */ (function () {
    function HelloGet(init) {
        Object.assign(this, init);
    }
    HelloGet.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloGet.prototype.getTypeName = function () { return 'HelloGet'; };
    return HelloGet;
}());
exports.HelloGet = HelloGet;
var HelloPost = /** @class */ (function (_super) {
    __extends(HelloPost, _super);
    function HelloPost(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    HelloPost.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloPost.prototype.getTypeName = function () { return 'HelloPost'; };
    return HelloPost;
}(HelloBase));
exports.HelloPost = HelloPost;
var HelloPut = /** @class */ (function () {
    function HelloPut(init) {
        Object.assign(this, init);
    }
    HelloPut.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloPut.prototype.getTypeName = function () { return 'HelloPut'; };
    return HelloPut;
}());
exports.HelloPut = HelloPut;
var HelloDelete = /** @class */ (function () {
    function HelloDelete(init) {
        Object.assign(this, init);
    }
    HelloDelete.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloDelete.prototype.getTypeName = function () { return 'HelloDelete'; };
    return HelloDelete;
}());
exports.HelloDelete = HelloDelete;
var HelloPatch = /** @class */ (function () {
    function HelloPatch(init) {
        Object.assign(this, init);
    }
    HelloPatch.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloPatch.prototype.getTypeName = function () { return 'HelloPatch'; };
    return HelloPatch;
}());
exports.HelloPatch = HelloPatch;
var HelloReturnVoid = /** @class */ (function () {
    function HelloReturnVoid(init) {
        Object.assign(this, init);
    }
    HelloReturnVoid.prototype.createResponse = function () { };
    HelloReturnVoid.prototype.getTypeName = function () { return 'HelloReturnVoid'; };
    return HelloReturnVoid;
}());
exports.HelloReturnVoid = HelloReturnVoid;
var EnumRequest = /** @class */ (function () {
    function EnumRequest(init) {
        Object.assign(this, init);
    }
    EnumRequest.prototype.createResponse = function () { return new EnumResponse(); };
    EnumRequest.prototype.getTypeName = function () { return 'EnumRequest'; };
    return EnumRequest;
}());
exports.EnumRequest = EnumRequest;
var ExcludeTest1 = /** @class */ (function () {
    function ExcludeTest1(init) {
        Object.assign(this, init);
    }
    ExcludeTest1.prototype.createResponse = function () { return new ExcludeTestNested(); };
    ExcludeTest1.prototype.getTypeName = function () { return 'ExcludeTest1'; };
    return ExcludeTest1;
}());
exports.ExcludeTest1 = ExcludeTest1;
var ExcludeTest2 = /** @class */ (function () {
    function ExcludeTest2(init) {
        Object.assign(this, init);
    }
    ExcludeTest2.prototype.createResponse = function () { return ''; };
    ExcludeTest2.prototype.getTypeName = function () { return 'ExcludeTest2'; };
    return ExcludeTest2;
}());
exports.ExcludeTest2 = ExcludeTest2;
var HelloAuthenticated = /** @class */ (function () {
    function HelloAuthenticated(init) {
        Object.assign(this, init);
    }
    HelloAuthenticated.prototype.createResponse = function () { return new HelloAuthenticatedResponse(); };
    HelloAuthenticated.prototype.getTypeName = function () { return 'HelloAuthenticated'; };
    return HelloAuthenticated;
}());
exports.HelloAuthenticated = HelloAuthenticated;
/**
 * Echoes a sentence
 */
// @Route("/echoes", "POST")
// @Api(Description="Echoes a sentence")
var Echoes = /** @class */ (function () {
    function Echoes(init) {
        Object.assign(this, init);
    }
    Echoes.prototype.createResponse = function () { return new Echo(); };
    Echoes.prototype.getTypeName = function () { return 'Echoes'; };
    return Echoes;
}());
exports.Echoes = Echoes;
var CachedEcho = /** @class */ (function () {
    function CachedEcho(init) {
        Object.assign(this, init);
    }
    CachedEcho.prototype.createResponse = function () { return new Echo(); };
    CachedEcho.prototype.getTypeName = function () { return 'CachedEcho'; };
    return CachedEcho;
}());
exports.CachedEcho = CachedEcho;
var AsyncTest = /** @class */ (function () {
    function AsyncTest(init) {
        Object.assign(this, init);
    }
    AsyncTest.prototype.createResponse = function () { return new Echo(); };
    AsyncTest.prototype.getTypeName = function () { return 'AsyncTest'; };
    return AsyncTest;
}());
exports.AsyncTest = AsyncTest;
// @Route("/throwhttperror/{Status}")
var ThrowHttpError = /** @class */ (function () {
    function ThrowHttpError(init) {
        Object.assign(this, init);
    }
    ThrowHttpError.prototype.createResponse = function () { return new ThrowHttpErrorResponse(); };
    ThrowHttpError.prototype.getTypeName = function () { return 'ThrowHttpError'; };
    return ThrowHttpError;
}());
exports.ThrowHttpError = ThrowHttpError;
// @Route("/throw404")
// @Route("/throw404/{Message}")
var Throw404 = /** @class */ (function () {
    function Throw404(init) {
        Object.assign(this, init);
    }
    return Throw404;
}());
exports.Throw404 = Throw404;
// @Route("/return404")
var Return404 = /** @class */ (function () {
    function Return404(init) {
        Object.assign(this, init);
    }
    return Return404;
}());
exports.Return404 = Return404;
// @Route("/return404result")
var Return404Result = /** @class */ (function () {
    function Return404Result(init) {
        Object.assign(this, init);
    }
    return Return404Result;
}());
exports.Return404Result = Return404Result;
// @Route("/throw/{Type}")
var ThrowType = /** @class */ (function () {
    function ThrowType(init) {
        Object.assign(this, init);
    }
    ThrowType.prototype.createResponse = function () { return new ThrowTypeResponse(); };
    ThrowType.prototype.getTypeName = function () { return 'ThrowType'; };
    return ThrowType;
}());
exports.ThrowType = ThrowType;
// @Route("/throwvalidation")
var ThrowValidation = /** @class */ (function () {
    function ThrowValidation(init) {
        Object.assign(this, init);
    }
    ThrowValidation.prototype.createResponse = function () { return new ThrowValidationResponse(); };
    ThrowValidation.prototype.getTypeName = function () { return 'ThrowValidation'; };
    return ThrowValidation;
}());
exports.ThrowValidation = ThrowValidation;
// @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
// @Route("/api/acsprofiles/{profileId}")
var ACSProfile = /** @class */ (function () {
    function ACSProfile(init) {
        Object.assign(this, init);
    }
    ACSProfile.prototype.createResponse = function () { return new acsprofileResponse(); };
    ACSProfile.prototype.getTypeName = function () { return 'ACSProfile'; };
    return ACSProfile;
}());
exports.ACSProfile = ACSProfile;
// @Route("/return/string")
var ReturnString = /** @class */ (function () {
    function ReturnString(init) {
        Object.assign(this, init);
    }
    ReturnString.prototype.createResponse = function () { return ''; };
    ReturnString.prototype.getTypeName = function () { return 'ReturnString'; };
    return ReturnString;
}());
exports.ReturnString = ReturnString;
// @Route("/return/bytes")
var ReturnBytes = /** @class */ (function () {
    function ReturnBytes(init) {
        Object.assign(this, init);
    }
    ReturnBytes.prototype.createResponse = function () { return new Uint8Array(0); };
    ReturnBytes.prototype.getTypeName = function () { return 'ReturnBytes'; };
    return ReturnBytes;
}());
exports.ReturnBytes = ReturnBytes;
// @Route("/return/stream")
var ReturnStream = /** @class */ (function () {
    function ReturnStream(init) {
        Object.assign(this, init);
    }
    ReturnStream.prototype.createResponse = function () { return new Blob(); };
    ReturnStream.prototype.getTypeName = function () { return 'ReturnStream'; };
    return ReturnStream;
}());
exports.ReturnStream = ReturnStream;
// @Route("/Request1/", "GET")
var GetRequest1 = /** @class */ (function () {
    function GetRequest1(init) {
        Object.assign(this, init);
    }
    GetRequest1.prototype.createResponse = function () { return new Array(); };
    GetRequest1.prototype.getTypeName = function () { return 'GetRequest1'; };
    return GetRequest1;
}());
exports.GetRequest1 = GetRequest1;
// @Route("/Request3", "GET")
var GetRequest2 = /** @class */ (function () {
    function GetRequest2(init) {
        Object.assign(this, init);
    }
    GetRequest2.prototype.createResponse = function () { return new ReturnedDto(); };
    GetRequest2.prototype.getTypeName = function () { return 'GetRequest2'; };
    return GetRequest2;
}());
exports.GetRequest2 = GetRequest2;
// @Route("/matchlast/{Id}")
var MatchesLastInt = /** @class */ (function () {
    function MatchesLastInt(init) {
        Object.assign(this, init);
    }
    return MatchesLastInt;
}());
exports.MatchesLastInt = MatchesLastInt;
// @Route("/matchlast/{Slug}")
var MatchesNotLastInt = /** @class */ (function () {
    function MatchesNotLastInt(init) {
        Object.assign(this, init);
    }
    return MatchesNotLastInt;
}());
exports.MatchesNotLastInt = MatchesNotLastInt;
// @Route("/matchregex/{Id}")
var MatchesId = /** @class */ (function () {
    function MatchesId(init) {
        Object.assign(this, init);
    }
    return MatchesId;
}());
exports.MatchesId = MatchesId;
// @Route("/matchregex/{Slug}")
var MatchesSlug = /** @class */ (function () {
    function MatchesSlug(init) {
        Object.assign(this, init);
    }
    return MatchesSlug;
}());
exports.MatchesSlug = MatchesSlug;
// @Route("/{Version}/userdata", "GET")
var SwaggerVersionTest = /** @class */ (function () {
    function SwaggerVersionTest(init) {
        Object.assign(this, init);
    }
    return SwaggerVersionTest;
}());
exports.SwaggerVersionTest = SwaggerVersionTest;
// @Route("/swagger/range")
var SwaggerRangeTest = /** @class */ (function () {
    function SwaggerRangeTest(init) {
        Object.assign(this, init);
    }
    return SwaggerRangeTest;
}());
exports.SwaggerRangeTest = SwaggerRangeTest;
// @Route("/test/errorview")
var TestErrorView = /** @class */ (function () {
    function TestErrorView(init) {
        Object.assign(this, init);
    }
    return TestErrorView;
}());
exports.TestErrorView = TestErrorView;
// @Route("/timestamp", "GET")
var GetTimestamp = /** @class */ (function () {
    function GetTimestamp(init) {
        Object.assign(this, init);
    }
    GetTimestamp.prototype.createResponse = function () { return new TimestampData(); };
    GetTimestamp.prototype.getTypeName = function () { return 'GetTimestamp'; };
    return GetTimestamp;
}());
exports.GetTimestamp = GetTimestamp;
var TestMiniverView = /** @class */ (function () {
    function TestMiniverView(init) {
        Object.assign(this, init);
    }
    return TestMiniverView;
}());
exports.TestMiniverView = TestMiniverView;
// @Route("/testexecproc")
var TestExecProc = /** @class */ (function () {
    function TestExecProc(init) {
        Object.assign(this, init);
    }
    return TestExecProc;
}());
exports.TestExecProc = TestExecProc;
// @Route("/files/{Path*}")
var GetFile = /** @class */ (function () {
    function GetFile(init) {
        Object.assign(this, init);
    }
    return GetFile;
}());
exports.GetFile = GetFile;
// @Route("/test/html2")
var TestHtml2 = /** @class */ (function () {
    function TestHtml2(init) {
        Object.assign(this, init);
    }
    return TestHtml2;
}());
exports.TestHtml2 = TestHtml2;
// @Route("/views/request")
var ViewRequest = /** @class */ (function () {
    function ViewRequest(init) {
        Object.assign(this, init);
    }
    ViewRequest.prototype.createResponse = function () { return new ViewResponse(); };
    ViewRequest.prototype.getTypeName = function () { return 'ViewRequest'; };
    return ViewRequest;
}());
exports.ViewRequest = ViewRequest;
// @Route("/index")
var IndexPage = /** @class */ (function () {
    function IndexPage(init) {
        Object.assign(this, init);
    }
    return IndexPage;
}());
exports.IndexPage = IndexPage;
// @Route("/return/text")
var ReturnText = /** @class */ (function () {
    function ReturnText(init) {
        Object.assign(this, init);
    }
    return ReturnText;
}());
exports.ReturnText = ReturnText;
// @Route("/gzip/{FileName}")
var DownloadGzipFile = /** @class */ (function () {
    function DownloadGzipFile(init) {
        Object.assign(this, init);
    }
    DownloadGzipFile.prototype.createResponse = function () { return new Uint8Array(0); };
    DownloadGzipFile.prototype.getTypeName = function () { return 'DownloadGzipFile'; };
    return DownloadGzipFile;
}());
exports.DownloadGzipFile = DownloadGzipFile;
// @Route("/match/{Language}/{Name*}")
var MatchName = /** @class */ (function () {
    function MatchName(init) {
        Object.assign(this, init);
    }
    MatchName.prototype.createResponse = function () { return new HelloResponse(); };
    MatchName.prototype.getTypeName = function () { return 'MatchName'; };
    return MatchName;
}());
exports.MatchName = MatchName;
// @Route("/match/{Language*}")
var MatchLang = /** @class */ (function () {
    function MatchLang(init) {
        Object.assign(this, init);
    }
    MatchLang.prototype.createResponse = function () { return new HelloResponse(); };
    MatchLang.prototype.getTypeName = function () { return 'MatchLang'; };
    return MatchLang;
}());
exports.MatchLang = MatchLang;
// @Route("/reqlogstest/{Name}")
var RequestLogsTest = /** @class */ (function () {
    function RequestLogsTest(init) {
        Object.assign(this, init);
    }
    RequestLogsTest.prototype.createResponse = function () { return ''; };
    RequestLogsTest.prototype.getTypeName = function () { return 'RequestLogsTest'; };
    return RequestLogsTest;
}());
exports.RequestLogsTest = RequestLogsTest;
var InProcRequest1 = /** @class */ (function () {
    function InProcRequest1(init) {
        Object.assign(this, init);
    }
    return InProcRequest1;
}());
exports.InProcRequest1 = InProcRequest1;
var InProcRequest2 = /** @class */ (function () {
    function InProcRequest2(init) {
        Object.assign(this, init);
    }
    return InProcRequest2;
}());
exports.InProcRequest2 = InProcRequest2;
/**
 * SwaggerTest Service Description
 */
// @Route("/swagger", "GET")
// @Route("/swagger/{Name}", "GET")
// @Route("/swagger/{Name}", "POST")
// @Api(Description="SwaggerTest Service Description")
// @ApiResponse(Description="Your request was not understood", StatusCode=400)
// @ApiResponse(Description="Oops, something broke", StatusCode=500)
// @DataContract
var SwaggerTest = /** @class */ (function () {
    function SwaggerTest(init) {
        Object.assign(this, init);
    }
    return SwaggerTest;
}());
exports.SwaggerTest = SwaggerTest;
// @Route("/swaggertest2", "POST")
var SwaggerTest2 = /** @class */ (function () {
    function SwaggerTest2(init) {
        Object.assign(this, init);
    }
    return SwaggerTest2;
}());
exports.SwaggerTest2 = SwaggerTest2;
// @Route("/swagger-complex", "POST")
var SwaggerComplex = /** @class */ (function () {
    function SwaggerComplex(init) {
        Object.assign(this, init);
    }
    SwaggerComplex.prototype.createResponse = function () { return new SwaggerComplexResponse(); };
    SwaggerComplex.prototype.getTypeName = function () { return 'SwaggerComplex'; };
    return SwaggerComplex;
}());
exports.SwaggerComplex = SwaggerComplex;
// @Route("/swaggerpost/{Required1}", "GET")
// @Route("/swaggerpost/{Required1}/{Optional1}", "GET")
// @Route("/swaggerpost", "POST")
var SwaggerPostTest = /** @class */ (function () {
    function SwaggerPostTest(init) {
        Object.assign(this, init);
    }
    SwaggerPostTest.prototype.createResponse = function () { return new HelloResponse(); };
    SwaggerPostTest.prototype.getTypeName = function () { return 'SwaggerPostTest'; };
    return SwaggerPostTest;
}());
exports.SwaggerPostTest = SwaggerPostTest;
// @Route("/swaggerpost2/{Required1}/{Required2}", "GET")
// @Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", "GET")
// @Route("/swaggerpost2", "POST")
var SwaggerPostTest2 = /** @class */ (function () {
    function SwaggerPostTest2(init) {
        Object.assign(this, init);
    }
    SwaggerPostTest2.prototype.createResponse = function () { return new HelloResponse(); };
    SwaggerPostTest2.prototype.getTypeName = function () { return 'SwaggerPostTest2'; };
    return SwaggerPostTest2;
}());
exports.SwaggerPostTest2 = SwaggerPostTest2;
// @Route("/swagger/multiattrtest", "POST")
// @ApiResponse(Description="Code 1", StatusCode=400)
// @ApiResponse(Description="Code 2", StatusCode=402)
// @ApiResponse(Description="Code 3", StatusCode=401)
var SwaggerMultiApiResponseTest = /** @class */ (function () {
    function SwaggerMultiApiResponseTest(init) {
        Object.assign(this, init);
    }
    SwaggerMultiApiResponseTest.prototype.createResponse = function () { };
    SwaggerMultiApiResponseTest.prototype.getTypeName = function () { return 'SwaggerMultiApiResponseTest'; };
    return SwaggerMultiApiResponseTest;
}());
exports.SwaggerMultiApiResponseTest = SwaggerMultiApiResponseTest;
// @Route("/defaultview/class")
var DefaultViewAttr = /** @class */ (function () {
    function DefaultViewAttr(init) {
        Object.assign(this, init);
    }
    return DefaultViewAttr;
}());
exports.DefaultViewAttr = DefaultViewAttr;
// @Route("/defaultview/action")
var DefaultViewActionAttr = /** @class */ (function () {
    function DefaultViewActionAttr(init) {
        Object.assign(this, init);
    }
    return DefaultViewActionAttr;
}());
exports.DefaultViewActionAttr = DefaultViewActionAttr;
// @Route("/dynamically/registered/{Name}")
var DynamicallyRegistered = /** @class */ (function () {
    function DynamicallyRegistered(init) {
        Object.assign(this, init);
    }
    return DynamicallyRegistered;
}());
exports.DynamicallyRegistered = DynamicallyRegistered;
// @Route("/auth")
// @Route("/auth/{provider}")
// @Route("/authenticate")
// @Route("/authenticate/{provider}")
// @DataContract
var Authenticate = /** @class */ (function () {
    function Authenticate(init) {
        Object.assign(this, init);
    }
    Authenticate.prototype.createResponse = function () { return new AuthenticateResponse(); };
    Authenticate.prototype.getTypeName = function () { return 'Authenticate'; };
    return Authenticate;
}());
exports.Authenticate = Authenticate;
// @Route("/assignroles")
// @DataContract
var AssignRoles = /** @class */ (function () {
    function AssignRoles(init) {
        Object.assign(this, init);
    }
    AssignRoles.prototype.createResponse = function () { return new AssignRolesResponse(); };
    AssignRoles.prototype.getTypeName = function () { return 'AssignRoles'; };
    return AssignRoles;
}());
exports.AssignRoles = AssignRoles;
// @Route("/unassignroles")
// @DataContract
var UnAssignRoles = /** @class */ (function () {
    function UnAssignRoles(init) {
        Object.assign(this, init);
    }
    UnAssignRoles.prototype.createResponse = function () { return new UnAssignRolesResponse(); };
    UnAssignRoles.prototype.getTypeName = function () { return 'UnAssignRoles'; };
    return UnAssignRoles;
}());
exports.UnAssignRoles = UnAssignRoles;
// @Route("/session-to-token")
// @DataContract
var ConvertSessionToToken = /** @class */ (function () {
    function ConvertSessionToToken(init) {
        Object.assign(this, init);
    }
    ConvertSessionToToken.prototype.createResponse = function () { return new ConvertSessionToTokenResponse(); };
    ConvertSessionToToken.prototype.getTypeName = function () { return 'ConvertSessionToToken'; };
    return ConvertSessionToToken;
}());
exports.ConvertSessionToToken = ConvertSessionToToken;
// @Route("/access-token")
// @DataContract
var GetAccessToken = /** @class */ (function () {
    function GetAccessToken(init) {
        Object.assign(this, init);
    }
    GetAccessToken.prototype.createResponse = function () { return new GetAccessTokenResponse(); };
    GetAccessToken.prototype.getTypeName = function () { return 'GetAccessToken'; };
    return GetAccessToken;
}());
exports.GetAccessToken = GetAccessToken;
// @Route("/apikeys")
// @Route("/apikeys/{Environment}")
// @DataContract
var GetApiKeys = /** @class */ (function () {
    function GetApiKeys(init) {
        Object.assign(this, init);
    }
    GetApiKeys.prototype.createResponse = function () { return new GetApiKeysResponse(); };
    GetApiKeys.prototype.getTypeName = function () { return 'GetApiKeys'; };
    return GetApiKeys;
}());
exports.GetApiKeys = GetApiKeys;
// @Route("/apikeys/regenerate")
// @Route("/apikeys/regenerate/{Environment}")
// @DataContract
var RegenerateApiKeys = /** @class */ (function () {
    function RegenerateApiKeys(init) {
        Object.assign(this, init);
    }
    RegenerateApiKeys.prototype.createResponse = function () { return new RegenerateApiKeysResponse(); };
    RegenerateApiKeys.prototype.getTypeName = function () { return 'RegenerateApiKeys'; };
    return RegenerateApiKeys;
}());
exports.RegenerateApiKeys = RegenerateApiKeys;
// @Route("/register")
// @DataContract
var Register = /** @class */ (function () {
    function Register(init) {
        Object.assign(this, init);
    }
    Register.prototype.createResponse = function () { return new RegisterResponse(); };
    Register.prototype.getTypeName = function () { return 'Register'; };
    return Register;
}());
exports.Register = Register;
// @Route("/pgsql/rockstars")
var QueryPostgresRockstars = /** @class */ (function (_super) {
    __extends(QueryPostgresRockstars, _super);
    function QueryPostgresRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryPostgresRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPostgresRockstars.prototype.getTypeName = function () { return 'QueryPostgresRockstars'; };
    return QueryPostgresRockstars;
}(QueryDb_1));
exports.QueryPostgresRockstars = QueryPostgresRockstars;
// @Route("/pgsql/pgrockstars")
var QueryPostgresPgRockstars = /** @class */ (function (_super) {
    __extends(QueryPostgresPgRockstars, _super);
    function QueryPostgresPgRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryPostgresPgRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPostgresPgRockstars.prototype.getTypeName = function () { return 'QueryPostgresPgRockstars'; };
    return QueryPostgresPgRockstars;
}(QueryDb_1));
exports.QueryPostgresPgRockstars = QueryPostgresPgRockstars;
var QueryRockstarsConventions = /** @class */ (function (_super) {
    __extends(QueryRockstarsConventions, _super);
    function QueryRockstarsConventions(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarsConventions.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsConventions.prototype.getTypeName = function () { return 'QueryRockstarsConventions'; };
    return QueryRockstarsConventions;
}(QueryDb_1));
exports.QueryRockstarsConventions = QueryRockstarsConventions;
// @AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")
var QueryCustomRockstars = /** @class */ (function (_super) {
    __extends(QueryCustomRockstars, _super);
    function QueryCustomRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryCustomRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryCustomRockstars.prototype.getTypeName = function () { return 'QueryCustomRockstars'; };
    return QueryCustomRockstars;
}(QueryDb_2));
exports.QueryCustomRockstars = QueryCustomRockstars;
// @Route("/customrockstars")
var QueryRockstarAlbums = /** @class */ (function (_super) {
    __extends(QueryRockstarAlbums, _super);
    function QueryRockstarAlbums(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarAlbums.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarAlbums.prototype.getTypeName = function () { return 'QueryRockstarAlbums'; };
    return QueryRockstarAlbums;
}(QueryDb_2));
exports.QueryRockstarAlbums = QueryRockstarAlbums;
var QueryRockstarAlbumsImplicit = /** @class */ (function (_super) {
    __extends(QueryRockstarAlbumsImplicit, _super);
    function QueryRockstarAlbumsImplicit(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarAlbumsImplicit.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarAlbumsImplicit.prototype.getTypeName = function () { return 'QueryRockstarAlbumsImplicit'; };
    return QueryRockstarAlbumsImplicit;
}(QueryDb_2));
exports.QueryRockstarAlbumsImplicit = QueryRockstarAlbumsImplicit;
var QueryRockstarAlbumsLeftJoin = /** @class */ (function (_super) {
    __extends(QueryRockstarAlbumsLeftJoin, _super);
    function QueryRockstarAlbumsLeftJoin(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarAlbumsLeftJoin.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarAlbumsLeftJoin.prototype.getTypeName = function () { return 'QueryRockstarAlbumsLeftJoin'; };
    return QueryRockstarAlbumsLeftJoin;
}(QueryDb_2));
exports.QueryRockstarAlbumsLeftJoin = QueryRockstarAlbumsLeftJoin;
var QueryOverridedRockstars = /** @class */ (function (_super) {
    __extends(QueryOverridedRockstars, _super);
    function QueryOverridedRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryOverridedRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryOverridedRockstars.prototype.getTypeName = function () { return 'QueryOverridedRockstars'; };
    return QueryOverridedRockstars;
}(QueryDb_1));
exports.QueryOverridedRockstars = QueryOverridedRockstars;
var QueryOverridedCustomRockstars = /** @class */ (function (_super) {
    __extends(QueryOverridedCustomRockstars, _super);
    function QueryOverridedCustomRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryOverridedCustomRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryOverridedCustomRockstars.prototype.getTypeName = function () { return 'QueryOverridedCustomRockstars'; };
    return QueryOverridedCustomRockstars;
}(QueryDb_2));
exports.QueryOverridedCustomRockstars = QueryOverridedCustomRockstars;
// @Route("/query-custom/rockstars")
var QueryFieldRockstars = /** @class */ (function (_super) {
    __extends(QueryFieldRockstars, _super);
    function QueryFieldRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryFieldRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryFieldRockstars.prototype.getTypeName = function () { return 'QueryFieldRockstars'; };
    return QueryFieldRockstars;
}(QueryDb_1));
exports.QueryFieldRockstars = QueryFieldRockstars;
var QueryFieldRockstarsDynamic = /** @class */ (function (_super) {
    __extends(QueryFieldRockstarsDynamic, _super);
    function QueryFieldRockstarsDynamic(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryFieldRockstarsDynamic.prototype.createResponse = function () { return new QueryResponse(); };
    QueryFieldRockstarsDynamic.prototype.getTypeName = function () { return 'QueryFieldRockstarsDynamic'; };
    return QueryFieldRockstarsDynamic;
}(QueryDb_1));
exports.QueryFieldRockstarsDynamic = QueryFieldRockstarsDynamic;
var QueryRockstarsFilter = /** @class */ (function (_super) {
    __extends(QueryRockstarsFilter, _super);
    function QueryRockstarsFilter(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarsFilter.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsFilter.prototype.getTypeName = function () { return 'QueryRockstarsFilter'; };
    return QueryRockstarsFilter;
}(QueryDb_1));
exports.QueryRockstarsFilter = QueryRockstarsFilter;
var QueryCustomRockstarsFilter = /** @class */ (function (_super) {
    __extends(QueryCustomRockstarsFilter, _super);
    function QueryCustomRockstarsFilter(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryCustomRockstarsFilter.prototype.createResponse = function () { return new QueryResponse(); };
    QueryCustomRockstarsFilter.prototype.getTypeName = function () { return 'QueryCustomRockstarsFilter'; };
    return QueryCustomRockstarsFilter;
}(QueryDb_2));
exports.QueryCustomRockstarsFilter = QueryCustomRockstarsFilter;
var QueryRockstarsIFilter = /** @class */ (function (_super) {
    __extends(QueryRockstarsIFilter, _super);
    function QueryRockstarsIFilter(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarsIFilter.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsIFilter.prototype.getTypeName = function () { return 'QueryRockstarsIFilter'; };
    return QueryRockstarsIFilter;
}(QueryDb_1));
exports.QueryRockstarsIFilter = QueryRockstarsIFilter;
// @Route("/OrRockstars")
var QueryOrRockstars = /** @class */ (function (_super) {
    __extends(QueryOrRockstars, _super);
    function QueryOrRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryOrRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryOrRockstars.prototype.getTypeName = function () { return 'QueryOrRockstars'; };
    return QueryOrRockstars;
}(QueryDb_1));
exports.QueryOrRockstars = QueryOrRockstars;
var QueryGetRockstars = /** @class */ (function (_super) {
    __extends(QueryGetRockstars, _super);
    function QueryGetRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryGetRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryGetRockstars.prototype.getTypeName = function () { return 'QueryGetRockstars'; };
    return QueryGetRockstars;
}(QueryDb_1));
exports.QueryGetRockstars = QueryGetRockstars;
var QueryGetRockstarsDynamic = /** @class */ (function (_super) {
    __extends(QueryGetRockstarsDynamic, _super);
    function QueryGetRockstarsDynamic(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryGetRockstarsDynamic.prototype.createResponse = function () { return new QueryResponse(); };
    QueryGetRockstarsDynamic.prototype.getTypeName = function () { return 'QueryGetRockstarsDynamic'; };
    return QueryGetRockstarsDynamic;
}(QueryDb_1));
exports.QueryGetRockstarsDynamic = QueryGetRockstarsDynamic;
// @Route("/movies/search")
var SearchMovies = /** @class */ (function (_super) {
    __extends(SearchMovies, _super);
    function SearchMovies(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    SearchMovies.prototype.createResponse = function () { return new QueryResponse(); };
    SearchMovies.prototype.getTypeName = function () { return 'SearchMovies'; };
    return SearchMovies;
}(QueryDb_1));
exports.SearchMovies = SearchMovies;
// @Route("/movies")
var QueryMovies = /** @class */ (function (_super) {
    __extends(QueryMovies, _super);
    function QueryMovies(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryMovies.prototype.createResponse = function () { return new QueryResponse(); };
    QueryMovies.prototype.getTypeName = function () { return 'QueryMovies'; };
    return QueryMovies;
}(QueryDb_1));
exports.QueryMovies = QueryMovies;
var StreamMovies = /** @class */ (function (_super) {
    __extends(StreamMovies, _super);
    function StreamMovies(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    StreamMovies.prototype.createResponse = function () { return new QueryResponse(); };
    StreamMovies.prototype.getTypeName = function () { return 'StreamMovies'; };
    return StreamMovies;
}(QueryDb_1));
exports.StreamMovies = StreamMovies;
var QueryUnknownRockstars = /** @class */ (function (_super) {
    __extends(QueryUnknownRockstars, _super);
    function QueryUnknownRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryUnknownRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryUnknownRockstars.prototype.getTypeName = function () { return 'QueryUnknownRockstars'; };
    return QueryUnknownRockstars;
}(QueryDb_1));
exports.QueryUnknownRockstars = QueryUnknownRockstars;
// @Route("/query/rockstar-references")
var QueryRockstarsWithReferences = /** @class */ (function (_super) {
    __extends(QueryRockstarsWithReferences, _super);
    function QueryRockstarsWithReferences(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryRockstarsWithReferences.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsWithReferences.prototype.getTypeName = function () { return 'QueryRockstarsWithReferences'; };
    return QueryRockstarsWithReferences;
}(QueryDb_1));
exports.QueryRockstarsWithReferences = QueryRockstarsWithReferences;
var QueryPocoBase = /** @class */ (function (_super) {
    __extends(QueryPocoBase, _super);
    function QueryPocoBase(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryPocoBase.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPocoBase.prototype.getTypeName = function () { return 'QueryPocoBase'; };
    return QueryPocoBase;
}(QueryDb_1));
exports.QueryPocoBase = QueryPocoBase;
var QueryPocoIntoBase = /** @class */ (function (_super) {
    __extends(QueryPocoIntoBase, _super);
    function QueryPocoIntoBase(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryPocoIntoBase.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPocoIntoBase.prototype.getTypeName = function () { return 'QueryPocoIntoBase'; };
    return QueryPocoIntoBase;
}(QueryDb_2));
exports.QueryPocoIntoBase = QueryPocoIntoBase;
// @Route("/query/alltypes")
var QueryAllTypes = /** @class */ (function (_super) {
    __extends(QueryAllTypes, _super);
    function QueryAllTypes(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryAllTypes.prototype.createResponse = function () { return new QueryResponse(); };
    QueryAllTypes.prototype.getTypeName = function () { return 'QueryAllTypes'; };
    return QueryAllTypes;
}(QueryDb_1));
exports.QueryAllTypes = QueryAllTypes;
// @Route("/querydata/rockstars")
var QueryDataRockstars = /** @class */ (function (_super) {
    __extends(QueryDataRockstars, _super);
    function QueryDataRockstars(init) {
        var _this = _super.call(this, init) || this;
        Object.assign(_this, init);
        return _this;
    }
    QueryDataRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryDataRockstars.prototype.getTypeName = function () { return 'QueryDataRockstars'; };
    return QueryDataRockstars;
}(QueryData));
exports.QueryDataRockstars = QueryDataRockstars;
