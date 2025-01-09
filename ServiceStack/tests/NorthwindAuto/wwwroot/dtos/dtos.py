""" Options:
Date: 2025-01-10 00:18:07
Version: 8.53
Tip: To override a DTO option, remove "#" prefix before updating
BaseUrl: http://localhost:20000

#GlobalNamespace: 
#AddServiceStackTypes: True
#AddResponseStatus: False
#AddImplicitVersion: 
#AddDescriptionAsComments: True
#IncludeTypes: 
#ExcludeTypes: 
#DefaultImports: datetime,decimal,marshmallow.fields:*,servicestack:*,typing:*,dataclasses:dataclass/field,dataclasses_json:dataclass_json/LetterCase/Undefined/config,enum:Enum/IntEnum
#DataClass: 
#DataClassJson: 
"""

import datetime
import decimal
from marshmallow.fields import *
from servicestack import *
from typing import *
from dataclasses import dataclass, field
from dataclasses_json import dataclass_json, LetterCase, Undefined, config
from enum import Enum, IntEnum


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUserBase:
    user_name: Optional[str] = None
    first_name: Optional[str] = None
    last_name: Optional[str] = None
    display_name: Optional[str] = None
    email: Optional[str] = None
    password: Optional[str] = None
    profile_url: Optional[str] = None
    phone_number: Optional[str] = None
    user_auth_properties: Optional[Dict[str, str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RequestLog:
    id: int = 0
    trace_id: Optional[str] = None
    operation_name: Optional[str] = None
    date_time: datetime.datetime = datetime.datetime(1, 1, 1)
    status_code: int = 0
    status_description: Optional[str] = None
    http_method: Optional[str] = None
    absolute_uri: Optional[str] = None
    path_info: Optional[str] = None
    request: Optional[str] = None
    # @StringLength(2147483647)
    request_body: Optional[str] = None

    user_auth_id: Optional[str] = None
    session_id: Optional[str] = None
    ip_address: Optional[str] = None
    forwarded_for: Optional[str] = None
    referer: Optional[str] = None
    headers: Dict[str, str] = field(default_factory=dict)
    form_data: Optional[Dict[str, str]] = None
    items: Dict[str, str] = field(default_factory=dict)
    response_headers: Optional[Dict[str, str]] = None
    response: Optional[str] = None
    response_body: Optional[str] = None
    session_body: Optional[str] = None
    error: Optional[ResponseStatus] = None
    exception_source: Optional[str] = None
    exception_data_body: Optional[str] = None
    request_duration: datetime.timedelta = datetime.timedelta()
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RedisEndpointInfo:
    host: Optional[str] = None
    port: int = 0
    ssl: Optional[bool] = None
    db: int = 0
    username: Optional[str] = None
    password: Optional[str] = None


class BackgroundJobState(str, Enum):
    QUEUED = 'Queued'
    STARTED = 'Started'
    EXECUTED = 'Executed'
    COMPLETED = 'Completed'
    FAILED = 'Failed'
    CANCELLED = 'Cancelled'


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class BackgroundJobBase:
    id: int = 0
    parent_id: Optional[int] = None
    ref_id: Optional[str] = None
    worker: Optional[str] = None
    tag: Optional[str] = None
    batch_id: Optional[str] = None
    callback: Optional[str] = None
    depends_on: Optional[int] = None
    run_after: Optional[datetime.datetime] = None
    created_date: datetime.datetime = datetime.datetime(1, 1, 1)
    created_by: Optional[str] = None
    request_id: Optional[str] = None
    request_type: Optional[str] = None
    command: Optional[str] = None
    request: Optional[str] = None
    request_body: Optional[str] = None
    user_id: Optional[str] = None
    response: Optional[str] = None
    response_body: Optional[str] = None
    state: Optional[BackgroundJobState] = None
    started_date: Optional[datetime.datetime] = None
    completed_date: Optional[datetime.datetime] = None
    notified_date: Optional[datetime.datetime] = None
    retry_limit: Optional[int] = None
    attempts: int = 0
    duration_ms: int = 0
    timeout_secs: Optional[int] = None
    progress: Optional[float] = None
    status: Optional[str] = None
    logs: Optional[str] = None
    last_activity_date: Optional[datetime.datetime] = None
    reply_to: Optional[str] = None
    error_code: Optional[str] = None
    error: Optional[ResponseStatus] = None
    args: Optional[Dict[str, str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class BackgroundJob(BackgroundJobBase):
    id: int = 0


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class JobSummary:
    id: int = 0
    parent_id: Optional[int] = None
    ref_id: Optional[str] = None
    worker: Optional[str] = None
    tag: Optional[str] = None
    batch_id: Optional[str] = None
    created_date: datetime.datetime = datetime.datetime(1, 1, 1)
    created_by: Optional[str] = None
    request_type: Optional[str] = None
    command: Optional[str] = None
    request: Optional[str] = None
    response: Optional[str] = None
    user_id: Optional[str] = None
    callback: Optional[str] = None
    started_date: Optional[datetime.datetime] = None
    completed_date: Optional[datetime.datetime] = None
    state: Optional[BackgroundJobState] = None
    duration_ms: int = 0
    attempts: int = 0
    error_code: Optional[str] = None
    error_message: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class BackgroundJobOptions:
    ref_id: Optional[str] = None
    parent_id: Optional[int] = None
    worker: Optional[str] = None
    run_after: Optional[datetime.datetime] = None
    callback: Optional[str] = None
    depends_on: Optional[int] = None
    user_id: Optional[str] = None
    retry_limit: Optional[int] = None
    reply_to: Optional[str] = None
    tag: Optional[str] = None
    batch_id: Optional[str] = None
    created_by: Optional[str] = None
    timeout_secs: Optional[int] = None
    timeout: Optional[datetime.timedelta] = None
    args: Optional[Dict[str, str]] = None
    run_command: Optional[bool] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ScheduledTask:
    id: int = 0
    name: Optional[str] = None
    interval: Optional[datetime.timedelta] = None
    cron_expression: Optional[str] = None
    request_type: Optional[str] = None
    command: Optional[str] = None
    request: Optional[str] = None
    request_body: Optional[str] = None
    options: Optional[BackgroundJobOptions] = None
    last_run: Optional[datetime.datetime] = None
    last_job_id: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class CompletedJob(BackgroundJobBase):
    pass


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class FailedJob(BackgroundJobBase):
    pass


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ValidateRule:
    validator: Optional[str] = None
    condition: Optional[str] = None
    error_code: Optional[str] = None
    message: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ValidationRule(ValidateRule):
    id: int = 0
    # @Required()
    type: Optional[str] = None

    field: Optional[str] = None
    created_by: Optional[str] = None
    created_date: Optional[datetime.datetime] = None
    modified_by: Optional[str] = None
    modified_date: Optional[datetime.datetime] = None
    suspended_by: Optional[str] = None
    suspended_date: Optional[datetime.datetime] = None
    notes: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AppInfo:
    base_url: Optional[str] = None
    service_stack_version: Optional[str] = None
    service_name: Optional[str] = None
    api_version: Optional[str] = None
    service_description: Optional[str] = None
    service_icon_url: Optional[str] = None
    brand_url: Optional[str] = None
    brand_image_url: Optional[str] = None
    text_color: Optional[str] = None
    link_color: Optional[str] = None
    background_color: Optional[str] = None
    background_image_url: Optional[str] = None
    icon_url: Optional[str] = None
    js_text_case: Optional[str] = None
    use_system_json: Optional[str] = None
    endpoint_routing: Optional[List[str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ImageInfo:
    svg: Optional[str] = None
    uri: Optional[str] = None
    alt: Optional[str] = None
    cls: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class LinkInfo:
    id: Optional[str] = None
    href: Optional[str] = None
    label: Optional[str] = None
    icon: Optional[ImageInfo] = None
    show: Optional[str] = None
    hide: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ThemeInfo:
    form: Optional[str] = None
    model_icon: Optional[ImageInfo] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ApiCss:
    form: Optional[str] = None
    fieldset: Optional[str] = None
    field: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AppTags:
    default: Optional[str] = None
    other: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class LocodeUi:
    css: Optional[ApiCss] = None
    tags: Optional[AppTags] = None
    max_field_length: int = 0
    max_nested_fields: int = 0
    max_nested_field_length: int = 0


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ExplorerUi:
    css: Optional[ApiCss] = None
    tags: Optional[AppTags] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUi:
    css: Optional[ApiCss] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class FormatInfo:
    method: Optional[str] = None
    options: Optional[str] = None
    locale: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ApiFormat:
    locale: Optional[str] = None
    assume_utc: bool = False
    number: Optional[FormatInfo] = None
    date: Optional[FormatInfo] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class UiInfo:
    brand_icon: Optional[ImageInfo] = None
    hide_tags: Optional[List[str]] = None
    modules: Optional[List[str]] = None
    always_hide_tags: Optional[List[str]] = None
    admin_links: Optional[List[LinkInfo]] = None
    theme: Optional[ThemeInfo] = None
    locode: Optional[LocodeUi] = None
    explorer: Optional[ExplorerUi] = None
    admin: Optional[AdminUi] = None
    default_formats: Optional[ApiFormat] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ConfigInfo:
    debug_mode: Optional[bool] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class FieldCss:
    field: Optional[str] = None
    input: Optional[str] = None
    label: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class InputInfo:
    id: Optional[str] = None
    name: Optional[str] = None
    type: Optional[str] = None
    value: Optional[str] = None
    placeholder: Optional[str] = None
    help: Optional[str] = None
    label: Optional[str] = None
    title: Optional[str] = None
    size: Optional[str] = None
    pattern: Optional[str] = None
    read_only: Optional[bool] = None
    required: Optional[bool] = None
    disabled: Optional[bool] = None
    autocomplete: Optional[str] = None
    autofocus: Optional[str] = None
    min: Optional[str] = None
    max: Optional[str] = None
    step: Optional[str] = None
    min_length: Optional[int] = None
    max_length: Optional[int] = None
    accept: Optional[str] = None
    capture: Optional[str] = None
    multiple: Optional[bool] = None
    allowable_values: Optional[List[str]] = None
    allowable_entries: Optional[List[KeyValuePair][str, str]] = None
    options: Optional[str] = None
    ignore: Optional[bool] = None
    css: Optional[FieldCss] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetaAuthProvider:
    name: Optional[str] = None
    label: Optional[str] = None
    type: Optional[str] = None
    nav_item: Optional[NavItem] = None
    icon: Optional[ImageInfo] = None
    form_layout: Optional[List[InputInfo]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class IdentityAuthInfo:
    has_refresh_token: Optional[bool] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AuthInfo:
    has_auth_secret: Optional[bool] = None
    has_auth_repository: Optional[bool] = None
    includes_roles: Optional[bool] = None
    includes_o_auth_tokens: Optional[bool] = None
    html_redirect: Optional[str] = None
    auth_providers: Optional[List[MetaAuthProvider]] = None
    identity_auth: Optional[IdentityAuthInfo] = None
    role_links: Optional[Dict[str, List[LinkInfo]]] = None
    service_routes: Optional[Dict[str, List[str]]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ApiKeyInfo:
    label: Optional[str] = None
    http_header: Optional[str] = None
    scopes: Optional[List[str]] = None
    features: Optional[List[str]] = None
    request_types: Optional[List[str]] = None
    expires_in: Optional[List[KeyValuePair[str, str]]] = None
    hide: Optional[List[str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataTypeName:
    name: Optional[str] = None
    namespace: Optional[str] = None
    generic_args: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataDataContract:
    name: Optional[str] = None
    namespace: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataDataMember:
    name: Optional[str] = None
    order: Optional[int] = None
    is_required: Optional[bool] = None
    emit_default_value: Optional[bool] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataAttribute:
    name: Optional[str] = None
    constructor_args: Optional[List[MetadataPropertyType]] = None
    args: Optional[List[MetadataPropertyType]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RefInfo:
    model: Optional[str] = None
    self_id: Optional[str] = None
    ref_id: Optional[str] = None
    ref_label: Optional[str] = None
    query_api: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataPropertyType:
    name: Optional[str] = None
    type: Optional[str] = None
    namespace: Optional[str] = None
    is_value_type: Optional[bool] = None
    is_enum: Optional[bool] = None
    is_primary_key: Optional[bool] = None
    generic_args: Optional[List[str]] = None
    value: Optional[str] = None
    description: Optional[str] = None
    data_member: Optional[MetadataDataMember] = None
    read_only: Optional[bool] = None
    param_type: Optional[str] = None
    display_type: Optional[str] = None
    is_required: Optional[bool] = None
    allowable_values: Optional[List[str]] = None
    allowable_min: Optional[int] = None
    allowable_max: Optional[int] = None
    attributes: Optional[List[MetadataAttribute]] = None
    upload_to: Optional[str] = None
    input: Optional[InputInfo] = None
    format: Optional[FormatInfo] = None
    ref: Optional[RefInfo] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataType:
    name: Optional[str] = None
    namespace: Optional[str] = None
    generic_args: Optional[List[str]] = None
    inherits: Optional[MetadataTypeName] = None
    implements: Optional[List[MetadataTypeName]] = None
    display_type: Optional[str] = None
    description: Optional[str] = None
    notes: Optional[str] = None
    icon: Optional[ImageInfo] = None
    is_nested: Optional[bool] = None
    is_enum: Optional[bool] = None
    is_enum_int: Optional[bool] = None
    is_interface: Optional[bool] = None
    is_abstract: Optional[bool] = None
    is_generic_type_def: Optional[bool] = None
    data_contract: Optional[MetadataDataContract] = None
    properties: Optional[List[MetadataPropertyType]] = None
    attributes: Optional[List[MetadataAttribute]] = None
    inner_types: Optional[List[MetadataTypeName]] = None
    enum_names: Optional[List[str]] = None
    enum_values: Optional[List[str]] = None
    enum_member_values: Optional[List[str]] = None
    enum_descriptions: Optional[List[str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class CommandInfo:
    name: Optional[str] = None
    tag: Optional[str] = None
    request: Optional[MetadataType] = None
    response: Optional[MetadataType] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class CommandsInfo:
    commands: Optional[List[CommandInfo]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AutoQueryConvention:
    name: Optional[str] = None
    value: Optional[str] = None
    types: Optional[str] = None
    value_type: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AutoQueryInfo:
    max_limit: Optional[int] = None
    untyped_queries: Optional[bool] = None
    raw_sql_filters: Optional[bool] = None
    auto_query_viewer: Optional[bool] = None
    async: Optional[bool] = None
    order_by_primary_key: Optional[bool] = None
    crud_events: Optional[bool] = None
    crud_events_services: Optional[bool] = None
    access_role: Optional[str] = None
    named_connection: Optional[str] = None
    viewer_conventions: Optional[List[AutoQueryConvention]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ScriptMethodType:
    name: Optional[str] = None
    param_names: Optional[List[str]] = None
    param_types: Optional[List[str]] = None
    return_type: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ValidationInfo:
    has_validation_source: Optional[bool] = None
    has_validation_source_admin: Optional[bool] = None
    service_routes: Optional[Dict[str, List[str]]] = None
    type_validators: Optional[List[ScriptMethodType]] = None
    property_validators: Optional[List[ScriptMethodType]] = None
    access_role: Optional[str] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class SharpPagesInfo:
    api_path: Optional[str] = None
    script_admin_role: Optional[str] = None
    metadata_debug_admin_role: Optional[str] = None
    metadata_debug: Optional[bool] = None
    spa_fallback: Optional[bool] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RequestLogsInfo:
    access_role: Optional[str] = None
    request_logger: Optional[str] = None
    default_limit: int = 0
    service_routes: Optional[Dict[str, List[str]]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ProfilingInfo:
    access_role: Optional[str] = None
    default_limit: int = 0
    summary_fields: Optional[List[str]] = None
    tag_label: Optional[str] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class FilesUploadLocation:
    name: Optional[str] = None
    read_access_role: Optional[str] = None
    write_access_role: Optional[str] = None
    allow_extensions: Optional[List[str]] = None
    allow_operations: Optional[str] = None
    max_file_count: Optional[int] = None
    min_file_bytes: Optional[int] = None
    max_file_bytes: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class FilesUploadInfo:
    base_path: Optional[str] = None
    locations: Optional[List[FilesUploadLocation]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MediaRule:
    size: Optional[str] = None
    rule: Optional[str] = None
    apply_to: Optional[List[str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUsersInfo:
    access_role: Optional[str] = None
    enabled: Optional[List[str]] = None
    user_auth: Optional[MetadataType] = None
    all_roles: Optional[List[str]] = None
    all_permissions: Optional[List[str]] = None
    query_user_auth_properties: Optional[List[str]] = None
    query_media_rules: Optional[List[MediaRule]] = None
    form_layout: Optional[List[InputInfo]] = None
    css: Optional[ApiCss] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminIdentityUsersInfo:
    access_role: Optional[str] = None
    enabled: Optional[List[str]] = None
    identity_user: Optional[MetadataType] = None
    all_roles: Optional[List[str]] = None
    all_permissions: Optional[List[str]] = None
    query_identity_user_properties: Optional[List[str]] = None
    query_media_rules: Optional[List[MediaRule]] = None
    form_layout: Optional[List[InputInfo]] = None
    css: Optional[ApiCss] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminRedisInfo:
    query_limit: int = 0
    databases: Optional[List[int]] = None
    modifiable_connection: Optional[bool] = None
    endpoint: Optional[RedisEndpointInfo] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class SchemaInfo:
    alias: Optional[str] = None
    name: Optional[str] = None
    tables: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class DatabaseInfo:
    alias: Optional[str] = None
    name: Optional[str] = None
    schemas: Optional[List[SchemaInfo]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDatabaseInfo:
    query_limit: int = 0
    databases: Optional[List[DatabaseInfo]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class PluginInfo:
    loaded: Optional[List[str]] = None
    auth: Optional[AuthInfo] = None
    api_key: Optional[ApiKeyInfo] = None
    commands: Optional[CommandsInfo] = None
    auto_query: Optional[AutoQueryInfo] = None
    validation: Optional[ValidationInfo] = None
    sharp_pages: Optional[SharpPagesInfo] = None
    request_logs: Optional[RequestLogsInfo] = None
    profiling: Optional[ProfilingInfo] = None
    files_upload: Optional[FilesUploadInfo] = None
    admin_users: Optional[AdminUsersInfo] = None
    admin_identity_users: Optional[AdminIdentityUsersInfo] = None
    admin_redis: Optional[AdminRedisInfo] = None
    admin_database: Optional[AdminDatabaseInfo] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class CustomPluginInfo:
    access_role: Optional[str] = None
    service_routes: Optional[Dict[str, List[str]]] = None
    enabled: Optional[List[str]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataTypesConfig:
    base_url: Optional[str] = None
    use_path: Optional[str] = None
    make_partial: bool = False
    make_virtual: bool = False
    make_internal: bool = False
    base_class: Optional[str] = None
    package: Optional[str] = None
    add_return_marker: bool = False
    add_description_as_comments: bool = False
    add_doc_annotations: bool = False
    add_data_contract_attributes: bool = False
    add_indexes_to_data_members: bool = False
    add_generated_code_attributes: bool = False
    add_implicit_version: Optional[int] = None
    add_response_status: bool = False
    add_service_stack_types: bool = False
    add_model_extensions: bool = False
    add_property_accessors: bool = False
    exclude_generic_base_types: bool = False
    setters_return_this: bool = False
    add_nullable_annotations: bool = False
    make_properties_optional: bool = False
    export_as_types: bool = False
    exclude_implemented_interfaces: bool = False
    add_default_xml_namespace: Optional[str] = None
    make_data_contracts_extensible: bool = False
    initialize_collections: bool = False
    add_namespaces: Optional[List[str]] = None
    default_namespaces: Optional[List[str]] = None
    default_imports: Optional[List[str]] = None
    include_types: Optional[List[str]] = None
    exclude_types: Optional[List[str]] = None
    export_tags: Optional[List[str]] = None
    treat_types_as_strings: Optional[List[str]] = None
    export_value_types: bool = False
    global_namespace: Optional[str] = None
    exclude_namespace: bool = False
    data_class: Optional[str] = None
    data_class_json: Optional[str] = None
    ignore_types: Optional[List[str]] = None
    export_types: Optional[List[str]] = None
    export_attributes: Optional[List[str]] = None
    ignore_types_in_namespaces: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataRoute:
    path: Optional[str] = None
    verbs: Optional[str] = None
    notes: Optional[str] = None
    summary: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ApiUiInfo:
    locode_css: Optional[ApiCss] = None
    explorer_css: Optional[ApiCss] = None
    form_layout: Optional[List[InputInfo]] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataOperationType:
    request: Optional[MetadataType] = None
    response: Optional[MetadataType] = None
    actions: Optional[List[str]] = None
    returns_void: Optional[bool] = None
    method: Optional[str] = None
    return_type: Optional[MetadataTypeName] = None
    routes: Optional[List[MetadataRoute]] = None
    data_model: Optional[MetadataTypeName] = None
    view_model: Optional[MetadataTypeName] = None
    requires_auth: Optional[bool] = None
    requires_api_key: Optional[bool] = None
    required_roles: Optional[List[str]] = None
    requires_any_role: Optional[List[str]] = None
    required_permissions: Optional[List[str]] = None
    requires_any_permission: Optional[List[str]] = None
    tags: Optional[List[str]] = None
    ui: Optional[ApiUiInfo] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class MetadataTypes:
    config: Optional[MetadataTypesConfig] = None
    namespaces: Optional[List[str]] = None
    types: Optional[List[MetadataType]] = None
    operations: Optional[List[MetadataOperationType]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ServerStats:
    redis: Optional[Dict[str, int]] = None
    server_events: Optional[Dict[str, str]] = None
    mq_description: Optional[str] = None
    mq_workers: Optional[Dict[str, int]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class DiagnosticEntry:
    id: int = 0
    trace_id: Optional[str] = None
    source: Optional[str] = None
    event_type: Optional[str] = None
    message: Optional[str] = None
    operation: Optional[str] = None
    thread_id: int = 0
    error: Optional[ResponseStatus] = None
    command_type: Optional[str] = None
    command: Optional[str] = None
    user_auth_id: Optional[str] = None
    session_id: Optional[str] = None
    arg: Optional[str] = None
    args: Optional[List[str]] = None
    arg_lengths: Optional[List[int]] = None
    named_args: Optional[Dict[str, Object]] = None
    duration: Optional[datetime.timedelta] = None
    timestamp: int = 0
    date: datetime.datetime = datetime.datetime(1, 1, 1)
    tag: Optional[str] = None
    stack_trace: Optional[str] = None
    meta: Dict[str, str] = field(default_factory=dict)


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RedisSearchResult:
    id: Optional[str] = None
    type: Optional[str] = None
    ttl: int = 0
    size: int = 0


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RedisText:
    text: Optional[str] = None
    children: Optional[List[RedisText]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class CommandSummary:
    type: Optional[str] = None
    name: Optional[str] = None
    count: int = 0
    failed: int = 0
    retries: int = 0
    total_ms: int = 0
    min_ms: int = 0
    max_ms: int = 0
    average_ms: float = 0.0
    median_ms: float = 0.0
    last_error: Optional[ResponseStatus] = None
    timings: Optional[ConcurrentQueue[int]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class CommandResult:
    type: Optional[str] = None
    name: Optional[str] = None
    ms: Optional[int] = None
    at: datetime.datetime = datetime.datetime(1, 1, 1)
    request: Optional[str] = None
    retries: Optional[int] = None
    attempt: int = 0
    error: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class PartialApiKey:
    id: int = 0
    name: Optional[str] = None
    user_id: Optional[str] = None
    user_name: Optional[str] = None
    visible_key: Optional[str] = None
    environment: Optional[str] = None
    created_date: datetime.datetime = datetime.datetime(1, 1, 1)
    expiry_date: Optional[datetime.datetime] = None
    cancelled_date: Optional[datetime.datetime] = None
    last_used_date: Optional[datetime.datetime] = None
    scopes: Optional[List[str]] = None
    features: Optional[List[str]] = None
    restrict_to: Optional[List[str]] = None
    notes: Optional[str] = None
    ref_id: Optional[int] = None
    ref_id_str: Optional[str] = None
    meta: Optional[Dict[str, str]] = None
    active: bool = False


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class JobStatSummary:
    name: Optional[str] = None
    total: int = 0
    completed: int = 0
    retries: int = 0
    failed: int = 0
    cancelled: int = 0


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class HourSummary:
    hour: Optional[str] = None
    total: int = 0
    completed: int = 0
    failed: int = 0
    cancelled: int = 0


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class WorkerStats:
    name: Optional[str] = None
    queued: int = 0
    received: int = 0
    completed: int = 0
    retries: int = 0
    failed: int = 0
    running_job: Optional[int] = None
    running_time: Optional[datetime.timedelta] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RequestLogEntry:
    id: int = 0
    trace_id: Optional[str] = None
    operation_name: Optional[str] = None
    date_time: datetime.datetime = datetime.datetime(1, 1, 1)
    status_code: int = 0
    status_description: Optional[str] = None
    http_method: Optional[str] = None
    absolute_uri: Optional[str] = None
    path_info: Optional[str] = None
    # @StringLength(2147483647)
    request_body: Optional[str] = None

    request_dto: Optional[Object] = None
    user_auth_id: Optional[str] = None
    session_id: Optional[str] = None
    ip_address: Optional[str] = None
    forwarded_for: Optional[str] = None
    referer: Optional[str] = None
    headers: Optional[Dict[str, str]] = None
    form_data: Optional[Dict[str, str]] = None
    items: Optional[Dict[str, str]] = None
    response_headers: Optional[Dict[str, str]] = None
    session: Optional[Object] = None
    response_dto: Optional[Object] = None
    error_response: Optional[Object] = None
    exception_source: Optional[str] = None
    exception_data: Optional[Dict] = None
    request_duration: datetime.timedelta = datetime.timedelta()
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDashboardResponse:
    server_stats: Optional[ServerStats] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUserResponse:
    id: Optional[str] = None
    result: Optional[Dict[str, Object]] = None
    details: Optional[List[Dict[str, Object]]] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUsersResponse:
    results: Optional[List[Dict[str, Object]]] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDeleteUserResponse:
    id: Optional[str] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminProfilingResponse:
    results: List[DiagnosticEntry] = field(default_factory=list)
    total: int = 0
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminRedisResponse:
    db: int = 0
    search_results: Optional[List[RedisSearchResult]] = None
    info: Optional[Dict[str, str]] = None
    endpoint: Optional[RedisEndpointInfo] = None
    result: Optional[RedisText] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDatabaseResponse:
    results: List[Dict[str, Object]] = field(default_factory=list)
    total: Optional[int] = None
    columns: Optional[List[MetadataPropertyType]] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ViewCommandsResponse:
    command_totals: List[CommandSummary] = field(default_factory=list)
    latest_commands: List[CommandResult] = field(default_factory=list)
    latest_failed: List[CommandResult] = field(default_factory=list)
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ExecuteCommandResponse:
    command_result: Optional[CommandResult] = None
    result: Optional[str] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminApiKeysResponse:
    results: Optional[List[PartialApiKey]] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminApiKeyResponse:
    result: Optional[str] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminJobDashboardResponse:
    commands: List[JobStatSummary] = field(default_factory=list)
    apis: List[JobStatSummary] = field(default_factory=list)
    workers: List[JobStatSummary] = field(default_factory=list)
    today: List[HourSummary] = field(default_factory=list)
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminJobInfoResponse:
    month_dbs: List[datetime.datetime] = field(default_factory=list)
    table_counts: Dict[str, int] = field(default_factory=dict)
    worker_stats: List[WorkerStats] = field(default_factory=list)
    queue_counts: Dict[str, int] = field(default_factory=dict)
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminGetJobResponse:
    result: Optional[JobSummary] = None
    queued: Optional[BackgroundJob] = None
    completed: Optional[CompletedJob] = None
    failed: Optional[FailedJob] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminGetJobProgressResponse:
    state: Optional[BackgroundJobState] = None
    progress: Optional[float] = None
    status: Optional[str] = None
    logs: Optional[str] = None
    duration_ms: Optional[int] = None
    error: Optional[ResponseStatus] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminRequeueFailedJobsJobsResponse:
    results: List[int] = field(default_factory=list)
    errors: Dict[int, str] = field(default_factory=dict)
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminCancelJobsResponse:
    results: List[int] = field(default_factory=list)
    errors: Dict[int, str] = field(default_factory=dict)
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RequestLogsResponse:
    results: Optional[List[RequestLogEntry]] = None
    usage: Optional[Dict[str, str]] = None
    total: int = 0
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class GetValidationRulesResponse:
    results: Optional[List[ValidationRule]] = None
    response_status: Optional[ResponseStatus] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDashboard(IReturn[AdminDashboardResponse], IGet):
    pass


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminGetUser(IReturn[AdminUserResponse], IGet):
    id: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryUsers(IReturn[AdminUsersResponse], IGet):
    query: Optional[str] = None
    order_by: Optional[str] = None
    skip: Optional[int] = None
    take: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminCreateUser(AdminUserBase, IReturn[AdminUserResponse], IPost):
    roles: Optional[List[str]] = None
    permissions: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUpdateUser(AdminUserBase, IReturn[AdminUserResponse], IPut):
    id: Optional[str] = None
    lock_user: Optional[bool] = None
    unlock_user: Optional[bool] = None
    lock_user_until: Optional[datetime.datetime] = None
    add_roles: Optional[List[str]] = None
    remove_roles: Optional[List[str]] = None
    add_permissions: Optional[List[str]] = None
    remove_permissions: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDeleteUser(IReturn[AdminDeleteUserResponse], IDelete):
    id: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryRequestLogs(QueryDb[RequestLog], IReturn[QueryResponse[RequestLog]]):
    month: Optional[datetime.datetime] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminProfiling(IReturn[AdminProfilingResponse]):
    source: Optional[str] = None
    event_type: Optional[str] = None
    thread_id: Optional[int] = None
    trace_id: Optional[str] = None
    user_auth_id: Optional[str] = None
    session_id: Optional[str] = None
    tag: Optional[str] = None
    skip: int = 0
    take: Optional[int] = None
    order_by: Optional[str] = None
    with_errors: Optional[bool] = None
    pending: Optional[bool] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminRedis(IReturn[AdminRedisResponse], IPost):
    db: Optional[int] = None
    query: Optional[str] = None
    reconnect: Optional[RedisEndpointInfo] = None
    take: Optional[int] = None
    position: Optional[int] = None
    args: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDatabase(IReturn[AdminDatabaseResponse], IGet):
    db: Optional[str] = None
    schema: Optional[str] = None
    table: Optional[str] = None
    fields: Optional[List[str]] = None
    take: Optional[int] = None
    skip: Optional[int] = None
    order_by: Optional[str] = None
    include: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ViewCommands(IReturn[ViewCommandsResponse], IGet):
    include: Optional[List[str]] = None
    skip: Optional[int] = None
    take: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ExecuteCommand(IReturn[ExecuteCommandResponse], IPost):
    command: Optional[str] = None
    request_json: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryApiKeys(IReturn[AdminApiKeysResponse], IGet):
    id: Optional[int] = None
    search: Optional[str] = None
    user_id: Optional[str] = None
    user_name: Optional[str] = None
    order_by: Optional[str] = None
    skip: Optional[int] = None
    take: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminCreateApiKey(IReturn[AdminApiKeyResponse], IPost):
    name: Optional[str] = None
    user_id: Optional[str] = None
    user_name: Optional[str] = None
    scopes: Optional[List[str]] = None
    features: Optional[List[str]] = None
    restrict_to: Optional[List[str]] = None
    expiry_date: Optional[datetime.datetime] = None
    notes: Optional[str] = None
    ref_id: Optional[int] = None
    ref_id_str: Optional[str] = None
    meta: Optional[Dict[str, str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminUpdateApiKey(IReturn[EmptyResponse], IPatch):
    # @Validate(Validator="GreaterThan(0)")
    id: int = 0

    name: Optional[str] = None
    user_id: Optional[str] = None
    user_name: Optional[str] = None
    scopes: Optional[List[str]] = None
    features: Optional[List[str]] = None
    restrict_to: Optional[List[str]] = None
    expiry_date: Optional[datetime.datetime] = None
    cancelled_date: Optional[datetime.datetime] = None
    notes: Optional[str] = None
    ref_id: Optional[int] = None
    ref_id_str: Optional[str] = None
    meta: Optional[Dict[str, str]] = None
    reset: Optional[List[str]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminDeleteApiKey(IReturn[EmptyResponse], IDelete):
    # @Validate(Validator="GreaterThan(0)")
    id: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminJobDashboard(IReturn[AdminJobDashboardResponse], IGet):
    from_: Optional[datetime.datetime] = field(metadata=config(field_name='from'), default=None)
    to: Optional[datetime.datetime] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminJobInfo(IReturn[AdminJobInfoResponse], IGet):
    month: Optional[datetime.datetime] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminGetJob(IReturn[AdminGetJobResponse], IGet):
    id: Optional[int] = None
    ref_id: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminGetJobProgress(IReturn[AdminGetJobProgressResponse], IGet):
    # @Validate(Validator="GreaterThan(0)")
    id: int = 0

    log_start: Optional[int] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryBackgroundJobs(QueryDb[BackgroundJob], IReturn[QueryResponse[BackgroundJob]]):
    id: Optional[int] = None
    ref_id: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryJobSummary(QueryDb[JobSummary], IReturn[QueryResponse[JobSummary]]):
    id: Optional[int] = None
    ref_id: Optional[str] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryScheduledTasks(QueryDb[ScheduledTask], IReturn[QueryResponse[ScheduledTask]]):
    pass


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryCompletedJobs(QueryDb[CompletedJob], IReturn[QueryResponse[CompletedJob]]):
    month: Optional[datetime.datetime] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminQueryFailedJobs(QueryDb[FailedJob], IReturn[QueryResponse[FailedJob]]):
    month: Optional[datetime.datetime] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminRequeueFailedJobs(IReturn[AdminRequeueFailedJobsJobsResponse]):
    ids: Optional[List[int]] = None


@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class AdminCancelJobs(IReturn[AdminCancelJobsResponse], IGet):
    ids: Optional[List[int]] = None
    worker: Optional[str] = None


# @Route("/requestlogs")
@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class RequestLogs(IReturn[RequestLogsResponse], IGet):
    before_secs: Optional[int] = None
    after_secs: Optional[int] = None
    operation_name: Optional[str] = None
    ip_address: Optional[str] = None
    forwarded_for: Optional[str] = None
    user_auth_id: Optional[str] = None
    session_id: Optional[str] = None
    referer: Optional[str] = None
    path_info: Optional[str] = None
    ids: Optional[List[int]] = None
    before_id: Optional[int] = None
    after_id: Optional[int] = None
    has_response: Optional[bool] = None
    with_errors: Optional[bool] = None
    enable_session_tracking: Optional[bool] = None
    enable_response_tracking: Optional[bool] = None
    enable_error_tracking: Optional[bool] = None
    duration_longer_than: Optional[datetime.timedelta] = None
    duration_less_than: Optional[datetime.timedelta] = None
    skip: int = 0
    take: Optional[int] = None
    order_by: Optional[str] = None


# @Route("/validation/rules/{Type}")
@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class GetValidationRules(IReturn[GetValidationRulesResponse], IGet):
    auth_secret: Optional[str] = None
    type: Optional[str] = None


# @Route("/validation/rules")
@dataclass_json(letter_case=LetterCase.CAMEL, undefined=Undefined.EXCLUDE)
@dataclass
class ModifyValidationRules(IReturnVoid):
    auth_secret: Optional[str] = None
    save_rules: Optional[List[ValidationRule]] = None
    delete_rule_ids: Optional[List[int]] = None
    suspend_rule_ids: Optional[List[int]] = None
    unsuspend_rule_ids: Optional[List[int]] = None
    clear_cache: Optional[bool] = None

