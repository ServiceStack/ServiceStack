//
// This file manually written from cef/include/internal/cef_types.h.
// C API name: cef_cert_status_t.
//
namespace Xilium.CefGlue
{
    /// <summary>
    /// Supported certificate status code values. See net\cert\cert_status_flags.h
    /// for more information. CERT_STATUS_NONE is new in CEF because we use an
    /// enum while cert_status_flags.h uses a typedef and static const variables.
    /// </summary>
    public enum CefCertStatus
    {
        None = 0,
        CommonNameInvalid = 1 << 0,
        DateInvalid = 1 << 1,
        AuthorityInvalid = 1 << 2,
        // 1 << 3 is reserved for ERR_CERT_CONTAINS_ERRORS (not useful with WinHTTP).
        NoRevocationMechanism = 1 << 4,
        UnableToCheckRevocation = 1 << 5,
        Revoked = 1 << 6,
        Invalid = 1 << 7,
        WeakSignatureAlgorithm = 1 << 8,
        // 1 << 9 was used for CERT_STATUS_NOT_IN_DNS
        NonUniqueName = 1 << 10,
        WeakKey = 1 << 11,
        // 1 << 12 was used for CERT_STATUS_WEAK_DH_KEY
        PinnedKeyMissing = 1 << 13,
        NameConstraintViolation = 1 << 14,
        ValidityTooLong = 1 << 15,

        // Bits 16 to 31 are for non-error statuses.
        IsEV = 1 << 16,
        RevCheckingEnabled = 1 << 17,
        // Bit 18 was CERT_STATUS_IS_DNSSEC
        Sha1SignaturePresent = 1 << 19,
        CTComplianceFailed = 1 << 20,
    }
}
