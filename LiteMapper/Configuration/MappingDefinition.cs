using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace LiteMapper.Configuration;

/// <summary>
/// Represents a mapping definition between a source type and a destination type.
/// Contains the configured member-level mapping rules for the mapping operation.
/// </summary>
/// <remarks>
/// A mapping definition stores member mappings using destination member names
/// as keys. Each destination member can have only one mapping definition.
///
/// Once sealed, the mapping definition becomes immutable and all registered
/// member mapping definitions are also sealed.
/// </remarks>
internal sealed class MappingDefinition
{
    private readonly Dictionary<string, MemberMappingDefinition> _memberMappings =
        new(StringComparer.Ordinal);

    private readonly IReadOnlyDictionary<string, MemberMappingDefinition>
        _readOnlyMemberMappings;

    private bool _isSealed;

    /// <summary>
    /// Gets the source type used in this mapping definition.
    /// </summary>
    /// <value>The source type of the mapping.</value>
    public Type SourceType { get; }

    /// <summary>
    /// Gets the destination type used in this mapping definition.
    /// </summary>
    /// <value>The destination type of the mapping.</value>
    public Type DestinationType { get; }

    /// <summary>
    /// Gets the collection of configured member-level mapping definitions.
    /// </summary>
    /// <value>
    /// A read-only dictionary containing destination member names
    /// and their corresponding mapping definitions.
    /// </value>
    public IReadOnlyDictionary<string, MemberMappingDefinition> MemberMappings =>
        _readOnlyMemberMappings;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingDefinition"/> class.
    /// </summary>
    /// <param name="sourceType">
    /// The source type from which values are mapped.
    /// </param>
    /// <param name="destinationType">
    /// The destination type to which values are mapped.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the source type or destination type is null.
    /// </exception>
    public MappingDefinition(Type sourceType, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(sourceType);
        ArgumentNullException.ThrowIfNull(destinationType);

        SourceType = sourceType;
        DestinationType = destinationType;

        _readOnlyMemberMappings =
            new ReadOnlyDictionary<string, MemberMappingDefinition>(
                _memberMappings);
    }

    /// <summary>
    /// Adds a member-level mapping definition to the current mapping.
    /// </summary>
    /// <param name="memberMapping">
    /// The member mapping definition to register.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified member mapping is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a mapping for the destination member has already been
    /// configured or when the mapping definition has been sealed.
    /// </exception>
    public void AddMemberMapping(MemberMappingDefinition memberMapping)
    {
        ArgumentNullException.ThrowIfNull(memberMapping);

        EnsureNotSealed();

        if (!_memberMappings.TryAdd(
            memberMapping.DestinationMember,
            memberMapping))
        {
            throw new InvalidOperationException(
                $"Mapping for destination property " +
                $"'{memberMapping.DestinationMember}' has already been configured.");
        }
    }

    /// <summary>
    /// Seals the mapping definition and all its registered member mappings.
    /// </summary>
    /// <remarks>
    /// Sealing prevents further modifications to the mapping definition.
    /// Calling this method multiple times has no additional effect.
    /// </remarks>
    public void Seal()
    {
        if (_isSealed)
            return;

        foreach (var memberMapping in _memberMappings.Values)
        {
            memberMapping.Seal();
        }

        _isSealed = true;
    }

    /// <summary>
    /// Ensures that the mapping definition has not been sealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a sealed mapping definition.
    /// </exception>
    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "This mapping definition is already sealed and cannot be modified.");
        }
    }
}

/// <summary>
/// Represents a mapping rule for an individual destination member.
/// Defines the source expression or indicates whether the destination
/// member should be ignored during mapping.
/// </summary>
/// <remarks>
/// A member mapping definition can be configured with either a source
/// expression or an ignore rule.
///
/// Once sealed, the definition becomes immutable and cannot be modified.
/// </remarks>
internal sealed class MemberMappingDefinition
{
    private bool _isSealed;

    /// <summary>
    /// Gets the name of the destination member associated with this mapping rule.
    /// </summary>
    /// <value>The destination member name.</value>
    public string DestinationMember { get; }

    /// <summary>
    /// Gets the expression used to retrieve the source value for the
    /// destination member.
    /// </summary>
    /// <value>
    /// A lambda expression representing the source member mapping,
    /// or null when the destination member is ignored or not configured.
    /// </value>
    public LambdaExpression? SourceExpression { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the destination member
    /// should be ignored during mapping.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if the destination member is ignored;
    /// otherwise, <see langword="false"/>.
    /// </value>
    public bool IsIgnored { get; private set; }

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="MemberMappingDefinition"/> class.
    /// </summary>
    /// <param name="destinationMember">
    /// The name of the destination member for which the mapping rule is defined.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination member name is null, empty, or consists
    /// only of white-space characters.
    /// </exception>
    public MemberMappingDefinition(string destinationMember)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(destinationMember);

        DestinationMember = destinationMember;
    }

    /// <summary>
    /// Configures the source expression used to populate the destination member.
    /// </summary>
    /// <param name="expression">
    /// A lambda expression that specifies how to retrieve the source value.
    /// </param>
    /// <remarks>
    /// Setting a source expression automatically clears the ignore flag
    /// for the destination member.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified expression is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a sealed member mapping definition.
    /// </exception>
    public void SetSourceExpression(LambdaExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);

        EnsureNotSealed();

        SourceExpression = expression;
        IsIgnored = false;
    }

    /// <summary>
    /// Configures the destination member to be ignored during mapping.
    /// </summary>
    /// <remarks>
    /// Ignoring a destination member clears any previously configured
    /// source expression for that member.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a sealed member mapping definition.
    /// </exception>
    public void SetIgnored()
    {
        EnsureNotSealed();

        SourceExpression = null;
        IsIgnored = true;
    }

    /// <summary>
    /// Seals the member mapping definition to prevent further modifications.
    /// </summary>
    /// <remarks>
    /// Once sealed, source expressions and ignore settings cannot be changed.
    /// </remarks>
    public void Seal()
    {
        _isSealed = true;
    }

    /// <summary>
    /// Ensures that the member mapping definition has not been sealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to modify a sealed member mapping definition.
    /// </exception>
    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                $"Mapping rule for '{DestinationMember}' is sealed.");
        }
    }
}