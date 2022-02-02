namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Supports creation and modification of menus. See cef_menu_id_t for the
    /// command ids that have default implementations. All user-defined command ids
    /// should be between MENU_ID_USER_FIRST and MENU_ID_USER_LAST. The methods of
    /// this class can only be accessed on the browser process the UI thread.
    /// </summary>
    public sealed unsafe partial class CefMenuModel
    {
        /// <summary>
        /// Create a new MenuModel with the specified |delegate|.
        /// </summary>
        public static CefMenuModel Create(CefMenuModelDelegate handler)
        {
            return CefMenuModel.FromNative(
                cef_menu_model_t.create(handler.ToNative())
                );
        }

        /// <summary>
        /// Returns true if this menu is a submenu.
        /// </summary>
        public bool IsSubMenu
        {
            get
            {
                return cef_menu_model_t.is_sub_menu(_self) != 0;
            }
        }

        /// <summary>
        /// Clears the menu. Returns true on success.
        /// </summary>
        public bool Clear()
        {
            return cef_menu_model_t.clear(_self) != 0;
        }

        /// <summary>
        /// Returns the number of items in this menu.
        /// </summary>
        public int Count
        {
            get { return cef_menu_model_t.get_count(_self); }
        }

        /// <summary>
        /// Add a separator to the menu. Returns true on success.
        /// </summary>
        public bool AddSeparator()
        {
            return cef_menu_model_t.add_separator(_self) != 0;
        }

        /// <summary>
        /// Add an item to the menu. Returns true on success.
        /// </summary>
        public bool AddItem(int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.add_item(_self, commandId, &n_label) != 0;
            }
        }

        /// <summary>
        /// Add a check item to the menu. Returns true on success.
        /// </summary>
        public bool AddCheckItem(int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.add_check_item(_self, commandId, &n_label) != 0;
            }
        }

        /// <summary>
        /// Add a radio item to the menu. Only a single item with the specified
        /// |group_id| can be checked at a time. Returns true on success.
        /// </summary>
        public bool AddRadioItem(int commandId, string label, int groupId)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.add_radio_item(_self, commandId, &n_label, groupId) != 0;
            }
        }

        /// <summary>
        /// Add a sub-menu to the menu. The new sub-menu is returned.
        /// </summary>
        public CefMenuModel AddSubMenu(int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return CefMenuModel.FromNative(
                    cef_menu_model_t.add_sub_menu(_self, commandId, &n_label)
                    );
            }
        }

        /// <summary>
        /// Insert a separator in the menu at the specified |index|. Returns true on
        /// success.
        /// </summary>
        public bool InsertSeparatorAt(int index)
        {
            return cef_menu_model_t.insert_separator_at(_self, index) != 0;
        }

        /// <summary>
        /// Insert an item in the menu at the specified |index|. Returns true on
        /// success.
        /// </summary>
        public bool InsertItemAt(int index, int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.insert_item_at(_self, index, commandId, &n_label) != 0;
            }
        }

        /// <summary>
        /// Insert a check item in the menu at the specified |index|. Returns true on
        /// success.
        /// </summary>
        public bool InsertCheckItemAt(int index, int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.insert_check_item_at(_self, index, commandId, &n_label) != 0;
            }
        }

        /// <summary>
        /// Insert a radio item in the menu at the specified |index|. Only a single
        /// item with the specified |group_id| can be checked at a time. Returns true
        /// on success.
        /// </summary>
        public bool InsertRadioItemAt(int index, int commandId, string label, int groupId)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.insert_radio_item_at(_self, index, commandId, &n_label, groupId) != 0;
            }
        }

        /// <summary>
        /// Insert a sub-menu in the menu at the specified |index|. The new sub-menu
        /// is returned.
        /// </summary>
        public CefMenuModel InsertSubMenuAt(int index, int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return CefMenuModel.FromNative(
                    cef_menu_model_t.add_sub_menu(_self, commandId, &n_label)
                    );
            }
        }

        /// <summary>
        /// Removes the item with the specified |commandId|. Returns true on success.
        /// </summary>
        public bool Remove(int commandId)
        {
            return cef_menu_model_t.remove(_self, commandId) != 0;
        }

        /// <summary>
        /// Removes the item at the specified |index|. Returns true on success.
        /// </summary>
        public bool RemoveAt(int index)
        {
            return cef_menu_model_t.remove_at(_self, index) != 0;
        }

        /// <summary>
        /// Returns the index associated with the specified |commandId| or -1 if not
        /// found due to the command id not existing in the menu.
        /// </summary>
        public int GetIndexOf(int commandId)
        {
            return cef_menu_model_t.get_index_of(_self, commandId);
        }

        /// <summary>
        /// Returns the command id at the specified |index| or -1 if not found due to
        /// invalid range or the index being a separator.
        /// </summary>
        public int GetCommandIdAt(int index)
        {
            return cef_menu_model_t.get_command_id_at(_self, index);
        }

        /// <summary>
        /// Sets the command id at the specified |index|. Returns true on success.
        /// </summary>
        public bool SetCommandIdAt(int index, int commandId)
        {
            return cef_menu_model_t.set_command_id_at(_self, index, commandId) != 0;
        }

        /// <summary>
        /// Returns the label for the specified |commandId| or empty if not found.
        /// </summary>
        public string GetLabel(int commandId)
        {
            var n_result = cef_menu_model_t.get_label(_self, commandId);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Returns the label at the specified |index| or empty if not found due to
        /// invalid range or the index being a separator.
        /// </summary>
        public string GetLabelAt(int index)
        {
            var n_result = cef_menu_model_t.get_label_at(_self, index);
            return cef_string_userfree.ToString(n_result);
        }

        /// <summary>
        /// Sets the label for the specified |commandId|. Returns true on success.
        /// </summary>
        public bool SetLabel(int commandId, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.set_label(_self, commandId, &n_label) != 0;
            }
        }

        /// <summary>
        /// Set the label at the specified |index|. Returns true on success.
        /// </summary>
        public bool SetLabelAt(int index, string label)
        {
            fixed (char* label_str = label)
            {
                var n_label = new cef_string_t(label_str, label.Length);
                return cef_menu_model_t.set_label_at(_self, index, &n_label) != 0;
            }
        }

        /// <summary>
        /// Returns the item type for the specified |commandId|.
        /// </summary>
        public CefMenuItemType GetItemType(int commandId)
        {
            return cef_menu_model_t.get_type(_self, commandId);
        }

        /// <summary>
        /// Returns the item type at the specified |index|.
        /// </summary>
        public CefMenuItemType GetItemTypeAt(int index)
        {
            return cef_menu_model_t.get_type_at(_self, index);
        }

        /// <summary>
        /// Returns the group id for the specified |commandId| or -1 if invalid.
        /// </summary>
        public int GetGroupId(int commandId)
        {
            return cef_menu_model_t.get_group_id(_self, commandId);
        }

        /// <summary>
        /// Returns the group id at the specified |index| or -1 if invalid.
        /// </summary>
        public int GetGroupIdAt(int index)
        {
            return cef_menu_model_t.get_group_id_at(_self, index);
        }

        /// <summary>
        /// Sets the group id for the specified |commandId|. Returns true on success.
        /// </summary>
        public bool SetGroupId(int commandId, int groupId)
        {
            return cef_menu_model_t.set_group_id(_self, commandId, groupId) != 0;
        }

        /// <summary>
        /// Sets the group id at the specified |index|. Returns true on success.
        /// </summary>
        public bool SetGroupIdAt(int index, int groupId)
        {
            return cef_menu_model_t.set_group_id_at(_self, index, groupId) != 0;
        }

        /// <summary>
        /// Returns the submenu for the specified |commandId| or empty if invalid.
        /// </summary>
        public CefMenuModel GetSubMenu(int commandId)
        {
            return CefMenuModel.FromNativeOrNull(
                cef_menu_model_t.get_sub_menu(_self, commandId)
                );
        }

        /// <summary>
        /// Returns the submenu at the specified |index| or empty if invalid.
        /// </summary>
        public CefMenuModel GetSubMenuAt(int index)
        {
            return CefMenuModel.FromNativeOrNull(
                cef_menu_model_t.get_sub_menu_at(_self, index)
                );
        }

        /// <summary>
        /// Returns true if the specified |commandId| is visible.
        /// </summary>
        public bool IsVisible(int commandId)
        {
            return cef_menu_model_t.is_visible(_self, commandId) != 0;
        }

        /// <summary>
        /// Returns true if the specified |index| is visible.
        /// </summary>
        public bool IsVisibleAt(int index)
        {
            return cef_menu_model_t.is_visible(_self, index) != 0;
        }

        /// <summary>
        /// Change the visibility of the specified |commandId|. Returns true on
        /// success.
        /// </summary>
        public bool SetVisible(int commandId, bool visible)
        {
            return cef_menu_model_t.set_visible(_self, commandId, visible ? 1 : 0) != 0;
        }

        /// <summary>
        /// Change the visibility at the specified |index|. Returns true on success.
        /// </summary>
        public bool SetVisibleAt(int index, bool visible)
        {
            return cef_menu_model_t.set_visible(_self, index, visible ? 1 : 0) != 0;
        }

        /// <summary>
        /// Returns true if the specified |commandId| is enabled.
        /// </summary>
        public bool IsEnabled(int commandId)
        {
            return cef_menu_model_t.is_enabled(_self, commandId) != 0;
        }

        /// <summary>
        /// Returns true if the specified |index| is enabled.
        /// </summary>
        public bool IsEnabledAt(int index)
        {
            return cef_menu_model_t.is_enabled_at(_self, index) != 0;
        }

        /// <summary>
        /// Change the enabled status of the specified |commandId|. Returns true on
        /// success.
        /// </summary>
        public bool SetEnabled(int commandId, bool enabled)
        {
            return cef_menu_model_t.set_enabled(_self, commandId, enabled ? 1 : 0) != 0;
        }

        /// <summary>
        /// Change the enabled status at the specified |index|. Returns true on
        /// success.
        /// </summary>
        public bool SetEnabledAt(int index, bool enabled)
        {
            return cef_menu_model_t.set_enabled_at(_self, index, enabled ? 1 : 0) != 0;
        }

        /// <summary>
        /// Returns true if the specified |commandId| is checked. Only applies to
        /// check and radio items.
        /// </summary>
        public bool IsChecked(int commandId)
        {
            return cef_menu_model_t.is_checked(_self, commandId) != 0;
        }

        /// <summary>
        /// Returns true if the specified |index| is checked. Only applies to check
        /// and radio items.
        /// </summary>
        public bool IsCheckedAt(int index)
        {
            return cef_menu_model_t.is_checked_at(_self, index) != 0;
        }

        /// <summary>
        /// Check the specified |commandId|. Only applies to check and radio items.
        /// Returns true on success.
        /// </summary>
        public bool SetChecked(int commandId, bool value)
        {
            return cef_menu_model_t.set_checked(_self, commandId, value ? 1 : 0) != 0;
        }

        /// <summary>
        /// Check the specified |index|. Only applies to check and radio items. Returns
        /// true on success.
        /// </summary>
        public bool SetCheckedAt(int index, bool value)
        {
            return cef_menu_model_t.set_checked_at(_self, index, value ? 1 : 0) != 0;
        }

        /// <summary>
        /// Returns true if the specified |commandId| has a keyboard accelerator
        /// assigned.
        /// </summary>
        public bool HasAccelerator(int commandId)
        {
            return cef_menu_model_t.has_accelerator(_self, commandId) != 0;
        }

        /// <summary>
        /// Returns true if the specified |index| has a keyboard accelerator assigned.
        /// </summary>
        public bool HasAcceleratorAt(int index)
        {
            return cef_menu_model_t.has_accelerator(_self, index) != 0;
        }

        /// <summary>
        /// Set the keyboard accelerator for the specified |commandId|. |key_code| can
        /// be any virtual key or character value. Returns true on success.
        /// </summary>
        public bool SetAccelerator(int commandId, int keyCode, bool shiftPressed, bool ctrlPressed, bool altPressed)
        {
            return cef_menu_model_t.set_accelerator(
                _self,
                commandId,
                keyCode,
                shiftPressed ? 1 : 0,
                ctrlPressed ? 1 : 0,
                altPressed ? 1 : 0
                ) != 0;
        }

        /// <summary>
        /// Set the keyboard accelerator at the specified |index|. |key_code| can be
        /// any virtual key or character value. Returns true on success.
        /// </summary>
        public bool SetAcceleratorAt(int index, int keyCode, bool shiftPressed, bool ctrlPressed, bool altPressed)
        {
            return cef_menu_model_t.set_accelerator_at(
                _self,
                index,
                keyCode,
                shiftPressed ? 1 : 0,
                ctrlPressed ? 1 : 0,
                altPressed ? 1 : 0
                ) != 0;
        }

        /// <summary>
        /// Remove the keyboard accelerator for the specified |commandId|. Returns
        /// true on success.
        /// </summary>
        public bool RemoveAccelerator(int commandId)
        {
            return cef_menu_model_t.remove_accelerator(_self, commandId) != 0;
        }

        /// <summary>
        /// Remove the keyboard accelerator at the specified |index|. Returns true on
        /// success.
        /// </summary>
        public bool RemoveAcceleratorAt(int index)
        {
            return cef_menu_model_t.remove_accelerator_at(_self, index) != 0;
        }

        /// <summary>
        /// Retrieves the keyboard accelerator for the specified |commandId|. Returns
        /// true on success.
        /// </summary>
        public bool GetAccelerator(int commandId, out int keyCode, out bool shiftPressed, out bool ctrlPressed, out bool altPressed)
        {
            int n_keyCode;
            int n_shiftPressed;
            int n_ctrlPressed;
            int n_altPressed;

            var result = cef_menu_model_t.get_accelerator(_self, commandId, &n_keyCode, &n_shiftPressed, &n_ctrlPressed, &n_altPressed) != 0;

            keyCode = n_keyCode;
            shiftPressed = n_shiftPressed != 0;
            ctrlPressed = n_ctrlPressed != 0;
            altPressed = n_altPressed != 0;

            return result;
        }

        /// <summary>
        /// Retrieves the keyboard accelerator for the specified |index|. Returns true
        /// on success.
        /// </summary>
        public bool GetAcceleratorAt(int index, out int keyCode, out bool shiftPressed, out bool ctrlPressed, out bool altPressed)
        {
            int n_keyCode;
            int n_shiftPressed;
            int n_ctrlPressed;
            int n_altPressed;

            var result = cef_menu_model_t.get_accelerator_at(_self, index, &n_keyCode, &n_shiftPressed, &n_ctrlPressed, &n_altPressed) != 0;

            keyCode = n_keyCode;
            shiftPressed = n_shiftPressed != 0;
            ctrlPressed = n_ctrlPressed != 0;
            altPressed = n_altPressed != 0;

            return result;
        }

        /// <summary>
        /// Set the explicit color for |command_id| and |color_type| to |color|.
        /// Specify a |color| value of 0 to remove the explicit color. If no explicit
        /// color or default color is set for |color_type| then the system color will
        /// be used. Returns true on success.
        /// </summary>
        public bool SetColor(int commandId, CefMenuColorType colorType, uint color)
        {
            return cef_menu_model_t.set_color(_self, commandId, colorType, color) != 0;
        }

        /// <summary>
        /// Set the explicit color for |command_id| and |index| to |color|. Specify a
        /// |color| value of 0 to remove the explicit color. Specify an |index| value
        /// of -1 to set the default color for items that do not have an explicit
        /// color set. If no explicit color or default color is set for |color_type|
        /// then the system color will be used. Returns true on success.
        /// </summary>
        public bool SetColorAt(int index, CefMenuColorType colorType, uint color)
        {
            return cef_menu_model_t.set_color_at(_self, index, colorType, color) != 0;
        }

        /// <summary>
        /// Returns in |color| the color that was explicitly set for |command_id| and
        /// |color_type|. If a color was not set then 0 will be returned in |color|.
        /// Returns true on success.
        /// </summary>
        public bool GetColor(int commandId, CefMenuColorType colorType, out uint color)
        {
            uint n_color;
            var result = cef_menu_model_t.get_color(_self, commandId, colorType, &n_color) != 0;
            color = n_color;
            return result;
        }

        /// <summary>
        /// Returns in |color| the color that was explicitly set for |command_id| and
        /// |color_type|. Specify an |index| value of -1 to return the default color
        /// in |color|. If a color was not set then 0 will be returned in |color|.
        /// Returns true on success.
        /// </summary>
        public bool GetColorAt(int index, CefMenuColorType colorType, out uint color)
        {
            uint n_color;
            var result = cef_menu_model_t.get_color_at(_self, index, colorType, &n_color) != 0;
            color = n_color;
            return result;
        }

        /// <summary>
        /// Sets the font list for the specified |command_id|. If |font_list| is empty
        /// the system font will be used. Returns true on success. The format is
        /// "&lt;FONT_FAMILY_LIST&gt;,[STYLES] &lt;SIZE&gt;", where:
        /// - FONT_FAMILY_LIST is a comma-separated list of font family names,
        /// - STYLES is an optional space-separated list of style names (case-sensitive
        /// "Bold" and "Italic" are supported), and
        /// - SIZE is an integer font size in pixels with the suffix "px".
        /// Here are examples of valid font description strings:
        /// - "Arial, Helvetica, Bold Italic 14px"
        /// - "Arial, 14px"
        /// </summary>
        public bool SetFontList(int commandId, string fontList)
        {
            fixed (char* fontList_str = fontList)
            {
                var n_fontList = new cef_string_t(fontList_str, fontList != null ? fontList.Length : 0);
                return cef_menu_model_t.set_font_list(_self, commandId, &n_fontList) != 0;
            }
        }

        /// <summary>
        /// Sets the font list for the specified |index|. Specify an |index| value of
        /// -1 to set the default font. If |font_list| is empty the system font will
        /// be used. Returns true on success. The format is
        /// "&lt;FONT_FAMILY_LIST&gt;,[STYLES] &lt;SIZE&gt;", where:
        /// - FONT_FAMILY_LIST is a comma-separated list of font family names,
        /// - STYLES is an optional space-separated list of style names (case-sensitive
        /// "Bold" and "Italic" are supported), and
        /// - SIZE is an integer font size in pixels with the suffix "px".
        /// Here are examples of valid font description strings:
        /// - "Arial, Helvetica, Bold Italic 14px"
        /// - "Arial, 14px"
        /// </summary>
        public bool SetFontListAt(int index, string fontList)
        {
            fixed (char* fontList_str = fontList)
            {
                var n_fontList = new cef_string_t(fontList_str, fontList != null ? fontList.Length : 0);
                return cef_menu_model_t.set_font_list_at(_self, index, &n_fontList) != 0;
            }
        }
    }
}
