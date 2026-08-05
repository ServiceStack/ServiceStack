package main

import (
	"bytes"
	"encoding/base64"
	"encoding/json"
	"fmt"
	"io"
	"mime/multipart"
	"net/http"
	"net/url"
	"reflect"
	"strconv"
	"strings"
)

const (
	JsonMimeType        = "application/json"
	AuthorizationHeader = "Authorization"
	ContentTypeHeader   = "Content-Type"
)

func stringPtr(s string) *string {
	return &s
}

// WebServiceException represents a ServiceStack API error response
type WebServiceException struct {
	StatusCode        int
	StatusDescription string
	Message           string
	ResponseStatus    *ResponseStatus
	InnerException    error
}

func (e *WebServiceException) Error() string {
	if e.ResponseStatus != nil && e.ResponseStatus.Message != nil && *e.ResponseStatus.Message != "" {
		return fmt.Sprintf("[%d] %s: %s", e.StatusCode, e.ResponseStatus.ErrorCode, *e.ResponseStatus.Message)
	}
	if e.StatusDescription != "" {
		return fmt.Sprintf("[%d] %s", e.StatusCode, e.StatusDescription)
	}
	return fmt.Sprintf("[%d] WebServiceException", e.StatusCode)
}

// UploadFile represents a file to be uploaded via multipart form
type UploadFile struct {
	FieldName   string
	FileName    string
	ContentType string
	Reader      io.Reader
}

// JsonServiceClient provides generic, strongly-typed HTTP communication with ServiceStack services
type JsonServiceClient struct {
	BaseUrl          string
	ReplyBaseUrl     string
	OneWayBaseUrl    string
	Headers          map[string]string
	BearerToken      string
	RefreshToken     string
	RefreshTokenURI  string
	Username         string
	Password         string
	HTTPClient       *http.Client
	RequestFilter    func(*http.Request)
	ResponseFilter   func(*http.Response)
	ExceptionFilter  func(*http.Response, error)
}

var (
	GlobalRequestFilter   func(*http.Request)
	GlobalResponseFilter  func(*http.Response)
	GlobalExceptionFilter func(*http.Response, error)
)

func NewJsonServiceClient(baseUrl string) *JsonServiceClient {
	client := &JsonServiceClient{
		BaseUrl:    strings.TrimSuffix(baseUrl, "/"),
		Headers:    map[string]string{"Accept": JsonMimeType},
		HTTPClient: &http.Client{},
	}
	client.SetBasePath("api")
	return client
}

func (c *JsonServiceClient) SetBasePath(basePath string) *JsonServiceClient {
	if basePath == "" {
		c.ReplyBaseUrl = CombineWith(c.BaseUrl, "json/reply") + "/"
		c.OneWayBaseUrl = CombineWith(c.BaseUrl, "json/oneway") + "/"
	} else {
		c.ReplyBaseUrl = CombineWith(c.BaseUrl, basePath) + "/"
		c.OneWayBaseUrl = CombineWith(c.BaseUrl, basePath) + "/"
	}
	return c
}

func (c *JsonServiceClient) SetCredentials(username, password string) *JsonServiceClient {
	c.Username = username
	c.Password = password
	return c
}

func (c *JsonServiceClient) SetBearerToken(token string) *JsonServiceClient {
	c.BearerToken = token
	return c
}

func (c *JsonServiceClient) SetRefreshToken(token string) *JsonServiceClient {
	c.RefreshToken = token
	return c
}

func (c *JsonServiceClient) SetHeader(key, value string) *JsonServiceClient {
	if c.Headers == nil {
		c.Headers = make(map[string]string)
	}
	c.Headers[key] = value
	return c
}

func (c *JsonServiceClient) ToAbsoluteUrl(pathOrUrl string) string {
	if strings.HasPrefix(pathOrUrl, "http://") || strings.HasPrefix(pathOrUrl, "https://") {
		return pathOrUrl
	}
	return CombineWith(c.BaseUrl, pathOrUrl)
}

func CombineWith(baseUrl, relativeUrl string) string {
	if baseUrl == "" {
		return relativeUrl
	}
	if relativeUrl == "" {
		return baseUrl
	}
	if !strings.HasSuffix(baseUrl, "/") {
		baseUrl += "/"
	}
	if strings.HasPrefix(relativeUrl, "/") {
		relativeUrl = relativeUrl[1:]
	}
	return baseUrl + relativeUrl
}

func HasRequestBody(method string) bool {
	m := strings.ToUpper(method)
	return m != "GET" && m != "DELETE" && m != "HEAD" && m != "OPTIONS"
}

func NameOf(request any) string {
	if request == nil {
		return ""
	}
	t := reflect.TypeOf(request)
	if t.Kind() == reflect.Ptr {
		t = t.Elem()
	}
	return t.Name()
}

func (c *JsonServiceClient) CreateUrlFromDTO(method string, request any) string {
	name := NameOf(request)
	if name == "ChatCompletion" {
		return c.BaseUrl + "/v1/chat/completions"
	}
	urlStr := CombineWith(c.ReplyBaseUrl, name)
	if !HasRequestBody(method) && request != nil {
		urlStr = AppendQueryString(urlStr, StructToMap(request))
	}
	return urlStr
}

func StructToMap(obj any) map[string]any {
	result := make(map[string]any)
	data, err := json.Marshal(obj)
	if err != nil {
		return result
	}
	json.Unmarshal(data, &result)
	return result
}

func AppendQueryString(urlStr string, args map[string]any) string {
	if len(args) == 0 {
		return urlStr
	}
	var pairs []string
	for k, v := range args {
		if v == nil {
			continue
		}
		valStr := fmt.Sprintf("%v", v)
		if valStr != "" {
			pairs = append(pairs, url.QueryEscape(k)+"="+url.QueryEscape(valStr))
		}
	}
	if len(pairs) == 0 {
		return urlStr
	}
	sep := "?"
	if strings.Contains(urlStr, "?") {
		sep = "&"
	}
	return urlStr + sep + strings.Join(pairs, "&")
}

// ── Generic Typed Client API ──

func Send[T any](c *JsonServiceClient, request any) (*T, error) {
	return SendWithMethod[T](c, request, ResolveHttpMethod(request), nil)
}

func SendWithMethod[T any](c *JsonServiceClient, request any, method string, args map[string]any) (*T, error) {
	reqUrl := c.CreateUrlFromDTO(method, request)
	if len(args) > 0 {
		reqUrl = AppendQueryString(reqUrl, args)
	}
	return SendUrl[T](c, reqUrl, method, request, nil)
}

func SendUrl[T any](c *JsonServiceClient, path string, method string, body any, args map[string]any) (*T, error) {
	absUrl := c.ToAbsoluteUrl(path)
	if len(args) > 0 {
		absUrl = AppendQueryString(absUrl, args)
	}

	var bodyBytes []byte
	var err error
	if body != nil && HasRequestBody(method) {
		if b, ok := body.([]byte); ok {
			bodyBytes = b
		} else if s, ok := body.(string); ok {
			bodyBytes = []byte(s)
		} else {
			bodyBytes, err = json.Marshal(body)
			if err != nil {
				return nil, fmt.Errorf("failed to marshal request body: %w", err)
			}
		}
	}

	httpReq, err := http.NewRequest(method, absUrl, bytes.NewBuffer(bodyBytes))
	if err != nil {
		return nil, fmt.Errorf("failed to create http request: %w", err)
	}

	for k, v := range c.Headers {
		httpReq.Header.Set(k, v)
	}
	if HasRequestBody(method) && httpReq.Header.Get(ContentTypeHeader) == "" {
		httpReq.Header.Set(ContentTypeHeader, JsonMimeType)
	}

	if c.BearerToken != "" {
		httpReq.Header.Set(AuthorizationHeader, "Bearer "+c.BearerToken)
	} else if c.Username != "" {
		auth := base64.StdEncoding.EncodeToString([]byte(c.Username + ":" + c.Password))
		httpReq.Header.Set(AuthorizationHeader, "Basic "+auth)
	}

	if c.RequestFilter != nil {
		c.RequestFilter(httpReq)
	}
	if GlobalRequestFilter != nil {
		GlobalRequestFilter(httpReq)
	}

	resp, err := c.HTTPClient.Do(httpReq)
	if err != nil {
		return nil, c.handleError(resp, err)
	}
	defer resp.Body.Close()

	if c.ResponseFilter != nil {
		c.ResponseFilter(resp)
	}
	if GlobalResponseFilter != nil {
		GlobalResponseFilter(resp)
	}

	respBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, c.handleError(resp, fmt.Errorf("failed to read response body: %w", err))
	}

	if resp.StatusCode >= 400 {
		return nil, c.handleStatusCodeError(resp, respBytes)
	}

	var target T
	targetType := reflect.TypeOf(target)

	if targetType == reflect.TypeOf([]byte{}) {
		var anyResult any = respBytes
		return anyResult.(*T), nil
	}
	if targetType == reflect.TypeOf("") {
		var anyResult any = string(respBytes)
		return anyResult.(*T), nil
	}

	if err := json.Unmarshal(respBytes, &target); err != nil {
		return nil, c.handleError(resp, fmt.Errorf("failed to unmarshal response into %T: %w. Body: %s", target, err, string(respBytes)))
	}

	return &target, nil
}

// ── HTTP Verb Helper Methods ──

func Get[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "GET", a)
}

func Post[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "POST", a)
}

func Put[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "PUT", a)
}

func Patch[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "PATCH", a)
}

func Delete[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "DELETE", a)
}

func Options[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "OPTIONS", a)
}

func Head[T any](c *JsonServiceClient, request any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendWithMethod[T](c, request, "HEAD", a)
}

// ── URL-Based Helper Methods ──

func GetUrl[T any](c *JsonServiceClient, path string, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendUrl[T](c, path, "GET", nil, a)
}

func PostUrl[T any](c *JsonServiceClient, path string, body any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendUrl[T](c, path, "POST", body, a)
}

func PutUrl[T any](c *JsonServiceClient, path string, body any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendUrl[T](c, path, "PUT", body, a)
}

func PatchUrl[T any](c *JsonServiceClient, path string, body any, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendUrl[T](c, path, "PATCH", body, a)
}

func DeleteUrl[T any](c *JsonServiceClient, path string, args ...map[string]any) (*T, error) {
	var a map[string]any
	if len(args) > 0 {
		a = args[0]
	}
	return SendUrl[T](c, path, "DELETE", nil, a)
}

// ── Batch Operations ──

func SendAll[T any](c *JsonServiceClient, requestDtos []any) ([]T, error) {
	if len(requestDtos) == 0 {
		return []T{}, nil
	}
	reqUrl := CombineWith(c.ReplyBaseUrl, NameOf(requestDtos[0])+"[]")
	res, err := SendUrl[[]T](c, reqUrl, "POST", requestDtos, nil)
	if err != nil {
		return nil, err
	}
	if res == nil {
		return []T{}, nil
	}
	return *res, nil
}

func SendAllOneWay(c *JsonServiceClient, requestDtos []any) error {
	if len(requestDtos) == 0 {
		return nil
	}
	reqUrl := CombineWith(c.OneWayBaseUrl, NameOf(requestDtos[0])+"[]")
	_, err := SendUrl[any](c, reqUrl, "POST", requestDtos, nil)
	return err
}

// ── File Upload Support ──

func PostFilesWithRequest[T any](c *JsonServiceClient, request any, files []UploadFile) (*T, error) {
	reqUrl := c.CreateUrlFromDTO("POST", request)
	return PostFilesWithRequestUrl[T](c, reqUrl, request, files)
}

func PostFilesWithRequestUrl[T any](c *JsonServiceClient, requestUrl string, request any, files []UploadFile) (*T, error) {
	bodyBuf := &bytes.Buffer{}
	writer := multipart.NewWriter(bodyBuf)

	requestMap := StructToMap(request)
	for k, v := range requestMap {
		if v != nil {
			writer.WriteField(k, fmt.Sprintf("%v", v))
		}
	}

	for _, file := range files {
		fieldName := file.FieldName
		if fieldName == "" {
			fieldName = "upload"
		}
		fileName := file.FileName
		if fileName == "" {
			fileName = "file"
		}
		part, err := writer.CreateFormFile(fieldName, fileName)
		if err != nil {
			return nil, fmt.Errorf("failed to create form file: %w", err)
		}
		if file.Reader != nil {
			_, err = io.Copy(part, file.Reader)
			if err != nil {
				return nil, fmt.Errorf("failed to copy file stream: %w", err)
			}
		}
	}
	writer.Close()

	absUrl := c.ToAbsoluteUrl(requestUrl)
	httpReq, err := http.NewRequest("POST", absUrl, bodyBuf)
	if err != nil {
		return nil, fmt.Errorf("failed to create multipart request: %w", err)
	}

	httpReq.Header.Set(ContentTypeHeader, writer.FormDataContentType())
	httpReq.Header.Set("Accept", JsonMimeType)
	if c.BearerToken != "" {
		httpReq.Header.Set(AuthorizationHeader, "Bearer "+c.BearerToken)
	}

	resp, err := c.HTTPClient.Do(httpReq)
	if err != nil {
		return nil, c.handleError(resp, err)
	}
	defer resp.Body.Close()

	respBytes, err := io.ReadAll(resp.Body)
	if err != nil {
		return nil, c.handleError(resp, err)
	}

	if resp.StatusCode >= 400 {
		return nil, c.handleStatusCodeError(resp, respBytes)
	}

	var target T
	if err := json.Unmarshal(respBytes, &target); err != nil {
		return nil, c.handleError(resp, err)
	}
	return &target, nil
}

// ── Error Handling & Resolution ──

func ResolveHttpMethod(request any) string {
	if request == nil {
		return "POST"
	}
	t := reflect.TypeOf(request)
	if t.Kind() == reflect.Ptr {
		t = t.Elem()
	}
	name := t.Name()
	if strings.HasPrefix(name, "Get") {
		return "GET"
	}
	if strings.HasPrefix(name, "Query") {
		return "GET"
	}
	if strings.HasPrefix(name, "Put") {
		return "PUT"
	}
	if strings.HasPrefix(name, "Patch") {
		return "PATCH"
	}
	if strings.HasPrefix(name, "Delete") {
		return "DELETE"
	}
	return "POST"
}

func (c *JsonServiceClient) handleStatusCodeError(resp *http.Response, bodyBytes []byte) error {
	webEx := &WebServiceException{
		StatusCode:        resp.StatusCode,
		StatusDescription: resp.Status,
	}

	var errorRes struct {
		ResponseStatus *ResponseStatus `json:"responseStatus"`
	}
	if err := json.Unmarshal(bodyBytes, &errorRes); err == nil && errorRes.ResponseStatus != nil {
		webEx.ResponseStatus = errorRes.ResponseStatus
	} else {
		msg := resp.Status
		webEx.ResponseStatus = &ResponseStatus{
			ErrorCode: strconv.Itoa(resp.StatusCode),
			Message:   &msg,
		}
	}

	return c.handleError(resp, webEx)
}

func (c *JsonServiceClient) handleError(resp *http.Response, err error) error {
	if c.ExceptionFilter != nil {
		c.ExceptionFilter(resp, err)
	}
	if GlobalExceptionFilter != nil {
		GlobalExceptionFilter(resp, err)
	}
	return err
}
