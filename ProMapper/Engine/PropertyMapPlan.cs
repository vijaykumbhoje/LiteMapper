namespace ProMapper.Engine;

/// <summary>
/// Represents the compiled mapping instructions for a single source-to-destination
/// property mapping.
/// </summary>
/// <remarks>
/// This class stores the delegates used to read the source property, read the
/// existing destination property, and optionally assign the mapped value.
///
/// It also determines whether the source value can be assigned directly to
/// the destination property and whether reference tracking is required.
/// </remarks>
internal sealed class PropertyMapPlan
{
    /// <summary>
    /// Gets the type of the destination property.
    /// </summary>
    /// <value>
    /// The declared property type on the destination object.
    /// </value>
    public Type DestinationPropertyType { get; }

    /// <summary>
    /// Gets the compiled delegate used to retrieve the source property value.
    /// </summary>
    /// <value>
    /// A delegate that accepts a source object and returns the property value.
    /// </value>
    public Func<object, object?> SourceGetter { get; }

    /// <summary>
    /// Gets the compiled delegate used to retrieve the current destination property value.
    /// </summary>
    /// <value>
    /// A delegate that accepts a destination object and returns the current
    /// property value.
    /// </value>
    public Func<object, object?> DestinationGetter { get; }

    /// <summary>
    /// Gets the compiled delegate used to assign a value to the destination property.
    /// </summary>
    /// <value>
    /// An assignment delegate, or <see langword="null"/> when the destination
    /// property does not have a public setter.
    /// </value>
    /// <remarks>
    /// A null setter can be used for supported getter-only collection properties,
    /// where the existing collection instance can be populated instead of
    /// assigning a new instance.
    /// </remarks>
    public Action<object, object?>? DestinationSetter { get; }

    /// <summary>
    /// Gets a value indicating whether the source property type can be assigned
    /// directly to the destination property type.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when the destination property type is assignable
    /// from the source property type; otherwise, <see langword="false"/>.
    /// </value>
    public bool CanAssignDirectly { get; }

    /// <summary>
    /// Gets a value indicating whether mapping this property requires
    /// reference tracking.
    /// </summary>
    /// <value>
    /// <see langword="true"/> when the destination property type is a
    /// reference type other than <see cref="string"/>; otherwise,
    /// <see langword="false"/>.
    /// </value>
    /// <remarks>
    /// Nullable value types are evaluated using their underlying type.
    /// Reference tracking is used to preserve object references and help
    /// handle circular references during nested mapping operations.
    /// </remarks>
    public bool RequiresReferenceTracking { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PropertyMapPlan"/> class.
    /// </summary>
    /// <param name="sourcePropertyType">
    /// The declared type of the source property.
    /// </param>
    /// <param name="destinationPropertyType">
    /// The declared type of the destination property.
    /// </param>
    /// <param name="sourceGetter">
    /// The compiled delegate used to retrieve the source property value.
    /// </param>
    /// <param name="destinationGetter">
    /// The compiled delegate used to retrieve the existing destination property value.
    /// </param>
    /// <param name="destinationSetter">
    /// The optional compiled delegate used to assign the mapped value to the
    /// destination property. Can be null for supported getter-only collections.
    /// </param>
    /// <remarks>
    /// The constructor determines whether direct assignment is possible by
    /// checking type assignability. It also determines whether reference
    /// tracking is required based on the destination property's underlying type.
    /// </remarks>
    public PropertyMapPlan(
        Type sourcePropertyType,
        Type destinationPropertyType,
        Func<object, object?> sourceGetter,
        Func<object, object?> destinationGetter,
        Action<object, object?>? destinationSetter)
    {
        DestinationPropertyType = destinationPropertyType;
        SourceGetter = sourceGetter;
        DestinationGetter = destinationGetter;
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