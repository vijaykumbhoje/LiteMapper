namespace ProMapper.Configuration;

/// <summary>
/// Maintains the collection of registered object mapping definitions
/// and provides operations for registration, lookup, validation,
/// and sealing of mappings.
/// </summary>
/// <remarks>
/// The registry identifies each mapping using a combination of its
/// source type and destination type.
///
/// Duplicate mappings for the same source and destination type
/// combination are not permitted.
///
/// Once sealed, the registry prevents further mapping registrations
/// and seals all registered mapping definitions.
/// </remarks>
internal sealed class MappingRegistry
{
    private readonly Dictionary<
        (Type Source, Type Destination),
        MappingDefinition> _definitions = new();

    private bool _isSealed;

    /// <summary>
    /// Registers a mapping definition in the registry.
    /// </summary>
    /// <param name="definition">
    /// The mapping definition containing the source type,
    /// destination type, and configured member mappings.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified mapping definition is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a mapping for the same source and destination
    /// type combination has already been registered or when the
    /// registry has been sealed.
    /// </exception>
    public void Register(MappingDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        EnsureNotSealed();

        var key = (definition.SourceType, definition.DestinationType);

        if (!_definitions.TryAdd(key, definition))
        {
            throw new InvalidOperationException(
                $"Mapping from '{definition.SourceType.Name}' to " +
                $"'{definition.DestinationType.Name}' is already registered.");
        }
    }

    /// <summary>
    /// Attempts to retrieve a registered mapping definition
    /// for the specified source and destination types.
    /// </summary>
    /// <param name="sourceType">
    /// The source type of the mapping to retrieve.
    /// </param>
    /// <param name="destinationType">
    /// The destination type of the mapping to retrieve.
    /// </param>
    /// <param name="definition">
    /// When this method returns, contains the matching mapping definition
    /// if found; otherwise, null.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a matching mapping definition exists;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public bool TryGet(
        Type sourceType,
        Type destinationType,
        out MappingDefinition? definition)
    {
        return _definitions.TryGetValue(
            (sourceType, destinationType),
            out definition);
    }

    /// <summary>
    /// Gets all mapping definitions currently registered in the registry.
    /// </summary>
    /// <value>
    /// A read-only collection containing the registered mapping definitions.
    /// </value>
    public IReadOnlyCollection<MappingDefinition> Definitions =>
        _definitions.Values.ToArray();

    /// <summary>
    /// Validates all registered mapping definitions to ensure
    /// that their configuration is structurally valid.
    /// </summary>
    /// <remarks>
    /// Validation checks include:
    /// <list type="bullet">
    /// <item>
    /// <description>Open generic source and destination types are not supported.</description>
    /// </item>
    /// <item>
    /// <description>Configured destination properties must exist.</description>
    /// </item>
    /// <item>
    /// <description>Destination properties must have public setters and cannot be indexers.</description>
    /// </item>
    /// <item>
    /// <description>Non-ignored destination members must have a configured source expression.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when any registered mapping definition contains
    /// an invalid configuration.
    /// </exception>
    public void Validate()
    {
        foreach (var definition in _definitions.Values)
        {
            ValidateDefinition(definition);
        }
    }

    /// <summary>
    /// Seals the registry and all registered mapping definitions
    /// to prevent further modifications.
    /// </summary>
    /// <remarks>
    /// Calling this method multiple times has no additional effect.
    /// Once sealed, new mapping definitions cannot be registered.
    /// </remarks>
    public void Seal()
    {
        if (_isSealed)
            return;

        foreach (var definition in _definitions.Values)
        {
            definition.Seal();
        }

        _isSealed = true;
    }

    /// <summary>
    /// Ensures that the registry has not been sealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a sealed registry.
    /// </exception>
    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Mapping configuration is already initialized.");
        }
    }

    /// <summary>
    /// Validates an individual mapping definition.
    /// </summary>
    /// <param name="definition">
    /// The mapping definition to validate.
    /// </param>
    /// <remarks>
    /// Validates the source and destination types and ensures that
    /// every configured destination member references a valid,
    /// publicly writable destination property.
    ///
    /// Also verifies that every non-ignored member has a configured
    /// source expression.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the mapping contains unsupported generic types,
    /// references a nonexistent destination property, targets an
    /// inaccessible or indexed property, or has no configured
    /// source expression for a non-ignored member.
    /// </exception>
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