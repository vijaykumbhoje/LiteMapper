namespace LiteMapper.Abstractions;

/// <summary>
/// Defines the contract for mapping objects between different types.
/// </summary>
public interface IMapper
{
    // Create a new destination object.
    TDestination Map<TDestination>(object source);

    /// <summary>
    /// Maps a source object to a new destination object.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of the source object.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The type of the destination object.
    /// </typeparam>
    /// <param name="source">
    /// The source object to map.
    /// </param>
    /// <returns>
    /// A new destination object containing the mapped values.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the source object is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no mapping is registered for the requested types.
    /// </exception>
    TDestination Map<TSource, TDestination>(TSource source);

    /// <summary>
    /// Maps a source object into an existing destination object.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of the source object.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The type of the destination object.
    /// </typeparam>
    /// <param name="source">
    /// The source object to map.
    /// </param>
    /// <param name="destination">
    /// The existing destination object to populate.
    /// </param>
    /// <returns>
    /// The populated destination object.
    /// </returns>
    TDestination Map<TSource, TDestination>(
        TSource source,
        TDestination destination);
}