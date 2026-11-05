namespace LiteMapper.Engine;

internal sealed class PropertyMapPlan
{
    public Type DestinationPropertyType { get; }

    public Func<object, object?> SourceGetter { get; }

    public Action<object, object?> DestinationSetter { get; }

    public bool CanAssignDirectly { get; }

    public bool RequiresReferenceTracking { get; }

    public PropertyMapPlan(
        Type sourcePropertyType,
        Type destinationPropertyType,
        Func<object, object?> sourceGetter,
        Action<object, object?> destinationSetter)
    {
        DestinationPropertyType = destinationPropertyType;
        SourceGetter = sourceGetter;
        DestinationSetter = destinationSetter;

        CanAssignDirectly =
            destinationPropertyType.IsAssignableFrom(sourcePropertyType);

        var targetType =
            Nullable.GetUnderlyingType(destinationPropertyType)
            ?? destinationPropertyType;

        RequiresReferenceTracking =
            !targetType.IsValueType &&
            targetType != typeof(string);
    }
}