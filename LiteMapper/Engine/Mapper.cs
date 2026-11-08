using LiteMapper.Abstractions;
using LiteMapper.Configuration;

namespace LiteMapper.Engine;

/// <summary>
/// Provides methods for mapping objects between different types
/// using configured mapping definitions and custom type converters.
/// </summary>
/// <remarks>
/// The mapper acts as the primary entry point for executing mapping
/// operations in LiteMapper.
///
/// It delegates mapping operations to the internal
/// <see cref="MappingEngine"/> and uses the supplied
/// <see cref="MapperConfiguration"/> to resolve mapping definitions
/// and custom type converters.
///
/// An optional <see cref="IServiceProvider"/> can be supplied to support
/// dependency injection when resolving custom type converters.
/// </remarks>
public sealed class Mapper : IMapper
{
    private readonly MappingEngine _engine;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mapper"/> class
    /// using the specified mapping configuration.
    /// </summary>
    /// <param name="configuration">
    /// The mapping configuration containing registered mapping definitions
    /// and custom type converters.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified configuration is null.
    /// </exception>
    public Mapper(MapperConfiguration configuration)
        : this(configuration, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Mapper"/> class
    /// using the specified mapping configuration and service provider.
    /// </summary>
    /// <param name="configuration">
    /// The mapping configuration containing registered mapping definitions
    /// and custom type converters.
    /// </param>
    /// <param name="serviceProvider">
    /// An optional service provider used to resolve custom type converters
    /// registered through dependency injection.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified configuration is null.
    /// </exception>
    public Mapper(
        MapperConfiguration configuration,
        IServiceProvider? serviceProvider)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        _engine = new MappingEngine(
            configuration.Registry,
            configuration.ConverterRegistry,
            serviceProvider);
    }

    /// <summary>
    /// Maps a source object to a new destination object of the specified type.
    /// </summary>
    /// <typeparam name="TDestination">
    /// The type of the destination object.
    /// </typeparam>
    /// <param name="source">
    /// The source object containing the values to map.
    /// </param>
    /// <returns>
    /// A new destination object containing the mapped values.
    /// </returns>
    /// <remarks>
    /// The source object's runtime type is used to identify the mapping
    /// definition for the requested destination type.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the source object is null, if null sources are not
    /// supported by the mapping engine.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching mapping definition exists or when
    /// the mapping operation cannot be completed.
    /// </exception>
    public TDestination Map<TDestination>(object source)
    {
        return (TDestination)_engine.Map(
            source,
            typeof(TDestination));
    }

    /// <summary>
    /// Maps a source object to a new destination object using
    /// the specified source and destination types.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of the source object.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The type of the destination object.
    /// </typeparam>
    /// <param name="source">
    /// The source object containing the values to map.
    /// </param>
    /// <returns>
    /// A new destination object containing the mapped values.
    /// </returns>
    /// <remarks>
    /// This overload provides strongly typed source and destination
    /// parameters while delegating the mapping operation to the
    /// internal mapping engine.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the source object is null, if null sources are not
    /// supported by the mapping engine.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching mapping definition exists or when
    /// the mapping operation cannot be completed.
    /// </exception>
    public TDestination Map<TSource, TDestination>(
        TSource source)
    {
        return (TDestination)_engine.Map(
            source!,
            typeof(TDestination));
    }

    /// <summary>
    /// Maps values from a source object into an existing destination object.
    /// </summary>
    /// <typeparam name="TSource">
    /// The type of the source object.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The type of the destination object.
    /// </typeparam>
    /// <param name="source">
    /// The source object containing the values to map.
    /// </param>
    /// <param name="destination">
    /// The existing destination object whose members will be populated
    /// with the mapped values.
    /// </param>
    /// <returns>
    /// The updated destination object.
    /// </returns>
    /// <remarks>
    /// Unlike mapping to a new destination object, this overload
    /// reuses the supplied destination instance.
    ///
    /// Existing destination values may be overwritten according
    /// to the configured mapping definitions.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the source or destination object is null,
    /// if null objects are not supported by the mapping engine.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no matching mapping definition exists or when
    /// the mapping operation cannot be completed.
    /// </exception>
    public TDestination Map<TSource, TDestination>(
        TSource source,
        TDestination destination)
    {
        return (TDestination)_engine.Map(
            source!,
            typeof(TDestination),
            destination);
    }
}