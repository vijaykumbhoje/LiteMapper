using System.Runtime.CompilerServices;

namespace LiteMapper.Engine;

internal sealed class MappingContext
{
    private readonly Dictionary<MappingKey, object> _mappedObjects =
        new(new MappingKeyComparer());

    public int CurrentDepth { get; private set; }

    public int MaxDepth { get; } = 64;

    public bool TryGetMappedObject(
        object source,
        Type destinationType,
        out object? destination)
    {
        return _mappedObjects.TryGetValue(
            new MappingKey(source, destinationType),
            out destination);
    }

    public void Register(
        object source,
        Type destinationType,
        object destination)
    {
        _mappedObjects[new MappingKey(source, destinationType)] = destination;
    }

    public void Enter()
    {
        CurrentDepth++;

        if (CurrentDepth > MaxDepth)
        {
            CurrentDepth--;

            throw new InvalidOperationException(
                $"Maximum mapping depth of {MaxDepth} was exceeded.");
        }
    }

    public void Exit()
    {
        CurrentDepth--;
    }

    private readonly record struct MappingKey(
        object Source,
        Type DestinationType);

    private sealed class MappingKeyComparer : IEqualityComparer<MappingKey>
    {
        public bool Equals(MappingKey x, MappingKey y)
        {
            return ReferenceEquals(x.Source, y.Source) &&
                   x.DestinationType == y.DestinationType;
        }

        public int GetHashCode(MappingKey obj)
        {
            return HashCode.Combine(
                RuntimeHelpers.GetHashCode(obj.Source),
                obj.DestinationType);
        }
    }
}