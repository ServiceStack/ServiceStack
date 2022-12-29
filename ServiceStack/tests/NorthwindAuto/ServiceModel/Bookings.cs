﻿// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using System;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' fill-rule='evenodd' d='M8 4h8V2h2v2h1a2 2 0 0 1 2 2v14a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2V6a2 2 0 0 1 2-2h1V2h2v2ZM5 8v12h14V8H5Zm2 3h2v2H7v-2Zm4 0h2v2h-2v-2Zm4 0h2v2h-2v-2Zm0 4h2v2h-2v-2Zm-4 0h2v2h-2v-2Zm-4 0h2v2H7v-2Z'/></svg>")]
[Description("Booking Details")]
[Notes("Captures a Persons Name & Room Booking information")]
public class Booking : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public int RoomNumber { get; set; }
    [IntlDateTime(DateStyle.Long)]
    public DateTime BookingStartDate { get; set; }
    [IntlRelativeTime]
    public DateTime? BookingEndDate { get; set; }
    [IntlNumber(Currency = NumberCurrency.USD)]
    public decimal Cost { get; set; }
    public string? Notes { get; set; }
    public bool? Cancelled { get; set; }
    [Computed, IntlRelativeTime]
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

[Tag("Bookings"), Description("Create a new Booking")]
[LocodeCss(Field="col-span-12 sm:col-span-6", Fieldset = "grid grid-cols-8 gap-2", Form = "border overflow-hidden max-w-screen-lg")]
[ExplorerCss(Field="col-span-12 sm:col-span-6", Fieldset = "grid grid-cols-6 gap-8", Form = "border border-indigo-500 overflow-hidden max-w-screen-lg")]
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
[LocodeCss(Field="col-span-12 sm:col-span-6", Fieldset = "grid grid-cols-6 gap-8", Form = "border border-indigo-500 overflow-hidden max-w-screen-lg")]
[ExplorerCss(Field="col-span-12 sm:col-span-6", Fieldset = "grid grid-cols-8 gap-2", Form = "border overflow-hidden max-w-screen-lg")]
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

[Field(nameof(CreatedBy), Type = "textarea", FieldCss="col-span-12 text-center", InputCss = "bg-gray-100")]
public class UpdateBooking2 : AuditBase {}

[Tag("Bookings"), Description("Delete a Booking")]
[Route("/booking/{Id}", "DELETE")]
[ValidateHasRole("Admin")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteBooking : IDeleteDb<Booking>, IReturnVoid
{
    public int Id { get; set; }
}
