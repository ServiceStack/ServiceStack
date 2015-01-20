/* Options:
Date: 2015-01-20 17:07:22
Version: 1
BaseUrl: http://localhost:55799

//GlobalNamespace: 
//MakePropertiesOptional: True
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
*/

declare module Check.ServiceModel
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

    interface NestedClass
    {
        value?: string;
    }

    interface ListResult
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

    // @Flags()
    enum EnumFlags
    {
        value1 = 1,
        value2 = 2,
        value3 = 4,
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
        nullableDateTime?: string;
        nullableTimeSpan?: string;
        stringList?: string[];
        stringArray?: string[];
        stringMap?: { [index:string]: string; };
        intStringMap?: { [index:number]: string; };
        subType?: SubType;
    }

    interface AllCollectionTypes
    {
        intArray?: number[];
        intList?: number[];
        stringArray?: string[];
        stringList?: string[];
        pocoArray?: Poco[];
        pocoList?: Poco[];
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
        providerOAuthAccess?: IAuthTokens[];

        // @DataMember(Order=36)
        roles?: string[];

        // @DataMember(Order=37)
        permissions?: string[];

        // @DataMember(Order=38)
        isAuthenticated?: boolean;

        // @DataMember(Order=39)
        sequence?: string;

        // @DataMember(Order=40)
        tag?: number;
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

    // @DataContract
    interface RestService
    {
        // @DataMember(Name="path")
        path?: string;

        // @DataMember(Name="description")
        description?: string;
    }

    interface QueryBase_2<From, Into> extends QueryBase
    {
    }

    interface CustomRockstar
    {
        firstName?: string;
        lastName?: string;
        age?: number;
        rockstarAlbumName?: string;
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

    interface SubType
    {
        id?: number;
        name?: string;
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

    // @DataContract
    interface QueryResponse<Rockstar>
    {
        // @DataMember(Order=1)
        offset?: number;

        // @DataMember(Order=2)
        total?: number;

        // @DataMember(Order=3)
        results?: Rockstar[];

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

    interface Echo
    {
        sentence?: string;
    }

    interface acsprofileResponse
    {
        profileId?: string;
    }

    // @DataContract
    interface ResourcesResponse
    {
        // @DataMember(Name="swaggerVersion")
        swaggerVersion?: string;

        // @DataMember(Name="apiVersion")
        apiVersion?: string;

        // @DataMember(Name="basePath")
        basePath?: string;

        // @DataMember(Name="apis")
        apis?: RestService[];
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
    interface ChangeRequest extends IReturn<ChangeRequest>
    {
        id?: string;
    }

    // @Route("/Routing/LeadPost.aspx")
    interface LegacyLeadPost
    {
        leadType?: string;
        myId?: number;
    }

    interface CustomHttpError extends IReturn<CustomHttpError>
    {
        statusCode?: number;
        statusDescription?: string;
    }

    interface CustomFieldHttpError extends IReturn<CustomFieldHttpError>
    {
    }

    // @Route("{PathInfo*}")
    interface FallbackRoute
    {
        pathInfo?: string;
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

    // @Route("/namedconnection")
    interface NamedConnection
    {
        emailAddresses?: string;
    }

    // @Route("/hello/{Name}")
    interface Hello extends IReturn<Hello>
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

    interface HelloList extends IReturn<ListResult[]>
    {
        names?: string[];
    }

    interface HelloArray extends IReturn<ArrayResult[]>
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
        // @Default(5)
        // @Required()
        id: number;

        // @DataMember(Name="Aliased")
        // @ApiMember(Description="Range Description", ParameterType="path", DataType="double", IsRequired=true)
        range?: number;

        // @StringLength(20)
        // @References(typeof(Hello))
        // @Meta("Foo", "Bar")
        name?: string;
    }

    interface HelloAllTypes extends IReturn<HelloAllTypes>
    {
        name?: string;
        allTypes?: AllTypes;
        allCollectionTypes?: AllCollectionTypes;
    }

    interface HelloString
    {
        name?: string;
    }

    interface HelloVoid
    {
        name?: string;
    }

    // @DataContract
    interface HelloWithDataContract extends IReturn<HelloWithDataContract>
    {
        // @DataMember(Name="name", Order=1, IsRequired=true, EmitDefaultValue=false)
        name?: string;

        // @DataMember(Name="id", Order=2, EmitDefaultValue=false)
        id?: number;
    }

    /**
    * Description on HelloWithDescription type
    */
    interface HelloWithDescription extends IReturn<HelloWithDescription>
    {
        name?: string;
    }

    interface HelloWithInheritance extends HelloBase, IReturn<HelloWithInheritance>
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
    interface HelloWithRoute extends IReturn<HelloWithRoute>
    {
        name?: string;
    }

    interface HelloWithType extends IReturn<HelloWithType>
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
    interface ThrowHttpError
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

    // @Route("/api/acsprofiles/{profileId}")
    // @Route("/api/acsprofiles", "POST,PUT,PATCH,DELETE")
    interface ACSProfile extends IReturn<acsprofileResponse>
    {
        profileId?: string;
        // @StringLength(20)
        // @Required()
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
    }

    // @Route("/resources")
    // @DataContract
    interface Resources extends IReturn<Resources>
    {
        // @DataMember(Name="apiKey")
        apiKey?: string;
    }

    // @Route("/resource/{Name*}")
    // @DataContract
    interface ResourceRequest
    {
        // @DataMember(Name="apiKey")
        apiKey?: string;

        // @DataMember(Name="name")
        name?: string;
    }

    // @Route("/postman")
    interface Postman
    {
        label?: string[];
        exportSession?: boolean;
        ssid?: string;
        sspid?: string;
        ssopt?: string;
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

}
