
using LiteMapper.Abstractions;
using LiteMapper.Configuration;

namespace LiteMapper.Engine;

public sealed class Mapper : IMapper
{
    private readonly MappingEngine _engine;

    public Mapper(MapperConfiguration configuration)
     : this(configuration, null)
    {
    }

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

    public TDestination Map<TDestination>(object source)
    {
        return (TDestination)_engine.Map(
            source,
            typeof(TDestination));
    }

    public TDestination Map<TSource, TDestination>(
        TSource source)
    {
        return (TDestination)_engine.Map(
            source!,
            typeof(TDestination));
    }

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
