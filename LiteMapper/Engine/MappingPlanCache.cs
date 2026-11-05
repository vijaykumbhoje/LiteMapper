using System.Collections.Concurrent;
using LiteMapper.Configuration;

namespace LiteMapper.Engine;

internal sealed class MappingPlanCache
{
    private readonly ConcurrentDictionary<
  (Type Source, Type Destination),
  Lazy<MappingPlan>> _plans = new();

    private readonly MappingRegistry _registry;

    public MappingPlanCache(MappingRegistry registry)
    {
        _registry = registry;
    }



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

    public int Count => _plans.Count;
}