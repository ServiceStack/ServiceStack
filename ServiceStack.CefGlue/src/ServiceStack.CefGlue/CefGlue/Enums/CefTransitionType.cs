//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_resource_type_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Transition type for a request. Made up of one source value and 0 or more
    /// qualifiers.
    /// </summary>
    [Flags]
    public enum CefTransitionType : uint
    {
        /// <summary>
        /// Source is a link click or the JavaScript window.open function. This is
        /// also the default value for requests like sub-resource loads that are not
        /// navigations.
        /// </summary>
        Link = 0,

        /// <summary>
        /// Source is some other "explicit" navigation. This is the default value for
        /// navigations where the actual type is unknown. See also TT_DIRECT_LOAD_FLAG.
        /// </summary>
        Explicit = 1,

        /// <summary>
        /// Source is a subframe navigation. This is any content that is automatically
        /// loaded in a non-toplevel frame. For example, if a page consists of several
        /// frames containing ads, those ad URLs will have this transition type.
        /// The user may not even realize the content in these pages is a separate
        /// frame, so may not care about the URL.
        /// </summary>
        AutoSubframe = 3,

        /// <summary>
        /// Source is a subframe navigation explicitly requested by the user that will
        /// generate new navigation entries in the back/forward list. These are
        /// probably more important than frames that were automatically loaded in
        /// the background because the user probably cares about the fact that this
        /// link was loaded.
        /// </summary>
        ManualSubframe = 4,

        /// <summary>
        /// Source is a form submission by the user. NOTE: In some situations
        /// submitting a form does not result in this transition type. This can happen
        /// if the form uses a script to submit the contents.
        /// </summary>
        FormSubmit = 7,

        /// <summary>
        /// Source is a "reload" of the page via the Reload function or by re-visiting
        /// the same URL. NOTE: This is distinct from the concept of whether a
        /// particular load uses "reload semantics" (i.e. bypasses cached data).
        /// </summary>
        Reload = 8,

        /// <summary>
        /// General mask defining the bits used for the source values.
        /// </summary>
        SourceMask = 0xFF,

        // Qualifiers.
        // Any of the core values above can be augmented by one or more qualifiers.
        // These qualifiers further define the transition.

        /// <summary>
        /// Attempted to visit a URL but was blocked.
        /// </summary>
        BlockedFlag = 0x00800000,

        /// <summary>
        /// Used the Forward or Back function to navigate among browsing history.
        /// Will be ORed to the transition type for the original load.
        /// </summary>
        ForwardBackFlag = 0x01000000,

        /// <summary>
        /// Loaded a URL directly via CreateBrowser, LoadURL or LoadRequest.
        /// </summary>
        DirectLoadFlag = 0x02000000,

        /// <summary>
        /// The beginning of a navigation chain.
        /// </summary>
        ChainStartFlag = 0x10000000,

        /// <summary>
        /// The last transition in a redirect chain.
        /// </summary>
        ChainEndFlag = 0x20000000,

        /// <summary>
        /// Redirects caused by JavaScript or a meta refresh tag on the page.
        /// </summary>
        ClientRedirectFlag = 0x40000000,

        /// <summary>
        /// Redirects sent from the server by HTTP headers.
        /// </summary>
        ServerRedirectFlag = 0x80000000,

        /// <summary>
        /// Used to test whether a transition involves a redirect.
        /// </summary>
        IsRedirectMask = 0xC0000000,

        /// <summary>
        /// General mask defining the bits used for the qualifiers.
        /// </summary>
        QualifierMask = 0xFFFFFF00,
    }
}
