using System;
using System.Collections.Generic;
using System.Net;

namespace ServiceStack.Stripe.Types;
    
public class StripeErrors
{
    public StripeError Error { get; set; }
}

public class StripeError
{
    public string Type { get; set; }
    public string Message { get; set; }
    public string Code { get; set; }
    public string Param { get; set; }
    public string DeclineCode { get; set; }
}

public class StripeException : Exception
{
    public StripeException(StripeError error)
        : base(error.Message)
    {
        Code = error.Code;
        Param = error.Param;
        Type = error.Type;
        DeclineCode = error.DeclineCode;
    }

    public string Code { get; set; }
    public string DeclineCode { get; set; }
    public string Param { get; set; }
    public string Type { get; set; }
    public HttpStatusCode StatusCode { get; set; }
}

public class StripeReference
{
    public string Id { get; set; }
    public bool Deleted { get; set; }
}

public class StripeObject
{
    public StripeType? Object { get; set; }
}

public class StripeId : StripeObject
{
    public string Id { get; set; }
}

public enum StripeType
{
    unknown,
    account,
    card,
    charge,
    coupon,
    customer,
    discount,
    dispute,
    @event,
    invoiceitem,
    invoice,
    line_item,
    plan,
    subscription,
    token,
    transfer,
    list,
    product,
}

public class StripeInvoice : StripeId
{
    public DateTime Date { get; set; }
    public DateTime PeriodStart { get; set; }
    public DateTime PeriodEnd { get; set; }
    public StripeCollection<StripeLineItem> Lines { get; set; }
    public int Subtotal { get; set; }
    public int Total { get; set; }
    public string Customer { get; set; }
    public bool Attempted { get; set; }
    public bool Closed { get; set; }
    public bool Paid { get; set; }
    public bool Livemode { get; set; }
    public int AttemptCount { get; set; }
    public int AmountDue { get; set; }
    public string Currency { get; set; }
    public int StartingBalance { get; set; }
    public int? EndingBalance { get; set; }
    public DateTime? NextPaymentAttempt { get; set; }
    public string Charge { get; set; }
    public StripeDiscount Discount { get; set; }
    public int? ApplicationFee { get; set; }
}

public class StripeCollection<T> : StripeId
{
    public string Url { get; set; }
    public bool? HasMore { get; set; }
    public List<T> Data { get; set; }
}

public class StripeLineItem : StripeId
{
    public string Type { get; set; }
    public bool Livemode { get; set; }
    public int Amount { get; set; }
    public string Currency { get; set; }
    public bool Proration { get; set; }
    public StripePeriod Period { get; set; }
    public int? Quantity { get; set; }
    public StripePlan Plan { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class StripeProduct : StripeId, IStripeProduct
{
    public bool Active { get; set; }
    public string[] Attributes { get; set; }
    public string Caption { get; set; }
    public DateTime? Created { get; set; }
    public string[] DeactivateOn { get; set; }
    public string Description { get; set; }
    public string[] Images { get; set; }
    public bool Livemode { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string Name { get; set; }
    public StripePackageDimensions PackageDimensions { get; set; }
    public bool Shippable { get; set; }
    public StripeCollection<StripeSku> Skus { get; set; }
    public string StatementDescriptor { get; set; }
    public StripeProductType Type { get; set; }
    public DateTime? Updated { get; set; }
    public string Url { get; set; }
}

public enum StripeProductType
{
    good,
    service,
}

public class StripePackageDimensions
{
    /// <summary>
    /// Height in inches
    /// </summary>
    public decimal Height { get; set; }

    /// <summary>
    /// Width in inches
    /// </summary>
    public decimal Width { get; set; }

    /// <summary>
    /// Weight in inches
    /// </summary>
    public decimal Weight { get; set; }

    /// <summary>
    /// Length in inches
    /// </summary>
    public decimal Length { get; set; }
}

public class StripeSku : StripeId
{
    public bool Active { get; set; }
    public Dictionary<string, string> Attributes { get; set; }
    public DateTime? Created { get; set; }
    public string Currency { get; set; }
    public string Image { get; set; }
    public StripeInventory Inventory { get; set; }
    public bool Livemode { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public StripePackageDimensions PackageDimensions { get; set; }
}

public class StripeInventory
{
    public int Quantity { get; set; }
    public StripeInventoryType Type { get; set; }
    public StripeInventoryValue Value { get; set; }
}

public enum StripeInventoryType
{
    finite,
    bucket,
    infinite,
}

public enum StripeInventoryValue
{
    in_stock,
    limited,
    out_of_stock,
}

public class StripePlan : StripeId
{
    public int Amount { get; set; }
    public DateTime? Created { get; set; }
    public string Currency { get; set; }
    public StripePlanInterval Interval { get; set; }
    public int? IntervalCount { get; set; }
    public bool Livemode { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public string Nickname { get; set; }
    public string Product { get; set; }
    public int? TrialPeriodDays { get; set; }
}

public class StripePlanProduct
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string StatementDescriptor { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class StripePeriod
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

public enum StripePlanInterval
{
    month,
    year
}

public class StripeDiscount : StripeId
{
    public string Customer { get; set; }
    public StripeCoupon Coupon { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? End { get; set; }
}

public class StripeCoupon : StripeId
{
    public int? AmountOff { get; set; }
    public DateTime? Created { get; set; }
    public string Currency { get; set; }
    public StripeCouponDuration Duration { get; set; }
    public int? DurationInMonths { get; set; }
    public bool Livemode { get; set; }
    public int? MaxRedemptions { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public int? PercentOff { get; set; }
    public DateTime? RedeemBy { get; set; }
    public int TimesRedeemed { get; set; }
    public bool Valid { get; set; }
}

public enum StripeCouponDuration
{
    forever,
    once,
    repeating
}

public class StripeCustomer : StripeId
{
    public int AccountBalance { get; set; }
    public string BusinessVatId { get; set; }
    public DateTime? Created { get; set; }
    public string DefaultSource { get; set; }
    public bool? Delinquent { get; set; }
    public string Description { get; set; }
    public StripeDiscount Discount { get; set; }
    public string Email { get; set; }
    public string InvoicePrefix { get; set; }
    public bool Livemode { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public StripeShipping Shipping { get; set; }
    public StripeCollection<StripeCard> Sources { get; set; }
    public StripeCollection<StripeSubscription> Subscriptions { get; set; }
    public bool Deleted { get; set; }
    public string Currency { get; set; }
}

public class StripeShipping
{
    public StripeAddress Address { get; set; }
    public string Name { get; set; }
    public string Phone { get; set; }
}

public class StripeDateRange
{
    public DateTime? Gt { get; set; }
    public DateTime? Gte { get; set; }
    public DateTime? Lt { get; set; }
    public DateTime? Lte { get; set; }
}

public class StripeCard : StripeId
{
    public StripeCard()
    {
        this.Object = StripeType.card;
    }

    public string Brand { get; set; }
    public string Number { get; set; }
    public string Last4 { get; set; }
    public string DynamicLast4 { get; set; }
    public int ExpMonth { get; set; }
    public int ExpYear { get; set; }
    public string Cvc { get; set; }
    public string Name { get; set; }

    public string AddressCity { get; set; }
    public string AddressCountry { get; set; }
    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string AddressState { get; set; }
    public string AddressZip { get; set; }
    public StripeCvcCheck? CvcCheck { get; set; }
    public string AddressLine1Check { get; set; }
    public string AddressZipCheck { get; set; }

    public string Funding { get; set; }

    public string Fingerprint { get; set; }
    public string Customer { get; set; }
    public string Country { get; set; }
}

public enum StripeCvcCheck
{
    Unknown,
    Pass,
    Fail,
    Unchecked
}

public class StripeSubscription : StripeId
{
    public DateTime? CurrentPeriodEnd { get; set; }
    public StripeSubscriptionStatus Status { get; set; }
    public StripePlan Plan { get; set; }
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? Start { get; set; }
    public DateTime? TrialStart { get; set; }
    public bool? CancelAtPeriodEnd { get; set; }
    public DateTime? TrialEnd { get; set; }
    public DateTime? CanceledAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string Customer { get; set; }
    public int Quantity { get; set; }
}

public enum StripeSubscriptionStatus
{
    Unknown,
    Trialing,
    Active,
    PastDue,
    Canceled,
    Unpaid
}

public class StripeCharge : StripeId
{
    public bool LiveMode { get; set; }
    public int Amount { get; set; }
    public bool Captured { get; set; }
    public StripeCard Source { get; set; }
    public DateTime Created { get; set; }
    public string Currency { get; set; }
    public bool Paid { get; set; }
    public bool Refunded { get; set; }
    public StripeCollection<StripeRefund> Refunds { get; set; }
    public int AmountRefunded { get; set; }
    public string BalanceTransaction { get; set; }
    public string Customer { get; set; }
    public string Description { get; set; }
    public StripeDispute Dispute { get; set; }
    public string FailureCode { get; set; }
    public string FailureMessage { get; set; }
    public string Invoice { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class CreateStripeCharge : StripeId
{
    public int Amount { get; set; }
    public string Currency { get; set; }
    public string Customer { get; set; }
    public StripeCard Card { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
    public bool Capture { get; set; }
    public int? ApplicationFee { get; set; }
}

public class GetStripeCharge
{
    public string Id { get; set; }
}

public class UpdateStripeCharge
{
    public string Description { get; set; }
    public Dictionary<string, string> Metadata { get; set; }
}

public class StripeRefund : StripeObject
{
    public int Amount { get; set; }
    public string Charge { get; set; }
    public DateTime Created { get; set; }
    public string Currency { get; set; }
    public string BalanceTransaction { get; set; }
    public string Description { get; set; }
    public string Reason { get; set; }
    public string ReceiptNumber { get; set; }
}

public class StripeDispute : StripeObject
{
    public StripeDisputeStatus Status { get; set; }
    public string Evidence { get; set; }
    public string Charge { get; set; }
    public DateTime? Created { get; set; }
    public string Currency { get; set; }
    public int Amount;
    public bool LiveMode { get; set; }
    public StripeDisputeReason Reason { get; set; }
    public DateTime? EvidenceDueBy { get; set; }
}

public class StripeFeeDetail
{
    public string Type { get; set; }
    public string Currency { get; set; }
    public string Application { get; set; }
    public string Description { get; set; }
    public int Amount { get; set; }
}

public enum StripeDisputeStatus
{
    Won,
    Lost,
    NeedsResponse,
    UnderReview
}

public enum StripeDisputeReason
{
    Duplicate,
    Fraudulent,
    SubscriptionCanceled,
    ProductUnacceptable,
    ProductNotReceived,
    Unrecognized,
    CreditNotProcessed,
    General
}

public static class Currencies
{
    public const string UnitedArabEmiratesDirham = "AED";
    public const string AfghanAfghani = "AFN";
    public const string AlbanianLek = "ALL";
    public const string ArmenianDram = "AMD";
    public const string NetherlandsAntilleanGulden = "ANG";
    public const string AngolanKwanza = "AOA";
    public const string ArgentinePeso = "ARS";
    public const string AustralianDollar = "AUD";
    public const string ArubanFlorin = "AWG";
    public const string AzerbaijaniManat = "AZN";
    public const string BosniaAndHerzegovinaConvertibleMark = "BAM";
    public const string BarbadianDollar = "BBD";
    public const string BangladeshiTaka = "BDT";
    public const string BulgarianLev = "BGN";
    public const string BurundianFran = "BIF";
    public const string BermudianDollar = "BMD";
    public const string BruneiDollar = "BND";
    public const string BolivianBoliviano = "BOB";
    public const string BrazilianReal = "BRL";
    public const string BahamianDollar = "BSD";
    public const string BotswanaPula = "BWP";
    public const string BelizeDollar = "BZD";
    public const string CanadianDollar = "CAD";
    public const string CongoleseFranc = "CDF";
    public const string SwissFranc = "CHF";
    public const string ChileanPeso = "CLP";
    public const string ChineseRenminbiYuan = "CNY";
    public const string ColombianPeso = "COP";
    public const string CostaRicanColón = "CRC";
    public const string CapeVerdeanEscudo = "CVE";
    public const string CzechKoruna = "CZK";
    public const string DjiboutianFranc = "DJF";
    public const string DanishKrone = "DKK";
    public const string DominicanPeso = "DOP";
    public const string AlgerianDinar = "DZD";
    public const string EstonianKroon = "EEK";
    public const string EgyptianPound = "EGP";
    public const string EthiopianBirr = "ETB";
    public const string Euro = "EUR";
    public const string FijianDollar = "FJD";
    public const string FalklandIslandsPound = "FKP";
    public const string BritishPound = "GBP";
    public const string GeorgianLari = "GEL";
    public const string GibraltarPound = "GIP";
    public const string GambianDalasi = "GMD";
    public const string GuineanFranc = "GNF";
    public const string GuatemalanQuetzal = "GTQ";
    public const string GuyaneseDollar = "GYD";
    public const string HongKongDollar = "HKD";
    public const string HonduranLempira = "HNL";
    public const string CroatianKuna = "HRK";
    public const string HaitianGourde = "HTG";
    public const string HungarianForint = "HUF";
    public const string IndonesianRupiah = "IDR";
    public const string IsraeliNewSheqel = "ILS";
    public const string IndianRupee = "INR";
    public const string IcelandicKróna = "ISK";
    public const string JamaicanDollar = "JMD";
    public const string JapaneseYen = "JPY";
    public const string KenyanShilling = "KES";
    public const string KyrgyzstaniSom = "KGS";
    public const string CambodianRiel = "KHR";
    public const string ComorianFranc = "KMF";
    public const string SouthKoreanWon = "KRW";
    public const string CaymanIslandsDollar = "KYD";
    public const string KazakhstaniTenge = "KZT";
    public const string LaoKip = "LAK";
    public const string LebanesePound = "LBP";
    public const string SriLankanRupee = "LKR";
    public const string LiberianDollar = "LRD";
    public const string LesothoLoti = "LSL";
    public const string LithuanianLitas = "LTL";
    public const string LatvianLats = "LVL";
    public const string MoroccanDirham = "MAD";
    public const string MoldovanLeu = "MDL";
    public const string MalagasyAriary = "MGA";
    public const string MacedonianDenar = "MKD";
    public const string MongolianTögrög = "MNT";
    public const string MacanesePataca = "MOP";
    public const string MauritanianOuguiya = "MRO";
    public const string MauritianRupee = "MUR";
    public const string MaldivianRufiyaa = "MVR";
    public const string MalawianKwacha = "MWK";
    public const string MexicanPeso = "MXN";
    public const string MalaysianRinggit = "MYR";
    public const string MozambicanMetical = "MZN";
    public const string NamibianDollar = "NAD";
    public const string NigerianNaira = "NGN";
    public const string NicaraguanCórdoba = "NIO";
    public const string NorwegianKrone = "NOK";
    public const string NepaleseRupee = "NPR";
    public const string NewZealandDollar = "NZD";
    public const string PanamanianBalboa = "PAB";
    public const string PeruvianNuevoSol = "PEN";
    public const string PapuaNewGuineanKina = "PGK";
    public const string PhilippinePeso = "PHP";
    public const string PakistaniRupee = "PKR";
    public const string PolishZłoty = "PLN";
    public const string ParaguayanGuaraní = "PYG";
    public const string QatariRiyal = "QAR";
    public const string RomanianLeu = "RON";
    public const string SerbianDinar = "RSD";
    public const string RussianRuble = "RUB";
    public const string RwandanFranc = "RWF";
    public const string SaudiRiyal = "SAR";
    public const string SolomonIslandsDollar = "SBD";
    public const string SeychelloisRupee = "SCR";
    public const string SwedishKrona = "SEK";
    public const string SingaporeDollar = "SGD";
    public const string SaintHelenianPound = "SHP";
    public const string SierraLeoneanLeone = "SLL";
    public const string SomaliShilling = "SOS";
    public const string SurinameseDollar = "SRD";
    public const string SãoToméandPríncipeDobra = "STD";
    public const string SalvadoranColón = "SVC";
    public const string SwaziLilangeni = "SZL";
    public const string ThaiBaht = "THB";
    public const string TajikistaniSomoni = "TJS";
    public const string TonganPaʻanga = "TOP";
    public const string TurkishLira = "TRY";
    public const string TrinidadandTobagoDollar = "TTD";
    public const string NewTaiwanDollar = "TWD";
    public const string TanzanianShilling = "TZS";
    public const string UkrainianHryvnia = "UAH";
    public const string UgandanShilling = "UGX";
    public const string UnitedStatesDollar = "USD";
    public const string UruguayanPeso = "UYU";
    public const string UzbekistaniSom = "UZS";
    public const string VenezuelanBolívar = "VEF";
    public const string VietnameseĐồng = "VND";
    public const string VanuatuVatu = "VUV";
    public const string SamoanTala = "WST";
    public const string CentralAfricanCfaFranc = "XAF";
    public const string EastCaribbeanDollar = "XCD";
    public const string WestAfricanCfaFranc = "XOF";
    public const string CfpFranc = "XPF";
    public const string YemeniRial = "YER";
    public const string SouthAfricanRand = "ZAR";
}