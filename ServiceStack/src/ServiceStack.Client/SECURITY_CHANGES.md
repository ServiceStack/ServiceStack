# Security Changes & Remediation Reference (`ServiceStack.Client`)

This document details security, robustness, and stability fixes across `ServiceStack.Client`.

---

## 1. XML Security & Infinite Loop Prevention in RSA Extraction (`CryptUtils`)
- **Severity**: High (DoS / Potential XXE)
- **Description**:
  - `PlatformRsaUtils.ExtractFromXml` parsed XML strings using default `XmlReader` settings, which on legacy frameworks (.NET 4.7.2) allows DTD processing and potential XXE attacks.
  - The inner loop `do { reader.Read(); } while (reader.NodeType != XmlNodeType.Text && reader.NodeType != XmlNodeType.EndElement);` did not check the boolean return value of `reader.Read()`. On truncated or malformed XML where EOF is reached, `reader.Read()` returns `false` and `reader.NodeType` remains `XmlNodeType.None`, resulting in an infinite loop that hangs the thread.
- **Change**:
  - Configured `XmlReaderSettings` with `DtdProcessing = DtdProcessing.Prohibit` and `XmlResolver = null`.
  - Converted the inner loop to check `while (reader.Read())` to ensure proper termination upon reaching EOF.

---

## 2. Bounds-Safe & Quote-Aware HTTP Digest Header Parsing (`WebRequestUtils`)
- **Severity**: Medium (Crash / Unhandled Exception)
- **Description**:
  - `AuthenticationInfo` split the header remainder by commas and assumed that any unquoted parameter was part of a split quoted value, accessing `pars[i + 1]` without bounds checking.
  - When parsing standard unquoted parameters (such as `stale=false`, `algorithm=MD5`, `qop=auth`), this threw an unhandled `IndexOutOfRangeException` if the parameter appeared at the end of the header, and corrupted parsing if in the middle.
  - An unrecognized parameter caused an immediate return, dropping remaining valid parameters.
- **Change**:
  - Added quote-count tracking to only merge tokens when an unmatched quote exists and `i + 1 < pars.Length`.
  - Replaced early return on unrecognized parameter pairs with `continue`.

---

## 3. ReDoS Protection & Regex Precompilation (`UserAgentHelper`)
- **Severity**: Medium (Denial of Service)
- **Description**:
  - Multiple regular expressions were evaluated against untrusted, user-supplied `User-Agent` HTTP header strings without timeout limits.
  - Ad-hoc regexes (Opera, MSIE, Trident, Googlebot, Bingbot, generic crawler patterns, and screen dimensions) were instantiated and compiled dynamically on every request.
- **Change**:
  - Defined pre-compiled, static `Regex` instances for all browser and crawler patterns with a 1-second timeout (`TimeSpan.FromSeconds(1)`).

---

## 4. Supported Encodings String Formatting (`StreamCompressors`)
- **Severity**: Low / Robustness
- **Description**:
  - In `StreamCompressors.GetRequired`, `string.Join(", ", Compressors.Keys.ToString())` called `.ToString()` on the keys collection instead of passing the collection directly to `string.Join`, outputting the type name instead of supported compression names in the exception message.
- **Change**:
  - Changed `Compressors.Keys.ToString()` to `Compressors.Keys`.

---

## 5. Route Constraint and Optional Parameter Support (`UrlExtensions`)
- **Severity**: Low / Routing Bug
- **Description**:
  - In `UrlExtensions.RestRoute`, variable extraction only stripped `*` from variable names. Routes with constraints (`{Id:int}`) or optional markers (`{Id?}`) failed with `Variable '{variableName}' does not match any property`.
- **Change**:
  - Normalized variable names with `.LeftPart(':').TrimEnd('?').Trim('*')` to support route constraints and optional parameters.

---

## 6. Route Variable Extraction Bounds Check (`UrlExtensions`)
- **Severity**: Medium / Crash Prevention
- **Description**:
  - In `UrlExtensions.GetUrlVariables`, malformed route components containing `{` or `}` with length < 2 (e.g. `"/test/{/path"`) triggered `component.Substring(1, component.Length - 2)` with a negative length (`-1`), throwing an unhandled `ArgumentOutOfRangeException`.
- **Change**:
  - Validated `component.Length >= 2 && component[0] == VariablePrefixChar && component[component.Length - 1] == VariablePostfixChar` before calling `Substring`.

---

## 7. Wildcard Route Replacement Null Safety (`UrlExtensions`)
- **Severity**: Low / Robustness
- **Description**:
  - In `RestRoute.Apply`, wildcard route parameters allow `null` values. `FormatVariable(null)` returned `null`, causing `uri.Replace(..., null)` to execute with a null replacement string.
- **Change**:
  - Used `variableValue ?? Empty` in `uri.Replace` ensuring reliable empty replacement on all runtimes without ambiguity.

---

## 8. Extension Method Parameter Null Guards (`UrlExtensions`)
- **Severity**: Low / Defensive Programming
- **Description**:
  - Direct invocations of `ToUrl`, `ToOneWayUrlOnly`, `ToOneWayUrl`, `ToReplyUrlOnly`, and `ToReplyUrl` with a null DTO failed with generic `NullReferenceException` instead of descriptive `ArgumentNullException`.
  - Invocations of `GetOperationName`, `GetFullyQualifiedName`, `ExpandTypeName`, `ExpandGenericTypeName`, and `ToApiUrl` threw `NullReferenceException` when passed a `null` `Type`.
- **Change**:
  - Added explicit `ArgumentNullException` guards for request DTOs and safe null returns for Type reflection helpers.

---

## 9. Exception & Error Formatting Null Safety (`WebServiceException`, `ResponseStatusUtils`)
- **Severity**: Low / Robustness
- **Description**:
  - `WebServiceException.ToString()` and `ResponseStatusUtils.GetDetailedError()` threw `NullReferenceException` if `status.Errors` contained a `null` element.
  - `ResponseStatusUtils.GetDetailedError()` threw `NullReferenceException` if called on a `null` `ResponseStatus`.
  - `WebServiceException.ToBuiltInResponseStatus` unnecessarily mutated `responseStatus` on unsuccessful conversions and did not cache successful conversions of generated DTOs.
- **Change**:
  - Added null guards for `status` and null error items in loops; improved caching in `ToBuiltInResponseStatus`; corrected typo in `ArgumentException` message.

---

## 10. Authentication Header & Diagnostics Null Safety (`WebRequestUtils`, `ClientDiagnosticUtils`)
- **Severity**: Low / Defensive Programming
- **Description**:
  - `AuthenticationInfo` constructor threw unhandled `NullReferenceException` when passed `null` or empty strings.
  - `ClientDiagnosticUtils.InitMessage` did not guard against a `null` `IMessage` before setting diagnostic properties.
- **Change**:
  - Added `ArgumentNullException` check on `authHeader` in `AuthenticationInfo` and guarded `msg != null` in `InitMessage`.
