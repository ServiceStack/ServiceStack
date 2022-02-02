namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class used for retrieving resources from the resource bundle (*.pak) files
    /// loaded by CEF during startup or via the CefResourceBundleHandler returned
    /// from CefApp::GetResourceBundleHandler. See CefSettings for additional options
    /// related to resource bundle loading. The methods of this class may be called
    /// on any thread unless otherwise indicated.
    /// </summary>
    public sealed unsafe partial class CefResourceBundle
    {
        /// <summary>
        /// Returns the global resource bundle instance.
        /// </summary>
        public static CefResourceBundle GetGlobal()
        {
            return CefResourceBundle.FromNative(cef_resource_bundle_t.get_global());
        }

        /// <summary>
        /// Returns the localized string for the specified |string_id| or an empty
        /// string if the value is not found. Include cef_pack_strings.h for a listing
        /// of valid string ID values.
        /// </summary>
        public string GetLocalizedString(int stringId)
        {
            var n_result = cef_resource_bundle_t.get_localized_string(_self, stringId);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns a CefBinaryValue containing the decompressed contents of the
        /// specified scale independent |resource_id| or NULL if not found. Include
        /// cef_pack_resources.h for a listing of valid resource ID values.
        /// </summary>
        public CefBinaryValue GetDataResource(int resource_id)
        {
            return CefBinaryValue.FromNativeOrNull(
                cef_resource_bundle_t.get_data_resource(_self, resource_id)
                );
        }

        /// <summary>
        /// Returns a CefBinaryValue containing the decompressed contents of the
        /// specified |resource_id| nearest the scale factor |scale_factor| or NULL if
        /// not found. Use a |scale_factor| value of SCALE_FACTOR_NONE for scale
        /// independent resources or call GetDataResource instead.Include
        /// cef_pack_resources.h for a listing of valid resource ID values.
        /// </summary>
        public CefBinaryValue GetDataResourceForScale(int resource_id, CefScaleFactor scale_factor)
        {
            return CefBinaryValue.FromNativeOrNull(
                cef_resource_bundle_t.get_data_resource_for_scale(_self, resource_id, scale_factor)
                );
        }
    }
}
