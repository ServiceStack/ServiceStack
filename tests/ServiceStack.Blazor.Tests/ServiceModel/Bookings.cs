// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using System;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Description("Booking Details")]
[Notes("Captures a Persons Name & Room Booking information")]
public class Booking : AuditBase
{
    [AutoIncrement]
    public int Id { get; set; }
    public string Name { get; set; }
    public RoomType RoomType { get; set; }
    public int RoomNumber { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    public decimal Cost { get; set; }
    public string Notes { get; set; }
    public bool? Cancelled { get; set; }
}

public enum RoomType
{
    Single,
    Double,
    Queen,
    Twin,
    Suite,
}

[Tag("bookings"), Description("Find Bookings")]
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

[Tag("bookings"), Description("Create a new Booking")]
[Route("/bookings", "POST")]
[ValidateHasRole("Employee")]
[AutoApply(Behavior.AuditCreate)]
public class CreateBooking : ICreateDb<Booking>, IReturn<IdResponse>
{
    [Description("Name this Booking is for")]
    public string Name { get; set; }
    [Input]
    public RoomType RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal Cost { get; set; }
    public DateTime BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    [Input(Type = "textarea")]
    public string Notes { get; set; }
}

[Tag("bookings"), Description("Update an existing Booking")]
[Route("/booking/{Id}", "PATCH")]
[ValidateHasRole("Employee")]
[AutoApply(Behavior.AuditModify)]
public class UpdateBooking : IPatchDb<Booking>, IReturn<IdResponse>
{
    public int Id { get; set; }
    public string Name { get; set; }
    [Input]
    public RoomType? RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int? RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal? Cost { get; set; }
    public DateTime? BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    [Input(Type = "textarea")]
    public string Notes { get; set; }
    public bool? Cancelled { get; set; }
}

[Tag("bookings"), Description("Delete a Booking")]
[Route("/booking/{Id}", "DELETE")]
[ValidateHasRole("Manager")]
[AutoApply(Behavior.AuditSoftDelete)]
public class DeleteBooking : IDeleteDb<Booking>, IReturnVoid
{
    public int Id { get; set; }
}
