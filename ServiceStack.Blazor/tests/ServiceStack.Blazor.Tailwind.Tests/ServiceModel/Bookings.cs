// Complete declarative AutoQuery services for Bookings CRUD example:
// https://docs.servicestack.net/autoquery-crud-bookings

using System;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace MyApp.ServiceModel;

[Icon(Svg = Icons.Booking)]
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

    [Ref(Model=nameof(Coupon), RefId=nameof(Coupon.Id), RefLabel=nameof(Coupon.Description))]
    [References(typeof(Coupon))]
    public string? CouponId { get; set; }

    [Reference]
    public Coupon Discount { get; set; }
    public string? Notes { get; set; }
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
[Notes("Find out how to quickly create a <a class='svg-external' target='_blank' href='https://youtu.be/rSFiikDjGos'>C# Bookings App from Scratch</a>")]
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
    [Description("Name this Booking is for"), ValidateNotEmpty]
    public string Name { get; set; }
    public RoomType RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal Cost { get; set; }
    [Required]
    public DateTime BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    [Input(Type = "textarea")]
    public string? Notes { get; set; }
    public string? CouponId { get; set; }
}

[Tag("bookings"), Description("Update an existing Booking")]
[Notes("Find out how to quickly create a <a class='svg-external' target='_blank' href='https://youtu.be/rSFiikDjGos'>C# Bookings App from Scratch</a>")]
[Route("/booking/{Id}", "PATCH")]
[ValidateHasRole("Employee")]
[AutoApply(Behavior.AuditModify)]
public class UpdateBooking : IPatchDb<Booking>, IReturn<IdResponse>
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public RoomType? RoomType { get; set; }
    [ValidateGreaterThan(0)]
    public int? RoomNumber { get; set; }
    [ValidateGreaterThan(0)]
    public decimal? Cost { get; set; }
    public DateTime? BookingStartDate { get; set; }
    public DateTime? BookingEndDate { get; set; }
    [Input(Type = "textarea")]
    public string? Notes { get; set; }
    public string? CouponId { get; set; }
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


[Description("Discount Coupons")]
[Icon(Svg = Icons.Coupon)]
public class Coupon
{
    public string Id { get; set; }
    public string Description { get; set; }
    public int Discount { get; set; }
    public DateTime ExpiryDate { get; set; }
}

[Tag("bookings"), Description("Find Coupons")]
[Route("/coupons", "GET")]
public class QueryCoupons : QueryDb<Coupon>
{
    public string Id { get; set; }
}

[Tag("bookings")]
[Route("/coupons", "POST")]
[ValidateHasRole("Employee")]
public class CreateCoupon : ICreateDb<Coupon>, IReturn<IdResponse>
{
    [ValidateNotEmpty]
    public string Description { get; set; }
    [ValidateGreaterThan(0)]
    public int Discount { get; set; }
    [Required]
    public DateTime ExpiryDate { get; set; }
}

[Tag("bookings")]
[Route("/coupons/{Id}", "PATCH")]
[ValidateHasRole("Employee")]
public class UpdateCoupon : IPatchDb<Coupon>, IReturn<IdResponse>
{
    public string Id { get; set; }
    [ValidateNotEmpty]
    public string Description { get; set; }
    [ValidateGreaterThan(0)]
    public int Discount { get; set; }
    [Required]
    public DateTime ExpiryDate { get; set; }
}


[Tag("bookings"), Description("Delete a Coupon")]
[Route("/coupons/{Id}", "DELETE")]
[ValidateHasRole("Manager")]
public class DeleteCoupon : IDeleteDb<Coupon>, IReturnVoid
{
    public string Id { get; set; }
}

public static class Icons
{
    public const string Booking = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M16 10H8c-.55 0-1 .45-1 1s.45 1 1 1h8c.55 0 1-.45 1-1s-.45-1-1-1zm3-7h-1V2c0-.55-.45-1-1-1s-1 .45-1 1v1H8V2c0-.55-.45-1-1-1s-1 .45-1 1v1H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-1 16H6c-.55 0-1-.45-1-1V8h14v10c0 .55-.45 1-1 1zm-5-5H8c-.55 0-1 .45-1 1s.45 1 1 1h5c.55 0 1-.45 1-1s-.45-1-1-1z'/></svg>";
    public const string Coupon = "<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 24 24'><path fill='currentColor' d='M2 9.5V4a1 1 0 0 1 1-1h18a1 1 0 0 1 1 1v5.5a2.5 2.5 0 1 0 0 5V20a1 1 0 0 1-1 1H3a1 1 0 0 1-1-1v-5.5a2.5 2.5 0 1 0 0-5zm2-1.532a4.5 4.5 0 0 1 0 8.064V19h16v-2.968a4.5 4.5 0 0 1 0-8.064V5H4v2.968zM9 9h6v2H9V9zm0 4h6v2H9v-2z' /></svg>";
}
