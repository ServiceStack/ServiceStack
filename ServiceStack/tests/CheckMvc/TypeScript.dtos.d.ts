/* Options:
Date: 2017-11-22 18:17:56
Version: 5.00
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:55799

GlobalNamespace: dtos
//MakePropertiesOptional: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/


declare module dtos
{

    interface IReturn<T>
    {
    }

    interface IReturnVoid
    {
    }

    interface IMeta
    {
        meta?: { [index:string]: string; };
    }

    interface IGet
    {
    }

    interface IPost
    {
    }

    interface IPut
    {
    }

    interface IDelete
    {
    }

    interface IPatch
    {
    }

    interface IHasSessionId
    {
        sessionId?: string;
    }

    interface IHasVersion
    {
        version?: number;
    }

    interface QueryBase
    {
        // @DataMember(Order=1)
        skip?: number;

        // @DataMember(Order=2)
        take?: number;

        // @DataMember(Order=3)
        orderBy?: string;

        // @DataMember(Order=4)
        orderByDesc?: string;

        // @DataMember(Order=5)
        include?: string;

        // @DataMember(Order=6)
        fields?: string;

        // @DataMember(Order=7)
        meta?: { [index:string]: string; };
    }

    interface QueryData<T> extends QueryBase
    {
    }

    interface RequestLogEntry
    {
        id?: number;
        dateTime?: string;
        statusCode?: number;
        statusDescription?: string;
        httpMethod?: string;
        absoluteUri?: string;
        pathInfo?: string;
        requestBody?: string;
        requestDto?: Object;
        userAuthId?: string;
        sessionId?: string;
        ipAddress?: string;
        forwardedFor?: string;
        referer?: string;
        headers?: { [index:string]: string; };
        formData?: { [index:string]: string; };
        items?: { [index:string]: string; };
        session?: Object;
        responseDto?: Object;
        errorResponse?: Object;
        exceptionSource?: string;
        exceptionData?: any;
        requestDuration?: string;
    }

    // @DataContract
    interface ResponseError
    {
        // @DataMember(Order=1, EmitDefaultValue=false)
        errorCode?: string;

        // @DataMember(Order=2, EmitDefaultValue=false)
        fieldName?: string;

        // @DataMember(Order=3, EmitDefaultValue=false)
        message?: string;

        // @DataMember(Order=4, EmitDefaultValue=false)
        meta?: { [index:string]: string; };
    }

    // @DataContract
    interface ResponseStatus
    {
        // @DataMember(Order=1)
        errorCode?: string;

        // @DataMember(Order=2)
        message?: string;

        // @DataMember(Order=3)
        stackTrace?: string;

        // @DataMember(Order=4)
        errors?: ResponseError[];

        // @DataMember(Order=5)
        meta?: { [index:string]: string; };
    }

    interface QueryDb_1<T> extends QueryBase
    {
    }

    interface Rockstar
    {
        /**
        * Идентификатор
        */
        id?: number;
        /**
        * Фамилия
        */
        firstName?: string;
        /**
        * Имя
        */
        lastName?: string;
        /**
        * Возраст
        */
        age?: number;
    }

    interface ObjectDesign
    {
        id?: number;
    }

    interface MetadataTestNestedChild
    {
        name?: string;
    }

    interface MetadataTestChild
    {
        name?: string;
        results?: MetadataTestNestedChild[];
    }

    interface MenuItemExampleItem
    {
        // @DataMember(Order=1)
        // @ApiMember()
        name1?: string;
    }

    interface MenuItemExample
    {
        // @DataMember(Order=1)
        // @ApiMember()
        name1?: string;

        menuItemExampleItem?: MenuItemExampleItem;
    }

    // @DataContract
    interface MenuExample
    {
        // @DataMember(Order=1)
        // @ApiMember()
        menuItemExample1?: MenuItemExample;
    }

    interface MetadataTypeName
    {
        name?: string;
        namespace?: string;
        genericArgs?: string[];
    }

    interface MetadataRoute
    {
        path?: string;
        verbs?: string;
        notes?: string;
        summary?: string;
    }

    interface MetadataDataContract
    {
        name?: string;
        namespace?: string;
    }

    interface MetadataDataMember
    {
        name?: string;
        order?: number;
        isRequired?: boolean;
        emitDefaultValue?: boolean;
    }

    interface MetadataAttribute
    {
        name?: string;
        constructorArgs?: MetadataPropertyType[];
        args?: MetadataPropertyType[];
    }

    interface MetadataPropertyType
    {
        name?: string;
        type?: string;
        isValueType?: boolean;
        isSystemType?: boolean;
        isEnum?: boolean;
        typeNamespace?: string;
        genericArgs?: string[];
        value?: string;
        description?: string;
        dataMember?: MetadataDataMember;
        readOnly?: boolean;
        paramType?: string;
        displayType?: string;
        isRequired?: boolean;
        allowableValues?: string[];
        allowableMin?: number;
        allowableMax?: number;
        attributes?: MetadataAttribute[];
    }

    interface MetadataType
    {
        name?: string;
        namespace?: string;
        genericArgs?: string[];
        inherits?: MetadataTypeName;
        implements?: MetadataTypeName[];
        displayType?: string;
        description?: string;
        returnVoidMarker?: boolean;
        isNested?: boolean;
        isEnum?: boolean;
        isEnumInt?: boolean;
        isInterface?: boolean;
        isAbstract?: boolean;
        returnMarkerTypeName?: MetadataTypeName;
        routes?: MetadataRoute[];
        dataContract?: MetadataDataContract;
        properties?: MetadataPropertyType[];
        attributes?: MetadataAttribute[];
        innerTypes?: MetadataTypeName[];
        enumNames?: string[];
        enumValues?: string[];
        meta?: { [index:string]: string; };
    }

    interface AutoQueryConvention
    {
        name?: string;
        value?: string;
        types?: string;
    }

    interface AutoQueryViewerConfig
    {
        serviceBaseUrl?: string;
        serviceName?: string;
        serviceDescription?: string;
        serviceIconUrl?: string;
        formats?: string[];
        maxLimit?: number;
        isPublic?: boolean;
        onlyShowAnnotatedServices?: boolean;
        implicitConventions?: AutoQueryConvention[];
        defaultSearchField?: string;
        defaultSearchType?: string;
        defaultSearchText?: string;
        brandUrl?: string;
        brandImageUrl?: string;
        textColor?: string;
        linkColor?: string;
        backgroundColor?: string;
        backgroundImageUrl?: string;
        iconUrl?: string;
        meta?: { [index:string]: string; };
    }

    interface AutoQueryViewerUserInfo
    {
        isAuthenticated?: boolean;
        queryCount?: number;
        meta?: { [index:string]: string; };
    }

    interface AutoQueryOperation
    {
        request?: string;
        from?: string;
        to?: string;
        meta?: { [index:string]: string; };
    }

    interface NativeTypesTestService
    {
    }

    interface NestedClass
    {
        value?: string;
    }

    interface ListResult
    {
        result?: string;
    }

    interface OnlyInReturnListArg
    {
        result?: string;
    }

    interface ArrayResult
    {
        result?: string;
    }

    type EnumType = "Value1" | "Value2";

    type EnumWithValues = "Value1" | "Value2";

    // @Flags()
    enum EnumFlags
    {
        Value1 = 1,
        Value2 = 2,
        Value3 = 4,
    }

    interface Poco
    {
        name?: string;
    }

    interface AllCollectionTypes
    {
        intArray?: number[];
        intList?: number[];
        stringArray?: string[];
        stringList?: string[];
        pocoArray?: Poco[];
        pocoList?: Poco[];
        nullableByteArray?: Uint8Array;
        nullableByteList?: number[];
        nullableDateTimeArray?: string[];
        nullableDateTimeList?: string[];
        pocoLookup?: { [index:string]: Poco[]; };
        pocoLookupMap?: { [index:string]: { [index:string]: Poco; }[]; };
    }

    interface KeyValuePair<TKey, TValue>
    {
        key?: TKey;
        value?: TValue;
    }

    interface SubType
    {
        id?: number;
        name?: string;
    }

    interface HelloBase
    {
        id?: number;
    }

    interface HelloResponseBase
    {
        refId?: number;
    }

    interface HelloBase_1<T>
    {
        items?: T[];
        counts?: number[];
    }

    interface Item
    {
        value?: string;
    }

    interface InheritedItem
    {
        name?: string;
    }

    interface HelloWithReturnResponse
    {
        result?: string;
    }

    interface HelloType
    {
        result?: string;
    }

    interface IAuthTokens
    {
        provider?: string;
        userId?: string;
        accessToken?: string;
        accessTokenSecret?: string;
        refreshToken?: string;
        refreshTokenExpiry?: string;
        requestToken?: string;
        requestTokenSecret?: string;
        items?: { [index:string]: string; };
    }

    // @DataContract
    interface AuthUserSession
    {
        // @DataMember(Order=1)
        referrerUrl?: string;

        // @DataMember(Order=2)
        id?: string;

        // @DataMember(Order=3)
        userAuthId?: string;

        // @DataMember(Order=4)
        userAuthName?: string;

        // @DataMember(Order=5)
        userName?: string;

        // @DataMember(Order=6)
        twitterUserId?: string;

        // @DataMember(Order=7)
        twitterScreenName?: string;

        // @DataMember(Order=8)
        facebookUserId?: string;

        // @DataMember(Order=9)
        facebookUserName?: string;

        // @DataMember(Order=10)
        firstName?: string;

        // @DataMember(Order=11)
        lastName?: string;

        // @DataMember(Order=12)
        displayName?: string;

        // @DataMember(Order=13)
        company?: string;

        // @DataMember(Order=14)
        email?: string;

        // @DataMember(Order=15)
        primaryEmail?: string;

        // @DataMember(Order=16)
        phoneNumber?: string;

        // @DataMember(Order=17)
        birthDate?: string;

        // @DataMember(Order=18)
        birthDateRaw?: string;

        // @DataMember(Order=19)
        address?: string;

        // @DataMember(Order=20)
        address2?: string;

        // @DataMember(Order=21)
        city?: string;

        // @DataMember(Order=22)
        state?: string;

        // @DataMember(Order=23)
        country?: string;

        // @DataMember(Order=24)
        culture?: string;

        // @DataMember(Order=25)
        fullName?: string;

        // @DataMember(Order=26)
        gender?: string;

        // @DataMember(Order=27)
        language?: string;

        // @DataMember(Order=28)
        mailAddress?: string;

        // @DataMember(Order=29)
        nickname?: string;

        // @DataMember(Order=30)
        postalCode?: string;

        // @DataMember(Order=31)
        timeZone?: string;

        // @DataMember(Order=32)
        requestTokenSecret?: string;

        // @DataMember(Order=33)
        createdAt?: string;

        // @DataMember(Order=34)
        lastModified?: string;

        // @DataMember(Order=35)
        roles?: string[];

        // @DataMember(Order=36)
        permissions?: string[];

        // @DataMember(Order=37)
        isAuthenticated?: boolean;

        // @DataMember(Order=38)
        fromToken?: boolean;

        // @DataMember(Order=39)
        profileUrl?: string;

        // @DataMember(Order=40)
        sequence?: string;

        // @DataMember(Order=41)
        tag?: number;

        // @DataMember(Order=42)
        authProvider?: string;

        // @DataMember(Order=43)
        providerOAuthAccess?: IAuthTokens[];

        // @DataMember(Order=44)
        meta?: { [index:string]: string; };
    }

    interface IPoco
    {
        name?: string;
    }

    interface IEmptyInterface
    {
    }

    interface EmptyClass
    {
    }

    interface ImplementsPoco
    {
        name?: string;
    }

    interface TypeB
    {
        foo?: string;
    }

    interface TypeA
    {
        bar?: TypeB[];
    }

    interface InnerType
    {
        id?: number;
        name?: string;
    }

    type InnerEnum = "Foo" | "Bar" | "Baz";

    interface InnerTypeItem
    {
        id?: number;
        name?: string;
    }

    type DayOfWeek = "Sunday" | "Monday" | "Tuesday" | "Wednesday" | "Thursday" | "Friday" | "Saturday";

    // @DataContract
    type ScopeType = "Global" | "Sale";

    interface Tuple_2<T1, T2>
    {
        item1?: T1;
        item2?: T2;
    }

    interface Tuple_3<T1, T2, T3>
    {
        item1?: T1;
        item2?: T2;
        item3?: T3;
    }

    interface IEcho
    {
        sentence?: string;
    }

    type MyColor = "Red" | "Green" | "Blue";

    interface SwaggerNestedModel
    {
        /**
        * NestedProperty description
        */
        // @ApiMember(Description="NestedProperty description")
        nestedProperty?: boolean;
    }

    interface SwaggerNestedModel2
    {
        /**
        * NestedProperty2 description
        */
        // @ApiMember(Description="NestedProperty2 description")
        nestedProperty2?: boolean;

        /**
        * MultipleValues description
        */
        // @ApiMember(Description="MultipleValues description")
        multipleValues?: string;

        /**
        * TestRange description
        */
        // @ApiMember(Description="TestRange description")
        testRange?: number;
    }

    type MyEnum = "A" | "B" | "C";

    // @DataContract
    interface UserApiKey
    {
        // @DataMember(Order=1)
        key?: string;

        // @DataMember(Order=2)
        keyType?: string;

        // @DataMember(Order=3)
        expiryDate?: string;
    }

    interface PgRockstar extends Rockstar
    {
    }

    interface QueryDb_2<From, Into> extends QueryBase
    {
    }

    interface CustomRockstar
    {
        // @AutoQueryViewerField(Title="Name")
        firstName?: string;

        // @AutoQueryViewerField(HideInSummary=true)
        lastName?: string;

        age?: number;
        // @AutoQueryViewerField(Title="Album")
        rockstarAlbumName?: string;

        // @AutoQueryViewerField(Title="Genre")
        rockstarGenreName?: string;
    }

    interface IFilterRockstars
    {
    }

    interface Movie
    {
        id?: number;
        imdbId?: string;
        title?: string;
        rating?: string;
        score?: number;
        director?: string;
        releaseDate?: string;
        tagLine?: string;
        genres?: string[];
    }

    interface RockstarAlbum
    {
        id?: number;
        rockstarId?: number;
        name?: string;
    }

    interface RockstarReference
    {
        id?: number;
        firstName?: string;
        lastName?: string;
        age?: number;
        albums?: RockstarAlbum[];
    }

    interface OnlyDefinedInGenericType
    {
        id?: number;
        name?: string;
    }

    interface OnlyDefinedInGenericTypeFrom
    {
        id?: number;
        name?: string;
    }

    interface OnlyDefinedInGenericTypeInto
    {
        id?: number;
        name?: string;
    }

    interface TypesGroup
    {
    }

    // @DataContract
    interface QueryResponse<T>
    {
        // @DataMember(Order=1)
        offset?: number;

        // @DataMember(Order=2)
        total?: number;

        // @DataMember(Order=3)
        results?: T[];

        // @DataMember(Order=4)
        meta?: { [index:string]: string; };

        // @DataMember(Order=5)
        responseStatus?: ResponseStatus;
    }

    // @DataContract
    interface UpdateEventSubscriberResponse
    {
        // @DataMember(Order=1)
        responseStatus?: ResponseStatus;
    }

    interface ChangeRequestResponse
    {
        contentType?: string;
        header?: string;
        queryString?: string;
        form?: string;
        responseStatus?: ResponseStatus;
    }

    interface CustomHttpErrorResponse
    {
        custom?: string;
        responseStatus?: ResponseStatus;
    }

    // @Route("/alwaysthrows")
    interface AlwaysThrows extends IReturn<AlwaysThrows>
    {
    }

    // @Route("/alwaysthrowsfilterattribute")
    interface AlwaysThrowsFilterAttribute extends IReturn<AlwaysThrowsFilterAttribute>
    {
    }

    // @Route("/alwaysthrowsglobalfilter")
    interface AlwaysThrowsGlobalFilter extends IReturn<AlwaysThrowsGlobalFilter>
    {
    }

    interface CustomFieldHttpErrorResponse
    {
        custom?: string;
        responseStatus?: ResponseStatus;
    }

    interface NoRepeatResponse
    {
        id?: string;
    }

    interface BatchThrowsResponse
    {
        result?: string;
        responseStatus?: ResponseStatus;
    }

    interface ObjectDesignResponse
    {
        data?: ObjectDesign;
    }

    interface MetadataTestResponse
    {
        id?: number;
        results?: MetadataTestChild[];
    }

    // @DataContract
    interface GetExampleResponse
    {
        // @DataMember(Order=1)
        responseStatus?: ResponseStatus;

        // @DataMember(Order=2)
        // @ApiMember()
        menuExample1?: MenuExample;
    }

    interface AutoQueryMetadataResponse
    {
        config?: AutoQueryViewerConfig;
        userInfo?: AutoQueryViewerUserInfo;
        operations?: AutoQueryOperation[];
        types?: MetadataType[];
        responseStatus?: ResponseStatus;
        meta?: { [index:string]: string; };
    }

    // @DataContract
    interface HelloACodeGenTestResponse
    {
        /**
        * Description for FirstResult
        */
        // @DataMember
        firstResult?: number;

        /**
        * Description for SecondResult
        */
        // @DataMember
        // @ApiMember(Description="Description for SecondResult")
        secondResult?: number;
    }

    interface HelloResponse
    {
        result?: string;
    }

    /**
    * Description on HelloAllResponse type
    */
    // @DataContract
    interface HelloAnnotatedResponse
    {
        // @DataMember
        result?: string;
    }

    interface HelloList extends IReturn<Array<ListResult>>
    {
        names?: string[];
    }

    interface HelloArray extends IReturn<Array<ArrayResult>>
    {
        names?: string[];
    }

    interface HelloExistingResponse
    {
        helloList?: HelloList;
        helloArray?: HelloArray;
        arrayResults?: ArrayResult[];
        listResults?: ListResult[];
    }

    interface AllTypes extends IReturn<AllTypes>
    {
        id?: number;
        nullableId?: number;
        byte?: number;
        short?: number;
        int?: number;
        long?: number;
        uShort?: number;
        uInt?: number;
        uLong?: number;
        float?: number;
        double?: number;
        decimal?: number;
        string?: string;
        dateTime?: string;
        timeSpan?: string;
        dateTimeOffset?: string;
        guid?: string;
        char?: string;
        keyValuePair?: KeyValuePair<string, string>;
        nullableDateTime?: string;
        nullableTimeSpan?: string;
        stringList?: string[];
        stringArray?: string[];
        stringMap?: { [index:string]: string; };
        intStringMap?: { [index:number]: string; };
        subType?: SubType;
        point?: string;
        // @DataMember(Name="aliasedName")
        originalName?: string;
    }

    interface HelloAllTypesResponse
    {
        result?: string;
        allTypes?: AllTypes;
        allCollectionTypes?: AllCollectionTypes;
    }

    // @DataContract
    interface HelloWithDataContractResponse
    {
        // @DataMember(Name="result", Order=1, IsRequired=true, EmitDefaultValue=false)
        result?: string;
    }

    /**
    * Description on HelloWithDescriptionResponse type
    */
    interface HelloWithDescriptionResponse
    {
        result?: string;
    }

    interface HelloWithInheritanceResponse extends HelloResponseBase
    {
        result?: string;
    }

    interface HelloWithAlternateReturnResponse extends HelloWithReturnResponse
    {
        altResult?: string;
    }

    interface HelloWithRouteResponse
    {
        result?: string;
    }

    interface HelloWithTypeResponse
    {
        result?: HelloType;
    }

    interface HelloStruct extends IReturn<HelloStruct>
    {
        point?: string;
        nullablePoint?: string;
    }

    interface HelloSessionResponse
    {
        result?: AuthUserSession;
    }

    interface HelloImplementsInterface extends IReturn<HelloImplementsInterface>, ImplementsPoco
    {
        name?: string;
    }

    interface Request1Response
    {
        test?: TypeA;
    }

    interface Request2Response
    {
        test?: TypeA;
    }

    interface HelloInnerTypesResponse
    {
        innerType?: InnerType;
        innerEnum?: InnerEnum;
        innerList?: InnerTypeItem[];
    }

    interface CustomUserSession extends AuthUserSession
    {
        // @DataMember
        customName?: string;

        // @DataMember
        customInfo?: string;
    }

    // @DataContract
    interface QueryResponseTemplate<T>
    {
        // @DataMember(Order=1)
        offset?: number;

        // @DataMember(Order=2)
        total?: number;

        // @DataMember(Order=3)
        results?: T[];

        // @DataMember(Order=4)
        meta?: { [index:string]: string; };

        // @DataMember(Order=5)
        responseStatus?: ResponseStatus;
    }

    interface HelloVerbResponse
    {
        result?: string;
    }

    interface EnumResponse
    {
        operator?: ScopeType;
    }

    interface ExcludeTestNested
    {
        id?: number;
    }

    interface RestrictLocalhost extends IReturn<RestrictLocalhost>
    {
        id?: number;
    }

    interface RestrictInternal extends IReturn<RestrictInternal>
    {
        id?: number;
    }

    interface HelloTuple extends IReturn<HelloTuple>
    {
        tuple2?: Tuple_2<string, number>;
        tuple3?: Tuple_3<string, number, boolean>;
        tuples2?: Tuple_2<string,number>[];
        tuples3?: Tuple_3<string,number,boolean>[];
    }

    interface HelloAuthenticatedResponse
    {
        version?: number;
        sessionId?: string;
        userName?: string;
        email?: string;
        isAuthenticated?: boolean;
        responseStatus?: ResponseStatus;
    }

    interface Echo
    {
        sentence?: string;
    }

    interface ThrowHttpErrorResponse
    {
    }

    interface ThrowTypeResponse
    {
        responseStatus?: ResponseStatus;
    }

    interface ThrowValidationResponse
    {
        age?: number;
        required?: string;
        email?: string;
        responseStatus?: ResponseStatus;
    }

    interface acsprofileResponse
    {
        profileId?: string;
    }

    interface ReturnedDto
    {
        id?: number;
    }

    // @Route("/matchroute/html")
    interface MatchesHtml extends IReturn<MatchesHtml>
    {
        name?: string;
    }

    // @Route("/matchroute/json")
    interface MatchesJson extends IReturn<MatchesJson>
    {
        name?: string;
    }

    interface TimestampData
    {
        timestamp?: number;
    }

    // @Route("/test/html")
    interface TestHtml extends IReturn<TestHtml>
    {
        name?: string;
    }

    interface SwaggerComplexResponse
    {
        // @DataMember
        // @ApiMember()
        isRequired?: boolean;

        // @DataMember
        // @ApiMember(IsRequired=true)
        arrayString?: string[];

        // @DataMember
        // @ApiMember()
        arrayInt?: number[];

        // @DataMember
        // @ApiMember()
        listString?: string[];

        // @DataMember
        // @ApiMember()
        listInt?: number[];

        // @DataMember
        // @ApiMember()
        dictionaryString?: { [index:string]: string; };
    }

    /**
    * Api GET All
    */
    // @Route("/swaggerexamples", "GET")
    // @Api(Description="Api GET All")
    interface GetSwaggerExamples extends IReturn<GetSwaggerExamples>
    {
        get?: string;
    }

    /**
    * Api GET Id
    */
    // @Route("/swaggerexamples/{Id}", "GET")
    // @Api(Description="Api GET Id")
    interface GetSwaggerExample extends IReturn<GetSwaggerExample>
    {
        id?: number;
        get?: string;
    }

    /**
    * Api POST
    */
    // @Route("/swaggerexamples", "POST")
    // @Api(Description="Api POST")
    interface PostSwaggerExamples extends IReturn<PostSwaggerExamples>
    {
        post?: string;
    }

    /**
    * Api PUT Id
    */
    // @Route("/swaggerexamples/{Id}", "PUT")
    // @Api(Description="Api PUT Id")
    interface PutSwaggerExample extends IReturn<PutSwaggerExample>
    {
        id?: number;
        get?: string;
    }

    // @Route("/lists", "GET")
    interface GetLists extends IReturn<GetLists>
    {
        id?: string;
    }

    // @DataContract
    interface AuthenticateResponse
    {
        // @DataMember(Order=1)
        userId?: string;

        // @DataMember(Order=2)
        sessionId?: string;

        // @DataMember(Order=3)
        userName?: string;

        // @DataMember(Order=4)
        displayName?: string;

        // @DataMember(Order=5)
        referrerUrl?: string;

        // @DataMember(Order=6)
        bearerToken?: string;

        // @DataMember(Order=7)
        refreshToken?: string;

        // @DataMember(Order=8)
        responseStatus?: ResponseStatus;

        // @DataMember(Order=9)
        meta?: { [index:string]: string; };
    }

    // @DataContract
    interface AssignRolesResponse
    {
        // @DataMember(Order=1)
        allRoles?: string[];

        // @DataMember(Order=2)
        allPermissions?: string[];

        // @DataMember(Order=3)
        responseStatus?: ResponseStatus;
    }

    // @DataContract
    interface UnAssignRolesResponse
    {
        // @DataMember(Order=1)
        allRoles?: string[];

        // @DataMember(Order=2)
        allPermissions?: string[];

        // @DataMember(Order=3)
        responseStatus?: ResponseStatus;
    }

    // @DataContract
    interface GetApiKeysResponse
    {
        // @DataMember(Order=1)
        results?: UserApiKey[];

        // @DataMember(Order=2)
        responseStatus?: ResponseStatus;
    }

    // @DataContract
    interface RegisterResponse
    {
        // @DataMember(Order=1)
        userId?: string;

        // @DataMember(Order=2)
        sessionId?: string;

        // @DataMember(Order=3)
        userName?: string;

        // @DataMember(Order=4)
        referrerUrl?: string;

        // @DataMember(Order=5)
        bearerToken?: string;

        // @DataMember(Order=6)
        refreshToken?: string;

        // @DataMember(Order=7)
        responseStatus?: ResponseStatus;

        // @DataMember(Order=8)
        meta?: { [index:string]: string; };
    }

    // @Route("/anontype")
    interface AnonType
    {
    }

    // @Route("/query/requestlogs")
    // @Route("/query/requestlogs/{Date}")
    interface QueryRequestLogs extends QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
        date?: string;
        viewErrors?: boolean;
    }

    interface TodayLogs extends QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    interface TodayErrorLogs extends QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    interface YesterdayLogs extends QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    interface YesterdayErrorLogs extends QueryData<RequestLogEntry>, IReturn<QueryResponse<RequestLogEntry>>, IMeta
    {
    }

    // @Route("/query/rockstars")
    interface QueryRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
    }

    interface GetEventSubscribers extends IReturn<any>, IGet
    {
        channels?: string[];
    }

    // @Route("/event-subscribers/{Id}", "POST")
    // @DataContract
    interface UpdateEventSubscriber extends IReturn<UpdateEventSubscriberResponse>, IPost
    {
        // @DataMember(Order=1)
        id?: string;

        // @DataMember(Order=2)
        subscribeChannels?: string[];

        // @DataMember(Order=3)
        unsubscribeChannels?: string[];
    }

    // @Route("/changerequest/{Id}")
    interface ChangeRequest extends IReturn<ChangeRequestResponse>
    {
        id?: string;
    }

    // @Route("/compress/{Path*}")
    interface CompressFile
    {
        path?: string;
    }

    // @Route("/Routing/LeadPost.aspx")
    interface LegacyLeadPost
    {
        leadType?: string;
        myId?: number;
    }

    // @Route("/info/{Id}")
    interface Info
    {
        id?: string;
    }

    interface CustomHttpError extends IReturn<CustomHttpErrorResponse>
    {
        statusCode?: number;
        statusDescription?: string;
    }

    interface CustomFieldHttpError extends IReturn<CustomFieldHttpErrorResponse>
    {
    }

    interface FallbackRoute
    {
        pathInfo?: string;
    }

    interface NoRepeat extends IReturn<NoRepeatResponse>
    {
        id?: string;
    }

    interface BatchThrows extends IReturn<BatchThrowsResponse>
    {
        id?: number;
        name?: string;
    }

    interface BatchThrowsAsync extends IReturn<BatchThrowsResponse>
    {
        id?: number;
        name?: string;
    }

    // @Route("/code/object", "GET")
    interface ObjectId extends IReturn<ObjectDesignResponse>
    {
        objectName?: string;
    }

    interface MetadataTest extends IReturn<MetadataTestResponse>
    {
        id?: number;
    }

    // @Route("/example", "GET")
    // @DataContract
    interface GetExample extends IReturn<GetExampleResponse>
    {
    }

    interface MetadataRequest extends IReturn<AutoQueryMetadataResponse>
    {
        metadataType?: MetadataType;
    }

    interface ExcludeMetadataProperty
    {
        id?: number;
    }

    // @Route("/namedconnection")
    interface NamedConnection
    {
        emailAddresses?: string;
    }

    /**
    * Description for HelloACodeGenTest
    */
    interface HelloACodeGenTest extends IReturn<HelloACodeGenTestResponse>
    {
        /**
        * Description for FirstField
        */
        firstField?: number;
        secondFields?: string[];
    }

    interface HelloInService extends IReturn<HelloResponse>
    {
        name?: string;
    }

    // @Route("/hello")
    // @Route("/hello/{Name}")
    interface Hello extends IReturn<HelloResponse>
    {
        // @Required()
        name: string;

        title?: string;
    }

    /**
    * Description on HelloAll type
    */
    // @DataContract
    interface HelloAnnotated extends IReturn<HelloAnnotatedResponse>
    {
        // @DataMember
        name?: string;
    }

    interface HelloWithNestedClass extends IReturn<HelloResponse>
    {
        name?: string;
        nestedClassProp?: NestedClass;
    }

    interface HelloReturnList extends IReturn<Array<OnlyInReturnListArg>>
    {
        names?: string[];
    }

    interface HelloExisting extends IReturn<HelloExistingResponse>
    {
        names?: string[];
    }

    interface HelloWithEnum
    {
        enumProp?: EnumType;
        enumWithValues?: EnumWithValues;
        nullableEnumProp?: EnumType;
        enumFlags?: EnumFlags;
    }

    interface RestrictedAttributes
    {
        id?: number;
        name?: string;
        hello?: Hello;
    }

    /**
    * AllowedAttributes Description
    */
    // @Route("/allowed-attributes", "GET")
    // @Api(Description="AllowedAttributes Description")
    // @ApiResponse(Description="Your request was not understood", StatusCode=400)
    // @DataContract
    interface AllowedAttributes
    {
        // @DataMember
        // @Required()
        id: number;

        /**
        * Range Description
        */
        // @DataMember(Name="Aliased")
        // @ApiMember(DataType="double", Description="Range Description", IsRequired=true, ParameterType="path")
        range?: number;
    }

    /**
    * Multi Line Class
    */
    // @Api(Description="Multi Line Class")
    interface HelloMultiline
    {
        /**
        * Multi Line Property
        */
        // @ApiMember(Description="Multi Line Property")
        overflow?: string;
    }

    interface HelloAllTypes extends IReturn<HelloAllTypesResponse>
    {
        name?: string;
        allTypes?: AllTypes;
        allCollectionTypes?: AllCollectionTypes;
    }

    interface HelloString extends IReturn<string>
    {
        name?: string;
    }

    interface HelloVoid extends IReturnVoid
    {
        name?: string;
    }

    // @DataContract
    interface HelloWithDataContract extends IReturn<HelloWithDataContractResponse>
    {
        // @DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)
        name?: string;

        // @DataMember(Name="id", Order=2, EmitDefaultValue=false)
        id?: number;
    }

    /**
    * Description on HelloWithDescription type
    */
    interface HelloWithDescription extends IReturn<HelloWithDescriptionResponse>
    {
        name?: string;
    }

    interface HelloWithInheritance extends HelloBase, IReturn<HelloWithInheritanceResponse>
    {
        name?: string;
    }

    interface HelloWithGenericInheritance extends HelloBase_1<Poco>
    {
        result?: string;
    }

    interface HelloWithGenericInheritance2 extends HelloBase_1<Hello>
    {
        result?: string;
    }

    interface HelloWithNestedInheritance extends HelloBase_1<Item>
    {
    }

    interface HelloWithListInheritance extends Array<InheritedItem>
    {
    }

    interface HelloWithReturn extends IReturn<HelloWithAlternateReturnResponse>
    {
        name?: string;
    }

    // @Route("/helloroute")
    interface HelloWithRoute extends IReturn<HelloWithRouteResponse>
    {
        name?: string;
    }

    interface HelloWithType extends IReturn<HelloWithTypeResponse>
    {
        name?: string;
    }

    interface HelloSession extends IReturn<HelloSessionResponse>
    {
    }

    interface HelloInterface
    {
        poco?: IPoco;
        emptyInterface?: IEmptyInterface;
        emptyClass?: EmptyClass;
        value?: string;
    }

    interface Request1 extends IReturn<Request1Response>
    {
        test?: TypeA;
    }

    interface Request2 extends IReturn<Request2Response>
    {
        test?: TypeA;
    }

    interface HelloInnerTypes extends IReturn<HelloInnerTypesResponse>
    {
    }

    interface GetUserSession extends IReturn<CustomUserSession>
    {
    }

    interface QueryTemplate extends IReturn<QueryResponseTemplate<Poco>>
    {
    }

    interface HelloReserved
    {
        class?: string;
        type?: string;
        extension?: string;
    }

    interface HelloDictionary extends IReturn<any>
    {
        key?: string;
        value?: string;
    }

    interface HelloBuiltin
    {
        dayOfWeek?: DayOfWeek;
    }

    interface HelloGet extends IReturn<HelloVerbResponse>, IGet
    {
        id?: number;
    }

    interface HelloPost extends HelloBase, IReturn<HelloVerbResponse>, IPost
    {
    }

    interface HelloPut extends IReturn<HelloVerbResponse>, IPut
    {
        id?: number;
    }

    interface HelloDelete extends IReturn<HelloVerbResponse>, IDelete
    {
        id?: number;
    }

    interface HelloPatch extends IReturn<HelloVerbResponse>, IPatch
    {
        id?: number;
    }

    interface HelloReturnVoid extends IReturnVoid
    {
        id?: number;
    }

    interface EnumRequest extends IReturn<EnumResponse>, IPut
    {
        operator?: ScopeType;
    }

    interface ExcludeTest1 extends IReturn<ExcludeTestNested>
    {
    }

    interface ExcludeTest2 extends IReturn<string>
    {
        excludeTestNested?: ExcludeTestNested;
    }

    interface HelloAuthenticated extends IReturn<HelloAuthenticatedResponse>, IHasSessionId
    {
        sessionId?: string;
        version?: number;
    }

    /**
    * Echoes a sentence
    */
    // @Route("/echoes", "POST")
    // @Api(Description="Echoes a sentence")
    interface Echoes extends IReturn<Echo>
    {
        /**
        * The sentence to echo.
        */
        // @ApiMember(DataType="string", Description="The sentence to echo.", IsRequired=true, Name="Sentence", ParameterType="form")
        sentence?: string;
    }

    interface CachedEcho extends IReturn<Echo>
    {
        reload?: boolean;
        sentence?: string;
    }

    interface AsyncTest extends IReturn<Echo>
    {
    }

    // @Route("/throwhttperror/{Status}")
    interface ThrowHttpError extends IReturn<ThrowHttpErrorResponse>
    {
        status?: number;
        message?: string;
    }

    // @Route("/throw404")
    // @Route("/throw404/{Message}")
    interface Throw404
    {
        message?: string;
    }

    // @Route("/return404")
    interface Return404
    {
    }

    // @Route("/return404result")
    interface Return404Result
    {
    }

    // @Route("/throw/{Type}")
    interface ThrowType extends IReturn<ThrowTypeResponse>
    {
        type?: string;
        message?: string;
    }

    // @Route("/throwvalidation")
    interface ThrowValidation extends IReturn<ThrowValidationResponse>
    {
        age?: number;
        required?: string;
        email?: string;
    }

    // @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
    // @Route("/api/acsprofiles/{profileId}")
    interface ACSProfile extends IReturn<acsprofileResponse>, IHasVersion, IHasSessionId
    {
        profileId?: string;
        // @Required()
        // @StringLength(20)
        shortName: string;

        // @StringLength(60)
        longName?: string;

        // @StringLength(20)
        regionId?: string;

        // @StringLength(20)
        groupId?: string;

        // @StringLength(12)
        deviceID?: string;

        lastUpdated?: string;
        enabled?: boolean;
        version?: number;
        sessionId?: string;
    }

    // @Route("/return/string")
    interface ReturnString extends IReturn<string>
    {
        data?: string;
    }

    // @Route("/return/bytes")
    interface ReturnBytes extends IReturn<Uint8Array>
    {
        data?: Uint8Array;
    }

    // @Route("/return/stream")
    interface ReturnStream extends IReturn<Blob>
    {
        data?: Uint8Array;
    }

    // @Route("/Request1/", "GET")
    interface GetRequest1 extends IReturn<Array<ReturnedDto>>, IGet
    {
    }

    // @Route("/Request3", "GET")
    interface GetRequest2 extends IReturn<ReturnedDto>, IGet
    {
    }

    // @Route("/matchlast/{Id}")
    interface MatchesLastInt
    {
        id?: number;
    }

    // @Route("/matchlast/{Slug}")
    interface MatchesNotLastInt
    {
        slug?: string;
    }

    // @Route("/matchregex/{Id}")
    interface MatchesId
    {
        id?: number;
    }

    // @Route("/matchregex/{Slug}")
    interface MatchesSlug
    {
        slug?: string;
    }

    // @Route("/{Version}/userdata", "GET")
    interface SwaggerVersionTest
    {
        version?: string;
    }

    // @Route("/test/errorview")
    interface TestErrorView
    {
        id?: string;
    }

    // @Route("/timestamp", "GET")
    interface GetTimestamp extends IReturn<TimestampData>
    {
    }

    interface TestMiniverView
    {
    }

    // @Route("/testexecproc")
    interface TestExecProc
    {
    }

    // @Route("/files/{Path*}")
    interface GetFile
    {
        path?: string;
    }

    // @Route("/test/html2")
    interface TestHtml2
    {
        name?: string;
    }

    // @Route("/views/request")
    interface ViewRequest
    {
        name?: string;
    }

    // @Route("/index")
    interface IndexPage
    {
        pathInfo?: string;
    }

    // @Route("/return/text")
    interface ReturnText
    {
        text?: string;
    }

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
    interface SwaggerTest
    {
        /**
        * Color Description
        */
        // @DataMember
        // @ApiMember(DataType="string", Description="Color Description", IsRequired=true, ParameterType="path")
        name?: string;

        // @DataMember
        // @ApiMember()
        color?: MyColor;

        /**
        * Aliased Description
        */
        // @DataMember(Name="Aliased")
        // @ApiMember(DataType="string", Description="Aliased Description", IsRequired=true)
        original?: string;

        /**
        * Not Aliased Description
        */
        // @DataMember
        // @ApiMember(DataType="string", Description="Not Aliased Description", IsRequired=true)
        notAliased?: string;

        /**
        * Format as password
        */
        // @DataMember
        // @ApiMember(DataType="password", Description="Format as password")
        password?: string;

        // @DataMember
        // @ApiMember(AllowMultiple=true)
        myDateBetween?: string[];

        /**
        * Nested model 1
        */
        // @DataMember
        // @ApiMember(DataType="SwaggerNestedModel", Description="Nested model 1")
        nestedModel1?: SwaggerNestedModel;

        /**
        * Nested model 2
        */
        // @DataMember
        // @ApiMember(DataType="SwaggerNestedModel2", Description="Nested model 2")
        nestedModel2?: SwaggerNestedModel2;
    }

    // @Route("/swaggertest2", "POST")
    interface SwaggerTest2
    {
        // @ApiMember()
        myEnumProperty?: MyEnum;

        // @ApiMember(DataType="string", IsRequired=true, Name="Token", ParameterType="header")
        token?: string;
    }

    // @Route("/swagger-complex", "POST")
    interface SwaggerComplex extends IReturn<SwaggerComplexResponse>
    {
        // @DataMember
        // @ApiMember()
        isRequired?: boolean;

        // @DataMember
        // @ApiMember(IsRequired=true)
        arrayString?: string[];

        // @DataMember
        // @ApiMember()
        arrayInt?: number[];

        // @DataMember
        // @ApiMember()
        listString?: string[];

        // @DataMember
        // @ApiMember()
        listInt?: number[];

        // @DataMember
        // @ApiMember()
        dictionaryString?: { [index:string]: string; };
    }

    // @Route("/swaggerpost/{Required1}", "GET")
    // @Route("/swaggerpost/{Required1}/{Optional1}", "GET")
    // @Route("/swaggerpost", "POST")
    interface SwaggerPostTest extends IReturn<HelloResponse>
    {
        // @ApiMember(Verb="POST")
        // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}", Verb="GET")
        // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")
        required1?: string;

        // @ApiMember(Verb="POST")
        // @ApiMember(ParameterType="path", Route="/swaggerpost/{Required1}/{Optional1}", Verb="GET")
        optional1?: string;
    }

    // @Route("/swaggerpost2/{Required1}/{Required2}", "GET")
    // @Route("/swaggerpost2/{Required1}/{Required2}/{Optional1}", "GET")
    // @Route("/swaggerpost2", "POST")
    interface SwaggerPostTest2 extends IReturn<HelloResponse>
    {
        // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")
        // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
        required1?: string;

        // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}", Verb="GET")
        // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
        required2?: string;

        // @ApiMember(ParameterType="path", Route="/swaggerpost2/{Required1}/{Required2}/{Optional1}", Verb="GET")
        optional1?: string;
    }

    // @Route("/swagger/multiattrtest", "POST")
    // @ApiResponse(Description="Code 1", StatusCode=400)
    // @ApiResponse(Description="Code 2", StatusCode=402)
    // @ApiResponse(Description="Code 3", StatusCode=401)
    interface SwaggerMultiApiResponseTest extends IReturnVoid
    {
    }

    // @Route("/dynamically/registered/{Name}")
    interface DynamicallyRegistered
    {
        name?: string;
    }

    // @Route("/auth")
    // @Route("/auth/{provider}")
    // @Route("/authenticate")
    // @Route("/authenticate/{provider}")
    // @DataContract
    interface Authenticate extends IReturn<AuthenticateResponse>, IPost, IMeta
    {
        // @DataMember(Order=1)
        provider?: string;

        // @DataMember(Order=2)
        state?: string;

        // @DataMember(Order=3)
        oauth_token?: string;

        // @DataMember(Order=4)
        oauth_verifier?: string;

        // @DataMember(Order=5)
        userName?: string;

        // @DataMember(Order=6)
        password?: string;

        // @DataMember(Order=7)
        rememberMe?: boolean;

        // @DataMember(Order=8)
        continue?: string;

        // @DataMember(Order=9)
        nonce?: string;

        // @DataMember(Order=10)
        uri?: string;

        // @DataMember(Order=11)
        response?: string;

        // @DataMember(Order=12)
        qop?: string;

        // @DataMember(Order=13)
        nc?: string;

        // @DataMember(Order=14)
        cnonce?: string;

        // @DataMember(Order=15)
        useTokenCookie?: boolean;

        // @DataMember(Order=16)
        accessToken?: string;

        // @DataMember(Order=17)
        accessTokenSecret?: string;

        // @DataMember(Order=18)
        meta?: { [index:string]: string; };
    }

    // @Route("/assignroles")
    // @DataContract
    interface AssignRoles extends IReturn<AssignRolesResponse>, IPost
    {
        // @DataMember(Order=1)
        userName?: string;

        // @DataMember(Order=2)
        permissions?: string[];

        // @DataMember(Order=3)
        roles?: string[];
    }

    // @Route("/unassignroles")
    // @DataContract
    interface UnAssignRoles extends IReturn<UnAssignRolesResponse>, IPost
    {
        // @DataMember(Order=1)
        userName?: string;

        // @DataMember(Order=2)
        permissions?: string[];

        // @DataMember(Order=3)
        roles?: string[];
    }

    // @Route("/apikeys")
    // @Route("/apikeys/{Environment}")
    // @DataContract
    interface GetApiKeys extends IReturn<GetApiKeysResponse>, IGet
    {
        // @DataMember(Order=1)
        environment?: string;
    }

    // @Route("/apikeys/regenerate")
    // @Route("/apikeys/regenerate/{Environment}")
    // @DataContract
    interface RegenerateApiKeys extends IReturn<GetApiKeysResponse>, IPost
    {
        // @DataMember(Order=1)
        environment?: string;
    }

    // @Route("/register")
    // @DataContract
    interface Register extends IReturn<RegisterResponse>, IPost
    {
        // @DataMember(Order=1)
        userName?: string;

        // @DataMember(Order=2)
        firstName?: string;

        // @DataMember(Order=3)
        lastName?: string;

        // @DataMember(Order=4)
        displayName?: string;

        // @DataMember(Order=5)
        email?: string;

        // @DataMember(Order=6)
        password?: string;

        // @DataMember(Order=7)
        autoLogin?: boolean;

        // @DataMember(Order=8)
        continue?: string;
    }

    // @Route("/pgsql/rockstars")
    interface QueryPostgresRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
    }

    // @Route("/pgsql/pgrockstars")
    interface QueryPostgresPgRockstars extends QueryDb_1<PgRockstar>, IReturn<QueryResponse<PgRockstar>>, IMeta
    {
        age?: number;
    }

    interface QueryRockstarsConventions extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        ids?: number[];
        ageOlderThan?: number;
        ageGreaterThanOrEqualTo?: number;
        ageGreaterThan?: number;
        greaterThanAge?: number;
        firstNameStartsWith?: string;
        lastNameEndsWith?: string;
        lastNameContains?: string;
        rockstarAlbumNameContains?: string;
        rockstarIdAfter?: number;
        rockstarIdOnOrAfter?: number;
    }

    // @AutoQueryViewer(Description="Use this option to search for Rockstars!", Title="Search for Rockstars")
    interface QueryCustomRockstars extends QueryDb_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        age?: number;
    }

    // @Route("/customrockstars")
    interface QueryRockstarAlbums extends QueryDb_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        age?: number;
        rockstarAlbumName?: string;
    }

    interface QueryRockstarAlbumsImplicit extends QueryDb_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
    }

    interface QueryRockstarAlbumsLeftJoin extends QueryDb_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        age?: number;
        albumName?: string;
    }

    interface QueryOverridedRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
    }

    interface QueryOverridedCustomRockstars extends QueryDb_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        age?: number;
    }

    // @Route("/query-custom/rockstars")
    interface QueryFieldRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        firstName?: string;
        firstNames?: string[];
        age?: number;
        firstNameCaseInsensitive?: string;
        firstNameStartsWith?: string;
        lastNameEndsWith?: string;
        firstNameBetween?: string[];
        orLastName?: string;
        firstNameContainsMulti?: string[];
    }

    interface QueryFieldRockstarsDynamic extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
    }

    interface QueryRockstarsFilter extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
    }

    interface QueryCustomRockstarsFilter extends QueryDb_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>, IMeta
    {
        age?: number;
    }

    interface QueryRockstarsIFilter extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta, IFilterRockstars
    {
        age?: number;
    }

    // @Route("/OrRockstars")
    interface QueryOrRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
        firstName?: string;
    }

    interface QueryGetRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        ids?: number[];
        ages?: number[];
        firstNames?: string[];
        idsBetween?: number[];
    }

    interface QueryGetRockstarsDynamic extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
    }

    // @Route("/movies/search")
    interface SearchMovies extends QueryDb_1<Movie>, IReturn<QueryResponse<Movie>>, IMeta
    {
    }

    // @Route("/movies")
    interface QueryMovies extends QueryDb_1<Movie>, IReturn<QueryResponse<Movie>>, IMeta
    {
        ids?: number[];
        imdbIds?: string[];
        ratings?: string[];
    }

    interface StreamMovies extends QueryDb_1<Movie>, IReturn<QueryResponse<Movie>>, IMeta
    {
        ratings?: string[];
    }

    interface QueryUnknownRockstars extends QueryDb_1<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        unknownInt?: number;
        unknownProperty?: string;
    }

    // @Route("/query/rockstar-references")
    interface QueryRockstarsWithReferences extends QueryDb_1<RockstarReference>, IReturn<QueryResponse<RockstarReference>>, IMeta
    {
        age?: number;
    }

    interface QueryPocoBase extends QueryDb_1<OnlyDefinedInGenericType>, IReturn<QueryResponse<OnlyDefinedInGenericType>>, IMeta
    {
        id?: number;
    }

    interface QueryPocoIntoBase extends QueryDb_2<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto>, IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>, IMeta
    {
        id?: number;
    }

    // @Route("/query/alltypes")
    interface QueryAllTypes extends QueryDb_1<AllTypes>, IReturn<QueryResponse<AllTypes>>, IMeta
    {
    }

    // @Route("/querydata/rockstars")
    interface QueryDataRockstars extends QueryData<Rockstar>, IReturn<QueryResponse<Rockstar>>, IMeta
    {
        age?: number;
    }

}
