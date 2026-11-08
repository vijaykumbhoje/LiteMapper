namespace ProMapper.Configuration;

/// <summary>
/// Provides a base class for defining reusable object mapping profiles.
/// </summary>
/// <remarks>
/// Mapping profiles group related mapping definitions between source
/// and destination types.
///
/// Derived profiles define mappings by calling
/// <see cref="CreateMap{TSource, TDestination}"/> and configuring
/// individual destination members using the fluent mapping API.
///
/// Profiles are registered with a <see cref="MapperConfiguration"/>,
/// which validates and seals the registered mapping definitions
/// during initialization.
/// </remarks>
public abstract class MappingProfile
{
    /// <summary>
    /// Gets the collection of mapping definitions configured
    /// within this profile.
    /// </summary>
    /// <value>
    /// A list containing the mapping definitions created by this profile.
    /// </value>
    internal List<MappingDefinition> Definitions { get; } = new();

    /// <summary>
    /// Creates a mapping definition between the specified source
    /// and destination types.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type from which values are mapped.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The destination type to which values are mapped.
    /// </typeparam>
    /// <returns>
    /// A <see cref="MappingExpression{TSource, TDestination}"/> instance
    /// that provides a fluent API for configuring member-level mappings.
    /// </returns>
    /// <remarks>
    /// Each call creates a new mapping definition and adds it to the
    /// current profile.
    ///
    /// Derived profiles can use the returned mapping expression to
    /// configure custom source expressions and ignored destination members.
    /// </remarks>
    protected MappingExpression<TSource, TDestination>
        CreateMap<TSource, TDestination>()
    {
        var definition = new MappingDefinition(
            typeof(TSource),
            typeof(TDestination));

        Definitions.Add(definition);

        return new MappingExpression<TSource, TDestination>(
            definition);
    }
}