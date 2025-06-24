using KdbndpTypes;

namespace ServiceStack.OrmLite.Kingbase.Converters;

public interface IKingbaseConverter
{
    public KdbndpDbType KdbndpDbType { get; }
}