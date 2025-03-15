/* Options:
Date: 2025-03-14 11:35:19
Version: 8.61
Tip: To override a DTO option, remove "//" prefix before updating
BaseUrl: http://localhost:20000

//GlobalNamespace: 
//AddServiceStackTypes: True
//AddResponseStatus: False
//AddImplicitVersion: 
//AddDescriptionAsComments: True
//IncludeTypes: 
//ExcludeTypes: 
//DefaultImports: package:servicestack/servicestack.dart
*/

import 'package:servicestack/servicestack.dart';

// @DataContract
class Property implements IConvertible
{
    // @DataMember(Order=1)
    String? name;

    // @DataMember(Order=2)
    String? value;

    Property({this.name,this.value});
    Property.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        value = json['value'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'value': value
    };

    getTypeName() => "Property";
    TypeContext? context = _ctx;
}

// @DataContract
abstract class AdminUserBase
{
    // @DataMember(Order=1)
    String? userName;

    // @DataMember(Order=2)
    String? firstName;

    // @DataMember(Order=3)
    String? lastName;

    // @DataMember(Order=4)
    String? displayName;

    // @DataMember(Order=5)
    String? email;

    // @DataMember(Order=6)
    String? password;

    // @DataMember(Order=7)
    String? profileUrl;

    // @DataMember(Order=8)
    String? phoneNumber;

    // @DataMember(Order=9)
    Map<String,String?>? userAuthProperties;

    // @DataMember(Order=10)
    Map<String,String?>? meta;

    AdminUserBase({this.userName,this.firstName,this.lastName,this.displayName,this.email,this.password,this.profileUrl,this.phoneNumber,this.userAuthProperties,this.meta});
    AdminUserBase.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        userName = json['userName'];
        firstName = json['firstName'];
        lastName = json['lastName'];
        displayName = json['displayName'];
        email = json['email'];
        password = json['password'];
        profileUrl = json['profileUrl'];
        phoneNumber = json['phoneNumber'];
        userAuthProperties = JsonConverters.toStringMap(json['userAuthProperties']);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'userName': userName,
        'firstName': firstName,
        'lastName': lastName,
        'displayName': displayName,
        'email': email,
        'password': password,
        'profileUrl': profileUrl,
        'phoneNumber': phoneNumber,
        'userAuthProperties': userAuthProperties,
        'meta': meta
    };

    getTypeName() => "AdminUserBase";
    TypeContext? context = _ctx;
}

class RequestLog implements IConvertible
{
    int? id;
    String? traceId;
    String? operationName;
    DateTime? dateTime;
    int? statusCode;
    String? statusDescription;
    String? httpMethod;
    String? absoluteUri;
    String? pathInfo;
    String? request;
    // @StringLength(2147483647)
    String? requestBody;

    String? userAuthId;
    String? sessionId;
    String? ipAddress;
    String? forwardedFor;
    String? referer;
    Map<String,String?>? headers = {};
    Map<String,String?>? formData;
    Map<String,String?>? items = {};
    Map<String,String?>? responseHeaders;
    String? response;
    String? responseBody;
    String? sessionBody;
    ResponseStatus? error;
    String? exceptionSource;
    String? exceptionDataBody;
    Duration? requestDuration;
    Map<String,String?>? meta;

    RequestLog({this.id,this.traceId,this.operationName,this.dateTime,this.statusCode,this.statusDescription,this.httpMethod,this.absoluteUri,this.pathInfo,this.request,this.requestBody,this.userAuthId,this.sessionId,this.ipAddress,this.forwardedFor,this.referer,this.headers,this.formData,this.items,this.responseHeaders,this.response,this.responseBody,this.sessionBody,this.error,this.exceptionSource,this.exceptionDataBody,this.requestDuration,this.meta});
    RequestLog.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        traceId = json['traceId'];
        operationName = json['operationName'];
        dateTime = JsonConverters.fromJson(json['dateTime'],'DateTime',context!);
        statusCode = json['statusCode'];
        statusDescription = json['statusDescription'];
        httpMethod = json['httpMethod'];
        absoluteUri = json['absoluteUri'];
        pathInfo = json['pathInfo'];
        request = json['request'];
        requestBody = json['requestBody'];
        userAuthId = json['userAuthId'];
        sessionId = json['sessionId'];
        ipAddress = json['ipAddress'];
        forwardedFor = json['forwardedFor'];
        referer = json['referer'];
        headers = JsonConverters.toStringMap(json['headers']);
        formData = JsonConverters.toStringMap(json['formData']);
        items = JsonConverters.toStringMap(json['items']);
        responseHeaders = JsonConverters.toStringMap(json['responseHeaders']);
        response = json['response'];
        responseBody = json['responseBody'];
        sessionBody = json['sessionBody'];
        error = JsonConverters.fromJson(json['error'],'ResponseStatus',context!);
        exceptionSource = json['exceptionSource'];
        exceptionDataBody = json['exceptionDataBody'];
        requestDuration = JsonConverters.fromJson(json['requestDuration'],'Duration',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'traceId': traceId,
        'operationName': operationName,
        'dateTime': JsonConverters.toJson(dateTime,'DateTime',context!),
        'statusCode': statusCode,
        'statusDescription': statusDescription,
        'httpMethod': httpMethod,
        'absoluteUri': absoluteUri,
        'pathInfo': pathInfo,
        'request': request,
        'requestBody': requestBody,
        'userAuthId': userAuthId,
        'sessionId': sessionId,
        'ipAddress': ipAddress,
        'forwardedFor': forwardedFor,
        'referer': referer,
        'headers': headers,
        'formData': formData,
        'items': items,
        'responseHeaders': responseHeaders,
        'response': response,
        'responseBody': responseBody,
        'sessionBody': sessionBody,
        'error': JsonConverters.toJson(error,'ResponseStatus',context!),
        'exceptionSource': exceptionSource,
        'exceptionDataBody': exceptionDataBody,
        'requestDuration': JsonConverters.toJson(requestDuration,'Duration',context!),
        'meta': meta
    };

    getTypeName() => "RequestLog";
    TypeContext? context = _ctx;
}

class RedisEndpointInfo implements IConvertible
{
    String? host;
    int? port;
    bool? ssl;
    int? db;
    String? username;
    String? password;

    RedisEndpointInfo({this.host,this.port,this.ssl,this.db,this.username,this.password});
    RedisEndpointInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        host = json['host'];
        port = json['port'];
        ssl = json['ssl'];
        db = json['db'];
        username = json['username'];
        password = json['password'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'host': host,
        'port': port,
        'ssl': ssl,
        'db': db,
        'username': username,
        'password': password
    };

    getTypeName() => "RedisEndpointInfo";
    TypeContext? context = _ctx;
}

enum BackgroundJobState
{
    Queued,
    Started,
    Executed,
    Completed,
    Failed,
    Cancelled,
}

abstract class BackgroundJobBase
{
    int? id;
    int? parentId;
    String? refId;
    String? worker;
    String? tag;
    String? batchId;
    String? callback;
    int? dependsOn;
    DateTime? runAfter;
    DateTime? createdDate;
    String? createdBy;
    String? requestId;
    String? requestType;
    String? command;
    String? request;
    String? requestBody;
    String? userId;
    String? response;
    String? responseBody;
    BackgroundJobState? state;
    DateTime? startedDate;
    DateTime? completedDate;
    DateTime? notifiedDate;
    int? retryLimit;
    int? attempts;
    int? durationMs;
    int? timeoutSecs;
    double? progress;
    String? status;
    String? logs;
    DateTime? lastActivityDate;
    String? replyTo;
    String? errorCode;
    ResponseStatus? error;
    Map<String,String?>? args;
    Map<String,String?>? meta;

    BackgroundJobBase({this.id,this.parentId,this.refId,this.worker,this.tag,this.batchId,this.callback,this.dependsOn,this.runAfter,this.createdDate,this.createdBy,this.requestId,this.requestType,this.command,this.request,this.requestBody,this.userId,this.response,this.responseBody,this.state,this.startedDate,this.completedDate,this.notifiedDate,this.retryLimit,this.attempts,this.durationMs,this.timeoutSecs,this.progress,this.status,this.logs,this.lastActivityDate,this.replyTo,this.errorCode,this.error,this.args,this.meta});
    BackgroundJobBase.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        parentId = json['parentId'];
        refId = json['refId'];
        worker = json['worker'];
        tag = json['tag'];
        batchId = json['batchId'];
        callback = json['callback'];
        dependsOn = json['dependsOn'];
        runAfter = JsonConverters.fromJson(json['runAfter'],'DateTime',context!);
        createdDate = JsonConverters.fromJson(json['createdDate'],'DateTime',context!);
        createdBy = json['createdBy'];
        requestId = json['requestId'];
        requestType = json['requestType'];
        command = json['command'];
        request = json['request'];
        requestBody = json['requestBody'];
        userId = json['userId'];
        response = json['response'];
        responseBody = json['responseBody'];
        state = JsonConverters.fromJson(json['state'],'BackgroundJobState',context!);
        startedDate = JsonConverters.fromJson(json['startedDate'],'DateTime',context!);
        completedDate = JsonConverters.fromJson(json['completedDate'],'DateTime',context!);
        notifiedDate = JsonConverters.fromJson(json['notifiedDate'],'DateTime',context!);
        retryLimit = json['retryLimit'];
        attempts = json['attempts'];
        durationMs = json['durationMs'];
        timeoutSecs = json['timeoutSecs'];
        progress = JsonConverters.toDouble(json['progress']);
        status = json['status'];
        logs = json['logs'];
        lastActivityDate = JsonConverters.fromJson(json['lastActivityDate'],'DateTime',context!);
        replyTo = json['replyTo'];
        errorCode = json['errorCode'];
        error = JsonConverters.fromJson(json['error'],'ResponseStatus',context!);
        args = JsonConverters.toStringMap(json['args']);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'parentId': parentId,
        'refId': refId,
        'worker': worker,
        'tag': tag,
        'batchId': batchId,
        'callback': callback,
        'dependsOn': dependsOn,
        'runAfter': JsonConverters.toJson(runAfter,'DateTime',context!),
        'createdDate': JsonConverters.toJson(createdDate,'DateTime',context!),
        'createdBy': createdBy,
        'requestId': requestId,
        'requestType': requestType,
        'command': command,
        'request': request,
        'requestBody': requestBody,
        'userId': userId,
        'response': response,
        'responseBody': responseBody,
        'state': JsonConverters.toJson(state,'BackgroundJobState',context!),
        'startedDate': JsonConverters.toJson(startedDate,'DateTime',context!),
        'completedDate': JsonConverters.toJson(completedDate,'DateTime',context!),
        'notifiedDate': JsonConverters.toJson(notifiedDate,'DateTime',context!),
        'retryLimit': retryLimit,
        'attempts': attempts,
        'durationMs': durationMs,
        'timeoutSecs': timeoutSecs,
        'progress': progress,
        'status': status,
        'logs': logs,
        'lastActivityDate': JsonConverters.toJson(lastActivityDate,'DateTime',context!),
        'replyTo': replyTo,
        'errorCode': errorCode,
        'error': JsonConverters.toJson(error,'ResponseStatus',context!),
        'args': args,
        'meta': meta
    };

    getTypeName() => "BackgroundJobBase";
    TypeContext? context = _ctx;
}

class BackgroundJob extends BackgroundJobBase implements IConvertible
{
    int? id;

    BackgroundJob({this.id});
    BackgroundJob.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        id = json['id'];
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'id': id
    });

    getTypeName() => "BackgroundJob";
    TypeContext? context = _ctx;
}

class JobSummary implements IConvertible
{
    int? id;
    int? parentId;
    String? refId;
    String? worker;
    String? tag;
    String? batchId;
    DateTime? createdDate;
    String? createdBy;
    String? requestType;
    String? command;
    String? request;
    String? response;
    String? userId;
    String? callback;
    DateTime? startedDate;
    DateTime? completedDate;
    BackgroundJobState? state;
    int? durationMs;
    int? attempts;
    String? errorCode;
    String? errorMessage;

    JobSummary({this.id,this.parentId,this.refId,this.worker,this.tag,this.batchId,this.createdDate,this.createdBy,this.requestType,this.command,this.request,this.response,this.userId,this.callback,this.startedDate,this.completedDate,this.state,this.durationMs,this.attempts,this.errorCode,this.errorMessage});
    JobSummary.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        parentId = json['parentId'];
        refId = json['refId'];
        worker = json['worker'];
        tag = json['tag'];
        batchId = json['batchId'];
        createdDate = JsonConverters.fromJson(json['createdDate'],'DateTime',context!);
        createdBy = json['createdBy'];
        requestType = json['requestType'];
        command = json['command'];
        request = json['request'];
        response = json['response'];
        userId = json['userId'];
        callback = json['callback'];
        startedDate = JsonConverters.fromJson(json['startedDate'],'DateTime',context!);
        completedDate = JsonConverters.fromJson(json['completedDate'],'DateTime',context!);
        state = JsonConverters.fromJson(json['state'],'BackgroundJobState',context!);
        durationMs = json['durationMs'];
        attempts = json['attempts'];
        errorCode = json['errorCode'];
        errorMessage = json['errorMessage'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'parentId': parentId,
        'refId': refId,
        'worker': worker,
        'tag': tag,
        'batchId': batchId,
        'createdDate': JsonConverters.toJson(createdDate,'DateTime',context!),
        'createdBy': createdBy,
        'requestType': requestType,
        'command': command,
        'request': request,
        'response': response,
        'userId': userId,
        'callback': callback,
        'startedDate': JsonConverters.toJson(startedDate,'DateTime',context!),
        'completedDate': JsonConverters.toJson(completedDate,'DateTime',context!),
        'state': JsonConverters.toJson(state,'BackgroundJobState',context!),
        'durationMs': durationMs,
        'attempts': attempts,
        'errorCode': errorCode,
        'errorMessage': errorMessage
    };

    getTypeName() => "JobSummary";
    TypeContext? context = _ctx;
}

class BackgroundJobOptions implements IConvertible
{
    String? refId;
    int? parentId;
    String? worker;
    DateTime? runAfter;
    String? callback;
    int? dependsOn;
    String? userId;
    int? retryLimit;
    String? replyTo;
    String? tag;
    String? batchId;
    String? createdBy;
    int? timeoutSecs;
    Duration? timeout;
    Map<String,String?>? args;
    bool? runCommand;

    BackgroundJobOptions({this.refId,this.parentId,this.worker,this.runAfter,this.callback,this.dependsOn,this.userId,this.retryLimit,this.replyTo,this.tag,this.batchId,this.createdBy,this.timeoutSecs,this.timeout,this.args,this.runCommand});
    BackgroundJobOptions.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        refId = json['refId'];
        parentId = json['parentId'];
        worker = json['worker'];
        runAfter = JsonConverters.fromJson(json['runAfter'],'DateTime',context!);
        callback = json['callback'];
        dependsOn = json['dependsOn'];
        userId = json['userId'];
        retryLimit = json['retryLimit'];
        replyTo = json['replyTo'];
        tag = json['tag'];
        batchId = json['batchId'];
        createdBy = json['createdBy'];
        timeoutSecs = json['timeoutSecs'];
        timeout = JsonConverters.fromJson(json['timeout'],'Duration',context!);
        args = JsonConverters.toStringMap(json['args']);
        runCommand = json['runCommand'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'refId': refId,
        'parentId': parentId,
        'worker': worker,
        'runAfter': JsonConverters.toJson(runAfter,'DateTime',context!),
        'callback': callback,
        'dependsOn': dependsOn,
        'userId': userId,
        'retryLimit': retryLimit,
        'replyTo': replyTo,
        'tag': tag,
        'batchId': batchId,
        'createdBy': createdBy,
        'timeoutSecs': timeoutSecs,
        'timeout': JsonConverters.toJson(timeout,'Duration',context!),
        'args': args,
        'runCommand': runCommand
    };

    getTypeName() => "BackgroundJobOptions";
    TypeContext? context = _ctx;
}

class ScheduledTask implements IConvertible
{
    int? id;
    String? name;
    Duration? interval;
    String? cronExpression;
    String? requestType;
    String? command;
    String? request;
    String? requestBody;
    BackgroundJobOptions? options;
    DateTime? lastRun;
    int? lastJobId;

    ScheduledTask({this.id,this.name,this.interval,this.cronExpression,this.requestType,this.command,this.request,this.requestBody,this.options,this.lastRun,this.lastJobId});
    ScheduledTask.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        name = json['name'];
        interval = JsonConverters.fromJson(json['interval'],'Duration',context!);
        cronExpression = json['cronExpression'];
        requestType = json['requestType'];
        command = json['command'];
        request = json['request'];
        requestBody = json['requestBody'];
        options = JsonConverters.fromJson(json['options'],'BackgroundJobOptions',context!);
        lastRun = JsonConverters.fromJson(json['lastRun'],'DateTime',context!);
        lastJobId = json['lastJobId'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'interval': JsonConverters.toJson(interval,'Duration',context!),
        'cronExpression': cronExpression,
        'requestType': requestType,
        'command': command,
        'request': request,
        'requestBody': requestBody,
        'options': JsonConverters.toJson(options,'BackgroundJobOptions',context!),
        'lastRun': JsonConverters.toJson(lastRun,'DateTime',context!),
        'lastJobId': lastJobId
    };

    getTypeName() => "ScheduledTask";
    TypeContext? context = _ctx;
}

class CompletedJob extends BackgroundJobBase implements IConvertible
{
    CompletedJob();
    CompletedJob.fromJson(Map<String, dynamic> json) : super.fromJson(json);
    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson();
    getTypeName() => "CompletedJob";
    TypeContext? context = _ctx;
}

class FailedJob extends BackgroundJobBase implements IConvertible
{
    FailedJob();
    FailedJob.fromJson(Map<String, dynamic> json) : super.fromJson(json);
    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson();
    getTypeName() => "FailedJob";
    TypeContext? context = _ctx;
}

class ValidateRule implements IConvertible
{
    String? validator;
    String? condition;
    String? errorCode;
    String? message;

    ValidateRule({this.validator,this.condition,this.errorCode,this.message});
    ValidateRule.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        validator = json['validator'];
        condition = json['condition'];
        errorCode = json['errorCode'];
        message = json['message'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'validator': validator,
        'condition': condition,
        'errorCode': errorCode,
        'message': message
    };

    getTypeName() => "ValidateRule";
    TypeContext? context = _ctx;
}

class ValidationRule extends ValidateRule implements IConvertible
{
    int? id;
    // @required()
    String? type;

    String? field;
    String? createdBy;
    DateTime? createdDate;
    String? modifiedBy;
    DateTime? modifiedDate;
    String? suspendedBy;
    DateTime? suspendedDate;
    String? notes;

    ValidationRule({this.id,this.type,this.field,this.createdBy,this.createdDate,this.modifiedBy,this.modifiedDate,this.suspendedBy,this.suspendedDate,this.notes});
    ValidationRule.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        id = json['id'];
        type = json['type'];
        field = json['field'];
        createdBy = json['createdBy'];
        createdDate = JsonConverters.fromJson(json['createdDate'],'DateTime',context!);
        modifiedBy = json['modifiedBy'];
        modifiedDate = JsonConverters.fromJson(json['modifiedDate'],'DateTime',context!);
        suspendedBy = json['suspendedBy'];
        suspendedDate = JsonConverters.fromJson(json['suspendedDate'],'DateTime',context!);
        notes = json['notes'];
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'id': id,
        'type': type,
        'field': field,
        'createdBy': createdBy,
        'createdDate': JsonConverters.toJson(createdDate,'DateTime',context!),
        'modifiedBy': modifiedBy,
        'modifiedDate': JsonConverters.toJson(modifiedDate,'DateTime',context!),
        'suspendedBy': suspendedBy,
        'suspendedDate': JsonConverters.toJson(suspendedDate,'DateTime',context!),
        'notes': notes
    });

    getTypeName() => "ValidationRule";
    TypeContext? context = _ctx;
}

class AppInfo implements IConvertible
{
    String? baseUrl;
    String? serviceStackVersion;
    String? serviceName;
    String? apiVersion;
    String? serviceDescription;
    String? serviceIconUrl;
    String? brandUrl;
    String? brandImageUrl;
    String? textColor;
    String? linkColor;
    String? backgroundColor;
    String? backgroundImageUrl;
    String? iconUrl;
    String? jsTextCase;
    String? useSystemJson;
    List<String>? endpointRouting;
    Map<String,String?>? meta;

    AppInfo({this.baseUrl,this.serviceStackVersion,this.serviceName,this.apiVersion,this.serviceDescription,this.serviceIconUrl,this.brandUrl,this.brandImageUrl,this.textColor,this.linkColor,this.backgroundColor,this.backgroundImageUrl,this.iconUrl,this.jsTextCase,this.useSystemJson,this.endpointRouting,this.meta});
    AppInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        baseUrl = json['baseUrl'];
        serviceStackVersion = json['serviceStackVersion'];
        serviceName = json['serviceName'];
        apiVersion = json['apiVersion'];
        serviceDescription = json['serviceDescription'];
        serviceIconUrl = json['serviceIconUrl'];
        brandUrl = json['brandUrl'];
        brandImageUrl = json['brandImageUrl'];
        textColor = json['textColor'];
        linkColor = json['linkColor'];
        backgroundColor = json['backgroundColor'];
        backgroundImageUrl = json['backgroundImageUrl'];
        iconUrl = json['iconUrl'];
        jsTextCase = json['jsTextCase'];
        useSystemJson = json['useSystemJson'];
        endpointRouting = JsonConverters.fromJson(json['endpointRouting'],'List<String>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'baseUrl': baseUrl,
        'serviceStackVersion': serviceStackVersion,
        'serviceName': serviceName,
        'apiVersion': apiVersion,
        'serviceDescription': serviceDescription,
        'serviceIconUrl': serviceIconUrl,
        'brandUrl': brandUrl,
        'brandImageUrl': brandImageUrl,
        'textColor': textColor,
        'linkColor': linkColor,
        'backgroundColor': backgroundColor,
        'backgroundImageUrl': backgroundImageUrl,
        'iconUrl': iconUrl,
        'jsTextCase': jsTextCase,
        'useSystemJson': useSystemJson,
        'endpointRouting': JsonConverters.toJson(endpointRouting,'List<String>',context!),
        'meta': meta
    };

    getTypeName() => "AppInfo";
    TypeContext? context = _ctx;
}

class ImageInfo implements IConvertible
{
    String? svg;
    String? uri;
    String? alt;
    String? cls;

    ImageInfo({this.svg,this.uri,this.alt,this.cls});
    ImageInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        svg = json['svg'];
        uri = json['uri'];
        alt = json['alt'];
        cls = json['cls'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'svg': svg,
        'uri': uri,
        'alt': alt,
        'cls': cls
    };

    getTypeName() => "ImageInfo";
    TypeContext? context = _ctx;
}

class LinkInfo implements IConvertible
{
    String? id;
    String? href;
    String? label;
    ImageInfo? icon;
    String? Show;
    String? Hide;

    LinkInfo({this.id,this.href,this.label,this.icon,this.Show,this.Hide});
    LinkInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        href = json['href'];
        label = json['label'];
        icon = JsonConverters.fromJson(json['icon'],'ImageInfo',context!);
        Show = json['show'];
        Hide = json['hide'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'href': href,
        'label': label,
        'icon': JsonConverters.toJson(icon,'ImageInfo',context!),
        'show': Show,
        'hide': Hide
    };

    getTypeName() => "LinkInfo";
    TypeContext? context = _ctx;
}

class ThemeInfo implements IConvertible
{
    String? form;
    ImageInfo? modelIcon;

    ThemeInfo({this.form,this.modelIcon});
    ThemeInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        form = json['form'];
        modelIcon = JsonConverters.fromJson(json['modelIcon'],'ImageInfo',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'form': form,
        'modelIcon': JsonConverters.toJson(modelIcon,'ImageInfo',context!)
    };

    getTypeName() => "ThemeInfo";
    TypeContext? context = _ctx;
}

class ApiCss implements IConvertible
{
    String? form;
    String? fieldset;
    String? field;

    ApiCss({this.form,this.fieldset,this.field});
    ApiCss.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        form = json['form'];
        fieldset = json['fieldset'];
        field = json['field'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'form': form,
        'fieldset': fieldset,
        'field': field
    };

    getTypeName() => "ApiCss";
    TypeContext? context = _ctx;
}

class AppTags implements IConvertible
{
    String? Default;
    String? other;

    AppTags({this.Default,this.other});
    AppTags.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        Default = json['default'];
        other = json['other'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'default': Default,
        'other': other
    };

    getTypeName() => "AppTags";
    TypeContext? context = _ctx;
}

class LocodeUi implements IConvertible
{
    ApiCss? css;
    AppTags? tags;
    int? maxFieldLength;
    int? maxNestedFields;
    int? maxNestedFieldLength;

    LocodeUi({this.css,this.tags,this.maxFieldLength,this.maxNestedFields,this.maxNestedFieldLength});
    LocodeUi.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        css = JsonConverters.fromJson(json['css'],'ApiCss',context!);
        tags = JsonConverters.fromJson(json['tags'],'AppTags',context!);
        maxFieldLength = json['maxFieldLength'];
        maxNestedFields = json['maxNestedFields'];
        maxNestedFieldLength = json['maxNestedFieldLength'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'css': JsonConverters.toJson(css,'ApiCss',context!),
        'tags': JsonConverters.toJson(tags,'AppTags',context!),
        'maxFieldLength': maxFieldLength,
        'maxNestedFields': maxNestedFields,
        'maxNestedFieldLength': maxNestedFieldLength
    };

    getTypeName() => "LocodeUi";
    TypeContext? context = _ctx;
}

class ExplorerUi implements IConvertible
{
    ApiCss? css;
    AppTags? tags;

    ExplorerUi({this.css,this.tags});
    ExplorerUi.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        css = JsonConverters.fromJson(json['css'],'ApiCss',context!);
        tags = JsonConverters.fromJson(json['tags'],'AppTags',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'css': JsonConverters.toJson(css,'ApiCss',context!),
        'tags': JsonConverters.toJson(tags,'AppTags',context!)
    };

    getTypeName() => "ExplorerUi";
    TypeContext? context = _ctx;
}

class AdminUi implements IConvertible
{
    ApiCss? css;

    AdminUi({this.css});
    AdminUi.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        css = JsonConverters.fromJson(json['css'],'ApiCss',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'css': JsonConverters.toJson(css,'ApiCss',context!)
    };

    getTypeName() => "AdminUi";
    TypeContext? context = _ctx;
}

class FormatInfo implements IConvertible
{
    String? method;
    String? options;
    String? locale;

    FormatInfo({this.method,this.options,this.locale});
    FormatInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        method = json['method'];
        options = json['options'];
        locale = json['locale'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'method': method,
        'options': options,
        'locale': locale
    };

    getTypeName() => "FormatInfo";
    TypeContext? context = _ctx;
}

class ApiFormat implements IConvertible
{
    String? locale;
    bool? assumeUtc;
    FormatInfo? number;
    FormatInfo? date;

    ApiFormat({this.locale,this.assumeUtc,this.number,this.date});
    ApiFormat.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        locale = json['locale'];
        assumeUtc = json['assumeUtc'];
        number = JsonConverters.fromJson(json['number'],'FormatInfo',context!);
        date = JsonConverters.fromJson(json['date'],'FormatInfo',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'locale': locale,
        'assumeUtc': assumeUtc,
        'number': JsonConverters.toJson(number,'FormatInfo',context!),
        'date': JsonConverters.toJson(date,'FormatInfo',context!)
    };

    getTypeName() => "ApiFormat";
    TypeContext? context = _ctx;
}

class UiInfo implements IConvertible
{
    ImageInfo? brandIcon;
    List<String>? hideTags;
    List<String>? modules;
    List<String>? alwaysHideTags;
    List<LinkInfo>? adminLinks;
    ThemeInfo? theme;
    LocodeUi? locode;
    ExplorerUi? explorer;
    AdminUi? admin;
    ApiFormat? defaultFormats;
    Map<String,String?>? meta;

    UiInfo({this.brandIcon,this.hideTags,this.modules,this.alwaysHideTags,this.adminLinks,this.theme,this.locode,this.explorer,this.admin,this.defaultFormats,this.meta});
    UiInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        brandIcon = JsonConverters.fromJson(json['brandIcon'],'ImageInfo',context!);
        hideTags = JsonConverters.fromJson(json['hideTags'],'List<String>',context!);
        modules = JsonConverters.fromJson(json['modules'],'List<String>',context!);
        alwaysHideTags = JsonConverters.fromJson(json['alwaysHideTags'],'List<String>',context!);
        adminLinks = JsonConverters.fromJson(json['adminLinks'],'List<LinkInfo>',context!);
        theme = JsonConverters.fromJson(json['theme'],'ThemeInfo',context!);
        locode = JsonConverters.fromJson(json['locode'],'LocodeUi',context!);
        explorer = JsonConverters.fromJson(json['explorer'],'ExplorerUi',context!);
        admin = JsonConverters.fromJson(json['admin'],'AdminUi',context!);
        defaultFormats = JsonConverters.fromJson(json['defaultFormats'],'ApiFormat',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'brandIcon': JsonConverters.toJson(brandIcon,'ImageInfo',context!),
        'hideTags': JsonConverters.toJson(hideTags,'List<String>',context!),
        'modules': JsonConverters.toJson(modules,'List<String>',context!),
        'alwaysHideTags': JsonConverters.toJson(alwaysHideTags,'List<String>',context!),
        'adminLinks': JsonConverters.toJson(adminLinks,'List<LinkInfo>',context!),
        'theme': JsonConverters.toJson(theme,'ThemeInfo',context!),
        'locode': JsonConverters.toJson(locode,'LocodeUi',context!),
        'explorer': JsonConverters.toJson(explorer,'ExplorerUi',context!),
        'admin': JsonConverters.toJson(admin,'AdminUi',context!),
        'defaultFormats': JsonConverters.toJson(defaultFormats,'ApiFormat',context!),
        'meta': meta
    };

    getTypeName() => "UiInfo";
    TypeContext? context = _ctx;
}

class ConfigInfo implements IConvertible
{
    bool? debugMode;
    Map<String,String?>? meta;

    ConfigInfo({this.debugMode,this.meta});
    ConfigInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        debugMode = json['debugMode'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'debugMode': debugMode,
        'meta': meta
    };

    getTypeName() => "ConfigInfo";
    TypeContext? context = _ctx;
}

class FieldCss implements IConvertible
{
    String? field;
    String? input;
    String? label;

    FieldCss({this.field,this.input,this.label});
    FieldCss.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        field = json['field'];
        input = json['input'];
        label = json['label'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'field': field,
        'input': input,
        'label': label
    };

    getTypeName() => "FieldCss";
    TypeContext? context = _ctx;
}

class InputInfo implements IConvertible
{
    String? id;
    String? name;
    String? type;
    String? value;
    String? placeholder;
    String? help;
    String? label;
    String? title;
    String? size;
    String? pattern;
    bool? readOnly;
    bool? Required;
    bool? disabled;
    String? autocomplete;
    String? autofocus;
    String? min;
    String? max;
    String? step;
    int? minLength;
    int? maxLength;
    String? accept;
    String? capture;
    bool? multiple;
    List<String>? allowableValues;
    List<KeyValuePair><String,String>? allowableEntries;
    String? options;
    bool? ignore;
    FieldCss? css;
    Map<String,String?>? meta;

    InputInfo({this.id,this.name,this.type,this.value,this.placeholder,this.help,this.label,this.title,this.size,this.pattern,this.readOnly,this.Required,this.disabled,this.autocomplete,this.autofocus,this.min,this.max,this.step,this.minLength,this.maxLength,this.accept,this.capture,this.multiple,this.allowableValues,this.allowableEntries,this.options,this.ignore,this.css,this.meta});
    InputInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        name = json['name'];
        type = json['type'];
        value = json['value'];
        placeholder = json['placeholder'];
        help = json['help'];
        label = json['label'];
        title = json['title'];
        size = json['size'];
        pattern = json['pattern'];
        readOnly = json['readOnly'];
        Required = json['required'];
        disabled = json['disabled'];
        autocomplete = json['autocomplete'];
        autofocus = json['autofocus'];
        min = json['min'];
        max = json['max'];
        step = json['step'];
        minLength = json['minLength'];
        maxLength = json['maxLength'];
        accept = json['accept'];
        capture = json['capture'];
        multiple = json['multiple'];
        allowableValues = JsonConverters.fromJson(json['allowableValues'],'List<String>',context!);
        allowableEntries = JsonConverters.fromJson(json['allowableEntries'],'List<KeyValuePair><String,String>',context!);
        options = json['options'];
        ignore = json['ignore'];
        css = JsonConverters.fromJson(json['css'],'FieldCss',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'type': type,
        'value': value,
        'placeholder': placeholder,
        'help': help,
        'label': label,
        'title': title,
        'size': size,
        'pattern': pattern,
        'readOnly': readOnly,
        'required': Required,
        'disabled': disabled,
        'autocomplete': autocomplete,
        'autofocus': autofocus,
        'min': min,
        'max': max,
        'step': step,
        'minLength': minLength,
        'maxLength': maxLength,
        'accept': accept,
        'capture': capture,
        'multiple': multiple,
        'allowableValues': JsonConverters.toJson(allowableValues,'List<String>',context!),
        'allowableEntries': JsonConverters.toJson(allowableEntries,'List<KeyValuePair><String,String>',context!),
        'options': options,
        'ignore': ignore,
        'css': JsonConverters.toJson(css,'FieldCss',context!),
        'meta': meta
    };

    getTypeName() => "InputInfo";
    TypeContext? context = _ctx;
}

class MetaAuthProvider implements IConvertible
{
    String? name;
    String? label;
    String? type;
    NavItem? navItem;
    ImageInfo? icon;
    List<InputInfo>? formLayout;
    Map<String,String?>? meta;

    MetaAuthProvider({this.name,this.label,this.type,this.navItem,this.icon,this.formLayout,this.meta});
    MetaAuthProvider.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        label = json['label'];
        type = json['type'];
        navItem = JsonConverters.fromJson(json['navItem'],'NavItem',context!);
        icon = JsonConverters.fromJson(json['icon'],'ImageInfo',context!);
        formLayout = JsonConverters.fromJson(json['formLayout'],'List<InputInfo>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'label': label,
        'type': type,
        'navItem': JsonConverters.toJson(navItem,'NavItem',context!),
        'icon': JsonConverters.toJson(icon,'ImageInfo',context!),
        'formLayout': JsonConverters.toJson(formLayout,'List<InputInfo>',context!),
        'meta': meta
    };

    getTypeName() => "MetaAuthProvider";
    TypeContext? context = _ctx;
}

class IdentityAuthInfo implements IConvertible
{
    bool? hasRefreshToken;
    Map<String,String?>? meta;

    IdentityAuthInfo({this.hasRefreshToken,this.meta});
    IdentityAuthInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        hasRefreshToken = json['hasRefreshToken'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'hasRefreshToken': hasRefreshToken,
        'meta': meta
    };

    getTypeName() => "IdentityAuthInfo";
    TypeContext? context = _ctx;
}

class AuthInfo implements IConvertible
{
    bool? hasAuthSecret;
    bool? hasAuthRepository;
    bool? includesRoles;
    bool? includesOAuthTokens;
    String? htmlRedirect;
    List<MetaAuthProvider>? authProviders;
    IdentityAuthInfo? identityAuth;
    Map<String,List<LinkInfo>?>? roleLinks;
    Map<String,List<String>?>? serviceRoutes;
    Map<String,String?>? meta;

    AuthInfo({this.hasAuthSecret,this.hasAuthRepository,this.includesRoles,this.includesOAuthTokens,this.htmlRedirect,this.authProviders,this.identityAuth,this.roleLinks,this.serviceRoutes,this.meta});
    AuthInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        hasAuthSecret = json['hasAuthSecret'];
        hasAuthRepository = json['hasAuthRepository'];
        includesRoles = json['includesRoles'];
        includesOAuthTokens = json['includesOAuthTokens'];
        htmlRedirect = json['htmlRedirect'];
        authProviders = JsonConverters.fromJson(json['authProviders'],'List<MetaAuthProvider>',context!);
        identityAuth = JsonConverters.fromJson(json['identityAuth'],'IdentityAuthInfo',context!);
        roleLinks = JsonConverters.fromJson(json['roleLinks'],'Map<String,List<LinkInfo>?>',context!);
        serviceRoutes = JsonConverters.fromJson(json['serviceRoutes'],'Map<String,List<String>?>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'hasAuthSecret': hasAuthSecret,
        'hasAuthRepository': hasAuthRepository,
        'includesRoles': includesRoles,
        'includesOAuthTokens': includesOAuthTokens,
        'htmlRedirect': htmlRedirect,
        'authProviders': JsonConverters.toJson(authProviders,'List<MetaAuthProvider>',context!),
        'identityAuth': JsonConverters.toJson(identityAuth,'IdentityAuthInfo',context!),
        'roleLinks': JsonConverters.toJson(roleLinks,'Map<String,List<LinkInfo>?>',context!),
        'serviceRoutes': JsonConverters.toJson(serviceRoutes,'Map<String,List<String>?>',context!),
        'meta': meta
    };

    getTypeName() => "AuthInfo";
    TypeContext? context = _ctx;
}

class ApiKeyInfo implements IConvertible
{
    String? label;
    String? httpHeader;
    List<String>? scopes;
    List<String>? features;
    List<String>? requestTypes;
    List<KeyValuePair<String,String>>? expiresIn;
    List<String>? Hide;
    Map<String,String?>? meta;

    ApiKeyInfo({this.label,this.httpHeader,this.scopes,this.features,this.requestTypes,this.expiresIn,this.Hide,this.meta});
    ApiKeyInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        label = json['label'];
        httpHeader = json['httpHeader'];
        scopes = JsonConverters.fromJson(json['scopes'],'List<String>',context!);
        features = JsonConverters.fromJson(json['features'],'List<String>',context!);
        requestTypes = JsonConverters.fromJson(json['requestTypes'],'List<String>',context!);
        expiresIn = JsonConverters.fromJson(json['expiresIn'],'List<KeyValuePair<String,String>>',context!);
        Hide = JsonConverters.fromJson(json['hide'],'List<String>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'label': label,
        'httpHeader': httpHeader,
        'scopes': JsonConverters.toJson(scopes,'List<String>',context!),
        'features': JsonConverters.toJson(features,'List<String>',context!),
        'requestTypes': JsonConverters.toJson(requestTypes,'List<String>',context!),
        'expiresIn': JsonConverters.toJson(expiresIn,'List<KeyValuePair<String,String>>',context!),
        'hide': JsonConverters.toJson(Hide,'List<String>',context!),
        'meta': meta
    };

    getTypeName() => "ApiKeyInfo";
    TypeContext? context = _ctx;
}

class MetadataTypeName implements IConvertible
{
    String? name;
    String? namespace;
    List<String>? genericArgs;

    MetadataTypeName({this.name,this.namespace,this.genericArgs});
    MetadataTypeName.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        namespace = json['namespace'];
        genericArgs = JsonConverters.fromJson(json['genericArgs'],'List<String>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'namespace': namespace,
        'genericArgs': JsonConverters.toJson(genericArgs,'List<String>',context!)
    };

    getTypeName() => "MetadataTypeName";
    TypeContext? context = _ctx;
}

class MetadataDataContract implements IConvertible
{
    String? name;
    String? namespace;

    MetadataDataContract({this.name,this.namespace});
    MetadataDataContract.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        namespace = json['namespace'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'namespace': namespace
    };

    getTypeName() => "MetadataDataContract";
    TypeContext? context = _ctx;
}

class MetadataDataMember implements IConvertible
{
    String? name;
    int? order;
    bool? isRequired;
    bool? emitDefaultValue;

    MetadataDataMember({this.name,this.order,this.isRequired,this.emitDefaultValue});
    MetadataDataMember.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        order = json['order'];
        isRequired = json['isRequired'];
        emitDefaultValue = json['emitDefaultValue'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'order': order,
        'isRequired': isRequired,
        'emitDefaultValue': emitDefaultValue
    };

    getTypeName() => "MetadataDataMember";
    TypeContext? context = _ctx;
}

class MetadataAttribute implements IConvertible
{
    String? name;
    List<MetadataPropertyType>? constructorArgs;
    List<MetadataPropertyType>? args;

    MetadataAttribute({this.name,this.constructorArgs,this.args});
    MetadataAttribute.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        constructorArgs = JsonConverters.fromJson(json['constructorArgs'],'List<MetadataPropertyType>',context!);
        args = JsonConverters.fromJson(json['args'],'List<MetadataPropertyType>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'constructorArgs': JsonConverters.toJson(constructorArgs,'List<MetadataPropertyType>',context!),
        'args': JsonConverters.toJson(args,'List<MetadataPropertyType>',context!)
    };

    getTypeName() => "MetadataAttribute";
    TypeContext? context = _ctx;
}

class RefInfo implements IConvertible
{
    String? model;
    String? selfId;
    String? refId;
    String? refLabel;
    String? queryApi;

    RefInfo({this.model,this.selfId,this.refId,this.refLabel,this.queryApi});
    RefInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        model = json['model'];
        selfId = json['selfId'];
        refId = json['refId'];
        refLabel = json['refLabel'];
        queryApi = json['queryApi'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'model': model,
        'selfId': selfId,
        'refId': refId,
        'refLabel': refLabel,
        'queryApi': queryApi
    };

    getTypeName() => "RefInfo";
    TypeContext? context = _ctx;
}

class MetadataPropertyType implements IConvertible
{
    String? name;
    String? type;
    String? namespace;
    bool? isValueType;
    bool? isEnum;
    bool? isPrimaryKey;
    List<String>? genericArgs;
    String? value;
    String? description;
    MetadataDataMember? dataMember;
    bool? readOnly;
    String? paramType;
    String? displayType;
    bool? isRequired;
    List<String>? allowableValues;
    int? allowableMin;
    int? allowableMax;
    List<MetadataAttribute>? attributes;
    String? uploadTo;
    InputInfo? input;
    FormatInfo? format;
    RefInfo? ref;

    MetadataPropertyType({this.name,this.type,this.namespace,this.isValueType,this.isEnum,this.isPrimaryKey,this.genericArgs,this.value,this.description,this.dataMember,this.readOnly,this.paramType,this.displayType,this.isRequired,this.allowableValues,this.allowableMin,this.allowableMax,this.attributes,this.uploadTo,this.input,this.format,this.ref});
    MetadataPropertyType.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        type = json['type'];
        namespace = json['namespace'];
        isValueType = json['isValueType'];
        isEnum = json['isEnum'];
        isPrimaryKey = json['isPrimaryKey'];
        genericArgs = JsonConverters.fromJson(json['genericArgs'],'List<String>',context!);
        value = json['value'];
        description = json['description'];
        dataMember = JsonConverters.fromJson(json['dataMember'],'MetadataDataMember',context!);
        readOnly = json['readOnly'];
        paramType = json['paramType'];
        displayType = json['displayType'];
        isRequired = json['isRequired'];
        allowableValues = JsonConverters.fromJson(json['allowableValues'],'List<String>',context!);
        allowableMin = json['allowableMin'];
        allowableMax = json['allowableMax'];
        attributes = JsonConverters.fromJson(json['attributes'],'List<MetadataAttribute>',context!);
        uploadTo = json['uploadTo'];
        input = JsonConverters.fromJson(json['input'],'InputInfo',context!);
        format = JsonConverters.fromJson(json['format'],'FormatInfo',context!);
        ref = JsonConverters.fromJson(json['ref'],'RefInfo',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'type': type,
        'namespace': namespace,
        'isValueType': isValueType,
        'isEnum': isEnum,
        'isPrimaryKey': isPrimaryKey,
        'genericArgs': JsonConverters.toJson(genericArgs,'List<String>',context!),
        'value': value,
        'description': description,
        'dataMember': JsonConverters.toJson(dataMember,'MetadataDataMember',context!),
        'readOnly': readOnly,
        'paramType': paramType,
        'displayType': displayType,
        'isRequired': isRequired,
        'allowableValues': JsonConverters.toJson(allowableValues,'List<String>',context!),
        'allowableMin': allowableMin,
        'allowableMax': allowableMax,
        'attributes': JsonConverters.toJson(attributes,'List<MetadataAttribute>',context!),
        'uploadTo': uploadTo,
        'input': JsonConverters.toJson(input,'InputInfo',context!),
        'format': JsonConverters.toJson(format,'FormatInfo',context!),
        'ref': JsonConverters.toJson(ref,'RefInfo',context!)
    };

    getTypeName() => "MetadataPropertyType";
    TypeContext? context = _ctx;
}

class MetadataType implements IConvertible
{
    String? name;
    String? namespace;
    List<String>? genericArgs;
    MetadataTypeName? inherits;
    List<MetadataTypeName>? Implements;
    String? displayType;
    String? description;
    String? notes;
    ImageInfo? icon;
    bool? isNested;
    bool? isEnum;
    bool? isEnumInt;
    bool? isInterface;
    bool? isAbstract;
    bool? isGenericTypeDef;
    MetadataDataContract? dataContract;
    List<MetadataPropertyType>? properties;
    List<MetadataAttribute>? attributes;
    List<MetadataTypeName>? innerTypes;
    List<String>? enumNames;
    List<String>? enumValues;
    List<String>? enumMemberValues;
    List<String>? enumDescriptions;
    Map<String,String?>? meta;

    MetadataType({this.name,this.namespace,this.genericArgs,this.inherits,this.Implements,this.displayType,this.description,this.notes,this.icon,this.isNested,this.isEnum,this.isEnumInt,this.isInterface,this.isAbstract,this.isGenericTypeDef,this.dataContract,this.properties,this.attributes,this.innerTypes,this.enumNames,this.enumValues,this.enumMemberValues,this.enumDescriptions,this.meta});
    MetadataType.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        namespace = json['namespace'];
        genericArgs = JsonConverters.fromJson(json['genericArgs'],'List<String>',context!);
        inherits = JsonConverters.fromJson(json['inherits'],'MetadataTypeName',context!);
        Implements = JsonConverters.fromJson(json['implements'],'List<MetadataTypeName>',context!);
        displayType = json['displayType'];
        description = json['description'];
        notes = json['notes'];
        icon = JsonConverters.fromJson(json['icon'],'ImageInfo',context!);
        isNested = json['isNested'];
        isEnum = json['isEnum'];
        isEnumInt = json['isEnumInt'];
        isInterface = json['isInterface'];
        isAbstract = json['isAbstract'];
        isGenericTypeDef = json['isGenericTypeDef'];
        dataContract = JsonConverters.fromJson(json['dataContract'],'MetadataDataContract',context!);
        properties = JsonConverters.fromJson(json['properties'],'List<MetadataPropertyType>',context!);
        attributes = JsonConverters.fromJson(json['attributes'],'List<MetadataAttribute>',context!);
        innerTypes = JsonConverters.fromJson(json['innerTypes'],'List<MetadataTypeName>',context!);
        enumNames = JsonConverters.fromJson(json['enumNames'],'List<String>',context!);
        enumValues = JsonConverters.fromJson(json['enumValues'],'List<String>',context!);
        enumMemberValues = JsonConverters.fromJson(json['enumMemberValues'],'List<String>',context!);
        enumDescriptions = JsonConverters.fromJson(json['enumDescriptions'],'List<String>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'namespace': namespace,
        'genericArgs': JsonConverters.toJson(genericArgs,'List<String>',context!),
        'inherits': JsonConverters.toJson(inherits,'MetadataTypeName',context!),
        'implements': JsonConverters.toJson(Implements,'List<MetadataTypeName>',context!),
        'displayType': displayType,
        'description': description,
        'notes': notes,
        'icon': JsonConverters.toJson(icon,'ImageInfo',context!),
        'isNested': isNested,
        'isEnum': isEnum,
        'isEnumInt': isEnumInt,
        'isInterface': isInterface,
        'isAbstract': isAbstract,
        'isGenericTypeDef': isGenericTypeDef,
        'dataContract': JsonConverters.toJson(dataContract,'MetadataDataContract',context!),
        'properties': JsonConverters.toJson(properties,'List<MetadataPropertyType>',context!),
        'attributes': JsonConverters.toJson(attributes,'List<MetadataAttribute>',context!),
        'innerTypes': JsonConverters.toJson(innerTypes,'List<MetadataTypeName>',context!),
        'enumNames': JsonConverters.toJson(enumNames,'List<String>',context!),
        'enumValues': JsonConverters.toJson(enumValues,'List<String>',context!),
        'enumMemberValues': JsonConverters.toJson(enumMemberValues,'List<String>',context!),
        'enumDescriptions': JsonConverters.toJson(enumDescriptions,'List<String>',context!),
        'meta': meta
    };

    getTypeName() => "MetadataType";
    TypeContext? context = _ctx;
}

class CommandInfo implements IConvertible
{
    String? name;
    String? tag;
    MetadataType? request;
    MetadataType? response;

    CommandInfo({this.name,this.tag,this.request,this.response});
    CommandInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        tag = json['tag'];
        request = JsonConverters.fromJson(json['request'],'MetadataType',context!);
        response = JsonConverters.fromJson(json['response'],'MetadataType',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'tag': tag,
        'request': JsonConverters.toJson(request,'MetadataType',context!),
        'response': JsonConverters.toJson(response,'MetadataType',context!)
    };

    getTypeName() => "CommandInfo";
    TypeContext? context = _ctx;
}

class CommandsInfo implements IConvertible
{
    List<CommandInfo>? commands;
    Map<String,String?>? meta;

    CommandsInfo({this.commands,this.meta});
    CommandsInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        commands = JsonConverters.fromJson(json['commands'],'List<CommandInfo>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'commands': JsonConverters.toJson(commands,'List<CommandInfo>',context!),
        'meta': meta
    };

    getTypeName() => "CommandsInfo";
    TypeContext? context = _ctx;
}

class AutoQueryConvention implements IConvertible
{
    String? name;
    String? value;
    String? types;
    String? valueType;

    AutoQueryConvention({this.name,this.value,this.types,this.valueType});
    AutoQueryConvention.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        value = json['value'];
        types = json['types'];
        valueType = json['valueType'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'value': value,
        'types': types,
        'valueType': valueType
    };

    getTypeName() => "AutoQueryConvention";
    TypeContext? context = _ctx;
}

class AutoQueryInfo implements IConvertible
{
    int? maxLimit;
    bool? untypedQueries;
    bool? rawSqlFilters;
    bool? autoQueryViewer;
    bool? Async;
    bool? orderByPrimaryKey;
    bool? crudEvents;
    bool? crudEventsServices;
    String? accessRole;
    String? namedConnection;
    List<AutoQueryConvention>? viewerConventions;
    Map<String,String?>? meta;

    AutoQueryInfo({this.maxLimit,this.untypedQueries,this.rawSqlFilters,this.autoQueryViewer,this.Async,this.orderByPrimaryKey,this.crudEvents,this.crudEventsServices,this.accessRole,this.namedConnection,this.viewerConventions,this.meta});
    AutoQueryInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        maxLimit = json['maxLimit'];
        untypedQueries = json['untypedQueries'];
        rawSqlFilters = json['rawSqlFilters'];
        autoQueryViewer = json['autoQueryViewer'];
        Async = json['async'];
        orderByPrimaryKey = json['orderByPrimaryKey'];
        crudEvents = json['crudEvents'];
        crudEventsServices = json['crudEventsServices'];
        accessRole = json['accessRole'];
        namedConnection = json['namedConnection'];
        viewerConventions = JsonConverters.fromJson(json['viewerConventions'],'List<AutoQueryConvention>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'maxLimit': maxLimit,
        'untypedQueries': untypedQueries,
        'rawSqlFilters': rawSqlFilters,
        'autoQueryViewer': autoQueryViewer,
        'async': Async,
        'orderByPrimaryKey': orderByPrimaryKey,
        'crudEvents': crudEvents,
        'crudEventsServices': crudEventsServices,
        'accessRole': accessRole,
        'namedConnection': namedConnection,
        'viewerConventions': JsonConverters.toJson(viewerConventions,'List<AutoQueryConvention>',context!),
        'meta': meta
    };

    getTypeName() => "AutoQueryInfo";
    TypeContext? context = _ctx;
}

class ScriptMethodType implements IConvertible
{
    String? name;
    List<String>? paramNames;
    List<String>? paramTypes;
    String? returnType;

    ScriptMethodType({this.name,this.paramNames,this.paramTypes,this.returnType});
    ScriptMethodType.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        paramNames = JsonConverters.fromJson(json['paramNames'],'List<String>',context!);
        paramTypes = JsonConverters.fromJson(json['paramTypes'],'List<String>',context!);
        returnType = json['returnType'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'paramNames': JsonConverters.toJson(paramNames,'List<String>',context!),
        'paramTypes': JsonConverters.toJson(paramTypes,'List<String>',context!),
        'returnType': returnType
    };

    getTypeName() => "ScriptMethodType";
    TypeContext? context = _ctx;
}

class ValidationInfo implements IConvertible
{
    bool? hasValidationSource;
    bool? hasValidationSourceAdmin;
    Map<String,List<String>?>? serviceRoutes;
    List<ScriptMethodType>? typeValidators;
    List<ScriptMethodType>? propertyValidators;
    String? accessRole;
    Map<String,String?>? meta;

    ValidationInfo({this.hasValidationSource,this.hasValidationSourceAdmin,this.serviceRoutes,this.typeValidators,this.propertyValidators,this.accessRole,this.meta});
    ValidationInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        hasValidationSource = json['hasValidationSource'];
        hasValidationSourceAdmin = json['hasValidationSourceAdmin'];
        serviceRoutes = JsonConverters.fromJson(json['serviceRoutes'],'Map<String,List<String>?>',context!);
        typeValidators = JsonConverters.fromJson(json['typeValidators'],'List<ScriptMethodType>',context!);
        propertyValidators = JsonConverters.fromJson(json['propertyValidators'],'List<ScriptMethodType>',context!);
        accessRole = json['accessRole'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'hasValidationSource': hasValidationSource,
        'hasValidationSourceAdmin': hasValidationSourceAdmin,
        'serviceRoutes': JsonConverters.toJson(serviceRoutes,'Map<String,List<String>?>',context!),
        'typeValidators': JsonConverters.toJson(typeValidators,'List<ScriptMethodType>',context!),
        'propertyValidators': JsonConverters.toJson(propertyValidators,'List<ScriptMethodType>',context!),
        'accessRole': accessRole,
        'meta': meta
    };

    getTypeName() => "ValidationInfo";
    TypeContext? context = _ctx;
}

class SharpPagesInfo implements IConvertible
{
    String? apiPath;
    String? scriptAdminRole;
    String? metadataDebugAdminRole;
    bool? metadataDebug;
    bool? spaFallback;
    Map<String,String?>? meta;

    SharpPagesInfo({this.apiPath,this.scriptAdminRole,this.metadataDebugAdminRole,this.metadataDebug,this.spaFallback,this.meta});
    SharpPagesInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        apiPath = json['apiPath'];
        scriptAdminRole = json['scriptAdminRole'];
        metadataDebugAdminRole = json['metadataDebugAdminRole'];
        metadataDebug = json['metadataDebug'];
        spaFallback = json['spaFallback'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'apiPath': apiPath,
        'scriptAdminRole': scriptAdminRole,
        'metadataDebugAdminRole': metadataDebugAdminRole,
        'metadataDebug': metadataDebug,
        'spaFallback': spaFallback,
        'meta': meta
    };

    getTypeName() => "SharpPagesInfo";
    TypeContext? context = _ctx;
}

class RequestLogsInfo implements IConvertible
{
    String? accessRole;
    String? requestLogger;
    int? defaultLimit;
    Map<String,List<String>?>? serviceRoutes;
    Map<String,String?>? meta;

    RequestLogsInfo({this.accessRole,this.requestLogger,this.defaultLimit,this.serviceRoutes,this.meta});
    RequestLogsInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        accessRole = json['accessRole'];
        requestLogger = json['requestLogger'];
        defaultLimit = json['defaultLimit'];
        serviceRoutes = JsonConverters.fromJson(json['serviceRoutes'],'Map<String,List<String>?>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'accessRole': accessRole,
        'requestLogger': requestLogger,
        'defaultLimit': defaultLimit,
        'serviceRoutes': JsonConverters.toJson(serviceRoutes,'Map<String,List<String>?>',context!),
        'meta': meta
    };

    getTypeName() => "RequestLogsInfo";
    TypeContext? context = _ctx;
}

class ProfilingInfo implements IConvertible
{
    String? accessRole;
    int? defaultLimit;
    List<String>? summaryFields;
    String? tagLabel;
    Map<String,String?>? meta;

    ProfilingInfo({this.accessRole,this.defaultLimit,this.summaryFields,this.tagLabel,this.meta});
    ProfilingInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        accessRole = json['accessRole'];
        defaultLimit = json['defaultLimit'];
        summaryFields = JsonConverters.fromJson(json['summaryFields'],'List<String>',context!);
        tagLabel = json['tagLabel'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'accessRole': accessRole,
        'defaultLimit': defaultLimit,
        'summaryFields': JsonConverters.toJson(summaryFields,'List<String>',context!),
        'tagLabel': tagLabel,
        'meta': meta
    };

    getTypeName() => "ProfilingInfo";
    TypeContext? context = _ctx;
}

class FilesUploadLocation implements IConvertible
{
    String? name;
    String? readAccessRole;
    String? writeAccessRole;
    List<String>? allowExtensions;
    String? allowOperations;
    int? maxFileCount;
    int? minFileBytes;
    int? maxFileBytes;

    FilesUploadLocation({this.name,this.readAccessRole,this.writeAccessRole,this.allowExtensions,this.allowOperations,this.maxFileCount,this.minFileBytes,this.maxFileBytes});
    FilesUploadLocation.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        readAccessRole = json['readAccessRole'];
        writeAccessRole = json['writeAccessRole'];
        allowExtensions = JsonConverters.fromJson(json['allowExtensions'],'List<String>',context!);
        allowOperations = json['allowOperations'];
        maxFileCount = json['maxFileCount'];
        minFileBytes = json['minFileBytes'];
        maxFileBytes = json['maxFileBytes'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'readAccessRole': readAccessRole,
        'writeAccessRole': writeAccessRole,
        'allowExtensions': JsonConverters.toJson(allowExtensions,'List<String>',context!),
        'allowOperations': allowOperations,
        'maxFileCount': maxFileCount,
        'minFileBytes': minFileBytes,
        'maxFileBytes': maxFileBytes
    };

    getTypeName() => "FilesUploadLocation";
    TypeContext? context = _ctx;
}

class FilesUploadInfo implements IConvertible
{
    String? basePath;
    List<FilesUploadLocation>? locations;
    Map<String,String?>? meta;

    FilesUploadInfo({this.basePath,this.locations,this.meta});
    FilesUploadInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        basePath = json['basePath'];
        locations = JsonConverters.fromJson(json['locations'],'List<FilesUploadLocation>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'basePath': basePath,
        'locations': JsonConverters.toJson(locations,'List<FilesUploadLocation>',context!),
        'meta': meta
    };

    getTypeName() => "FilesUploadInfo";
    TypeContext? context = _ctx;
}

class MediaRule implements IConvertible
{
    String? size;
    String? rule;
    List<String>? applyTo;
    Map<String,String?>? meta;

    MediaRule({this.size,this.rule,this.applyTo,this.meta});
    MediaRule.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        size = json['size'];
        rule = json['rule'];
        applyTo = JsonConverters.fromJson(json['applyTo'],'List<String>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'size': size,
        'rule': rule,
        'applyTo': JsonConverters.toJson(applyTo,'List<String>',context!),
        'meta': meta
    };

    getTypeName() => "MediaRule";
    TypeContext? context = _ctx;
}

class AdminUsersInfo implements IConvertible
{
    String? accessRole;
    List<String>? enabled;
    MetadataType? userAuth;
    List<String>? allRoles;
    List<String>? allPermissions;
    List<String>? queryUserAuthProperties;
    List<MediaRule>? queryMediaRules;
    List<InputInfo>? formLayout;
    ApiCss? css;
    Map<String,String?>? meta;

    AdminUsersInfo({this.accessRole,this.enabled,this.userAuth,this.allRoles,this.allPermissions,this.queryUserAuthProperties,this.queryMediaRules,this.formLayout,this.css,this.meta});
    AdminUsersInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        accessRole = json['accessRole'];
        enabled = JsonConverters.fromJson(json['enabled'],'List<String>',context!);
        userAuth = JsonConverters.fromJson(json['userAuth'],'MetadataType',context!);
        allRoles = JsonConverters.fromJson(json['allRoles'],'List<String>',context!);
        allPermissions = JsonConverters.fromJson(json['allPermissions'],'List<String>',context!);
        queryUserAuthProperties = JsonConverters.fromJson(json['queryUserAuthProperties'],'List<String>',context!);
        queryMediaRules = JsonConverters.fromJson(json['queryMediaRules'],'List<MediaRule>',context!);
        formLayout = JsonConverters.fromJson(json['formLayout'],'List<InputInfo>',context!);
        css = JsonConverters.fromJson(json['css'],'ApiCss',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'accessRole': accessRole,
        'enabled': JsonConverters.toJson(enabled,'List<String>',context!),
        'userAuth': JsonConverters.toJson(userAuth,'MetadataType',context!),
        'allRoles': JsonConverters.toJson(allRoles,'List<String>',context!),
        'allPermissions': JsonConverters.toJson(allPermissions,'List<String>',context!),
        'queryUserAuthProperties': JsonConverters.toJson(queryUserAuthProperties,'List<String>',context!),
        'queryMediaRules': JsonConverters.toJson(queryMediaRules,'List<MediaRule>',context!),
        'formLayout': JsonConverters.toJson(formLayout,'List<InputInfo>',context!),
        'css': JsonConverters.toJson(css,'ApiCss',context!),
        'meta': meta
    };

    getTypeName() => "AdminUsersInfo";
    TypeContext? context = _ctx;
}

class AdminIdentityUsersInfo implements IConvertible
{
    String? accessRole;
    List<String>? enabled;
    MetadataType? identityUser;
    List<String>? allRoles;
    List<String>? allPermissions;
    List<String>? queryIdentityUserProperties;
    List<MediaRule>? queryMediaRules;
    List<InputInfo>? formLayout;
    ApiCss? css;
    Map<String,String?>? meta;

    AdminIdentityUsersInfo({this.accessRole,this.enabled,this.identityUser,this.allRoles,this.allPermissions,this.queryIdentityUserProperties,this.queryMediaRules,this.formLayout,this.css,this.meta});
    AdminIdentityUsersInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        accessRole = json['accessRole'];
        enabled = JsonConverters.fromJson(json['enabled'],'List<String>',context!);
        identityUser = JsonConverters.fromJson(json['identityUser'],'MetadataType',context!);
        allRoles = JsonConverters.fromJson(json['allRoles'],'List<String>',context!);
        allPermissions = JsonConverters.fromJson(json['allPermissions'],'List<String>',context!);
        queryIdentityUserProperties = JsonConverters.fromJson(json['queryIdentityUserProperties'],'List<String>',context!);
        queryMediaRules = JsonConverters.fromJson(json['queryMediaRules'],'List<MediaRule>',context!);
        formLayout = JsonConverters.fromJson(json['formLayout'],'List<InputInfo>',context!);
        css = JsonConverters.fromJson(json['css'],'ApiCss',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'accessRole': accessRole,
        'enabled': JsonConverters.toJson(enabled,'List<String>',context!),
        'identityUser': JsonConverters.toJson(identityUser,'MetadataType',context!),
        'allRoles': JsonConverters.toJson(allRoles,'List<String>',context!),
        'allPermissions': JsonConverters.toJson(allPermissions,'List<String>',context!),
        'queryIdentityUserProperties': JsonConverters.toJson(queryIdentityUserProperties,'List<String>',context!),
        'queryMediaRules': JsonConverters.toJson(queryMediaRules,'List<MediaRule>',context!),
        'formLayout': JsonConverters.toJson(formLayout,'List<InputInfo>',context!),
        'css': JsonConverters.toJson(css,'ApiCss',context!),
        'meta': meta
    };

    getTypeName() => "AdminIdentityUsersInfo";
    TypeContext? context = _ctx;
}

class AdminRedisInfo implements IConvertible
{
    int? queryLimit;
    List<int>? databases;
    bool? modifiableConnection;
    RedisEndpointInfo? endpoint;
    Map<String,String?>? meta;

    AdminRedisInfo({this.queryLimit,this.databases,this.modifiableConnection,this.endpoint,this.meta});
    AdminRedisInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        queryLimit = json['queryLimit'];
        databases = JsonConverters.fromJson(json['databases'],'List<int>',context!);
        modifiableConnection = json['modifiableConnection'];
        endpoint = JsonConverters.fromJson(json['endpoint'],'RedisEndpointInfo',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'queryLimit': queryLimit,
        'databases': JsonConverters.toJson(databases,'List<int>',context!),
        'modifiableConnection': modifiableConnection,
        'endpoint': JsonConverters.toJson(endpoint,'RedisEndpointInfo',context!),
        'meta': meta
    };

    getTypeName() => "AdminRedisInfo";
    TypeContext? context = _ctx;
}

class SchemaInfo implements IConvertible
{
    String? alias;
    String? name;
    List<String>? tables;

    SchemaInfo({this.alias,this.name,this.tables});
    SchemaInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        alias = json['alias'];
        name = json['name'];
        tables = JsonConverters.fromJson(json['tables'],'List<String>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'alias': alias,
        'name': name,
        'tables': JsonConverters.toJson(tables,'List<String>',context!)
    };

    getTypeName() => "SchemaInfo";
    TypeContext? context = _ctx;
}

class DatabaseInfo implements IConvertible
{
    String? alias;
    String? name;
    List<SchemaInfo>? schemas;

    DatabaseInfo({this.alias,this.name,this.schemas});
    DatabaseInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        alias = json['alias'];
        name = json['name'];
        schemas = JsonConverters.fromJson(json['schemas'],'List<SchemaInfo>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'alias': alias,
        'name': name,
        'schemas': JsonConverters.toJson(schemas,'List<SchemaInfo>',context!)
    };

    getTypeName() => "DatabaseInfo";
    TypeContext? context = _ctx;
}

class AdminDatabaseInfo implements IConvertible
{
    int? queryLimit;
    List<DatabaseInfo>? databases;
    Map<String,String?>? meta;

    AdminDatabaseInfo({this.queryLimit,this.databases,this.meta});
    AdminDatabaseInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        queryLimit = json['queryLimit'];
        databases = JsonConverters.fromJson(json['databases'],'List<DatabaseInfo>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'queryLimit': queryLimit,
        'databases': JsonConverters.toJson(databases,'List<DatabaseInfo>',context!),
        'meta': meta
    };

    getTypeName() => "AdminDatabaseInfo";
    TypeContext? context = _ctx;
}

class PluginInfo implements IConvertible
{
    List<String>? loaded;
    AuthInfo? auth;
    ApiKeyInfo? apiKey;
    CommandsInfo? commands;
    AutoQueryInfo? autoQuery;
    ValidationInfo? validation;
    SharpPagesInfo? sharpPages;
    RequestLogsInfo? requestLogs;
    ProfilingInfo? profiling;
    FilesUploadInfo? filesUpload;
    AdminUsersInfo? adminUsers;
    AdminIdentityUsersInfo? adminIdentityUsers;
    AdminRedisInfo? adminRedis;
    AdminDatabaseInfo? adminDatabase;
    Map<String,String?>? meta;

    PluginInfo({this.loaded,this.auth,this.apiKey,this.commands,this.autoQuery,this.validation,this.sharpPages,this.requestLogs,this.profiling,this.filesUpload,this.adminUsers,this.adminIdentityUsers,this.adminRedis,this.adminDatabase,this.meta});
    PluginInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        loaded = JsonConverters.fromJson(json['loaded'],'List<String>',context!);
        auth = JsonConverters.fromJson(json['auth'],'AuthInfo',context!);
        apiKey = JsonConverters.fromJson(json['apiKey'],'ApiKeyInfo',context!);
        commands = JsonConverters.fromJson(json['commands'],'CommandsInfo',context!);
        autoQuery = JsonConverters.fromJson(json['autoQuery'],'AutoQueryInfo',context!);
        validation = JsonConverters.fromJson(json['validation'],'ValidationInfo',context!);
        sharpPages = JsonConverters.fromJson(json['sharpPages'],'SharpPagesInfo',context!);
        requestLogs = JsonConverters.fromJson(json['requestLogs'],'RequestLogsInfo',context!);
        profiling = JsonConverters.fromJson(json['profiling'],'ProfilingInfo',context!);
        filesUpload = JsonConverters.fromJson(json['filesUpload'],'FilesUploadInfo',context!);
        adminUsers = JsonConverters.fromJson(json['adminUsers'],'AdminUsersInfo',context!);
        adminIdentityUsers = JsonConverters.fromJson(json['adminIdentityUsers'],'AdminIdentityUsersInfo',context!);
        adminRedis = JsonConverters.fromJson(json['adminRedis'],'AdminRedisInfo',context!);
        adminDatabase = JsonConverters.fromJson(json['adminDatabase'],'AdminDatabaseInfo',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'loaded': JsonConverters.toJson(loaded,'List<String>',context!),
        'auth': JsonConverters.toJson(auth,'AuthInfo',context!),
        'apiKey': JsonConverters.toJson(apiKey,'ApiKeyInfo',context!),
        'commands': JsonConverters.toJson(commands,'CommandsInfo',context!),
        'autoQuery': JsonConverters.toJson(autoQuery,'AutoQueryInfo',context!),
        'validation': JsonConverters.toJson(validation,'ValidationInfo',context!),
        'sharpPages': JsonConverters.toJson(sharpPages,'SharpPagesInfo',context!),
        'requestLogs': JsonConverters.toJson(requestLogs,'RequestLogsInfo',context!),
        'profiling': JsonConverters.toJson(profiling,'ProfilingInfo',context!),
        'filesUpload': JsonConverters.toJson(filesUpload,'FilesUploadInfo',context!),
        'adminUsers': JsonConverters.toJson(adminUsers,'AdminUsersInfo',context!),
        'adminIdentityUsers': JsonConverters.toJson(adminIdentityUsers,'AdminIdentityUsersInfo',context!),
        'adminRedis': JsonConverters.toJson(adminRedis,'AdminRedisInfo',context!),
        'adminDatabase': JsonConverters.toJson(adminDatabase,'AdminDatabaseInfo',context!),
        'meta': meta
    };

    getTypeName() => "PluginInfo";
    TypeContext? context = _ctx;
}

class CustomPluginInfo implements IConvertible
{
    String? accessRole;
    Map<String,List<String>?>? serviceRoutes;
    List<String>? enabled;
    Map<String,String?>? meta;

    CustomPluginInfo({this.accessRole,this.serviceRoutes,this.enabled,this.meta});
    CustomPluginInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        accessRole = json['accessRole'];
        serviceRoutes = JsonConverters.fromJson(json['serviceRoutes'],'Map<String,List<String>?>',context!);
        enabled = JsonConverters.fromJson(json['enabled'],'List<String>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'accessRole': accessRole,
        'serviceRoutes': JsonConverters.toJson(serviceRoutes,'Map<String,List<String>?>',context!),
        'enabled': JsonConverters.toJson(enabled,'List<String>',context!),
        'meta': meta
    };

    getTypeName() => "CustomPluginInfo";
    TypeContext? context = _ctx;
}

class MetadataTypesConfig implements IConvertible
{
    String? baseUrl;
    String? usePath;
    bool? makePartial;
    bool? makeVirtual;
    bool? makeInternal;
    String? baseClass;
    String? package;
    bool? addReturnMarker;
    bool? addDescriptionAsComments;
    bool? addDocAnnotations;
    bool? addDataContractAttributes;
    bool? addIndexesToDataMembers;
    bool? addGeneratedCodeAttributes;
    int? addImplicitVersion;
    bool? addResponseStatus;
    bool? addServiceStackTypes;
    bool? addModelExtensions;
    bool? addPropertyAccessors;
    bool? excludeGenericBaseTypes;
    bool? settersReturnThis;
    bool? addNullableAnnotations;
    bool? makePropertiesOptional;
    bool? exportAsTypes;
    bool? excludeImplementedInterfaces;
    String? addDefaultXmlNamespace;
    bool? makeDataContractsExtensible;
    bool? initializeCollections;
    List<String>? addNamespaces;
    List<String>? defaultNamespaces;
    List<String>? defaultImports;
    List<String>? includeTypes;
    List<String>? excludeTypes;
    List<String>? exportTags;
    List<String>? treatTypesAsStrings;
    bool? exportValueTypes;
    String? globalNamespace;
    bool? excludeNamespace;
    String? dataClass;
    String? dataClassJson;
    List<String>? ignoreTypes;
    List<String>? exportTypes;
    List<String>? exportAttributes;
    List<String>? ignoreTypesInNamespaces;

    MetadataTypesConfig({this.baseUrl,this.usePath,this.makePartial,this.makeVirtual,this.makeInternal,this.baseClass,this.package,this.addReturnMarker,this.addDescriptionAsComments,this.addDocAnnotations,this.addDataContractAttributes,this.addIndexesToDataMembers,this.addGeneratedCodeAttributes,this.addImplicitVersion,this.addResponseStatus,this.addServiceStackTypes,this.addModelExtensions,this.addPropertyAccessors,this.excludeGenericBaseTypes,this.settersReturnThis,this.addNullableAnnotations,this.makePropertiesOptional,this.exportAsTypes,this.excludeImplementedInterfaces,this.addDefaultXmlNamespace,this.makeDataContractsExtensible,this.initializeCollections,this.addNamespaces,this.defaultNamespaces,this.defaultImports,this.includeTypes,this.excludeTypes,this.exportTags,this.treatTypesAsStrings,this.exportValueTypes,this.globalNamespace,this.excludeNamespace,this.dataClass,this.dataClassJson,this.ignoreTypes,this.exportTypes,this.exportAttributes,this.ignoreTypesInNamespaces});
    MetadataTypesConfig.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        baseUrl = json['baseUrl'];
        usePath = json['usePath'];
        makePartial = json['makePartial'];
        makeVirtual = json['makeVirtual'];
        makeInternal = json['makeInternal'];
        baseClass = json['baseClass'];
        package = json['package'];
        addReturnMarker = json['addReturnMarker'];
        addDescriptionAsComments = json['addDescriptionAsComments'];
        addDocAnnotations = json['addDocAnnotations'];
        addDataContractAttributes = json['addDataContractAttributes'];
        addIndexesToDataMembers = json['addIndexesToDataMembers'];
        addGeneratedCodeAttributes = json['addGeneratedCodeAttributes'];
        addImplicitVersion = json['addImplicitVersion'];
        addResponseStatus = json['addResponseStatus'];
        addServiceStackTypes = json['addServiceStackTypes'];
        addModelExtensions = json['addModelExtensions'];
        addPropertyAccessors = json['addPropertyAccessors'];
        excludeGenericBaseTypes = json['excludeGenericBaseTypes'];
        settersReturnThis = json['settersReturnThis'];
        addNullableAnnotations = json['addNullableAnnotations'];
        makePropertiesOptional = json['makePropertiesOptional'];
        exportAsTypes = json['exportAsTypes'];
        excludeImplementedInterfaces = json['excludeImplementedInterfaces'];
        addDefaultXmlNamespace = json['addDefaultXmlNamespace'];
        makeDataContractsExtensible = json['makeDataContractsExtensible'];
        initializeCollections = json['initializeCollections'];
        addNamespaces = JsonConverters.fromJson(json['addNamespaces'],'List<String>',context!);
        defaultNamespaces = JsonConverters.fromJson(json['defaultNamespaces'],'List<String>',context!);
        defaultImports = JsonConverters.fromJson(json['defaultImports'],'List<String>',context!);
        includeTypes = JsonConverters.fromJson(json['includeTypes'],'List<String>',context!);
        excludeTypes = JsonConverters.fromJson(json['excludeTypes'],'List<String>',context!);
        exportTags = JsonConverters.fromJson(json['exportTags'],'List<String>',context!);
        treatTypesAsStrings = JsonConverters.fromJson(json['treatTypesAsStrings'],'List<String>',context!);
        exportValueTypes = json['exportValueTypes'];
        globalNamespace = json['globalNamespace'];
        excludeNamespace = json['excludeNamespace'];
        dataClass = json['dataClass'];
        dataClassJson = json['dataClassJson'];
        ignoreTypes = JsonConverters.fromJson(json['ignoreTypes'],'List<String>',context!);
        exportTypes = JsonConverters.fromJson(json['exportTypes'],'List<String>',context!);
        exportAttributes = JsonConverters.fromJson(json['exportAttributes'],'List<String>',context!);
        ignoreTypesInNamespaces = JsonConverters.fromJson(json['ignoreTypesInNamespaces'],'List<String>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'baseUrl': baseUrl,
        'usePath': usePath,
        'makePartial': makePartial,
        'makeVirtual': makeVirtual,
        'makeInternal': makeInternal,
        'baseClass': baseClass,
        'package': package,
        'addReturnMarker': addReturnMarker,
        'addDescriptionAsComments': addDescriptionAsComments,
        'addDocAnnotations': addDocAnnotations,
        'addDataContractAttributes': addDataContractAttributes,
        'addIndexesToDataMembers': addIndexesToDataMembers,
        'addGeneratedCodeAttributes': addGeneratedCodeAttributes,
        'addImplicitVersion': addImplicitVersion,
        'addResponseStatus': addResponseStatus,
        'addServiceStackTypes': addServiceStackTypes,
        'addModelExtensions': addModelExtensions,
        'addPropertyAccessors': addPropertyAccessors,
        'excludeGenericBaseTypes': excludeGenericBaseTypes,
        'settersReturnThis': settersReturnThis,
        'addNullableAnnotations': addNullableAnnotations,
        'makePropertiesOptional': makePropertiesOptional,
        'exportAsTypes': exportAsTypes,
        'excludeImplementedInterfaces': excludeImplementedInterfaces,
        'addDefaultXmlNamespace': addDefaultXmlNamespace,
        'makeDataContractsExtensible': makeDataContractsExtensible,
        'initializeCollections': initializeCollections,
        'addNamespaces': JsonConverters.toJson(addNamespaces,'List<String>',context!),
        'defaultNamespaces': JsonConverters.toJson(defaultNamespaces,'List<String>',context!),
        'defaultImports': JsonConverters.toJson(defaultImports,'List<String>',context!),
        'includeTypes': JsonConverters.toJson(includeTypes,'List<String>',context!),
        'excludeTypes': JsonConverters.toJson(excludeTypes,'List<String>',context!),
        'exportTags': JsonConverters.toJson(exportTags,'List<String>',context!),
        'treatTypesAsStrings': JsonConverters.toJson(treatTypesAsStrings,'List<String>',context!),
        'exportValueTypes': exportValueTypes,
        'globalNamespace': globalNamespace,
        'excludeNamespace': excludeNamespace,
        'dataClass': dataClass,
        'dataClassJson': dataClassJson,
        'ignoreTypes': JsonConverters.toJson(ignoreTypes,'List<String>',context!),
        'exportTypes': JsonConverters.toJson(exportTypes,'List<String>',context!),
        'exportAttributes': JsonConverters.toJson(exportAttributes,'List<String>',context!),
        'ignoreTypesInNamespaces': JsonConverters.toJson(ignoreTypesInNamespaces,'List<String>',context!)
    };

    getTypeName() => "MetadataTypesConfig";
    TypeContext? context = _ctx;
}

class MetadataRoute implements IConvertible
{
    String? path;
    String? verbs;
    String? notes;
    String? summary;

    MetadataRoute({this.path,this.verbs,this.notes,this.summary});
    MetadataRoute.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        path = json['path'];
        verbs = json['verbs'];
        notes = json['notes'];
        summary = json['summary'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'path': path,
        'verbs': verbs,
        'notes': notes,
        'summary': summary
    };

    getTypeName() => "MetadataRoute";
    TypeContext? context = _ctx;
}

class ApiUiInfo implements IConvertible
{
    ApiCss? locodeCss;
    ApiCss? explorerCss;
    List<InputInfo>? formLayout;
    Map<String,String?>? meta;

    ApiUiInfo({this.locodeCss,this.explorerCss,this.formLayout,this.meta});
    ApiUiInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        locodeCss = JsonConverters.fromJson(json['locodeCss'],'ApiCss',context!);
        explorerCss = JsonConverters.fromJson(json['explorerCss'],'ApiCss',context!);
        formLayout = JsonConverters.fromJson(json['formLayout'],'List<InputInfo>',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'locodeCss': JsonConverters.toJson(locodeCss,'ApiCss',context!),
        'explorerCss': JsonConverters.toJson(explorerCss,'ApiCss',context!),
        'formLayout': JsonConverters.toJson(formLayout,'List<InputInfo>',context!),
        'meta': meta
    };

    getTypeName() => "ApiUiInfo";
    TypeContext? context = _ctx;
}

class MetadataOperationType implements IConvertible
{
    MetadataType? request;
    MetadataType? response;
    List<String>? actions;
    bool? returnsVoid;
    String? method;
    MetadataTypeName? returnType;
    List<MetadataRoute>? routes;
    MetadataTypeName? dataModel;
    MetadataTypeName? viewModel;
    bool? requiresAuth;
    bool? requiresApiKey;
    List<String>? requiredRoles;
    List<String>? requiresAnyRole;
    List<String>? requiredPermissions;
    List<String>? requiresAnyPermission;
    List<String>? tags;
    ApiUiInfo? ui;

    MetadataOperationType({this.request,this.response,this.actions,this.returnsVoid,this.method,this.returnType,this.routes,this.dataModel,this.viewModel,this.requiresAuth,this.requiresApiKey,this.requiredRoles,this.requiresAnyRole,this.requiredPermissions,this.requiresAnyPermission,this.tags,this.ui});
    MetadataOperationType.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        request = JsonConverters.fromJson(json['request'],'MetadataType',context!);
        response = JsonConverters.fromJson(json['response'],'MetadataType',context!);
        actions = JsonConverters.fromJson(json['actions'],'List<String>',context!);
        returnsVoid = json['returnsVoid'];
        method = json['method'];
        returnType = JsonConverters.fromJson(json['returnType'],'MetadataTypeName',context!);
        routes = JsonConverters.fromJson(json['routes'],'List<MetadataRoute>',context!);
        dataModel = JsonConverters.fromJson(json['dataModel'],'MetadataTypeName',context!);
        viewModel = JsonConverters.fromJson(json['viewModel'],'MetadataTypeName',context!);
        requiresAuth = json['requiresAuth'];
        requiresApiKey = json['requiresApiKey'];
        requiredRoles = JsonConverters.fromJson(json['requiredRoles'],'List<String>',context!);
        requiresAnyRole = JsonConverters.fromJson(json['requiresAnyRole'],'List<String>',context!);
        requiredPermissions = JsonConverters.fromJson(json['requiredPermissions'],'List<String>',context!);
        requiresAnyPermission = JsonConverters.fromJson(json['requiresAnyPermission'],'List<String>',context!);
        tags = JsonConverters.fromJson(json['tags'],'List<String>',context!);
        ui = JsonConverters.fromJson(json['ui'],'ApiUiInfo',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'request': JsonConverters.toJson(request,'MetadataType',context!),
        'response': JsonConverters.toJson(response,'MetadataType',context!),
        'actions': JsonConverters.toJson(actions,'List<String>',context!),
        'returnsVoid': returnsVoid,
        'method': method,
        'returnType': JsonConverters.toJson(returnType,'MetadataTypeName',context!),
        'routes': JsonConverters.toJson(routes,'List<MetadataRoute>',context!),
        'dataModel': JsonConverters.toJson(dataModel,'MetadataTypeName',context!),
        'viewModel': JsonConverters.toJson(viewModel,'MetadataTypeName',context!),
        'requiresAuth': requiresAuth,
        'requiresApiKey': requiresApiKey,
        'requiredRoles': JsonConverters.toJson(requiredRoles,'List<String>',context!),
        'requiresAnyRole': JsonConverters.toJson(requiresAnyRole,'List<String>',context!),
        'requiredPermissions': JsonConverters.toJson(requiredPermissions,'List<String>',context!),
        'requiresAnyPermission': JsonConverters.toJson(requiresAnyPermission,'List<String>',context!),
        'tags': JsonConverters.toJson(tags,'List<String>',context!),
        'ui': JsonConverters.toJson(ui,'ApiUiInfo',context!)
    };

    getTypeName() => "MetadataOperationType";
    TypeContext? context = _ctx;
}

class MetadataTypes implements IConvertible
{
    MetadataTypesConfig? config;
    List<String>? namespaces;
    List<MetadataType>? types;
    List<MetadataOperationType>? operations;

    MetadataTypes({this.config,this.namespaces,this.types,this.operations});
    MetadataTypes.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        config = JsonConverters.fromJson(json['config'],'MetadataTypesConfig',context!);
        namespaces = JsonConverters.fromJson(json['namespaces'],'List<String>',context!);
        types = JsonConverters.fromJson(json['types'],'List<MetadataType>',context!);
        operations = JsonConverters.fromJson(json['operations'],'List<MetadataOperationType>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'config': JsonConverters.toJson(config,'MetadataTypesConfig',context!),
        'namespaces': JsonConverters.toJson(namespaces,'List<String>',context!),
        'types': JsonConverters.toJson(types,'List<MetadataType>',context!),
        'operations': JsonConverters.toJson(operations,'List<MetadataOperationType>',context!)
    };

    getTypeName() => "MetadataTypes";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminRole implements IConvertible
{
    AdminRole();
    AdminRole.fromJson(Map<String, dynamic> json) : super();
    fromMap(Map<String, dynamic> json) {
        return this;
    }

    Map<String, dynamic> toJson() => {};
    getTypeName() => "AdminRole";
    TypeContext? context = _ctx;
}

class ServerStats implements IConvertible
{
    Map<String,int?>? redis;
    Map<String,String?>? serverEvents;
    String? mqDescription;
    Map<String,int?>? mqWorkers;

    ServerStats({this.redis,this.serverEvents,this.mqDescription,this.mqWorkers});
    ServerStats.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        redis = JsonConverters.fromJson(json['redis'],'Map<String,int?>',context!);
        serverEvents = JsonConverters.toStringMap(json['serverEvents']);
        mqDescription = json['mqDescription'];
        mqWorkers = JsonConverters.fromJson(json['mqWorkers'],'Map<String,int?>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'redis': JsonConverters.toJson(redis,'Map<String,int?>',context!),
        'serverEvents': serverEvents,
        'mqDescription': mqDescription,
        'mqWorkers': JsonConverters.toJson(mqWorkers,'Map<String,int?>',context!)
    };

    getTypeName() => "ServerStats";
    TypeContext? context = _ctx;
}

class DiagnosticEntry implements IConvertible
{
    int? id;
    String? traceId;
    String? source;
    String? eventType;
    String? message;
    String? operation;
    int? threadId;
    ResponseStatus? error;
    String? commandType;
    String? command;
    String? userAuthId;
    String? sessionId;
    String? arg;
    List<String>? args;
    List<int>? argLengths;
    Map<String,dynamic?>? namedArgs;
    Duration? duration;
    int? timestamp;
    DateTime? date;
    String? tag;
    String? stackTrace;
    Map<String,String?>? meta = {};

    DiagnosticEntry({this.id,this.traceId,this.source,this.eventType,this.message,this.operation,this.threadId,this.error,this.commandType,this.command,this.userAuthId,this.sessionId,this.arg,this.args,this.argLengths,this.namedArgs,this.duration,this.timestamp,this.date,this.tag,this.stackTrace,this.meta});
    DiagnosticEntry.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        traceId = json['traceId'];
        source = json['source'];
        eventType = json['eventType'];
        message = json['message'];
        operation = json['operation'];
        threadId = json['threadId'];
        error = JsonConverters.fromJson(json['error'],'ResponseStatus',context!);
        commandType = json['commandType'];
        command = json['command'];
        userAuthId = json['userAuthId'];
        sessionId = json['sessionId'];
        arg = json['arg'];
        args = JsonConverters.fromJson(json['args'],'List<String>',context!);
        argLengths = JsonConverters.fromJson(json['argLengths'],'List<int>',context!);
        namedArgs = JsonConverters.fromJson(json['namedArgs'],'Map<String,dynamic?>',context!);
        duration = JsonConverters.fromJson(json['duration'],'Duration',context!);
        timestamp = json['timestamp'];
        date = JsonConverters.fromJson(json['date'],'DateTime',context!);
        tag = json['tag'];
        stackTrace = json['stackTrace'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'traceId': traceId,
        'source': source,
        'eventType': eventType,
        'message': message,
        'operation': operation,
        'threadId': threadId,
        'error': JsonConverters.toJson(error,'ResponseStatus',context!),
        'commandType': commandType,
        'command': command,
        'userAuthId': userAuthId,
        'sessionId': sessionId,
        'arg': arg,
        'args': JsonConverters.toJson(args,'List<String>',context!),
        'argLengths': JsonConverters.toJson(argLengths,'List<int>',context!),
        'namedArgs': JsonConverters.toJson(namedArgs,'Map<String,dynamic?>',context!),
        'duration': JsonConverters.toJson(duration,'Duration',context!),
        'timestamp': timestamp,
        'date': JsonConverters.toJson(date,'DateTime',context!),
        'tag': tag,
        'stackTrace': stackTrace,
        'meta': meta
    };

    getTypeName() => "DiagnosticEntry";
    TypeContext? context = _ctx;
}

class RedisSearchResult implements IConvertible
{
    String? id;
    String? type;
    int? ttl;
    int? size;

    RedisSearchResult({this.id,this.type,this.ttl,this.size});
    RedisSearchResult.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        type = json['type'];
        ttl = json['ttl'];
        size = json['size'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'type': type,
        'ttl': ttl,
        'size': size
    };

    getTypeName() => "RedisSearchResult";
    TypeContext? context = _ctx;
}

class RedisText implements IConvertible
{
    String? text;
    List<RedisText>? children;

    RedisText({this.text,this.children});
    RedisText.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        text = json['text'];
        children = JsonConverters.fromJson(json['children'],'List<RedisText>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'text': text,
        'children': JsonConverters.toJson(children,'List<RedisText>',context!)
    };

    getTypeName() => "RedisText";
    TypeContext? context = _ctx;
}

class CommandSummary implements IConvertible
{
    String? type;
    String? name;
    int? count;
    int? failed;
    int? retries;
    int? totalMs;
    int? minMs;
    int? maxMs;
    double? averageMs;
    double? medianMs;
    ResponseStatus? lastError;
    List<int>? timings;

    CommandSummary({this.type,this.name,this.count,this.failed,this.retries,this.totalMs,this.minMs,this.maxMs,this.averageMs,this.medianMs,this.lastError,this.timings});
    CommandSummary.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        type = json['type'];
        name = json['name'];
        count = json['count'];
        failed = json['failed'];
        retries = json['retries'];
        totalMs = json['totalMs'];
        minMs = json['minMs'];
        maxMs = json['maxMs'];
        averageMs = JsonConverters.toDouble(json['averageMs']);
        medianMs = JsonConverters.toDouble(json['medianMs']);
        lastError = JsonConverters.fromJson(json['lastError'],'ResponseStatus',context!);
        timings = JsonConverters.fromJson(json['timings'],'List<int>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'type': type,
        'name': name,
        'count': count,
        'failed': failed,
        'retries': retries,
        'totalMs': totalMs,
        'minMs': minMs,
        'maxMs': maxMs,
        'averageMs': averageMs,
        'medianMs': medianMs,
        'lastError': JsonConverters.toJson(lastError,'ResponseStatus',context!),
        'timings': JsonConverters.toJson(timings,'List<int>',context!)
    };

    getTypeName() => "CommandSummary";
    TypeContext? context = _ctx;
}

class CommandResult implements IConvertible
{
    String? type;
    String? name;
    int? ms;
    DateTime? at;
    String? request;
    int? retries;
    int? attempt;
    ResponseStatus? error;

    CommandResult({this.type,this.name,this.ms,this.at,this.request,this.retries,this.attempt,this.error});
    CommandResult.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        type = json['type'];
        name = json['name'];
        ms = json['ms'];
        at = JsonConverters.fromJson(json['at'],'DateTime',context!);
        request = json['request'];
        retries = json['retries'];
        attempt = json['attempt'];
        error = JsonConverters.fromJson(json['error'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'type': type,
        'name': name,
        'ms': ms,
        'at': JsonConverters.toJson(at,'DateTime',context!),
        'request': request,
        'retries': retries,
        'attempt': attempt,
        'error': JsonConverters.toJson(error,'ResponseStatus',context!)
    };

    getTypeName() => "CommandResult";
    TypeContext? context = _ctx;
}

// @DataContract
class PartialApiKey implements IConvertible
{
    // @DataMember(Order=1)
    int? id;

    // @DataMember(Order=2)
    String? name;

    // @DataMember(Order=3)
    String? userId;

    // @DataMember(Order=4)
    String? userName;

    // @DataMember(Order=5)
    String? visibleKey;

    // @DataMember(Order=6)
    String? environment;

    // @DataMember(Order=7)
    DateTime? createdDate;

    // @DataMember(Order=8)
    DateTime? expiryDate;

    // @DataMember(Order=9)
    DateTime? cancelledDate;

    // @DataMember(Order=10)
    DateTime? lastUsedDate;

    // @DataMember(Order=11)
    List<String>? scopes;

    // @DataMember(Order=12)
    List<String>? features;

    // @DataMember(Order=13)
    List<String>? restrictTo;

    // @DataMember(Order=14)
    String? notes;

    // @DataMember(Order=15)
    int? refId;

    // @DataMember(Order=16)
    String? refIdStr;

    // @DataMember(Order=17)
    Map<String,String?>? meta;

    // @DataMember(Order=18)
    bool? active;

    PartialApiKey({this.id,this.name,this.userId,this.userName,this.visibleKey,this.environment,this.createdDate,this.expiryDate,this.cancelledDate,this.lastUsedDate,this.scopes,this.features,this.restrictTo,this.notes,this.refId,this.refIdStr,this.meta,this.active});
    PartialApiKey.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        name = json['name'];
        userId = json['userId'];
        userName = json['userName'];
        visibleKey = json['visibleKey'];
        environment = json['environment'];
        createdDate = JsonConverters.fromJson(json['createdDate'],'DateTime',context!);
        expiryDate = JsonConverters.fromJson(json['expiryDate'],'DateTime',context!);
        cancelledDate = JsonConverters.fromJson(json['cancelledDate'],'DateTime',context!);
        lastUsedDate = JsonConverters.fromJson(json['lastUsedDate'],'DateTime',context!);
        scopes = JsonConverters.fromJson(json['scopes'],'List<String>',context!);
        features = JsonConverters.fromJson(json['features'],'List<String>',context!);
        restrictTo = JsonConverters.fromJson(json['restrictTo'],'List<String>',context!);
        notes = json['notes'];
        refId = json['refId'];
        refIdStr = json['refIdStr'];
        meta = JsonConverters.toStringMap(json['meta']);
        active = json['active'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'userId': userId,
        'userName': userName,
        'visibleKey': visibleKey,
        'environment': environment,
        'createdDate': JsonConverters.toJson(createdDate,'DateTime',context!),
        'expiryDate': JsonConverters.toJson(expiryDate,'DateTime',context!),
        'cancelledDate': JsonConverters.toJson(cancelledDate,'DateTime',context!),
        'lastUsedDate': JsonConverters.toJson(lastUsedDate,'DateTime',context!),
        'scopes': JsonConverters.toJson(scopes,'List<String>',context!),
        'features': JsonConverters.toJson(features,'List<String>',context!),
        'restrictTo': JsonConverters.toJson(restrictTo,'List<String>',context!),
        'notes': notes,
        'refId': refId,
        'refIdStr': refIdStr,
        'meta': meta,
        'active': active
    };

    getTypeName() => "PartialApiKey";
    TypeContext? context = _ctx;
}

class JobStatSummary implements IConvertible
{
    String? name;
    int? total;
    int? completed;
    int? retries;
    int? failed;
    int? cancelled;

    JobStatSummary({this.name,this.total,this.completed,this.retries,this.failed,this.cancelled});
    JobStatSummary.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        total = json['total'];
        completed = json['completed'];
        retries = json['retries'];
        failed = json['failed'];
        cancelled = json['cancelled'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'total': total,
        'completed': completed,
        'retries': retries,
        'failed': failed,
        'cancelled': cancelled
    };

    getTypeName() => "JobStatSummary";
    TypeContext? context = _ctx;
}

class HourSummary implements IConvertible
{
    String? hour;
    int? total;
    int? completed;
    int? failed;
    int? cancelled;

    HourSummary({this.hour,this.total,this.completed,this.failed,this.cancelled});
    HourSummary.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        hour = json['hour'];
        total = json['total'];
        completed = json['completed'];
        failed = json['failed'];
        cancelled = json['cancelled'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'hour': hour,
        'total': total,
        'completed': completed,
        'failed': failed,
        'cancelled': cancelled
    };

    getTypeName() => "HourSummary";
    TypeContext? context = _ctx;
}

class WorkerStats implements IConvertible
{
    String? name;
    int? queued;
    int? received;
    int? completed;
    int? retries;
    int? failed;
    int? runningJob;
    Duration? runningTime;

    WorkerStats({this.name,this.queued,this.received,this.completed,this.retries,this.failed,this.runningJob,this.runningTime});
    WorkerStats.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        queued = json['queued'];
        received = json['received'];
        completed = json['completed'];
        retries = json['retries'];
        failed = json['failed'];
        runningJob = json['runningJob'];
        runningTime = JsonConverters.fromJson(json['runningTime'],'Duration',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'queued': queued,
        'received': received,
        'completed': completed,
        'retries': retries,
        'failed': failed,
        'runningJob': runningJob,
        'runningTime': JsonConverters.toJson(runningTime,'Duration',context!)
    };

    getTypeName() => "WorkerStats";
    TypeContext? context = _ctx;
}

class RequestLogEntry implements IConvertible
{
    int? id;
    String? traceId;
    String? operationName;
    DateTime? dateTime;
    int? statusCode;
    String? statusDescription;
    String? httpMethod;
    String? absoluteUri;
    String? pathInfo;
    // @StringLength(2147483647)
    String? requestBody;

    dynamic? requestDto;
    String? userAuthId;
    String? sessionId;
    String? ipAddress;
    String? forwardedFor;
    String? referer;
    Map<String,String?>? headers;
    Map<String,String?>? formData;
    Map<String,String?>? items;
    Map<String,String?>? responseHeaders;
    dynamic? session;
    dynamic? responseDto;
    dynamic? errorResponse;
    String? exceptionSource;
    dynamic? exceptionData;
    Duration? requestDuration;
    Map<String,String?>? meta;

    RequestLogEntry({this.id,this.traceId,this.operationName,this.dateTime,this.statusCode,this.statusDescription,this.httpMethod,this.absoluteUri,this.pathInfo,this.requestBody,this.requestDto,this.userAuthId,this.sessionId,this.ipAddress,this.forwardedFor,this.referer,this.headers,this.formData,this.items,this.responseHeaders,this.session,this.responseDto,this.errorResponse,this.exceptionSource,this.exceptionData,this.requestDuration,this.meta});
    RequestLogEntry.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        traceId = json['traceId'];
        operationName = json['operationName'];
        dateTime = JsonConverters.fromJson(json['dateTime'],'DateTime',context!);
        statusCode = json['statusCode'];
        statusDescription = json['statusDescription'];
        httpMethod = json['httpMethod'];
        absoluteUri = json['absoluteUri'];
        pathInfo = json['pathInfo'];
        requestBody = json['requestBody'];
        requestDto = JsonConverters.fromJson(json['requestDto'],'dynamic',context!);
        userAuthId = json['userAuthId'];
        sessionId = json['sessionId'];
        ipAddress = json['ipAddress'];
        forwardedFor = json['forwardedFor'];
        referer = json['referer'];
        headers = JsonConverters.toStringMap(json['headers']);
        formData = JsonConverters.toStringMap(json['formData']);
        items = JsonConverters.toStringMap(json['items']);
        responseHeaders = JsonConverters.toStringMap(json['responseHeaders']);
        session = JsonConverters.fromJson(json['session'],'dynamic',context!);
        responseDto = JsonConverters.fromJson(json['responseDto'],'dynamic',context!);
        errorResponse = JsonConverters.fromJson(json['errorResponse'],'dynamic',context!);
        exceptionSource = json['exceptionSource'];
        exceptionData = JsonConverters.fromJson(json['exceptionData'],'dynamic',context!);
        requestDuration = JsonConverters.fromJson(json['requestDuration'],'Duration',context!);
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'traceId': traceId,
        'operationName': operationName,
        'dateTime': JsonConverters.toJson(dateTime,'DateTime',context!),
        'statusCode': statusCode,
        'statusDescription': statusDescription,
        'httpMethod': httpMethod,
        'absoluteUri': absoluteUri,
        'pathInfo': pathInfo,
        'requestBody': requestBody,
        'requestDto': JsonConverters.toJson(requestDto,'dynamic',context!),
        'userAuthId': userAuthId,
        'sessionId': sessionId,
        'ipAddress': ipAddress,
        'forwardedFor': forwardedFor,
        'referer': referer,
        'headers': headers,
        'formData': formData,
        'items': items,
        'responseHeaders': responseHeaders,
        'session': JsonConverters.toJson(session,'dynamic',context!),
        'responseDto': JsonConverters.toJson(responseDto,'dynamic',context!),
        'errorResponse': JsonConverters.toJson(errorResponse,'dynamic',context!),
        'exceptionSource': exceptionSource,
        'exceptionData': JsonConverters.toJson(exceptionData,'dynamic',context!),
        'requestDuration': JsonConverters.toJson(requestDuration,'Duration',context!),
        'meta': meta
    };

    getTypeName() => "RequestLogEntry";
    TypeContext? context = _ctx;
}

// @DataContract
class RequestSummary implements IConvertible
{
    // @DataMember(Order=1)
    String? name;

    // @DataMember(Order=2)
    int? requests;

    // @DataMember(Order=3)
    int? requestLength;

    // @DataMember(Order=4)
    double? duration;

    RequestSummary({this.name,this.requests,this.requestLength,this.duration});
    RequestSummary.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        requests = json['requests'];
        requestLength = json['requestLength'];
        duration = JsonConverters.toDouble(json['duration']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'requests': requests,
        'requestLength': requestLength,
        'duration': duration
    };

    getTypeName() => "RequestSummary";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminGetRolesResponse implements IConvertible
{
    // @DataMember(Order=1)
    List<AdminRole>? results;

    // @DataMember(Order=2)
    ResponseStatus? responseStatus;

    AdminGetRolesResponse({this.results,this.responseStatus});
    AdminGetRolesResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<AdminRole>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<AdminRole>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminGetRolesResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminGetRoleResponse implements IConvertible
{
    // @DataMember(Order=1)
    AdminRole? result;

    // @DataMember(Order=2)
    List<Property>? claims;

    // @DataMember(Order=3)
    ResponseStatus? responseStatus;

    AdminGetRoleResponse({this.result,this.claims,this.responseStatus});
    AdminGetRoleResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        result = JsonConverters.fromJson(json['result'],'AdminRole',context!);
        claims = JsonConverters.fromJson(json['claims'],'List<Property>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'result': JsonConverters.toJson(result,'AdminRole',context!),
        'claims': JsonConverters.toJson(claims,'List<Property>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminGetRoleResponse";
    TypeContext? context = _ctx;
}

class AdminDashboardResponse implements IConvertible
{
    ServerStats? serverStats;
    ResponseStatus? responseStatus;

    AdminDashboardResponse({this.serverStats,this.responseStatus});
    AdminDashboardResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        serverStats = JsonConverters.fromJson(json['serverStats'],'ServerStats',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'serverStats': JsonConverters.toJson(serverStats,'ServerStats',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminDashboardResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminUserResponse implements IConvertible
{
    // @DataMember(Order=1)
    String? id;

    // @DataMember(Order=2)
    Map<String,dynamic?>? result;

    // @DataMember(Order=3)
    List<Map<String,dynamic>>? details;

    // @DataMember(Order=4)
    List<Property>? claims;

    // @DataMember(Order=5)
    ResponseStatus? responseStatus;

    AdminUserResponse({this.id,this.result,this.details,this.claims,this.responseStatus});
    AdminUserResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        result = JsonConverters.fromJson(json['result'],'Map<String,dynamic?>',context!);
        details = JsonConverters.fromJson(json['details'],'List<Map<String,dynamic>>',context!);
        claims = JsonConverters.fromJson(json['claims'],'List<Property>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'result': JsonConverters.toJson(result,'Map<String,dynamic?>',context!),
        'details': JsonConverters.toJson(details,'List<Map<String,dynamic>>',context!),
        'claims': JsonConverters.toJson(claims,'List<Property>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminUserResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminUsersResponse implements IConvertible
{
    // @DataMember(Order=1)
    List<Map<String,dynamic>>? results;

    // @DataMember(Order=2)
    ResponseStatus? responseStatus;

    AdminUsersResponse({this.results,this.responseStatus});
    AdminUsersResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<Map<String,dynamic>>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<Map<String,dynamic>>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminUsersResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminDeleteUserResponse implements IConvertible
{
    // @DataMember(Order=1)
    String? id;

    // @DataMember(Order=2)
    ResponseStatus? responseStatus;

    AdminDeleteUserResponse({this.id,this.responseStatus});
    AdminDeleteUserResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminDeleteUserResponse";
    TypeContext? context = _ctx;
}

class AdminProfilingResponse implements IConvertible
{
    List<DiagnosticEntry>? results = [];
    int? total;
    ResponseStatus? responseStatus;

    AdminProfilingResponse({this.results,this.total,this.responseStatus});
    AdminProfilingResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<DiagnosticEntry>',context!);
        total = json['total'];
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<DiagnosticEntry>',context!),
        'total': total,
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminProfilingResponse";
    TypeContext? context = _ctx;
}

class AdminRedisResponse implements IConvertible
{
    int? db;
    List<RedisSearchResult>? searchResults;
    Map<String,String?>? info;
    RedisEndpointInfo? endpoint;
    RedisText? result;
    ResponseStatus? responseStatus;

    AdminRedisResponse({this.db,this.searchResults,this.info,this.endpoint,this.result,this.responseStatus});
    AdminRedisResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        db = json['db'];
        searchResults = JsonConverters.fromJson(json['searchResults'],'List<RedisSearchResult>',context!);
        info = JsonConverters.toStringMap(json['info']);
        endpoint = JsonConverters.fromJson(json['endpoint'],'RedisEndpointInfo',context!);
        result = JsonConverters.fromJson(json['result'],'RedisText',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'db': db,
        'searchResults': JsonConverters.toJson(searchResults,'List<RedisSearchResult>',context!),
        'info': info,
        'endpoint': JsonConverters.toJson(endpoint,'RedisEndpointInfo',context!),
        'result': JsonConverters.toJson(result,'RedisText',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminRedisResponse";
    TypeContext? context = _ctx;
}

class AdminDatabaseResponse implements IConvertible
{
    List<Map<String,dynamic>>? results = [];
    int? total;
    List<MetadataPropertyType>? columns;
    ResponseStatus? responseStatus;

    AdminDatabaseResponse({this.results,this.total,this.columns,this.responseStatus});
    AdminDatabaseResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<Map<String,dynamic>>',context!);
        total = json['total'];
        columns = JsonConverters.fromJson(json['columns'],'List<MetadataPropertyType>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<Map<String,dynamic>>',context!),
        'total': total,
        'columns': JsonConverters.toJson(columns,'List<MetadataPropertyType>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminDatabaseResponse";
    TypeContext? context = _ctx;
}

class ViewCommandsResponse implements IConvertible
{
    List<CommandSummary>? commandTotals = [];
    List<CommandResult>? latestCommands = [];
    List<CommandResult>? latestFailed = [];
    ResponseStatus? responseStatus;

    ViewCommandsResponse({this.commandTotals,this.latestCommands,this.latestFailed,this.responseStatus});
    ViewCommandsResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        commandTotals = JsonConverters.fromJson(json['commandTotals'],'List<CommandSummary>',context!);
        latestCommands = JsonConverters.fromJson(json['latestCommands'],'List<CommandResult>',context!);
        latestFailed = JsonConverters.fromJson(json['latestFailed'],'List<CommandResult>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'commandTotals': JsonConverters.toJson(commandTotals,'List<CommandSummary>',context!),
        'latestCommands': JsonConverters.toJson(latestCommands,'List<CommandResult>',context!),
        'latestFailed': JsonConverters.toJson(latestFailed,'List<CommandResult>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "ViewCommandsResponse";
    TypeContext? context = _ctx;
}

class ExecuteCommandResponse implements IConvertible
{
    CommandResult? commandResult;
    String? result;
    ResponseStatus? responseStatus;

    ExecuteCommandResponse({this.commandResult,this.result,this.responseStatus});
    ExecuteCommandResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        commandResult = JsonConverters.fromJson(json['commandResult'],'CommandResult',context!);
        result = json['result'];
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'commandResult': JsonConverters.toJson(commandResult,'CommandResult',context!),
        'result': result,
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "ExecuteCommandResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminApiKeysResponse implements IConvertible
{
    // @DataMember(Order=1)
    List<PartialApiKey>? results;

    // @DataMember(Order=2)
    ResponseStatus? responseStatus;

    AdminApiKeysResponse({this.results,this.responseStatus});
    AdminApiKeysResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<PartialApiKey>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<PartialApiKey>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminApiKeysResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminApiKeyResponse implements IConvertible
{
    // @DataMember(Order=1)
    String? result;

    // @DataMember(Order=2)
    ResponseStatus? responseStatus;

    AdminApiKeyResponse({this.result,this.responseStatus});
    AdminApiKeyResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        result = json['result'];
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'result': result,
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminApiKeyResponse";
    TypeContext? context = _ctx;
}

class AdminJobDashboardResponse implements IConvertible
{
    List<JobStatSummary>? commands = [];
    List<JobStatSummary>? apis = [];
    List<JobStatSummary>? workers = [];
    List<HourSummary>? today = [];
    ResponseStatus? responseStatus;

    AdminJobDashboardResponse({this.commands,this.apis,this.workers,this.today,this.responseStatus});
    AdminJobDashboardResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        commands = JsonConverters.fromJson(json['commands'],'List<JobStatSummary>',context!);
        apis = JsonConverters.fromJson(json['apis'],'List<JobStatSummary>',context!);
        workers = JsonConverters.fromJson(json['workers'],'List<JobStatSummary>',context!);
        today = JsonConverters.fromJson(json['today'],'List<HourSummary>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'commands': JsonConverters.toJson(commands,'List<JobStatSummary>',context!),
        'apis': JsonConverters.toJson(apis,'List<JobStatSummary>',context!),
        'workers': JsonConverters.toJson(workers,'List<JobStatSummary>',context!),
        'today': JsonConverters.toJson(today,'List<HourSummary>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminJobDashboardResponse";
    TypeContext? context = _ctx;
}

class AdminJobInfoResponse implements IConvertible
{
    List<DateTime>? monthDbs = [];
    Map<String,int?>? tableCounts = {};
    List<WorkerStats>? workerStats = [];
    Map<String,int?>? queueCounts = {};
    Map<String,int?>? workerCounts = {};
    Map<BackgroundJobState,int?>? stateCounts = {};
    ResponseStatus? responseStatus;

    AdminJobInfoResponse({this.monthDbs,this.tableCounts,this.workerStats,this.queueCounts,this.workerCounts,this.stateCounts,this.responseStatus});
    AdminJobInfoResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        monthDbs = JsonConverters.fromJson(json['monthDbs'],'List<DateTime>',context!);
        tableCounts = JsonConverters.fromJson(json['tableCounts'],'Map<String,int?>',context!);
        workerStats = JsonConverters.fromJson(json['workerStats'],'List<WorkerStats>',context!);
        queueCounts = JsonConverters.fromJson(json['queueCounts'],'Map<String,int?>',context!);
        workerCounts = JsonConverters.fromJson(json['workerCounts'],'Map<String,int?>',context!);
        stateCounts = JsonConverters.fromJson(json['stateCounts'],'Map<BackgroundJobState,int?>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'monthDbs': JsonConverters.toJson(monthDbs,'List<DateTime>',context!),
        'tableCounts': JsonConverters.toJson(tableCounts,'Map<String,int?>',context!),
        'workerStats': JsonConverters.toJson(workerStats,'List<WorkerStats>',context!),
        'queueCounts': JsonConverters.toJson(queueCounts,'Map<String,int?>',context!),
        'workerCounts': JsonConverters.toJson(workerCounts,'Map<String,int?>',context!),
        'stateCounts': JsonConverters.toJson(stateCounts,'Map<BackgroundJobState,int?>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminJobInfoResponse";
    TypeContext? context = _ctx;
}

class AdminGetJobResponse implements IConvertible
{
    JobSummary? result;
    BackgroundJob? queued;
    CompletedJob? completed;
    FailedJob? failed;
    ResponseStatus? responseStatus;

    AdminGetJobResponse({this.result,this.queued,this.completed,this.failed,this.responseStatus});
    AdminGetJobResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        result = JsonConverters.fromJson(json['result'],'JobSummary',context!);
        queued = JsonConverters.fromJson(json['queued'],'BackgroundJob',context!);
        completed = JsonConverters.fromJson(json['completed'],'CompletedJob',context!);
        failed = JsonConverters.fromJson(json['failed'],'FailedJob',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'result': JsonConverters.toJson(result,'JobSummary',context!),
        'queued': JsonConverters.toJson(queued,'BackgroundJob',context!),
        'completed': JsonConverters.toJson(completed,'CompletedJob',context!),
        'failed': JsonConverters.toJson(failed,'FailedJob',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminGetJobResponse";
    TypeContext? context = _ctx;
}

class AdminGetJobProgressResponse implements IConvertible
{
    BackgroundJobState? state;
    double? progress;
    String? status;
    String? logs;
    int? durationMs;
    ResponseStatus? error;
    ResponseStatus? responseStatus;

    AdminGetJobProgressResponse({this.state,this.progress,this.status,this.logs,this.durationMs,this.error,this.responseStatus});
    AdminGetJobProgressResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        state = JsonConverters.fromJson(json['state'],'BackgroundJobState',context!);
        progress = JsonConverters.toDouble(json['progress']);
        status = json['status'];
        logs = json['logs'];
        durationMs = json['durationMs'];
        error = JsonConverters.fromJson(json['error'],'ResponseStatus',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'state': JsonConverters.toJson(state,'BackgroundJobState',context!),
        'progress': progress,
        'status': status,
        'logs': logs,
        'durationMs': durationMs,
        'error': JsonConverters.toJson(error,'ResponseStatus',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminGetJobProgressResponse";
    TypeContext? context = _ctx;
}

class AdminRequeueFailedJobsJobsResponse implements IConvertible
{
    List<int>? results = [];
    Map<int,String?>? errors = {};
    ResponseStatus? responseStatus;

    AdminRequeueFailedJobsJobsResponse({this.results,this.errors,this.responseStatus});
    AdminRequeueFailedJobsJobsResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<int>',context!);
        errors = JsonConverters.fromJson(json['errors'],'Map<int,String?>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<int>',context!),
        'errors': JsonConverters.toJson(errors,'Map<int,String?>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminRequeueFailedJobsJobsResponse";
    TypeContext? context = _ctx;
}

class AdminCancelJobsResponse implements IConvertible
{
    List<int>? results = [];
    Map<int,String?>? errors = {};
    ResponseStatus? responseStatus;

    AdminCancelJobsResponse({this.results,this.errors,this.responseStatus});
    AdminCancelJobsResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<int>',context!);
        errors = JsonConverters.fromJson(json['errors'],'Map<int,String?>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<int>',context!),
        'errors': JsonConverters.toJson(errors,'Map<int,String?>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "AdminCancelJobsResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class RequestLogsResponse implements IConvertible
{
    // @DataMember(Order=1)
    List<RequestLogEntry>? results;

    // @DataMember(Order=2)
    Map<String,String?>? usage;

    // @DataMember(Order=3)
    int? total;

    // @DataMember(Order=4)
    ResponseStatus? responseStatus;

    RequestLogsResponse({this.results,this.usage,this.total,this.responseStatus});
    RequestLogsResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<RequestLogEntry>',context!);
        usage = JsonConverters.toStringMap(json['usage']);
        total = json['total'];
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<RequestLogEntry>',context!),
        'usage': usage,
        'total': total,
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "RequestLogsResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AnalyticsReports implements IConvertible
{
    // @DataMember(Order=1)
    Map<String,RequestSummary?>? apis;

    // @DataMember(Order=2)
    Map<String,RequestSummary?>? users;

    // @DataMember(Order=3)
    Map<String,RequestSummary?>? tags;

    // @DataMember(Order=4)
    Map<String,RequestSummary?>? status;

    // @DataMember(Order=5)
    Map<String,RequestSummary?>? days;

    // @DataMember(Order=6)
    Map<String,RequestSummary?>? apiKeys;

    // @DataMember(Order=7)
    Map<String,RequestSummary?>? ipAddresses;

    // @DataMember(Order=8)
    Map<String,int?>? durationRange;

    AnalyticsReports({this.apis,this.users,this.tags,this.status,this.days,this.apiKeys,this.ipAddresses,this.durationRange});
    AnalyticsReports.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        apis = JsonConverters.fromJson(json['apis'],'Map<String,RequestSummary?>',context!);
        users = JsonConverters.fromJson(json['users'],'Map<String,RequestSummary?>',context!);
        tags = JsonConverters.fromJson(json['tags'],'Map<String,RequestSummary?>',context!);
        status = JsonConverters.fromJson(json['status'],'Map<String,RequestSummary?>',context!);
        days = JsonConverters.fromJson(json['days'],'Map<String,RequestSummary?>',context!);
        apiKeys = JsonConverters.fromJson(json['apiKeys'],'Map<String,RequestSummary?>',context!);
        ipAddresses = JsonConverters.fromJson(json['ipAddresses'],'Map<String,RequestSummary?>',context!);
        durationRange = JsonConverters.fromJson(json['durationRange'],'Map<String,int?>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'apis': JsonConverters.toJson(apis,'Map<String,RequestSummary?>',context!),
        'users': JsonConverters.toJson(users,'Map<String,RequestSummary?>',context!),
        'tags': JsonConverters.toJson(tags,'Map<String,RequestSummary?>',context!),
        'status': JsonConverters.toJson(status,'Map<String,RequestSummary?>',context!),
        'days': JsonConverters.toJson(days,'Map<String,RequestSummary?>',context!),
        'apiKeys': JsonConverters.toJson(apiKeys,'Map<String,RequestSummary?>',context!),
        'ipAddresses': JsonConverters.toJson(ipAddresses,'Map<String,RequestSummary?>',context!),
        'durationRange': JsonConverters.toJson(durationRange,'Map<String,int?>',context!)
    };

    getTypeName() => "AnalyticsReports";
    TypeContext? context = _ctx;
}

// @DataContract
class GetValidationRulesResponse implements IConvertible
{
    // @DataMember(Order=1)
    List<ValidationRule>? results;

    // @DataMember(Order=2)
    ResponseStatus? responseStatus;

    GetValidationRulesResponse({this.results,this.responseStatus});
    GetValidationRulesResponse.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        results = JsonConverters.fromJson(json['results'],'List<ValidationRule>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'results': JsonConverters.toJson(results,'List<ValidationRule>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    getTypeName() => "GetValidationRulesResponse";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminCreateRole implements IReturn<IdResponse>, IPost, IConvertible
{
    // @DataMember(Order=1)
    String? name;

    AdminCreateRole({this.name});
    AdminCreateRole.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name
    };

    createResponse() => IdResponse();
    getResponseTypeName() => "IdResponse";
    getTypeName() => "AdminCreateRole";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminGetRoles implements IReturn<AdminGetRolesResponse>, IGet, IConvertible
{
    AdminGetRoles();
    AdminGetRoles.fromJson(Map<String, dynamic> json) : super();
    fromMap(Map<String, dynamic> json) {
        return this;
    }

    Map<String, dynamic> toJson() => {};
    createResponse() => AdminGetRolesResponse();
    getResponseTypeName() => "AdminGetRolesResponse";
    getTypeName() => "AdminGetRoles";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminGetRole implements IReturn<AdminGetRoleResponse>, IGet, IConvertible
{
    // @DataMember(Order=1)
    String? id;

    AdminGetRole({this.id});
    AdminGetRole.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id
    };

    createResponse() => AdminGetRoleResponse();
    getResponseTypeName() => "AdminGetRoleResponse";
    getTypeName() => "AdminGetRole";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminUpdateRole implements IReturn<IdResponse>, IPost, IConvertible
{
    // @DataMember(Order=1)
    String? id;

    // @DataMember(Order=2)
    String? name;

    // @DataMember(Order=3)
    List<Property>? addClaims;

    // @DataMember(Order=4)
    List<Property>? removeClaims;

    // @DataMember(Order=5)
    ResponseStatus? responseStatus;

    AdminUpdateRole({this.id,this.name,this.addClaims,this.removeClaims,this.responseStatus});
    AdminUpdateRole.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        name = json['name'];
        addClaims = JsonConverters.fromJson(json['addClaims'],'List<Property>',context!);
        removeClaims = JsonConverters.fromJson(json['removeClaims'],'List<Property>',context!);
        responseStatus = JsonConverters.fromJson(json['responseStatus'],'ResponseStatus',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'addClaims': JsonConverters.toJson(addClaims,'List<Property>',context!),
        'removeClaims': JsonConverters.toJson(removeClaims,'List<Property>',context!),
        'responseStatus': JsonConverters.toJson(responseStatus,'ResponseStatus',context!)
    };

    createResponse() => IdResponse();
    getResponseTypeName() => "IdResponse";
    getTypeName() => "AdminUpdateRole";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminDeleteRole implements IReturnVoid, IDelete, IConvertible
{
    // @DataMember(Order=1)
    String? id;

    AdminDeleteRole({this.id});
    AdminDeleteRole.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id
    };

    createResponse() {}
    getTypeName() => "AdminDeleteRole";
    TypeContext? context = _ctx;
}

class AdminDashboard implements IReturn<AdminDashboardResponse>, IGet, IConvertible
{
    AdminDashboard();
    AdminDashboard.fromJson(Map<String, dynamic> json) : super();
    fromMap(Map<String, dynamic> json) {
        return this;
    }

    Map<String, dynamic> toJson() => {};
    createResponse() => AdminDashboardResponse();
    getResponseTypeName() => "AdminDashboardResponse";
    getTypeName() => "AdminDashboard";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminGetUser implements IReturn<AdminUserResponse>, IGet, IConvertible
{
    // @DataMember(Order=10)
    String? id;

    AdminGetUser({this.id});
    AdminGetUser.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id
    };

    createResponse() => AdminUserResponse();
    getResponseTypeName() => "AdminUserResponse";
    getTypeName() => "AdminGetUser";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminQueryUsers implements IReturn<AdminUsersResponse>, IGet, IConvertible
{
    // @DataMember(Order=1)
    String? query;

    // @DataMember(Order=2)
    String? orderBy;

    // @DataMember(Order=3)
    int? skip;

    // @DataMember(Order=4)
    int? take;

    AdminQueryUsers({this.query,this.orderBy,this.skip,this.take});
    AdminQueryUsers.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        query = json['query'];
        orderBy = json['orderBy'];
        skip = json['skip'];
        take = json['take'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'query': query,
        'orderBy': orderBy,
        'skip': skip,
        'take': take
    };

    createResponse() => AdminUsersResponse();
    getResponseTypeName() => "AdminUsersResponse";
    getTypeName() => "AdminQueryUsers";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminCreateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPost, IConvertible
{
    // @DataMember(Order=10)
    List<String>? roles;

    // @DataMember(Order=11)
    List<String>? permissions;

    AdminCreateUser({this.roles,this.permissions});
    AdminCreateUser.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        roles = JsonConverters.fromJson(json['roles'],'List<String>',context!);
        permissions = JsonConverters.fromJson(json['permissions'],'List<String>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'roles': JsonConverters.toJson(roles,'List<String>',context!),
        'permissions': JsonConverters.toJson(permissions,'List<String>',context!)
    });

    createResponse() => AdminUserResponse();
    getResponseTypeName() => "AdminUserResponse";
    getTypeName() => "AdminCreateUser";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminUpdateUser extends AdminUserBase implements IReturn<AdminUserResponse>, IPut, IConvertible
{
    // @DataMember(Order=10)
    String? id;

    // @DataMember(Order=11)
    bool? lockUser;

    // @DataMember(Order=12)
    bool? unlockUser;

    // @DataMember(Order=13)
    DateTime? lockUserUntil;

    // @DataMember(Order=14)
    List<String>? addRoles;

    // @DataMember(Order=15)
    List<String>? removeRoles;

    // @DataMember(Order=16)
    List<String>? addPermissions;

    // @DataMember(Order=17)
    List<String>? removePermissions;

    // @DataMember(Order=18)
    List<Property>? addClaims;

    // @DataMember(Order=19)
    List<Property>? removeClaims;

    AdminUpdateUser({this.id,this.lockUser,this.unlockUser,this.lockUserUntil,this.addRoles,this.removeRoles,this.addPermissions,this.removePermissions,this.addClaims,this.removeClaims});
    AdminUpdateUser.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        id = json['id'];
        lockUser = json['lockUser'];
        unlockUser = json['unlockUser'];
        lockUserUntil = JsonConverters.fromJson(json['lockUserUntil'],'DateTime',context!);
        addRoles = JsonConverters.fromJson(json['addRoles'],'List<String>',context!);
        removeRoles = JsonConverters.fromJson(json['removeRoles'],'List<String>',context!);
        addPermissions = JsonConverters.fromJson(json['addPermissions'],'List<String>',context!);
        removePermissions = JsonConverters.fromJson(json['removePermissions'],'List<String>',context!);
        addClaims = JsonConverters.fromJson(json['addClaims'],'List<Property>',context!);
        removeClaims = JsonConverters.fromJson(json['removeClaims'],'List<Property>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'id': id,
        'lockUser': lockUser,
        'unlockUser': unlockUser,
        'lockUserUntil': JsonConverters.toJson(lockUserUntil,'DateTime',context!),
        'addRoles': JsonConverters.toJson(addRoles,'List<String>',context!),
        'removeRoles': JsonConverters.toJson(removeRoles,'List<String>',context!),
        'addPermissions': JsonConverters.toJson(addPermissions,'List<String>',context!),
        'removePermissions': JsonConverters.toJson(removePermissions,'List<String>',context!),
        'addClaims': JsonConverters.toJson(addClaims,'List<Property>',context!),
        'removeClaims': JsonConverters.toJson(removeClaims,'List<Property>',context!)
    });

    createResponse() => AdminUserResponse();
    getResponseTypeName() => "AdminUserResponse";
    getTypeName() => "AdminUpdateUser";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminDeleteUser implements IReturn<AdminDeleteUserResponse>, IDelete, IConvertible
{
    // @DataMember(Order=10)
    String? id;

    AdminDeleteUser({this.id});
    AdminDeleteUser.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id
    };

    createResponse() => AdminDeleteUserResponse();
    getResponseTypeName() => "AdminDeleteUserResponse";
    getTypeName() => "AdminDeleteUser";
    TypeContext? context = _ctx;
}

class AdminQueryRequestLogs extends QueryDb<RequestLog> implements IReturn<QueryResponse<RequestLog>>, IConvertible, IGet
{
    DateTime? month;

    AdminQueryRequestLogs({this.month});
    AdminQueryRequestLogs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        month = JsonConverters.fromJson(json['month'],'DateTime',context!);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'month': JsonConverters.toJson(month,'DateTime',context!)
    });

    createResponse() => QueryResponse<RequestLog>();
    getResponseTypeName() => "QueryResponse<RequestLog>";
    getTypeName() => "AdminQueryRequestLogs";
    TypeContext? context = _ctx;
}

class AdminProfiling implements IReturn<AdminProfilingResponse>, IConvertible, IPost
{
    String? source;
    String? eventType;
    int? threadId;
    String? traceId;
    String? userAuthId;
    String? sessionId;
    String? tag;
    int? skip;
    int? take;
    String? orderBy;
    bool? withErrors;
    bool? pending;

    AdminProfiling({this.source,this.eventType,this.threadId,this.traceId,this.userAuthId,this.sessionId,this.tag,this.skip,this.take,this.orderBy,this.withErrors,this.pending});
    AdminProfiling.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        source = json['source'];
        eventType = json['eventType'];
        threadId = json['threadId'];
        traceId = json['traceId'];
        userAuthId = json['userAuthId'];
        sessionId = json['sessionId'];
        tag = json['tag'];
        skip = json['skip'];
        take = json['take'];
        orderBy = json['orderBy'];
        withErrors = json['withErrors'];
        pending = json['pending'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'source': source,
        'eventType': eventType,
        'threadId': threadId,
        'traceId': traceId,
        'userAuthId': userAuthId,
        'sessionId': sessionId,
        'tag': tag,
        'skip': skip,
        'take': take,
        'orderBy': orderBy,
        'withErrors': withErrors,
        'pending': pending
    };

    createResponse() => AdminProfilingResponse();
    getResponseTypeName() => "AdminProfilingResponse";
    getTypeName() => "AdminProfiling";
    TypeContext? context = _ctx;
}

class AdminRedis implements IReturn<AdminRedisResponse>, IPost, IConvertible
{
    int? db;
    String? query;
    RedisEndpointInfo? reconnect;
    int? take;
    int? position;
    List<String>? args;

    AdminRedis({this.db,this.query,this.reconnect,this.take,this.position,this.args});
    AdminRedis.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        db = json['db'];
        query = json['query'];
        reconnect = JsonConverters.fromJson(json['reconnect'],'RedisEndpointInfo',context!);
        take = json['take'];
        position = json['position'];
        args = JsonConverters.fromJson(json['args'],'List<String>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'db': db,
        'query': query,
        'reconnect': JsonConverters.toJson(reconnect,'RedisEndpointInfo',context!),
        'take': take,
        'position': position,
        'args': JsonConverters.toJson(args,'List<String>',context!)
    };

    createResponse() => AdminRedisResponse();
    getResponseTypeName() => "AdminRedisResponse";
    getTypeName() => "AdminRedis";
    TypeContext? context = _ctx;
}

class AdminDatabase implements IReturn<AdminDatabaseResponse>, IGet, IConvertible
{
    String? db;
    String? schema;
    String? table;
    List<String>? fields;
    int? take;
    int? skip;
    String? orderBy;
    String? include;

    AdminDatabase({this.db,this.schema,this.table,this.fields,this.take,this.skip,this.orderBy,this.include});
    AdminDatabase.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        db = json['db'];
        schema = json['schema'];
        table = json['table'];
        fields = JsonConverters.fromJson(json['fields'],'List<String>',context!);
        take = json['take'];
        skip = json['skip'];
        orderBy = json['orderBy'];
        include = json['include'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'db': db,
        'schema': schema,
        'table': table,
        'fields': JsonConverters.toJson(fields,'List<String>',context!),
        'take': take,
        'skip': skip,
        'orderBy': orderBy,
        'include': include
    };

    createResponse() => AdminDatabaseResponse();
    getResponseTypeName() => "AdminDatabaseResponse";
    getTypeName() => "AdminDatabase";
    TypeContext? context = _ctx;
}

class ViewCommands implements IReturn<ViewCommandsResponse>, IGet, IConvertible
{
    List<String>? include;
    int? skip;
    int? take;

    ViewCommands({this.include,this.skip,this.take});
    ViewCommands.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        include = JsonConverters.fromJson(json['include'],'List<String>',context!);
        skip = json['skip'];
        take = json['take'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'include': JsonConverters.toJson(include,'List<String>',context!),
        'skip': skip,
        'take': take
    };

    createResponse() => ViewCommandsResponse();
    getResponseTypeName() => "ViewCommandsResponse";
    getTypeName() => "ViewCommands";
    TypeContext? context = _ctx;
}

class ExecuteCommand implements IReturn<ExecuteCommandResponse>, IPost, IConvertible
{
    String? command;
    String? requestJson;

    ExecuteCommand({this.command,this.requestJson});
    ExecuteCommand.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        command = json['command'];
        requestJson = json['requestJson'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'command': command,
        'requestJson': requestJson
    };

    createResponse() => ExecuteCommandResponse();
    getResponseTypeName() => "ExecuteCommandResponse";
    getTypeName() => "ExecuteCommand";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminQueryApiKeys implements IReturn<AdminApiKeysResponse>, IGet, IConvertible
{
    // @DataMember(Order=1)
    int? id;

    // @DataMember(Order=2)
    String? search;

    // @DataMember(Order=3)
    String? userId;

    // @DataMember(Order=4)
    String? userName;

    // @DataMember(Order=5)
    String? orderBy;

    // @DataMember(Order=6)
    int? skip;

    // @DataMember(Order=7)
    int? take;

    AdminQueryApiKeys({this.id,this.search,this.userId,this.userName,this.orderBy,this.skip,this.take});
    AdminQueryApiKeys.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        search = json['search'];
        userId = json['userId'];
        userName = json['userName'];
        orderBy = json['orderBy'];
        skip = json['skip'];
        take = json['take'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'search': search,
        'userId': userId,
        'userName': userName,
        'orderBy': orderBy,
        'skip': skip,
        'take': take
    };

    createResponse() => AdminApiKeysResponse();
    getResponseTypeName() => "AdminApiKeysResponse";
    getTypeName() => "AdminQueryApiKeys";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminCreateApiKey implements IReturn<AdminApiKeyResponse>, IPost, IConvertible
{
    // @DataMember(Order=1)
    String? name;

    // @DataMember(Order=2)
    String? userId;

    // @DataMember(Order=3)
    String? userName;

    // @DataMember(Order=4)
    List<String>? scopes;

    // @DataMember(Order=5)
    List<String>? features;

    // @DataMember(Order=6)
    List<String>? restrictTo;

    // @DataMember(Order=7)
    DateTime? expiryDate;

    // @DataMember(Order=8)
    String? notes;

    // @DataMember(Order=9)
    int? refId;

    // @DataMember(Order=10)
    String? refIdStr;

    // @DataMember(Order=11)
    Map<String,String?>? meta;

    AdminCreateApiKey({this.name,this.userId,this.userName,this.scopes,this.features,this.restrictTo,this.expiryDate,this.notes,this.refId,this.refIdStr,this.meta});
    AdminCreateApiKey.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        name = json['name'];
        userId = json['userId'];
        userName = json['userName'];
        scopes = JsonConverters.fromJson(json['scopes'],'List<String>',context!);
        features = JsonConverters.fromJson(json['features'],'List<String>',context!);
        restrictTo = JsonConverters.fromJson(json['restrictTo'],'List<String>',context!);
        expiryDate = JsonConverters.fromJson(json['expiryDate'],'DateTime',context!);
        notes = json['notes'];
        refId = json['refId'];
        refIdStr = json['refIdStr'];
        meta = JsonConverters.toStringMap(json['meta']);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'name': name,
        'userId': userId,
        'userName': userName,
        'scopes': JsonConverters.toJson(scopes,'List<String>',context!),
        'features': JsonConverters.toJson(features,'List<String>',context!),
        'restrictTo': JsonConverters.toJson(restrictTo,'List<String>',context!),
        'expiryDate': JsonConverters.toJson(expiryDate,'DateTime',context!),
        'notes': notes,
        'refId': refId,
        'refIdStr': refIdStr,
        'meta': meta
    };

    createResponse() => AdminApiKeyResponse();
    getResponseTypeName() => "AdminApiKeyResponse";
    getTypeName() => "AdminCreateApiKey";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminUpdateApiKey implements IReturn<EmptyResponse>, IPatch, IConvertible
{
    // @DataMember(Order=1)
    // @Validate(Validator="GreaterThan(0)")
    int? id;

    // @DataMember(Order=2)
    String? name;

    // @DataMember(Order=3)
    String? userId;

    // @DataMember(Order=4)
    String? userName;

    // @DataMember(Order=5)
    List<String>? scopes;

    // @DataMember(Order=6)
    List<String>? features;

    // @DataMember(Order=7)
    List<String>? restrictTo;

    // @DataMember(Order=8)
    DateTime? expiryDate;

    // @DataMember(Order=9)
    DateTime? cancelledDate;

    // @DataMember(Order=10)
    String? notes;

    // @DataMember(Order=11)
    int? refId;

    // @DataMember(Order=12)
    String? refIdStr;

    // @DataMember(Order=13)
    Map<String,String?>? meta;

    // @DataMember(Order=14)
    List<String>? reset;

    AdminUpdateApiKey({this.id,this.name,this.userId,this.userName,this.scopes,this.features,this.restrictTo,this.expiryDate,this.cancelledDate,this.notes,this.refId,this.refIdStr,this.meta,this.reset});
    AdminUpdateApiKey.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        name = json['name'];
        userId = json['userId'];
        userName = json['userName'];
        scopes = JsonConverters.fromJson(json['scopes'],'List<String>',context!);
        features = JsonConverters.fromJson(json['features'],'List<String>',context!);
        restrictTo = JsonConverters.fromJson(json['restrictTo'],'List<String>',context!);
        expiryDate = JsonConverters.fromJson(json['expiryDate'],'DateTime',context!);
        cancelledDate = JsonConverters.fromJson(json['cancelledDate'],'DateTime',context!);
        notes = json['notes'];
        refId = json['refId'];
        refIdStr = json['refIdStr'];
        meta = JsonConverters.toStringMap(json['meta']);
        reset = JsonConverters.fromJson(json['reset'],'List<String>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'name': name,
        'userId': userId,
        'userName': userName,
        'scopes': JsonConverters.toJson(scopes,'List<String>',context!),
        'features': JsonConverters.toJson(features,'List<String>',context!),
        'restrictTo': JsonConverters.toJson(restrictTo,'List<String>',context!),
        'expiryDate': JsonConverters.toJson(expiryDate,'DateTime',context!),
        'cancelledDate': JsonConverters.toJson(cancelledDate,'DateTime',context!),
        'notes': notes,
        'refId': refId,
        'refIdStr': refIdStr,
        'meta': meta,
        'reset': JsonConverters.toJson(reset,'List<String>',context!)
    };

    createResponse() => EmptyResponse();
    getResponseTypeName() => "EmptyResponse";
    getTypeName() => "AdminUpdateApiKey";
    TypeContext? context = _ctx;
}

// @DataContract
class AdminDeleteApiKey implements IReturn<EmptyResponse>, IDelete, IConvertible
{
    // @DataMember(Order=1)
    // @Validate(Validator="GreaterThan(0)")
    int? id;

    AdminDeleteApiKey({this.id});
    AdminDeleteApiKey.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id
    };

    createResponse() => EmptyResponse();
    getResponseTypeName() => "EmptyResponse";
    getTypeName() => "AdminDeleteApiKey";
    TypeContext? context = _ctx;
}

class AdminJobDashboard implements IReturn<AdminJobDashboardResponse>, IGet, IConvertible
{
    DateTime? from;
    DateTime? to;

    AdminJobDashboard({this.from,this.to});
    AdminJobDashboard.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        from = JsonConverters.fromJson(json['from'],'DateTime',context!);
        to = JsonConverters.fromJson(json['to'],'DateTime',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'from': JsonConverters.toJson(from,'DateTime',context!),
        'to': JsonConverters.toJson(to,'DateTime',context!)
    };

    createResponse() => AdminJobDashboardResponse();
    getResponseTypeName() => "AdminJobDashboardResponse";
    getTypeName() => "AdminJobDashboard";
    TypeContext? context = _ctx;
}

class AdminJobInfo implements IReturn<AdminJobInfoResponse>, IGet, IConvertible
{
    DateTime? month;

    AdminJobInfo({this.month});
    AdminJobInfo.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        month = JsonConverters.fromJson(json['month'],'DateTime',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'month': JsonConverters.toJson(month,'DateTime',context!)
    };

    createResponse() => AdminJobInfoResponse();
    getResponseTypeName() => "AdminJobInfoResponse";
    getTypeName() => "AdminJobInfo";
    TypeContext? context = _ctx;
}

class AdminGetJob implements IReturn<AdminGetJobResponse>, IGet, IConvertible
{
    int? id;
    String? refId;

    AdminGetJob({this.id,this.refId});
    AdminGetJob.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        refId = json['refId'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'refId': refId
    };

    createResponse() => AdminGetJobResponse();
    getResponseTypeName() => "AdminGetJobResponse";
    getTypeName() => "AdminGetJob";
    TypeContext? context = _ctx;
}

class AdminGetJobProgress implements IReturn<AdminGetJobProgressResponse>, IGet, IConvertible
{
    // @Validate(Validator="GreaterThan(0)")
    int? id;

    int? logStart;

    AdminGetJobProgress({this.id,this.logStart});
    AdminGetJobProgress.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        id = json['id'];
        logStart = json['logStart'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'id': id,
        'logStart': logStart
    };

    createResponse() => AdminGetJobProgressResponse();
    getResponseTypeName() => "AdminGetJobProgressResponse";
    getTypeName() => "AdminGetJobProgress";
    TypeContext? context = _ctx;
}

class AdminQueryBackgroundJobs extends QueryDb<BackgroundJob> implements IReturn<QueryResponse<BackgroundJob>>, IConvertible, IGet
{
    int? id;
    String? refId;

    AdminQueryBackgroundJobs({this.id,this.refId});
    AdminQueryBackgroundJobs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        id = json['id'];
        refId = json['refId'];
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'id': id,
        'refId': refId
    });

    createResponse() => QueryResponse<BackgroundJob>();
    getResponseTypeName() => "QueryResponse<BackgroundJob>";
    getTypeName() => "AdminQueryBackgroundJobs";
    TypeContext? context = _ctx;
}

class AdminQueryJobSummary extends QueryDb<JobSummary> implements IReturn<QueryResponse<JobSummary>>, IConvertible, IGet
{
    int? id;
    String? refId;

    AdminQueryJobSummary({this.id,this.refId});
    AdminQueryJobSummary.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        id = json['id'];
        refId = json['refId'];
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'id': id,
        'refId': refId
    });

    createResponse() => QueryResponse<JobSummary>();
    getResponseTypeName() => "QueryResponse<JobSummary>";
    getTypeName() => "AdminQueryJobSummary";
    TypeContext? context = _ctx;
}

class AdminQueryScheduledTasks extends QueryDb<ScheduledTask> implements IReturn<QueryResponse<ScheduledTask>>, IConvertible, IGet
{
    AdminQueryScheduledTasks();
    AdminQueryScheduledTasks.fromJson(Map<String, dynamic> json) : super.fromJson(json);
    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson();
    createResponse() => QueryResponse<ScheduledTask>();
    getResponseTypeName() => "QueryResponse<ScheduledTask>";
    getTypeName() => "AdminQueryScheduledTasks";
    TypeContext? context = _ctx;
}

class AdminQueryCompletedJobs extends QueryDb<CompletedJob> implements IReturn<QueryResponse<CompletedJob>>, IConvertible, IGet
{
    DateTime? month;

    AdminQueryCompletedJobs({this.month});
    AdminQueryCompletedJobs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        month = JsonConverters.fromJson(json['month'],'DateTime',context!);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'month': JsonConverters.toJson(month,'DateTime',context!)
    });

    createResponse() => QueryResponse<CompletedJob>();
    getResponseTypeName() => "QueryResponse<CompletedJob>";
    getTypeName() => "AdminQueryCompletedJobs";
    TypeContext? context = _ctx;
}

class AdminQueryFailedJobs extends QueryDb<FailedJob> implements IReturn<QueryResponse<FailedJob>>, IConvertible, IGet
{
    DateTime? month;

    AdminQueryFailedJobs({this.month});
    AdminQueryFailedJobs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        super.fromMap(json);
        month = JsonConverters.fromJson(json['month'],'DateTime',context!);
        return this;
    }

    Map<String, dynamic> toJson() => super.toJson()..addAll({
        'month': JsonConverters.toJson(month,'DateTime',context!)
    });

    createResponse() => QueryResponse<FailedJob>();
    getResponseTypeName() => "QueryResponse<FailedJob>";
    getTypeName() => "AdminQueryFailedJobs";
    TypeContext? context = _ctx;
}

class AdminRequeueFailedJobs implements IReturn<AdminRequeueFailedJobsJobsResponse>, IConvertible, IPost
{
    List<int>? ids;

    AdminRequeueFailedJobs({this.ids});
    AdminRequeueFailedJobs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        ids = JsonConverters.fromJson(json['ids'],'List<int>',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'ids': JsonConverters.toJson(ids,'List<int>',context!)
    };

    createResponse() => AdminRequeueFailedJobsJobsResponse();
    getResponseTypeName() => "AdminRequeueFailedJobsJobsResponse";
    getTypeName() => "AdminRequeueFailedJobs";
    TypeContext? context = _ctx;
}

class AdminCancelJobs implements IReturn<AdminCancelJobsResponse>, IGet, IConvertible
{
    List<int>? ids;
    String? worker;
    BackgroundJobState? state;
    String? cancelWorker;

    AdminCancelJobs({this.ids,this.worker,this.state,this.cancelWorker});
    AdminCancelJobs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        ids = JsonConverters.fromJson(json['ids'],'List<int>',context!);
        worker = json['worker'];
        state = JsonConverters.fromJson(json['state'],'BackgroundJobState',context!);
        cancelWorker = json['cancelWorker'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'ids': JsonConverters.toJson(ids,'List<int>',context!),
        'worker': worker,
        'state': JsonConverters.toJson(state,'BackgroundJobState',context!),
        'cancelWorker': cancelWorker
    };

    createResponse() => AdminCancelJobsResponse();
    getResponseTypeName() => "AdminCancelJobsResponse";
    getTypeName() => "AdminCancelJobs";
    TypeContext? context = _ctx;
}

// @Route("/requestlogs")
// @DataContract
class RequestLogs implements IReturn<RequestLogsResponse>, IGet, IConvertible
{
    // @DataMember(Order=1)
    int? beforeSecs;

    // @DataMember(Order=2)
    int? afterSecs;

    // @DataMember(Order=3)
    String? operationName;

    // @DataMember(Order=4)
    String? ipAddress;

    // @DataMember(Order=5)
    String? forwardedFor;

    // @DataMember(Order=6)
    String? userAuthId;

    // @DataMember(Order=7)
    String? sessionId;

    // @DataMember(Order=8)
    String? referer;

    // @DataMember(Order=9)
    String? pathInfo;

    // @DataMember(Order=10)
    List<int>? ids;

    // @DataMember(Order=11)
    int? beforeId;

    // @DataMember(Order=12)
    int? afterId;

    // @DataMember(Order=13)
    bool? hasResponse;

    // @DataMember(Order=14)
    bool? withErrors;

    // @DataMember(Order=15)
    bool? enableSessionTracking;

    // @DataMember(Order=16)
    bool? enableResponseTracking;

    // @DataMember(Order=17)
    bool? enableErrorTracking;

    // @DataMember(Order=18)
    Duration? durationLongerThan;

    // @DataMember(Order=19)
    Duration? durationLessThan;

    // @DataMember(Order=20)
    int? skip;

    // @DataMember(Order=21)
    int? take;

    // @DataMember(Order=22)
    String? orderBy;

    RequestLogs({this.beforeSecs,this.afterSecs,this.operationName,this.ipAddress,this.forwardedFor,this.userAuthId,this.sessionId,this.referer,this.pathInfo,this.ids,this.beforeId,this.afterId,this.hasResponse,this.withErrors,this.enableSessionTracking,this.enableResponseTracking,this.enableErrorTracking,this.durationLongerThan,this.durationLessThan,this.skip,this.take,this.orderBy});
    RequestLogs.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        beforeSecs = json['beforeSecs'];
        afterSecs = json['afterSecs'];
        operationName = json['operationName'];
        ipAddress = json['ipAddress'];
        forwardedFor = json['forwardedFor'];
        userAuthId = json['userAuthId'];
        sessionId = json['sessionId'];
        referer = json['referer'];
        pathInfo = json['pathInfo'];
        ids = JsonConverters.fromJson(json['ids'],'List<int>',context!);
        beforeId = json['beforeId'];
        afterId = json['afterId'];
        hasResponse = json['hasResponse'];
        withErrors = json['withErrors'];
        enableSessionTracking = json['enableSessionTracking'];
        enableResponseTracking = json['enableResponseTracking'];
        enableErrorTracking = json['enableErrorTracking'];
        durationLongerThan = JsonConverters.fromJson(json['durationLongerThan'],'Duration',context!);
        durationLessThan = JsonConverters.fromJson(json['durationLessThan'],'Duration',context!);
        skip = json['skip'];
        take = json['take'];
        orderBy = json['orderBy'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'beforeSecs': beforeSecs,
        'afterSecs': afterSecs,
        'operationName': operationName,
        'ipAddress': ipAddress,
        'forwardedFor': forwardedFor,
        'userAuthId': userAuthId,
        'sessionId': sessionId,
        'referer': referer,
        'pathInfo': pathInfo,
        'ids': JsonConverters.toJson(ids,'List<int>',context!),
        'beforeId': beforeId,
        'afterId': afterId,
        'hasResponse': hasResponse,
        'withErrors': withErrors,
        'enableSessionTracking': enableSessionTracking,
        'enableResponseTracking': enableResponseTracking,
        'enableErrorTracking': enableErrorTracking,
        'durationLongerThan': JsonConverters.toJson(durationLongerThan,'Duration',context!),
        'durationLessThan': JsonConverters.toJson(durationLessThan,'Duration',context!),
        'skip': skip,
        'take': take,
        'orderBy': orderBy
    };

    createResponse() => RequestLogsResponse();
    getResponseTypeName() => "RequestLogsResponse";
    getTypeName() => "RequestLogs";
    TypeContext? context = _ctx;
}

// @DataContract
class GetAnalyticsReports implements IReturn<AnalyticsReports>, IGet, IConvertible
{
    // @DataMember(Order=1)
    DateTime? month;

    GetAnalyticsReports({this.month});
    GetAnalyticsReports.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        month = JsonConverters.fromJson(json['month'],'DateTime',context!);
        return this;
    }

    Map<String, dynamic> toJson() => {
        'month': JsonConverters.toJson(month,'DateTime',context!)
    };

    createResponse() => AnalyticsReports();
    getResponseTypeName() => "AnalyticsReports";
    getTypeName() => "GetAnalyticsReports";
    TypeContext? context = _ctx;
}

// @Route("/validation/rules/{Type}")
// @DataContract
class GetValidationRules implements IReturn<GetValidationRulesResponse>, IGet, IConvertible
{
    // @DataMember(Order=1)
    String? authSecret;

    // @DataMember(Order=2)
    String? type;

    GetValidationRules({this.authSecret,this.type});
    GetValidationRules.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        authSecret = json['authSecret'];
        type = json['type'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'authSecret': authSecret,
        'type': type
    };

    createResponse() => GetValidationRulesResponse();
    getResponseTypeName() => "GetValidationRulesResponse";
    getTypeName() => "GetValidationRules";
    TypeContext? context = _ctx;
}

// @Route("/validation/rules")
// @DataContract
class ModifyValidationRules implements IReturnVoid, IConvertible, IPost
{
    // @DataMember(Order=1)
    String? authSecret;

    // @DataMember(Order=2)
    List<ValidationRule>? saveRules;

    // @DataMember(Order=3)
    List<int>? deleteRuleIds;

    // @DataMember(Order=4)
    List<int>? suspendRuleIds;

    // @DataMember(Order=5)
    List<int>? unsuspendRuleIds;

    // @DataMember(Order=6)
    bool? clearCache;

    ModifyValidationRules({this.authSecret,this.saveRules,this.deleteRuleIds,this.suspendRuleIds,this.unsuspendRuleIds,this.clearCache});
    ModifyValidationRules.fromJson(Map<String, dynamic> json) { fromMap(json); }

    fromMap(Map<String, dynamic> json) {
        authSecret = json['authSecret'];
        saveRules = JsonConverters.fromJson(json['saveRules'],'List<ValidationRule>',context!);
        deleteRuleIds = JsonConverters.fromJson(json['deleteRuleIds'],'List<int>',context!);
        suspendRuleIds = JsonConverters.fromJson(json['suspendRuleIds'],'List<int>',context!);
        unsuspendRuleIds = JsonConverters.fromJson(json['unsuspendRuleIds'],'List<int>',context!);
        clearCache = json['clearCache'];
        return this;
    }

    Map<String, dynamic> toJson() => {
        'authSecret': authSecret,
        'saveRules': JsonConverters.toJson(saveRules,'List<ValidationRule>',context!),
        'deleteRuleIds': JsonConverters.toJson(deleteRuleIds,'List<int>',context!),
        'suspendRuleIds': JsonConverters.toJson(suspendRuleIds,'List<int>',context!),
        'unsuspendRuleIds': JsonConverters.toJson(unsuspendRuleIds,'List<int>',context!),
        'clearCache': clearCache
    };

    createResponse() {}
    getTypeName() => "ModifyValidationRules";
    TypeContext? context = _ctx;
}

TypeContext _ctx = TypeContext(library: 'localhost', types: <String, TypeInfo> {
    'Property': TypeInfo(TypeOf.Class, create:() => Property()),
    'AdminUserBase': TypeInfo(TypeOf.AbstractClass),
    'RequestLog': TypeInfo(TypeOf.Class, create:() => RequestLog()),
    'RedisEndpointInfo': TypeInfo(TypeOf.Class, create:() => RedisEndpointInfo()),
    'BackgroundJobState': TypeInfo(TypeOf.Enum, enumValues:BackgroundJobState.values),
    'BackgroundJobBase': TypeInfo(TypeOf.AbstractClass),
    'BackgroundJob': TypeInfo(TypeOf.Class, create:() => BackgroundJob()),
    'JobSummary': TypeInfo(TypeOf.Class, create:() => JobSummary()),
    'BackgroundJobOptions': TypeInfo(TypeOf.Class, create:() => BackgroundJobOptions()),
    'ScheduledTask': TypeInfo(TypeOf.Class, create:() => ScheduledTask()),
    'CompletedJob': TypeInfo(TypeOf.Class, create:() => CompletedJob()),
    'FailedJob': TypeInfo(TypeOf.Class, create:() => FailedJob()),
    'ValidateRule': TypeInfo(TypeOf.Class, create:() => ValidateRule()),
    'ValidationRule': TypeInfo(TypeOf.Class, create:() => ValidationRule()),
    'AppInfo': TypeInfo(TypeOf.Class, create:() => AppInfo()),
    'ImageInfo': TypeInfo(TypeOf.Class, create:() => ImageInfo()),
    'LinkInfo': TypeInfo(TypeOf.Class, create:() => LinkInfo()),
    'ThemeInfo': TypeInfo(TypeOf.Class, create:() => ThemeInfo()),
    'ApiCss': TypeInfo(TypeOf.Class, create:() => ApiCss()),
    'AppTags': TypeInfo(TypeOf.Class, create:() => AppTags()),
    'LocodeUi': TypeInfo(TypeOf.Class, create:() => LocodeUi()),
    'ExplorerUi': TypeInfo(TypeOf.Class, create:() => ExplorerUi()),
    'AdminUi': TypeInfo(TypeOf.Class, create:() => AdminUi()),
    'FormatInfo': TypeInfo(TypeOf.Class, create:() => FormatInfo()),
    'ApiFormat': TypeInfo(TypeOf.Class, create:() => ApiFormat()),
    'UiInfo': TypeInfo(TypeOf.Class, create:() => UiInfo()),
    'List<LinkInfo>': TypeInfo(TypeOf.Class, create:() => <LinkInfo>[]),
    'ConfigInfo': TypeInfo(TypeOf.Class, create:() => ConfigInfo()),
    'FieldCss': TypeInfo(TypeOf.Class, create:() => FieldCss()),
    'InputInfo': TypeInfo(TypeOf.Class, create:() => InputInfo()),
    'List<KeyValuePair><String,String>': TypeInfo(TypeOf.Class, create:() => <KeyValuePair><String,String>[]),
    'KeyValuePair': TypeInfo(TypeOf.Class, create:() => KeyValuePair()),
    'MetaAuthProvider': TypeInfo(TypeOf.Class, create:() => MetaAuthProvider()),
    'List<InputInfo>': TypeInfo(TypeOf.Class, create:() => <InputInfo>[]),
    'IdentityAuthInfo': TypeInfo(TypeOf.Class, create:() => IdentityAuthInfo()),
    'AuthInfo': TypeInfo(TypeOf.Class, create:() => AuthInfo()),
    'List<MetaAuthProvider>': TypeInfo(TypeOf.Class, create:() => <MetaAuthProvider>[]),
    'Map<String,List<LinkInfo>?>': TypeInfo(TypeOf.Class, create:() => Map<String,List<LinkInfo>?>()),
    'Map<String,List<String>?>': TypeInfo(TypeOf.Class, create:() => Map<String,List<String>?>()),
    'ApiKeyInfo': TypeInfo(TypeOf.Class, create:() => ApiKeyInfo()),
    'List<KeyValuePair<String,String>>': TypeInfo(TypeOf.Class, create:() => <KeyValuePair<String,String>>[]),
    'MetadataTypeName': TypeInfo(TypeOf.Class, create:() => MetadataTypeName()),
    'MetadataDataContract': TypeInfo(TypeOf.Class, create:() => MetadataDataContract()),
    'MetadataDataMember': TypeInfo(TypeOf.Class, create:() => MetadataDataMember()),
    'MetadataAttribute': TypeInfo(TypeOf.Class, create:() => MetadataAttribute()),
    'List<MetadataPropertyType>': TypeInfo(TypeOf.Class, create:() => <MetadataPropertyType>[]),
    'MetadataPropertyType': TypeInfo(TypeOf.Class, create:() => MetadataPropertyType()),
    'RefInfo': TypeInfo(TypeOf.Class, create:() => RefInfo()),
    'List<MetadataAttribute>': TypeInfo(TypeOf.Class, create:() => <MetadataAttribute>[]),
    'MetadataType': TypeInfo(TypeOf.Class, create:() => MetadataType()),
    'List<MetadataTypeName>': TypeInfo(TypeOf.Class, create:() => <MetadataTypeName>[]),
    'CommandInfo': TypeInfo(TypeOf.Class, create:() => CommandInfo()),
    'CommandsInfo': TypeInfo(TypeOf.Class, create:() => CommandsInfo()),
    'List<CommandInfo>': TypeInfo(TypeOf.Class, create:() => <CommandInfo>[]),
    'AutoQueryConvention': TypeInfo(TypeOf.Class, create:() => AutoQueryConvention()),
    'AutoQueryInfo': TypeInfo(TypeOf.Class, create:() => AutoQueryInfo()),
    'List<AutoQueryConvention>': TypeInfo(TypeOf.Class, create:() => <AutoQueryConvention>[]),
    'ScriptMethodType': TypeInfo(TypeOf.Class, create:() => ScriptMethodType()),
    'ValidationInfo': TypeInfo(TypeOf.Class, create:() => ValidationInfo()),
    'List<ScriptMethodType>': TypeInfo(TypeOf.Class, create:() => <ScriptMethodType>[]),
    'SharpPagesInfo': TypeInfo(TypeOf.Class, create:() => SharpPagesInfo()),
    'RequestLogsInfo': TypeInfo(TypeOf.Class, create:() => RequestLogsInfo()),
    'ProfilingInfo': TypeInfo(TypeOf.Class, create:() => ProfilingInfo()),
    'FilesUploadLocation': TypeInfo(TypeOf.Class, create:() => FilesUploadLocation()),
    'FilesUploadInfo': TypeInfo(TypeOf.Class, create:() => FilesUploadInfo()),
    'List<FilesUploadLocation>': TypeInfo(TypeOf.Class, create:() => <FilesUploadLocation>[]),
    'MediaRule': TypeInfo(TypeOf.Class, create:() => MediaRule()),
    'AdminUsersInfo': TypeInfo(TypeOf.Class, create:() => AdminUsersInfo()),
    'List<MediaRule>': TypeInfo(TypeOf.Class, create:() => <MediaRule>[]),
    'AdminIdentityUsersInfo': TypeInfo(TypeOf.Class, create:() => AdminIdentityUsersInfo()),
    'AdminRedisInfo': TypeInfo(TypeOf.Class, create:() => AdminRedisInfo()),
    'SchemaInfo': TypeInfo(TypeOf.Class, create:() => SchemaInfo()),
    'DatabaseInfo': TypeInfo(TypeOf.Class, create:() => DatabaseInfo()),
    'List<SchemaInfo>': TypeInfo(TypeOf.Class, create:() => <SchemaInfo>[]),
    'AdminDatabaseInfo': TypeInfo(TypeOf.Class, create:() => AdminDatabaseInfo()),
    'List<DatabaseInfo>': TypeInfo(TypeOf.Class, create:() => <DatabaseInfo>[]),
    'PluginInfo': TypeInfo(TypeOf.Class, create:() => PluginInfo()),
    'CustomPluginInfo': TypeInfo(TypeOf.Class, create:() => CustomPluginInfo()),
    'MetadataTypesConfig': TypeInfo(TypeOf.Class, create:() => MetadataTypesConfig()),
    'MetadataRoute': TypeInfo(TypeOf.Class, create:() => MetadataRoute()),
    'ApiUiInfo': TypeInfo(TypeOf.Class, create:() => ApiUiInfo()),
    'MetadataOperationType': TypeInfo(TypeOf.Class, create:() => MetadataOperationType()),
    'List<MetadataRoute>': TypeInfo(TypeOf.Class, create:() => <MetadataRoute>[]),
    'MetadataTypes': TypeInfo(TypeOf.Class, create:() => MetadataTypes()),
    'List<MetadataType>': TypeInfo(TypeOf.Class, create:() => <MetadataType>[]),
    'List<MetadataOperationType>': TypeInfo(TypeOf.Class, create:() => <MetadataOperationType>[]),
    'AdminRole': TypeInfo(TypeOf.Class, create:() => AdminRole()),
    'ServerStats': TypeInfo(TypeOf.Class, create:() => ServerStats()),
    'Map<String,int?>': TypeInfo(TypeOf.Class, create:() => Map<String,int?>()),
    'DiagnosticEntry': TypeInfo(TypeOf.Class, create:() => DiagnosticEntry()),
    'Map<String,dynamic?>': TypeInfo(TypeOf.Class, create:() => Map<String,dynamic?>()),
    'RedisSearchResult': TypeInfo(TypeOf.Class, create:() => RedisSearchResult()),
    'RedisText': TypeInfo(TypeOf.Class, create:() => RedisText()),
    'List<RedisText>': TypeInfo(TypeOf.Class, create:() => <RedisText>[]),
    'CommandSummary': TypeInfo(TypeOf.Class, create:() => CommandSummary()),
    'CommandResult': TypeInfo(TypeOf.Class, create:() => CommandResult()),
    'PartialApiKey': TypeInfo(TypeOf.Class, create:() => PartialApiKey()),
    'JobStatSummary': TypeInfo(TypeOf.Class, create:() => JobStatSummary()),
    'HourSummary': TypeInfo(TypeOf.Class, create:() => HourSummary()),
    'WorkerStats': TypeInfo(TypeOf.Class, create:() => WorkerStats()),
    'RequestLogEntry': TypeInfo(TypeOf.Class, create:() => RequestLogEntry()),
    'RequestSummary': TypeInfo(TypeOf.Class, create:() => RequestSummary()),
    'AdminGetRolesResponse': TypeInfo(TypeOf.Class, create:() => AdminGetRolesResponse()),
    'List<AdminRole>': TypeInfo(TypeOf.Class, create:() => <AdminRole>[]),
    'AdminGetRoleResponse': TypeInfo(TypeOf.Class, create:() => AdminGetRoleResponse()),
    'List<Property>': TypeInfo(TypeOf.Class, create:() => <Property>[]),
    'AdminDashboardResponse': TypeInfo(TypeOf.Class, create:() => AdminDashboardResponse()),
    'AdminUserResponse': TypeInfo(TypeOf.Class, create:() => AdminUserResponse()),
    'List<Map<String,dynamic>>': TypeInfo(TypeOf.Class, create:() => <Map<String,dynamic>>[]),
    'Map<String,dynamic>': TypeInfo(TypeOf.Class, create:() => Map<String,dynamic>()),
    'AdminUsersResponse': TypeInfo(TypeOf.Class, create:() => AdminUsersResponse()),
    'AdminDeleteUserResponse': TypeInfo(TypeOf.Class, create:() => AdminDeleteUserResponse()),
    'AdminProfilingResponse': TypeInfo(TypeOf.Class, create:() => AdminProfilingResponse()),
    'List<DiagnosticEntry>': TypeInfo(TypeOf.Class, create:() => <DiagnosticEntry>[]),
    'AdminRedisResponse': TypeInfo(TypeOf.Class, create:() => AdminRedisResponse()),
    'List<RedisSearchResult>': TypeInfo(TypeOf.Class, create:() => <RedisSearchResult>[]),
    'AdminDatabaseResponse': TypeInfo(TypeOf.Class, create:() => AdminDatabaseResponse()),
    'ViewCommandsResponse': TypeInfo(TypeOf.Class, create:() => ViewCommandsResponse()),
    'List<CommandSummary>': TypeInfo(TypeOf.Class, create:() => <CommandSummary>[]),
    'List<CommandResult>': TypeInfo(TypeOf.Class, create:() => <CommandResult>[]),
    'ExecuteCommandResponse': TypeInfo(TypeOf.Class, create:() => ExecuteCommandResponse()),
    'AdminApiKeysResponse': TypeInfo(TypeOf.Class, create:() => AdminApiKeysResponse()),
    'List<PartialApiKey>': TypeInfo(TypeOf.Class, create:() => <PartialApiKey>[]),
    'AdminApiKeyResponse': TypeInfo(TypeOf.Class, create:() => AdminApiKeyResponse()),
    'AdminJobDashboardResponse': TypeInfo(TypeOf.Class, create:() => AdminJobDashboardResponse()),
    'List<JobStatSummary>': TypeInfo(TypeOf.Class, create:() => <JobStatSummary>[]),
    'List<HourSummary>': TypeInfo(TypeOf.Class, create:() => <HourSummary>[]),
    'AdminJobInfoResponse': TypeInfo(TypeOf.Class, create:() => AdminJobInfoResponse()),
    'List<DateTime>': TypeInfo(TypeOf.Class, create:() => <DateTime>[]),
    'List<WorkerStats>': TypeInfo(TypeOf.Class, create:() => <WorkerStats>[]),
    'Map<BackgroundJobState,int?>': TypeInfo(TypeOf.Class, create:() => Map<BackgroundJobState,int?>()),
    'AdminGetJobResponse': TypeInfo(TypeOf.Class, create:() => AdminGetJobResponse()),
    'AdminGetJobProgressResponse': TypeInfo(TypeOf.Class, create:() => AdminGetJobProgressResponse()),
    'AdminRequeueFailedJobsJobsResponse': TypeInfo(TypeOf.Class, create:() => AdminRequeueFailedJobsJobsResponse()),
    'Map<int,String?>': TypeInfo(TypeOf.Class, create:() => Map<int,String?>()),
    'AdminCancelJobsResponse': TypeInfo(TypeOf.Class, create:() => AdminCancelJobsResponse()),
    'RequestLogsResponse': TypeInfo(TypeOf.Class, create:() => RequestLogsResponse()),
    'List<RequestLogEntry>': TypeInfo(TypeOf.Class, create:() => <RequestLogEntry>[]),
    'AnalyticsReports': TypeInfo(TypeOf.Class, create:() => AnalyticsReports()),
    'Map<String,RequestSummary?>': TypeInfo(TypeOf.Class, create:() => Map<String,RequestSummary?>()),
    'GetValidationRulesResponse': TypeInfo(TypeOf.Class, create:() => GetValidationRulesResponse()),
    'List<ValidationRule>': TypeInfo(TypeOf.Class, create:() => <ValidationRule>[]),
    'AdminCreateRole': TypeInfo(TypeOf.Class, create:() => AdminCreateRole()),
    'AdminGetRoles': TypeInfo(TypeOf.Class, create:() => AdminGetRoles()),
    'AdminGetRole': TypeInfo(TypeOf.Class, create:() => AdminGetRole()),
    'AdminUpdateRole': TypeInfo(TypeOf.Class, create:() => AdminUpdateRole()),
    'AdminDeleteRole': TypeInfo(TypeOf.Class, create:() => AdminDeleteRole()),
    'AdminDashboard': TypeInfo(TypeOf.Class, create:() => AdminDashboard()),
    'AdminGetUser': TypeInfo(TypeOf.Class, create:() => AdminGetUser()),
    'AdminQueryUsers': TypeInfo(TypeOf.Class, create:() => AdminQueryUsers()),
    'AdminCreateUser': TypeInfo(TypeOf.Class, create:() => AdminCreateUser()),
    'AdminUpdateUser': TypeInfo(TypeOf.Class, create:() => AdminUpdateUser()),
    'AdminDeleteUser': TypeInfo(TypeOf.Class, create:() => AdminDeleteUser()),
    'QueryResponse<RequestLog>': TypeInfo(TypeOf.Class, create:() => QueryResponse<RequestLog>()),
    'AdminQueryRequestLogs': TypeInfo(TypeOf.Class, create:() => AdminQueryRequestLogs()),
    'List<RequestLog>': TypeInfo(TypeOf.Class, create:() => <RequestLog>[]),
    'AdminProfiling': TypeInfo(TypeOf.Class, create:() => AdminProfiling()),
    'AdminRedis': TypeInfo(TypeOf.Class, create:() => AdminRedis()),
    'AdminDatabase': TypeInfo(TypeOf.Class, create:() => AdminDatabase()),
    'ViewCommands': TypeInfo(TypeOf.Class, create:() => ViewCommands()),
    'ExecuteCommand': TypeInfo(TypeOf.Class, create:() => ExecuteCommand()),
    'AdminQueryApiKeys': TypeInfo(TypeOf.Class, create:() => AdminQueryApiKeys()),
    'AdminCreateApiKey': TypeInfo(TypeOf.Class, create:() => AdminCreateApiKey()),
    'AdminUpdateApiKey': TypeInfo(TypeOf.Class, create:() => AdminUpdateApiKey()),
    'AdminDeleteApiKey': TypeInfo(TypeOf.Class, create:() => AdminDeleteApiKey()),
    'AdminJobDashboard': TypeInfo(TypeOf.Class, create:() => AdminJobDashboard()),
    'AdminJobInfo': TypeInfo(TypeOf.Class, create:() => AdminJobInfo()),
    'AdminGetJob': TypeInfo(TypeOf.Class, create:() => AdminGetJob()),
    'AdminGetJobProgress': TypeInfo(TypeOf.Class, create:() => AdminGetJobProgress()),
    'QueryResponse<BackgroundJob>': TypeInfo(TypeOf.Class, create:() => QueryResponse<BackgroundJob>()),
    'AdminQueryBackgroundJobs': TypeInfo(TypeOf.Class, create:() => AdminQueryBackgroundJobs()),
    'List<BackgroundJob>': TypeInfo(TypeOf.Class, create:() => <BackgroundJob>[]),
    'QueryResponse<JobSummary>': TypeInfo(TypeOf.Class, create:() => QueryResponse<JobSummary>()),
    'AdminQueryJobSummary': TypeInfo(TypeOf.Class, create:() => AdminQueryJobSummary()),
    'List<JobSummary>': TypeInfo(TypeOf.Class, create:() => <JobSummary>[]),
    'QueryResponse<ScheduledTask>': TypeInfo(TypeOf.Class, create:() => QueryResponse<ScheduledTask>()),
    'AdminQueryScheduledTasks': TypeInfo(TypeOf.Class, create:() => AdminQueryScheduledTasks()),
    'List<ScheduledTask>': TypeInfo(TypeOf.Class, create:() => <ScheduledTask>[]),
    'QueryResponse<CompletedJob>': TypeInfo(TypeOf.Class, create:() => QueryResponse<CompletedJob>()),
    'AdminQueryCompletedJobs': TypeInfo(TypeOf.Class, create:() => AdminQueryCompletedJobs()),
    'List<CompletedJob>': TypeInfo(TypeOf.Class, create:() => <CompletedJob>[]),
    'QueryResponse<FailedJob>': TypeInfo(TypeOf.Class, create:() => QueryResponse<FailedJob>()),
    'AdminQueryFailedJobs': TypeInfo(TypeOf.Class, create:() => AdminQueryFailedJobs()),
    'List<FailedJob>': TypeInfo(TypeOf.Class, create:() => <FailedJob>[]),
    'AdminRequeueFailedJobs': TypeInfo(TypeOf.Class, create:() => AdminRequeueFailedJobs()),
    'AdminCancelJobs': TypeInfo(TypeOf.Class, create:() => AdminCancelJobs()),
    'RequestLogs': TypeInfo(TypeOf.Class, create:() => RequestLogs()),
    'GetAnalyticsReports': TypeInfo(TypeOf.Class, create:() => GetAnalyticsReports()),
    'GetValidationRules': TypeInfo(TypeOf.Class, create:() => GetValidationRules()),
    'ModifyValidationRules': TypeInfo(TypeOf.Class, create:() => ModifyValidationRules()),
});

