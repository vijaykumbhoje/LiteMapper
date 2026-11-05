using LiteMapper.Abstractions;
using LiteMapper.Configuration;
using LiteMapper.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LiteMapper.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLiteMapper(
        this IServiceCollection services,
        Action<MapperConfiguration> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var configuration = new MapperConfiguration(configure);

        services.TryAddSingleton(configuration);
        services.TryAddScoped<IMapper, Mapper>();

        return services;
    }
}