namespace LiteMapper.Configuration;

internal sealed class MappingRegistry
{
    private readonly Dictionary<
        (Type Source, Type Destination),
        MappingDefinition> _definitions = new();

    public void Register(MappingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var key = (definition.SourceType, definition.DestinationType);

        if (_definitions.ContainsKey(key))
        {
            throw new InvalidOperationException(
                $"Mapping from '{definition.SourceType.Name}' to " +
                $"'{definition.DestinationType.Name}' is already registered.");
        }

        _definitions.Add(key, definition);
    }

    public bool TryGet(
        Type sourceType,
        Type destinationType,
        out MappingDefinition? definition)
    {
        return _definitions.TryGetValue(
            (sourceType, destinationType), out definition);
    }

    public IReadOnlyCollection<MappingDefinition> Definitions =>
        _definitions.Values.ToArray();

    public void Validate()
    {
        foreach (var definition in _definitions.Values)
        {
            ValidateDefinition(definition);
        }
    }

    public void Seal()
    {
        foreach (var definition in _definitions.Values)
        {
            definition.Seal();
        }
    }

    private static void ValidateDefinition(MappingDefinition definition)
    {
        var sourceType = definition.SourceType;
        var destinationType = definition.DestinationType;

        if (sourceType.ContainsGenericParameters ||
            destinationType.ContainsGenericParameters)
        {
            throw new InvalidOperationException(
                $"Open generic mappings are not supported: " +
                $"'{sourceType.Name}' to '{destinationType.Name}'.");
        }

        foreach (var memberMapping in definition.MemberMappings.Values)
        {
            var destinationProperty = destinationType.GetProperty(
                memberMapping.DestinationMember);

            if (destinationProperty == null)
            {
                throw new InvalidOperationException(
                    $"Destination property '{memberMapping.DestinationMember}' " +
                    $"was not found on '{destinationType.Name}'.");
            }

            if (destinationProperty.SetMethod?.IsPublic != true ||
                destinationProperty.GetIndexParameters().Length > 0)
            {
                throw new InvalidOperationException(
                    $"Destination property '{destinationProperty.Name}' " +
                    "must have a public setter and cannot be an indexer.");
            }

            if (!memberMapping.IsIgnored &&
                memberMapping.SourceExpression == null)
            {
                throw new InvalidOperationException(
                    $"No source expression configured for " +
                    $"'{destinationProperty.Name}'.");
            }
        }
    }
}