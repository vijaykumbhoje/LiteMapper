namespace LiteMapper.Abstractions;

public interface IMapper
{
    // Create a new destination object.
    TDestination Map<TDestination>(object source);

    // Create a new destination object using explicit types.
    TDestination Map<TSource, TDestination>(TSource source);

    // Map into an existing destination object.
    TDestination Map<TSource, TDestination>(
        TSource source,
        TDestination destination);
}