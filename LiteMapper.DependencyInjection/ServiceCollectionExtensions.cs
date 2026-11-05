
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

        // Create and validate configuration immediately.
        var configuration = new MapperConfiguration(configure);

        // Register configuration and mapper only if not already registered.
        services.TryAddSingleton(configuration);
        services.TryAddSingleton<IMapper, Mapper>();

        return services;
    }
}
