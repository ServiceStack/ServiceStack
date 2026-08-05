# frozen_string_literal: true
# encoding: utf-8

require 'net/http'
require 'uri'
require 'json'

class WebServiceException < StandardError
  attr_accessor :status_code, :status_description, :response_status, :inner_exception

  def initialize(message = nil, status_code = nil, status_description = nil, response_status = nil)
    super(message || status_description || "HTTP Error #{status_code}")
    @status_code = status_code
    @status_description = status_description
    @response_status = response_status
  end
end

class SendContext
  attr_accessor :headers, :method, :url, :request, :body, :body_string, :args, :response_as

  def initialize(headers: {}, method: 'POST', url: nil, request: nil, body: nil, body_string: nil, args: nil, response_as: nil)
    @headers = headers
    @method = method
    @url = url
    @request = request
    @body = body
    @body_string = body_string
    @args = args
    @response_as = response_as
  end
end

class JsonServiceClient
  attr_accessor :base_url, :reply_base_url, :oneway_base_url, :headers, :bearer_token,
                :refresh_token, :username, :password, :request_filter, :response_filter, :exception_filter

  class << self
    attr_accessor :global_request_filter, :global_response_filter, :global_exception_filter
  end

  def initialize(base_url)
    raise ArgumentError, 'base_url is required' if base_url.nil? || base_url.empty?

    @base_url = base_url
    @headers = { 'Accept' => 'application/json' }
    set_base_path('api')
  end

  def set_base_path(base_path = '')
    if base_path.nil? || base_path.empty?
      @reply_base_url = combine_with(@base_url, 'json/reply/')
      @oneway_base_url = combine_with(@base_url, 'json/oneway/')
    else
      @reply_base_url = combine_with(@base_url, "#{base_path}/")
      @oneway_base_url = combine_with(@base_url, "#{base_path}/")
    end
    self
  end

  def set_credentials(username, password)
    @username = username
    @password = password
    self
  end

  def set_bearer_token(bearer_token)
    @bearer_token = bearer_token
    self
  end

  def set_refresh_token(refresh_token)
    @refresh_token = refresh_token
    self
  end

  def get(request, args = nil)
    send(request, 'GET', nil, args)
  end

  def post(request, body = nil, args = nil)
    send(request, 'POST', body, args)
  end

  def put(request, body = nil, args = nil)
    send(request, 'PUT', body, args)
  end

  def patch(request, body = nil, args = nil)
    send(request, 'PATCH', body, args)
  end

  def delete(request, args = nil)
    send(request, 'DELETE', nil, args)
  end

  def options(request, args = nil)
    send(request, 'OPTIONS', nil, args)
  end

  def head(request, args = nil)
    send(request, 'HEAD', nil, args)
  end

  def get_url(path, response_as = nil, args = nil)
    send_url(path, 'GET', response_as, nil, args)
  end

  def post_url(path, body = nil, response_as = nil, args = nil)
    send_url(path, 'POST', response_as, body, args)
  end

  def put_url(path, body = nil, response_as = nil, args = nil)
    send_url(path, 'PUT', response_as, body, args)
  end

  def patch_url(path, body = nil, response_as = nil, args = nil)
    send_url(path, 'PATCH', response_as, body, args)
  end

  def delete_url(path, response_as = nil, args = nil)
    send_url(path, 'DELETE', response_as, nil, args)
  end

  def to_absolute_url(path_or_url)
    return path_or_url if path_or_url.start_with?('http://', 'https://')

    combine_with(@base_url, path_or_url)
  end

  def send(request, method = nil, body = nil, args = nil)
    response_as = resolve_response_type(request)
    method ||= resolve_http_method(request)

    info = SendContext.new(
      headers: @headers.dup,
      method: method,
      url: nil,
      request: request,
      body: body,
      args: args,
      response_as: response_as
    )

    send_request(info)
  end

  def send_url(path, method = nil, response_as = nil, body = nil, args = nil)
    response_as ||= resolve_response_type(body) if body
    method ||= body ? resolve_http_method(body) : 'GET'

    info = SendContext.new(
      headers: @headers.dup,
      method: method,
      url: to_absolute_url(path),
      request: nil,
      body: body,
      args: args,
      response_as: response_as
    )

    send_request(info)
  end

  def send_request(info)
    info = create_request(info)

    uri = URI.parse(info.url)
    http = Net::HTTP.new(uri.host, uri.port)
    if uri.scheme == 'https'
      http.use_ssl = true
      http.verify_mode = OpenSSL::SSL::VERIFY_NONE
    end

    req = case info.method.to_s.upcase
          when 'GET' then Net::HTTP::Get.new(uri.request_uri)
          when 'POST' then Net::HTTP::Post.new(uri.request_uri)
          when 'PUT' then Net::HTTP::Put.new(uri.request_uri)
          when 'PATCH' then Net::HTTP::Patch.new(uri.request_uri)
          when 'DELETE' then Net::HTTP::Delete.new(uri.request_uri)
          when 'OPTIONS' then Net::HTTP::Options.new(uri.request_uri)
          when 'HEAD' then Net::HTTP::Head.new(uri.request_uri)
          else Net::HTTP::Post.new(uri.request_uri)
          end

    info.headers.each { |k, v| req[k] = v }

    if @bearer_token
      req['Authorization'] = "Bearer #{@bearer_token}"
    elsif @username
      req.basic_auth(@username, @password)
    end

    if has_request_body(info.method) && info.body_string
      req.body = info.body_string
      req['Content-Type'] ||= 'application/json'
    end

    response = http.request(req)

    response_filter.call(response) if response_filter
    JsonServiceClient.global_response_filter.call(response) if JsonServiceClient.global_response_filter

    if response.code.to_i >= 400
      handle_error(response, info)
    else
      create_response(response, info)
    end
  rescue StandardError => e
    raise e if e.is_a?(WebServiceException)

    web_ex = WebServiceException.new(e.message, 500, e.message)
    web_ex.inner_exception = e
    raise web_ex
  end

  private

  def create_request(info)
    url = info.url
    body = info.body || info.request

    unless url
      url = create_url_from_dto(info.method, body)
    end

    url = append_querystring(url, info.args) if info.args && !info.args.empty?
    info.url = url

    request_filter.call(info) if request_filter
    JsonServiceClient.global_request_filter.call(info) if JsonServiceClient.global_request_filter

    if has_request_body(info.method)
      info.body_string = body.is_a?(String) ? body : JSON.generate(object_to_hash(body))
    end

    info
  end

  def create_response(response, info)
    into = info.response_as
    body_text = response.body

    return body_text if into == String || into.nil? && body_text.nil?

    parsed_json = body_text && !body_text.empty? ? JSON.parse(body_text) : nil
    return parsed_json if into.nil?

    hash_to_object(into, parsed_json)
  end

  def handle_error(response, info)
    status_code = response.code.to_i
    status_desc = response.message
    resp_status = nil

    if response.body && !response.body.empty?
      begin
        err_hash = JSON.parse(response.body)
        if err_hash.is_a?(Hash) && (err_hash['responseStatus'] || err_hash['response_status'])
          rs_hash = err_hash['responseStatus'] || err_hash['response_status']
          resp_status = hash_to_object(defined?(ResponseStatus) ? ResponseStatus : nil, rs_hash)
        end
      rescue StandardError
        # ignore parse error
      end
    end

    ex = WebServiceException.new("HTTP #{status_code} #{status_desc}", status_code, status_desc, resp_status)
    exception_filter.call(response, ex) if exception_filter
    JsonServiceClient.global_exception_filter.call(response, ex) if JsonServiceClient.global_exception_filter
    raise ex
  end

  def create_url_from_dto(method, request)
    dto_name = request.respond_to?(:get_type_name) ? request.get_type_name : request.class.name.split('::').last
    url = combine_with(@reply_base_url, dto_name)

    unless has_request_body(method)
      dto_hash = object_to_hash(request)
      url = append_querystring(url, dto_hash)
    end

    url
  end

  def combine_with(base_url, relative_url)
    return relative_url if base_url.nil? || base_url.empty?
    return base_url if relative_url.nil? || relative_url.empty?

    base = base_url.end_with?('/') ? base_url : "#{base_url}/"
    rel = relative_url.start_with?('/') ? relative_url[1..] : relative_url

    URI.join(base, rel).to_s
  end

  def append_querystring(url, args)
    return url if args.nil? || args.empty?

    params = []
    args.each do |key, val|
      next if val.nil?

      qs_val = qsvalue(val)
      params << "#{URI.encode_www_form_component(key.to_s)}=#{qs_val}" unless qs_val.nil?
    end

    return url if params.empty?

    separator = url.include?('?') ? '&' : '?'
    "#{url}#{separator}#{params.join('&')}"
  end

  def qsvalue(arg)
    return '' if arg.nil?
    return arg.to_s.downcase if arg.is_a?(TrueClass) || arg.is_a?(FalseClass)
    return "[#{arg.map { |x| qsvalue(x) }.join(',')}]" if arg.is_a?(Array)
    return "{#{arg.map { |k, v| "#{k}:#{qsvalue(v)}" }.join(',')}}" if arg.is_a?(Hash)

    URI.encode_www_form_component(arg.to_s)
  end

  def has_request_body(method)
    !%w[GET DELETE HEAD OPTIONS].include?(method.to_s.upcase)
  end

  def resolve_response_type(request)
    return request.response_type if request.respond_to?(:response_type)

    nil
  end

  def resolve_http_method(request)
    return request.get_type_name if request.respond_to?(:get_type_name)

    'POST'
  end

  def object_to_hash(obj)
    return nil if obj.nil?
    return obj if obj.is_a?(Numeric) || obj.is_a?(String) || obj.is_a?(TrueClass) || obj.is_a?(FalseClass)

    if obj.is_a?(Array)
      return obj.map { |item| object_to_hash(item) }
    end

    if obj.is_a?(Hash)
      res = {}
      obj.each { |k, v| res[k.to_s] = object_to_hash(v) }
      return res
    end

    res = {}
    obj.instance_variables.each do |var|
      key = var.to_s.delete_prefix('@')
      val = obj.instance_variable_get(var)
      res[key] = object_to_hash(val) unless val.nil?
    end
    res
  end

  def snake_case(str)
    str.to_s.gsub(/([A-Z]+)([A-Z][a-z])/, '\1_\2')
       .gsub(/([a-z\d])([A-Z])/, '\1_\2')
       .downcase
  end

  def hash_to_object(target_type, val)
    return val if val.nil? || target_type.nil?
    return val if [String, Integer, Float, TrueClass, FalseClass, Numeric].include?(target_type)

    if val.is_a?(Array)
      return val.map { |item| hash_to_object(target_type, item) }
    end

    return val unless val.is_a?(Hash) && target_type.is_a?(Class)

    inst = target_type.new
    val.each do |k, v|
      snake_key = snake_case(k)
      setter = "#{snake_key}="

      next unless inst.respond_to?(setter)

      sub_type = resolve_nested_type(snake_key)
      converted_val = sub_type ? hash_to_object(sub_type, v) : v
      inst.public_send(setter, converted_val)
    end
    inst
  end

  def resolve_nested_type(key)
    case key.to_s
    when 'response_status'
      defined?(ResponseStatus) ? ResponseStatus : nil
    when 'message'
      defined?(AiMessage) ? AiMessage : nil
    when 'choices'
      defined?(AiChoice) ? AiChoice : nil
    when 'usage'
      defined?(AiUsage) ? AiUsage : nil
    when 'audio'
      defined?(AiChatAudio) ? AiChatAudio : nil
    when 'function'
      defined?(ToolFunction) ? ToolFunction : nil
    when 'tool_calls'
      defined?(ToolCall) ? ToolCall : nil
    when 'response_format'
      defined?(AiResponseFormat) ? AiResponseFormat : nil
    end
  end
end
