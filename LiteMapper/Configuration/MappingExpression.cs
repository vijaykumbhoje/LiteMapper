using System.Linq.Expressions;
using System.Reflection;

namespace LiteMapper.Configuration;

/// <summary>
/// Provides a fluent API for configuring member-level mappings
/// between a source type and a destination type.
/// </summary>
/// <typeparam name="TSource">
/// The source type from which values are mapped.
/// </typeparam>
/// <typeparam name="TDestination">
/// The destination type to which values are mapped.
/// </typeparam>
/// <remarks>
/// This class allows individual destination properties to be configured
/// using strongly typed expressions.
///
/// Member mappings can be customized using
/// <see cref="MemberOptions{TSource, TMember}"/> to specify source
/// expressions or ignore destination members.
///
/// Configuration is stored in the associated
/// <see cref="MappingDefinition"/>.
/// </remarks>
public sealed class MappingExpression<TSource, TDestination>
{
    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="MappingExpression{TSource, TDestination}"/> class.
    /// </summary>
    /// <param name="definition">
    /// The mapping definition that stores the configured member mappings.
    /// </param>
    internal MappingExpression(MappingDefinition definition)
    {
        Definition = definition;
    }

    /// <summary>
    /// Gets the underlying mapping definition associated with this expression.
    /// </summary>
    /// <value>
    /// The mapping definition containing the configured member mappings.
    /// </value>
    internal MappingDefinition Definition { get; }

    /// <summary>
    /// Configures the mapping for a specific destination property.
    /// </summary>
    /// <typeparam name="TMember">
    /// The type of the destination property being configured.
    /// </typeparam>
    /// <param name="destinationExpression">
    /// An expression identifying the destination property to configure.
    /// The expression must select a direct property of the destination object.
    /// </param>
    /// <param name="options">
    /// An action used to configure the mapping behavior for the selected
    /// destination property, including source expressions and ignore rules.
    /// </param>
    /// <returns>
    /// The current <see cref="MappingExpression{TSource, TDestination}"/>
    /// instance to support fluent configuration.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the destination expression or configuration action is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the destination expression does not select a direct property,
    /// when the selected member is not a property, or when the destination
    /// property does not have a public setter or is an indexer.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the configuration action does not specify a source expression
    /// or an ignore rule for the destination property.
    /// </exception>
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

    /// <summary>
    /// Extracts and validates a direct property access expression
    /// from the specified lambda expression.
    /// </summary>
    /// <param name="expression">
    /// The lambda expression containing the destination property access.
    /// </param>
    /// <returns>
    /// A <see cref="MemberExpression"/> representing the selected property.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when the expression does not select a direct property
    /// of the destination object.
    /// </exception>
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