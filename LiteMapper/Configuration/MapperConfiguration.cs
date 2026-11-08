using LiteMapper.Conversion;

namespace LiteMapper.Configuration;

/// <summary>
/// Represents the configuration used by LiteMapper to define
/// object mappings, mapping profiles, and custom type converters.
/// </summary>
/// <remarks>
/// The configuration is initialized through the constructor and
/// becomes immutable after initialization.
///
/// During initialization, registered mapping definitions are validated,
/// and both mapping and type converter registries are sealed to prevent
/// further modifications.
/// </remarks>
public sealed class MapperConfiguration
{
    private readonly MappingRegistry _registry = new();
    private readonly TypeConverterRegistry _converterRegistry = new();
    private bool _isSealed;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="MapperConfiguration"/> class.
    /// </summary>
    /// <param name="configure">
    /// An action used to configure mapping profiles and custom type converters.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the configuration action is null.
    /// </exception>
    public MapperConfiguration(Action<MapperConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this);

        _registry.Validate();

        _registry.Seal();
        _converterRegistry.Seal();

        _isSealed = true;
    }

    /// <summary>
    /// Registers a mapping profile containing one or more mapping definitions.
    /// </summary>
    /// <param name="profile">
    /// The mapping profile containing the mapping definitions to register.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified profile is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to add a profile after the configuration
    /// has been initialized.
    /// </exception>
    public void AddProfile(MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Mapping configuration is already initialized.");
        }

        foreach (var definition in profile.Definitions)
        {
            _registry.Register(definition);
        }
    }

    /// <summary>
    /// Validates all registered mapping definitions to ensure
    /// that the configuration is correctly defined.
    /// </summary>
    /// <remarks>
    /// This method checks the registered mappings for configuration
    /// errors. It can be used to explicitly validate the configuration.
    /// </remarks>
    public void AssertConfigurationIsValid()
    {
        _registry.Validate();
    }

    /// <summary>
    /// Gets the internal registry containing registered mapping definitions.
    /// </summary>
    /// <value>
    /// The mapping registry used internally by LiteMapper.
    /// </value>
    internal MappingRegistry Registry => _registry;

    /// <summary>
    /// Registers a custom type converter for the specified source
    /// and destination types.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type handled by the converter.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The destination type produced by the converter.
    /// </typeparam>
    /// <param name="converter">
    /// The custom type converter implementation.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified converter is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to register a converter after the
    /// configuration has been initialized.
    /// </exception>
    public void AddTypeConverter<TSource, TDestination>(
        ITypeConverter<TSource, TDestination> converter)
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Mapping configuration is already initialized.");
        }

        ArgumentNullException.ThrowIfNull(converter);

        _converterRegistry.Register(converter);
    }

    /// <summary>
    /// Registers a custom type converter using its implementation type.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type handled by the converter.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The destination type produced by the converter.
    /// </typeparam>
    /// <typeparam name="TConverter">
    /// The concrete converter implementation type.
    /// Must implement
    /// <see cref="ITypeConverter{TSource, TDestination}"/>.
    /// </typeparam>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to register a converter after the
    /// configuration has been initialized.
    /// </exception>
    public void AddTypeConverter<TSource, TDestination, TConverter>()
        where TConverter : class, ITypeConverter<TSource, TDestination>
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Mapping configuration is already initialized.");
        }

        _converterRegistry.RegisterType<TSource, TDestination, TConverter>();
    }

    /// <summary>
    /// Gets the internal registry containing registered custom type converters.
    /// </summary>
    /// <value>
    /// The type converter registry used internally by LiteMapper.
    /// </value>
    internal TypeConverterRegistry ConverterRegistry =>
        _converterRegistry;
}