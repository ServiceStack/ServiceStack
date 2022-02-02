//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_plugin_policy_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Plugin policies supported by CefRequestContextHandler::OnBeforePluginLoad.
    /// </summary>
    public enum CefPluginPolicy : int
    {
        /// <summary>
        /// Allow the content.
        /// </summary>
        Allow,

        /// <summary>
        /// Allow important content and block unimportant content based on heuristics.
        /// The user can manually load blocked content.
        /// </summary>
        DetectImportant,

        /// <summary>
        /// Block the content. The user can manually load blocked content.
        /// </summary>
        Block,

        /// <summary>
        /// Disable the content. The user cannot load disabled content.
        /// </summary>
        Disable,
    }
}
