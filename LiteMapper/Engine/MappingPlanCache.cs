using System.Collections.Concurrent;
using LiteMapper.Configuration;

namespace LiteMapper.Engine;

/// <summary>
/// Provides a thread-safe cache for compiled <see cref="MappingPlan"/> instances.
/// </summary>
/// <remarks>
/// Mapping plans are cached using the source and destination type pair as the
/// cache key. This avoids repeatedly building mapping plans for the same
/// combination of types.
///
/// The cache uses <see cref="ConcurrentDictionary{TKey, TValue}"/> to support
/// concurrent access and <see cref="Lazy{T}"/> to ensure that each mapping
/// plan is initialized only once when multiple threads request it simultaneously.
/// </remarks>
internal sealed class MappingPlanCache
{
    /// <summary>
    /// Stores mapping plans indexed by their source and destination type pair.
    /// </summary>
    /// <remarks>
    /// Each value is wrapped in <see cref="Lazy{T}"/> using
    /// <see cref="LazyThreadSafetyMode.ExecutionAndPublication"/> to provide
    /// thread-safe, deferred initialization of mapping plans.
    /// </remarks>
    private readonly ConcurrentDictionary<
        (Type Source, Type Destination),
        Lazy<MappingPlan>> _plans = new();

    /// <summary>
    /// Gets the registry containing the configured mapping definitions.
    /// </summary>
    private readonly MappingRegistry _registry;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingPlanCache"/> class.
    /// </summary>
    /// <param name="registry">
    /// The registry used to retrieve mapping definitions when a mapping plan
    /// is not already cached.
    /// </param>
    public MappingPlanCache(MappingRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Gets the compiled mapping plan for the specified source and destination types.
    /// </summary>
    /// <param name="sourceType">
    /// The source type of the mapping operation.
    /// </param>
    /// <param name="destinationType">
    /// The destination type of the mapping operation.
    /// </param>
    /// <returns>
    /// The cached or newly created <see cref="MappingPlan"/> for the specified
    /// source and destination type combination.
    /// </returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no mapping definition is registered for the specified
    /// source and destination types.
    /// </exception>
    /// <remarks>
    /// The method first checks the cache for an existing mapping plan.
    /// If no plan is found, it creates a lazy initialization entry and adds
    /// it to the concurrent dictionary.
    ///
    /// The mapping definition is retrieved from the
    /// <see cref="MappingRegistry"/> and used to build the mapping plan.
    /// The resulting plan is initialized only when its value is accessed.
    /// </remarks>
    public MappingPlan GetPlan(
        Type sourceType,
        Type destinationType)
    {
        var key = (sourceType, destinationType);

        // Fast path: most mapping operations should find an existing plan.
        if (_plans.TryGetValue(key, out var cachedPlan))
        {
            return cachedPlan.Value;
        }

        // Slow path: create the plan only when it is missing.
        var lazyPlan = _plans.GetOrAdd(
            key,
            key => new Lazy<MappingPlan>(
                () =>
                {
                    if (!_registry.TryGet(
                        key.Source,
                        key.Destination,
                        out var definition) ||
                        definition == null)
                    {
                        throw new InvalidOperationException(
                            $"No mapping registered from " +
                            $"'{key.Source.Name}' to '{key.Destination.Name}'.");
                    }

                    return MappingPlan.Build(
                        key.Source,
                        key.Destination,
                        definition);
                },
                LazyThreadSafetyMode.ExecutionAndPublication));

        return lazyPlan.Value;
    }

    /// <summary>
    /// Gets the number of mapping plans currently stored in the cache.
    /// </summary>
    /// <value>
    /// The number of source and destination type combinations
    /// currently cached.
    /// </value>
    public int Count => _plans.Count;
}