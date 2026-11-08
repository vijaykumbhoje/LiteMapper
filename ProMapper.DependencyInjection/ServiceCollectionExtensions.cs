using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ProMapper.Abstractions;
using ProMapper.Configuration;
using ProMapper.Engine;

namespace ProMapper.DependencyInjection;

/// <summary>
/// Provides extension methods for registering ProMapper services
/// with the .NET dependency injection container.
/// </summary>
/// <remarks>
/// These extensions simplify the integration of ProMapper into applications
/// that use <see cref="IServiceCollection"/> for dependency management.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers ProMapper services and configures mapping definitions
    /// in the dependency injection container.
    /// </summary>
    /// <param name="services">
    /// The service collection to which ProMapper services are registered.
    /// </param>
    /// <param name="configure">
    /// An action used to configure mapping definitions, profiles,
    /// and other mapping options through <see cref="MapperConfiguration"/>.
    /// </param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that
    /// additional service registrations can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or
    /// <paramref name="configure"/> is null.
    /// </exception>
    /// <remarks>
    /// The method creates a <see cref="MapperConfiguration"/> using
    /// the supplied configuration action and registers the following services:
    ///
    /// <list type="bullet">
    /// <item>
    /// <description>
    /// <see cref="MapperConfiguration"/> is registered as a singleton.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// <see cref="IMapper"/> is registered with a scoped lifetime,
    /// using <see cref="Mapper"/> as its implementation.
    /// </description>
    /// </item>
    /// </list>
    ///
    /// Existing registrations are preserved because
    /// <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection)"/>
    /// and <see cref="ServiceCollectionDescriptorExtensions.TryAddScoped{TService, TImplementation}(IServiceCollection)"/>
    /// are used.
    /// </remarks>
    public static IServiceCollection AddProMapper(
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