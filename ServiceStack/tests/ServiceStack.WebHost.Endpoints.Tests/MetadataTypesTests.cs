using NUnit.Framework;

namespace ServiceStack.WebHost.Endpoints.Tests;

public class MetadataTypesTests
{
    [Test]
    public void Does_not_include_inherited_properties_in_Metadata_Type()
    {
        var metadataType = typeof(Booking).ToMetadataType();
        var metadataProps = metadataType.Properties.Map(x => x.Name);

        Assert.That(metadataProps, Is.EquivalentTo(new[]
        {
            nameof(Booking.Id),
            nameof(Booking.RoomType),
            nameof(Booking.RoomNumber),
            nameof(Booking.BookingStartDate),
            nameof(Booking.BookingEndDate),
            nameof(Booking.Notes),
            nameof(Booking.Cancelled),
            nameof(Booking.Cost),
        }));
    }
}