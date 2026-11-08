using System.Linq.Expressions;

namespace LiteMapper.Configuration;

/// <summary>
/// Provides options for configuring how an individual destination member
/// is populated during an object mapping operation.
/// </summary>
/// <typeparam name="TSource">
/// The source type from which the member value is retrieved.
/// </typeparam>
/// <typeparam name="TMember">
/// The type of the destination member being configured.
/// </typeparam>
/// <remarks>
/// This class provides methods to define a custom source expression
/// or to ignore the destination member during mapping.
///
/// Instances are created internally by
/// <see cref="MappingExpression{TSource, TDestination}"/>
/// when configuring individual destination members.
/// </remarks>
public sealed class MemberOptions<TSource, TMember>
{
    private readonly MemberMappingDefinition _definition;

    /// <summary>
    /// Initializes a new instance of the
    /// <see cref="MemberOptions{TSource, TMember}"/> class.
    /// </summary>
    /// <param name="definition">
    /// The member mapping definition associated with the destination member.
    /// </param>
    internal MemberOptions(MemberMappingDefinition definition)
    {
        _definition = definition;
    }

    /// <summary>
    /// Configures a custom source expression used to populate
    /// the destination member.
    /// </summary>
    /// <typeparam name="TSourceMember">
    /// The type of the value returned by the source expression.
    /// </typeparam>
    /// <param name="sourceExpression">
    /// A lambda expression that specifies how to retrieve the value
    /// from the source object.
    /// </param>
    /// <remarks>
    /// The source expression can reference source properties, perform
    /// calculations, or combine multiple source values to produce
    /// the destination member value.
    ///
    /// Configuring a source expression clears any previously configured
    /// ignore rule for the destination member.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified source expression is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the associated member mapping definition has been sealed.
    /// </exception>
    public void MapFrom<TSourceMember>(
        Expression<Func<TSource, TSourceMember>> sourceExpression)
    {
        ArgumentNullException.ThrowIfNull(sourceExpression);
        _definition.SetSourceExpression(sourceExpression);
    }

    /// <summary>
    /// Configures the destination member to be ignored during mapping.
    /// </summary>
    /// <remarks>
    /// When a destination member is ignored, LiteMapper does not apply
    /// a source expression to populate that member.
    ///
    /// Calling this method clears any previously configured source expression.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the associated member mapping definition has been sealed.
    /// </exception>
    public void Ignore()
    {
        _definition.SetIgnored();
    }
}