//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_file_dialog_mode_t.
//
namespace Xilium.CefGlue
{
    using System;

    /// <summary>
    /// Supported file dialog modes.
    /// </summary>
    [Flags]
    public enum CefFileDialogMode
    {
        /// <summary>
        /// Requires that the file exists before allowing the user to pick it.
        /// </summary>
        Open = 0,

        /// <summary>
        /// Like Open, but allows picking multiple files to open.
        /// </summary>
        OpenMultiple,

        /// <summary>
        /// Like Open, but selects a folder to open.
        /// </summary>
        OpenFolder,

        /// <summary>
        /// Allows picking a nonexistent file, and prompts to overwrite if the file
        /// already exists.
        /// </summary>
        Save,

        /// <summary>
        /// General mask defining the bits used for the type values.
        /// </summary>
        TypeMask = 0xFF,

        // Qualifiers.
        // Any of the type values above can be augmented by one or more qualifiers.
        // These qualifiers further define the dialog behavior.

        /// <summary>
        /// Prompt to overwrite if the user selects an existing file with the Save
        /// dialog.
        /// </summary>
        OverwritePromptFlag = 0x01000000,

        /// <summary>
        /// Do not display read-only files.
        /// </summary>
        HideReadOnlyFlag = 0x02000000,
    }
}
