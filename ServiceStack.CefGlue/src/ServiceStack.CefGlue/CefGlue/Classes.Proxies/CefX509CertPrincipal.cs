namespace Xilium.CefGlue
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using Xilium.CefGlue.Interop;

    /// <summary>
    /// Class representing the issuer or subject field of an X.509 certificate.
    /// </summary>
    public sealed unsafe partial class CefX509CertPrincipal
    {
        /// <summary>
        /// Returns a name that can be used to represent the issuer. It tries in this
        /// order: Common Name (CN), Organization Name (O) and Organizational Unit
        /// Name (OU) and returns the first non-empty one found.
        /// </summary>
        public string GetDisplayName()
        {
            return cef_string_userfree.ToString(
                cef_x509cert_principal_t.get_display_name(_self)
                );
        }

        /// <summary>
        /// Returns the common name.
        /// </summary>
        public string GetCommonName()
        {
            return cef_string_userfree.ToString(
                cef_x509cert_principal_t.get_common_name(_self)
                );
        }

        /// <summary>
        /// Returns the locality name.
        /// </summary>
        public string GetLocalityName()
        {
            return cef_string_userfree.ToString(
                cef_x509cert_principal_t.get_locality_name(_self)
                );
        }

        /// <summary>
        /// Returns the state or province name.
        /// </summary>
        public string GetStateOrProvinceName()
        {
            return cef_string_userfree.ToString(
                cef_x509cert_principal_t.get_state_or_province_name(_self)
                );
        }

        /// <summary>
        /// Returns the country name.
        /// </summary>
        public string GetCountryName()
        {
            return cef_string_userfree.ToString(
                cef_x509cert_principal_t.get_country_name(_self)
                );
        }

        /// <summary>
        /// Retrieve the list of street addresses.
        /// </summary>
        public string[] GetStreetAddresses()
        {
            cef_string_list* n_result = libcef.string_list_alloc();
            cef_x509cert_principal_t.get_street_addresses(_self, n_result);
            var result = cef_string_list.ToArray(n_result);
            libcef.string_list_free(n_result);
            return result;
        }

        /// <summary>
        /// Retrieve the list of organization names.
        /// </summary>
        public string[] GetOrganizationNames()
        {
            cef_string_list* n_result = libcef.string_list_alloc();
            cef_x509cert_principal_t.get_organization_names(_self, n_result);
            var result = cef_string_list.ToArray(n_result);
            libcef.string_list_free(n_result);
            return result;
        }

        /// <summary>
        /// Retrieve the list of organization unit names.
        /// </summary>
        public string[] GetOrganizationUnitNames()
        {
            cef_string_list* n_result = libcef.string_list_alloc();
            cef_x509cert_principal_t.get_organization_unit_names(_self, n_result);
            var result = cef_string_list.ToArray(n_result);
            libcef.string_list_free(n_result);
            return result;
        }

        /// <summary>
        /// Retrieve the list of domain components.
        /// </summary>
        public string[] GetDomainComponents()
        {
            cef_string_list* n_result = libcef.string_list_alloc();
            cef_x509cert_principal_t.get_domain_components(_self, n_result);
            var result = cef_string_list.ToArray(n_result);
            libcef.string_list_free(n_result);
            return result;
        }
    }
}
