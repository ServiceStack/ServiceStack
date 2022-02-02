//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_cdm_registration_error_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Error codes for CDM registration. See cef_web_plugin.h for details.
    /// </summary>
    public enum CefCdmRegistrationError
    {
        /// <summary>
        /// No error. Registration completed successfully.
        /// </summary>
        None = 0,

        /// <summary>
        /// Required files or manifest contents are missing.
        /// </summary>
        IncorrectContents,

        /// <summary>
        /// The CDM is incompatible with the current Chromium version.
        /// </summary>
        Incompatible,

        /// <summary>
        /// CDM registration is not supported at this time.
        /// </summary>
        NotSupported,
    }
}
