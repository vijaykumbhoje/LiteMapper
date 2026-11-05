
namespace LiteMapper.Conversion;

public interface ITypeConverter<in TSource, out TDestination>
{
    TDestination Convert(TSource source);
}
