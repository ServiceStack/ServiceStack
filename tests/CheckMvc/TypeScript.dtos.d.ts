/* Options:
Date: 2015-11-23 10:40:47
Version: 4.00
BaseUrl: http://localhost:55799

GlobalNamespace: dtos
//ExportAsTypes: False
//MakePropertiesOptional: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/


declare module dtos
{

    interface IReturnVoid
    {
    }

    interface IReturn<T>
    {
    }

    interface QueryBase_1<T> extends QueryBase
    {
    }

    interface Rockstar
    {
        id?: number;
        firstName?: string;
        lastName?: string;
        age?: number;
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

    interface MetadataTestChild
    {
        name?: string;
        results?: MetadataTestNestedChild[];
    }

    // @DataContract
    interface MenuExample
    {
        // @DataMember(Order=1)
        // @ApiMember()
        menuItemExample1?: MenuItemExample;
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
    }

    interface AutoQueryViewerConfig
    {
        serviceBaseUrl?: string;
        serviceName?: string;
        serviceDescription?: string;
        serviceIconUrl?: string;
        isPublic?: boolean;
        onlyShowAnnotatedServices?: boolean;
        implicitConventions?: Property[];
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
    }

    interface AutoQueryOperation
    {
        request?: string;
        from?: string;
        to?: string;
    }

    interface Issue221Base<T>
    {
        id?: T;
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

    enum EnumType
    {
        value1,
        value2,
    }

    enum EnumWithValues
    {
        value1 = 1,
        value2 = 2,
    }

    // @Flags()
    enum EnumFlags
    {
        value1 = 1,
        value2 = 2,
        value3 = 4,
    }

    interface AllCollectionTypes
    {
        intArray?: number[];
        intList?: number[];
        stringArray?: string[];
        stringList?: string[];
        pocoArray?: Poco[];
        pocoList?: Poco[];
        pocoLookup?: { [index:string]: Poco[]; };
        pocoLookupMap?: { [index:string]: { [index:string]: Poco; }[]; };
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

    interface Poco
    {
        name?: string;
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
        sequence?: string;

        // @DataMember(Order=39)
        tag?: number;

        // @DataMember(Order=40)
        providerOAuthAccess?: IAuthTokens[];
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

    interface TypeA
    {
        bar?: TypeB[];
    }

    interface InnerType
    {
        id?: number;
        name?: string;
    }

    enum InnerEnum
    {
        foo,
        bar,
        baz,
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

    enum DayOfWeek
    {
        sunday,
        monday,
        tuesday,
        wednesday,
        thursday,
        friday,
        saturday,
    }

    // @DataContract
    enum ScopeType
    {
        global = 1,
        sale = 2,
    }

    interface QueryBase_2<From, Into> extends QueryBase
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
        meta?: { [index:string]: string; };
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

    interface MetadataTestNestedChild
    {
        name?: string;
    }

    interface MenuItemExample
    {
        // @DataMember(Order=1)
        // @ApiMember()
        name1?: string;

        menuItemExampleItem?: MenuItemExampleItem;
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

    interface MetadataPropertyType
    {
        name?: string;
        type?: string;
        isValueType?: boolean;
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

    interface MetadataAttribute
    {
        name?: string;
        constructorArgs?: MetadataPropertyType[];
        args?: MetadataPropertyType[];
    }

    // @DataContract
    interface Property
    {
        // @DataMember
        name?: string;

        // @DataMember
        value?: string;
    }

    interface TypeB
    {
        foo?: string;
    }

    interface TypesGroup
    {
    }

    interface RockstarAlbum
    {
        id?: number;
        rockstarId?: number;
        name?: string;
    }

    interface MenuItemExampleItem
    {
        // @DataMember(Order=1)
        // @ApiMember()
        name1?: string;
    }

    interface MetadataDataMember
    {
        name?: string;
        order?: number;
        isRequired?: boolean;
        emitDefaultValue?: boolean;
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
        operations?: AutoQueryOperation[];
        types?: MetadataType[];
        responseStatus?: ResponseStatus;
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

    interface HelloExistingResponse
    {
        helloList?: HelloList;
        helloArray?: HelloArray;
        arrayResults?: ArrayResult[];
        listResults?: ListResult[];
    }

    interface HelloAllTypesResponse
    {
        result?: string;
        allTypes?: AllTypes;
        allCollectionTypes?: AllCollectionTypes;
    }

    interface AllTypes
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
        nullableDateTime?: string;
        nullableTimeSpan?: string;
        stringList?: string[];
        stringArray?: string[];
        stringMap?: { [index:string]: string; };
        intStringMap?: { [index:number]: string; };
        subType?: SubType;
        // @DataMember(Name="aliasedName")
        originalName?: string;
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

    interface HelloSessionResponse
    {
        result?: AuthUserSession;
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

    interface acsprofileResponse
    {
        profileId?: string;
    }

    // @Route("/anontype")
    interface AnonType
    {
    }

    // @Route("/query/rockstars")
    interface QueryRockstars extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        age?: number;
    }

    // @Route("/changerequest/{Id}")
    interface ChangeRequest extends IReturn<ChangeRequestResponse>
    {
        id?: string;
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

    // @Route("{PathInfo*}")
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

    // @Route("/namedconnection")
    interface NamedConnection
    {
        emailAddresses?: string;
    }

    interface Issue221Long extends Issue221Base<number>
    {
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

    interface HelloList extends IReturn<Array<ListResult>>
    {
        names?: string[];
    }

    interface HelloReturnList extends IReturn<Array<OnlyInReturnListArg>>
    {
        names?: string[];
    }

    interface HelloArray extends IReturn<Array<ArrayResult>>
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
    // @Api("AllowedAttributes Description")
    // @ApiResponse(400, "Your request was not understood")
    // @DataContract
    interface AllowedAttributes
    {
        // @DataMember(Name="Aliased")
        // @ApiMember(Description="Range Description", ParameterType="path", DataType="double", IsRequired=true)
        range?: number;
    }

    /**
    * Multi Line Class
    */
    // @Api("Multi Line Class")
    interface HelloMultiline
    {
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

    interface HelloGet extends IReturn<HelloVerbResponse>
    {
        id?: number;
    }

    interface HelloPost extends HelloBase, IReturn<HelloVerbResponse>
    {
    }

    interface HelloPut extends IReturn<HelloVerbResponse>
    {
        id?: number;
    }

    interface HelloDelete extends IReturn<HelloVerbResponse>
    {
        id?: number;
    }

    interface HelloPatch extends IReturn<HelloVerbResponse>
    {
        id?: number;
    }

    interface HelloReturnVoid extends IReturnVoid
    {
        id?: number;
    }

    interface EnumRequest extends IReturn<EnumResponse>
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

    /**
    * Echoes a sentence
    */
    // @Route("/echoes", "POST")
    // @Api("Echoes a sentence")
    interface Echoes extends IReturn<Echo>
    {
        // @ApiMember(Description="The sentence to echo.", ParameterType="form", DataType="string", IsRequired=true, Name="Sentence")
        sentence?: string;
    }

    interface CachedEcho
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

    // @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
    // @Route("/api/acsprofiles/{profileId}")
    interface ACSProfile extends IReturn<acsprofileResponse>
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

    interface TestMiniverView
    {
    }

    // @Route("/testexecproc")
    interface TestExecProc
    {
    }

    interface QueryRockstarsConventions extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
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

    // @AutoQueryViewer(Title="Search for Rockstars", Description="Use this option to search for Rockstars!")
    interface QueryCustomRockstars extends QueryBase_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        age?: number;
    }

    // @Route("/customrockstars")
    interface QueryRockstarAlbums extends QueryBase_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        age?: number;
        rockstarAlbumName?: string;
    }

    interface QueryRockstarAlbumsImplicit extends QueryBase_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
    }

    interface QueryRockstarAlbumsLeftJoin extends QueryBase_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        age?: number;
        albumName?: string;
    }

    interface QueryOverridedRockstars extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        age?: number;
    }

    interface QueryOverridedCustomRockstars extends QueryBase_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        age?: number;
    }

    interface QueryFieldRockstars extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        firstName?: string;
        firstNames?: string[];
        age?: number;
        firstNameCaseInsensitive?: string;
        firstNameStartsWith?: string;
        lastNameEndsWith?: string;
        firstNameBetween?: string[];
        orLastName?: string;
    }

    interface QueryFieldRockstarsDynamic extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        age?: number;
    }

    interface QueryRockstarsFilter extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        age?: number;
    }

    interface QueryCustomRockstarsFilter extends QueryBase_2<Rockstar, CustomRockstar>, IReturn<QueryResponse<CustomRockstar>>
    {
        age?: number;
    }

    interface QueryRockstarsIFilter extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        age?: number;
    }

    // @Route("/OrRockstars")
    interface QueryOrRockstars extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        age?: number;
        firstName?: string;
    }

    interface QueryGetRockstars extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        ids?: number[];
        ages?: number[];
        firstNames?: string[];
        idsBetween?: number[];
    }

    interface QueryGetRockstarsDynamic extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
    }

    // @Route("/movies/search")
    interface SearchMovies extends QueryBase_1<Movie>, IReturn<QueryResponse<Movie>>
    {
    }

    // @Route("/movies")
    interface QueryMovies extends QueryBase_1<Movie>, IReturn<QueryResponse<Movie>>
    {
        ids?: number[];
        imdbIds?: string[];
        ratings?: string[];
    }

    interface StreamMovies extends QueryBase_1<Movie>, IReturn<QueryResponse<Movie>>
    {
        ratings?: string[];
    }

    interface QueryUnknownRockstars extends QueryBase_1<Rockstar>, IReturn<QueryResponse<Rockstar>>
    {
        unknownInt?: number;
        unknownProperty?: string;
    }

    // @Route("/query/rockstar-references")
    interface QueryRockstarsWithReferences extends QueryBase_1<RockstarReference>, IReturn<QueryResponse<RockstarReference>>
    {
        age?: number;
    }

    interface QueryPocoBase extends QueryBase_1<OnlyDefinedInGenericType>, IReturn<QueryResponse<OnlyDefinedInGenericType>>
    {
        id?: number;
    }

    interface QueryPocoIntoBase extends QueryBase_2<OnlyDefinedInGenericTypeFrom, OnlyDefinedInGenericTypeInto>, IReturn<QueryResponse<OnlyDefinedInGenericTypeInto>>
    {
        id?: number;
    }

}
