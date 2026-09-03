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

## 5. Route Constraint and Wildcard Formatting (`UrlExtensions`)
- **Severity**: Low / Routing Bug
- **Description**:
  - In `UrlExtensions.RestRoute`, variable extraction only stripped `*` from variable names. Routes with constraints (`{Id:int}`) or optional markers (`{Id?}`) failed with `Variable '{variableName}' does not match any property`.
  - Wildcard parameter formatting converted `/` to `%2F` via `Uri.EscapeDataString`, breaking REST hierarchical paths.
- **Change**:
  - Normalized variable names with `.LeftPart(':').TrimEnd('?').Trim('*')` to support route constraints and optional parameters.
  - Preserved `/` directory separators for wildcard route variables while URL-escaping individual path segments.
