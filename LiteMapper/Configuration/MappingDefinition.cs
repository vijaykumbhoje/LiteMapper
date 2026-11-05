using System.Linq.Expressions;

namespace LiteMapper.Configuration;

internal sealed class MappingDefinition
{
    private readonly Dictionary<string, MemberMappingDefinition> _memberMappings =
        new(StringComparer.Ordinal);

    private bool _isSealed;

    public Type SourceType { get; }
    public Type DestinationType { get; }

    public IReadOnlyDictionary<string, MemberMappingDefinition> MemberMappings =>
        _memberMappings;

    public MappingDefinition(Type sourceType, Type destinationType)
    {
        SourceType = sourceType;
        DestinationType = destinationType;
    }

    public void AddMemberMapping(MemberMappingDefinition memberMapping)
    {
        EnsureNotSealed();

        if (!_memberMappings.TryAdd(
            memberMapping.DestinationMember, memberMapping))
        {
            throw new InvalidOperationException(
                $"Mapping for destination property " +
                $"'{memberMapping.DestinationMember}' has already been configured.");
        }
    }

    public void Seal()
    {
        if (_isSealed)
            return;

        foreach (var memberMapping in _memberMappings.Values)
            memberMapping.Seal();

        _isSealed = true;
    }

    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "This mapping definition is already sealed and cannot be modified.");
        }
    }
}

internal sealed class MemberMappingDefinition
{
    private bool _isSealed;

    public string DestinationMember { get; }
    public LambdaExpression? SourceExpression { get; private set; }
    public bool IsIgnored { get; private set; }

    public MemberMappingDefinition(string destinationMember)
    {
        DestinationMember = destinationMember;
    }

    public void SetSourceExpression(LambdaExpression expression)
    {
        EnsureNotSealed();

        SourceExpression = expression;
        IsIgnored = false;
    }

    public void SetIgnored()
    {
        EnsureNotSealed();

        SourceExpression = null;
        IsIgnored = true;
    }

    public void Seal()
    {
        _isSealed = true;
    }

    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                $"Mapping rule for '{DestinationMember}' is sealed.");
        }
    }
}