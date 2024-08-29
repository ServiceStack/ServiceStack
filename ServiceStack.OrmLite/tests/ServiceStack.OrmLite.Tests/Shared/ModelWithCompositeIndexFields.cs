using ServiceStack.DataAnnotations;

namespace ServiceStack.Common.Tests.Models;

[CompositeIndex(true, "Composite1", "Composite2")]
public class ModelWithCompositeIndexFields
{
    public string Id { get; set; }

    [Index]
    public string Name { get; set; }

    public string AlbumId { get; set; }

    [Index(true)]
    public string UniqueName { get; set; }

    public string Composite1 { get; set; }

    public string Composite2 { get; set; }
}

[CompositeIndex("Composite1", "Composite2 DESC")]
public class ModelWithCompositeIndexFieldsDesc
{
    public string Id { get; set; }

    [Index]
    public string Name { get; set; }

    public string AlbumId { get; set; }

    [Index(true)]
    public string UniqueName { get; set; }

    public string Composite1 { get; set; }

    public string Composite2 { get; set; }
}

[CompositeIndex("Field WithSpace1", "Field WithSpace2 DESC")]
public class ModelWithCompositeIndexOnFieldSpacesDesc
{
    public string Id { get; set; }

    [Alias("Field WithSpace1")]
    public string FieldWithSpace1 { get; set; }

    [Alias("Field WithSpace2")]
    public string FieldWithSpace2 { get; set; }
}

[CompositeIndex(true, nameof(UserId), nameof(UserRole))]
public class ModelWithEnum
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public UserRoleEnum UserRole { get; set; }
}

public enum UserRoleEnum
{
    User,
    Admin
}