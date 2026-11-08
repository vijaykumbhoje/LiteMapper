using System.Linq.Expressions;
using System.Reflection;
using ProMapper.Configuration;

namespace ProMapper.Engine;

/// <summary>
/// Represents a compiled mapping plan between a source type and a destination type.
/// </summary>
/// <remarks>
/// A mapping plan contains the property mapping instructions required to map
/// objects of the configured source and destination types.
///
/// Property accessors and setters are compiled into delegates using expression
/// trees to avoid repeated reflection during mapping operations.
///
/// The plan also determines whether reference tracking is required based on
/// the properties included in the mapping.
/// </remarks>
internal sealed class MappingPlan
{
    /// <summary>
    /// Gets the compiled property mapping instructions.
    /// </summary>
    /// <value>
    /// A read-only collection of <see cref="PropertyMapPlan"/> instances
    /// describing how individual properties are mapped.
    /// </value>
    public IReadOnlyList<PropertyMapPlan> Properties { get; }

    /// <summary>
    /// Gets a value indicating whether this mapping plan requires
    /// reference tracking.
    /// </summary>
    /// <value>
    /// <see langword="true"/> if at least one property mapping requires
    /// reference tracking; otherwise, <see langword="false"/>.
    /// </value>
    public bool RequiresReferenceTracking { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingPlan"/> class.
    /// </summary>
    /// <param name="properties">
    /// The compiled property mapping instructions.
    /// </param>
    /// <remarks>
    /// The supplied property mappings are exposed as a read-only collection.
    /// The reference tracking requirement is calculated from the individual
    /// property mapping plans.
    /// </remarks>
    private MappingPlan(List<PropertyMapPlan> properties)
    {
        Properties = properties.AsReadOnly();

        RequiresReferenceTracking =
            properties.Any(p => p.RequiresReferenceTracking);
    }

    /// <summary>
    /// Builds a mapping plan for the specified source and destination types.
    /// </summary>
    /// <param name="sourceType">
    /// The source type whose properties provide mapping values.
    /// </param>
    /// <param name="destinationType">
    /// The destination type whose properties receive mapped values.
    /// </param>
    /// <param name="definition">
    /// The mapping definition containing explicit member configurations,
    /// including ignored members and custom source expressions.
    /// </param>
    /// <returns>
    /// A compiled <see cref="MappingPlan"/> containing the property mappings
    /// supported by the supplied types and configuration.
    /// </returns>
    /// <remarks>
    /// Property mappings are created using the following rules:
    /// <list type="number">
    /// <item>
    /// <description>
    /// Destination properties must have a public getter and must not be indexers.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Destination properties must have a public setter or be a supported
    /// getter-only collection.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// Explicit member configurations take precedence over convention-based
    /// property matching.
    /// </description>
    /// </item>
    /// <item>
    /// <description>
    /// When no explicit configuration exists, source properties are matched
    /// by name using case-insensitive comparison.
    /// </description>
    /// </item>
    /// </list>
    ///
    /// Getter and setter delegates are compiled for the selected properties
    /// and stored in the resulting mapping plan.
    /// </remarks>
    public static MappingPlan Build(
        Type sourceType,
        Type destinationType,
        MappingDefinition definition)
    {
        var properties = new List<PropertyMapPlan>();

        var sourceProperties = sourceType.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        var destinationProperties = destinationType.GetProperties(
            BindingFlags.Instance | BindingFlags.Public);

        foreach (var destinationProperty in destinationProperties)
        {
            if (destinationProperty.GetIndexParameters().Length > 0 ||
                destinationProperty.GetMethod?.IsPublic != true)
            {
                continue;
            }

            var hasPublicSetter =
                destinationProperty.SetMethod?.IsPublic == true;

            if (!hasPublicSetter &&
                !IsSupportedGetterOnlyCollection(destinationProperty.PropertyType))
            {
                continue;
            }

            Func<object, object?>? sourceGetter = null;
            Type? sourcePropertyType = null;

            // Explicit configuration takes precedence over convention mapping.
            if (definition.MemberMappings.TryGetValue(
                destinationProperty.Name,
                out var memberMapping))
            {
                if (memberMapping.IsIgnored)
                    continue;

                if (memberMapping.SourceExpression == null)
                    continue;

                sourceGetter = CompileSourceExpression(
                    memberMapping.SourceExpression);

                sourcePropertyType =
                    memberMapping.SourceExpression.ReturnType;
            }
            else
            {
                var sourceProperty = sourceProperties.FirstOrDefault(
                    property =>
                        property.CanRead &&
                        property.GetMethod?.IsPublic == true &&
                        property.GetIndexParameters().Length == 0 &&
                        string.Equals(
                            property.Name,
                            destinationProperty.Name,
                            StringComparison.OrdinalIgnoreCase));

                if (sourceProperty == null)
                    continue;

                sourcePropertyType = sourceProperty.PropertyType;

                sourceGetter = CompileSourcePropertyGetter(
                    sourceType,
                    sourceProperty);
            }

            var destinationGetter = CompileDestinationPropertyGetter(
                destinationType,
                destinationProperty);

            Action<object, object?>? destinationSetter =
                hasPublicSetter
                    ? CompilePropertySetter(destinationType, destinationProperty)
                    : null;

            properties.Add(new PropertyMapPlan(
                sourcePropertyType!,
                destinationProperty.PropertyType,
                sourceGetter,
                destinationGetter,
                destinationSetter));
        }

        return new MappingPlan(properties);
    }

    /// <summary>
    /// Compiles a delegate that retrieves a property value from a source object.
    /// </summary>
    /// <param name="sourceType">
    /// The runtime type expected for the source object.
    /// </param>
    /// <param name="property">
    /// The source property to access.
    /// </param>
    /// <returns>
    /// A delegate that accepts a source object and returns the selected
    /// property value as an object.
    /// </returns>
    /// <remarks>
    /// An expression tree is created to cast the input object to the specified
    /// source type, access the property, convert the result to
    /// <see cref="object"/>, and compile the expression into a delegate.
    /// </remarks>
    private static Func<object, object?> CompileSourcePropertyGetter(
        Type sourceType,
        PropertyInfo property)
    {
        var parameter = Expression.Parameter(typeof(object), "source");

        var typedSource = Expression.Convert(
            parameter,
            sourceType);

        var propertyAccess = Expression.Property(
            typedSource,
            property);

        var result = Expression.Convert(
            propertyAccess,
            typeof(object));

        return Expression.Lambda<Func<object, object?>>(
            result,
            parameter).Compile();
    }

    /// <summary>
    /// Compiles a delegate that retrieves a property value from a destination object.
    /// </summary>
    /// <param name="destinationType">
    /// The runtime type expected for the destination object.
    /// </param>
    /// <param name="property">
    /// The destination property to access.
    /// </param>
    /// <returns>
    /// A delegate that accepts a destination object and returns the selected
    /// property value as an object.
    /// </returns>
    /// <remarks>
    /// The generated expression casts the input object to the destination type,
    /// accesses the specified property, converts its value to
    /// <see cref="object"/>, and compiles the expression into a delegate.
    /// </remarks>
    private static Func<object, object?> CompileDestinationPropertyGetter(
        Type destinationType,
        PropertyInfo property)
    {
        var parameter = Expression.Parameter(
            typeof(object),
            "destination");

        var typedDestination = Expression.Convert(
            parameter,
            destinationType);

        var propertyAccess = Expression.Property(
            typedDestination,
            property);

        var result = Expression.Convert(
            propertyAccess,
            typeof(object));

        return Expression.Lambda<Func<object, object?>>(
            result,
            parameter).Compile();
    }

    /// <summary>
    /// Compiles a configured source expression into a delegate that accepts
    /// a source object.
    /// </summary>
    /// <param name="sourceExpression">
    /// The configured lambda expression used to retrieve the source value.
    /// </param>
    /// <returns>
    /// A delegate that accepts a source object and evaluates the configured
    /// expression, returning its result as an object.
    /// </returns>
    /// <remarks>
    /// The original lambda parameter is replaced with a parameter that accepts
    /// <see cref="object"/>. The replacement expression casts that object to
    /// the original parameter type before evaluating the expression.
    /// </remarks>
    private static Func<object, object?> CompileSourceExpression(
        LambdaExpression sourceExpression)
    {
        var sourceParameter = Expression.Parameter(
            typeof(object),
            "source");

        var typedSource = Expression.Convert(
            sourceParameter,
            sourceExpression.Parameters[0].Type);

        var body = new ParameterReplaceVisitor(
            sourceExpression.Parameters[0],
            typedSource).Visit(sourceExpression.Body)!;

        var result = Expression.Convert(body, typeof(object));

        return Expression.Lambda<Func<object, object?>>(
            result,
            sourceParameter).Compile();
    }

    /// <summary>
    /// Compiles a delegate that assigns a value to a destination property.
    /// </summary>
    /// <param name="destinationType">
    /// The runtime type expected for the destination object.
    /// </param>
    /// <param name="property">
    /// The destination property to assign.
    /// </param>
    /// <returns>
    /// A delegate that accepts a destination object and a value, then assigns
    /// the value to the specified property.
    /// </returns>
    /// <remarks>
    /// The generated expression casts the destination object and assigned value
    /// to their respective types, performs the property assignment, and
    /// compiles the expression into an action delegate.
    /// </remarks>
    private static Action<object, object?> CompilePropertySetter(
        Type destinationType,
        PropertyInfo property)
    {
        var destinationParameter = Expression.Parameter(
            typeof(object),
            "destination");

        var valueParameter = Expression.Parameter(
            typeof(object),
            "value");

        var typedDestination = Expression.Convert(
            destinationParameter,
            destinationType);

        var typedValue = Expression.Convert(
            valueParameter,
            property.PropertyType);

        var assignment = Expression.Assign(
            Expression.Property(typedDestination, property),
            typedValue);

        return Expression.Lambda<Action<object, object?>>(
            assignment,
            destinationParameter,
            valueParameter).Compile();
    }

    /// <summary>
    /// Determines whether a property type is a supported collection type
    /// that can be mapped without a public setter.
    /// </summary>
    /// <param name="type">
    /// The property type to inspect.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if the type is a supported generic collection
    /// type other than a string or array; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Supported generic collection definitions include
    /// <see cref="List{T}"/>, <see cref="IEnumerable{T}"/>,
    /// <see cref="ICollection{T}"/>, <see cref="IReadOnlyCollection{T}"/>,
    /// <see cref="IReadOnlyList{T}"/>, <see cref="HashSet{T}"/>,
    /// <see cref="ISet{T}"/>, <see cref="Queue{T}"/>, and
    /// <see cref="Stack{T}"/>.
    ///
    /// This check allows supported getter-only collection properties to
    /// participate in mapping because their existing collection instances
    /// can be populated without assigning a new collection reference.
    /// </remarks>
    private static bool IsSupportedGetterOnlyCollection(Type type)
    {
        if (type == typeof(string) || type.IsArray)
            return false;

        var enumerableType = type
            .GetInterfaces()
            .Append(type)
            .FirstOrDefault(t =>
                t.IsGenericType &&
                t.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType == null)
            return false;

        var supportedTypes = new[]
        {
            typeof(List<>),
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>),
            typeof(HashSet<>),
            typeof(ISet<>),
            typeof(Queue<>),
            typeof(Stack<>)
        };

        return type.IsGenericType &&
               supportedTypes.Contains(type.GetGenericTypeDefinition());
    }

    /// <summary>
    /// Replaces occurrences of a specified expression parameter with
    /// another expression.
    /// </summary>
    /// <remarks>
    /// This visitor is used when compiling configured source expressions.
    /// It replaces the original strongly typed lambda parameter with an
    /// expression that converts an object parameter to the required source type.
    /// </remarks>
    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly Expression _replacement;

        /// <summary>
        /// Initializes a new instance of the
        /// <see cref="ParameterReplaceVisitor"/> class.
        /// </summary>
        /// <param name="source">
        /// The original parameter expression to replace.
        /// </param>
        /// <param name="replacement">
        /// The expression that replaces the original parameter.
        /// </param>
        public ParameterReplaceVisitor(
            ParameterExpression source,
            Expression replacement)
        {
            _source = source;
            _replacement = replacement;
        }

        /// <summary>
        /// Visits a parameter expression and replaces it when it matches
        /// the configured source parameter.
        /// </summary>
        /// <param name="node">
        /// The parameter expression being visited.
        /// </param>
        /// <returns>
        /// The replacement expression when <paramref name="node"/> matches
        /// the configured source parameter; otherwise, the result of the
        /// base visitor implementation.
        /// </returns>
        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == _source
                ? _replacement
                : base.VisitParameter(node);
        }
    }
}