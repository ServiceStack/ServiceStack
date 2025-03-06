<?php namespace dtos;
/* Options:
Date: 2025-03-06 19:46:05
Version: 8.61
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//GlobalNamespace: dtos
//MakePropertiesOptional: False
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: 
*/


use DateTime;
use Exception;
use DateInterval;
use JsonSerializable;
use ServiceStack\{IReturn,IReturnVoid,IGet,IPost,IPut,IDelete,IPatch,IMeta,IHasSessionId,IHasBearerToken,IHasVersion};
use ServiceStack\{ICrud,ICreateDb,IUpdateDb,IPatchDb,IDeleteDb,ISaveDb,AuditBase,QueryDb,QueryDb2,QueryData,QueryData2,QueryResponse};
use ServiceStack\{ResponseStatus,ResponseError,EmptyResponse,IdResponse,ArrayList,KeyValuePair2,StringResponse,StringsResponse,Tuple2,Tuple3,ByteArray};
use ServiceStack\{JsonConverters,Returns,TypeContext};


// @DataContract
class Property implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $name=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $value=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['value'])) $this->value = $o['value'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->value)) $o['value'] = $this->value;
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminUserBase implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $userName=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $firstName=null,

        // @DataMember(Order=3)
        /** @var string|null */
        public ?string $lastName=null,

        // @DataMember(Order=4)
        /** @var string|null */
        public ?string $displayName=null,

        // @DataMember(Order=5)
        /** @var string|null */
        public ?string $email=null,

        // @DataMember(Order=6)
        /** @var string|null */
        public ?string $password=null,

        // @DataMember(Order=7)
        /** @var string|null */
        public ?string $profileUrl=null,

        // @DataMember(Order=8)
        /** @var string|null */
        public ?string $phoneNumber=null,

        // @DataMember(Order=9)
        /** @var array<string,string>|null */
        public ?array $userAuthProperties=null,

        // @DataMember(Order=10)
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['userName'])) $this->userName = $o['userName'];
        if (isset($o['firstName'])) $this->firstName = $o['firstName'];
        if (isset($o['lastName'])) $this->lastName = $o['lastName'];
        if (isset($o['displayName'])) $this->displayName = $o['displayName'];
        if (isset($o['email'])) $this->email = $o['email'];
        if (isset($o['password'])) $this->password = $o['password'];
        if (isset($o['profileUrl'])) $this->profileUrl = $o['profileUrl'];
        if (isset($o['phoneNumber'])) $this->phoneNumber = $o['phoneNumber'];
        if (isset($o['userAuthProperties'])) $this->userAuthProperties = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['userAuthProperties']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->userName)) $o['userName'] = $this->userName;
        if (isset($this->firstName)) $o['firstName'] = $this->firstName;
        if (isset($this->lastName)) $o['lastName'] = $this->lastName;
        if (isset($this->displayName)) $o['displayName'] = $this->displayName;
        if (isset($this->email)) $o['email'] = $this->email;
        if (isset($this->password)) $o['password'] = $this->password;
        if (isset($this->profileUrl)) $o['profileUrl'] = $this->profileUrl;
        if (isset($this->phoneNumber)) $o['phoneNumber'] = $this->phoneNumber;
        if (isset($this->userAuthProperties)) $o['userAuthProperties'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->userAuthProperties);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class RequestLog implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $id=0,
        /** @var string */
        public string $traceId='',
        /** @var string */
        public string $operationName='',
        /** @var DateTime */
        public DateTime $dateTime=new DateTime(),
        /** @var int */
        public int $statusCode=0,
        /** @var string|null */
        public ?string $statusDescription=null,
        /** @var string|null */
        public ?string $httpMethod=null,
        /** @var string|null */
        public ?string $absoluteUri=null,
        /** @var string|null */
        public ?string $pathInfo=null,
        /** @var string|null */
        public ?string $request=null,
        // @StringLength(2147483647)
        /** @var string|null */
        public ?string $requestBody=null,

        /** @var string|null */
        public ?string $userAuthId=null,
        /** @var string|null */
        public ?string $sessionId=null,
        /** @var string|null */
        public ?string $ipAddress=null,
        /** @var string|null */
        public ?string $forwardedFor=null,
        /** @var string|null */
        public ?string $referer=null,
        /** @var array<string,string>|null */
        public ?array $headers=null,
        /** @var array<string,string>|null */
        public ?array $formData=null,
        /** @var array<string,string>|null */
        public ?array $items=null,
        /** @var array<string,string>|null */
        public ?array $responseHeaders=null,
        /** @var string|null */
        public ?string $response=null,
        /** @var string|null */
        public ?string $responseBody=null,
        /** @var string|null */
        public ?string $sessionBody=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $error=null,
        /** @var string|null */
        public ?string $exceptionSource=null,
        /** @var string|null */
        public ?string $exceptionDataBody=null,
        /** @var DateInterval|null */
        public ?DateInterval $requestDuration=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['traceId'])) $this->traceId = $o['traceId'];
        if (isset($o['operationName'])) $this->operationName = $o['operationName'];
        if (isset($o['dateTime'])) $this->dateTime = JsonConverters::from('DateTime', $o['dateTime']);
        if (isset($o['statusCode'])) $this->statusCode = $o['statusCode'];
        if (isset($o['statusDescription'])) $this->statusDescription = $o['statusDescription'];
        if (isset($o['httpMethod'])) $this->httpMethod = $o['httpMethod'];
        if (isset($o['absoluteUri'])) $this->absoluteUri = $o['absoluteUri'];
        if (isset($o['pathInfo'])) $this->pathInfo = $o['pathInfo'];
        if (isset($o['request'])) $this->request = $o['request'];
        if (isset($o['requestBody'])) $this->requestBody = $o['requestBody'];
        if (isset($o['userAuthId'])) $this->userAuthId = $o['userAuthId'];
        if (isset($o['sessionId'])) $this->sessionId = $o['sessionId'];
        if (isset($o['ipAddress'])) $this->ipAddress = $o['ipAddress'];
        if (isset($o['forwardedFor'])) $this->forwardedFor = $o['forwardedFor'];
        if (isset($o['referer'])) $this->referer = $o['referer'];
        if (isset($o['headers'])) $this->headers = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['headers']);
        if (isset($o['formData'])) $this->formData = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['formData']);
        if (isset($o['items'])) $this->items = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['items']);
        if (isset($o['responseHeaders'])) $this->responseHeaders = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['responseHeaders']);
        if (isset($o['response'])) $this->response = $o['response'];
        if (isset($o['responseBody'])) $this->responseBody = $o['responseBody'];
        if (isset($o['sessionBody'])) $this->sessionBody = $o['sessionBody'];
        if (isset($o['error'])) $this->error = JsonConverters::from('ResponseStatus', $o['error']);
        if (isset($o['exceptionSource'])) $this->exceptionSource = $o['exceptionSource'];
        if (isset($o['exceptionDataBody'])) $this->exceptionDataBody = $o['exceptionDataBody'];
        if (isset($o['requestDuration'])) $this->requestDuration = JsonConverters::from('DateInterval', $o['requestDuration']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->traceId)) $o['traceId'] = $this->traceId;
        if (isset($this->operationName)) $o['operationName'] = $this->operationName;
        if (isset($this->dateTime)) $o['dateTime'] = JsonConverters::to('DateTime', $this->dateTime);
        if (isset($this->statusCode)) $o['statusCode'] = $this->statusCode;
        if (isset($this->statusDescription)) $o['statusDescription'] = $this->statusDescription;
        if (isset($this->httpMethod)) $o['httpMethod'] = $this->httpMethod;
        if (isset($this->absoluteUri)) $o['absoluteUri'] = $this->absoluteUri;
        if (isset($this->pathInfo)) $o['pathInfo'] = $this->pathInfo;
        if (isset($this->request)) $o['request'] = $this->request;
        if (isset($this->requestBody)) $o['requestBody'] = $this->requestBody;
        if (isset($this->userAuthId)) $o['userAuthId'] = $this->userAuthId;
        if (isset($this->sessionId)) $o['sessionId'] = $this->sessionId;
        if (isset($this->ipAddress)) $o['ipAddress'] = $this->ipAddress;
        if (isset($this->forwardedFor)) $o['forwardedFor'] = $this->forwardedFor;
        if (isset($this->referer)) $o['referer'] = $this->referer;
        if (isset($this->headers)) $o['headers'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->headers);
        if (isset($this->formData)) $o['formData'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->formData);
        if (isset($this->items)) $o['items'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->items);
        if (isset($this->responseHeaders)) $o['responseHeaders'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->responseHeaders);
        if (isset($this->response)) $o['response'] = $this->response;
        if (isset($this->responseBody)) $o['responseBody'] = $this->responseBody;
        if (isset($this->sessionBody)) $o['sessionBody'] = $this->sessionBody;
        if (isset($this->error)) $o['error'] = JsonConverters::to('ResponseStatus', $this->error);
        if (isset($this->exceptionSource)) $o['exceptionSource'] = $this->exceptionSource;
        if (isset($this->exceptionDataBody)) $o['exceptionDataBody'] = $this->exceptionDataBody;
        if (isset($this->requestDuration)) $o['requestDuration'] = JsonConverters::to('DateInterval', $this->requestDuration);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class RedisEndpointInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $host=null,
        /** @var int */
        public int $port=0,
        /** @var bool|null */
        public ?bool $ssl=null,
        /** @var int */
        public int $db=0,
        /** @var string|null */
        public ?string $username=null,
        /** @var string|null */
        public ?string $password=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['host'])) $this->host = $o['host'];
        if (isset($o['port'])) $this->port = $o['port'];
        if (isset($o['ssl'])) $this->ssl = $o['ssl'];
        if (isset($o['db'])) $this->db = $o['db'];
        if (isset($o['username'])) $this->username = $o['username'];
        if (isset($o['password'])) $this->password = $o['password'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->host)) $o['host'] = $this->host;
        if (isset($this->port)) $o['port'] = $this->port;
        if (isset($this->ssl)) $o['ssl'] = $this->ssl;
        if (isset($this->db)) $o['db'] = $this->db;
        if (isset($this->username)) $o['username'] = $this->username;
        if (isset($this->password)) $o['password'] = $this->password;
        return empty($o) ? new class(){} : $o;
    }
}

enum BackgroundJobState : string
{
    case Queued = 'Queued';
    case Started = 'Started';
    case Executed = 'Executed';
    case Completed = 'Completed';
    case Failed = 'Failed';
    case Cancelled = 'Cancelled';
}

class BackgroundJobBase implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $id=0,
        /** @var int|null */
        public ?int $parentId=null,
        /** @var string|null */
        public ?string $refId=null,
        /** @var string|null */
        public ?string $worker=null,
        /** @var string|null */
        public ?string $tag=null,
        /** @var string|null */
        public ?string $batchId=null,
        /** @var string|null */
        public ?string $callback=null,
        /** @var int|null */
        public ?int $dependsOn=null,
        /** @var DateTime|null */
        public ?DateTime $runAfter=null,
        /** @var DateTime */
        public DateTime $createdDate=new DateTime(),
        /** @var string|null */
        public ?string $createdBy=null,
        /** @var string|null */
        public ?string $requestId=null,
        /** @var string */
        public string $requestType='',
        /** @var string|null */
        public ?string $command=null,
        /** @var string */
        public string $request='',
        /** @var string */
        public string $requestBody='',
        /** @var string|null */
        public ?string $userId=null,
        /** @var string|null */
        public ?string $response=null,
        /** @var string|null */
        public ?string $responseBody=null,
        /** @var BackgroundJobState|null */
        public ?BackgroundJobState $state=null,
        /** @var DateTime|null */
        public ?DateTime $startedDate=null,
        /** @var DateTime|null */
        public ?DateTime $completedDate=null,
        /** @var DateTime|null */
        public ?DateTime $notifiedDate=null,
        /** @var int|null */
        public ?int $retryLimit=null,
        /** @var int */
        public int $attempts=0,
        /** @var int */
        public int $durationMs=0,
        /** @var int|null */
        public ?int $timeoutSecs=null,
        /** @var float|null */
        public ?float $progress=null,
        /** @var string|null */
        public ?string $status=null,
        /** @var string|null */
        public ?string $logs=null,
        /** @var DateTime|null */
        public ?DateTime $lastActivityDate=null,
        /** @var string|null */
        public ?string $replyTo=null,
        /** @var string|null */
        public ?string $errorCode=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $error=null,
        /** @var array<string,string>|null */
        public ?array $args=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['parentId'])) $this->parentId = $o['parentId'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['worker'])) $this->worker = $o['worker'];
        if (isset($o['tag'])) $this->tag = $o['tag'];
        if (isset($o['batchId'])) $this->batchId = $o['batchId'];
        if (isset($o['callback'])) $this->callback = $o['callback'];
        if (isset($o['dependsOn'])) $this->dependsOn = $o['dependsOn'];
        if (isset($o['runAfter'])) $this->runAfter = JsonConverters::from('DateTime', $o['runAfter']);
        if (isset($o['createdDate'])) $this->createdDate = JsonConverters::from('DateTime', $o['createdDate']);
        if (isset($o['createdBy'])) $this->createdBy = $o['createdBy'];
        if (isset($o['requestId'])) $this->requestId = $o['requestId'];
        if (isset($o['requestType'])) $this->requestType = $o['requestType'];
        if (isset($o['command'])) $this->command = $o['command'];
        if (isset($o['request'])) $this->request = $o['request'];
        if (isset($o['requestBody'])) $this->requestBody = $o['requestBody'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['response'])) $this->response = $o['response'];
        if (isset($o['responseBody'])) $this->responseBody = $o['responseBody'];
        if (isset($o['state'])) $this->state = JsonConverters::from('BackgroundJobState', $o['state']);
        if (isset($o['startedDate'])) $this->startedDate = JsonConverters::from('DateTime', $o['startedDate']);
        if (isset($o['completedDate'])) $this->completedDate = JsonConverters::from('DateTime', $o['completedDate']);
        if (isset($o['notifiedDate'])) $this->notifiedDate = JsonConverters::from('DateTime', $o['notifiedDate']);
        if (isset($o['retryLimit'])) $this->retryLimit = $o['retryLimit'];
        if (isset($o['attempts'])) $this->attempts = $o['attempts'];
        if (isset($o['durationMs'])) $this->durationMs = $o['durationMs'];
        if (isset($o['timeoutSecs'])) $this->timeoutSecs = $o['timeoutSecs'];
        if (isset($o['progress'])) $this->progress = $o['progress'];
        if (isset($o['status'])) $this->status = $o['status'];
        if (isset($o['logs'])) $this->logs = $o['logs'];
        if (isset($o['lastActivityDate'])) $this->lastActivityDate = JsonConverters::from('DateTime', $o['lastActivityDate']);
        if (isset($o['replyTo'])) $this->replyTo = $o['replyTo'];
        if (isset($o['errorCode'])) $this->errorCode = $o['errorCode'];
        if (isset($o['error'])) $this->error = JsonConverters::from('ResponseStatus', $o['error']);
        if (isset($o['args'])) $this->args = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['args']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->parentId)) $o['parentId'] = $this->parentId;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->worker)) $o['worker'] = $this->worker;
        if (isset($this->tag)) $o['tag'] = $this->tag;
        if (isset($this->batchId)) $o['batchId'] = $this->batchId;
        if (isset($this->callback)) $o['callback'] = $this->callback;
        if (isset($this->dependsOn)) $o['dependsOn'] = $this->dependsOn;
        if (isset($this->runAfter)) $o['runAfter'] = JsonConverters::to('DateTime', $this->runAfter);
        if (isset($this->createdDate)) $o['createdDate'] = JsonConverters::to('DateTime', $this->createdDate);
        if (isset($this->createdBy)) $o['createdBy'] = $this->createdBy;
        if (isset($this->requestId)) $o['requestId'] = $this->requestId;
        if (isset($this->requestType)) $o['requestType'] = $this->requestType;
        if (isset($this->command)) $o['command'] = $this->command;
        if (isset($this->request)) $o['request'] = $this->request;
        if (isset($this->requestBody)) $o['requestBody'] = $this->requestBody;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->response)) $o['response'] = $this->response;
        if (isset($this->responseBody)) $o['responseBody'] = $this->responseBody;
        if (isset($this->state)) $o['state'] = JsonConverters::to('BackgroundJobState', $this->state);
        if (isset($this->startedDate)) $o['startedDate'] = JsonConverters::to('DateTime', $this->startedDate);
        if (isset($this->completedDate)) $o['completedDate'] = JsonConverters::to('DateTime', $this->completedDate);
        if (isset($this->notifiedDate)) $o['notifiedDate'] = JsonConverters::to('DateTime', $this->notifiedDate);
        if (isset($this->retryLimit)) $o['retryLimit'] = $this->retryLimit;
        if (isset($this->attempts)) $o['attempts'] = $this->attempts;
        if (isset($this->durationMs)) $o['durationMs'] = $this->durationMs;
        if (isset($this->timeoutSecs)) $o['timeoutSecs'] = $this->timeoutSecs;
        if (isset($this->progress)) $o['progress'] = $this->progress;
        if (isset($this->status)) $o['status'] = $this->status;
        if (isset($this->logs)) $o['logs'] = $this->logs;
        if (isset($this->lastActivityDate)) $o['lastActivityDate'] = JsonConverters::to('DateTime', $this->lastActivityDate);
        if (isset($this->replyTo)) $o['replyTo'] = $this->replyTo;
        if (isset($this->errorCode)) $o['errorCode'] = $this->errorCode;
        if (isset($this->error)) $o['error'] = JsonConverters::to('ResponseStatus', $this->error);
        if (isset($this->args)) $o['args'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->args);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class BackgroundJob extends BackgroundJobBase implements JsonSerializable
{
    /**
     * @param int $id
     * @param int|null $parentId
     * @param string|null $refId
     * @param string|null $worker
     * @param string|null $tag
     * @param string|null $batchId
     * @param string|null $callback
     * @param int|null $dependsOn
     * @param DateTime|null $runAfter
     * @param DateTime $createdDate
     * @param string|null $createdBy
     * @param string|null $requestId
     * @param string $requestType
     * @param string|null $command
     * @param string $request
     * @param string $requestBody
     * @param string|null $userId
     * @param string|null $response
     * @param string|null $responseBody
     * @param BackgroundJobState|null $state
     * @param DateTime|null $startedDate
     * @param DateTime|null $completedDate
     * @param DateTime|null $notifiedDate
     * @param int|null $retryLimit
     * @param int $attempts
     * @param int $durationMs
     * @param int|null $timeoutSecs
     * @param float|null $progress
     * @param string|null $status
     * @param string|null $logs
     * @param DateTime|null $lastActivityDate
     * @param string|null $replyTo
     * @param string|null $errorCode
     * @param ResponseStatus|null $error
     * @param array<string,string>|null $args
     * @param array<string,string>|null $meta
     */
    public function __construct(
        int $id=0,
        ?int $parentId=null,
        ?string $refId=null,
        ?string $worker=null,
        ?string $tag=null,
        ?string $batchId=null,
        ?string $callback=null,
        ?int $dependsOn=null,
        ?DateTime $runAfter=null,
        DateTime $createdDate=new DateTime(),
        ?string $createdBy=null,
        ?string $requestId=null,
        string $requestType='',
        ?string $command=null,
        string $request='',
        string $requestBody='',
        ?string $userId=null,
        ?string $response=null,
        ?string $responseBody=null,
        ?BackgroundJobState $state=null,
        ?DateTime $startedDate=null,
        ?DateTime $completedDate=null,
        ?DateTime $notifiedDate=null,
        ?int $retryLimit=null,
        int $attempts=0,
        int $durationMs=0,
        ?int $timeoutSecs=null,
        ?float $progress=null,
        ?string $status=null,
        ?string $logs=null,
        ?DateTime $lastActivityDate=null,
        ?string $replyTo=null,
        ?string $errorCode=null,
        ?ResponseStatus $error=null,
        ?array $args=null,
        ?array $meta=null
    ) {
        parent::__construct($id,$parentId,$refId,$worker,$tag,$batchId,$callback,$dependsOn,$runAfter,$createdDate,$createdBy,$requestId,$requestType,$command,$request,$requestBody,$userId,$response,$responseBody,$state,$startedDate,$completedDate,$notifiedDate,$retryLimit,$attempts,$durationMs,$timeoutSecs,$progress,$status,$logs,$lastActivityDate,$replyTo,$errorCode,$error,$args,$meta);
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['id'])) $this->id = $o['id'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->id)) $o['id'] = $this->id;
        return empty($o) ? new class(){} : $o;
    }
}

class JobSummary implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $id=0,
        /** @var int|null */
        public ?int $parentId=null,
        /** @var string|null */
        public ?string $refId=null,
        /** @var string|null */
        public ?string $worker=null,
        /** @var string|null */
        public ?string $tag=null,
        /** @var string|null */
        public ?string $batchId=null,
        /** @var DateTime */
        public DateTime $createdDate=new DateTime(),
        /** @var string|null */
        public ?string $createdBy=null,
        /** @var string */
        public string $requestType='',
        /** @var string|null */
        public ?string $command=null,
        /** @var string */
        public string $request='',
        /** @var string|null */
        public ?string $response=null,
        /** @var string|null */
        public ?string $userId=null,
        /** @var string|null */
        public ?string $callback=null,
        /** @var DateTime|null */
        public ?DateTime $startedDate=null,
        /** @var DateTime|null */
        public ?DateTime $completedDate=null,
        /** @var BackgroundJobState|null */
        public ?BackgroundJobState $state=null,
        /** @var int */
        public int $durationMs=0,
        /** @var int */
        public int $attempts=0,
        /** @var string|null */
        public ?string $errorCode=null,
        /** @var string|null */
        public ?string $errorMessage=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['parentId'])) $this->parentId = $o['parentId'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['worker'])) $this->worker = $o['worker'];
        if (isset($o['tag'])) $this->tag = $o['tag'];
        if (isset($o['batchId'])) $this->batchId = $o['batchId'];
        if (isset($o['createdDate'])) $this->createdDate = JsonConverters::from('DateTime', $o['createdDate']);
        if (isset($o['createdBy'])) $this->createdBy = $o['createdBy'];
        if (isset($o['requestType'])) $this->requestType = $o['requestType'];
        if (isset($o['command'])) $this->command = $o['command'];
        if (isset($o['request'])) $this->request = $o['request'];
        if (isset($o['response'])) $this->response = $o['response'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['callback'])) $this->callback = $o['callback'];
        if (isset($o['startedDate'])) $this->startedDate = JsonConverters::from('DateTime', $o['startedDate']);
        if (isset($o['completedDate'])) $this->completedDate = JsonConverters::from('DateTime', $o['completedDate']);
        if (isset($o['state'])) $this->state = JsonConverters::from('BackgroundJobState', $o['state']);
        if (isset($o['durationMs'])) $this->durationMs = $o['durationMs'];
        if (isset($o['attempts'])) $this->attempts = $o['attempts'];
        if (isset($o['errorCode'])) $this->errorCode = $o['errorCode'];
        if (isset($o['errorMessage'])) $this->errorMessage = $o['errorMessage'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->parentId)) $o['parentId'] = $this->parentId;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->worker)) $o['worker'] = $this->worker;
        if (isset($this->tag)) $o['tag'] = $this->tag;
        if (isset($this->batchId)) $o['batchId'] = $this->batchId;
        if (isset($this->createdDate)) $o['createdDate'] = JsonConverters::to('DateTime', $this->createdDate);
        if (isset($this->createdBy)) $o['createdBy'] = $this->createdBy;
        if (isset($this->requestType)) $o['requestType'] = $this->requestType;
        if (isset($this->command)) $o['command'] = $this->command;
        if (isset($this->request)) $o['request'] = $this->request;
        if (isset($this->response)) $o['response'] = $this->response;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->callback)) $o['callback'] = $this->callback;
        if (isset($this->startedDate)) $o['startedDate'] = JsonConverters::to('DateTime', $this->startedDate);
        if (isset($this->completedDate)) $o['completedDate'] = JsonConverters::to('DateTime', $this->completedDate);
        if (isset($this->state)) $o['state'] = JsonConverters::to('BackgroundJobState', $this->state);
        if (isset($this->durationMs)) $o['durationMs'] = $this->durationMs;
        if (isset($this->attempts)) $o['attempts'] = $this->attempts;
        if (isset($this->errorCode)) $o['errorCode'] = $this->errorCode;
        if (isset($this->errorMessage)) $o['errorMessage'] = $this->errorMessage;
        return empty($o) ? new class(){} : $o;
    }
}

class BackgroundJobOptions implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $refId=null,
        /** @var int|null */
        public ?int $parentId=null,
        /** @var string|null */
        public ?string $worker=null,
        /** @var DateTime|null */
        public ?DateTime $runAfter=null,
        /** @var string|null */
        public ?string $callback=null,
        /** @var int|null */
        public ?int $dependsOn=null,
        /** @var string|null */
        public ?string $userId=null,
        /** @var int|null */
        public ?int $retryLimit=null,
        /** @var string|null */
        public ?string $replyTo=null,
        /** @var string|null */
        public ?string $tag=null,
        /** @var string|null */
        public ?string $batchId=null,
        /** @var string|null */
        public ?string $createdBy=null,
        /** @var int|null */
        public ?int $timeoutSecs=null,
        /** @var DateInterval|null */
        public ?DateInterval $timeout=null,
        /** @var array<string,string>|null */
        public ?array $args=null,
        /** @var bool|null */
        public ?bool $runCommand=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['parentId'])) $this->parentId = $o['parentId'];
        if (isset($o['worker'])) $this->worker = $o['worker'];
        if (isset($o['runAfter'])) $this->runAfter = JsonConverters::from('DateTime', $o['runAfter']);
        if (isset($o['callback'])) $this->callback = $o['callback'];
        if (isset($o['dependsOn'])) $this->dependsOn = $o['dependsOn'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['retryLimit'])) $this->retryLimit = $o['retryLimit'];
        if (isset($o['replyTo'])) $this->replyTo = $o['replyTo'];
        if (isset($o['tag'])) $this->tag = $o['tag'];
        if (isset($o['batchId'])) $this->batchId = $o['batchId'];
        if (isset($o['createdBy'])) $this->createdBy = $o['createdBy'];
        if (isset($o['timeoutSecs'])) $this->timeoutSecs = $o['timeoutSecs'];
        if (isset($o['timeout'])) $this->timeout = JsonConverters::from('TimeSpan', $o['timeout']);
        if (isset($o['args'])) $this->args = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['args']);
        if (isset($o['runCommand'])) $this->runCommand = $o['runCommand'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->parentId)) $o['parentId'] = $this->parentId;
        if (isset($this->worker)) $o['worker'] = $this->worker;
        if (isset($this->runAfter)) $o['runAfter'] = JsonConverters::to('DateTime', $this->runAfter);
        if (isset($this->callback)) $o['callback'] = $this->callback;
        if (isset($this->dependsOn)) $o['dependsOn'] = $this->dependsOn;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->retryLimit)) $o['retryLimit'] = $this->retryLimit;
        if (isset($this->replyTo)) $o['replyTo'] = $this->replyTo;
        if (isset($this->tag)) $o['tag'] = $this->tag;
        if (isset($this->batchId)) $o['batchId'] = $this->batchId;
        if (isset($this->createdBy)) $o['createdBy'] = $this->createdBy;
        if (isset($this->timeoutSecs)) $o['timeoutSecs'] = $this->timeoutSecs;
        if (isset($this->timeout)) $o['timeout'] = JsonConverters::to('TimeSpan', $this->timeout);
        if (isset($this->args)) $o['args'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->args);
        if (isset($this->runCommand)) $o['runCommand'] = $this->runCommand;
        return empty($o) ? new class(){} : $o;
    }
}

class ScheduledTask implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $id=0,
        /** @var string */
        public string $name='',
        /** @var DateInterval|null */
        public ?DateInterval $interval=null,
        /** @var string|null */
        public ?string $cronExpression=null,
        /** @var string */
        public string $requestType='',
        /** @var string|null */
        public ?string $command=null,
        /** @var string */
        public string $request='',
        /** @var string */
        public string $requestBody='',
        /** @var BackgroundJobOptions|null */
        public ?BackgroundJobOptions $options=null,
        /** @var DateTime|null */
        public ?DateTime $lastRun=null,
        /** @var int|null */
        public ?int $lastJobId=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['interval'])) $this->interval = JsonConverters::from('TimeSpan', $o['interval']);
        if (isset($o['cronExpression'])) $this->cronExpression = $o['cronExpression'];
        if (isset($o['requestType'])) $this->requestType = $o['requestType'];
        if (isset($o['command'])) $this->command = $o['command'];
        if (isset($o['request'])) $this->request = $o['request'];
        if (isset($o['requestBody'])) $this->requestBody = $o['requestBody'];
        if (isset($o['options'])) $this->options = JsonConverters::from('BackgroundJobOptions', $o['options']);
        if (isset($o['lastRun'])) $this->lastRun = JsonConverters::from('DateTime', $o['lastRun']);
        if (isset($o['lastJobId'])) $this->lastJobId = $o['lastJobId'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->interval)) $o['interval'] = JsonConverters::to('TimeSpan', $this->interval);
        if (isset($this->cronExpression)) $o['cronExpression'] = $this->cronExpression;
        if (isset($this->requestType)) $o['requestType'] = $this->requestType;
        if (isset($this->command)) $o['command'] = $this->command;
        if (isset($this->request)) $o['request'] = $this->request;
        if (isset($this->requestBody)) $o['requestBody'] = $this->requestBody;
        if (isset($this->options)) $o['options'] = JsonConverters::to('BackgroundJobOptions', $this->options);
        if (isset($this->lastRun)) $o['lastRun'] = JsonConverters::to('DateTime', $this->lastRun);
        if (isset($this->lastJobId)) $o['lastJobId'] = $this->lastJobId;
        return empty($o) ? new class(){} : $o;
    }
}

class CompletedJob extends BackgroundJobBase implements JsonSerializable
{
    /**
     * @param int $id
     * @param int|null $parentId
     * @param string|null $refId
     * @param string|null $worker
     * @param string|null $tag
     * @param string|null $batchId
     * @param string|null $callback
     * @param int|null $dependsOn
     * @param DateTime|null $runAfter
     * @param DateTime $createdDate
     * @param string|null $createdBy
     * @param string|null $requestId
     * @param string $requestType
     * @param string|null $command
     * @param string $request
     * @param string $requestBody
     * @param string|null $userId
     * @param string|null $response
     * @param string|null $responseBody
     * @param BackgroundJobState|null $state
     * @param DateTime|null $startedDate
     * @param DateTime|null $completedDate
     * @param DateTime|null $notifiedDate
     * @param int|null $retryLimit
     * @param int $attempts
     * @param int $durationMs
     * @param int|null $timeoutSecs
     * @param float|null $progress
     * @param string|null $status
     * @param string|null $logs
     * @param DateTime|null $lastActivityDate
     * @param string|null $replyTo
     * @param string|null $errorCode
     * @param ResponseStatus|null $error
     * @param array<string,string>|null $args
     * @param array<string,string>|null $meta
     */
    public function __construct(
        int $id=0,
        ?int $parentId=null,
        ?string $refId=null,
        ?string $worker=null,
        ?string $tag=null,
        ?string $batchId=null,
        ?string $callback=null,
        ?int $dependsOn=null,
        ?DateTime $runAfter=null,
        DateTime $createdDate=new DateTime(),
        ?string $createdBy=null,
        ?string $requestId=null,
        string $requestType='',
        ?string $command=null,
        string $request='',
        string $requestBody='',
        ?string $userId=null,
        ?string $response=null,
        ?string $responseBody=null,
        ?BackgroundJobState $state=null,
        ?DateTime $startedDate=null,
        ?DateTime $completedDate=null,
        ?DateTime $notifiedDate=null,
        ?int $retryLimit=null,
        int $attempts=0,
        int $durationMs=0,
        ?int $timeoutSecs=null,
        ?float $progress=null,
        ?string $status=null,
        ?string $logs=null,
        ?DateTime $lastActivityDate=null,
        ?string $replyTo=null,
        ?string $errorCode=null,
        ?ResponseStatus $error=null,
        ?array $args=null,
        ?array $meta=null
    ) {
        parent::__construct($id,$parentId,$refId,$worker,$tag,$batchId,$callback,$dependsOn,$runAfter,$createdDate,$createdBy,$requestId,$requestType,$command,$request,$requestBody,$userId,$response,$responseBody,$state,$startedDate,$completedDate,$notifiedDate,$retryLimit,$attempts,$durationMs,$timeoutSecs,$progress,$status,$logs,$lastActivityDate,$replyTo,$errorCode,$error,$args,$meta);
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        return empty($o) ? new class(){} : $o;
    }
}

class FailedJob extends BackgroundJobBase implements JsonSerializable
{
    /**
     * @param int $id
     * @param int|null $parentId
     * @param string|null $refId
     * @param string|null $worker
     * @param string|null $tag
     * @param string|null $batchId
     * @param string|null $callback
     * @param int|null $dependsOn
     * @param DateTime|null $runAfter
     * @param DateTime $createdDate
     * @param string|null $createdBy
     * @param string|null $requestId
     * @param string $requestType
     * @param string|null $command
     * @param string $request
     * @param string $requestBody
     * @param string|null $userId
     * @param string|null $response
     * @param string|null $responseBody
     * @param BackgroundJobState|null $state
     * @param DateTime|null $startedDate
     * @param DateTime|null $completedDate
     * @param DateTime|null $notifiedDate
     * @param int|null $retryLimit
     * @param int $attempts
     * @param int $durationMs
     * @param int|null $timeoutSecs
     * @param float|null $progress
     * @param string|null $status
     * @param string|null $logs
     * @param DateTime|null $lastActivityDate
     * @param string|null $replyTo
     * @param string|null $errorCode
     * @param ResponseStatus|null $error
     * @param array<string,string>|null $args
     * @param array<string,string>|null $meta
     */
    public function __construct(
        int $id=0,
        ?int $parentId=null,
        ?string $refId=null,
        ?string $worker=null,
        ?string $tag=null,
        ?string $batchId=null,
        ?string $callback=null,
        ?int $dependsOn=null,
        ?DateTime $runAfter=null,
        DateTime $createdDate=new DateTime(),
        ?string $createdBy=null,
        ?string $requestId=null,
        string $requestType='',
        ?string $command=null,
        string $request='',
        string $requestBody='',
        ?string $userId=null,
        ?string $response=null,
        ?string $responseBody=null,
        ?BackgroundJobState $state=null,
        ?DateTime $startedDate=null,
        ?DateTime $completedDate=null,
        ?DateTime $notifiedDate=null,
        ?int $retryLimit=null,
        int $attempts=0,
        int $durationMs=0,
        ?int $timeoutSecs=null,
        ?float $progress=null,
        ?string $status=null,
        ?string $logs=null,
        ?DateTime $lastActivityDate=null,
        ?string $replyTo=null,
        ?string $errorCode=null,
        ?ResponseStatus $error=null,
        ?array $args=null,
        ?array $meta=null
    ) {
        parent::__construct($id,$parentId,$refId,$worker,$tag,$batchId,$callback,$dependsOn,$runAfter,$createdDate,$createdBy,$requestId,$requestType,$command,$request,$requestBody,$userId,$response,$responseBody,$state,$startedDate,$completedDate,$notifiedDate,$retryLimit,$attempts,$durationMs,$timeoutSecs,$progress,$status,$logs,$lastActivityDate,$replyTo,$errorCode,$error,$args,$meta);
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        return empty($o) ? new class(){} : $o;
    }
}

class ValidateRule implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $validator=null,
        /** @var string|null */
        public ?string $condition=null,
        /** @var string|null */
        public ?string $errorCode=null,
        /** @var string|null */
        public ?string $message=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['validator'])) $this->validator = $o['validator'];
        if (isset($o['condition'])) $this->condition = $o['condition'];
        if (isset($o['errorCode'])) $this->errorCode = $o['errorCode'];
        if (isset($o['message'])) $this->message = $o['message'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->validator)) $o['validator'] = $this->validator;
        if (isset($this->condition)) $o['condition'] = $this->condition;
        if (isset($this->errorCode)) $o['errorCode'] = $this->errorCode;
        if (isset($this->message)) $o['message'] = $this->message;
        return empty($o) ? new class(){} : $o;
    }
}

class ValidationRule extends ValidateRule implements JsonSerializable
{
    /**
     * @param string|null $validator
     * @param string|null $condition
     * @param string|null $errorCode
     * @param string|null $message
     */
    public function __construct(
        ?string $validator=null,
        ?string $condition=null,
        ?string $errorCode=null,
        ?string $message=null,
        /** @var int */
        public int $id=0,
        // @Required()
        /** @var string */
        public string $type='',

        /** @var string|null */
        public ?string $field=null,
        /** @var string|null */
        public ?string $createdBy=null,
        /** @var DateTime|null */
        public ?DateTime $createdDate=null,
        /** @var string|null */
        public ?string $modifiedBy=null,
        /** @var DateTime|null */
        public ?DateTime $modifiedDate=null,
        /** @var string|null */
        public ?string $suspendedBy=null,
        /** @var DateTime|null */
        public ?DateTime $suspendedDate=null,
        /** @var string|null */
        public ?string $notes=null
    ) {
        parent::__construct($validator,$condition,$errorCode,$message);
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['field'])) $this->field = $o['field'];
        if (isset($o['createdBy'])) $this->createdBy = $o['createdBy'];
        if (isset($o['createdDate'])) $this->createdDate = JsonConverters::from('DateTime', $o['createdDate']);
        if (isset($o['modifiedBy'])) $this->modifiedBy = $o['modifiedBy'];
        if (isset($o['modifiedDate'])) $this->modifiedDate = JsonConverters::from('DateTime', $o['modifiedDate']);
        if (isset($o['suspendedBy'])) $this->suspendedBy = $o['suspendedBy'];
        if (isset($o['suspendedDate'])) $this->suspendedDate = JsonConverters::from('DateTime', $o['suspendedDate']);
        if (isset($o['notes'])) $this->notes = $o['notes'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->field)) $o['field'] = $this->field;
        if (isset($this->createdBy)) $o['createdBy'] = $this->createdBy;
        if (isset($this->createdDate)) $o['createdDate'] = JsonConverters::to('DateTime', $this->createdDate);
        if (isset($this->modifiedBy)) $o['modifiedBy'] = $this->modifiedBy;
        if (isset($this->modifiedDate)) $o['modifiedDate'] = JsonConverters::to('DateTime', $this->modifiedDate);
        if (isset($this->suspendedBy)) $o['suspendedBy'] = $this->suspendedBy;
        if (isset($this->suspendedDate)) $o['suspendedDate'] = JsonConverters::to('DateTime', $this->suspendedDate);
        if (isset($this->notes)) $o['notes'] = $this->notes;
        return empty($o) ? new class(){} : $o;
    }
}

class AppInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $baseUrl=null,
        /** @var string|null */
        public ?string $serviceStackVersion=null,
        /** @var string|null */
        public ?string $serviceName=null,
        /** @var string|null */
        public ?string $apiVersion=null,
        /** @var string|null */
        public ?string $serviceDescription=null,
        /** @var string|null */
        public ?string $serviceIconUrl=null,
        /** @var string|null */
        public ?string $brandUrl=null,
        /** @var string|null */
        public ?string $brandImageUrl=null,
        /** @var string|null */
        public ?string $textColor=null,
        /** @var string|null */
        public ?string $linkColor=null,
        /** @var string|null */
        public ?string $backgroundColor=null,
        /** @var string|null */
        public ?string $backgroundImageUrl=null,
        /** @var string|null */
        public ?string $iconUrl=null,
        /** @var string|null */
        public ?string $jsTextCase=null,
        /** @var string|null */
        public ?string $useSystemJson=null,
        /** @var array<string>|null */
        public ?array $endpointRouting=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['baseUrl'])) $this->baseUrl = $o['baseUrl'];
        if (isset($o['serviceStackVersion'])) $this->serviceStackVersion = $o['serviceStackVersion'];
        if (isset($o['serviceName'])) $this->serviceName = $o['serviceName'];
        if (isset($o['apiVersion'])) $this->apiVersion = $o['apiVersion'];
        if (isset($o['serviceDescription'])) $this->serviceDescription = $o['serviceDescription'];
        if (isset($o['serviceIconUrl'])) $this->serviceIconUrl = $o['serviceIconUrl'];
        if (isset($o['brandUrl'])) $this->brandUrl = $o['brandUrl'];
        if (isset($o['brandImageUrl'])) $this->brandImageUrl = $o['brandImageUrl'];
        if (isset($o['textColor'])) $this->textColor = $o['textColor'];
        if (isset($o['linkColor'])) $this->linkColor = $o['linkColor'];
        if (isset($o['backgroundColor'])) $this->backgroundColor = $o['backgroundColor'];
        if (isset($o['backgroundImageUrl'])) $this->backgroundImageUrl = $o['backgroundImageUrl'];
        if (isset($o['iconUrl'])) $this->iconUrl = $o['iconUrl'];
        if (isset($o['jsTextCase'])) $this->jsTextCase = $o['jsTextCase'];
        if (isset($o['useSystemJson'])) $this->useSystemJson = $o['useSystemJson'];
        if (isset($o['endpointRouting'])) $this->endpointRouting = JsonConverters::fromArray('string', $o['endpointRouting']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->baseUrl)) $o['baseUrl'] = $this->baseUrl;
        if (isset($this->serviceStackVersion)) $o['serviceStackVersion'] = $this->serviceStackVersion;
        if (isset($this->serviceName)) $o['serviceName'] = $this->serviceName;
        if (isset($this->apiVersion)) $o['apiVersion'] = $this->apiVersion;
        if (isset($this->serviceDescription)) $o['serviceDescription'] = $this->serviceDescription;
        if (isset($this->serviceIconUrl)) $o['serviceIconUrl'] = $this->serviceIconUrl;
        if (isset($this->brandUrl)) $o['brandUrl'] = $this->brandUrl;
        if (isset($this->brandImageUrl)) $o['brandImageUrl'] = $this->brandImageUrl;
        if (isset($this->textColor)) $o['textColor'] = $this->textColor;
        if (isset($this->linkColor)) $o['linkColor'] = $this->linkColor;
        if (isset($this->backgroundColor)) $o['backgroundColor'] = $this->backgroundColor;
        if (isset($this->backgroundImageUrl)) $o['backgroundImageUrl'] = $this->backgroundImageUrl;
        if (isset($this->iconUrl)) $o['iconUrl'] = $this->iconUrl;
        if (isset($this->jsTextCase)) $o['jsTextCase'] = $this->jsTextCase;
        if (isset($this->useSystemJson)) $o['useSystemJson'] = $this->useSystemJson;
        if (isset($this->endpointRouting)) $o['endpointRouting'] = JsonConverters::toArray('string', $this->endpointRouting);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class ImageInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $svg=null,
        /** @var string|null */
        public ?string $uri=null,
        /** @var string|null */
        public ?string $alt=null,
        /** @var string|null */
        public ?string $cls=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['svg'])) $this->svg = $o['svg'];
        if (isset($o['uri'])) $this->uri = $o['uri'];
        if (isset($o['alt'])) $this->alt = $o['alt'];
        if (isset($o['cls'])) $this->cls = $o['cls'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->svg)) $o['svg'] = $this->svg;
        if (isset($this->uri)) $o['uri'] = $this->uri;
        if (isset($this->alt)) $o['alt'] = $this->alt;
        if (isset($this->cls)) $o['cls'] = $this->cls;
        return empty($o) ? new class(){} : $o;
    }
}

class LinkInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $id=null,
        /** @var string|null */
        public ?string $href=null,
        /** @var string|null */
        public ?string $label=null,
        /** @var ImageInfo|null */
        public ?ImageInfo $icon=null,
        /** @var string|null */
        public ?string $show=null,
        /** @var string|null */
        public ?string $hide=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['href'])) $this->href = $o['href'];
        if (isset($o['label'])) $this->label = $o['label'];
        if (isset($o['icon'])) $this->icon = JsonConverters::from('ImageInfo', $o['icon']);
        if (isset($o['show'])) $this->show = $o['show'];
        if (isset($o['hide'])) $this->hide = $o['hide'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->href)) $o['href'] = $this->href;
        if (isset($this->label)) $o['label'] = $this->label;
        if (isset($this->icon)) $o['icon'] = JsonConverters::to('ImageInfo', $this->icon);
        if (isset($this->show)) $o['show'] = $this->show;
        if (isset($this->hide)) $o['hide'] = $this->hide;
        return empty($o) ? new class(){} : $o;
    }
}

class ThemeInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $form=null,
        /** @var ImageInfo|null */
        public ?ImageInfo $modelIcon=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['form'])) $this->form = $o['form'];
        if (isset($o['modelIcon'])) $this->modelIcon = JsonConverters::from('ImageInfo', $o['modelIcon']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->form)) $o['form'] = $this->form;
        if (isset($this->modelIcon)) $o['modelIcon'] = JsonConverters::to('ImageInfo', $this->modelIcon);
        return empty($o) ? new class(){} : $o;
    }
}

class ApiCss implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $form=null,
        /** @var string|null */
        public ?string $fieldset=null,
        /** @var string|null */
        public ?string $field=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['form'])) $this->form = $o['form'];
        if (isset($o['fieldset'])) $this->fieldset = $o['fieldset'];
        if (isset($o['field'])) $this->field = $o['field'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->form)) $o['form'] = $this->form;
        if (isset($this->fieldset)) $o['fieldset'] = $this->fieldset;
        if (isset($this->field)) $o['field'] = $this->field;
        return empty($o) ? new class(){} : $o;
    }
}

class AppTags implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $default=null,
        /** @var string|null */
        public ?string $other=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['default'])) $this->default = $o['default'];
        if (isset($o['other'])) $this->other = $o['other'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->default)) $o['default'] = $this->default;
        if (isset($this->other)) $o['other'] = $this->other;
        return empty($o) ? new class(){} : $o;
    }
}

class LocodeUi implements JsonSerializable
{
    public function __construct(
        /** @var ApiCss|null */
        public ?ApiCss $css=null,
        /** @var AppTags|null */
        public ?AppTags $tags=null,
        /** @var int */
        public int $maxFieldLength=0,
        /** @var int */
        public int $maxNestedFields=0,
        /** @var int */
        public int $maxNestedFieldLength=0
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['css'])) $this->css = JsonConverters::from('ApiCss', $o['css']);
        if (isset($o['tags'])) $this->tags = JsonConverters::from('AppTags', $o['tags']);
        if (isset($o['maxFieldLength'])) $this->maxFieldLength = $o['maxFieldLength'];
        if (isset($o['maxNestedFields'])) $this->maxNestedFields = $o['maxNestedFields'];
        if (isset($o['maxNestedFieldLength'])) $this->maxNestedFieldLength = $o['maxNestedFieldLength'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->css)) $o['css'] = JsonConverters::to('ApiCss', $this->css);
        if (isset($this->tags)) $o['tags'] = JsonConverters::to('AppTags', $this->tags);
        if (isset($this->maxFieldLength)) $o['maxFieldLength'] = $this->maxFieldLength;
        if (isset($this->maxNestedFields)) $o['maxNestedFields'] = $this->maxNestedFields;
        if (isset($this->maxNestedFieldLength)) $o['maxNestedFieldLength'] = $this->maxNestedFieldLength;
        return empty($o) ? new class(){} : $o;
    }
}

class ExplorerUi implements JsonSerializable
{
    public function __construct(
        /** @var ApiCss|null */
        public ?ApiCss $css=null,
        /** @var AppTags|null */
        public ?AppTags $tags=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['css'])) $this->css = JsonConverters::from('ApiCss', $o['css']);
        if (isset($o['tags'])) $this->tags = JsonConverters::from('AppTags', $o['tags']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->css)) $o['css'] = JsonConverters::to('ApiCss', $this->css);
        if (isset($this->tags)) $o['tags'] = JsonConverters::to('AppTags', $this->tags);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminUi implements JsonSerializable
{
    public function __construct(
        /** @var ApiCss|null */
        public ?ApiCss $css=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['css'])) $this->css = JsonConverters::from('ApiCss', $o['css']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->css)) $o['css'] = JsonConverters::to('ApiCss', $this->css);
        return empty($o) ? new class(){} : $o;
    }
}

class FormatInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $method=null,
        /** @var string|null */
        public ?string $options=null,
        /** @var string|null */
        public ?string $locale=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['method'])) $this->method = $o['method'];
        if (isset($o['options'])) $this->options = $o['options'];
        if (isset($o['locale'])) $this->locale = $o['locale'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->method)) $o['method'] = $this->method;
        if (isset($this->options)) $o['options'] = $this->options;
        if (isset($this->locale)) $o['locale'] = $this->locale;
        return empty($o) ? new class(){} : $o;
    }
}

class ApiFormat implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $locale=null,
        /** @var bool|null */
        public ?bool $assumeUtc=null,
        /** @var FormatInfo|null */
        public ?FormatInfo $number=null,
        /** @var FormatInfo|null */
        public ?FormatInfo $date=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['locale'])) $this->locale = $o['locale'];
        if (isset($o['assumeUtc'])) $this->assumeUtc = $o['assumeUtc'];
        if (isset($o['number'])) $this->number = JsonConverters::from('FormatInfo', $o['number']);
        if (isset($o['date'])) $this->date = JsonConverters::from('FormatInfo', $o['date']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->locale)) $o['locale'] = $this->locale;
        if (isset($this->assumeUtc)) $o['assumeUtc'] = $this->assumeUtc;
        if (isset($this->number)) $o['number'] = JsonConverters::to('FormatInfo', $this->number);
        if (isset($this->date)) $o['date'] = JsonConverters::to('FormatInfo', $this->date);
        return empty($o) ? new class(){} : $o;
    }
}

class UiInfo implements JsonSerializable
{
    public function __construct(
        /** @var ImageInfo|null */
        public ?ImageInfo $brandIcon=null,
        /** @var array<string>|null */
        public ?array $hideTags=null,
        /** @var array<string>|null */
        public ?array $modules=null,
        /** @var array<string>|null */
        public ?array $alwaysHideTags=null,
        /** @var array<LinkInfo>|null */
        public ?array $adminLinks=null,
        /** @var ThemeInfo|null */
        public ?ThemeInfo $theme=null,
        /** @var LocodeUi|null */
        public ?LocodeUi $locode=null,
        /** @var ExplorerUi|null */
        public ?ExplorerUi $explorer=null,
        /** @var AdminUi|null */
        public ?AdminUi $admin=null,
        /** @var ApiFormat|null */
        public ?ApiFormat $defaultFormats=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['brandIcon'])) $this->brandIcon = JsonConverters::from('ImageInfo', $o['brandIcon']);
        if (isset($o['hideTags'])) $this->hideTags = JsonConverters::fromArray('string', $o['hideTags']);
        if (isset($o['modules'])) $this->modules = JsonConverters::fromArray('string', $o['modules']);
        if (isset($o['alwaysHideTags'])) $this->alwaysHideTags = JsonConverters::fromArray('string', $o['alwaysHideTags']);
        if (isset($o['adminLinks'])) $this->adminLinks = JsonConverters::fromArray('LinkInfo', $o['adminLinks']);
        if (isset($o['theme'])) $this->theme = JsonConverters::from('ThemeInfo', $o['theme']);
        if (isset($o['locode'])) $this->locode = JsonConverters::from('LocodeUi', $o['locode']);
        if (isset($o['explorer'])) $this->explorer = JsonConverters::from('ExplorerUi', $o['explorer']);
        if (isset($o['admin'])) $this->admin = JsonConverters::from('AdminUi', $o['admin']);
        if (isset($o['defaultFormats'])) $this->defaultFormats = JsonConverters::from('ApiFormat', $o['defaultFormats']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->brandIcon)) $o['brandIcon'] = JsonConverters::to('ImageInfo', $this->brandIcon);
        if (isset($this->hideTags)) $o['hideTags'] = JsonConverters::toArray('string', $this->hideTags);
        if (isset($this->modules)) $o['modules'] = JsonConverters::toArray('string', $this->modules);
        if (isset($this->alwaysHideTags)) $o['alwaysHideTags'] = JsonConverters::toArray('string', $this->alwaysHideTags);
        if (isset($this->adminLinks)) $o['adminLinks'] = JsonConverters::toArray('LinkInfo', $this->adminLinks);
        if (isset($this->theme)) $o['theme'] = JsonConverters::to('ThemeInfo', $this->theme);
        if (isset($this->locode)) $o['locode'] = JsonConverters::to('LocodeUi', $this->locode);
        if (isset($this->explorer)) $o['explorer'] = JsonConverters::to('ExplorerUi', $this->explorer);
        if (isset($this->admin)) $o['admin'] = JsonConverters::to('AdminUi', $this->admin);
        if (isset($this->defaultFormats)) $o['defaultFormats'] = JsonConverters::to('ApiFormat', $this->defaultFormats);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class ConfigInfo implements JsonSerializable
{
    public function __construct(
        /** @var bool|null */
        public ?bool $debugMode=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['debugMode'])) $this->debugMode = $o['debugMode'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->debugMode)) $o['debugMode'] = $this->debugMode;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class FieldCss implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $field=null,
        /** @var string|null */
        public ?string $input=null,
        /** @var string|null */
        public ?string $label=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['field'])) $this->field = $o['field'];
        if (isset($o['input'])) $this->input = $o['input'];
        if (isset($o['label'])) $this->label = $o['label'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->field)) $o['field'] = $this->field;
        if (isset($this->input)) $o['input'] = $this->input;
        if (isset($this->label)) $o['label'] = $this->label;
        return empty($o) ? new class(){} : $o;
    }
}

class InputInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $id=null,
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $type=null,
        /** @var string|null */
        public ?string $value=null,
        /** @var string|null */
        public ?string $placeholder=null,
        /** @var string|null */
        public ?string $help=null,
        /** @var string|null */
        public ?string $label=null,
        /** @var string|null */
        public ?string $title=null,
        /** @var string|null */
        public ?string $size=null,
        /** @var string|null */
        public ?string $pattern=null,
        /** @var bool|null */
        public ?bool $readOnly=null,
        /** @var bool|null */
        public ?bool $required=null,
        /** @var bool|null */
        public ?bool $disabled=null,
        /** @var string|null */
        public ?string $autocomplete=null,
        /** @var string|null */
        public ?string $autofocus=null,
        /** @var string|null */
        public ?string $min=null,
        /** @var string|null */
        public ?string $max=null,
        /** @var string|null */
        public ?string $step=null,
        /** @var int|null */
        public ?int $minLength=null,
        /** @var int|null */
        public ?int $maxLength=null,
        /** @var string|null */
        public ?string $accept=null,
        /** @var string|null */
        public ?string $capture=null,
        /** @var bool|null */
        public ?bool $multiple=null,
        /** @var string[]|null */
        public ?array $allowableValues=null,
        /** @var KeyValuePair[]2<string, string>|null */
        public ?KeyValuePair[]2 $allowableEntries=null,
        /** @var string|null */
        public ?string $options=null,
        /** @var bool|null */
        public ?bool $ignore=null,
        /** @var FieldCss|null */
        public ?FieldCss $css=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['value'])) $this->value = $o['value'];
        if (isset($o['placeholder'])) $this->placeholder = $o['placeholder'];
        if (isset($o['help'])) $this->help = $o['help'];
        if (isset($o['label'])) $this->label = $o['label'];
        if (isset($o['title'])) $this->title = $o['title'];
        if (isset($o['size'])) $this->size = $o['size'];
        if (isset($o['pattern'])) $this->pattern = $o['pattern'];
        if (isset($o['readOnly'])) $this->readOnly = $o['readOnly'];
        if (isset($o['required'])) $this->required = $o['required'];
        if (isset($o['disabled'])) $this->disabled = $o['disabled'];
        if (isset($o['autocomplete'])) $this->autocomplete = $o['autocomplete'];
        if (isset($o['autofocus'])) $this->autofocus = $o['autofocus'];
        if (isset($o['min'])) $this->min = $o['min'];
        if (isset($o['max'])) $this->max = $o['max'];
        if (isset($o['step'])) $this->step = $o['step'];
        if (isset($o['minLength'])) $this->minLength = $o['minLength'];
        if (isset($o['maxLength'])) $this->maxLength = $o['maxLength'];
        if (isset($o['accept'])) $this->accept = $o['accept'];
        if (isset($o['capture'])) $this->capture = $o['capture'];
        if (isset($o['multiple'])) $this->multiple = $o['multiple'];
        if (isset($o['allowableValues'])) $this->allowableValues = JsonConverters::fromArray('string', $o['allowableValues']);
        if (isset($o['allowableEntries'])) $this->allowableEntries = JsonConverters::from(JsonConverters::context('KeyValuePair[]2',genericArgs:['string','string']), $o['allowableEntries']);
        if (isset($o['options'])) $this->options = $o['options'];
        if (isset($o['ignore'])) $this->ignore = $o['ignore'];
        if (isset($o['css'])) $this->css = JsonConverters::from('FieldCss', $o['css']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->value)) $o['value'] = $this->value;
        if (isset($this->placeholder)) $o['placeholder'] = $this->placeholder;
        if (isset($this->help)) $o['help'] = $this->help;
        if (isset($this->label)) $o['label'] = $this->label;
        if (isset($this->title)) $o['title'] = $this->title;
        if (isset($this->size)) $o['size'] = $this->size;
        if (isset($this->pattern)) $o['pattern'] = $this->pattern;
        if (isset($this->readOnly)) $o['readOnly'] = $this->readOnly;
        if (isset($this->required)) $o['required'] = $this->required;
        if (isset($this->disabled)) $o['disabled'] = $this->disabled;
        if (isset($this->autocomplete)) $o['autocomplete'] = $this->autocomplete;
        if (isset($this->autofocus)) $o['autofocus'] = $this->autofocus;
        if (isset($this->min)) $o['min'] = $this->min;
        if (isset($this->max)) $o['max'] = $this->max;
        if (isset($this->step)) $o['step'] = $this->step;
        if (isset($this->minLength)) $o['minLength'] = $this->minLength;
        if (isset($this->maxLength)) $o['maxLength'] = $this->maxLength;
        if (isset($this->accept)) $o['accept'] = $this->accept;
        if (isset($this->capture)) $o['capture'] = $this->capture;
        if (isset($this->multiple)) $o['multiple'] = $this->multiple;
        if (isset($this->allowableValues)) $o['allowableValues'] = JsonConverters::toArray('string', $this->allowableValues);
        if (isset($this->allowableEntries)) $o['allowableEntries'] = JsonConverters::to(JsonConverters::context('KeyValuePair[]2',genericArgs:['string','string']), $this->allowableEntries);
        if (isset($this->options)) $o['options'] = $this->options;
        if (isset($this->ignore)) $o['ignore'] = $this->ignore;
        if (isset($this->css)) $o['css'] = JsonConverters::to('FieldCss', $this->css);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class MetaAuthProvider implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $label=null,
        /** @var string|null */
        public ?string $type=null,
        /** @var NavItem|null */
        public ?NavItem $navItem=null,
        /** @var ImageInfo|null */
        public ?ImageInfo $icon=null,
        /** @var array<InputInfo>|null */
        public ?array $formLayout=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['label'])) $this->label = $o['label'];
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['navItem'])) $this->navItem = JsonConverters::from('NavItem', $o['navItem']);
        if (isset($o['icon'])) $this->icon = JsonConverters::from('ImageInfo', $o['icon']);
        if (isset($o['formLayout'])) $this->formLayout = JsonConverters::fromArray('InputInfo', $o['formLayout']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->label)) $o['label'] = $this->label;
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->navItem)) $o['navItem'] = JsonConverters::to('NavItem', $this->navItem);
        if (isset($this->icon)) $o['icon'] = JsonConverters::to('ImageInfo', $this->icon);
        if (isset($this->formLayout)) $o['formLayout'] = JsonConverters::toArray('InputInfo', $this->formLayout);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class IdentityAuthInfo implements JsonSerializable
{
    public function __construct(
        /** @var bool|null */
        public ?bool $hasRefreshToken=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['hasRefreshToken'])) $this->hasRefreshToken = $o['hasRefreshToken'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->hasRefreshToken)) $o['hasRefreshToken'] = $this->hasRefreshToken;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class AuthInfo implements JsonSerializable
{
    public function __construct(
        /** @var bool|null */
        public ?bool $hasAuthSecret=null,
        /** @var bool|null */
        public ?bool $hasAuthRepository=null,
        /** @var bool|null */
        public ?bool $includesRoles=null,
        /** @var bool|null */
        public ?bool $includesOAuthTokens=null,
        /** @var string|null */
        public ?string $htmlRedirect=null,
        /** @var array<MetaAuthProvider>|null */
        public ?array $authProviders=null,
        /** @var IdentityAuthInfo|null */
        public ?IdentityAuthInfo $identityAuth=null,
        /** @var array<string,LinkInfo[]>|null */
        public ?array $roleLinks=null,
        /** @var array<string,string[]>|null */
        public ?array $serviceRoutes=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['hasAuthSecret'])) $this->hasAuthSecret = $o['hasAuthSecret'];
        if (isset($o['hasAuthRepository'])) $this->hasAuthRepository = $o['hasAuthRepository'];
        if (isset($o['includesRoles'])) $this->includesRoles = $o['includesRoles'];
        if (isset($o['includesOAuthTokens'])) $this->includesOAuthTokens = $o['includesOAuthTokens'];
        if (isset($o['htmlRedirect'])) $this->htmlRedirect = $o['htmlRedirect'];
        if (isset($o['authProviders'])) $this->authProviders = JsonConverters::fromArray('MetaAuthProvider', $o['authProviders']);
        if (isset($o['identityAuth'])) $this->identityAuth = JsonConverters::from('IdentityAuthInfo', $o['identityAuth']);
        if (isset($o['roleLinks'])) $this->roleLinks = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','List<LinkInfo>']), $o['roleLinks']);
        if (isset($o['serviceRoutes'])) $this->serviceRoutes = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $o['serviceRoutes']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->hasAuthSecret)) $o['hasAuthSecret'] = $this->hasAuthSecret;
        if (isset($this->hasAuthRepository)) $o['hasAuthRepository'] = $this->hasAuthRepository;
        if (isset($this->includesRoles)) $o['includesRoles'] = $this->includesRoles;
        if (isset($this->includesOAuthTokens)) $o['includesOAuthTokens'] = $this->includesOAuthTokens;
        if (isset($this->htmlRedirect)) $o['htmlRedirect'] = $this->htmlRedirect;
        if (isset($this->authProviders)) $o['authProviders'] = JsonConverters::toArray('MetaAuthProvider', $this->authProviders);
        if (isset($this->identityAuth)) $o['identityAuth'] = JsonConverters::to('IdentityAuthInfo', $this->identityAuth);
        if (isset($this->roleLinks)) $o['roleLinks'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','List<LinkInfo>']), $this->roleLinks);
        if (isset($this->serviceRoutes)) $o['serviceRoutes'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $this->serviceRoutes);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class ApiKeyInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $label=null,
        /** @var string|null */
        public ?string $httpHeader=null,
        /** @var array<string>|null */
        public ?array $scopes=null,
        /** @var array<string>|null */
        public ?array $features=null,
        /** @var array<string>|null */
        public ?array $requestTypes=null,
        /** @var array<KeyValuePair<string,string>>|null */
        public ?array $expiresIn=null,
        /** @var array<string>|null */
        public ?array $hide=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['label'])) $this->label = $o['label'];
        if (isset($o['httpHeader'])) $this->httpHeader = $o['httpHeader'];
        if (isset($o['scopes'])) $this->scopes = JsonConverters::fromArray('string', $o['scopes']);
        if (isset($o['features'])) $this->features = JsonConverters::fromArray('string', $o['features']);
        if (isset($o['requestTypes'])) $this->requestTypes = JsonConverters::fromArray('string', $o['requestTypes']);
        if (isset($o['expiresIn'])) $this->expiresIn = JsonConverters::fromArray('KeyValuePair<String,String>', $o['expiresIn']);
        if (isset($o['hide'])) $this->hide = JsonConverters::fromArray('string', $o['hide']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->label)) $o['label'] = $this->label;
        if (isset($this->httpHeader)) $o['httpHeader'] = $this->httpHeader;
        if (isset($this->scopes)) $o['scopes'] = JsonConverters::toArray('string', $this->scopes);
        if (isset($this->features)) $o['features'] = JsonConverters::toArray('string', $this->features);
        if (isset($this->requestTypes)) $o['requestTypes'] = JsonConverters::toArray('string', $this->requestTypes);
        if (isset($this->expiresIn)) $o['expiresIn'] = JsonConverters::toArray('KeyValuePair<String,String>', $this->expiresIn);
        if (isset($this->hide)) $o['hide'] = JsonConverters::toArray('string', $this->hide);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataTypeName implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $namespace=null,
        /** @var string[]|null */
        public ?array $genericArgs=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['namespace'])) $this->namespace = $o['namespace'];
        if (isset($o['genericArgs'])) $this->genericArgs = JsonConverters::fromArray('string', $o['genericArgs']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->namespace)) $o['namespace'] = $this->namespace;
        if (isset($this->genericArgs)) $o['genericArgs'] = JsonConverters::toArray('string', $this->genericArgs);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataDataContract implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $namespace=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['namespace'])) $this->namespace = $o['namespace'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->namespace)) $o['namespace'] = $this->namespace;
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataDataMember implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var int|null */
        public ?int $order=null,
        /** @var bool|null */
        public ?bool $isRequired=null,
        /** @var bool|null */
        public ?bool $emitDefaultValue=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['order'])) $this->order = $o['order'];
        if (isset($o['isRequired'])) $this->isRequired = $o['isRequired'];
        if (isset($o['emitDefaultValue'])) $this->emitDefaultValue = $o['emitDefaultValue'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->order)) $o['order'] = $this->order;
        if (isset($this->isRequired)) $o['isRequired'] = $this->isRequired;
        if (isset($this->emitDefaultValue)) $o['emitDefaultValue'] = $this->emitDefaultValue;
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataAttribute implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var array<MetadataPropertyType>|null */
        public ?array $constructorArgs=null,
        /** @var array<MetadataPropertyType>|null */
        public ?array $args=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['constructorArgs'])) $this->constructorArgs = JsonConverters::fromArray('MetadataPropertyType', $o['constructorArgs']);
        if (isset($o['args'])) $this->args = JsonConverters::fromArray('MetadataPropertyType', $o['args']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->constructorArgs)) $o['constructorArgs'] = JsonConverters::toArray('MetadataPropertyType', $this->constructorArgs);
        if (isset($this->args)) $o['args'] = JsonConverters::toArray('MetadataPropertyType', $this->args);
        return empty($o) ? new class(){} : $o;
    }
}

class RefInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $model=null,
        /** @var string|null */
        public ?string $selfId=null,
        /** @var string|null */
        public ?string $refId=null,
        /** @var string|null */
        public ?string $refLabel=null,
        /** @var string|null */
        public ?string $queryApi=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['model'])) $this->model = $o['model'];
        if (isset($o['selfId'])) $this->selfId = $o['selfId'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['refLabel'])) $this->refLabel = $o['refLabel'];
        if (isset($o['queryApi'])) $this->queryApi = $o['queryApi'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->model)) $o['model'] = $this->model;
        if (isset($this->selfId)) $o['selfId'] = $this->selfId;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->refLabel)) $o['refLabel'] = $this->refLabel;
        if (isset($this->queryApi)) $o['queryApi'] = $this->queryApi;
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataPropertyType implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $type=null,
        /** @var string|null */
        public ?string $namespace=null,
        /** @var bool|null */
        public ?bool $isValueType=null,
        /** @var bool|null */
        public ?bool $isEnum=null,
        /** @var bool|null */
        public ?bool $isPrimaryKey=null,
        /** @var string[]|null */
        public ?array $genericArgs=null,
        /** @var string|null */
        public ?string $value=null,
        /** @var string|null */
        public ?string $description=null,
        /** @var MetadataDataMember|null */
        public ?MetadataDataMember $dataMember=null,
        /** @var bool|null */
        public ?bool $readOnly=null,
        /** @var string|null */
        public ?string $paramType=null,
        /** @var string|null */
        public ?string $displayType=null,
        /** @var bool|null */
        public ?bool $isRequired=null,
        /** @var string[]|null */
        public ?array $allowableValues=null,
        /** @var int|null */
        public ?int $allowableMin=null,
        /** @var int|null */
        public ?int $allowableMax=null,
        /** @var array<MetadataAttribute>|null */
        public ?array $attributes=null,
        /** @var string|null */
        public ?string $uploadTo=null,
        /** @var InputInfo|null */
        public ?InputInfo $input=null,
        /** @var FormatInfo|null */
        public ?FormatInfo $format=null,
        /** @var RefInfo|null */
        public ?RefInfo $ref=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['namespace'])) $this->namespace = $o['namespace'];
        if (isset($o['isValueType'])) $this->isValueType = $o['isValueType'];
        if (isset($o['isEnum'])) $this->isEnum = $o['isEnum'];
        if (isset($o['isPrimaryKey'])) $this->isPrimaryKey = $o['isPrimaryKey'];
        if (isset($o['genericArgs'])) $this->genericArgs = JsonConverters::fromArray('string', $o['genericArgs']);
        if (isset($o['value'])) $this->value = $o['value'];
        if (isset($o['description'])) $this->description = $o['description'];
        if (isset($o['dataMember'])) $this->dataMember = JsonConverters::from('MetadataDataMember', $o['dataMember']);
        if (isset($o['readOnly'])) $this->readOnly = $o['readOnly'];
        if (isset($o['paramType'])) $this->paramType = $o['paramType'];
        if (isset($o['displayType'])) $this->displayType = $o['displayType'];
        if (isset($o['isRequired'])) $this->isRequired = $o['isRequired'];
        if (isset($o['allowableValues'])) $this->allowableValues = JsonConverters::fromArray('string', $o['allowableValues']);
        if (isset($o['allowableMin'])) $this->allowableMin = $o['allowableMin'];
        if (isset($o['allowableMax'])) $this->allowableMax = $o['allowableMax'];
        if (isset($o['attributes'])) $this->attributes = JsonConverters::fromArray('MetadataAttribute', $o['attributes']);
        if (isset($o['uploadTo'])) $this->uploadTo = $o['uploadTo'];
        if (isset($o['input'])) $this->input = JsonConverters::from('InputInfo', $o['input']);
        if (isset($o['format'])) $this->format = JsonConverters::from('FormatInfo', $o['format']);
        if (isset($o['ref'])) $this->ref = JsonConverters::from('RefInfo', $o['ref']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->namespace)) $o['namespace'] = $this->namespace;
        if (isset($this->isValueType)) $o['isValueType'] = $this->isValueType;
        if (isset($this->isEnum)) $o['isEnum'] = $this->isEnum;
        if (isset($this->isPrimaryKey)) $o['isPrimaryKey'] = $this->isPrimaryKey;
        if (isset($this->genericArgs)) $o['genericArgs'] = JsonConverters::toArray('string', $this->genericArgs);
        if (isset($this->value)) $o['value'] = $this->value;
        if (isset($this->description)) $o['description'] = $this->description;
        if (isset($this->dataMember)) $o['dataMember'] = JsonConverters::to('MetadataDataMember', $this->dataMember);
        if (isset($this->readOnly)) $o['readOnly'] = $this->readOnly;
        if (isset($this->paramType)) $o['paramType'] = $this->paramType;
        if (isset($this->displayType)) $o['displayType'] = $this->displayType;
        if (isset($this->isRequired)) $o['isRequired'] = $this->isRequired;
        if (isset($this->allowableValues)) $o['allowableValues'] = JsonConverters::toArray('string', $this->allowableValues);
        if (isset($this->allowableMin)) $o['allowableMin'] = $this->allowableMin;
        if (isset($this->allowableMax)) $o['allowableMax'] = $this->allowableMax;
        if (isset($this->attributes)) $o['attributes'] = JsonConverters::toArray('MetadataAttribute', $this->attributes);
        if (isset($this->uploadTo)) $o['uploadTo'] = $this->uploadTo;
        if (isset($this->input)) $o['input'] = JsonConverters::to('InputInfo', $this->input);
        if (isset($this->format)) $o['format'] = JsonConverters::to('FormatInfo', $this->format);
        if (isset($this->ref)) $o['ref'] = JsonConverters::to('RefInfo', $this->ref);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataType implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $namespace=null,
        /** @var string[]|null */
        public ?array $genericArgs=null,
        /** @var MetadataTypeName|null */
        public ?MetadataTypeName $inherits=null,
        /** @var MetadataTypeName[]|null */
        public ?array $implements=null,
        /** @var string|null */
        public ?string $displayType=null,
        /** @var string|null */
        public ?string $description=null,
        /** @var string|null */
        public ?string $notes=null,
        /** @var ImageInfo|null */
        public ?ImageInfo $icon=null,
        /** @var bool|null */
        public ?bool $isNested=null,
        /** @var bool|null */
        public ?bool $isEnum=null,
        /** @var bool|null */
        public ?bool $isEnumInt=null,
        /** @var bool|null */
        public ?bool $isInterface=null,
        /** @var bool|null */
        public ?bool $isAbstract=null,
        /** @var bool|null */
        public ?bool $isGenericTypeDef=null,
        /** @var MetadataDataContract|null */
        public ?MetadataDataContract $dataContract=null,
        /** @var array<MetadataPropertyType>|null */
        public ?array $properties=null,
        /** @var array<MetadataAttribute>|null */
        public ?array $attributes=null,
        /** @var array<MetadataTypeName>|null */
        public ?array $innerTypes=null,
        /** @var array<string>|null */
        public ?array $enumNames=null,
        /** @var array<string>|null */
        public ?array $enumValues=null,
        /** @var array<string>|null */
        public ?array $enumMemberValues=null,
        /** @var array<string>|null */
        public ?array $enumDescriptions=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['namespace'])) $this->namespace = $o['namespace'];
        if (isset($o['genericArgs'])) $this->genericArgs = JsonConverters::fromArray('string', $o['genericArgs']);
        if (isset($o['inherits'])) $this->inherits = JsonConverters::from('MetadataTypeName', $o['inherits']);
        if (isset($o['implements'])) $this->implements = JsonConverters::fromArray('MetadataTypeName', $o['implements']);
        if (isset($o['displayType'])) $this->displayType = $o['displayType'];
        if (isset($o['description'])) $this->description = $o['description'];
        if (isset($o['notes'])) $this->notes = $o['notes'];
        if (isset($o['icon'])) $this->icon = JsonConverters::from('ImageInfo', $o['icon']);
        if (isset($o['isNested'])) $this->isNested = $o['isNested'];
        if (isset($o['isEnum'])) $this->isEnum = $o['isEnum'];
        if (isset($o['isEnumInt'])) $this->isEnumInt = $o['isEnumInt'];
        if (isset($o['isInterface'])) $this->isInterface = $o['isInterface'];
        if (isset($o['isAbstract'])) $this->isAbstract = $o['isAbstract'];
        if (isset($o['isGenericTypeDef'])) $this->isGenericTypeDef = $o['isGenericTypeDef'];
        if (isset($o['dataContract'])) $this->dataContract = JsonConverters::from('MetadataDataContract', $o['dataContract']);
        if (isset($o['properties'])) $this->properties = JsonConverters::fromArray('MetadataPropertyType', $o['properties']);
        if (isset($o['attributes'])) $this->attributes = JsonConverters::fromArray('MetadataAttribute', $o['attributes']);
        if (isset($o['innerTypes'])) $this->innerTypes = JsonConverters::fromArray('MetadataTypeName', $o['innerTypes']);
        if (isset($o['enumNames'])) $this->enumNames = JsonConverters::fromArray('string', $o['enumNames']);
        if (isset($o['enumValues'])) $this->enumValues = JsonConverters::fromArray('string', $o['enumValues']);
        if (isset($o['enumMemberValues'])) $this->enumMemberValues = JsonConverters::fromArray('string', $o['enumMemberValues']);
        if (isset($o['enumDescriptions'])) $this->enumDescriptions = JsonConverters::fromArray('string', $o['enumDescriptions']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->namespace)) $o['namespace'] = $this->namespace;
        if (isset($this->genericArgs)) $o['genericArgs'] = JsonConverters::toArray('string', $this->genericArgs);
        if (isset($this->inherits)) $o['inherits'] = JsonConverters::to('MetadataTypeName', $this->inherits);
        if (isset($this->implements)) $o['implements'] = JsonConverters::toArray('MetadataTypeName', $this->implements);
        if (isset($this->displayType)) $o['displayType'] = $this->displayType;
        if (isset($this->description)) $o['description'] = $this->description;
        if (isset($this->notes)) $o['notes'] = $this->notes;
        if (isset($this->icon)) $o['icon'] = JsonConverters::to('ImageInfo', $this->icon);
        if (isset($this->isNested)) $o['isNested'] = $this->isNested;
        if (isset($this->isEnum)) $o['isEnum'] = $this->isEnum;
        if (isset($this->isEnumInt)) $o['isEnumInt'] = $this->isEnumInt;
        if (isset($this->isInterface)) $o['isInterface'] = $this->isInterface;
        if (isset($this->isAbstract)) $o['isAbstract'] = $this->isAbstract;
        if (isset($this->isGenericTypeDef)) $o['isGenericTypeDef'] = $this->isGenericTypeDef;
        if (isset($this->dataContract)) $o['dataContract'] = JsonConverters::to('MetadataDataContract', $this->dataContract);
        if (isset($this->properties)) $o['properties'] = JsonConverters::toArray('MetadataPropertyType', $this->properties);
        if (isset($this->attributes)) $o['attributes'] = JsonConverters::toArray('MetadataAttribute', $this->attributes);
        if (isset($this->innerTypes)) $o['innerTypes'] = JsonConverters::toArray('MetadataTypeName', $this->innerTypes);
        if (isset($this->enumNames)) $o['enumNames'] = JsonConverters::toArray('string', $this->enumNames);
        if (isset($this->enumValues)) $o['enumValues'] = JsonConverters::toArray('string', $this->enumValues);
        if (isset($this->enumMemberValues)) $o['enumMemberValues'] = JsonConverters::toArray('string', $this->enumMemberValues);
        if (isset($this->enumDescriptions)) $o['enumDescriptions'] = JsonConverters::toArray('string', $this->enumDescriptions);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class CommandInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $tag=null,
        /** @var MetadataType|null */
        public ?MetadataType $request=null,
        /** @var MetadataType|null */
        public ?MetadataType $response=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['tag'])) $this->tag = $o['tag'];
        if (isset($o['request'])) $this->request = JsonConverters::from('MetadataType', $o['request']);
        if (isset($o['response'])) $this->response = JsonConverters::from('MetadataType', $o['response']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->tag)) $o['tag'] = $this->tag;
        if (isset($this->request)) $o['request'] = JsonConverters::to('MetadataType', $this->request);
        if (isset($this->response)) $o['response'] = JsonConverters::to('MetadataType', $this->response);
        return empty($o) ? new class(){} : $o;
    }
}

class CommandsInfo implements JsonSerializable
{
    public function __construct(
        /** @var array<CommandInfo>|null */
        public ?array $commands=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['commands'])) $this->commands = JsonConverters::fromArray('CommandInfo', $o['commands']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->commands)) $o['commands'] = JsonConverters::toArray('CommandInfo', $this->commands);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class AutoQueryConvention implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $value=null,
        /** @var string|null */
        public ?string $types=null,
        /** @var string|null */
        public ?string $valueType=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['value'])) $this->value = $o['value'];
        if (isset($o['types'])) $this->types = $o['types'];
        if (isset($o['valueType'])) $this->valueType = $o['valueType'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->value)) $o['value'] = $this->value;
        if (isset($this->types)) $o['types'] = $this->types;
        if (isset($this->valueType)) $o['valueType'] = $this->valueType;
        return empty($o) ? new class(){} : $o;
    }
}

class AutoQueryInfo implements JsonSerializable
{
    public function __construct(
        /** @var int|null */
        public ?int $maxLimit=null,
        /** @var bool|null */
        public ?bool $untypedQueries=null,
        /** @var bool|null */
        public ?bool $rawSqlFilters=null,
        /** @var bool|null */
        public ?bool $autoQueryViewer=null,
        /** @var bool|null */
        public ?bool $async=null,
        /** @var bool|null */
        public ?bool $orderByPrimaryKey=null,
        /** @var bool|null */
        public ?bool $crudEvents=null,
        /** @var bool|null */
        public ?bool $crudEventsServices=null,
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var string|null */
        public ?string $namedConnection=null,
        /** @var array<AutoQueryConvention>|null */
        public ?array $viewerConventions=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['maxLimit'])) $this->maxLimit = $o['maxLimit'];
        if (isset($o['untypedQueries'])) $this->untypedQueries = $o['untypedQueries'];
        if (isset($o['rawSqlFilters'])) $this->rawSqlFilters = $o['rawSqlFilters'];
        if (isset($o['autoQueryViewer'])) $this->autoQueryViewer = $o['autoQueryViewer'];
        if (isset($o['async'])) $this->async = $o['async'];
        if (isset($o['orderByPrimaryKey'])) $this->orderByPrimaryKey = $o['orderByPrimaryKey'];
        if (isset($o['crudEvents'])) $this->crudEvents = $o['crudEvents'];
        if (isset($o['crudEventsServices'])) $this->crudEventsServices = $o['crudEventsServices'];
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['namedConnection'])) $this->namedConnection = $o['namedConnection'];
        if (isset($o['viewerConventions'])) $this->viewerConventions = JsonConverters::fromArray('AutoQueryConvention', $o['viewerConventions']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->maxLimit)) $o['maxLimit'] = $this->maxLimit;
        if (isset($this->untypedQueries)) $o['untypedQueries'] = $this->untypedQueries;
        if (isset($this->rawSqlFilters)) $o['rawSqlFilters'] = $this->rawSqlFilters;
        if (isset($this->autoQueryViewer)) $o['autoQueryViewer'] = $this->autoQueryViewer;
        if (isset($this->async)) $o['async'] = $this->async;
        if (isset($this->orderByPrimaryKey)) $o['orderByPrimaryKey'] = $this->orderByPrimaryKey;
        if (isset($this->crudEvents)) $o['crudEvents'] = $this->crudEvents;
        if (isset($this->crudEventsServices)) $o['crudEventsServices'] = $this->crudEventsServices;
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->namedConnection)) $o['namedConnection'] = $this->namedConnection;
        if (isset($this->viewerConventions)) $o['viewerConventions'] = JsonConverters::toArray('AutoQueryConvention', $this->viewerConventions);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class ScriptMethodType implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string[]|null */
        public ?array $paramNames=null,
        /** @var string[]|null */
        public ?array $paramTypes=null,
        /** @var string|null */
        public ?string $returnType=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['paramNames'])) $this->paramNames = JsonConverters::fromArray('string', $o['paramNames']);
        if (isset($o['paramTypes'])) $this->paramTypes = JsonConverters::fromArray('string', $o['paramTypes']);
        if (isset($o['returnType'])) $this->returnType = $o['returnType'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->paramNames)) $o['paramNames'] = JsonConverters::toArray('string', $this->paramNames);
        if (isset($this->paramTypes)) $o['paramTypes'] = JsonConverters::toArray('string', $this->paramTypes);
        if (isset($this->returnType)) $o['returnType'] = $this->returnType;
        return empty($o) ? new class(){} : $o;
    }
}

class ValidationInfo implements JsonSerializable
{
    public function __construct(
        /** @var bool|null */
        public ?bool $hasValidationSource=null,
        /** @var bool|null */
        public ?bool $hasValidationSourceAdmin=null,
        /** @var array<string,string[]>|null */
        public ?array $serviceRoutes=null,
        /** @var array<ScriptMethodType>|null */
        public ?array $typeValidators=null,
        /** @var array<ScriptMethodType>|null */
        public ?array $propertyValidators=null,
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['hasValidationSource'])) $this->hasValidationSource = $o['hasValidationSource'];
        if (isset($o['hasValidationSourceAdmin'])) $this->hasValidationSourceAdmin = $o['hasValidationSourceAdmin'];
        if (isset($o['serviceRoutes'])) $this->serviceRoutes = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $o['serviceRoutes']);
        if (isset($o['typeValidators'])) $this->typeValidators = JsonConverters::fromArray('ScriptMethodType', $o['typeValidators']);
        if (isset($o['propertyValidators'])) $this->propertyValidators = JsonConverters::fromArray('ScriptMethodType', $o['propertyValidators']);
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->hasValidationSource)) $o['hasValidationSource'] = $this->hasValidationSource;
        if (isset($this->hasValidationSourceAdmin)) $o['hasValidationSourceAdmin'] = $this->hasValidationSourceAdmin;
        if (isset($this->serviceRoutes)) $o['serviceRoutes'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $this->serviceRoutes);
        if (isset($this->typeValidators)) $o['typeValidators'] = JsonConverters::toArray('ScriptMethodType', $this->typeValidators);
        if (isset($this->propertyValidators)) $o['propertyValidators'] = JsonConverters::toArray('ScriptMethodType', $this->propertyValidators);
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class SharpPagesInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $apiPath=null,
        /** @var string|null */
        public ?string $scriptAdminRole=null,
        /** @var string|null */
        public ?string $metadataDebugAdminRole=null,
        /** @var bool|null */
        public ?bool $metadataDebug=null,
        /** @var bool|null */
        public ?bool $spaFallback=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['apiPath'])) $this->apiPath = $o['apiPath'];
        if (isset($o['scriptAdminRole'])) $this->scriptAdminRole = $o['scriptAdminRole'];
        if (isset($o['metadataDebugAdminRole'])) $this->metadataDebugAdminRole = $o['metadataDebugAdminRole'];
        if (isset($o['metadataDebug'])) $this->metadataDebug = $o['metadataDebug'];
        if (isset($o['spaFallback'])) $this->spaFallback = $o['spaFallback'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->apiPath)) $o['apiPath'] = $this->apiPath;
        if (isset($this->scriptAdminRole)) $o['scriptAdminRole'] = $this->scriptAdminRole;
        if (isset($this->metadataDebugAdminRole)) $o['metadataDebugAdminRole'] = $this->metadataDebugAdminRole;
        if (isset($this->metadataDebug)) $o['metadataDebug'] = $this->metadataDebug;
        if (isset($this->spaFallback)) $o['spaFallback'] = $this->spaFallback;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class RequestLogsInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var string|null */
        public ?string $requestLogger=null,
        /** @var int */
        public int $defaultLimit=0,
        /** @var array<string,string[]>|null */
        public ?array $serviceRoutes=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['requestLogger'])) $this->requestLogger = $o['requestLogger'];
        if (isset($o['defaultLimit'])) $this->defaultLimit = $o['defaultLimit'];
        if (isset($o['serviceRoutes'])) $this->serviceRoutes = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $o['serviceRoutes']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->requestLogger)) $o['requestLogger'] = $this->requestLogger;
        if (isset($this->defaultLimit)) $o['defaultLimit'] = $this->defaultLimit;
        if (isset($this->serviceRoutes)) $o['serviceRoutes'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $this->serviceRoutes);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class ProfilingInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var int */
        public int $defaultLimit=0,
        /** @var array<string>|null */
        public ?array $summaryFields=null,
        /** @var string|null */
        public ?string $tagLabel=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['defaultLimit'])) $this->defaultLimit = $o['defaultLimit'];
        if (isset($o['summaryFields'])) $this->summaryFields = JsonConverters::fromArray('string', $o['summaryFields']);
        if (isset($o['tagLabel'])) $this->tagLabel = $o['tagLabel'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->defaultLimit)) $o['defaultLimit'] = $this->defaultLimit;
        if (isset($this->summaryFields)) $o['summaryFields'] = JsonConverters::toArray('string', $this->summaryFields);
        if (isset($this->tagLabel)) $o['tagLabel'] = $this->tagLabel;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class FilesUploadLocation implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $name=null,
        /** @var string|null */
        public ?string $readAccessRole=null,
        /** @var string|null */
        public ?string $writeAccessRole=null,
        /** @var array<string>|null */
        public ?array $allowExtensions=null,
        /** @var string|null */
        public ?string $allowOperations=null,
        /** @var int|null */
        public ?int $maxFileCount=null,
        /** @var int|null */
        public ?int $minFileBytes=null,
        /** @var int|null */
        public ?int $maxFileBytes=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['readAccessRole'])) $this->readAccessRole = $o['readAccessRole'];
        if (isset($o['writeAccessRole'])) $this->writeAccessRole = $o['writeAccessRole'];
        if (isset($o['allowExtensions'])) $this->allowExtensions = JsonConverters::fromArray('string', $o['allowExtensions']);
        if (isset($o['allowOperations'])) $this->allowOperations = $o['allowOperations'];
        if (isset($o['maxFileCount'])) $this->maxFileCount = $o['maxFileCount'];
        if (isset($o['minFileBytes'])) $this->minFileBytes = $o['minFileBytes'];
        if (isset($o['maxFileBytes'])) $this->maxFileBytes = $o['maxFileBytes'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->readAccessRole)) $o['readAccessRole'] = $this->readAccessRole;
        if (isset($this->writeAccessRole)) $o['writeAccessRole'] = $this->writeAccessRole;
        if (isset($this->allowExtensions)) $o['allowExtensions'] = JsonConverters::toArray('string', $this->allowExtensions);
        if (isset($this->allowOperations)) $o['allowOperations'] = $this->allowOperations;
        if (isset($this->maxFileCount)) $o['maxFileCount'] = $this->maxFileCount;
        if (isset($this->minFileBytes)) $o['minFileBytes'] = $this->minFileBytes;
        if (isset($this->maxFileBytes)) $o['maxFileBytes'] = $this->maxFileBytes;
        return empty($o) ? new class(){} : $o;
    }
}

class FilesUploadInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $basePath=null,
        /** @var array<FilesUploadLocation>|null */
        public ?array $locations=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['basePath'])) $this->basePath = $o['basePath'];
        if (isset($o['locations'])) $this->locations = JsonConverters::fromArray('FilesUploadLocation', $o['locations']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->basePath)) $o['basePath'] = $this->basePath;
        if (isset($this->locations)) $o['locations'] = JsonConverters::toArray('FilesUploadLocation', $this->locations);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class MediaRule implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $size=null,
        /** @var string|null */
        public ?string $rule=null,
        /** @var string[]|null */
        public ?array $applyTo=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['size'])) $this->size = $o['size'];
        if (isset($o['rule'])) $this->rule = $o['rule'];
        if (isset($o['applyTo'])) $this->applyTo = JsonConverters::fromArray('string', $o['applyTo']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->size)) $o['size'] = $this->size;
        if (isset($this->rule)) $o['rule'] = $this->rule;
        if (isset($this->applyTo)) $o['applyTo'] = JsonConverters::toArray('string', $this->applyTo);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminUsersInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var array<string>|null */
        public ?array $enabled=null,
        /** @var MetadataType|null */
        public ?MetadataType $userAuth=null,
        /** @var array<string>|null */
        public ?array $allRoles=null,
        /** @var array<string>|null */
        public ?array $allPermissions=null,
        /** @var array<string>|null */
        public ?array $queryUserAuthProperties=null,
        /** @var array<MediaRule>|null */
        public ?array $queryMediaRules=null,
        /** @var array<InputInfo>|null */
        public ?array $formLayout=null,
        /** @var ApiCss|null */
        public ?ApiCss $css=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['enabled'])) $this->enabled = JsonConverters::fromArray('string', $o['enabled']);
        if (isset($o['userAuth'])) $this->userAuth = JsonConverters::from('MetadataType', $o['userAuth']);
        if (isset($o['allRoles'])) $this->allRoles = JsonConverters::fromArray('string', $o['allRoles']);
        if (isset($o['allPermissions'])) $this->allPermissions = JsonConverters::fromArray('string', $o['allPermissions']);
        if (isset($o['queryUserAuthProperties'])) $this->queryUserAuthProperties = JsonConverters::fromArray('string', $o['queryUserAuthProperties']);
        if (isset($o['queryMediaRules'])) $this->queryMediaRules = JsonConverters::fromArray('MediaRule', $o['queryMediaRules']);
        if (isset($o['formLayout'])) $this->formLayout = JsonConverters::fromArray('InputInfo', $o['formLayout']);
        if (isset($o['css'])) $this->css = JsonConverters::from('ApiCss', $o['css']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->enabled)) $o['enabled'] = JsonConverters::toArray('string', $this->enabled);
        if (isset($this->userAuth)) $o['userAuth'] = JsonConverters::to('MetadataType', $this->userAuth);
        if (isset($this->allRoles)) $o['allRoles'] = JsonConverters::toArray('string', $this->allRoles);
        if (isset($this->allPermissions)) $o['allPermissions'] = JsonConverters::toArray('string', $this->allPermissions);
        if (isset($this->queryUserAuthProperties)) $o['queryUserAuthProperties'] = JsonConverters::toArray('string', $this->queryUserAuthProperties);
        if (isset($this->queryMediaRules)) $o['queryMediaRules'] = JsonConverters::toArray('MediaRule', $this->queryMediaRules);
        if (isset($this->formLayout)) $o['formLayout'] = JsonConverters::toArray('InputInfo', $this->formLayout);
        if (isset($this->css)) $o['css'] = JsonConverters::to('ApiCss', $this->css);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminIdentityUsersInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var array<string>|null */
        public ?array $enabled=null,
        /** @var MetadataType|null */
        public ?MetadataType $identityUser=null,
        /** @var array<string>|null */
        public ?array $allRoles=null,
        /** @var array<string>|null */
        public ?array $allPermissions=null,
        /** @var array<string>|null */
        public ?array $queryIdentityUserProperties=null,
        /** @var array<MediaRule>|null */
        public ?array $queryMediaRules=null,
        /** @var array<InputInfo>|null */
        public ?array $formLayout=null,
        /** @var ApiCss|null */
        public ?ApiCss $css=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['enabled'])) $this->enabled = JsonConverters::fromArray('string', $o['enabled']);
        if (isset($o['identityUser'])) $this->identityUser = JsonConverters::from('MetadataType', $o['identityUser']);
        if (isset($o['allRoles'])) $this->allRoles = JsonConverters::fromArray('string', $o['allRoles']);
        if (isset($o['allPermissions'])) $this->allPermissions = JsonConverters::fromArray('string', $o['allPermissions']);
        if (isset($o['queryIdentityUserProperties'])) $this->queryIdentityUserProperties = JsonConverters::fromArray('string', $o['queryIdentityUserProperties']);
        if (isset($o['queryMediaRules'])) $this->queryMediaRules = JsonConverters::fromArray('MediaRule', $o['queryMediaRules']);
        if (isset($o['formLayout'])) $this->formLayout = JsonConverters::fromArray('InputInfo', $o['formLayout']);
        if (isset($o['css'])) $this->css = JsonConverters::from('ApiCss', $o['css']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->enabled)) $o['enabled'] = JsonConverters::toArray('string', $this->enabled);
        if (isset($this->identityUser)) $o['identityUser'] = JsonConverters::to('MetadataType', $this->identityUser);
        if (isset($this->allRoles)) $o['allRoles'] = JsonConverters::toArray('string', $this->allRoles);
        if (isset($this->allPermissions)) $o['allPermissions'] = JsonConverters::toArray('string', $this->allPermissions);
        if (isset($this->queryIdentityUserProperties)) $o['queryIdentityUserProperties'] = JsonConverters::toArray('string', $this->queryIdentityUserProperties);
        if (isset($this->queryMediaRules)) $o['queryMediaRules'] = JsonConverters::toArray('MediaRule', $this->queryMediaRules);
        if (isset($this->formLayout)) $o['formLayout'] = JsonConverters::toArray('InputInfo', $this->formLayout);
        if (isset($this->css)) $o['css'] = JsonConverters::to('ApiCss', $this->css);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminRedisInfo implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $queryLimit=0,
        /** @var array<int>|null */
        public ?array $databases=null,
        /** @var bool|null */
        public ?bool $modifiableConnection=null,
        /** @var RedisEndpointInfo|null */
        public ?RedisEndpointInfo $endpoint=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['queryLimit'])) $this->queryLimit = $o['queryLimit'];
        if (isset($o['databases'])) $this->databases = JsonConverters::fromArray('int', $o['databases']);
        if (isset($o['modifiableConnection'])) $this->modifiableConnection = $o['modifiableConnection'];
        if (isset($o['endpoint'])) $this->endpoint = JsonConverters::from('RedisEndpointInfo', $o['endpoint']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->queryLimit)) $o['queryLimit'] = $this->queryLimit;
        if (isset($this->databases)) $o['databases'] = JsonConverters::toArray('int', $this->databases);
        if (isset($this->modifiableConnection)) $o['modifiableConnection'] = $this->modifiableConnection;
        if (isset($this->endpoint)) $o['endpoint'] = JsonConverters::to('RedisEndpointInfo', $this->endpoint);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class SchemaInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $alias=null,
        /** @var string|null */
        public ?string $name=null,
        /** @var array<string>|null */
        public ?array $tables=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['alias'])) $this->alias = $o['alias'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['tables'])) $this->tables = JsonConverters::fromArray('string', $o['tables']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->alias)) $o['alias'] = $this->alias;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->tables)) $o['tables'] = JsonConverters::toArray('string', $this->tables);
        return empty($o) ? new class(){} : $o;
    }
}

class DatabaseInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $alias=null,
        /** @var string|null */
        public ?string $name=null,
        /** @var array<SchemaInfo>|null */
        public ?array $schemas=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['alias'])) $this->alias = $o['alias'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['schemas'])) $this->schemas = JsonConverters::fromArray('SchemaInfo', $o['schemas']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->alias)) $o['alias'] = $this->alias;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->schemas)) $o['schemas'] = JsonConverters::toArray('SchemaInfo', $this->schemas);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminDatabaseInfo implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $queryLimit=0,
        /** @var array<DatabaseInfo>|null */
        public ?array $databases=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['queryLimit'])) $this->queryLimit = $o['queryLimit'];
        if (isset($o['databases'])) $this->databases = JsonConverters::fromArray('DatabaseInfo', $o['databases']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->queryLimit)) $o['queryLimit'] = $this->queryLimit;
        if (isset($this->databases)) $o['databases'] = JsonConverters::toArray('DatabaseInfo', $this->databases);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class PluginInfo implements JsonSerializable
{
    public function __construct(
        /** @var array<string>|null */
        public ?array $loaded=null,
        /** @var AuthInfo|null */
        public ?AuthInfo $auth=null,
        /** @var ApiKeyInfo|null */
        public ?ApiKeyInfo $apiKey=null,
        /** @var CommandsInfo|null */
        public ?CommandsInfo $commands=null,
        /** @var AutoQueryInfo|null */
        public ?AutoQueryInfo $autoQuery=null,
        /** @var ValidationInfo|null */
        public ?ValidationInfo $validation=null,
        /** @var SharpPagesInfo|null */
        public ?SharpPagesInfo $sharpPages=null,
        /** @var RequestLogsInfo|null */
        public ?RequestLogsInfo $requestLogs=null,
        /** @var ProfilingInfo|null */
        public ?ProfilingInfo $profiling=null,
        /** @var FilesUploadInfo|null */
        public ?FilesUploadInfo $filesUpload=null,
        /** @var AdminUsersInfo|null */
        public ?AdminUsersInfo $adminUsers=null,
        /** @var AdminIdentityUsersInfo|null */
        public ?AdminIdentityUsersInfo $adminIdentityUsers=null,
        /** @var AdminRedisInfo|null */
        public ?AdminRedisInfo $adminRedis=null,
        /** @var AdminDatabaseInfo|null */
        public ?AdminDatabaseInfo $adminDatabase=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['loaded'])) $this->loaded = JsonConverters::fromArray('string', $o['loaded']);
        if (isset($o['auth'])) $this->auth = JsonConverters::from('AuthInfo', $o['auth']);
        if (isset($o['apiKey'])) $this->apiKey = JsonConverters::from('ApiKeyInfo', $o['apiKey']);
        if (isset($o['commands'])) $this->commands = JsonConverters::from('CommandsInfo', $o['commands']);
        if (isset($o['autoQuery'])) $this->autoQuery = JsonConverters::from('AutoQueryInfo', $o['autoQuery']);
        if (isset($o['validation'])) $this->validation = JsonConverters::from('ValidationInfo', $o['validation']);
        if (isset($o['sharpPages'])) $this->sharpPages = JsonConverters::from('SharpPagesInfo', $o['sharpPages']);
        if (isset($o['requestLogs'])) $this->requestLogs = JsonConverters::from('RequestLogsInfo', $o['requestLogs']);
        if (isset($o['profiling'])) $this->profiling = JsonConverters::from('ProfilingInfo', $o['profiling']);
        if (isset($o['filesUpload'])) $this->filesUpload = JsonConverters::from('FilesUploadInfo', $o['filesUpload']);
        if (isset($o['adminUsers'])) $this->adminUsers = JsonConverters::from('AdminUsersInfo', $o['adminUsers']);
        if (isset($o['adminIdentityUsers'])) $this->adminIdentityUsers = JsonConverters::from('AdminIdentityUsersInfo', $o['adminIdentityUsers']);
        if (isset($o['adminRedis'])) $this->adminRedis = JsonConverters::from('AdminRedisInfo', $o['adminRedis']);
        if (isset($o['adminDatabase'])) $this->adminDatabase = JsonConverters::from('AdminDatabaseInfo', $o['adminDatabase']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->loaded)) $o['loaded'] = JsonConverters::toArray('string', $this->loaded);
        if (isset($this->auth)) $o['auth'] = JsonConverters::to('AuthInfo', $this->auth);
        if (isset($this->apiKey)) $o['apiKey'] = JsonConverters::to('ApiKeyInfo', $this->apiKey);
        if (isset($this->commands)) $o['commands'] = JsonConverters::to('CommandsInfo', $this->commands);
        if (isset($this->autoQuery)) $o['autoQuery'] = JsonConverters::to('AutoQueryInfo', $this->autoQuery);
        if (isset($this->validation)) $o['validation'] = JsonConverters::to('ValidationInfo', $this->validation);
        if (isset($this->sharpPages)) $o['sharpPages'] = JsonConverters::to('SharpPagesInfo', $this->sharpPages);
        if (isset($this->requestLogs)) $o['requestLogs'] = JsonConverters::to('RequestLogsInfo', $this->requestLogs);
        if (isset($this->profiling)) $o['profiling'] = JsonConverters::to('ProfilingInfo', $this->profiling);
        if (isset($this->filesUpload)) $o['filesUpload'] = JsonConverters::to('FilesUploadInfo', $this->filesUpload);
        if (isset($this->adminUsers)) $o['adminUsers'] = JsonConverters::to('AdminUsersInfo', $this->adminUsers);
        if (isset($this->adminIdentityUsers)) $o['adminIdentityUsers'] = JsonConverters::to('AdminIdentityUsersInfo', $this->adminIdentityUsers);
        if (isset($this->adminRedis)) $o['adminRedis'] = JsonConverters::to('AdminRedisInfo', $this->adminRedis);
        if (isset($this->adminDatabase)) $o['adminDatabase'] = JsonConverters::to('AdminDatabaseInfo', $this->adminDatabase);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class CustomPluginInfo implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $accessRole=null,
        /** @var array<string,string[]>|null */
        public ?array $serviceRoutes=null,
        /** @var array<string>|null */
        public ?array $enabled=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['accessRole'])) $this->accessRole = $o['accessRole'];
        if (isset($o['serviceRoutes'])) $this->serviceRoutes = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $o['serviceRoutes']);
        if (isset($o['enabled'])) $this->enabled = JsonConverters::fromArray('string', $o['enabled']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->accessRole)) $o['accessRole'] = $this->accessRole;
        if (isset($this->serviceRoutes)) $o['serviceRoutes'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','String[]']), $this->serviceRoutes);
        if (isset($this->enabled)) $o['enabled'] = JsonConverters::toArray('string', $this->enabled);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataTypesConfig implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $baseUrl=null,
        /** @var string|null */
        public ?string $usePath=null,
        /** @var bool|null */
        public ?bool $makePartial=null,
        /** @var bool|null */
        public ?bool $makeVirtual=null,
        /** @var bool|null */
        public ?bool $makeInternal=null,
        /** @var string|null */
        public ?string $baseClass=null,
        /** @var string|null */
        public ?string $package=null,
        /** @var bool|null */
        public ?bool $addReturnMarker=null,
        /** @var bool|null */
        public ?bool $addDescriptionAsComments=null,
        /** @var bool|null */
        public ?bool $addDocAnnotations=null,
        /** @var bool|null */
        public ?bool $addDataContractAttributes=null,
        /** @var bool|null */
        public ?bool $addIndexesToDataMembers=null,
        /** @var bool|null */
        public ?bool $addGeneratedCodeAttributes=null,
        /** @var int|null */
        public ?int $addImplicitVersion=null,
        /** @var bool|null */
        public ?bool $addResponseStatus=null,
        /** @var bool|null */
        public ?bool $addServiceStackTypes=null,
        /** @var bool|null */
        public ?bool $addModelExtensions=null,
        /** @var bool|null */
        public ?bool $addPropertyAccessors=null,
        /** @var bool|null */
        public ?bool $excludeGenericBaseTypes=null,
        /** @var bool|null */
        public ?bool $settersReturnThis=null,
        /** @var bool|null */
        public ?bool $addNullableAnnotations=null,
        /** @var bool|null */
        public ?bool $makePropertiesOptional=null,
        /** @var bool|null */
        public ?bool $exportAsTypes=null,
        /** @var bool|null */
        public ?bool $excludeImplementedInterfaces=null,
        /** @var string|null */
        public ?string $addDefaultXmlNamespace=null,
        /** @var bool|null */
        public ?bool $makeDataContractsExtensible=null,
        /** @var bool|null */
        public ?bool $initializeCollections=null,
        /** @var array<string>|null */
        public ?array $addNamespaces=null,
        /** @var array<string>|null */
        public ?array $defaultNamespaces=null,
        /** @var array<string>|null */
        public ?array $defaultImports=null,
        /** @var array<string>|null */
        public ?array $includeTypes=null,
        /** @var array<string>|null */
        public ?array $excludeTypes=null,
        /** @var array<string>|null */
        public ?array $exportTags=null,
        /** @var array<string>|null */
        public ?array $treatTypesAsStrings=null,
        /** @var bool|null */
        public ?bool $exportValueTypes=null,
        /** @var string|null */
        public ?string $globalNamespace=null,
        /** @var bool|null */
        public ?bool $excludeNamespace=null,
        /** @var string|null */
        public ?string $dataClass=null,
        /** @var string|null */
        public ?string $dataClassJson=null,
        /** @var array<string>|null */
        public ?array $ignoreTypes=null,
        /** @var array<string>|null */
        public ?array $exportTypes=null,
        /** @var array<string>|null */
        public ?array $exportAttributes=null,
        /** @var array<string>|null */
        public ?array $ignoreTypesInNamespaces=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['baseUrl'])) $this->baseUrl = $o['baseUrl'];
        if (isset($o['usePath'])) $this->usePath = $o['usePath'];
        if (isset($o['makePartial'])) $this->makePartial = $o['makePartial'];
        if (isset($o['makeVirtual'])) $this->makeVirtual = $o['makeVirtual'];
        if (isset($o['makeInternal'])) $this->makeInternal = $o['makeInternal'];
        if (isset($o['baseClass'])) $this->baseClass = $o['baseClass'];
        if (isset($o['package'])) $this->package = $o['package'];
        if (isset($o['addReturnMarker'])) $this->addReturnMarker = $o['addReturnMarker'];
        if (isset($o['addDescriptionAsComments'])) $this->addDescriptionAsComments = $o['addDescriptionAsComments'];
        if (isset($o['addDocAnnotations'])) $this->addDocAnnotations = $o['addDocAnnotations'];
        if (isset($o['addDataContractAttributes'])) $this->addDataContractAttributes = $o['addDataContractAttributes'];
        if (isset($o['addIndexesToDataMembers'])) $this->addIndexesToDataMembers = $o['addIndexesToDataMembers'];
        if (isset($o['addGeneratedCodeAttributes'])) $this->addGeneratedCodeAttributes = $o['addGeneratedCodeAttributes'];
        if (isset($o['addImplicitVersion'])) $this->addImplicitVersion = $o['addImplicitVersion'];
        if (isset($o['addResponseStatus'])) $this->addResponseStatus = $o['addResponseStatus'];
        if (isset($o['addServiceStackTypes'])) $this->addServiceStackTypes = $o['addServiceStackTypes'];
        if (isset($o['addModelExtensions'])) $this->addModelExtensions = $o['addModelExtensions'];
        if (isset($o['addPropertyAccessors'])) $this->addPropertyAccessors = $o['addPropertyAccessors'];
        if (isset($o['excludeGenericBaseTypes'])) $this->excludeGenericBaseTypes = $o['excludeGenericBaseTypes'];
        if (isset($o['settersReturnThis'])) $this->settersReturnThis = $o['settersReturnThis'];
        if (isset($o['addNullableAnnotations'])) $this->addNullableAnnotations = $o['addNullableAnnotations'];
        if (isset($o['makePropertiesOptional'])) $this->makePropertiesOptional = $o['makePropertiesOptional'];
        if (isset($o['exportAsTypes'])) $this->exportAsTypes = $o['exportAsTypes'];
        if (isset($o['excludeImplementedInterfaces'])) $this->excludeImplementedInterfaces = $o['excludeImplementedInterfaces'];
        if (isset($o['addDefaultXmlNamespace'])) $this->addDefaultXmlNamespace = $o['addDefaultXmlNamespace'];
        if (isset($o['makeDataContractsExtensible'])) $this->makeDataContractsExtensible = $o['makeDataContractsExtensible'];
        if (isset($o['initializeCollections'])) $this->initializeCollections = $o['initializeCollections'];
        if (isset($o['addNamespaces'])) $this->addNamespaces = JsonConverters::fromArray('string', $o['addNamespaces']);
        if (isset($o['defaultNamespaces'])) $this->defaultNamespaces = JsonConverters::fromArray('string', $o['defaultNamespaces']);
        if (isset($o['defaultImports'])) $this->defaultImports = JsonConverters::fromArray('string', $o['defaultImports']);
        if (isset($o['includeTypes'])) $this->includeTypes = JsonConverters::fromArray('string', $o['includeTypes']);
        if (isset($o['excludeTypes'])) $this->excludeTypes = JsonConverters::fromArray('string', $o['excludeTypes']);
        if (isset($o['exportTags'])) $this->exportTags = JsonConverters::fromArray('string', $o['exportTags']);
        if (isset($o['treatTypesAsStrings'])) $this->treatTypesAsStrings = JsonConverters::fromArray('string', $o['treatTypesAsStrings']);
        if (isset($o['exportValueTypes'])) $this->exportValueTypes = $o['exportValueTypes'];
        if (isset($o['globalNamespace'])) $this->globalNamespace = $o['globalNamespace'];
        if (isset($o['excludeNamespace'])) $this->excludeNamespace = $o['excludeNamespace'];
        if (isset($o['dataClass'])) $this->dataClass = $o['dataClass'];
        if (isset($o['dataClassJson'])) $this->dataClassJson = $o['dataClassJson'];
        if (isset($o['ignoreTypes'])) $this->ignoreTypes = JsonConverters::fromArray('Type', $o['ignoreTypes']);
        if (isset($o['exportTypes'])) $this->exportTypes = JsonConverters::fromArray('Type', $o['exportTypes']);
        if (isset($o['exportAttributes'])) $this->exportAttributes = JsonConverters::fromArray('Type', $o['exportAttributes']);
        if (isset($o['ignoreTypesInNamespaces'])) $this->ignoreTypesInNamespaces = JsonConverters::fromArray('string', $o['ignoreTypesInNamespaces']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->baseUrl)) $o['baseUrl'] = $this->baseUrl;
        if (isset($this->usePath)) $o['usePath'] = $this->usePath;
        if (isset($this->makePartial)) $o['makePartial'] = $this->makePartial;
        if (isset($this->makeVirtual)) $o['makeVirtual'] = $this->makeVirtual;
        if (isset($this->makeInternal)) $o['makeInternal'] = $this->makeInternal;
        if (isset($this->baseClass)) $o['baseClass'] = $this->baseClass;
        if (isset($this->package)) $o['package'] = $this->package;
        if (isset($this->addReturnMarker)) $o['addReturnMarker'] = $this->addReturnMarker;
        if (isset($this->addDescriptionAsComments)) $o['addDescriptionAsComments'] = $this->addDescriptionAsComments;
        if (isset($this->addDocAnnotations)) $o['addDocAnnotations'] = $this->addDocAnnotations;
        if (isset($this->addDataContractAttributes)) $o['addDataContractAttributes'] = $this->addDataContractAttributes;
        if (isset($this->addIndexesToDataMembers)) $o['addIndexesToDataMembers'] = $this->addIndexesToDataMembers;
        if (isset($this->addGeneratedCodeAttributes)) $o['addGeneratedCodeAttributes'] = $this->addGeneratedCodeAttributes;
        if (isset($this->addImplicitVersion)) $o['addImplicitVersion'] = $this->addImplicitVersion;
        if (isset($this->addResponseStatus)) $o['addResponseStatus'] = $this->addResponseStatus;
        if (isset($this->addServiceStackTypes)) $o['addServiceStackTypes'] = $this->addServiceStackTypes;
        if (isset($this->addModelExtensions)) $o['addModelExtensions'] = $this->addModelExtensions;
        if (isset($this->addPropertyAccessors)) $o['addPropertyAccessors'] = $this->addPropertyAccessors;
        if (isset($this->excludeGenericBaseTypes)) $o['excludeGenericBaseTypes'] = $this->excludeGenericBaseTypes;
        if (isset($this->settersReturnThis)) $o['settersReturnThis'] = $this->settersReturnThis;
        if (isset($this->addNullableAnnotations)) $o['addNullableAnnotations'] = $this->addNullableAnnotations;
        if (isset($this->makePropertiesOptional)) $o['makePropertiesOptional'] = $this->makePropertiesOptional;
        if (isset($this->exportAsTypes)) $o['exportAsTypes'] = $this->exportAsTypes;
        if (isset($this->excludeImplementedInterfaces)) $o['excludeImplementedInterfaces'] = $this->excludeImplementedInterfaces;
        if (isset($this->addDefaultXmlNamespace)) $o['addDefaultXmlNamespace'] = $this->addDefaultXmlNamespace;
        if (isset($this->makeDataContractsExtensible)) $o['makeDataContractsExtensible'] = $this->makeDataContractsExtensible;
        if (isset($this->initializeCollections)) $o['initializeCollections'] = $this->initializeCollections;
        if (isset($this->addNamespaces)) $o['addNamespaces'] = JsonConverters::toArray('string', $this->addNamespaces);
        if (isset($this->defaultNamespaces)) $o['defaultNamespaces'] = JsonConverters::toArray('string', $this->defaultNamespaces);
        if (isset($this->defaultImports)) $o['defaultImports'] = JsonConverters::toArray('string', $this->defaultImports);
        if (isset($this->includeTypes)) $o['includeTypes'] = JsonConverters::toArray('string', $this->includeTypes);
        if (isset($this->excludeTypes)) $o['excludeTypes'] = JsonConverters::toArray('string', $this->excludeTypes);
        if (isset($this->exportTags)) $o['exportTags'] = JsonConverters::toArray('string', $this->exportTags);
        if (isset($this->treatTypesAsStrings)) $o['treatTypesAsStrings'] = JsonConverters::toArray('string', $this->treatTypesAsStrings);
        if (isset($this->exportValueTypes)) $o['exportValueTypes'] = $this->exportValueTypes;
        if (isset($this->globalNamespace)) $o['globalNamespace'] = $this->globalNamespace;
        if (isset($this->excludeNamespace)) $o['excludeNamespace'] = $this->excludeNamespace;
        if (isset($this->dataClass)) $o['dataClass'] = $this->dataClass;
        if (isset($this->dataClassJson)) $o['dataClassJson'] = $this->dataClassJson;
        if (isset($this->ignoreTypes)) $o['ignoreTypes'] = JsonConverters::toArray('Type', $this->ignoreTypes);
        if (isset($this->exportTypes)) $o['exportTypes'] = JsonConverters::toArray('Type', $this->exportTypes);
        if (isset($this->exportAttributes)) $o['exportAttributes'] = JsonConverters::toArray('Type', $this->exportAttributes);
        if (isset($this->ignoreTypesInNamespaces)) $o['ignoreTypesInNamespaces'] = JsonConverters::toArray('string', $this->ignoreTypesInNamespaces);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataRoute implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $path=null,
        /** @var string|null */
        public ?string $verbs=null,
        /** @var string|null */
        public ?string $notes=null,
        /** @var string|null */
        public ?string $summary=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['path'])) $this->path = $o['path'];
        if (isset($o['verbs'])) $this->verbs = $o['verbs'];
        if (isset($o['notes'])) $this->notes = $o['notes'];
        if (isset($o['summary'])) $this->summary = $o['summary'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->path)) $o['path'] = $this->path;
        if (isset($this->verbs)) $o['verbs'] = $this->verbs;
        if (isset($this->notes)) $o['notes'] = $this->notes;
        if (isset($this->summary)) $o['summary'] = $this->summary;
        return empty($o) ? new class(){} : $o;
    }
}

class ApiUiInfo implements JsonSerializable
{
    public function __construct(
        /** @var ApiCss|null */
        public ?ApiCss $locodeCss=null,
        /** @var ApiCss|null */
        public ?ApiCss $explorerCss=null,
        /** @var array<InputInfo>|null */
        public ?array $formLayout=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['locodeCss'])) $this->locodeCss = JsonConverters::from('ApiCss', $o['locodeCss']);
        if (isset($o['explorerCss'])) $this->explorerCss = JsonConverters::from('ApiCss', $o['explorerCss']);
        if (isset($o['formLayout'])) $this->formLayout = JsonConverters::fromArray('InputInfo', $o['formLayout']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->locodeCss)) $o['locodeCss'] = JsonConverters::to('ApiCss', $this->locodeCss);
        if (isset($this->explorerCss)) $o['explorerCss'] = JsonConverters::to('ApiCss', $this->explorerCss);
        if (isset($this->formLayout)) $o['formLayout'] = JsonConverters::toArray('InputInfo', $this->formLayout);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataOperationType implements JsonSerializable
{
    public function __construct(
        /** @var MetadataType|null */
        public ?MetadataType $request=null,
        /** @var MetadataType|null */
        public ?MetadataType $response=null,
        /** @var array<string>|null */
        public ?array $actions=null,
        /** @var bool|null */
        public ?bool $returnsVoid=null,
        /** @var string|null */
        public ?string $method=null,
        /** @var MetadataTypeName|null */
        public ?MetadataTypeName $returnType=null,
        /** @var array<MetadataRoute>|null */
        public ?array $routes=null,
        /** @var MetadataTypeName|null */
        public ?MetadataTypeName $dataModel=null,
        /** @var MetadataTypeName|null */
        public ?MetadataTypeName $viewModel=null,
        /** @var bool|null */
        public ?bool $requiresAuth=null,
        /** @var bool|null */
        public ?bool $requiresApiKey=null,
        /** @var array<string>|null */
        public ?array $requiredRoles=null,
        /** @var array<string>|null */
        public ?array $requiresAnyRole=null,
        /** @var array<string>|null */
        public ?array $requiredPermissions=null,
        /** @var array<string>|null */
        public ?array $requiresAnyPermission=null,
        /** @var array<string>|null */
        public ?array $tags=null,
        /** @var ApiUiInfo|null */
        public ?ApiUiInfo $ui=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['request'])) $this->request = JsonConverters::from('MetadataType', $o['request']);
        if (isset($o['response'])) $this->response = JsonConverters::from('MetadataType', $o['response']);
        if (isset($o['actions'])) $this->actions = JsonConverters::fromArray('string', $o['actions']);
        if (isset($o['returnsVoid'])) $this->returnsVoid = $o['returnsVoid'];
        if (isset($o['method'])) $this->method = $o['method'];
        if (isset($o['returnType'])) $this->returnType = JsonConverters::from('MetadataTypeName', $o['returnType']);
        if (isset($o['routes'])) $this->routes = JsonConverters::fromArray('MetadataRoute', $o['routes']);
        if (isset($o['dataModel'])) $this->dataModel = JsonConverters::from('MetadataTypeName', $o['dataModel']);
        if (isset($o['viewModel'])) $this->viewModel = JsonConverters::from('MetadataTypeName', $o['viewModel']);
        if (isset($o['requiresAuth'])) $this->requiresAuth = $o['requiresAuth'];
        if (isset($o['requiresApiKey'])) $this->requiresApiKey = $o['requiresApiKey'];
        if (isset($o['requiredRoles'])) $this->requiredRoles = JsonConverters::fromArray('string', $o['requiredRoles']);
        if (isset($o['requiresAnyRole'])) $this->requiresAnyRole = JsonConverters::fromArray('string', $o['requiresAnyRole']);
        if (isset($o['requiredPermissions'])) $this->requiredPermissions = JsonConverters::fromArray('string', $o['requiredPermissions']);
        if (isset($o['requiresAnyPermission'])) $this->requiresAnyPermission = JsonConverters::fromArray('string', $o['requiresAnyPermission']);
        if (isset($o['tags'])) $this->tags = JsonConverters::fromArray('string', $o['tags']);
        if (isset($o['ui'])) $this->ui = JsonConverters::from('ApiUiInfo', $o['ui']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->request)) $o['request'] = JsonConverters::to('MetadataType', $this->request);
        if (isset($this->response)) $o['response'] = JsonConverters::to('MetadataType', $this->response);
        if (isset($this->actions)) $o['actions'] = JsonConverters::toArray('string', $this->actions);
        if (isset($this->returnsVoid)) $o['returnsVoid'] = $this->returnsVoid;
        if (isset($this->method)) $o['method'] = $this->method;
        if (isset($this->returnType)) $o['returnType'] = JsonConverters::to('MetadataTypeName', $this->returnType);
        if (isset($this->routes)) $o['routes'] = JsonConverters::toArray('MetadataRoute', $this->routes);
        if (isset($this->dataModel)) $o['dataModel'] = JsonConverters::to('MetadataTypeName', $this->dataModel);
        if (isset($this->viewModel)) $o['viewModel'] = JsonConverters::to('MetadataTypeName', $this->viewModel);
        if (isset($this->requiresAuth)) $o['requiresAuth'] = $this->requiresAuth;
        if (isset($this->requiresApiKey)) $o['requiresApiKey'] = $this->requiresApiKey;
        if (isset($this->requiredRoles)) $o['requiredRoles'] = JsonConverters::toArray('string', $this->requiredRoles);
        if (isset($this->requiresAnyRole)) $o['requiresAnyRole'] = JsonConverters::toArray('string', $this->requiresAnyRole);
        if (isset($this->requiredPermissions)) $o['requiredPermissions'] = JsonConverters::toArray('string', $this->requiredPermissions);
        if (isset($this->requiresAnyPermission)) $o['requiresAnyPermission'] = JsonConverters::toArray('string', $this->requiresAnyPermission);
        if (isset($this->tags)) $o['tags'] = JsonConverters::toArray('string', $this->tags);
        if (isset($this->ui)) $o['ui'] = JsonConverters::to('ApiUiInfo', $this->ui);
        return empty($o) ? new class(){} : $o;
    }
}

class MetadataTypes implements JsonSerializable
{
    public function __construct(
        /** @var MetadataTypesConfig|null */
        public ?MetadataTypesConfig $config=null,
        /** @var array<string>|null */
        public ?array $namespaces=null,
        /** @var array<MetadataType>|null */
        public ?array $types=null,
        /** @var array<MetadataOperationType>|null */
        public ?array $operations=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['config'])) $this->config = JsonConverters::from('MetadataTypesConfig', $o['config']);
        if (isset($o['namespaces'])) $this->namespaces = JsonConverters::fromArray('string', $o['namespaces']);
        if (isset($o['types'])) $this->types = JsonConverters::fromArray('MetadataType', $o['types']);
        if (isset($o['operations'])) $this->operations = JsonConverters::fromArray('MetadataOperationType', $o['operations']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->config)) $o['config'] = JsonConverters::to('MetadataTypesConfig', $this->config);
        if (isset($this->namespaces)) $o['namespaces'] = JsonConverters::toArray('string', $this->namespaces);
        if (isset($this->types)) $o['types'] = JsonConverters::toArray('MetadataType', $this->types);
        if (isset($this->operations)) $o['operations'] = JsonConverters::toArray('MetadataOperationType', $this->operations);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminRole implements JsonSerializable
{
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        return empty($o) ? new class(){} : $o;
    }
}

class ServerStats implements JsonSerializable
{
    public function __construct(
        /** @var array<string,int>|null */
        public ?array $redis=null,
        /** @var array<string,string>|null */
        public ?array $serverEvents=null,
        /** @var string|null */
        public ?string $mqDescription=null,
        /** @var array<string,int>|null */
        public ?array $mqWorkers=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['redis'])) $this->redis = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','int']), $o['redis']);
        if (isset($o['serverEvents'])) $this->serverEvents = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['serverEvents']);
        if (isset($o['mqDescription'])) $this->mqDescription = $o['mqDescription'];
        if (isset($o['mqWorkers'])) $this->mqWorkers = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','int']), $o['mqWorkers']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->redis)) $o['redis'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','int']), $this->redis);
        if (isset($this->serverEvents)) $o['serverEvents'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->serverEvents);
        if (isset($this->mqDescription)) $o['mqDescription'] = $this->mqDescription;
        if (isset($this->mqWorkers)) $o['mqWorkers'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','int']), $this->mqWorkers);
        return empty($o) ? new class(){} : $o;
    }
}

class DiagnosticEntry implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $id=0,
        /** @var string|null */
        public ?string $traceId=null,
        /** @var string */
        public string $source='',
        /** @var string */
        public string $eventType='',
        /** @var string */
        public string $message='',
        /** @var string */
        public string $operation='',
        /** @var int */
        public int $threadId=0,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $error=null,
        /** @var string */
        public string $commandType='',
        /** @var string */
        public string $command='',
        /** @var string|null */
        public ?string $userAuthId=null,
        /** @var string|null */
        public ?string $sessionId=null,
        /** @var string|null */
        public ?string $arg=null,
        /** @var array<string>|null */
        public ?array $args=null,
        /** @var array<int>|null */
        public ?array $argLengths=null,
        /** @var array<string,Object>|null */
        public ?array $namedArgs=null,
        /** @var DateInterval|null */
        public ?DateInterval $duration=null,
        /** @var int */
        public int $timestamp=0,
        /** @var DateTime */
        public DateTime $date=new DateTime(),
        /** @var string|null */
        public ?string $tag=null,
        /** @var string|null */
        public ?string $stackTrace=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['traceId'])) $this->traceId = $o['traceId'];
        if (isset($o['source'])) $this->source = $o['source'];
        if (isset($o['eventType'])) $this->eventType = $o['eventType'];
        if (isset($o['message'])) $this->message = $o['message'];
        if (isset($o['operation'])) $this->operation = $o['operation'];
        if (isset($o['threadId'])) $this->threadId = $o['threadId'];
        if (isset($o['error'])) $this->error = JsonConverters::from('ResponseStatus', $o['error']);
        if (isset($o['commandType'])) $this->commandType = $o['commandType'];
        if (isset($o['command'])) $this->command = $o['command'];
        if (isset($o['userAuthId'])) $this->userAuthId = $o['userAuthId'];
        if (isset($o['sessionId'])) $this->sessionId = $o['sessionId'];
        if (isset($o['arg'])) $this->arg = $o['arg'];
        if (isset($o['args'])) $this->args = JsonConverters::fromArray('string', $o['args']);
        if (isset($o['argLengths'])) $this->argLengths = JsonConverters::fromArray('int', $o['argLengths']);
        if (isset($o['namedArgs'])) $this->namedArgs = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','Object']), $o['namedArgs']);
        if (isset($o['duration'])) $this->duration = JsonConverters::from('TimeSpan', $o['duration']);
        if (isset($o['timestamp'])) $this->timestamp = $o['timestamp'];
        if (isset($o['date'])) $this->date = JsonConverters::from('DateTime', $o['date']);
        if (isset($o['tag'])) $this->tag = $o['tag'];
        if (isset($o['stackTrace'])) $this->stackTrace = $o['stackTrace'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->traceId)) $o['traceId'] = $this->traceId;
        if (isset($this->source)) $o['source'] = $this->source;
        if (isset($this->eventType)) $o['eventType'] = $this->eventType;
        if (isset($this->message)) $o['message'] = $this->message;
        if (isset($this->operation)) $o['operation'] = $this->operation;
        if (isset($this->threadId)) $o['threadId'] = $this->threadId;
        if (isset($this->error)) $o['error'] = JsonConverters::to('ResponseStatus', $this->error);
        if (isset($this->commandType)) $o['commandType'] = $this->commandType;
        if (isset($this->command)) $o['command'] = $this->command;
        if (isset($this->userAuthId)) $o['userAuthId'] = $this->userAuthId;
        if (isset($this->sessionId)) $o['sessionId'] = $this->sessionId;
        if (isset($this->arg)) $o['arg'] = $this->arg;
        if (isset($this->args)) $o['args'] = JsonConverters::toArray('string', $this->args);
        if (isset($this->argLengths)) $o['argLengths'] = JsonConverters::toArray('int', $this->argLengths);
        if (isset($this->namedArgs)) $o['namedArgs'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','Object']), $this->namedArgs);
        if (isset($this->duration)) $o['duration'] = JsonConverters::to('TimeSpan', $this->duration);
        if (isset($this->timestamp)) $o['timestamp'] = $this->timestamp;
        if (isset($this->date)) $o['date'] = JsonConverters::to('DateTime', $this->date);
        if (isset($this->tag)) $o['tag'] = $this->tag;
        if (isset($this->stackTrace)) $o['stackTrace'] = $this->stackTrace;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

class RedisSearchResult implements JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $id='',
        /** @var string */
        public string $type='',
        /** @var int */
        public int $ttl=0,
        /** @var int */
        public int $size=0
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['ttl'])) $this->ttl = $o['ttl'];
        if (isset($o['size'])) $this->size = $o['size'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->ttl)) $o['ttl'] = $this->ttl;
        if (isset($this->size)) $o['size'] = $this->size;
        return empty($o) ? new class(){} : $o;
    }
}

class RedisText implements JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $text=null,
        /** @var array<RedisText>|null */
        public ?array $children=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['text'])) $this->text = $o['text'];
        if (isset($o['children'])) $this->children = JsonConverters::fromArray('RedisText', $o['children']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->text)) $o['text'] = $this->text;
        if (isset($this->children)) $o['children'] = JsonConverters::toArray('RedisText', $this->children);
        return empty($o) ? new class(){} : $o;
    }
}

class CommandSummary implements JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $type='',
        /** @var string */
        public string $name='',
        /** @var int */
        public int $count=0,
        /** @var int */
        public int $failed=0,
        /** @var int */
        public int $retries=0,
        /** @var int */
        public int $totalMs=0,
        /** @var int */
        public int $minMs=0,
        /** @var int */
        public int $maxMs=0,
        /** @var float */
        public float $averageMs=0.0,
        /** @var float */
        public float $medianMs=0.0,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $lastError=null,
        /** @var ConcurrentQueue<int>|null */
        public ?ConcurrentQueue $timings=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['count'])) $this->count = $o['count'];
        if (isset($o['failed'])) $this->failed = $o['failed'];
        if (isset($o['retries'])) $this->retries = $o['retries'];
        if (isset($o['totalMs'])) $this->totalMs = $o['totalMs'];
        if (isset($o['minMs'])) $this->minMs = $o['minMs'];
        if (isset($o['maxMs'])) $this->maxMs = $o['maxMs'];
        if (isset($o['averageMs'])) $this->averageMs = $o['averageMs'];
        if (isset($o['medianMs'])) $this->medianMs = $o['medianMs'];
        if (isset($o['lastError'])) $this->lastError = JsonConverters::from('ResponseStatus', $o['lastError']);
        if (isset($o['timings'])) $this->timings = JsonConverters::from(JsonConverters::context('ConcurrentQueue',genericArgs:['int']), $o['timings']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->count)) $o['count'] = $this->count;
        if (isset($this->failed)) $o['failed'] = $this->failed;
        if (isset($this->retries)) $o['retries'] = $this->retries;
        if (isset($this->totalMs)) $o['totalMs'] = $this->totalMs;
        if (isset($this->minMs)) $o['minMs'] = $this->minMs;
        if (isset($this->maxMs)) $o['maxMs'] = $this->maxMs;
        if (isset($this->averageMs)) $o['averageMs'] = $this->averageMs;
        if (isset($this->medianMs)) $o['medianMs'] = $this->medianMs;
        if (isset($this->lastError)) $o['lastError'] = JsonConverters::to('ResponseStatus', $this->lastError);
        if (isset($this->timings)) $o['timings'] = JsonConverters::to(JsonConverters::context('ConcurrentQueue',genericArgs:['int']), $this->timings);
        return empty($o) ? new class(){} : $o;
    }
}

class CommandResult implements JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $type='',
        /** @var string */
        public string $name='',
        /** @var int|null */
        public ?int $ms=null,
        /** @var DateTime */
        public DateTime $at=new DateTime(),
        /** @var string */
        public string $request='',
        /** @var int|null */
        public ?int $retries=null,
        /** @var int */
        public int $attempt=0,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $error=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['type'])) $this->type = $o['type'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['ms'])) $this->ms = $o['ms'];
        if (isset($o['at'])) $this->at = JsonConverters::from('DateTime', $o['at']);
        if (isset($o['request'])) $this->request = $o['request'];
        if (isset($o['retries'])) $this->retries = $o['retries'];
        if (isset($o['attempt'])) $this->attempt = $o['attempt'];
        if (isset($o['error'])) $this->error = JsonConverters::from('ResponseStatus', $o['error']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->type)) $o['type'] = $this->type;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->ms)) $o['ms'] = $this->ms;
        if (isset($this->at)) $o['at'] = JsonConverters::to('DateTime', $this->at);
        if (isset($this->request)) $o['request'] = $this->request;
        if (isset($this->retries)) $o['retries'] = $this->retries;
        if (isset($this->attempt)) $o['attempt'] = $this->attempt;
        if (isset($this->error)) $o['error'] = JsonConverters::to('ResponseStatus', $this->error);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class PartialApiKey implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var int */
        public int $id=0,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $name=null,

        // @DataMember(Order=3)
        /** @var string|null */
        public ?string $userId=null,

        // @DataMember(Order=4)
        /** @var string|null */
        public ?string $userName=null,

        // @DataMember(Order=5)
        /** @var string|null */
        public ?string $visibleKey=null,

        // @DataMember(Order=6)
        /** @var string|null */
        public ?string $environment=null,

        // @DataMember(Order=7)
        /** @var DateTime */
        public DateTime $createdDate=new DateTime(),

        // @DataMember(Order=8)
        /** @var DateTime|null */
        public ?DateTime $expiryDate=null,

        // @DataMember(Order=9)
        /** @var DateTime|null */
        public ?DateTime $cancelledDate=null,

        // @DataMember(Order=10)
        /** @var DateTime|null */
        public ?DateTime $lastUsedDate=null,

        // @DataMember(Order=11)
        /** @var array<string>|null */
        public ?array $scopes=null,

        // @DataMember(Order=12)
        /** @var array<string>|null */
        public ?array $features=null,

        // @DataMember(Order=13)
        /** @var array<string>|null */
        public ?array $restrictTo=null,

        // @DataMember(Order=14)
        /** @var string|null */
        public ?string $notes=null,

        // @DataMember(Order=15)
        /** @var int|null */
        public ?int $refId=null,

        // @DataMember(Order=16)
        /** @var string|null */
        public ?string $refIdStr=null,

        // @DataMember(Order=17)
        /** @var array<string,string>|null */
        public ?array $meta=null,

        // @DataMember(Order=18)
        /** @var bool|null */
        public ?bool $active=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['userName'])) $this->userName = $o['userName'];
        if (isset($o['visibleKey'])) $this->visibleKey = $o['visibleKey'];
        if (isset($o['environment'])) $this->environment = $o['environment'];
        if (isset($o['createdDate'])) $this->createdDate = JsonConverters::from('DateTime', $o['createdDate']);
        if (isset($o['expiryDate'])) $this->expiryDate = JsonConverters::from('DateTime', $o['expiryDate']);
        if (isset($o['cancelledDate'])) $this->cancelledDate = JsonConverters::from('DateTime', $o['cancelledDate']);
        if (isset($o['lastUsedDate'])) $this->lastUsedDate = JsonConverters::from('DateTime', $o['lastUsedDate']);
        if (isset($o['scopes'])) $this->scopes = JsonConverters::fromArray('string', $o['scopes']);
        if (isset($o['features'])) $this->features = JsonConverters::fromArray('string', $o['features']);
        if (isset($o['restrictTo'])) $this->restrictTo = JsonConverters::fromArray('string', $o['restrictTo']);
        if (isset($o['notes'])) $this->notes = $o['notes'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['refIdStr'])) $this->refIdStr = $o['refIdStr'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
        if (isset($o['active'])) $this->active = $o['active'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->userName)) $o['userName'] = $this->userName;
        if (isset($this->visibleKey)) $o['visibleKey'] = $this->visibleKey;
        if (isset($this->environment)) $o['environment'] = $this->environment;
        if (isset($this->createdDate)) $o['createdDate'] = JsonConverters::to('DateTime', $this->createdDate);
        if (isset($this->expiryDate)) $o['expiryDate'] = JsonConverters::to('DateTime', $this->expiryDate);
        if (isset($this->cancelledDate)) $o['cancelledDate'] = JsonConverters::to('DateTime', $this->cancelledDate);
        if (isset($this->lastUsedDate)) $o['lastUsedDate'] = JsonConverters::to('DateTime', $this->lastUsedDate);
        if (isset($this->scopes)) $o['scopes'] = JsonConverters::toArray('string', $this->scopes);
        if (isset($this->features)) $o['features'] = JsonConverters::toArray('string', $this->features);
        if (isset($this->restrictTo)) $o['restrictTo'] = JsonConverters::toArray('string', $this->restrictTo);
        if (isset($this->notes)) $o['notes'] = $this->notes;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->refIdStr)) $o['refIdStr'] = $this->refIdStr;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        if (isset($this->active)) $o['active'] = $this->active;
        return empty($o) ? new class(){} : $o;
    }
}

class JobStatSummary implements JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $name='',
        /** @var int */
        public int $total=0,
        /** @var int */
        public int $completed=0,
        /** @var int */
        public int $retries=0,
        /** @var int */
        public int $failed=0,
        /** @var int */
        public int $cancelled=0
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['total'])) $this->total = $o['total'];
        if (isset($o['completed'])) $this->completed = $o['completed'];
        if (isset($o['retries'])) $this->retries = $o['retries'];
        if (isset($o['failed'])) $this->failed = $o['failed'];
        if (isset($o['cancelled'])) $this->cancelled = $o['cancelled'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->total)) $o['total'] = $this->total;
        if (isset($this->completed)) $o['completed'] = $this->completed;
        if (isset($this->retries)) $o['retries'] = $this->retries;
        if (isset($this->failed)) $o['failed'] = $this->failed;
        if (isset($this->cancelled)) $o['cancelled'] = $this->cancelled;
        return empty($o) ? new class(){} : $o;
    }
}

class HourSummary implements JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $hour='',
        /** @var int */
        public int $total=0,
        /** @var int */
        public int $completed=0,
        /** @var int */
        public int $failed=0,
        /** @var int */
        public int $cancelled=0
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['hour'])) $this->hour = $o['hour'];
        if (isset($o['total'])) $this->total = $o['total'];
        if (isset($o['completed'])) $this->completed = $o['completed'];
        if (isset($o['failed'])) $this->failed = $o['failed'];
        if (isset($o['cancelled'])) $this->cancelled = $o['cancelled'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->hour)) $o['hour'] = $this->hour;
        if (isset($this->total)) $o['total'] = $this->total;
        if (isset($this->completed)) $o['completed'] = $this->completed;
        if (isset($this->failed)) $o['failed'] = $this->failed;
        if (isset($this->cancelled)) $o['cancelled'] = $this->cancelled;
        return empty($o) ? new class(){} : $o;
    }
}

class WorkerStats implements JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $name='',
        /** @var int */
        public int $queued=0,
        /** @var int */
        public int $received=0,
        /** @var int */
        public int $completed=0,
        /** @var int */
        public int $retries=0,
        /** @var int */
        public int $failed=0,
        /** @var int|null */
        public ?int $runningJob=null,
        /** @var DateInterval|null */
        public ?DateInterval $runningTime=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['queued'])) $this->queued = $o['queued'];
        if (isset($o['received'])) $this->received = $o['received'];
        if (isset($o['completed'])) $this->completed = $o['completed'];
        if (isset($o['retries'])) $this->retries = $o['retries'];
        if (isset($o['failed'])) $this->failed = $o['failed'];
        if (isset($o['runningJob'])) $this->runningJob = $o['runningJob'];
        if (isset($o['runningTime'])) $this->runningTime = JsonConverters::from('TimeSpan', $o['runningTime']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->queued)) $o['queued'] = $this->queued;
        if (isset($this->received)) $o['received'] = $this->received;
        if (isset($this->completed)) $o['completed'] = $this->completed;
        if (isset($this->retries)) $o['retries'] = $this->retries;
        if (isset($this->failed)) $o['failed'] = $this->failed;
        if (isset($this->runningJob)) $o['runningJob'] = $this->runningJob;
        if (isset($this->runningTime)) $o['runningTime'] = JsonConverters::to('TimeSpan', $this->runningTime);
        return empty($o) ? new class(){} : $o;
    }
}

class RequestLogEntry implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $id=0,
        /** @var string|null */
        public ?string $traceId=null,
        /** @var string|null */
        public ?string $operationName=null,
        /** @var DateTime */
        public DateTime $dateTime=new DateTime(),
        /** @var int */
        public int $statusCode=0,
        /** @var string|null */
        public ?string $statusDescription=null,
        /** @var string|null */
        public ?string $httpMethod=null,
        /** @var string|null */
        public ?string $absoluteUri=null,
        /** @var string|null */
        public ?string $pathInfo=null,
        // @StringLength(2147483647)
        /** @var string|null */
        public ?string $requestBody=null,

        /** @var Object|null */
        public ?Object $requestDto=null,
        /** @var string|null */
        public ?string $userAuthId=null,
        /** @var string|null */
        public ?string $sessionId=null,
        /** @var string|null */
        public ?string $ipAddress=null,
        /** @var string|null */
        public ?string $forwardedFor=null,
        /** @var string|null */
        public ?string $referer=null,
        /** @var array<string,string>|null */
        public ?array $headers=null,
        /** @var array<string,string>|null */
        public ?array $formData=null,
        /** @var array<string,string>|null */
        public ?array $items=null,
        /** @var array<string,string>|null */
        public ?array $responseHeaders=null,
        /** @var Object|null */
        public ?Object $session=null,
        /** @var Object|null */
        public ?Object $responseDto=null,
        /** @var Object|null */
        public ?Object $errorResponse=null,
        /** @var string|null */
        public ?string $exceptionSource=null,
        /** @var array|null */
        public ?array $exceptionData=null,
        /** @var DateInterval|null */
        public ?DateInterval $requestDuration=null,
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['traceId'])) $this->traceId = $o['traceId'];
        if (isset($o['operationName'])) $this->operationName = $o['operationName'];
        if (isset($o['dateTime'])) $this->dateTime = JsonConverters::from('DateTime', $o['dateTime']);
        if (isset($o['statusCode'])) $this->statusCode = $o['statusCode'];
        if (isset($o['statusDescription'])) $this->statusDescription = $o['statusDescription'];
        if (isset($o['httpMethod'])) $this->httpMethod = $o['httpMethod'];
        if (isset($o['absoluteUri'])) $this->absoluteUri = $o['absoluteUri'];
        if (isset($o['pathInfo'])) $this->pathInfo = $o['pathInfo'];
        if (isset($o['requestBody'])) $this->requestBody = $o['requestBody'];
        if (isset($o['requestDto'])) $this->requestDto = JsonConverters::from('Object', $o['requestDto']);
        if (isset($o['userAuthId'])) $this->userAuthId = $o['userAuthId'];
        if (isset($o['sessionId'])) $this->sessionId = $o['sessionId'];
        if (isset($o['ipAddress'])) $this->ipAddress = $o['ipAddress'];
        if (isset($o['forwardedFor'])) $this->forwardedFor = $o['forwardedFor'];
        if (isset($o['referer'])) $this->referer = $o['referer'];
        if (isset($o['headers'])) $this->headers = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['headers']);
        if (isset($o['formData'])) $this->formData = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['formData']);
        if (isset($o['items'])) $this->items = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['items']);
        if (isset($o['responseHeaders'])) $this->responseHeaders = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['responseHeaders']);
        if (isset($o['session'])) $this->session = JsonConverters::from('Object', $o['session']);
        if (isset($o['responseDto'])) $this->responseDto = JsonConverters::from('Object', $o['responseDto']);
        if (isset($o['errorResponse'])) $this->errorResponse = JsonConverters::from('Object', $o['errorResponse']);
        if (isset($o['exceptionSource'])) $this->exceptionSource = $o['exceptionSource'];
        if (isset($o['exceptionData'])) $this->exceptionData = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','object']), $o['exceptionData']);
        if (isset($o['requestDuration'])) $this->requestDuration = JsonConverters::from('DateInterval', $o['requestDuration']);
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->traceId)) $o['traceId'] = $this->traceId;
        if (isset($this->operationName)) $o['operationName'] = $this->operationName;
        if (isset($this->dateTime)) $o['dateTime'] = JsonConverters::to('DateTime', $this->dateTime);
        if (isset($this->statusCode)) $o['statusCode'] = $this->statusCode;
        if (isset($this->statusDescription)) $o['statusDescription'] = $this->statusDescription;
        if (isset($this->httpMethod)) $o['httpMethod'] = $this->httpMethod;
        if (isset($this->absoluteUri)) $o['absoluteUri'] = $this->absoluteUri;
        if (isset($this->pathInfo)) $o['pathInfo'] = $this->pathInfo;
        if (isset($this->requestBody)) $o['requestBody'] = $this->requestBody;
        if (isset($this->requestDto)) $o['requestDto'] = JsonConverters::to('Object', $this->requestDto);
        if (isset($this->userAuthId)) $o['userAuthId'] = $this->userAuthId;
        if (isset($this->sessionId)) $o['sessionId'] = $this->sessionId;
        if (isset($this->ipAddress)) $o['ipAddress'] = $this->ipAddress;
        if (isset($this->forwardedFor)) $o['forwardedFor'] = $this->forwardedFor;
        if (isset($this->referer)) $o['referer'] = $this->referer;
        if (isset($this->headers)) $o['headers'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->headers);
        if (isset($this->formData)) $o['formData'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->formData);
        if (isset($this->items)) $o['items'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->items);
        if (isset($this->responseHeaders)) $o['responseHeaders'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->responseHeaders);
        if (isset($this->session)) $o['session'] = JsonConverters::to('Object', $this->session);
        if (isset($this->responseDto)) $o['responseDto'] = JsonConverters::to('Object', $this->responseDto);
        if (isset($this->errorResponse)) $o['errorResponse'] = JsonConverters::to('Object', $this->errorResponse);
        if (isset($this->exceptionSource)) $o['exceptionSource'] = $this->exceptionSource;
        if (isset($this->exceptionData)) $o['exceptionData'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','object']), $this->exceptionData);
        if (isset($this->requestDuration)) $o['requestDuration'] = JsonConverters::to('DateInterval', $this->requestDuration);
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminGetRolesResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var array<AdminRole>|null */
        public ?array $results=null,

        // @DataMember(Order=2)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('AdminRole', $o['results']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('AdminRole', $this->results);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminGetRoleResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var AdminRole|null */
        public ?AdminRole $result=null,

        // @DataMember(Order=2)
        /** @var array<Property>|null */
        public ?array $claims=null,

        // @DataMember(Order=3)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['result'])) $this->result = JsonConverters::from('AdminRole', $o['result']);
        if (isset($o['claims'])) $this->claims = JsonConverters::fromArray('Property', $o['claims']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->result)) $o['result'] = JsonConverters::to('AdminRole', $this->result);
        if (isset($this->claims)) $o['claims'] = JsonConverters::toArray('Property', $this->claims);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminDashboardResponse implements JsonSerializable
{
    public function __construct(
        /** @var ServerStats|null */
        public ?ServerStats $serverStats=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['serverStats'])) $this->serverStats = JsonConverters::from('ServerStats', $o['serverStats']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->serverStats)) $o['serverStats'] = JsonConverters::to('ServerStats', $this->serverStats);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminUserResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $id=null,

        // @DataMember(Order=2)
        /** @var array<string,Object>|null */
        public ?array $result=null,

        // @DataMember(Order=3)
        /** @var array<array<string,Object>>|null */
        public ?array $details=null,

        // @DataMember(Order=4)
        /** @var array<Property>|null */
        public ?array $claims=null,

        // @DataMember(Order=5)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['result'])) $this->result = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','Object']), $o['result']);
        if (isset($o['details'])) $this->details = JsonConverters::fromArray('Dictionary<String,Object>', $o['details']);
        if (isset($o['claims'])) $this->claims = JsonConverters::fromArray('Property', $o['claims']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->result)) $o['result'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','Object']), $this->result);
        if (isset($this->details)) $o['details'] = JsonConverters::toArray('Dictionary<String,Object>', $this->details);
        if (isset($this->claims)) $o['claims'] = JsonConverters::toArray('Property', $this->claims);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminUsersResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var array<array<string,Object>>|null */
        public ?array $results=null,

        // @DataMember(Order=2)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('Dictionary<String,Object>', $o['results']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('Dictionary<String,Object>', $this->results);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminDeleteUserResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $id=null,

        // @DataMember(Order=2)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminProfilingResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<DiagnosticEntry>|null */
        public ?array $results=null,
        /** @var int */
        public int $total=0,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('DiagnosticEntry', $o['results']);
        if (isset($o['total'])) $this->total = $o['total'];
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('DiagnosticEntry', $this->results);
        if (isset($this->total)) $o['total'] = $this->total;
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminRedisResponse implements JsonSerializable
{
    public function __construct(
        /** @var int */
        public int $db=0,
        /** @var array<RedisSearchResult>|null */
        public ?array $searchResults=null,
        /** @var array<string,string>|null */
        public ?array $info=null,
        /** @var RedisEndpointInfo|null */
        public ?RedisEndpointInfo $endpoint=null,
        /** @var RedisText|null */
        public ?RedisText $result=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['db'])) $this->db = $o['db'];
        if (isset($o['searchResults'])) $this->searchResults = JsonConverters::fromArray('RedisSearchResult', $o['searchResults']);
        if (isset($o['info'])) $this->info = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['info']);
        if (isset($o['endpoint'])) $this->endpoint = JsonConverters::from('RedisEndpointInfo', $o['endpoint']);
        if (isset($o['result'])) $this->result = JsonConverters::from('RedisText', $o['result']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->db)) $o['db'] = $this->db;
        if (isset($this->searchResults)) $o['searchResults'] = JsonConverters::toArray('RedisSearchResult', $this->searchResults);
        if (isset($this->info)) $o['info'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->info);
        if (isset($this->endpoint)) $o['endpoint'] = JsonConverters::to('RedisEndpointInfo', $this->endpoint);
        if (isset($this->result)) $o['result'] = JsonConverters::to('RedisText', $this->result);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminDatabaseResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<array<string,Object>>|null */
        public ?array $results=null,
        /** @var int|null */
        public ?int $total=null,
        /** @var array<MetadataPropertyType>|null */
        public ?array $columns=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('Dictionary<String,Object>', $o['results']);
        if (isset($o['total'])) $this->total = $o['total'];
        if (isset($o['columns'])) $this->columns = JsonConverters::fromArray('MetadataPropertyType', $o['columns']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('Dictionary<String,Object>', $this->results);
        if (isset($this->total)) $o['total'] = $this->total;
        if (isset($this->columns)) $o['columns'] = JsonConverters::toArray('MetadataPropertyType', $this->columns);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class ViewCommandsResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<CommandSummary>|null */
        public ?array $commandTotals=null,
        /** @var array<CommandResult>|null */
        public ?array $latestCommands=null,
        /** @var array<CommandResult>|null */
        public ?array $latestFailed=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['commandTotals'])) $this->commandTotals = JsonConverters::fromArray('CommandSummary', $o['commandTotals']);
        if (isset($o['latestCommands'])) $this->latestCommands = JsonConverters::fromArray('CommandResult', $o['latestCommands']);
        if (isset($o['latestFailed'])) $this->latestFailed = JsonConverters::fromArray('CommandResult', $o['latestFailed']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->commandTotals)) $o['commandTotals'] = JsonConverters::toArray('CommandSummary', $this->commandTotals);
        if (isset($this->latestCommands)) $o['latestCommands'] = JsonConverters::toArray('CommandResult', $this->latestCommands);
        if (isset($this->latestFailed)) $o['latestFailed'] = JsonConverters::toArray('CommandResult', $this->latestFailed);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class ExecuteCommandResponse implements JsonSerializable
{
    public function __construct(
        /** @var CommandResult|null */
        public ?CommandResult $commandResult=null,
        /** @var string|null */
        public ?string $result=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['commandResult'])) $this->commandResult = JsonConverters::from('CommandResult', $o['commandResult']);
        if (isset($o['result'])) $this->result = $o['result'];
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->commandResult)) $o['commandResult'] = JsonConverters::to('CommandResult', $this->commandResult);
        if (isset($this->result)) $o['result'] = $this->result;
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminApiKeysResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var array<PartialApiKey>|null */
        public ?array $results=null,

        // @DataMember(Order=2)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('PartialApiKey', $o['results']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('PartialApiKey', $this->results);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class AdminApiKeyResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $result=null,

        // @DataMember(Order=2)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['result'])) $this->result = $o['result'];
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->result)) $o['result'] = $this->result;
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminJobDashboardResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<JobStatSummary>|null */
        public ?array $commands=null,
        /** @var array<JobStatSummary>|null */
        public ?array $apis=null,
        /** @var array<JobStatSummary>|null */
        public ?array $workers=null,
        /** @var array<HourSummary>|null */
        public ?array $today=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['commands'])) $this->commands = JsonConverters::fromArray('JobStatSummary', $o['commands']);
        if (isset($o['apis'])) $this->apis = JsonConverters::fromArray('JobStatSummary', $o['apis']);
        if (isset($o['workers'])) $this->workers = JsonConverters::fromArray('JobStatSummary', $o['workers']);
        if (isset($o['today'])) $this->today = JsonConverters::fromArray('HourSummary', $o['today']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->commands)) $o['commands'] = JsonConverters::toArray('JobStatSummary', $this->commands);
        if (isset($this->apis)) $o['apis'] = JsonConverters::toArray('JobStatSummary', $this->apis);
        if (isset($this->workers)) $o['workers'] = JsonConverters::toArray('JobStatSummary', $this->workers);
        if (isset($this->today)) $o['today'] = JsonConverters::toArray('HourSummary', $this->today);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminJobInfoResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<DateTime>|null */
        public ?array $monthDbs=null,
        /** @var array<string,int>|null */
        public ?array $tableCounts=null,
        /** @var array<WorkerStats>|null */
        public ?array $workerStats=null,
        /** @var array<string,int>|null */
        public ?array $queueCounts=null,
        /** @var array<string,int>|null */
        public ?array $workerCounts=null,
        /** @var array<string,int>|null */
        public ?array $stateCounts=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['monthDbs'])) $this->monthDbs = JsonConverters::fromArray('DateTime', $o['monthDbs']);
        if (isset($o['tableCounts'])) $this->tableCounts = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','int']), $o['tableCounts']);
        if (isset($o['workerStats'])) $this->workerStats = JsonConverters::fromArray('WorkerStats', $o['workerStats']);
        if (isset($o['queueCounts'])) $this->queueCounts = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','int']), $o['queueCounts']);
        if (isset($o['workerCounts'])) $this->workerCounts = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','int']), $o['workerCounts']);
        if (isset($o['stateCounts'])) $this->stateCounts = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['BackgroundJobState','int']), $o['stateCounts']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->monthDbs)) $o['monthDbs'] = JsonConverters::toArray('DateTime', $this->monthDbs);
        if (isset($this->tableCounts)) $o['tableCounts'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','int']), $this->tableCounts);
        if (isset($this->workerStats)) $o['workerStats'] = JsonConverters::toArray('WorkerStats', $this->workerStats);
        if (isset($this->queueCounts)) $o['queueCounts'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','int']), $this->queueCounts);
        if (isset($this->workerCounts)) $o['workerCounts'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','int']), $this->workerCounts);
        if (isset($this->stateCounts)) $o['stateCounts'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['BackgroundJobState','int']), $this->stateCounts);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminGetJobResponse implements JsonSerializable
{
    public function __construct(
        /** @var JobSummary|null */
        public ?JobSummary $result=null,
        /** @var BackgroundJob|null */
        public ?BackgroundJob $queued=null,
        /** @var CompletedJob|null */
        public ?CompletedJob $completed=null,
        /** @var FailedJob|null */
        public ?FailedJob $failed=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['result'])) $this->result = JsonConverters::from('JobSummary', $o['result']);
        if (isset($o['queued'])) $this->queued = JsonConverters::from('BackgroundJob', $o['queued']);
        if (isset($o['completed'])) $this->completed = JsonConverters::from('CompletedJob', $o['completed']);
        if (isset($o['failed'])) $this->failed = JsonConverters::from('FailedJob', $o['failed']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->result)) $o['result'] = JsonConverters::to('JobSummary', $this->result);
        if (isset($this->queued)) $o['queued'] = JsonConverters::to('BackgroundJob', $this->queued);
        if (isset($this->completed)) $o['completed'] = JsonConverters::to('CompletedJob', $this->completed);
        if (isset($this->failed)) $o['failed'] = JsonConverters::to('FailedJob', $this->failed);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminGetJobProgressResponse implements JsonSerializable
{
    public function __construct(
        /** @var BackgroundJobState|null */
        public ?BackgroundJobState $state=null,
        /** @var float|null */
        public ?float $progress=null,
        /** @var string|null */
        public ?string $status=null,
        /** @var string|null */
        public ?string $logs=null,
        /** @var int|null */
        public ?int $durationMs=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $error=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['state'])) $this->state = JsonConverters::from('BackgroundJobState', $o['state']);
        if (isset($o['progress'])) $this->progress = $o['progress'];
        if (isset($o['status'])) $this->status = $o['status'];
        if (isset($o['logs'])) $this->logs = $o['logs'];
        if (isset($o['durationMs'])) $this->durationMs = $o['durationMs'];
        if (isset($o['error'])) $this->error = JsonConverters::from('ResponseStatus', $o['error']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->state)) $o['state'] = JsonConverters::to('BackgroundJobState', $this->state);
        if (isset($this->progress)) $o['progress'] = $this->progress;
        if (isset($this->status)) $o['status'] = $this->status;
        if (isset($this->logs)) $o['logs'] = $this->logs;
        if (isset($this->durationMs)) $o['durationMs'] = $this->durationMs;
        if (isset($this->error)) $o['error'] = JsonConverters::to('ResponseStatus', $this->error);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminRequeueFailedJobsJobsResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<int>|null */
        public ?array $results=null,
        /** @var array<string,string>|null */
        public ?array $errors=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('int', $o['results']);
        if (isset($o['errors'])) $this->errors = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['int','string']), $o['errors']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('int', $this->results);
        if (isset($this->errors)) $o['errors'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['int','string']), $this->errors);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

class AdminCancelJobsResponse implements JsonSerializable
{
    public function __construct(
        /** @var array<int>|null */
        public ?array $results=null,
        /** @var array<string,string>|null */
        public ?array $errors=null,
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('int', $o['results']);
        if (isset($o['errors'])) $this->errors = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['int','string']), $o['errors']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('int', $this->results);
        if (isset($this->errors)) $o['errors'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['int','string']), $this->errors);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class RequestLogsResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var array<RequestLogEntry>|null */
        public ?array $results=null,

        // @DataMember(Order=2)
        /** @var array<string,string>|null */
        public ?array $usage=null,

        // @DataMember(Order=3)
        /** @var int */
        public int $total=0,

        // @DataMember(Order=4)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('RequestLogEntry', $o['results']);
        if (isset($o['usage'])) $this->usage = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['usage']);
        if (isset($o['total'])) $this->total = $o['total'];
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('RequestLogEntry', $this->results);
        if (isset($this->usage)) $o['usage'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->usage);
        if (isset($this->total)) $o['total'] = $this->total;
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
class GetValidationRulesResponse implements JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var array<ValidationRule>|null */
        public ?array $results=null,

        // @DataMember(Order=2)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['results'])) $this->results = JsonConverters::fromArray('ValidationRule', $o['results']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->results)) $o['results'] = JsonConverters::toArray('ValidationRule', $this->results);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
}

// @DataContract
#[Returns('IdResponse')]
class AdminCreateRole implements IReturn, IPost, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $name=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminCreateRole'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new IdResponse(); }
}

// @DataContract
#[Returns('AdminGetRolesResponse')]
class AdminGetRoles implements IReturn, IGet, JsonSerializable
{
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminGetRoles'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminGetRolesResponse(); }
}

// @DataContract
#[Returns('AdminGetRoleResponse')]
class AdminGetRole implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $id=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminGetRole'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminGetRoleResponse(); }
}

// @DataContract
#[Returns('IdResponse')]
class AdminUpdateRole implements IReturn, IPost, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $id=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $name=null,

        // @DataMember(Order=3)
        /** @var array<Property>|null */
        public ?array $addClaims=null,

        // @DataMember(Order=4)
        /** @var array<Property>|null */
        public ?array $removeClaims=null,

        // @DataMember(Order=5)
        /** @var ResponseStatus|null */
        public ?ResponseStatus $responseStatus=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['addClaims'])) $this->addClaims = JsonConverters::fromArray('Property', $o['addClaims']);
        if (isset($o['removeClaims'])) $this->removeClaims = JsonConverters::fromArray('Property', $o['removeClaims']);
        if (isset($o['responseStatus'])) $this->responseStatus = JsonConverters::from('ResponseStatus', $o['responseStatus']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->addClaims)) $o['addClaims'] = JsonConverters::toArray('Property', $this->addClaims);
        if (isset($this->removeClaims)) $o['removeClaims'] = JsonConverters::toArray('Property', $this->removeClaims);
        if (isset($this->responseStatus)) $o['responseStatus'] = JsonConverters::to('ResponseStatus', $this->responseStatus);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminUpdateRole'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new IdResponse(); }
}

// @DataContract
class AdminDeleteRole implements IReturnVoid, IDelete, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $id=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminDeleteRole'; }
    public function getMethod(): string { return 'DELETE'; }
    public function createResponse(): void {}
}

#[Returns('AdminDashboardResponse')]
class AdminDashboard implements IReturn, IGet, JsonSerializable
{
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminDashboard'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminDashboardResponse(); }
}

// @DataContract
#[Returns('AdminUserResponse')]
class AdminGetUser implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=10)
        /** @var string|null */
        public ?string $id=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminGetUser'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminUserResponse(); }
}

// @DataContract
#[Returns('AdminUsersResponse')]
class AdminQueryUsers implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $query=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $orderBy=null,

        // @DataMember(Order=3)
        /** @var int|null */
        public ?int $skip=null,

        // @DataMember(Order=4)
        /** @var int|null */
        public ?int $take=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['query'])) $this->query = $o['query'];
        if (isset($o['orderBy'])) $this->orderBy = $o['orderBy'];
        if (isset($o['skip'])) $this->skip = $o['skip'];
        if (isset($o['take'])) $this->take = $o['take'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->query)) $o['query'] = $this->query;
        if (isset($this->orderBy)) $o['orderBy'] = $this->orderBy;
        if (isset($this->skip)) $o['skip'] = $this->skip;
        if (isset($this->take)) $o['take'] = $this->take;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryUsers'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminUsersResponse(); }
}

// @DataContract
#[Returns('AdminUserResponse')]
class AdminCreateUser extends AdminUserBase implements IReturn, IPost, JsonSerializable
{
    /**
     * @param string|null $userName
     * @param string|null $firstName
     * @param string|null $lastName
     * @param string|null $displayName
     * @param string|null $email
     * @param string|null $password
     * @param string|null $profileUrl
     * @param string|null $phoneNumber
     * @param array<string,string>|null $userAuthProperties
     * @param array<string,string>|null $meta
     */
    public function __construct(
        ?string $userName=null,
        ?string $firstName=null,
        ?string $lastName=null,
        ?string $displayName=null,
        ?string $email=null,
        ?string $password=null,
        ?string $profileUrl=null,
        ?string $phoneNumber=null,
        ?array $userAuthProperties=null,
        ?array $meta=null,
        // @DataMember(Order=10)
        /** @var array<string>|null */
        public ?array $roles=null,

        // @DataMember(Order=11)
        /** @var array<string>|null */
        public ?array $permissions=null
    ) {
        parent::__construct($userName,$firstName,$lastName,$displayName,$email,$password,$profileUrl,$phoneNumber,$userAuthProperties,$meta);
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['roles'])) $this->roles = JsonConverters::fromArray('string', $o['roles']);
        if (isset($o['permissions'])) $this->permissions = JsonConverters::fromArray('string', $o['permissions']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->roles)) $o['roles'] = JsonConverters::toArray('string', $this->roles);
        if (isset($this->permissions)) $o['permissions'] = JsonConverters::toArray('string', $this->permissions);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminCreateUser'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new AdminUserResponse(); }
}

// @DataContract
#[Returns('AdminUserResponse')]
class AdminUpdateUser extends AdminUserBase implements IReturn, IPut, JsonSerializable
{
    /**
     * @param string|null $userName
     * @param string|null $firstName
     * @param string|null $lastName
     * @param string|null $displayName
     * @param string|null $email
     * @param string|null $password
     * @param string|null $profileUrl
     * @param string|null $phoneNumber
     * @param array<string,string>|null $userAuthProperties
     * @param array<string,string>|null $meta
     */
    public function __construct(
        ?string $userName=null,
        ?string $firstName=null,
        ?string $lastName=null,
        ?string $displayName=null,
        ?string $email=null,
        ?string $password=null,
        ?string $profileUrl=null,
        ?string $phoneNumber=null,
        ?array $userAuthProperties=null,
        ?array $meta=null,
        // @DataMember(Order=10)
        /** @var string|null */
        public ?string $id=null,

        // @DataMember(Order=11)
        /** @var bool|null */
        public ?bool $lockUser=null,

        // @DataMember(Order=12)
        /** @var bool|null */
        public ?bool $unlockUser=null,

        // @DataMember(Order=13)
        /** @var DateTime|null */
        public ?DateTime $lockUserUntil=null,

        // @DataMember(Order=14)
        /** @var array<string>|null */
        public ?array $addRoles=null,

        // @DataMember(Order=15)
        /** @var array<string>|null */
        public ?array $removeRoles=null,

        // @DataMember(Order=16)
        /** @var array<string>|null */
        public ?array $addPermissions=null,

        // @DataMember(Order=17)
        /** @var array<string>|null */
        public ?array $removePermissions=null,

        // @DataMember(Order=18)
        /** @var array<Property>|null */
        public ?array $addClaims=null,

        // @DataMember(Order=19)
        /** @var array<Property>|null */
        public ?array $removeClaims=null
    ) {
        parent::__construct($userName,$firstName,$lastName,$displayName,$email,$password,$profileUrl,$phoneNumber,$userAuthProperties,$meta);
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['lockUser'])) $this->lockUser = $o['lockUser'];
        if (isset($o['unlockUser'])) $this->unlockUser = $o['unlockUser'];
        if (isset($o['lockUserUntil'])) $this->lockUserUntil = JsonConverters::from('DateTimeOffset', $o['lockUserUntil']);
        if (isset($o['addRoles'])) $this->addRoles = JsonConverters::fromArray('string', $o['addRoles']);
        if (isset($o['removeRoles'])) $this->removeRoles = JsonConverters::fromArray('string', $o['removeRoles']);
        if (isset($o['addPermissions'])) $this->addPermissions = JsonConverters::fromArray('string', $o['addPermissions']);
        if (isset($o['removePermissions'])) $this->removePermissions = JsonConverters::fromArray('string', $o['removePermissions']);
        if (isset($o['addClaims'])) $this->addClaims = JsonConverters::fromArray('Property', $o['addClaims']);
        if (isset($o['removeClaims'])) $this->removeClaims = JsonConverters::fromArray('Property', $o['removeClaims']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->lockUser)) $o['lockUser'] = $this->lockUser;
        if (isset($this->unlockUser)) $o['unlockUser'] = $this->unlockUser;
        if (isset($this->lockUserUntil)) $o['lockUserUntil'] = JsonConverters::to('DateTimeOffset', $this->lockUserUntil);
        if (isset($this->addRoles)) $o['addRoles'] = JsonConverters::toArray('string', $this->addRoles);
        if (isset($this->removeRoles)) $o['removeRoles'] = JsonConverters::toArray('string', $this->removeRoles);
        if (isset($this->addPermissions)) $o['addPermissions'] = JsonConverters::toArray('string', $this->addPermissions);
        if (isset($this->removePermissions)) $o['removePermissions'] = JsonConverters::toArray('string', $this->removePermissions);
        if (isset($this->addClaims)) $o['addClaims'] = JsonConverters::toArray('Property', $this->addClaims);
        if (isset($this->removeClaims)) $o['removeClaims'] = JsonConverters::toArray('Property', $this->removeClaims);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminUpdateUser'; }
    public function getMethod(): string { return 'PUT'; }
    public function createResponse(): mixed { return new AdminUserResponse(); }
}

// @DataContract
#[Returns('AdminDeleteUserResponse')]
class AdminDeleteUser implements IReturn, IDelete, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=10)
        /** @var string|null */
        public ?string $id=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminDeleteUser'; }
    public function getMethod(): string { return 'DELETE'; }
    public function createResponse(): mixed { return new AdminDeleteUserResponse(); }
}

#[Returns('QueryResponse')]
/**
 * @template QueryDb of RequestLog
 */
class AdminQueryRequestLogs extends QueryDb implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var DateTime|null */
        public ?DateTime $month=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['month'])) $this->month = JsonConverters::from('DateTime', $o['month']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->month)) $o['month'] = JsonConverters::to('DateTime', $this->month);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryRequestLogs'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return QueryResponse::create(genericArgs:['RequestLog']); }
}

#[Returns('AdminProfilingResponse')]
class AdminProfiling implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $source=null,
        /** @var string|null */
        public ?string $eventType=null,
        /** @var int|null */
        public ?int $threadId=null,
        /** @var string|null */
        public ?string $traceId=null,
        /** @var string|null */
        public ?string $userAuthId=null,
        /** @var string|null */
        public ?string $sessionId=null,
        /** @var string|null */
        public ?string $tag=null,
        /** @var int */
        public int $skip=0,
        /** @var int|null */
        public ?int $take=null,
        /** @var string|null */
        public ?string $orderBy=null,
        /** @var bool|null */
        public ?bool $withErrors=null,
        /** @var bool|null */
        public ?bool $pending=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['source'])) $this->source = $o['source'];
        if (isset($o['eventType'])) $this->eventType = $o['eventType'];
        if (isset($o['threadId'])) $this->threadId = $o['threadId'];
        if (isset($o['traceId'])) $this->traceId = $o['traceId'];
        if (isset($o['userAuthId'])) $this->userAuthId = $o['userAuthId'];
        if (isset($o['sessionId'])) $this->sessionId = $o['sessionId'];
        if (isset($o['tag'])) $this->tag = $o['tag'];
        if (isset($o['skip'])) $this->skip = $o['skip'];
        if (isset($o['take'])) $this->take = $o['take'];
        if (isset($o['orderBy'])) $this->orderBy = $o['orderBy'];
        if (isset($o['withErrors'])) $this->withErrors = $o['withErrors'];
        if (isset($o['pending'])) $this->pending = $o['pending'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->source)) $o['source'] = $this->source;
        if (isset($this->eventType)) $o['eventType'] = $this->eventType;
        if (isset($this->threadId)) $o['threadId'] = $this->threadId;
        if (isset($this->traceId)) $o['traceId'] = $this->traceId;
        if (isset($this->userAuthId)) $o['userAuthId'] = $this->userAuthId;
        if (isset($this->sessionId)) $o['sessionId'] = $this->sessionId;
        if (isset($this->tag)) $o['tag'] = $this->tag;
        if (isset($this->skip)) $o['skip'] = $this->skip;
        if (isset($this->take)) $o['take'] = $this->take;
        if (isset($this->orderBy)) $o['orderBy'] = $this->orderBy;
        if (isset($this->withErrors)) $o['withErrors'] = $this->withErrors;
        if (isset($this->pending)) $o['pending'] = $this->pending;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminProfiling'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new AdminProfilingResponse(); }
}

#[Returns('AdminRedisResponse')]
class AdminRedis implements IReturn, IPost, JsonSerializable
{
    public function __construct(
        /** @var int|null */
        public ?int $db=null,
        /** @var string|null */
        public ?string $query=null,
        /** @var RedisEndpointInfo|null */
        public ?RedisEndpointInfo $reconnect=null,
        /** @var int|null */
        public ?int $take=null,
        /** @var int|null */
        public ?int $position=null,
        /** @var array<string>|null */
        public ?array $args=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['db'])) $this->db = $o['db'];
        if (isset($o['query'])) $this->query = $o['query'];
        if (isset($o['reconnect'])) $this->reconnect = JsonConverters::from('RedisEndpointInfo', $o['reconnect']);
        if (isset($o['take'])) $this->take = $o['take'];
        if (isset($o['position'])) $this->position = $o['position'];
        if (isset($o['args'])) $this->args = JsonConverters::fromArray('string', $o['args']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->db)) $o['db'] = $this->db;
        if (isset($this->query)) $o['query'] = $this->query;
        if (isset($this->reconnect)) $o['reconnect'] = JsonConverters::to('RedisEndpointInfo', $this->reconnect);
        if (isset($this->take)) $o['take'] = $this->take;
        if (isset($this->position)) $o['position'] = $this->position;
        if (isset($this->args)) $o['args'] = JsonConverters::toArray('string', $this->args);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminRedis'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new AdminRedisResponse(); }
}

#[Returns('AdminDatabaseResponse')]
class AdminDatabase implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        /** @var string|null */
        public ?string $db=null,
        /** @var string|null */
        public ?string $schema=null,
        /** @var string|null */
        public ?string $table=null,
        /** @var array<string>|null */
        public ?array $fields=null,
        /** @var int|null */
        public ?int $take=null,
        /** @var int|null */
        public ?int $skip=null,
        /** @var string|null */
        public ?string $orderBy=null,
        /** @var string|null */
        public ?string $include=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['db'])) $this->db = $o['db'];
        if (isset($o['schema'])) $this->schema = $o['schema'];
        if (isset($o['table'])) $this->table = $o['table'];
        if (isset($o['fields'])) $this->fields = JsonConverters::fromArray('string', $o['fields']);
        if (isset($o['take'])) $this->take = $o['take'];
        if (isset($o['skip'])) $this->skip = $o['skip'];
        if (isset($o['orderBy'])) $this->orderBy = $o['orderBy'];
        if (isset($o['include'])) $this->include = $o['include'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->db)) $o['db'] = $this->db;
        if (isset($this->schema)) $o['schema'] = $this->schema;
        if (isset($this->table)) $o['table'] = $this->table;
        if (isset($this->fields)) $o['fields'] = JsonConverters::toArray('string', $this->fields);
        if (isset($this->take)) $o['take'] = $this->take;
        if (isset($this->skip)) $o['skip'] = $this->skip;
        if (isset($this->orderBy)) $o['orderBy'] = $this->orderBy;
        if (isset($this->include)) $o['include'] = $this->include;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminDatabase'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminDatabaseResponse(); }
}

#[Returns('ViewCommandsResponse')]
class ViewCommands implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        /** @var array<string>|null */
        public ?array $include=null,
        /** @var int|null */
        public ?int $skip=null,
        /** @var int|null */
        public ?int $take=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['include'])) $this->include = JsonConverters::fromArray('string', $o['include']);
        if (isset($o['skip'])) $this->skip = $o['skip'];
        if (isset($o['take'])) $this->take = $o['take'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->include)) $o['include'] = JsonConverters::toArray('string', $this->include);
        if (isset($this->skip)) $o['skip'] = $this->skip;
        if (isset($this->take)) $o['take'] = $this->take;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'ViewCommands'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new ViewCommandsResponse(); }
}

#[Returns('ExecuteCommandResponse')]
class ExecuteCommand implements IReturn, IPost, JsonSerializable
{
    public function __construct(
        /** @var string */
        public string $command='',
        /** @var string|null */
        public ?string $requestJson=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['command'])) $this->command = $o['command'];
        if (isset($o['requestJson'])) $this->requestJson = $o['requestJson'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->command)) $o['command'] = $this->command;
        if (isset($this->requestJson)) $o['requestJson'] = $this->requestJson;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'ExecuteCommand'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new ExecuteCommandResponse(); }
}

// @DataContract
#[Returns('AdminApiKeysResponse')]
class AdminQueryApiKeys implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var int|null */
        public ?int $id=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $search=null,

        // @DataMember(Order=3)
        /** @var string|null */
        public ?string $userId=null,

        // @DataMember(Order=4)
        /** @var string|null */
        public ?string $userName=null,

        // @DataMember(Order=5)
        /** @var string|null */
        public ?string $orderBy=null,

        // @DataMember(Order=6)
        /** @var int|null */
        public ?int $skip=null,

        // @DataMember(Order=7)
        /** @var int|null */
        public ?int $take=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['search'])) $this->search = $o['search'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['userName'])) $this->userName = $o['userName'];
        if (isset($o['orderBy'])) $this->orderBy = $o['orderBy'];
        if (isset($o['skip'])) $this->skip = $o['skip'];
        if (isset($o['take'])) $this->take = $o['take'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->search)) $o['search'] = $this->search;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->userName)) $o['userName'] = $this->userName;
        if (isset($this->orderBy)) $o['orderBy'] = $this->orderBy;
        if (isset($this->skip)) $o['skip'] = $this->skip;
        if (isset($this->take)) $o['take'] = $this->take;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryApiKeys'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminApiKeysResponse(); }
}

// @DataContract
#[Returns('AdminApiKeyResponse')]
class AdminCreateApiKey implements IReturn, IPost, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $name=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $userId=null,

        // @DataMember(Order=3)
        /** @var string|null */
        public ?string $userName=null,

        // @DataMember(Order=4)
        /** @var array<string>|null */
        public ?array $scopes=null,

        // @DataMember(Order=5)
        /** @var array<string>|null */
        public ?array $features=null,

        // @DataMember(Order=6)
        /** @var array<string>|null */
        public ?array $restrictTo=null,

        // @DataMember(Order=7)
        /** @var DateTime|null */
        public ?DateTime $expiryDate=null,

        // @DataMember(Order=8)
        /** @var string|null */
        public ?string $notes=null,

        // @DataMember(Order=9)
        /** @var int|null */
        public ?int $refId=null,

        // @DataMember(Order=10)
        /** @var string|null */
        public ?string $refIdStr=null,

        // @DataMember(Order=11)
        /** @var array<string,string>|null */
        public ?array $meta=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['userName'])) $this->userName = $o['userName'];
        if (isset($o['scopes'])) $this->scopes = JsonConverters::fromArray('string', $o['scopes']);
        if (isset($o['features'])) $this->features = JsonConverters::fromArray('string', $o['features']);
        if (isset($o['restrictTo'])) $this->restrictTo = JsonConverters::fromArray('string', $o['restrictTo']);
        if (isset($o['expiryDate'])) $this->expiryDate = JsonConverters::from('DateTime', $o['expiryDate']);
        if (isset($o['notes'])) $this->notes = $o['notes'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['refIdStr'])) $this->refIdStr = $o['refIdStr'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->userName)) $o['userName'] = $this->userName;
        if (isset($this->scopes)) $o['scopes'] = JsonConverters::toArray('string', $this->scopes);
        if (isset($this->features)) $o['features'] = JsonConverters::toArray('string', $this->features);
        if (isset($this->restrictTo)) $o['restrictTo'] = JsonConverters::toArray('string', $this->restrictTo);
        if (isset($this->expiryDate)) $o['expiryDate'] = JsonConverters::to('DateTime', $this->expiryDate);
        if (isset($this->notes)) $o['notes'] = $this->notes;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->refIdStr)) $o['refIdStr'] = $this->refIdStr;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminCreateApiKey'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new AdminApiKeyResponse(); }
}

// @DataContract
#[Returns('EmptyResponse')]
class AdminUpdateApiKey implements IReturn, IPatch, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        // @Validate(Validator="GreaterThan(0)")
        /** @var int */
        public int $id=0,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $name=null,

        // @DataMember(Order=3)
        /** @var string|null */
        public ?string $userId=null,

        // @DataMember(Order=4)
        /** @var string|null */
        public ?string $userName=null,

        // @DataMember(Order=5)
        /** @var array<string>|null */
        public ?array $scopes=null,

        // @DataMember(Order=6)
        /** @var array<string>|null */
        public ?array $features=null,

        // @DataMember(Order=7)
        /** @var array<string>|null */
        public ?array $restrictTo=null,

        // @DataMember(Order=8)
        /** @var DateTime|null */
        public ?DateTime $expiryDate=null,

        // @DataMember(Order=9)
        /** @var DateTime|null */
        public ?DateTime $cancelledDate=null,

        // @DataMember(Order=10)
        /** @var string|null */
        public ?string $notes=null,

        // @DataMember(Order=11)
        /** @var int|null */
        public ?int $refId=null,

        // @DataMember(Order=12)
        /** @var string|null */
        public ?string $refIdStr=null,

        // @DataMember(Order=13)
        /** @var array<string,string>|null */
        public ?array $meta=null,

        // @DataMember(Order=14)
        /** @var array<string>|null */
        public ?array $reset=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['name'])) $this->name = $o['name'];
        if (isset($o['userId'])) $this->userId = $o['userId'];
        if (isset($o['userName'])) $this->userName = $o['userName'];
        if (isset($o['scopes'])) $this->scopes = JsonConverters::fromArray('string', $o['scopes']);
        if (isset($o['features'])) $this->features = JsonConverters::fromArray('string', $o['features']);
        if (isset($o['restrictTo'])) $this->restrictTo = JsonConverters::fromArray('string', $o['restrictTo']);
        if (isset($o['expiryDate'])) $this->expiryDate = JsonConverters::from('DateTime', $o['expiryDate']);
        if (isset($o['cancelledDate'])) $this->cancelledDate = JsonConverters::from('DateTime', $o['cancelledDate']);
        if (isset($o['notes'])) $this->notes = $o['notes'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
        if (isset($o['refIdStr'])) $this->refIdStr = $o['refIdStr'];
        if (isset($o['meta'])) $this->meta = JsonConverters::from(JsonConverters::context('Dictionary',genericArgs:['string','string']), $o['meta']);
        if (isset($o['reset'])) $this->reset = JsonConverters::fromArray('string', $o['reset']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->name)) $o['name'] = $this->name;
        if (isset($this->userId)) $o['userId'] = $this->userId;
        if (isset($this->userName)) $o['userName'] = $this->userName;
        if (isset($this->scopes)) $o['scopes'] = JsonConverters::toArray('string', $this->scopes);
        if (isset($this->features)) $o['features'] = JsonConverters::toArray('string', $this->features);
        if (isset($this->restrictTo)) $o['restrictTo'] = JsonConverters::toArray('string', $this->restrictTo);
        if (isset($this->expiryDate)) $o['expiryDate'] = JsonConverters::to('DateTime', $this->expiryDate);
        if (isset($this->cancelledDate)) $o['cancelledDate'] = JsonConverters::to('DateTime', $this->cancelledDate);
        if (isset($this->notes)) $o['notes'] = $this->notes;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        if (isset($this->refIdStr)) $o['refIdStr'] = $this->refIdStr;
        if (isset($this->meta)) $o['meta'] = JsonConverters::to(JsonConverters::context('Dictionary',genericArgs:['string','string']), $this->meta);
        if (isset($this->reset)) $o['reset'] = JsonConverters::toArray('string', $this->reset);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminUpdateApiKey'; }
    public function getMethod(): string { return 'PATCH'; }
    public function createResponse(): mixed { return new EmptyResponse(); }
}

// @DataContract
#[Returns('EmptyResponse')]
class AdminDeleteApiKey implements IReturn, IDelete, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        // @Validate(Validator="GreaterThan(0)")
        /** @var int|null */
        public ?int $id=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminDeleteApiKey'; }
    public function getMethod(): string { return 'DELETE'; }
    public function createResponse(): mixed { return new EmptyResponse(); }
}

#[Returns('AdminJobDashboardResponse')]
class AdminJobDashboard implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        /** @var DateTime|null */
        public ?DateTime $from=null,
        /** @var DateTime|null */
        public ?DateTime $to=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['from'])) $this->from = JsonConverters::from('DateTime', $o['from']);
        if (isset($o['to'])) $this->to = JsonConverters::from('DateTime', $o['to']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->from)) $o['from'] = JsonConverters::to('DateTime', $this->from);
        if (isset($this->to)) $o['to'] = JsonConverters::to('DateTime', $this->to);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminJobDashboard'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminJobDashboardResponse(); }
}

#[Returns('AdminJobInfoResponse')]
class AdminJobInfo implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        /** @var DateTime|null */
        public ?DateTime $month=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['month'])) $this->month = JsonConverters::from('DateTime', $o['month']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->month)) $o['month'] = JsonConverters::to('DateTime', $this->month);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminJobInfo'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminJobInfoResponse(); }
}

#[Returns('AdminGetJobResponse')]
class AdminGetJob implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        /** @var int|null */
        public ?int $id=null,
        /** @var string|null */
        public ?string $refId=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminGetJob'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminGetJobResponse(); }
}

#[Returns('AdminGetJobProgressResponse')]
class AdminGetJobProgress implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @Validate(Validator="GreaterThan(0)")
        /** @var int */
        public int $id=0,

        /** @var int|null */
        public ?int $logStart=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['logStart'])) $this->logStart = $o['logStart'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->logStart)) $o['logStart'] = $this->logStart;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminGetJobProgress'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminGetJobProgressResponse(); }
}

#[Returns('QueryResponse')]
/**
 * @template QueryDb of BackgroundJob
 */
class AdminQueryBackgroundJobs extends QueryDb implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var int|null */
        public ?int $id=null,
        /** @var string|null */
        public ?string $refId=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryBackgroundJobs'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return QueryResponse::create(genericArgs:['BackgroundJob']); }
}

#[Returns('QueryResponse')]
/**
 * @template QueryDb of JobSummary
 */
class AdminQueryJobSummary extends QueryDb implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var int|null */
        public ?int $id=null,
        /** @var string|null */
        public ?string $refId=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['id'])) $this->id = $o['id'];
        if (isset($o['refId'])) $this->refId = $o['refId'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->id)) $o['id'] = $this->id;
        if (isset($this->refId)) $o['refId'] = $this->refId;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryJobSummary'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return QueryResponse::create(genericArgs:['JobSummary']); }
}

#[Returns('QueryResponse')]
/**
 * @template QueryDb of ScheduledTask
 */
class AdminQueryScheduledTasks extends QueryDb implements IReturn, JsonSerializable
{
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryScheduledTasks'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return QueryResponse::create(genericArgs:['ScheduledTask']); }
}

#[Returns('QueryResponse')]
/**
 * @template QueryDb of CompletedJob
 */
class AdminQueryCompletedJobs extends QueryDb implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var DateTime|null */
        public ?DateTime $month=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['month'])) $this->month = JsonConverters::from('DateTime', $o['month']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->month)) $o['month'] = JsonConverters::to('DateTime', $this->month);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryCompletedJobs'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return QueryResponse::create(genericArgs:['CompletedJob']); }
}

#[Returns('QueryResponse')]
/**
 * @template QueryDb of FailedJob
 */
class AdminQueryFailedJobs extends QueryDb implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var DateTime|null */
        public ?DateTime $month=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        parent::fromMap($o);
        if (isset($o['month'])) $this->month = JsonConverters::from('DateTime', $o['month']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = parent::jsonSerialize();
        if (isset($this->month)) $o['month'] = JsonConverters::to('DateTime', $this->month);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminQueryFailedJobs'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return QueryResponse::create(genericArgs:['FailedJob']); }
}

#[Returns('AdminRequeueFailedJobsJobsResponse')]
class AdminRequeueFailedJobs implements IReturn, JsonSerializable
{
    public function __construct(
        /** @var array<int>|null */
        public ?array $ids=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['ids'])) $this->ids = JsonConverters::fromArray('int', $o['ids']);
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->ids)) $o['ids'] = JsonConverters::toArray('int', $this->ids);
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminRequeueFailedJobs'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): mixed { return new AdminRequeueFailedJobsJobsResponse(); }
}

#[Returns('AdminCancelJobsResponse')]
class AdminCancelJobs implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        /** @var array<int>|null */
        public ?array $ids=null,
        /** @var string|null */
        public ?string $worker=null,
        /** @var BackgroundJobState|null */
        public ?BackgroundJobState $state=null,
        /** @var string|null */
        public ?string $cancelWorker=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['ids'])) $this->ids = JsonConverters::fromArray('int', $o['ids']);
        if (isset($o['worker'])) $this->worker = $o['worker'];
        if (isset($o['state'])) $this->state = JsonConverters::from('BackgroundJobState', $o['state']);
        if (isset($o['cancelWorker'])) $this->cancelWorker = $o['cancelWorker'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->ids)) $o['ids'] = JsonConverters::toArray('int', $this->ids);
        if (isset($this->worker)) $o['worker'] = $this->worker;
        if (isset($this->state)) $o['state'] = JsonConverters::to('BackgroundJobState', $this->state);
        if (isset($this->cancelWorker)) $o['cancelWorker'] = $this->cancelWorker;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'AdminCancelJobs'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new AdminCancelJobsResponse(); }
}

// @Route("/requestlogs")
// @DataContract
#[Returns('RequestLogsResponse')]
class RequestLogs implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var int|null */
        public ?int $beforeSecs=null,

        // @DataMember(Order=2)
        /** @var int|null */
        public ?int $afterSecs=null,

        // @DataMember(Order=3)
        /** @var string|null */
        public ?string $operationName=null,

        // @DataMember(Order=4)
        /** @var string|null */
        public ?string $ipAddress=null,

        // @DataMember(Order=5)
        /** @var string|null */
        public ?string $forwardedFor=null,

        // @DataMember(Order=6)
        /** @var string|null */
        public ?string $userAuthId=null,

        // @DataMember(Order=7)
        /** @var string|null */
        public ?string $sessionId=null,

        // @DataMember(Order=8)
        /** @var string|null */
        public ?string $referer=null,

        // @DataMember(Order=9)
        /** @var string|null */
        public ?string $pathInfo=null,

        // @DataMember(Order=10)
        /** @var int[]|null */
        public ?array $ids=null,

        // @DataMember(Order=11)
        /** @var int|null */
        public ?int $beforeId=null,

        // @DataMember(Order=12)
        /** @var int|null */
        public ?int $afterId=null,

        // @DataMember(Order=13)
        /** @var bool|null */
        public ?bool $hasResponse=null,

        // @DataMember(Order=14)
        /** @var bool|null */
        public ?bool $withErrors=null,

        // @DataMember(Order=15)
        /** @var bool|null */
        public ?bool $enableSessionTracking=null,

        // @DataMember(Order=16)
        /** @var bool|null */
        public ?bool $enableResponseTracking=null,

        // @DataMember(Order=17)
        /** @var bool|null */
        public ?bool $enableErrorTracking=null,

        // @DataMember(Order=18)
        /** @var DateInterval|null */
        public ?DateInterval $durationLongerThan=null,

        // @DataMember(Order=19)
        /** @var DateInterval|null */
        public ?DateInterval $durationLessThan=null,

        // @DataMember(Order=20)
        /** @var int */
        public int $skip=0,

        // @DataMember(Order=21)
        /** @var int|null */
        public ?int $take=null,

        // @DataMember(Order=22)
        /** @var string|null */
        public ?string $orderBy=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['beforeSecs'])) $this->beforeSecs = $o['beforeSecs'];
        if (isset($o['afterSecs'])) $this->afterSecs = $o['afterSecs'];
        if (isset($o['operationName'])) $this->operationName = $o['operationName'];
        if (isset($o['ipAddress'])) $this->ipAddress = $o['ipAddress'];
        if (isset($o['forwardedFor'])) $this->forwardedFor = $o['forwardedFor'];
        if (isset($o['userAuthId'])) $this->userAuthId = $o['userAuthId'];
        if (isset($o['sessionId'])) $this->sessionId = $o['sessionId'];
        if (isset($o['referer'])) $this->referer = $o['referer'];
        if (isset($o['pathInfo'])) $this->pathInfo = $o['pathInfo'];
        if (isset($o['ids'])) $this->ids = JsonConverters::fromArray('int', $o['ids']);
        if (isset($o['beforeId'])) $this->beforeId = $o['beforeId'];
        if (isset($o['afterId'])) $this->afterId = $o['afterId'];
        if (isset($o['hasResponse'])) $this->hasResponse = $o['hasResponse'];
        if (isset($o['withErrors'])) $this->withErrors = $o['withErrors'];
        if (isset($o['enableSessionTracking'])) $this->enableSessionTracking = $o['enableSessionTracking'];
        if (isset($o['enableResponseTracking'])) $this->enableResponseTracking = $o['enableResponseTracking'];
        if (isset($o['enableErrorTracking'])) $this->enableErrorTracking = $o['enableErrorTracking'];
        if (isset($o['durationLongerThan'])) $this->durationLongerThan = JsonConverters::from('TimeSpan', $o['durationLongerThan']);
        if (isset($o['durationLessThan'])) $this->durationLessThan = JsonConverters::from('TimeSpan', $o['durationLessThan']);
        if (isset($o['skip'])) $this->skip = $o['skip'];
        if (isset($o['take'])) $this->take = $o['take'];
        if (isset($o['orderBy'])) $this->orderBy = $o['orderBy'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->beforeSecs)) $o['beforeSecs'] = $this->beforeSecs;
        if (isset($this->afterSecs)) $o['afterSecs'] = $this->afterSecs;
        if (isset($this->operationName)) $o['operationName'] = $this->operationName;
        if (isset($this->ipAddress)) $o['ipAddress'] = $this->ipAddress;
        if (isset($this->forwardedFor)) $o['forwardedFor'] = $this->forwardedFor;
        if (isset($this->userAuthId)) $o['userAuthId'] = $this->userAuthId;
        if (isset($this->sessionId)) $o['sessionId'] = $this->sessionId;
        if (isset($this->referer)) $o['referer'] = $this->referer;
        if (isset($this->pathInfo)) $o['pathInfo'] = $this->pathInfo;
        if (isset($this->ids)) $o['ids'] = JsonConverters::toArray('int', $this->ids);
        if (isset($this->beforeId)) $o['beforeId'] = $this->beforeId;
        if (isset($this->afterId)) $o['afterId'] = $this->afterId;
        if (isset($this->hasResponse)) $o['hasResponse'] = $this->hasResponse;
        if (isset($this->withErrors)) $o['withErrors'] = $this->withErrors;
        if (isset($this->enableSessionTracking)) $o['enableSessionTracking'] = $this->enableSessionTracking;
        if (isset($this->enableResponseTracking)) $o['enableResponseTracking'] = $this->enableResponseTracking;
        if (isset($this->enableErrorTracking)) $o['enableErrorTracking'] = $this->enableErrorTracking;
        if (isset($this->durationLongerThan)) $o['durationLongerThan'] = JsonConverters::to('TimeSpan', $this->durationLongerThan);
        if (isset($this->durationLessThan)) $o['durationLessThan'] = JsonConverters::to('TimeSpan', $this->durationLessThan);
        if (isset($this->skip)) $o['skip'] = $this->skip;
        if (isset($this->take)) $o['take'] = $this->take;
        if (isset($this->orderBy)) $o['orderBy'] = $this->orderBy;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'RequestLogs'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new RequestLogsResponse(); }
}

// @Route("/validation/rules/{Type}")
// @DataContract
#[Returns('GetValidationRulesResponse')]
class GetValidationRules implements IReturn, IGet, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $authSecret=null,

        // @DataMember(Order=2)
        /** @var string|null */
        public ?string $type=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['authSecret'])) $this->authSecret = $o['authSecret'];
        if (isset($o['type'])) $this->type = $o['type'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->authSecret)) $o['authSecret'] = $this->authSecret;
        if (isset($this->type)) $o['type'] = $this->type;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'GetValidationRules'; }
    public function getMethod(): string { return 'GET'; }
    public function createResponse(): mixed { return new GetValidationRulesResponse(); }
}

// @Route("/validation/rules")
// @DataContract
class ModifyValidationRules implements IReturnVoid, JsonSerializable
{
    public function __construct(
        // @DataMember(Order=1)
        /** @var string|null */
        public ?string $authSecret=null,

        // @DataMember(Order=2)
        /** @var array<ValidationRule>|null */
        public ?array $saveRules=null,

        // @DataMember(Order=3)
        /** @var int[]|null */
        public ?array $deleteRuleIds=null,

        // @DataMember(Order=4)
        /** @var int[]|null */
        public ?array $suspendRuleIds=null,

        // @DataMember(Order=5)
        /** @var int[]|null */
        public ?array $unsuspendRuleIds=null,

        // @DataMember(Order=6)
        /** @var bool|null */
        public ?bool $clearCache=null
    ) {
    }

    /** @throws Exception */
    public function fromMap($o): void {
        if (isset($o['authSecret'])) $this->authSecret = $o['authSecret'];
        if (isset($o['saveRules'])) $this->saveRules = JsonConverters::fromArray('ValidationRule', $o['saveRules']);
        if (isset($o['deleteRuleIds'])) $this->deleteRuleIds = JsonConverters::fromArray('int', $o['deleteRuleIds']);
        if (isset($o['suspendRuleIds'])) $this->suspendRuleIds = JsonConverters::fromArray('int', $o['suspendRuleIds']);
        if (isset($o['unsuspendRuleIds'])) $this->unsuspendRuleIds = JsonConverters::fromArray('int', $o['unsuspendRuleIds']);
        if (isset($o['clearCache'])) $this->clearCache = $o['clearCache'];
    }
    
    /** @throws Exception */
    public function jsonSerialize(): mixed
    {
        $o = [];
        if (isset($this->authSecret)) $o['authSecret'] = $this->authSecret;
        if (isset($this->saveRules)) $o['saveRules'] = JsonConverters::toArray('ValidationRule', $this->saveRules);
        if (isset($this->deleteRuleIds)) $o['deleteRuleIds'] = JsonConverters::toArray('int', $this->deleteRuleIds);
        if (isset($this->suspendRuleIds)) $o['suspendRuleIds'] = JsonConverters::toArray('int', $this->suspendRuleIds);
        if (isset($this->unsuspendRuleIds)) $o['unsuspendRuleIds'] = JsonConverters::toArray('int', $this->unsuspendRuleIds);
        if (isset($this->clearCache)) $o['clearCache'] = $this->clearCache;
        return empty($o) ? new class(){} : $o;
    }
    public function getTypeName(): string { return 'ModifyValidationRules'; }
    public function getMethod(): string { return 'POST'; }
    public function createResponse(): void {}
}

