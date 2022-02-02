namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Implement this interface to receive notification when CDM registration is
    /// complete. The methods of this class will be called on the browser process
    /// UI thread.
    /// </summary>
    public abstract unsafe partial class CefRegisterCdmCallback
    {
        private void on_cdm_registration_complete(cef_register_cdm_callback_t* self, CefCdmRegistrationError result, cef_string_t* error_message)
        {
            CheckSelf(self);
            OnCdmRegistrationComplete(result, cef_string_t.ToString(error_message));
        }

        /// <summary>
        /// Method that will be called when CDM registration is complete. |result|
        /// will be CEF_CDM_REGISTRATION_ERROR_NONE if registration completed
        /// successfully. Otherwise, |result| and |error_message| will contain
        /// additional information about why registration failed.
        /// </summary>
        protected abstract void OnCdmRegistrationComplete(CefCdmRegistrationError result, string errorMessage);
    }
}
