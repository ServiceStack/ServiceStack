"use strict";
/* Options:
Date: 2017-11-22 18:17:46
Version: 5.00
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:55799

//GlobalNamespace:
//MakePropertiesOptional: True
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
exports.__esModule = true;
var QueryBase = /** @class */ (function () {
    function QueryBase() {
    }
    return QueryBase;
}());
exports.QueryBase = QueryBase;
var QueryData = /** @class */ (function (_super) {
    __extends(QueryData, _super);
    function QueryData() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return QueryData;
}(QueryBase));
exports.QueryData = QueryData;
var RequestLogEntry = /** @class */ (function () {
    function RequestLogEntry() {
    }
    return RequestLogEntry;
}());
exports.RequestLogEntry = RequestLogEntry;
// @DataContract
var ResponseError = /** @class */ (function () {
    function ResponseError() {
    }
    return ResponseError;
}());
exports.ResponseError = ResponseError;
// @DataContract
var ResponseStatus = /** @class */ (function () {
    function ResponseStatus() {
    }
    return ResponseStatus;
}());
exports.ResponseStatus = ResponseStatus;
var QueryDb_1 = /** @class */ (function (_super) {
    __extends(QueryDb_1, _super);
    function QueryDb_1() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return QueryDb_1;
}(QueryBase));
exports.QueryDb_1 = QueryDb_1;
var Rockstar = /** @class */ (function () {
    function Rockstar() {
    }
    return Rockstar;
}());
exports.Rockstar = Rockstar;
var ObjectDesign = /** @class */ (function () {
    function ObjectDesign() {
    }
    return ObjectDesign;
}());
exports.ObjectDesign = ObjectDesign;
var MetadataTestNestedChild = /** @class */ (function () {
    function MetadataTestNestedChild() {
    }
    return MetadataTestNestedChild;
}());
exports.MetadataTestNestedChild = MetadataTestNestedChild;
var MetadataTestChild = /** @class */ (function () {
    function MetadataTestChild() {
    }
    return MetadataTestChild;
}());
exports.MetadataTestChild = MetadataTestChild;
var MenuItemExampleItem = /** @class */ (function () {
    function MenuItemExampleItem() {
    }
    return MenuItemExampleItem;
}());
exports.MenuItemExampleItem = MenuItemExampleItem;
var MenuItemExample = /** @class */ (function () {
    function MenuItemExample() {
    }
    return MenuItemExample;
}());
exports.MenuItemExample = MenuItemExample;
// @DataContract
var MenuExample = /** @class */ (function () {
    function MenuExample() {
    }
    return MenuExample;
}());
exports.MenuExample = MenuExample;
var MetadataTypeName = /** @class */ (function () {
    function MetadataTypeName() {
    }
    return MetadataTypeName;
}());
exports.MetadataTypeName = MetadataTypeName;
var MetadataRoute = /** @class */ (function () {
    function MetadataRoute() {
    }
    return MetadataRoute;
}());
exports.MetadataRoute = MetadataRoute;
var MetadataDataContract = /** @class */ (function () {
    function MetadataDataContract() {
    }
    return MetadataDataContract;
}());
exports.MetadataDataContract = MetadataDataContract;
var MetadataDataMember = /** @class */ (function () {
    function MetadataDataMember() {
    }
    return MetadataDataMember;
}());
exports.MetadataDataMember = MetadataDataMember;
var MetadataAttribute = /** @class */ (function () {
    function MetadataAttribute() {
    }
    return MetadataAttribute;
}());
exports.MetadataAttribute = MetadataAttribute;
var MetadataPropertyType = /** @class */ (function () {
    function MetadataPropertyType() {
    }
    return MetadataPropertyType;
}());
exports.MetadataPropertyType = MetadataPropertyType;
var MetadataType = /** @class */ (function () {
    function MetadataType() {
    }
    return MetadataType;
}());
exports.MetadataType = MetadataType;
var AutoQueryConvention = /** @class */ (function () {
    function AutoQueryConvention() {
    }
    return AutoQueryConvention;
}());
exports.AutoQueryConvention = AutoQueryConvention;
var AutoQueryViewerConfig = /** @class */ (function () {
    function AutoQueryViewerConfig() {
    }
    return AutoQueryViewerConfig;
}());
exports.AutoQueryViewerConfig = AutoQueryViewerConfig;
var AutoQueryViewerUserInfo = /** @class */ (function () {
    function AutoQueryViewerUserInfo() {
    }
    return AutoQueryViewerUserInfo;
}());
exports.AutoQueryViewerUserInfo = AutoQueryViewerUserInfo;
var AutoQueryOperation = /** @class */ (function () {
    function AutoQueryOperation() {
    }
    return AutoQueryOperation;
}());
exports.AutoQueryOperation = AutoQueryOperation;
var NativeTypesTestService = /** @class */ (function () {
    function NativeTypesTestService() {
    }
    return NativeTypesTestService;
}());
exports.NativeTypesTestService = NativeTypesTestService;
var NestedClass = /** @class */ (function () {
    function NestedClass() {
    }
    return NestedClass;
}());
exports.NestedClass = NestedClass;
var ListResult = /** @class */ (function () {
    function ListResult() {
    }
    return ListResult;
}());
exports.ListResult = ListResult;
var OnlyInReturnListArg = /** @class */ (function () {
    function OnlyInReturnListArg() {
    }
    return OnlyInReturnListArg;
}());
exports.OnlyInReturnListArg = OnlyInReturnListArg;
var ArrayResult = /** @class */ (function () {
    function ArrayResult() {
    }
    return ArrayResult;
}());
exports.ArrayResult = ArrayResult;
// @Flags()
var EnumFlags;
(function (EnumFlags) {
    EnumFlags[EnumFlags["Value1"] = 1] = "Value1";
    EnumFlags[EnumFlags["Value2"] = 2] = "Value2";
    EnumFlags[EnumFlags["Value3"] = 4] = "Value3";
})(EnumFlags = exports.EnumFlags || (exports.EnumFlags = {}));
var Poco = /** @class */ (function () {
    function Poco() {
    }
    return Poco;
}());
exports.Poco = Poco;
var AllCollectionTypes = /** @class */ (function () {
    function AllCollectionTypes() {
    }
    return AllCollectionTypes;
}());
exports.AllCollectionTypes = AllCollectionTypes;
var KeyValuePair = /** @class */ (function () {
    function KeyValuePair() {
    }
    return KeyValuePair;
}());
exports.KeyValuePair = KeyValuePair;
var SubType = /** @class */ (function () {
    function SubType() {
    }
    return SubType;
}());
exports.SubType = SubType;
var HelloBase = /** @class */ (function () {
    function HelloBase() {
    }
    return HelloBase;
}());
exports.HelloBase = HelloBase;
var HelloResponseBase = /** @class */ (function () {
    function HelloResponseBase() {
    }
    return HelloResponseBase;
}());
exports.HelloResponseBase = HelloResponseBase;
var HelloBase_1 = /** @class */ (function () {
    function HelloBase_1() {
    }
    return HelloBase_1;
}());
exports.HelloBase_1 = HelloBase_1;
var Item = /** @class */ (function () {
    function Item() {
    }
    return Item;
}());
exports.Item = Item;
var InheritedItem = /** @class */ (function () {
    function InheritedItem() {
    }
    return InheritedItem;
}());
exports.InheritedItem = InheritedItem;
var HelloWithReturnResponse = /** @class */ (function () {
    function HelloWithReturnResponse() {
    }
    return HelloWithReturnResponse;
}());
exports.HelloWithReturnResponse = HelloWithReturnResponse;
var HelloType = /** @class */ (function () {
    function HelloType() {
    }
    return HelloType;
}());
exports.HelloType = HelloType;
// @DataContract
var AuthUserSession = /** @class */ (function () {
    function AuthUserSession() {
    }
    return AuthUserSession;
}());
exports.AuthUserSession = AuthUserSession;
var EmptyClass = /** @class */ (function () {
    function EmptyClass() {
    }
    return EmptyClass;
}());
exports.EmptyClass = EmptyClass;
var TypeB = /** @class */ (function () {
    function TypeB() {
    }
    return TypeB;
}());
exports.TypeB = TypeB;
var TypeA = /** @class */ (function () {
    function TypeA() {
    }
    return TypeA;
}());
exports.TypeA = TypeA;
var InnerType = /** @class */ (function () {
    function InnerType() {
    }
    return InnerType;
}());
exports.InnerType = InnerType;
var InnerTypeItem = /** @class */ (function () {
    function InnerTypeItem() {
    }
    return InnerTypeItem;
}());
exports.InnerTypeItem = InnerTypeItem;
var Tuple_2 = /** @class */ (function () {
    function Tuple_2() {
    }
    return Tuple_2;
}());
exports.Tuple_2 = Tuple_2;
var Tuple_3 = /** @class */ (function () {
    function Tuple_3() {
    }
    return Tuple_3;
}());
exports.Tuple_3 = Tuple_3;
var SwaggerNestedModel = /** @class */ (function () {
    function SwaggerNestedModel() {
    }
    return SwaggerNestedModel;
}());
exports.SwaggerNestedModel = SwaggerNestedModel;
var SwaggerNestedModel2 = /** @class */ (function () {
    function SwaggerNestedModel2() {
    }
    return SwaggerNestedModel2;
}());
exports.SwaggerNestedModel2 = SwaggerNestedModel2;
// @DataContract
var UserApiKey = /** @class */ (function () {
    function UserApiKey() {
    }
    return UserApiKey;
}());
exports.UserApiKey = UserApiKey;
var PgRockstar = /** @class */ (function (_super) {
    __extends(PgRockstar, _super);
    function PgRockstar() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return PgRockstar;
}(Rockstar));
exports.PgRockstar = PgRockstar;
var QueryDb_2 = /** @class */ (function (_super) {
    __extends(QueryDb_2, _super);
    function QueryDb_2() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return QueryDb_2;
}(QueryBase));
exports.QueryDb_2 = QueryDb_2;
var CustomRockstar = /** @class */ (function () {
    function CustomRockstar() {
    }
    return CustomRockstar;
}());
exports.CustomRockstar = CustomRockstar;
var Movie = /** @class */ (function () {
    function Movie() {
    }
    return Movie;
}());
exports.Movie = Movie;
var RockstarAlbum = /** @class */ (function () {
    function RockstarAlbum() {
    }
    return RockstarAlbum;
}());
exports.RockstarAlbum = RockstarAlbum;
var RockstarReference = /** @class */ (function () {
    function RockstarReference() {
    }
    return RockstarReference;
}());
exports.RockstarReference = RockstarReference;
var OnlyDefinedInGenericType = /** @class */ (function () {
    function OnlyDefinedInGenericType() {
    }
    return OnlyDefinedInGenericType;
}());
exports.OnlyDefinedInGenericType = OnlyDefinedInGenericType;
var OnlyDefinedInGenericTypeFrom = /** @class */ (function () {
    function OnlyDefinedInGenericTypeFrom() {
    }
    return OnlyDefinedInGenericTypeFrom;
}());
exports.OnlyDefinedInGenericTypeFrom = OnlyDefinedInGenericTypeFrom;
var OnlyDefinedInGenericTypeInto = /** @class */ (function () {
    function OnlyDefinedInGenericTypeInto() {
    }
    return OnlyDefinedInGenericTypeInto;
}());
exports.OnlyDefinedInGenericTypeInto = OnlyDefinedInGenericTypeInto;
var TypesGroup = /** @class */ (function () {
    function TypesGroup() {
    }
    return TypesGroup;
}());
exports.TypesGroup = TypesGroup;
// @DataContract
var QueryResponse = /** @class */ (function () {
    function QueryResponse() {
    }
    return QueryResponse;
}());
exports.QueryResponse = QueryResponse;
// @DataContract
var UpdateEventSubscriberResponse = /** @class */ (function () {
    function UpdateEventSubscriberResponse() {
    }
    return UpdateEventSubscriberResponse;
}());
exports.UpdateEventSubscriberResponse = UpdateEventSubscriberResponse;
var ChangeRequestResponse = /** @class */ (function () {
    function ChangeRequestResponse() {
    }
    return ChangeRequestResponse;
}());
exports.ChangeRequestResponse = ChangeRequestResponse;
var CustomHttpErrorResponse = /** @class */ (function () {
    function CustomHttpErrorResponse() {
    }
    return CustomHttpErrorResponse;
}());
exports.CustomHttpErrorResponse = CustomHttpErrorResponse;
// @Route("/alwaysthrows")
var AlwaysThrows = /** @class */ (function () {
    function AlwaysThrows() {
    }
    AlwaysThrows.prototype.createResponse = function () { return new AlwaysThrows(); };
    AlwaysThrows.prototype.getTypeName = function () { return "AlwaysThrows"; };
    return AlwaysThrows;
}());
exports.AlwaysThrows = AlwaysThrows;
// @Route("/alwaysthrowsfilterattribute")
var AlwaysThrowsFilterAttribute = /** @class */ (function () {
    function AlwaysThrowsFilterAttribute() {
    }
    AlwaysThrowsFilterAttribute.prototype.createResponse = function () { return new AlwaysThrowsFilterAttribute(); };
    AlwaysThrowsFilterAttribute.prototype.getTypeName = function () { return "AlwaysThrowsFilterAttribute"; };
    return AlwaysThrowsFilterAttribute;
}());
exports.AlwaysThrowsFilterAttribute = AlwaysThrowsFilterAttribute;
// @Route("/alwaysthrowsglobalfilter")
var AlwaysThrowsGlobalFilter = /** @class */ (function () {
    function AlwaysThrowsGlobalFilter() {
    }
    AlwaysThrowsGlobalFilter.prototype.createResponse = function () { return new AlwaysThrowsGlobalFilter(); };
    AlwaysThrowsGlobalFilter.prototype.getTypeName = function () { return "AlwaysThrowsGlobalFilter"; };
    return AlwaysThrowsGlobalFilter;
}());
exports.AlwaysThrowsGlobalFilter = AlwaysThrowsGlobalFilter;
var CustomFieldHttpErrorResponse = /** @class */ (function () {
    function CustomFieldHttpErrorResponse() {
    }
    return CustomFieldHttpErrorResponse;
}());
exports.CustomFieldHttpErrorResponse = CustomFieldHttpErrorResponse;
var NoRepeatResponse = /** @class */ (function () {
    function NoRepeatResponse() {
    }
    return NoRepeatResponse;
}());
exports.NoRepeatResponse = NoRepeatResponse;
var BatchThrowsResponse = /** @class */ (function () {
    function BatchThrowsResponse() {
    }
    return BatchThrowsResponse;
}());
exports.BatchThrowsResponse = BatchThrowsResponse;
var ObjectDesignResponse = /** @class */ (function () {
    function ObjectDesignResponse() {
    }
    return ObjectDesignResponse;
}());
exports.ObjectDesignResponse = ObjectDesignResponse;
var MetadataTestResponse = /** @class */ (function () {
    function MetadataTestResponse() {
    }
    return MetadataTestResponse;
}());
exports.MetadataTestResponse = MetadataTestResponse;
// @DataContract
var GetExampleResponse = /** @class */ (function () {
    function GetExampleResponse() {
    }
    return GetExampleResponse;
}());
exports.GetExampleResponse = GetExampleResponse;
var AutoQueryMetadataResponse = /** @class */ (function () {
    function AutoQueryMetadataResponse() {
    }
    return AutoQueryMetadataResponse;
}());
exports.AutoQueryMetadataResponse = AutoQueryMetadataResponse;
// @DataContract
var HelloACodeGenTestResponse = /** @class */ (function () {
    function HelloACodeGenTestResponse() {
    }
    return HelloACodeGenTestResponse;
}());
exports.HelloACodeGenTestResponse = HelloACodeGenTestResponse;
var HelloResponse = /** @class */ (function () {
    function HelloResponse() {
    }
    return HelloResponse;
}());
exports.HelloResponse = HelloResponse;
/**
* Description on HelloAllResponse type
*/
// @DataContract
var HelloAnnotatedResponse = /** @class */ (function () {
    function HelloAnnotatedResponse() {
    }
    return HelloAnnotatedResponse;
}());
exports.HelloAnnotatedResponse = HelloAnnotatedResponse;
var HelloList = /** @class */ (function () {
    function HelloList() {
    }
    HelloList.prototype.createResponse = function () { return new Array(); };
    HelloList.prototype.getTypeName = function () { return "HelloList"; };
    return HelloList;
}());
exports.HelloList = HelloList;
var HelloArray = /** @class */ (function () {
    function HelloArray() {
    }
    HelloArray.prototype.createResponse = function () { return new Array(); };
    HelloArray.prototype.getTypeName = function () { return "HelloArray"; };
    return HelloArray;
}());
exports.HelloArray = HelloArray;
var HelloExistingResponse = /** @class */ (function () {
    function HelloExistingResponse() {
    }
    return HelloExistingResponse;
}());
exports.HelloExistingResponse = HelloExistingResponse;
var AllTypes = /** @class */ (function () {
    function AllTypes() {
    }
    AllTypes.prototype.createResponse = function () { return new AllTypes(); };
    AllTypes.prototype.getTypeName = function () { return "AllTypes"; };
    return AllTypes;
}());
exports.AllTypes = AllTypes;
var HelloAllTypesResponse = /** @class */ (function () {
    function HelloAllTypesResponse() {
    }
    return HelloAllTypesResponse;
}());
exports.HelloAllTypesResponse = HelloAllTypesResponse;
// @DataContract
var HelloWithDataContractResponse = /** @class */ (function () {
    function HelloWithDataContractResponse() {
    }
    return HelloWithDataContractResponse;
}());
exports.HelloWithDataContractResponse = HelloWithDataContractResponse;
/**
* Description on HelloWithDescriptionResponse type
*/
var HelloWithDescriptionResponse = /** @class */ (function () {
    function HelloWithDescriptionResponse() {
    }
    return HelloWithDescriptionResponse;
}());
exports.HelloWithDescriptionResponse = HelloWithDescriptionResponse;
var HelloWithInheritanceResponse = /** @class */ (function (_super) {
    __extends(HelloWithInheritanceResponse, _super);
    function HelloWithInheritanceResponse() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return HelloWithInheritanceResponse;
}(HelloResponseBase));
exports.HelloWithInheritanceResponse = HelloWithInheritanceResponse;
var HelloWithAlternateReturnResponse = /** @class */ (function (_super) {
    __extends(HelloWithAlternateReturnResponse, _super);
    function HelloWithAlternateReturnResponse() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return HelloWithAlternateReturnResponse;
}(HelloWithReturnResponse));
exports.HelloWithAlternateReturnResponse = HelloWithAlternateReturnResponse;
var HelloWithRouteResponse = /** @class */ (function () {
    function HelloWithRouteResponse() {
    }
    return HelloWithRouteResponse;
}());
exports.HelloWithRouteResponse = HelloWithRouteResponse;
var HelloWithTypeResponse = /** @class */ (function () {
    function HelloWithTypeResponse() {
    }
    return HelloWithTypeResponse;
}());
exports.HelloWithTypeResponse = HelloWithTypeResponse;
var HelloStruct = /** @class */ (function () {
    function HelloStruct() {
    }
    HelloStruct.prototype.createResponse = function () { return new HelloStruct(); };
    HelloStruct.prototype.getTypeName = function () { return "HelloStruct"; };
    return HelloStruct;
}());
exports.HelloStruct = HelloStruct;
var HelloSessionResponse = /** @class */ (function () {
    function HelloSessionResponse() {
    }
    return HelloSessionResponse;
}());
exports.HelloSessionResponse = HelloSessionResponse;
var HelloImplementsInterface = /** @class */ (function () {
    function HelloImplementsInterface() {
    }
    HelloImplementsInterface.prototype.createResponse = function () { return new HelloImplementsInterface(); };
    HelloImplementsInterface.prototype.getTypeName = function () { return "HelloImplementsInterface"; };
    return HelloImplementsInterface;
}());
exports.HelloImplementsInterface = HelloImplementsInterface;
var Request1Response = /** @class */ (function () {
    function Request1Response() {
    }
    return Request1Response;
}());
exports.Request1Response = Request1Response;
var Request2Response = /** @class */ (function () {
    function Request2Response() {
    }
    return Request2Response;
}());
exports.Request2Response = Request2Response;
var HelloInnerTypesResponse = /** @class */ (function () {
    function HelloInnerTypesResponse() {
    }
    return HelloInnerTypesResponse;
}());
exports.HelloInnerTypesResponse = HelloInnerTypesResponse;
var CustomUserSession = /** @class */ (function (_super) {
    __extends(CustomUserSession, _super);
    function CustomUserSession() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return CustomUserSession;
}(AuthUserSession));
exports.CustomUserSession = CustomUserSession;
// @DataContract
var QueryResponseTemplate = /** @class */ (function () {
    function QueryResponseTemplate() {
    }
    return QueryResponseTemplate;
}());
exports.QueryResponseTemplate = QueryResponseTemplate;
var HelloVerbResponse = /** @class */ (function () {
    function HelloVerbResponse() {
    }
    return HelloVerbResponse;
}());
exports.HelloVerbResponse = HelloVerbResponse;
var EnumResponse = /** @class */ (function () {
    function EnumResponse() {
    }
    return EnumResponse;
}());
exports.EnumResponse = EnumResponse;
var ExcludeTestNested = /** @class */ (function () {
    function ExcludeTestNested() {
    }
    return ExcludeTestNested;
}());
exports.ExcludeTestNested = ExcludeTestNested;
var RestrictLocalhost = /** @class */ (function () {
    function RestrictLocalhost() {
    }
    RestrictLocalhost.prototype.createResponse = function () { return new RestrictLocalhost(); };
    RestrictLocalhost.prototype.getTypeName = function () { return "RestrictLocalhost"; };
    return RestrictLocalhost;
}());
exports.RestrictLocalhost = RestrictLocalhost;
var RestrictInternal = /** @class */ (function () {
    function RestrictInternal() {
    }
    RestrictInternal.prototype.createResponse = function () { return new RestrictInternal(); };
    RestrictInternal.prototype.getTypeName = function () { return "RestrictInternal"; };
    return RestrictInternal;
}());
exports.RestrictInternal = RestrictInternal;
var HelloTuple = /** @class */ (function () {
    function HelloTuple() {
    }
    HelloTuple.prototype.createResponse = function () { return new HelloTuple(); };
    HelloTuple.prototype.getTypeName = function () { return "HelloTuple"; };
    return HelloTuple;
}());
exports.HelloTuple = HelloTuple;
var HelloAuthenticatedResponse = /** @class */ (function () {
    function HelloAuthenticatedResponse() {
    }
    return HelloAuthenticatedResponse;
}());
exports.HelloAuthenticatedResponse = HelloAuthenticatedResponse;
var Echo = /** @class */ (function () {
    function Echo() {
    }
    return Echo;
}());
exports.Echo = Echo;
var ThrowHttpErrorResponse = /** @class */ (function () {
    function ThrowHttpErrorResponse() {
    }
    return ThrowHttpErrorResponse;
}());
exports.ThrowHttpErrorResponse = ThrowHttpErrorResponse;
var ThrowTypeResponse = /** @class */ (function () {
    function ThrowTypeResponse() {
    }
    return ThrowTypeResponse;
}());
exports.ThrowTypeResponse = ThrowTypeResponse;
var ThrowValidationResponse = /** @class */ (function () {
    function ThrowValidationResponse() {
    }
    return ThrowValidationResponse;
}());
exports.ThrowValidationResponse = ThrowValidationResponse;
var acsprofileResponse = /** @class */ (function () {
    function acsprofileResponse() {
    }
    return acsprofileResponse;
}());
exports.acsprofileResponse = acsprofileResponse;
var ReturnedDto = /** @class */ (function () {
    function ReturnedDto() {
    }
    return ReturnedDto;
}());
exports.ReturnedDto = ReturnedDto;
// @Route("/matchroute/html")
var MatchesHtml = /** @class */ (function () {
    function MatchesHtml() {
    }
    MatchesHtml.prototype.createResponse = function () { return new MatchesHtml(); };
    MatchesHtml.prototype.getTypeName = function () { return "MatchesHtml"; };
    return MatchesHtml;
}());
exports.MatchesHtml = MatchesHtml;
// @Route("/matchroute/json")
var MatchesJson = /** @class */ (function () {
    function MatchesJson() {
    }
    MatchesJson.prototype.createResponse = function () { return new MatchesJson(); };
    MatchesJson.prototype.getTypeName = function () { return "MatchesJson"; };
    return MatchesJson;
}());
exports.MatchesJson = MatchesJson;
var TimestampData = /** @class */ (function () {
    function TimestampData() {
    }
    return TimestampData;
}());
exports.TimestampData = TimestampData;
// @Route("/test/html")
var TestHtml = /** @class */ (function () {
    function TestHtml() {
    }
    TestHtml.prototype.createResponse = function () { return new TestHtml(); };
    TestHtml.prototype.getTypeName = function () { return "TestHtml"; };
    return TestHtml;
}());
exports.TestHtml = TestHtml;
var SwaggerComplexResponse = /** @class */ (function () {
    function SwaggerComplexResponse() {
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
    function GetSwaggerExamples() {
    }
    GetSwaggerExamples.prototype.createResponse = function () { return new GetSwaggerExamples(); };
    GetSwaggerExamples.prototype.getTypeName = function () { return "GetSwaggerExamples"; };
    return GetSwaggerExamples;
}());
exports.GetSwaggerExamples = GetSwaggerExamples;
/**
* Api GET Id
*/
// @Route("/swaggerexamples/{Id}", "GET")
// @Api(Description="Api GET Id")
var GetSwaggerExample = /** @class */ (function () {
    function GetSwaggerExample() {
    }
    GetSwaggerExample.prototype.createResponse = function () { return new GetSwaggerExample(); };
    GetSwaggerExample.prototype.getTypeName = function () { return "GetSwaggerExample"; };
    return GetSwaggerExample;
}());
exports.GetSwaggerExample = GetSwaggerExample;
/**
* Api POST
*/
// @Route("/swaggerexamples", "POST")
// @Api(Description="Api POST")
var PostSwaggerExamples = /** @class */ (function () {
    function PostSwaggerExamples() {
    }
    PostSwaggerExamples.prototype.createResponse = function () { return new PostSwaggerExamples(); };
    PostSwaggerExamples.prototype.getTypeName = function () { return "PostSwaggerExamples"; };
    return PostSwaggerExamples;
}());
exports.PostSwaggerExamples = PostSwaggerExamples;
/**
* Api PUT Id
*/
// @Route("/swaggerexamples/{Id}", "PUT")
// @Api(Description="Api PUT Id")
var PutSwaggerExample = /** @class */ (function () {
    function PutSwaggerExample() {
    }
    PutSwaggerExample.prototype.createResponse = function () { return new PutSwaggerExample(); };
    PutSwaggerExample.prototype.getTypeName = function () { return "PutSwaggerExample"; };
    return PutSwaggerExample;
}());
exports.PutSwaggerExample = PutSwaggerExample;
// @Route("/lists", "GET")
var GetLists = /** @class */ (function () {
    function GetLists() {
    }
    GetLists.prototype.createResponse = function () { return new GetLists(); };
    GetLists.prototype.getTypeName = function () { return "GetLists"; };
    return GetLists;
}());
exports.GetLists = GetLists;
// @DataContract
var AuthenticateResponse = /** @class */ (function () {
    function AuthenticateResponse() {
    }
    return AuthenticateResponse;
}());
exports.AuthenticateResponse = AuthenticateResponse;
// @DataContract
var AssignRolesResponse = /** @class */ (function () {
    function AssignRolesResponse() {
    }
    return AssignRolesResponse;
}());
exports.AssignRolesResponse = AssignRolesResponse;
// @DataContract
var UnAssignRolesResponse = /** @class */ (function () {
    function UnAssignRolesResponse() {
    }
    return UnAssignRolesResponse;
}());
exports.UnAssignRolesResponse = UnAssignRolesResponse;
// @DataContract
var GetApiKeysResponse = /** @class */ (function () {
    function GetApiKeysResponse() {
    }
    return GetApiKeysResponse;
}());
exports.GetApiKeysResponse = GetApiKeysResponse;
// @DataContract
var RegisterResponse = /** @class */ (function () {
    function RegisterResponse() {
    }
    return RegisterResponse;
}());
exports.RegisterResponse = RegisterResponse;
// @Route("/anontype")
var AnonType = /** @class */ (function () {
    function AnonType() {
    }
    return AnonType;
}());
exports.AnonType = AnonType;
// @Route("/query/requestlogs")
// @Route("/query/requestlogs/{Date}")
var QueryRequestLogs = /** @class */ (function (_super) {
    __extends(QueryRequestLogs, _super);
    function QueryRequestLogs() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRequestLogs.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRequestLogs.prototype.getTypeName = function () { return "QueryRequestLogs"; };
    return QueryRequestLogs;
}(QueryData));
exports.QueryRequestLogs = QueryRequestLogs;
var TodayLogs = /** @class */ (function (_super) {
    __extends(TodayLogs, _super);
    function TodayLogs() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    TodayLogs.prototype.createResponse = function () { return new QueryResponse(); };
    TodayLogs.prototype.getTypeName = function () { return "TodayLogs"; };
    return TodayLogs;
}(QueryData));
exports.TodayLogs = TodayLogs;
var TodayErrorLogs = /** @class */ (function (_super) {
    __extends(TodayErrorLogs, _super);
    function TodayErrorLogs() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    TodayErrorLogs.prototype.createResponse = function () { return new QueryResponse(); };
    TodayErrorLogs.prototype.getTypeName = function () { return "TodayErrorLogs"; };
    return TodayErrorLogs;
}(QueryData));
exports.TodayErrorLogs = TodayErrorLogs;
var YesterdayLogs = /** @class */ (function (_super) {
    __extends(YesterdayLogs, _super);
    function YesterdayLogs() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    YesterdayLogs.prototype.createResponse = function () { return new QueryResponse(); };
    YesterdayLogs.prototype.getTypeName = function () { return "YesterdayLogs"; };
    return YesterdayLogs;
}(QueryData));
exports.YesterdayLogs = YesterdayLogs;
var YesterdayErrorLogs = /** @class */ (function (_super) {
    __extends(YesterdayErrorLogs, _super);
    function YesterdayErrorLogs() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    YesterdayErrorLogs.prototype.createResponse = function () { return new QueryResponse(); };
    YesterdayErrorLogs.prototype.getTypeName = function () { return "YesterdayErrorLogs"; };
    return YesterdayErrorLogs;
}(QueryData));
exports.YesterdayErrorLogs = YesterdayErrorLogs;
// @Route("/query/rockstars")
var QueryRockstars = /** @class */ (function (_super) {
    __extends(QueryRockstars, _super);
    function QueryRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstars.prototype.getTypeName = function () { return "QueryRockstars"; };
    return QueryRockstars;
}(QueryDb_1));
exports.QueryRockstars = QueryRockstars;
var GetEventSubscribers = /** @class */ (function () {
    function GetEventSubscribers() {
    }
    GetEventSubscribers.prototype.createResponse = function () { return new Object(); };
    GetEventSubscribers.prototype.getTypeName = function () { return "GetEventSubscribers"; };
    return GetEventSubscribers;
}());
exports.GetEventSubscribers = GetEventSubscribers;
// @Route("/event-subscribers/{Id}", "POST")
// @DataContract
var UpdateEventSubscriber = /** @class */ (function () {
    function UpdateEventSubscriber() {
    }
    UpdateEventSubscriber.prototype.createResponse = function () { return new UpdateEventSubscriberResponse(); };
    UpdateEventSubscriber.prototype.getTypeName = function () { return "UpdateEventSubscriber"; };
    return UpdateEventSubscriber;
}());
exports.UpdateEventSubscriber = UpdateEventSubscriber;
// @Route("/changerequest/{Id}")
var ChangeRequest = /** @class */ (function () {
    function ChangeRequest() {
    }
    ChangeRequest.prototype.createResponse = function () { return new ChangeRequestResponse(); };
    ChangeRequest.prototype.getTypeName = function () { return "ChangeRequest"; };
    return ChangeRequest;
}());
exports.ChangeRequest = ChangeRequest;
// @Route("/compress/{Path*}")
var CompressFile = /** @class */ (function () {
    function CompressFile() {
    }
    return CompressFile;
}());
exports.CompressFile = CompressFile;
// @Route("/Routing/LeadPost.aspx")
var LegacyLeadPost = /** @class */ (function () {
    function LegacyLeadPost() {
    }
    return LegacyLeadPost;
}());
exports.LegacyLeadPost = LegacyLeadPost;
// @Route("/info/{Id}")
var Info = /** @class */ (function () {
    function Info() {
    }
    return Info;
}());
exports.Info = Info;
var CustomHttpError = /** @class */ (function () {
    function CustomHttpError() {
    }
    CustomHttpError.prototype.createResponse = function () { return new CustomHttpErrorResponse(); };
    CustomHttpError.prototype.getTypeName = function () { return "CustomHttpError"; };
    return CustomHttpError;
}());
exports.CustomHttpError = CustomHttpError;
var CustomFieldHttpError = /** @class */ (function () {
    function CustomFieldHttpError() {
    }
    CustomFieldHttpError.prototype.createResponse = function () { return new CustomFieldHttpErrorResponse(); };
    CustomFieldHttpError.prototype.getTypeName = function () { return "CustomFieldHttpError"; };
    return CustomFieldHttpError;
}());
exports.CustomFieldHttpError = CustomFieldHttpError;
var FallbackRoute = /** @class */ (function () {
    function FallbackRoute() {
    }
    return FallbackRoute;
}());
exports.FallbackRoute = FallbackRoute;
var NoRepeat = /** @class */ (function () {
    function NoRepeat() {
    }
    NoRepeat.prototype.createResponse = function () { return new NoRepeatResponse(); };
    NoRepeat.prototype.getTypeName = function () { return "NoRepeat"; };
    return NoRepeat;
}());
exports.NoRepeat = NoRepeat;
var BatchThrows = /** @class */ (function () {
    function BatchThrows() {
    }
    BatchThrows.prototype.createResponse = function () { return new BatchThrowsResponse(); };
    BatchThrows.prototype.getTypeName = function () { return "BatchThrows"; };
    return BatchThrows;
}());
exports.BatchThrows = BatchThrows;
var BatchThrowsAsync = /** @class */ (function () {
    function BatchThrowsAsync() {
    }
    BatchThrowsAsync.prototype.createResponse = function () { return new BatchThrowsResponse(); };
    BatchThrowsAsync.prototype.getTypeName = function () { return "BatchThrowsAsync"; };
    return BatchThrowsAsync;
}());
exports.BatchThrowsAsync = BatchThrowsAsync;
// @Route("/code/object", "GET")
var ObjectId = /** @class */ (function () {
    function ObjectId() {
    }
    ObjectId.prototype.createResponse = function () { return new ObjectDesignResponse(); };
    ObjectId.prototype.getTypeName = function () { return "ObjectId"; };
    return ObjectId;
}());
exports.ObjectId = ObjectId;
var MetadataTest = /** @class */ (function () {
    function MetadataTest() {
    }
    MetadataTest.prototype.createResponse = function () { return new MetadataTestResponse(); };
    MetadataTest.prototype.getTypeName = function () { return "MetadataTest"; };
    return MetadataTest;
}());
exports.MetadataTest = MetadataTest;
// @Route("/example", "GET")
// @DataContract
var GetExample = /** @class */ (function () {
    function GetExample() {
    }
    GetExample.prototype.createResponse = function () { return new GetExampleResponse(); };
    GetExample.prototype.getTypeName = function () { return "GetExample"; };
    return GetExample;
}());
exports.GetExample = GetExample;
var MetadataRequest = /** @class */ (function () {
    function MetadataRequest() {
    }
    MetadataRequest.prototype.createResponse = function () { return new AutoQueryMetadataResponse(); };
    MetadataRequest.prototype.getTypeName = function () { return "MetadataRequest"; };
    return MetadataRequest;
}());
exports.MetadataRequest = MetadataRequest;
var ExcludeMetadataProperty = /** @class */ (function () {
    function ExcludeMetadataProperty() {
    }
    return ExcludeMetadataProperty;
}());
exports.ExcludeMetadataProperty = ExcludeMetadataProperty;
// @Route("/namedconnection")
var NamedConnection = /** @class */ (function () {
    function NamedConnection() {
    }
    return NamedConnection;
}());
exports.NamedConnection = NamedConnection;
/**
* Description for HelloACodeGenTest
*/
var HelloACodeGenTest = /** @class */ (function () {
    function HelloACodeGenTest() {
    }
    HelloACodeGenTest.prototype.createResponse = function () { return new HelloACodeGenTestResponse(); };
    HelloACodeGenTest.prototype.getTypeName = function () { return "HelloACodeGenTest"; };
    return HelloACodeGenTest;
}());
exports.HelloACodeGenTest = HelloACodeGenTest;
var HelloInService = /** @class */ (function () {
    function HelloInService() {
    }
    HelloInService.prototype.createResponse = function () { return new HelloResponse(); };
    HelloInService.prototype.getTypeName = function () { return "NativeTypesTestService.HelloInService"; };
    return HelloInService;
}());
exports.HelloInService = HelloInService;
// @Route("/hello")
// @Route("/hello/{Name}")
var Hello = /** @class */ (function () {
    function Hello() {
    }
    Hello.prototype.createResponse = function () { return new HelloResponse(); };
    Hello.prototype.getTypeName = function () { return "Hello"; };
    return Hello;
}());
exports.Hello = Hello;
/**
* Description on HelloAll type
*/
// @DataContract
var HelloAnnotated = /** @class */ (function () {
    function HelloAnnotated() {
    }
    HelloAnnotated.prototype.createResponse = function () { return new HelloAnnotatedResponse(); };
    HelloAnnotated.prototype.getTypeName = function () { return "HelloAnnotated"; };
    return HelloAnnotated;
}());
exports.HelloAnnotated = HelloAnnotated;
var HelloWithNestedClass = /** @class */ (function () {
    function HelloWithNestedClass() {
    }
    HelloWithNestedClass.prototype.createResponse = function () { return new HelloResponse(); };
    HelloWithNestedClass.prototype.getTypeName = function () { return "HelloWithNestedClass"; };
    return HelloWithNestedClass;
}());
exports.HelloWithNestedClass = HelloWithNestedClass;
var HelloReturnList = /** @class */ (function () {
    function HelloReturnList() {
    }
    HelloReturnList.prototype.createResponse = function () { return new Array(); };
    HelloReturnList.prototype.getTypeName = function () { return "HelloReturnList"; };
    return HelloReturnList;
}());
exports.HelloReturnList = HelloReturnList;
var HelloExisting = /** @class */ (function () {
    function HelloExisting() {
    }
    HelloExisting.prototype.createResponse = function () { return new HelloExistingResponse(); };
    HelloExisting.prototype.getTypeName = function () { return "HelloExisting"; };
    return HelloExisting;
}());
exports.HelloExisting = HelloExisting;
var HelloWithEnum = /** @class */ (function () {
    function HelloWithEnum() {
    }
    return HelloWithEnum;
}());
exports.HelloWithEnum = HelloWithEnum;
var RestrictedAttributes = /** @class */ (function () {
    function RestrictedAttributes() {
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
    function AllowedAttributes() {
    }
    return AllowedAttributes;
}());
exports.AllowedAttributes = AllowedAttributes;
/**
* Multi Line Class
*/
// @Api(Description="Multi Line Class")
var HelloMultiline = /** @class */ (function () {
    function HelloMultiline() {
    }
    return HelloMultiline;
}());
exports.HelloMultiline = HelloMultiline;
var HelloAllTypes = /** @class */ (function () {
    function HelloAllTypes() {
    }
    HelloAllTypes.prototype.createResponse = function () { return new HelloAllTypesResponse(); };
    HelloAllTypes.prototype.getTypeName = function () { return "HelloAllTypes"; };
    return HelloAllTypes;
}());
exports.HelloAllTypes = HelloAllTypes;
var HelloString = /** @class */ (function () {
    function HelloString() {
    }
    HelloString.prototype.createResponse = function () { return ""; };
    HelloString.prototype.getTypeName = function () { return "HelloString"; };
    return HelloString;
}());
exports.HelloString = HelloString;
var HelloVoid = /** @class */ (function () {
    function HelloVoid() {
    }
    HelloVoid.prototype.createResponse = function () { };
    HelloVoid.prototype.getTypeName = function () { return "HelloVoid"; };
    return HelloVoid;
}());
exports.HelloVoid = HelloVoid;
// @DataContract
var HelloWithDataContract = /** @class */ (function () {
    function HelloWithDataContract() {
    }
    HelloWithDataContract.prototype.createResponse = function () { return new HelloWithDataContractResponse(); };
    HelloWithDataContract.prototype.getTypeName = function () { return "HelloWithDataContract"; };
    return HelloWithDataContract;
}());
exports.HelloWithDataContract = HelloWithDataContract;
/**
* Description on HelloWithDescription type
*/
var HelloWithDescription = /** @class */ (function () {
    function HelloWithDescription() {
    }
    HelloWithDescription.prototype.createResponse = function () { return new HelloWithDescriptionResponse(); };
    HelloWithDescription.prototype.getTypeName = function () { return "HelloWithDescription"; };
    return HelloWithDescription;
}());
exports.HelloWithDescription = HelloWithDescription;
var HelloWithInheritance = /** @class */ (function (_super) {
    __extends(HelloWithInheritance, _super);
    function HelloWithInheritance() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    HelloWithInheritance.prototype.createResponse = function () { return new HelloWithInheritanceResponse(); };
    HelloWithInheritance.prototype.getTypeName = function () { return "HelloWithInheritance"; };
    return HelloWithInheritance;
}(HelloBase));
exports.HelloWithInheritance = HelloWithInheritance;
var HelloWithGenericInheritance = /** @class */ (function (_super) {
    __extends(HelloWithGenericInheritance, _super);
    function HelloWithGenericInheritance() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return HelloWithGenericInheritance;
}(HelloBase_1));
exports.HelloWithGenericInheritance = HelloWithGenericInheritance;
var HelloWithGenericInheritance2 = /** @class */ (function (_super) {
    __extends(HelloWithGenericInheritance2, _super);
    function HelloWithGenericInheritance2() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return HelloWithGenericInheritance2;
}(HelloBase_1));
exports.HelloWithGenericInheritance2 = HelloWithGenericInheritance2;
var HelloWithNestedInheritance = /** @class */ (function (_super) {
    __extends(HelloWithNestedInheritance, _super);
    function HelloWithNestedInheritance() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return HelloWithNestedInheritance;
}(HelloBase_1));
exports.HelloWithNestedInheritance = HelloWithNestedInheritance;
var HelloWithListInheritance = /** @class */ (function (_super) {
    __extends(HelloWithListInheritance, _super);
    function HelloWithListInheritance() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    return HelloWithListInheritance;
}(Array));
exports.HelloWithListInheritance = HelloWithListInheritance;
var HelloWithReturn = /** @class */ (function () {
    function HelloWithReturn() {
    }
    HelloWithReturn.prototype.createResponse = function () { return new HelloWithAlternateReturnResponse(); };
    HelloWithReturn.prototype.getTypeName = function () { return "HelloWithReturn"; };
    return HelloWithReturn;
}());
exports.HelloWithReturn = HelloWithReturn;
// @Route("/helloroute")
var HelloWithRoute = /** @class */ (function () {
    function HelloWithRoute() {
    }
    HelloWithRoute.prototype.createResponse = function () { return new HelloWithRouteResponse(); };
    HelloWithRoute.prototype.getTypeName = function () { return "HelloWithRoute"; };
    return HelloWithRoute;
}());
exports.HelloWithRoute = HelloWithRoute;
var HelloWithType = /** @class */ (function () {
    function HelloWithType() {
    }
    HelloWithType.prototype.createResponse = function () { return new HelloWithTypeResponse(); };
    HelloWithType.prototype.getTypeName = function () { return "HelloWithType"; };
    return HelloWithType;
}());
exports.HelloWithType = HelloWithType;
var HelloSession = /** @class */ (function () {
    function HelloSession() {
    }
    HelloSession.prototype.createResponse = function () { return new HelloSessionResponse(); };
    HelloSession.prototype.getTypeName = function () { return "HelloSession"; };
    return HelloSession;
}());
exports.HelloSession = HelloSession;
var HelloInterface = /** @class */ (function () {
    function HelloInterface() {
    }
    return HelloInterface;
}());
exports.HelloInterface = HelloInterface;
var Request1 = /** @class */ (function () {
    function Request1() {
    }
    Request1.prototype.createResponse = function () { return new Request1Response(); };
    Request1.prototype.getTypeName = function () { return "Request1"; };
    return Request1;
}());
exports.Request1 = Request1;
var Request2 = /** @class */ (function () {
    function Request2() {
    }
    Request2.prototype.createResponse = function () { return new Request2Response(); };
    Request2.prototype.getTypeName = function () { return "Request2"; };
    return Request2;
}());
exports.Request2 = Request2;
var HelloInnerTypes = /** @class */ (function () {
    function HelloInnerTypes() {
    }
    HelloInnerTypes.prototype.createResponse = function () { return new HelloInnerTypesResponse(); };
    HelloInnerTypes.prototype.getTypeName = function () { return "HelloInnerTypes"; };
    return HelloInnerTypes;
}());
exports.HelloInnerTypes = HelloInnerTypes;
var GetUserSession = /** @class */ (function () {
    function GetUserSession() {
    }
    GetUserSession.prototype.createResponse = function () { return new CustomUserSession(); };
    GetUserSession.prototype.getTypeName = function () { return "GetUserSession"; };
    return GetUserSession;
}());
exports.GetUserSession = GetUserSession;
var QueryTemplate = /** @class */ (function () {
    function QueryTemplate() {
    }
    QueryTemplate.prototype.createResponse = function () { return new QueryResponseTemplate(); };
    QueryTemplate.prototype.getTypeName = function () { return "QueryTemplate"; };
    return QueryTemplate;
}());
exports.QueryTemplate = QueryTemplate;
var HelloReserved = /** @class */ (function () {
    function HelloReserved() {
    }
    return HelloReserved;
}());
exports.HelloReserved = HelloReserved;
var HelloDictionary = /** @class */ (function () {
    function HelloDictionary() {
    }
    HelloDictionary.prototype.createResponse = function () { return new Object(); };
    HelloDictionary.prototype.getTypeName = function () { return "HelloDictionary"; };
    return HelloDictionary;
}());
exports.HelloDictionary = HelloDictionary;
var HelloBuiltin = /** @class */ (function () {
    function HelloBuiltin() {
    }
    return HelloBuiltin;
}());
exports.HelloBuiltin = HelloBuiltin;
var HelloGet = /** @class */ (function () {
    function HelloGet() {
    }
    HelloGet.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloGet.prototype.getTypeName = function () { return "HelloGet"; };
    return HelloGet;
}());
exports.HelloGet = HelloGet;
var HelloPost = /** @class */ (function (_super) {
    __extends(HelloPost, _super);
    function HelloPost() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    HelloPost.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloPost.prototype.getTypeName = function () { return "HelloPost"; };
    return HelloPost;
}(HelloBase));
exports.HelloPost = HelloPost;
var HelloPut = /** @class */ (function () {
    function HelloPut() {
    }
    HelloPut.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloPut.prototype.getTypeName = function () { return "HelloPut"; };
    return HelloPut;
}());
exports.HelloPut = HelloPut;
var HelloDelete = /** @class */ (function () {
    function HelloDelete() {
    }
    HelloDelete.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloDelete.prototype.getTypeName = function () { return "HelloDelete"; };
    return HelloDelete;
}());
exports.HelloDelete = HelloDelete;
var HelloPatch = /** @class */ (function () {
    function HelloPatch() {
    }
    HelloPatch.prototype.createResponse = function () { return new HelloVerbResponse(); };
    HelloPatch.prototype.getTypeName = function () { return "HelloPatch"; };
    return HelloPatch;
}());
exports.HelloPatch = HelloPatch;
var HelloReturnVoid = /** @class */ (function () {
    function HelloReturnVoid() {
    }
    HelloReturnVoid.prototype.createResponse = function () { };
    HelloReturnVoid.prototype.getTypeName = function () { return "HelloReturnVoid"; };
    return HelloReturnVoid;
}());
exports.HelloReturnVoid = HelloReturnVoid;
var EnumRequest = /** @class */ (function () {
    function EnumRequest() {
    }
    EnumRequest.prototype.createResponse = function () { return new EnumResponse(); };
    EnumRequest.prototype.getTypeName = function () { return "EnumRequest"; };
    return EnumRequest;
}());
exports.EnumRequest = EnumRequest;
var ExcludeTest1 = /** @class */ (function () {
    function ExcludeTest1() {
    }
    ExcludeTest1.prototype.createResponse = function () { return new ExcludeTestNested(); };
    ExcludeTest1.prototype.getTypeName = function () { return "ExcludeTest1"; };
    return ExcludeTest1;
}());
exports.ExcludeTest1 = ExcludeTest1;
var ExcludeTest2 = /** @class */ (function () {
    function ExcludeTest2() {
    }
    ExcludeTest2.prototype.createResponse = function () { return ""; };
    ExcludeTest2.prototype.getTypeName = function () { return "ExcludeTest2"; };
    return ExcludeTest2;
}());
exports.ExcludeTest2 = ExcludeTest2;
var HelloAuthenticated = /** @class */ (function () {
    function HelloAuthenticated() {
    }
    HelloAuthenticated.prototype.createResponse = function () { return new HelloAuthenticatedResponse(); };
    HelloAuthenticated.prototype.getTypeName = function () { return "HelloAuthenticated"; };
    return HelloAuthenticated;
}());
exports.HelloAuthenticated = HelloAuthenticated;
/**
* Echoes a sentence
*/
// @Route("/echoes", "POST")
// @Api(Description="Echoes a sentence")
var Echoes = /** @class */ (function () {
    function Echoes() {
    }
    Echoes.prototype.createResponse = function () { return new Echo(); };
    Echoes.prototype.getTypeName = function () { return "Echoes"; };
    return Echoes;
}());
exports.Echoes = Echoes;
var CachedEcho = /** @class */ (function () {
    function CachedEcho() {
    }
    CachedEcho.prototype.createResponse = function () { return new Echo(); };
    CachedEcho.prototype.getTypeName = function () { return "CachedEcho"; };
    return CachedEcho;
}());
exports.CachedEcho = CachedEcho;
var AsyncTest = /** @class */ (function () {
    function AsyncTest() {
    }
    AsyncTest.prototype.createResponse = function () { return new Echo(); };
    AsyncTest.prototype.getTypeName = function () { return "AsyncTest"; };
    return AsyncTest;
}());
exports.AsyncTest = AsyncTest;
// @Route("/throwhttperror/{Status}")
var ThrowHttpError = /** @class */ (function () {
    function ThrowHttpError() {
    }
    ThrowHttpError.prototype.createResponse = function () { return new ThrowHttpErrorResponse(); };
    ThrowHttpError.prototype.getTypeName = function () { return "ThrowHttpError"; };
    return ThrowHttpError;
}());
exports.ThrowHttpError = ThrowHttpError;
// @Route("/throw404")
// @Route("/throw404/{Message}")
var Throw404 = /** @class */ (function () {
    function Throw404() {
    }
    return Throw404;
}());
exports.Throw404 = Throw404;
// @Route("/return404")
var Return404 = /** @class */ (function () {
    function Return404() {
    }
    return Return404;
}());
exports.Return404 = Return404;
// @Route("/return404result")
var Return404Result = /** @class */ (function () {
    function Return404Result() {
    }
    return Return404Result;
}());
exports.Return404Result = Return404Result;
// @Route("/throw/{Type}")
var ThrowType = /** @class */ (function () {
    function ThrowType() {
    }
    ThrowType.prototype.createResponse = function () { return new ThrowTypeResponse(); };
    ThrowType.prototype.getTypeName = function () { return "ThrowType"; };
    return ThrowType;
}());
exports.ThrowType = ThrowType;
// @Route("/throwvalidation")
var ThrowValidation = /** @class */ (function () {
    function ThrowValidation() {
    }
    ThrowValidation.prototype.createResponse = function () { return new ThrowValidationResponse(); };
    ThrowValidation.prototype.getTypeName = function () { return "ThrowValidation"; };
    return ThrowValidation;
}());
exports.ThrowValidation = ThrowValidation;
// @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
// @Route("/api/acsprofiles/{profileId}")
var ACSProfile = /** @class */ (function () {
    function ACSProfile() {
    }
    ACSProfile.prototype.createResponse = function () { return new acsprofileResponse(); };
    ACSProfile.prototype.getTypeName = function () { return "ACSProfile"; };
    return ACSProfile;
}());
exports.ACSProfile = ACSProfile;
// @Route("/return/string")
var ReturnString = /** @class */ (function () {
    function ReturnString() {
    }
    ReturnString.prototype.createResponse = function () { return ""; };
    ReturnString.prototype.getTypeName = function () { return "ReturnString"; };
    return ReturnString;
}());
exports.ReturnString = ReturnString;
// @Route("/return/bytes")
var ReturnBytes = /** @class */ (function () {
    function ReturnBytes() {
    }
    ReturnBytes.prototype.createResponse = function () { return new Uint8Array(0); };
    ReturnBytes.prototype.getTypeName = function () { return "ReturnBytes"; };
    return ReturnBytes;
}());
exports.ReturnBytes = ReturnBytes;
// @Route("/return/stream")
var ReturnStream = /** @class */ (function () {
    function ReturnStream() {
    }
    ReturnStream.prototype.createResponse = function () { return new Blob(); };
    ReturnStream.prototype.getTypeName = function () { return "ReturnStream"; };
    return ReturnStream;
}());
exports.ReturnStream = ReturnStream;
// @Route("/Request1/", "GET")
var GetRequest1 = /** @class */ (function () {
    function GetRequest1() {
    }
    GetRequest1.prototype.createResponse = function () { return new Array(); };
    GetRequest1.prototype.getTypeName = function () { return "GetRequest1"; };
    return GetRequest1;
}());
exports.GetRequest1 = GetRequest1;
// @Route("/Request3", "GET")
var GetRequest2 = /** @class */ (function () {
    function GetRequest2() {
    }
    GetRequest2.prototype.createResponse = function () { return new ReturnedDto(); };
    GetRequest2.prototype.getTypeName = function () { return "GetRequest2"; };
    return GetRequest2;
}());
exports.GetRequest2 = GetRequest2;
// @Route("/matchlast/{Id}")
var MatchesLastInt = /** @class */ (function () {
    function MatchesLastInt() {
    }
    return MatchesLastInt;
}());
exports.MatchesLastInt = MatchesLastInt;
// @Route("/matchlast/{Slug}")
var MatchesNotLastInt = /** @class */ (function () {
    function MatchesNotLastInt() {
    }
    return MatchesNotLastInt;
}());
exports.MatchesNotLastInt = MatchesNotLastInt;
// @Route("/matchregex/{Id}")
var MatchesId = /** @class */ (function () {
    function MatchesId() {
    }
    return MatchesId;
}());
exports.MatchesId = MatchesId;
// @Route("/matchregex/{Slug}")
var MatchesSlug = /** @class */ (function () {
    function MatchesSlug() {
    }
    return MatchesSlug;
}());
exports.MatchesSlug = MatchesSlug;
// @Route("/{Version}/userdata", "GET")
var SwaggerVersionTest = /** @class */ (function () {
    function SwaggerVersionTest() {
    }
    return SwaggerVersionTest;
}());
exports.SwaggerVersionTest = SwaggerVersionTest;
// @Route("/test/errorview")
var TestErrorView = /** @class */ (function () {
    function TestErrorView() {
    }
    return TestErrorView;
}());
exports.TestErrorView = TestErrorView;
// @Route("/timestamp", "GET")
var GetTimestamp = /** @class */ (function () {
    function GetTimestamp() {
    }
    GetTimestamp.prototype.createResponse = function () { return new TimestampData(); };
    GetTimestamp.prototype.getTypeName = function () { return "GetTimestamp"; };
    return GetTimestamp;
}());
exports.GetTimestamp = GetTimestamp;
var TestMiniverView = /** @class */ (function () {
    function TestMiniverView() {
    }
    return TestMiniverView;
}());
exports.TestMiniverView = TestMiniverView;
// @Route("/testexecproc")
var TestExecProc = /** @class */ (function () {
    function TestExecProc() {
    }
    return TestExecProc;
}());
exports.TestExecProc = TestExecProc;
// @Route("/files/{Path*}")
var GetFile = /** @class */ (function () {
    function GetFile() {
    }
    return GetFile;
}());
exports.GetFile = GetFile;
// @Route("/test/html2")
var TestHtml2 = /** @class */ (function () {
    function TestHtml2() {
    }
    return TestHtml2;
}());
exports.TestHtml2 = TestHtml2;
// @Route("/views/request")
var ViewRequest = /** @class */ (function () {
    function ViewRequest() {
    }
    return ViewRequest;
}());
exports.ViewRequest = ViewRequest;
// @Route("/index")
var IndexPage = /** @class */ (function () {
    function IndexPage() {
    }
    return IndexPage;
}());
exports.IndexPage = IndexPage;
// @Route("/return/text")
var ReturnText = /** @class */ (function () {
    function ReturnText() {
    }
    return ReturnText;
}());
exports.ReturnText = ReturnText;
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
    function SwaggerTest() {
    }
    return SwaggerTest;
}());
exports.SwaggerTest = SwaggerTest;
// @Route("/swaggertest2", "POST")
var SwaggerTest2 = /** @class */ (function () {
    function SwaggerTest2() {
    }
    return SwaggerTest2;
}());
exports.SwaggerTest2 = SwaggerTest2;
// @Route("/swagger-complex", "POST")
var SwaggerComplex = /** @class */ (function () {
    function SwaggerComplex() {
    }
    SwaggerComplex.prototype.createResponse = function () { return new SwaggerComplexResponse(); };
    SwaggerComplex.prototype.getTypeName = function () { return "SwaggerComplex"; };
    return SwaggerComplex;
}());
exports.SwaggerComplex = SwaggerComplex;
// @Route("/swaggerpost/{Required1}", "GET")
// @Route("/swaggerpost/{Required1}/{Optional1}", "GET")
// @Route("/swaggerpost", "POST")
var SwaggerPostTest = /** @class */ (function () {
    function SwaggerPostTest() {
    }
    SwaggerPostTest.prototype.createResponse = function () { return new HelloResponse(); };
    SwaggerPostTest.prototype.getTypeName = function () { return "SwaggerPostTest"; };
    return SwaggerPostTest;
}());
exports.SwaggerPostTest = SwaggerPostTest;
// @Route("/swaggerpost2/{Required1}/{Required2}", "GET")
// @Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", "GET")
// @Route("/swaggerpost2", "POST")
var SwaggerPostTest2 = /** @class */ (function () {
    function SwaggerPostTest2() {
    }
    SwaggerPostTest2.prototype.createResponse = function () { return new HelloResponse(); };
    SwaggerPostTest2.prototype.getTypeName = function () { return "SwaggerPostTest2"; };
    return SwaggerPostTest2;
}());
exports.SwaggerPostTest2 = SwaggerPostTest2;
// @Route("/swagger/multiattrtest", "POST")
// @ApiResponse(Description="Code 1", StatusCode=400)
// @ApiResponse(Description="Code 2", StatusCode=402)
// @ApiResponse(Description="Code 3", StatusCode=401)
var SwaggerMultiApiResponseTest = /** @class */ (function () {
    function SwaggerMultiApiResponseTest() {
    }
    SwaggerMultiApiResponseTest.prototype.createResponse = function () { };
    SwaggerMultiApiResponseTest.prototype.getTypeName = function () { return "SwaggerMultiApiResponseTest"; };
    return SwaggerMultiApiResponseTest;
}());
exports.SwaggerMultiApiResponseTest = SwaggerMultiApiResponseTest;
// @Route("/dynamically/registered/{Name}")
var DynamicallyRegistered = /** @class */ (function () {
    function DynamicallyRegistered() {
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
    function Authenticate() {
    }
    Authenticate.prototype.createResponse = function () { return new AuthenticateResponse(); };
    Authenticate.prototype.getTypeName = function () { return "Authenticate"; };
    return Authenticate;
}());
exports.Authenticate = Authenticate;
// @Route("/assignroles")
// @DataContract
var AssignRoles = /** @class */ (function () {
    function AssignRoles() {
    }
    AssignRoles.prototype.createResponse = function () { return new AssignRolesResponse(); };
    AssignRoles.prototype.getTypeName = function () { return "AssignRoles"; };
    return AssignRoles;
}());
exports.AssignRoles = AssignRoles;
// @Route("/unassignroles")
// @DataContract
var UnAssignRoles = /** @class */ (function () {
    function UnAssignRoles() {
    }
    UnAssignRoles.prototype.createResponse = function () { return new UnAssignRolesResponse(); };
    UnAssignRoles.prototype.getTypeName = function () { return "UnAssignRoles"; };
    return UnAssignRoles;
}());
exports.UnAssignRoles = UnAssignRoles;
// @Route("/apikeys")
// @Route("/apikeys/{Environment}")
// @DataContract
var GetApiKeys = /** @class */ (function () {
    function GetApiKeys() {
    }
    GetApiKeys.prototype.createResponse = function () { return new GetApiKeysResponse(); };
    GetApiKeys.prototype.getTypeName = function () { return "GetApiKeys"; };
    return GetApiKeys;
}());
exports.GetApiKeys = GetApiKeys;
// @Route("/apikeys/regenerate")
// @Route("/apikeys/regenerate/{Environment}")
// @DataContract
var RegenerateApiKeys = /** @class */ (function () {
    function RegenerateApiKeys() {
    }
    RegenerateApiKeys.prototype.createResponse = function () { return new GetApiKeysResponse(); };
    RegenerateApiKeys.prototype.getTypeName = function () { return "RegenerateApiKeys"; };
    return RegenerateApiKeys;
}());
exports.RegenerateApiKeys = RegenerateApiKeys;
// @Route("/register")
// @DataContract
var Register = /** @class */ (function () {
    function Register() {
    }
    Register.prototype.createResponse = function () { return new RegisterResponse(); };
    Register.prototype.getTypeName = function () { return "Register"; };
    return Register;
}());
exports.Register = Register;
// @Route("/pgsql/rockstars")
var QueryPostgresRockstars = /** @class */ (function (_super) {
    __extends(QueryPostgresRockstars, _super);
    function QueryPostgresRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryPostgresRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPostgresRockstars.prototype.getTypeName = function () { return "QueryPostgresRockstars"; };
    return QueryPostgresRockstars;
}(QueryDb_1));
exports.QueryPostgresRockstars = QueryPostgresRockstars;
// @Route("/pgsql/pgrockstars")
var QueryPostgresPgRockstars = /** @class */ (function (_super) {
    __extends(QueryPostgresPgRockstars, _super);
    function QueryPostgresPgRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryPostgresPgRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPostgresPgRockstars.prototype.getTypeName = function () { return "QueryPostgresPgRockstars"; };
    return QueryPostgresPgRockstars;
}(QueryDb_1));
exports.QueryPostgresPgRockstars = QueryPostgresPgRockstars;
var QueryRockstarsConventions = /** @class */ (function (_super) {
    __extends(QueryRockstarsConventions, _super);
    function QueryRockstarsConventions() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarsConventions.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsConventions.prototype.getTypeName = function () { return "QueryRockstarsConventions"; };
    return QueryRockstarsConventions;
}(QueryDb_1));
exports.QueryRockstarsConventions = QueryRockstarsConventions;
// @AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")
var QueryCustomRockstars = /** @class */ (function (_super) {
    __extends(QueryCustomRockstars, _super);
    function QueryCustomRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryCustomRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryCustomRockstars.prototype.getTypeName = function () { return "QueryCustomRockstars"; };
    return QueryCustomRockstars;
}(QueryDb_2));
exports.QueryCustomRockstars = QueryCustomRockstars;
// @Route("/customrockstars")
var QueryRockstarAlbums = /** @class */ (function (_super) {
    __extends(QueryRockstarAlbums, _super);
    function QueryRockstarAlbums() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarAlbums.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarAlbums.prototype.getTypeName = function () { return "QueryRockstarAlbums"; };
    return QueryRockstarAlbums;
}(QueryDb_2));
exports.QueryRockstarAlbums = QueryRockstarAlbums;
var QueryRockstarAlbumsImplicit = /** @class */ (function (_super) {
    __extends(QueryRockstarAlbumsImplicit, _super);
    function QueryRockstarAlbumsImplicit() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarAlbumsImplicit.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarAlbumsImplicit.prototype.getTypeName = function () { return "QueryRockstarAlbumsImplicit"; };
    return QueryRockstarAlbumsImplicit;
}(QueryDb_2));
exports.QueryRockstarAlbumsImplicit = QueryRockstarAlbumsImplicit;
var QueryRockstarAlbumsLeftJoin = /** @class */ (function (_super) {
    __extends(QueryRockstarAlbumsLeftJoin, _super);
    function QueryRockstarAlbumsLeftJoin() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarAlbumsLeftJoin.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarAlbumsLeftJoin.prototype.getTypeName = function () { return "QueryRockstarAlbumsLeftJoin"; };
    return QueryRockstarAlbumsLeftJoin;
}(QueryDb_2));
exports.QueryRockstarAlbumsLeftJoin = QueryRockstarAlbumsLeftJoin;
var QueryOverridedRockstars = /** @class */ (function (_super) {
    __extends(QueryOverridedRockstars, _super);
    function QueryOverridedRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryOverridedRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryOverridedRockstars.prototype.getTypeName = function () { return "QueryOverridedRockstars"; };
    return QueryOverridedRockstars;
}(QueryDb_1));
exports.QueryOverridedRockstars = QueryOverridedRockstars;
var QueryOverridedCustomRockstars = /** @class */ (function (_super) {
    __extends(QueryOverridedCustomRockstars, _super);
    function QueryOverridedCustomRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryOverridedCustomRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryOverridedCustomRockstars.prototype.getTypeName = function () { return "QueryOverridedCustomRockstars"; };
    return QueryOverridedCustomRockstars;
}(QueryDb_2));
exports.QueryOverridedCustomRockstars = QueryOverridedCustomRockstars;
// @Route("/query-custom/rockstars")
var QueryFieldRockstars = /** @class */ (function (_super) {
    __extends(QueryFieldRockstars, _super);
    function QueryFieldRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryFieldRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryFieldRockstars.prototype.getTypeName = function () { return "QueryFieldRockstars"; };
    return QueryFieldRockstars;
}(QueryDb_1));
exports.QueryFieldRockstars = QueryFieldRockstars;
var QueryFieldRockstarsDynamic = /** @class */ (function (_super) {
    __extends(QueryFieldRockstarsDynamic, _super);
    function QueryFieldRockstarsDynamic() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryFieldRockstarsDynamic.prototype.createResponse = function () { return new QueryResponse(); };
    QueryFieldRockstarsDynamic.prototype.getTypeName = function () { return "QueryFieldRockstarsDynamic"; };
    return QueryFieldRockstarsDynamic;
}(QueryDb_1));
exports.QueryFieldRockstarsDynamic = QueryFieldRockstarsDynamic;
var QueryRockstarsFilter = /** @class */ (function (_super) {
    __extends(QueryRockstarsFilter, _super);
    function QueryRockstarsFilter() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarsFilter.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsFilter.prototype.getTypeName = function () { return "QueryRockstarsFilter"; };
    return QueryRockstarsFilter;
}(QueryDb_1));
exports.QueryRockstarsFilter = QueryRockstarsFilter;
var QueryCustomRockstarsFilter = /** @class */ (function (_super) {
    __extends(QueryCustomRockstarsFilter, _super);
    function QueryCustomRockstarsFilter() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryCustomRockstarsFilter.prototype.createResponse = function () { return new QueryResponse(); };
    QueryCustomRockstarsFilter.prototype.getTypeName = function () { return "QueryCustomRockstarsFilter"; };
    return QueryCustomRockstarsFilter;
}(QueryDb_2));
exports.QueryCustomRockstarsFilter = QueryCustomRockstarsFilter;
var QueryRockstarsIFilter = /** @class */ (function (_super) {
    __extends(QueryRockstarsIFilter, _super);
    function QueryRockstarsIFilter() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarsIFilter.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsIFilter.prototype.getTypeName = function () { return "QueryRockstarsIFilter"; };
    return QueryRockstarsIFilter;
}(QueryDb_1));
exports.QueryRockstarsIFilter = QueryRockstarsIFilter;
// @Route("/OrRockstars")
var QueryOrRockstars = /** @class */ (function (_super) {
    __extends(QueryOrRockstars, _super);
    function QueryOrRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryOrRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryOrRockstars.prototype.getTypeName = function () { return "QueryOrRockstars"; };
    return QueryOrRockstars;
}(QueryDb_1));
exports.QueryOrRockstars = QueryOrRockstars;
var QueryGetRockstars = /** @class */ (function (_super) {
    __extends(QueryGetRockstars, _super);
    function QueryGetRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryGetRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryGetRockstars.prototype.getTypeName = function () { return "QueryGetRockstars"; };
    return QueryGetRockstars;
}(QueryDb_1));
exports.QueryGetRockstars = QueryGetRockstars;
var QueryGetRockstarsDynamic = /** @class */ (function (_super) {
    __extends(QueryGetRockstarsDynamic, _super);
    function QueryGetRockstarsDynamic() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryGetRockstarsDynamic.prototype.createResponse = function () { return new QueryResponse(); };
    QueryGetRockstarsDynamic.prototype.getTypeName = function () { return "QueryGetRockstarsDynamic"; };
    return QueryGetRockstarsDynamic;
}(QueryDb_1));
exports.QueryGetRockstarsDynamic = QueryGetRockstarsDynamic;
// @Route("/movies/search")
var SearchMovies = /** @class */ (function (_super) {
    __extends(SearchMovies, _super);
    function SearchMovies() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    SearchMovies.prototype.createResponse = function () { return new QueryResponse(); };
    SearchMovies.prototype.getTypeName = function () { return "SearchMovies"; };
    return SearchMovies;
}(QueryDb_1));
exports.SearchMovies = SearchMovies;
// @Route("/movies")
var QueryMovies = /** @class */ (function (_super) {
    __extends(QueryMovies, _super);
    function QueryMovies() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryMovies.prototype.createResponse = function () { return new QueryResponse(); };
    QueryMovies.prototype.getTypeName = function () { return "QueryMovies"; };
    return QueryMovies;
}(QueryDb_1));
exports.QueryMovies = QueryMovies;
var StreamMovies = /** @class */ (function (_super) {
    __extends(StreamMovies, _super);
    function StreamMovies() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    StreamMovies.prototype.createResponse = function () { return new QueryResponse(); };
    StreamMovies.prototype.getTypeName = function () { return "StreamMovies"; };
    return StreamMovies;
}(QueryDb_1));
exports.StreamMovies = StreamMovies;
var QueryUnknownRockstars = /** @class */ (function (_super) {
    __extends(QueryUnknownRockstars, _super);
    function QueryUnknownRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryUnknownRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryUnknownRockstars.prototype.getTypeName = function () { return "QueryUnknownRockstars"; };
    return QueryUnknownRockstars;
}(QueryDb_1));
exports.QueryUnknownRockstars = QueryUnknownRockstars;
// @Route("/query/rockstar-references")
var QueryRockstarsWithReferences = /** @class */ (function (_super) {
    __extends(QueryRockstarsWithReferences, _super);
    function QueryRockstarsWithReferences() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryRockstarsWithReferences.prototype.createResponse = function () { return new QueryResponse(); };
    QueryRockstarsWithReferences.prototype.getTypeName = function () { return "QueryRockstarsWithReferences"; };
    return QueryRockstarsWithReferences;
}(QueryDb_1));
exports.QueryRockstarsWithReferences = QueryRockstarsWithReferences;
var QueryPocoBase = /** @class */ (function (_super) {
    __extends(QueryPocoBase, _super);
    function QueryPocoBase() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryPocoBase.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPocoBase.prototype.getTypeName = function () { return "QueryPocoBase"; };
    return QueryPocoBase;
}(QueryDb_1));
exports.QueryPocoBase = QueryPocoBase;
var QueryPocoIntoBase = /** @class */ (function (_super) {
    __extends(QueryPocoIntoBase, _super);
    function QueryPocoIntoBase() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryPocoIntoBase.prototype.createResponse = function () { return new QueryResponse(); };
    QueryPocoIntoBase.prototype.getTypeName = function () { return "QueryPocoIntoBase"; };
    return QueryPocoIntoBase;
}(QueryDb_2));
exports.QueryPocoIntoBase = QueryPocoIntoBase;
// @Route("/query/alltypes")
var QueryAllTypes = /** @class */ (function (_super) {
    __extends(QueryAllTypes, _super);
    function QueryAllTypes() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryAllTypes.prototype.createResponse = function () { return new QueryResponse(); };
    QueryAllTypes.prototype.getTypeName = function () { return "QueryAllTypes"; };
    return QueryAllTypes;
}(QueryDb_1));
exports.QueryAllTypes = QueryAllTypes;
// @Route("/querydata/rockstars")
var QueryDataRockstars = /** @class */ (function (_super) {
    __extends(QueryDataRockstars, _super);
    function QueryDataRockstars() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    QueryDataRockstars.prototype.createResponse = function () { return new QueryResponse(); };
    QueryDataRockstars.prototype.getTypeName = function () { return "QueryDataRockstars"; };
    return QueryDataRockstars;
}(QueryData));
exports.QueryDataRockstars = QueryDataRockstars;
