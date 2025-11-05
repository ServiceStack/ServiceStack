export default {
  "namespaces": [
    "ServiceStack",
    "System",
    "ServiceStack.Jobs",
    "System.Collections.Generic"
  ],
  "types": [
    {
      "name": "QueryDb\u00601",
      "namespace": "ServiceStack",
      "genericArgs": [
        "T"
      ],
      "inherits": {
        "name": "QueryBase",
        "namespace": "ServiceStack"
      },
      "implements": [],
      "isAbstract": true,
      "isGenericTypeDef": true
    },
    {
      "name": "BackgroundJob",
      "namespace": "ServiceStack.Jobs",
      "inherits": {
        "name": "BackgroundJobBase",
        "namespace": "ServiceStack.Jobs"
      },
      "implements": [],
      "icon": {
        "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 32 32\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M10.293 5.293L7 8.586L5.707 7.293L4.293 8.707L7 11.414l4.707-4.707zM14 7v2h14V7zm0 8v2h14v-2zm0 8v2h14v-2z\u0027/\u003E\u003C/svg\u003E"
      },
      "properties": [
        {
          "name": "Id",
          "type": "Int64",
          "namespace": "System",
          "isValueType": true,
          "isPrimaryKey": true
        }
      ]
    },
    {
      "name": "JobSummary",
      "namespace": "ServiceStack.Jobs",
      "implements": [],
      "icon": {
        "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 24 24\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M21 21H3v-2h18zM8 3H4v14h4zm6 3h-4v11h4zm6 4h-4v7h4z\u0027/\u003E\u003C/svg\u003E"
      },
      "properties": [
        {
          "name": "Id",
          "type": "Int64",
          "namespace": "System",
          "isValueType": true,
          "isPrimaryKey": true
        },
        {
          "name": "ParentId",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int64"
          ]
        },
        {
          "name": "RefId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Worker",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Tag",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "BatchId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "CreatedDate",
          "type": "DateTime",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "CreatedBy",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RequestType",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "Command",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Request",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "Response",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "UserId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Callback",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "StartedDate",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "CompletedDate",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "State",
          "type": "BackgroundJobState",
          "namespace": "ServiceStack.Jobs",
          "isValueType": true,
          "isEnum": true
        },
        {
          "name": "DurationMs",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "Attempts",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "ErrorCode",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ErrorMessage",
          "type": "String",
          "namespace": "System"
        }
      ]
    },
    {
      "name": "ScheduledTask",
      "namespace": "ServiceStack.Jobs",
      "implements": [],
      "properties": [
        {
          "name": "Id",
          "type": "Int64",
          "namespace": "System",
          "isValueType": true,
          "isPrimaryKey": true
        },
        {
          "name": "Name",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "Interval",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "TimeSpan"
          ]
        },
        {
          "name": "CronExpression",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RequestType",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "Command",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Request",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "RequestBody",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "Options",
          "type": "BackgroundJobOptions",
          "namespace": "ServiceStack.Jobs"
        },
        {
          "name": "LastRun",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "LastJobId",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int64"
          ]
        }
      ]
    },
    {
      "name": "CompletedJob",
      "namespace": "ServiceStack.Jobs",
      "inherits": {
        "name": "BackgroundJobBase",
        "namespace": "ServiceStack.Jobs"
      },
      "implements": [],
      "icon": {
        "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 2048 2048\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m603 685l-136-136l-659 659l-275-275l-136 136l411 411z\u0027/\u003E\u003C/svg\u003E"
      }
    },
    {
      "name": "FailedJob",
      "namespace": "ServiceStack.Jobs",
      "inherits": {
        "name": "BackgroundJobBase",
        "namespace": "ServiceStack.Jobs"
      },
      "implements": [],
      "icon": {
        "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 2048 2048\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m113 1024l342-342l-113-113l-342 342l-342-342l-113 113l342 342l-342 342l113 113l342-342l342 342l113-113z\u0027/\u003E\u003C/svg\u003E"
      }
    },
    {
      "name": "RequestLog",
      "namespace": "ServiceStack",
      "implements": [],
      "properties": [
        {
          "name": "Id",
          "type": "Int64",
          "namespace": "System",
          "isValueType": true,
          "isPrimaryKey": true
        },
        {
          "name": "TraceId",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "OperationName",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "DateTime",
          "type": "DateTime",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "StatusCode",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "StatusDescription",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "HttpMethod",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "AbsoluteUri",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "PathInfo",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Request",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RequestBody",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "UserAuthId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "SessionId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "IpAddress",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ForwardedFor",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Referer",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Headers",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ],
          "isRequired": true
        },
        {
          "name": "FormData",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ]
        },
        {
          "name": "Items",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ],
          "isRequired": true
        },
        {
          "name": "ResponseHeaders",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ]
        },
        {
          "name": "Response",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ResponseBody",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "SessionBody",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Error",
          "type": "ResponseStatus",
          "namespace": "ServiceStack"
        },
        {
          "name": "ExceptionSource",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ExceptionDataBody",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RequestDuration",
          "type": "TimeSpan",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "Meta",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ]
        }
      ]
    },
    {
      "name": "Property",
      "namespace": "ServiceStack",
      "implements": [],
      "dataContract": {},
      "properties": [
        {
          "name": "Name",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 1
          },
          "isRequired": true
        },
        {
          "name": "Value",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 2
          },
          "isRequired": true
        }
      ]
    },
    {
      "name": "ResponseStatus",
      "namespace": "ServiceStack",
      "implements": [],
      "dataContract": {},
      "properties": [
        {
          "name": "ErrorCode",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 1
          },
          "isRequired": true
        },
        {
          "name": "Message",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 2
          }
        },
        {
          "name": "StackTrace",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 3
          }
        },
        {
          "name": "Errors",
          "type": "List\u00601",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "ResponseError"
          ],
          "dataMember": {
            "order": 4
          }
        },
        {
          "name": "Meta",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ],
          "dataMember": {
            "order": 5
          }
        }
      ]
    },
    {
      "name": "QueryResponse\u00601",
      "namespace": "ServiceStack",
      "genericArgs": [
        "T"
      ],
      "implements": [],
      "isGenericTypeDef": true,
      "dataContract": {},
      "properties": [
        {
          "name": "Offset",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true,
          "dataMember": {
            "order": 1
          }
        },
        {
          "name": "Total",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true,
          "dataMember": {
            "order": 2
          }
        },
        {
          "name": "Results",
          "type": "List\u00601",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "T"
          ],
          "dataMember": {
            "order": 3
          },
          "isRequired": true
        },
        {
          "name": "Meta",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ],
          "dataMember": {
            "order": 4
          }
        },
        {
          "name": "ResponseStatus",
          "type": "ResponseStatus",
          "namespace": "ServiceStack",
          "dataMember": {
            "order": 5
          }
        }
      ]
    },
    {
      "name": "QueryBase",
      "namespace": "ServiceStack",
      "implements": [],
      "isAbstract": true,
      "dataContract": {},
      "properties": [
        {
          "name": "Skip",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int32"
          ],
          "dataMember": {
            "order": 1
          }
        },
        {
          "name": "Take",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int32"
          ],
          "dataMember": {
            "order": 2
          }
        },
        {
          "name": "OrderBy",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 3
          },
          "input": {
            "id": "OrderBy",
            "type": "tag",
            "required": false,
            "options": "{ allowableValues:$dataModelFields }"
          }
        },
        {
          "name": "OrderByDesc",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 4
          },
          "input": {
            "id": "OrderByDesc",
            "type": "tag",
            "required": false,
            "options": "{ allowableValues:$dataModelFields }"
          }
        },
        {
          "name": "Include",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 5
          },
          "input": {
            "id": "Include",
            "type": "tag",
            "required": false,
            "options": "{ allowableValues:[\u0027total\u0027] }"
          }
        },
        {
          "name": "Fields",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 6
          },
          "input": {
            "id": "Fields",
            "type": "tag",
            "required": false,
            "options": "{ allowableValues:$dataModelFields }",
            "css": {
              "field": "col-span-12"
            }
          }
        },
        {
          "name": "Meta",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ],
          "dataMember": {
            "order": 7
          }
        }
      ]
    },
    {
      "name": "BackgroundJobState",
      "namespace": "ServiceStack.Jobs",
      "implements": [],
      "isEnum": true,
      "enumNames": [
        "Queued",
        "Started",
        "Executed",
        "Completed",
        "Failed",
        "Cancelled"
      ]
    },
    {
      "name": "BackgroundJobBase",
      "namespace": "ServiceStack.Jobs",
      "implements": [],
      "isAbstract": true,
      "properties": [
        {
          "name": "Id",
          "type": "Int64",
          "namespace": "System",
          "isValueType": true,
          "isPrimaryKey": true
        },
        {
          "name": "ParentId",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int64"
          ]
        },
        {
          "name": "RefId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Worker",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Tag",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "BatchId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Callback",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "DependsOn",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int64"
          ]
        },
        {
          "name": "RunAfter",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "CreatedDate",
          "type": "DateTime",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "CreatedBy",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RequestId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RequestType",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "Command",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Request",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "RequestBody",
          "type": "String",
          "namespace": "System",
          "isRequired": true
        },
        {
          "name": "UserId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Response",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ResponseBody",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "State",
          "type": "BackgroundJobState",
          "namespace": "ServiceStack.Jobs",
          "isValueType": true,
          "isEnum": true
        },
        {
          "name": "StartedDate",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "CompletedDate",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "NotifiedDate",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "RetryLimit",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int32"
          ]
        },
        {
          "name": "Attempts",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "DurationMs",
          "type": "Int32",
          "namespace": "System",
          "isValueType": true
        },
        {
          "name": "TimeoutSecs",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int32"
          ]
        },
        {
          "name": "Progress",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Double"
          ]
        },
        {
          "name": "Status",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Logs",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "LastActivityDate",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "ReplyTo",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ErrorCode",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Error",
          "type": "ResponseStatus",
          "namespace": "ServiceStack"
        },
        {
          "name": "Args",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ]
        },
        {
          "name": "Meta",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ]
        }
      ]
    },
    {
      "name": "BackgroundJobOptions",
      "namespace": "ServiceStack.Jobs",
      "implements": [],
      "properties": [
        {
          "name": "RefId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "ParentId",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int64"
          ]
        },
        {
          "name": "Worker",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RunAfter",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "DateTime"
          ]
        },
        {
          "name": "Callback",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "DependsOn",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int64"
          ]
        },
        {
          "name": "UserId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "RetryLimit",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int32"
          ]
        },
        {
          "name": "ReplyTo",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "Tag",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "BatchId",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "CreatedBy",
          "type": "String",
          "namespace": "System"
        },
        {
          "name": "TimeoutSecs",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Int32"
          ]
        },
        {
          "name": "Timeout",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "TimeSpan"
          ]
        },
        {
          "name": "Args",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ]
        },
        {
          "name": "RunCommand",
          "type": "Nullable\u00601",
          "namespace": "System",
          "isValueType": true,
          "genericArgs": [
            "Boolean"
          ]
        }
      ]
    },
    {
      "name": "ResponseError",
      "namespace": "ServiceStack",
      "implements": [],
      "dataContract": {},
      "properties": [
        {
          "name": "ErrorCode",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 1
          },
          "isRequired": true
        },
        {
          "name": "FieldName",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 2
          },
          "isRequired": true
        },
        {
          "name": "Message",
          "type": "String",
          "namespace": "System",
          "dataMember": {
            "order": 3
          },
          "isRequired": true
        },
        {
          "name": "Meta",
          "type": "Dictionary\u00602",
          "namespace": "System.Collections.Generic",
          "genericArgs": [
            "String",
            "String"
          ],
          "dataMember": {
            "order": 4
          }
        }
      ]
    }
  ],
  "operations": [
    {
      "request": {
        "name": "AdminQueryBackgroundJobs",
        "namespace": "ServiceStack",
        "inherits": {
          "name": "QueryDb\u00601",
          "namespace": "ServiceStack",
          "genericArgs": [
            "BackgroundJob"
          ]
        },
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 32 32\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M10.293 5.293L7 8.586L5.707 7.293L4.293 8.707L7 11.414l4.707-4.707zM14 7v2h14V7zm0 8v2h14v-2zm0 8v2h14v-2z\u0027/\u003E\u003C/svg\u003E"
        },
        "properties": [
          {
            "name": "Id",
            "type": "Nullable\u00601",
            "namespace": "System",
            "isValueType": true,
            "isPrimaryKey": true,
            "genericArgs": [
              "Int32"
            ]
          },
          {
            "name": "RefId",
            "type": "String",
            "namespace": "System"
          }
        ]
      },
      "response": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "T"
        ],
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 32 32\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M10.293 5.293L7 8.586L5.707 7.293L4.293 8.707L7 11.414l4.707-4.707zM14 7v2h14V7zm0 8v2h14v-2zm0 8v2h14v-2z\u0027/\u003E\u003C/svg\u003E"
        },
        "dataContract": {},
        "properties": [
          {
            "name": "Offset",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 1
            }
          },
          {
            "name": "Total",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "Results",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "BackgroundJob"
            ],
            "dataMember": {
              "order": 3
            },
            "isRequired": true
          },
          {
            "name": "Meta",
            "type": "Dictionary\u00602",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "String",
              "String"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "GET",
      "returnType": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "BackgroundJob"
        ]
      },
      "dataModel": {
        "name": "BackgroundJob",
        "namespace": "ServiceStack.Jobs"
      },
      "viewModel": {
        "name": "BackgroundJob",
        "namespace": "ServiceStack.Jobs"
      },
      "tags": [
        "jobs"
      ]
    },
    {
      "request": {
        "name": "AdminQueryJobSummary",
        "namespace": "ServiceStack",
        "inherits": {
          "name": "QueryDb\u00601",
          "namespace": "ServiceStack",
          "genericArgs": [
            "JobSummary"
          ]
        },
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 24 24\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M21 21H3v-2h18zM8 3H4v14h4zm6 3h-4v11h4zm6 4h-4v7h4z\u0027/\u003E\u003C/svg\u003E"
        },
        "properties": [
          {
            "name": "Id",
            "type": "Nullable\u00601",
            "namespace": "System",
            "isValueType": true,
            "isPrimaryKey": true,
            "genericArgs": [
              "Int32"
            ]
          },
          {
            "name": "RefId",
            "type": "String",
            "namespace": "System"
          }
        ]
      },
      "response": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "T"
        ],
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 24 24\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M21 21H3v-2h18zM8 3H4v14h4zm6 3h-4v11h4zm6 4h-4v7h4z\u0027/\u003E\u003C/svg\u003E"
        },
        "dataContract": {},
        "properties": [
          {
            "name": "Offset",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 1
            }
          },
          {
            "name": "Total",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "Results",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "JobSummary"
            ],
            "dataMember": {
              "order": 3
            },
            "isRequired": true
          },
          {
            "name": "Meta",
            "type": "Dictionary\u00602",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "String",
              "String"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "GET",
      "returnType": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "JobSummary"
        ]
      },
      "dataModel": {
        "name": "JobSummary",
        "namespace": "ServiceStack.Jobs"
      },
      "viewModel": {
        "name": "JobSummary",
        "namespace": "ServiceStack.Jobs"
      },
      "tags": [
        "jobs"
      ]
    },
    {
      "request": {
        "name": "AdminQueryScheduledTasks",
        "namespace": "ServiceStack",
        "inherits": {
          "name": "QueryDb\u00601",
          "namespace": "ServiceStack",
          "genericArgs": [
            "ScheduledTask"
          ]
        },
        "implements": []
      },
      "response": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "T"
        ],
        "implements": [],
        "dataContract": {},
        "properties": [
          {
            "name": "Offset",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 1
            }
          },
          {
            "name": "Total",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "Results",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "ScheduledTask"
            ],
            "dataMember": {
              "order": 3
            },
            "isRequired": true
          },
          {
            "name": "Meta",
            "type": "Dictionary\u00602",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "String",
              "String"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "GET",
      "returnType": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "ScheduledTask"
        ]
      },
      "dataModel": {
        "name": "ScheduledTask",
        "namespace": "ServiceStack.Jobs"
      },
      "viewModel": {
        "name": "ScheduledTask",
        "namespace": "ServiceStack.Jobs"
      },
      "tags": [
        "jobs"
      ]
    },
    {
      "request": {
        "name": "AdminQueryCompletedJobs",
        "namespace": "ServiceStack",
        "inherits": {
          "name": "QueryDb\u00601",
          "namespace": "ServiceStack",
          "genericArgs": [
            "CompletedJob"
          ]
        },
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 2048 2048\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m603 685l-136-136l-659 659l-275-275l-136 136l411 411z\u0027/\u003E\u003C/svg\u003E"
        },
        "properties": [
          {
            "name": "Month",
            "type": "Nullable\u00601",
            "namespace": "System",
            "isValueType": true,
            "genericArgs": [
              "DateTime"
            ]
          }
        ]
      },
      "response": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "T"
        ],
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 2048 2048\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m603 685l-136-136l-659 659l-275-275l-136 136l411 411z\u0027/\u003E\u003C/svg\u003E"
        },
        "dataContract": {},
        "properties": [
          {
            "name": "Offset",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 1
            }
          },
          {
            "name": "Total",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "Results",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "CompletedJob"
            ],
            "dataMember": {
              "order": 3
            },
            "isRequired": true
          },
          {
            "name": "Meta",
            "type": "Dictionary\u00602",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "String",
              "String"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "GET",
      "returnType": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "CompletedJob"
        ]
      },
      "dataModel": {
        "name": "CompletedJob",
        "namespace": "ServiceStack.Jobs"
      },
      "viewModel": {
        "name": "CompletedJob",
        "namespace": "ServiceStack.Jobs"
      },
      "tags": [
        "jobs"
      ]
    },
    {
      "request": {
        "name": "AdminQueryFailedJobs",
        "namespace": "ServiceStack",
        "inherits": {
          "name": "QueryDb\u00601",
          "namespace": "ServiceStack",
          "genericArgs": [
            "FailedJob"
          ]
        },
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 2048 2048\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m113 1024l342-342l-113-113l-342 342l-342-342l-113 113l342 342l-342 342l113 113l342-342l342 342l113-113z\u0027/\u003E\u003C/svg\u003E"
        },
        "properties": [
          {
            "name": "Month",
            "type": "Nullable\u00601",
            "namespace": "System",
            "isValueType": true,
            "genericArgs": [
              "DateTime"
            ]
          }
        ]
      },
      "response": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "T"
        ],
        "implements": [],
        "icon": {
          "svg": "\u003Csvg xmlns=\u0027http://www.w3.org/2000/svg\u0027 width=\u00271em\u0027 height=\u00271em\u0027 viewBox=\u00270 0 2048 2048\u0027\u003E\u003Cpath fill=\u0027currentColor\u0027 d=\u0027M1024 0q141 0 272 36t244 104t207 160t161 207t103 245t37 272q0 141-36 272t-104 244t-160 207t-207 161t-245 103t-272 37q-141 0-272-36t-244-104t-207-160t-161-207t-103-245t-37-272q0-141 36-272t104-244t160-207t207-161T752 37t272-37m113 1024l342-342l-113-113l-342 342l-342-342l-113 113l342 342l-342 342l113 113l342-342l342 342l113-113z\u0027/\u003E\u003C/svg\u003E"
        },
        "dataContract": {},
        "properties": [
          {
            "name": "Offset",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 1
            }
          },
          {
            "name": "Total",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "Results",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "FailedJob"
            ],
            "dataMember": {
              "order": 3
            },
            "isRequired": true
          },
          {
            "name": "Meta",
            "type": "Dictionary\u00602",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "String",
              "String"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "GET",
      "returnType": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "FailedJob"
        ]
      },
      "dataModel": {
        "name": "FailedJob",
        "namespace": "ServiceStack.Jobs"
      },
      "viewModel": {
        "name": "FailedJob",
        "namespace": "ServiceStack.Jobs"
      },
      "tags": [
        "jobs"
      ]
    },
    {
      "request": {
        "name": "AdminQueryRequestLogs",
        "namespace": "ServiceStack",
        "inherits": {
          "name": "QueryDb\u00601",
          "namespace": "ServiceStack",
          "genericArgs": [
            "RequestLog"
          ]
        },
        "implements": [],
        "properties": [
          {
            "name": "Month",
            "type": "Nullable\u00601",
            "namespace": "System",
            "isValueType": true,
            "genericArgs": [
              "DateTime"
            ]
          }
        ]
      },
      "response": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "T"
        ],
        "implements": [],
        "dataContract": {},
        "properties": [
          {
            "name": "Offset",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 1
            }
          },
          {
            "name": "Total",
            "type": "Int32",
            "namespace": "System",
            "isValueType": true,
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "Results",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "RequestLog"
            ],
            "dataMember": {
              "order": 3
            },
            "isRequired": true
          },
          {
            "name": "Meta",
            "type": "Dictionary\u00602",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "String",
              "String"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "actions": [],
      "method": "GET",
      "returnType": {
        "name": "QueryResponse\u00601",
        "namespace": "ServiceStack",
        "genericArgs": [
          "RequestLog"
        ]
      },
      "dataModel": {
        "name": "RequestLog",
        "namespace": "ServiceStack"
      },
      "viewModel": {
        "name": "RequestLog",
        "namespace": "ServiceStack"
      },
      "tags": [
        "admin"
      ]
    },
    {
      "request": {
        "name": "AdminCreateRole",
        "namespace": "ServiceStack",
        "implements": [],
        "dataContract": {},
        "properties": [
          {
            "name": "Name",
            "type": "String",
            "namespace": "System",
            "dataMember": {
              "order": 1
            }
          }
        ]
      },
      "response": {
        "name": "IdResponse",
        "namespace": "ServiceStack",
        "implements": [],
        "dataContract": {},
        "properties": [
          {
            "name": "Id",
            "type": "String",
            "namespace": "System",
            "isPrimaryKey": true,
            "dataMember": {
              "order": 1
            },
            "isRequired": true
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 2
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "POST",
      "returnType": {
        "name": "IdResponse",
        "namespace": "ServiceStack"
      },
      "tags": [
        "admin"
      ]
    },
    {
      "request": {
        "name": "AdminUpdateRole",
        "namespace": "ServiceStack",
        "implements": [],
        "dataContract": {},
        "properties": [
          {
            "name": "Id",
            "type": "String",
            "namespace": "System",
            "isPrimaryKey": true,
            "dataMember": {
              "order": 1
            },
            "input": {
              "id": "Id",
              "type": "text",
              "readOnly": true
            }
          },
          {
            "name": "Name",
            "type": "String",
            "namespace": "System",
            "dataMember": {
              "order": 2
            }
          },
          {
            "name": "AddClaims",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "Property"
            ],
            "dataMember": {
              "order": 3
            }
          },
          {
            "name": "RemoveClaims",
            "type": "List\u00601",
            "namespace": "System.Collections.Generic",
            "genericArgs": [
              "Property"
            ],
            "dataMember": {
              "order": 4
            }
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 5
            }
          }
        ]
      },
      "response": {
        "name": "IdResponse",
        "namespace": "ServiceStack",
        "implements": [],
        "dataContract": {},
        "properties": [
          {
            "name": "Id",
            "type": "String",
            "namespace": "System",
            "isPrimaryKey": true,
            "dataMember": {
              "order": 1
            },
            "isRequired": true
          },
          {
            "name": "ResponseStatus",
            "type": "ResponseStatus",
            "namespace": "ServiceStack",
            "dataMember": {
              "order": 2
            }
          }
        ]
      },
      "actions": [
        "ANY"
      ],
      "method": "POST",
      "returnType": {
        "name": "IdResponse",
        "namespace": "ServiceStack"
      },
      "tags": [
        "admin"
      ]
    }
  ]
}
