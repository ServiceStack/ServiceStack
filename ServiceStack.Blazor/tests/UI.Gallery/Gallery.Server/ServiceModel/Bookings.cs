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
    [Input(Type="file")]
    public string? Photo { get; set; }
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
