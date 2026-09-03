# Security Changes & Remediation Reference (`ServiceStack.HttpClient`)

This document details security, robustness, and stability fixes across `ServiceStack.HttpClient`.

---

## 1. DoS Prevention via Null Dereference Handling (`CachedHttpClient`)
- **Severity**: Medium (Unhandled Exception / Client Crash)
- **Description**:
  - In `CachedHttpClient.OnExceptionFilter`, `webRes.RequestMessage.Method` threw an unhandled `NullReferenceException` when handling mocked or detached HTTP responses where `RequestMessage` is null.
  - In `CachedHttpClient.OnResultsFilterResponse`, accessing `webRes.Content.Headers.LastModified` and `webRes.Content.Headers.ContentLength` threw `NullReferenceException` on HTTP responses without content (such as `304 Not Modified` or `204 No Content`).
- **Remediation**:
  - Added null-propagation checks (`webRes.RequestMessage?.Method == HttpMethod.Get` and `webRes.Content?.Headers...`) to safely handle contentless and detached HTTP response messages.

---

## 2. Multipart Form Parameter Confusion Fix (`JsonHttpClient`, `JsonApiClient`)
- **Severity**: Medium (Data Integrity / Input Validation Confusion)
- **Description**:
  - In `JsonHttpClient.PostFileWithRequest`, the method parameter `fieldName` was ignored and `fileName` was passed as the form field name to the asynchronous overload (`fieldName: fileName`).
  - In `JsonHttpClient.PostFilesWithRequestAsync` and `JsonApiClient.PostFilesWithRequestAsync`, `content.Add(fileContent, fileName, fileName)` registered the file using the file name for both the form field name and file name, discarding `file.FieldName`. This caused servers expecting designated form field names (e.g. `file` or custom upload keys) to reject requests or fail model binding.
- **Remediation**:
  - Corrected parameter propagation in `PostFileWithRequest` to pass `fieldName: fieldName`.
  - Updated `PostFilesWithRequestAsync` to register multipart form content using `content.Add(fileContent, fieldName, fileName)`.

---

## 3. Exception Diagnostics & Information Loss Remediation (`JsonHttpClient`)
- **Severity**: Low (Operational Diagnostics / Error Swallowing)
- **Description**:
  - In `JsonHttpClient.CreateException<TResponse>`, deserialization/transport failures returned an empty `new WebServiceException()`, discarding the inner exception, exception message, HTTP status code, and reason phrase.
- **Remediation**:
  - Populated `WebServiceException` with the inner exception message, inner exception, HTTP status code, and status description from the response.

---

## 4. Query String Appending Corruption Fix (`JsonHttpClient`)
- **Severity**: Low (Malformed Request URL)
- **Description**:
  - In `JsonHttpClient.SendAsync`, query parameters were appended with unconditional `?` (`absoluteUrl += "?" + queryString;`). If the input URL already contained query parameters, this produced a malformed URL with multiple `?` separators.
- **Remediation**:
  - Updated query parameter concatenation to test `absoluteUrl.IndexOf('?') >= 0 ? "&" : "?"`.
