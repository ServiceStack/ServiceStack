# Security Changes & Remediation Reference (`ServiceStack.Blazor`)

This document details security vulnerabilities identified and remediated in `ServiceStack.Blazor`.

---

## 1. Inverted Role & Permission Authorization Checks in `AuthBlazorComponentBase`
- **Severity**: Critical (Broken Access Control)
- **Description**: `AuthBlazorComponentBase.CanAccess(MetadataOperationType op)` previously checked `if (op.RequiredRoles != null && op.RequiredRoles.All(role => roles.Contains(role))) return false;` (and similarly for `RequiredPermissions`). Because the negation operator (`!`) was missing, users who possessed all required roles/permissions were denied access (`return false`), while unauthorized users who lacked the required roles/permissions bypassed the check and were granted access (`return true`). In addition, `InvalidAccessMessage` checked `roles` instead of `permissions` for missing permissions.
- **Change**: Added the missing negation `!` to ensure `CanAccess` returns `false` only when the user *lacks* required roles or permissions, and corrected the permissions check in `InvalidAccessMessage`.

---

## 2. Open Redirect Vulnerability via Unvalidated Return URLs in `NavigationUtils`
- **Severity**: High (Open Redirect / Phishing - CWE-601)
- **Description**: `NavigationUtils.GetReturnUrl(this NavigationManager nav)` returned the `return` query string directly without validating whether it represented a local application path. Attackers could craft links to sign-in or sign-up pages (`?return=https://attacker.com` or `?return=//attacker.com`) that redirected users to external phishing sites upon successful authentication.
- **Change**: Added `NavigationUtils.IsLocalUrl(this string? url)` validation. `GetReturnUrl` now verifies that the return URL begins with a single `/` (and not `//` or `/\`) before returning it, falling back to `"/"` for external or invalid URLs.

---

## 3. Cross-Circuit State Contamination in Blazor Server (`UiComponentBase.RenderActions`)
- **Severity**: High (Information Disclosure & Circuit State Pollution)
- **Description**: `UiComponentBase.RenderActions` was declared as a `static ConcurrentDictionary` shared across all component instances. In Blazor Server hosting models where all connected users share process memory, render actions queued by User A (such as document title changes) could be dequeued and executed by User B's circuit during User B's `OnAfterRenderAsync` lifecycle method.
- **Change**: Changed `renderActions` to an instance-level `ConcurrentQueue<Func<IJSRuntime, Task>>` on each component instance.

---

## 4. Attribute Name vs. Value Desync in `DynamicInputBase`
- **Severity**: Medium (Event Handler Attribute Bypass)
- **Description**: In `DynamicInputBase.AllAttributes`, the sanitization loop checked whether `val` (the attribute value) started with `"on"` or matched `SanitizeAttribute(s)` (`@bind`), rather than checking `key` (the attribute name). Consequently, dangerous DOM event handlers (`onclick`, `onload`, `onerror`) were not removed if their values did not begin with `"on"`, while benign attribute values starting with `"on"` (e.g. `title="online"`) were stripped.
- **Change**: Updated the check to inspect `key.StartsWith("on", StringComparison.OrdinalIgnoreCase)` and `SanitizeAttribute(key)`.

---

## 5. HTML Injection & Output Encoding in `FormatValueAsHtml` & `HtmlUtils`
- **Severity**: Medium (Cross-Site Scripting - XSS)
- **Description**:
  - `BlazorUtils.FormatValueAsHtml` interpolated dictionary keys (`<b>{key}</b>`) and enumerable scalar items directly into raw HTML without HTML encoding.
  - `HtmlUtils.HtmlDump` and `HtmlList` interpolated `options.Id` and `className` without HTML encoding, did not restrict `headerTag`, and lacked a recursion depth limit against stack overflows on deep or cyclic graphs.
- **Change**:
  - HTML-encoded dictionary keys and scalar values in `BlazorUtils.FormatValueAsHtml`.
  - HTML-encoded `options.Id` and `className`, restricted `headerTag` to alphanumeric identifiers, and added `options.MaxDepth` recursion limit in `HtmlDumpOptions`.

---

## 6. Unbounded Default File Upload Limit in `AutoFormBase`
- **Severity**: Low / Medium (Denial of Service)
- **Description**: In `AutoFormBase.OnSave`, file upload stream limits defaulted to `int.MaxValue` (~2 GB) when `uploadInfo.MaxFileBytes` was unspecified, bypassing Blazor's built-in buffer safety limit.
- **Change**: Introduced `DefaultMaxFileSize` (50 MB) and `DefaultMaxAllowedFiles` (100) fallback defaults when metadata does not specify custom limits.
