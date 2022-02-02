namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to handle menu model events. The methods of this
    /// class will be called on the browser process UI thread unless otherwise
    /// indicated.
    /// </summary>
    public abstract unsafe partial class CefMenuModelDelegate
    {
        private void execute_command(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model, int command_id, CefEventFlags event_flags)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            ExecuteCommand(m_menuModel, command_id, event_flags);
        }

        /// <summary>
        /// Perform the action associated with the specified |command_id| and
        /// optional |event_flags|.
        /// </summary>
        protected abstract void ExecuteCommand(CefMenuModel menuModel, int commandId, CefEventFlags eventFlags);


        private void mouse_outside_menu(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model, cef_point_t* screen_point)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            var m_screenPoint = new CefPoint(screen_point->x, screen_point->y);
            MouseOutsideMenu(m_menuModel, m_screenPoint);
        }

        /// <summary>
        /// Called when the user moves the mouse outside the menu and over the owning
        /// window.
        /// </summary>
        protected virtual void MouseOutsideMenu(CefMenuModel menuModel, CefPoint screenPoint) { }


        private void unhandled_open_submenu(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model, int is_rtl)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            UnhandledOpenSubmenu(m_menuModel, is_rtl != 0);
        }

        /// <summary>
        /// Called on unhandled open submenu keyboard commands. |is_rtl| will be true
        /// if the menu is displaying a right-to-left language.
        /// </summary>
        protected virtual void UnhandledOpenSubmenu(CefMenuModel menuModel, bool isRtl) { }


        private void unhandled_close_submenu(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model, int is_rtl)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            UnhandledCloseSubmenu(m_menuModel, is_rtl != 0);
        }

        /// <summary>
        /// Called on unhandled close submenu keyboard commands. |is_rtl| will be true
        /// if the menu is displaying a right-to-left language.
        /// </summary>
        protected virtual void UnhandledCloseSubmenu(CefMenuModel menuModel, bool isRtl) { }


        private void menu_will_show(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            MenuWillShow(m_menuModel);
        }

        /// <summary>
        /// The menu is about to show.
        /// </summary>
        protected abstract void MenuWillShow(CefMenuModel menuModel);


        private void menu_closed(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            MenuClosed(m_menuModel);
        }

        /// <summary>
        /// The menu has closed.
        /// </summary>
        protected abstract void MenuClosed(CefMenuModel menuModel);


        private int format_label(cef_menu_model_delegate_t* self, cef_menu_model_t* menu_model, cef_string_t* label)
        {
            CheckSelf(self);

            var m_menuModel = CefMenuModel.FromNative(menu_model);
            var m_label = cef_string_t.ToString(label);

            if (FormatLabel(m_menuModel, ref m_label))
            {
                cef_string_t.Copy(m_label, label);
                return 1;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Optionally modify a menu item label. Return true if |label| was modified.
        /// </summary>
        protected abstract bool FormatLabel(CefMenuModel menuModel, ref string label);
    }
}
