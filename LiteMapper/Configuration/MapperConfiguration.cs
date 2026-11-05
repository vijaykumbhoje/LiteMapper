using LiteMapper.Conversion;

namespace LiteMapper.Configuration;

public sealed class MapperConfiguration
{
    private readonly MappingRegistry _registry = new();
    private readonly TypeConverterRegistry _converterRegistry = new();
    private bool _isSealed;

    public MapperConfiguration(Action<MapperConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        configure(this);

        _registry.Validate();

        _registry.Seal();
        _converterRegistry.Seal();

        _isSealed = true;
    }

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

    public void AssertConfigurationIsValid()
    {
        _registry.Validate();
    }

    internal MappingRegistry Registry => _registry;

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

    internal TypeConverterRegistry ConverterRegistry =>
    _converterRegistry;
}