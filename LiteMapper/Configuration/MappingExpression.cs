
using System.Linq.Expressions;
using System.Reflection;

namespace LiteMapper.Configuration;

public sealed class MappingExpression<TSource, TDestination>
{
    internal MappingExpression(MappingDefinition definition)
    {
        Definition = definition;
    }

    internal MappingDefinition Definition { get; }

    public MappingExpression<TSource, TDestination> ForMember<TMember>(
     Expression<Func<TDestination, TMember>> destinationExpression,
     Action<MemberOptions<TSource, TMember>> options)
    {
        ArgumentNullException.ThrowIfNull(destinationExpression);
        ArgumentNullException.ThrowIfNull(options);

        var memberExpression = GetMemberExpression(destinationExpression);

        if (memberExpression.Member is not PropertyInfo property)
        {
            throw new ArgumentException(
                "Destination expression must select a property.");
        }

        if (property.SetMethod?.IsPublic != true ||
            property.GetIndexParameters().Length > 0)
        {
            throw new ArgumentException(
                $"Destination property '{property.Name}' must have a public setter and cannot be an indexer.");
        }

        var memberDefinition = new MemberMappingDefinition(property.Name);

        var memberOptions =
            new MemberOptions<TSource, TMember>(memberDefinition);

        options(memberOptions);

        if (!memberDefinition.IsIgnored &&
            memberDefinition.SourceExpression == null)
        {
            throw new InvalidOperationException(
                $"No mapping rule was configured for '{property.Name}'.");
        }

        Definition.AddMemberMapping(memberDefinition);

        return this;
    }

    private static MemberExpression GetMemberExpression(
        LambdaExpression expression)
    {
        Expression body = expression.Body;

        if (body is UnaryExpression unary &&
            unary.NodeType == ExpressionType.Convert)
        {
            body = unary.Operand;
        }

        if (body is MemberExpression member &&
            member.Member is PropertyInfo &&
            member.Expression == expression.Parameters[0])
        {
            return member;
        }

        throw new ArgumentException(
            "Expression must select a direct destination property.");
    }
}
