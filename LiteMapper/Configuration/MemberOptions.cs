using System.Linq.Expressions;

namespace LiteMapper.Configuration;

public sealed class MemberOptions<TSource, TMember>
{
    private readonly MemberMappingDefinition _definition;

    internal MemberOptions(MemberMappingDefinition definition)
    {
        _definition = definition;
    }

    public void MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceExpression)
    {
        ArgumentNullException.ThrowIfNull(sourceExpression);
        _definition.SetSourceExpression(sourceExpression);
    }

    public void Ignore()
    {
        _definition.SetIgnored();
    }
}