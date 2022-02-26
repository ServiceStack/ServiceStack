// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using System;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 48 48'><path fill='#CFD8DC' d='M5 38V14h38v24c0 2.2-1.8 4-4 4H9c-2.2 0-4-1.8-4-4z'/><path fill='#F44336' d='M43 10v6H5v-6c0-2.2 1.8-4 4-4h30c2.2 0 4 1.8 4 4z'/><g fill='#B71C1C'><circle cx='33' cy='10' r='3'/><circle cx='15' cy='10' r='3'/></g><path fill='#B0BEC5' d='M33 3c-1.1 0-2 .9-2 2v5c0 1.1.9 2 2 2s2-.9 2-2V5c0-1.1-.9-2-2-2zM15 3c-1.1 0-2 .9-2 2v5c0 1.1.9 2 2 2s2-.9 2-2V5c0-1.1-.9-2-2-2z'/><path fill='#90A4AE' d='M13 20h4v4h-4zm6 0h4v4h-4zm6 0h4v4h-4zm6 0h4v4h-4zm-18 6h4v4h-4zm6 0h4v4h-4zm6 0h4v4h-4zm6 0h4v4h-4zm-18 6h4v4h-4zm6 0h4v4h-4zm6 0h4v4h-4zm6 0h4v4h-4z'/></svg>")]
[Description("Booking Details")]
[Notes("Captures a Persons Name & Room Booking information")]
public class Booking : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int RoomNumber { get; set; }
    [Intl(IntlFormat.DateTime, Date = DateStyle.Long)]
    public DateTime BookingStartDate { get; set; }
    [IntlRelativeTime]
    public DateTime? BookingEndDate { get; set; }
    [Intl(IntlFormat.Number, Currency = NumberCurrency.USD)]
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public bool? Cancelled { get; set; }
    [IntlRelativeTime]
    public TimeSpan TimeAgo => DateTime.UtcNow - this.BookingStartDate;
}

public enum RoomType
{
    Single,
    Double,
    Queen,
    Twin,
    Suite,
}

[Tag("Bookings"), Description("Find Bookings")]
[Notes("Find out how to quickly create a <a class='svg-external' target='_blank' href='https://youtu.be/nhc4MZufkcM'>C# Bookings App from Scratch</a>")]
[Route("/bookings", "GET")]
[Route("/bookings/{Id}", "GET")]
[AutoApply(Behavior.AuditQuery)]
public class QueryBookings : QueryDb<Booking> 
{
    public int? Id { get; set; }
}

// Uncomment below to enable DeletedBookings API to view deleted bookings:
// [Route("/bookings/deleted")]
// [AutoFilter(QueryTerm.Ensure, nameof(AuditBase.DeletedDate), Template = SqlTemplate.IsNotNull)]
// public class DeletedBookings : QueryDb<Booking> {}

[Tag("Bookings"), Description("Create a new Booking"), QueryCss(Field="col-span-12 sm:col-span-4")]
[Route("/bookings", "POST")]
[ValidateHasRole("Employee")]
[AutoApply(Behavior.AuditCreate)]
public class CreateBooking : ICreateDb<Booking>, IReturn<IdResponse>
{
    [Description("Name this Booking is for"), ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal Cost { get; set; }
    public DateTime BookingStartDate { get; set; }
    [FieldCss(Label = "text-green-800", Input = "bg-green-100")]
    public DateTime? BookingEndDate { get; set; }
    [Input(Type = "textarea"), FieldCss(Field="col-span-12 text-center", Input = "bg-green-100")]
    public string? Notes { get; set; }
}

[Tag("Bookings"), Description("Update an existing Booking")]//, QueryStyles(Rows="col-span-12 sm:col-span-4")
[ValidateHasRole("Manager")]
[AutoApply(Behavior.AuditModify)]
[Field(nameof(BookingEndDate), LabelCss = "text-gray-800", InputCss = "bg-gray-100")]
[Field(nameof(Notes), Type = "textarea", FieldCss="col-span-12 text-center", InputCss = "bg-gray-100")]
#if true
[Route("/booking/{Id}", "PATCH")]
public class UpdateBooking : IPatchDb<Booking>, IReturn<IdResponse>
{
    public int Id { get; set; }
    [ValidateNotNull]
    public string? Name { get; set; }
    public RoomType? RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int? RoomNumber { get; set; }
    [ValidateGreaterThan(0), AllowReset]
    public decimal? Cost { get; set; }
    public DateTime? BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    public string? Notes { get; set; }
    public bool? Cancelled { get; set; }
}
#else
[Route("/booking/{Id}", "PUT")]
public class UpdateBooking : IUpdateDb<Booking>, IReturn<IdResponse>
{
    public int Id { get; set; }
    [Description("Name this Booking is for"), ValidateNotEmpty]
    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal Cost { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    public string? Notes { get; set; }
    public bool? Cancelled { get; set; }
}
#endif

[Tag("Bookings"), Description("Delete a Booking")]
[Route("/booking/{Id}", "DELETE")]
[ValidateHasRole("Admin")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteBooking : IDeleteDb<Booking>, IReturnVoid
{
    public int Id { get; set; }
}
