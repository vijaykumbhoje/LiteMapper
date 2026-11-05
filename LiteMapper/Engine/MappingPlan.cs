using System.Linq.Expressions;
using System.Reflection;
using LiteMapper.Configuration;

namespace LiteMapper.Engine;

internal sealed class MappingPlan
{
    public IReadOnlyList<PropertyMapPlan> Properties { get; }

    public bool RequiresReferenceTracking { get; }

    private MappingPlan(List<PropertyMapPlan> properties)
    {
        Properties = properties;

        RequiresReferenceTracking =
            properties.Any(p => p.RequiresReferenceTracking);
    }

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
            if (destinationProperty.SetMethod?.IsPublic != true ||
                destinationProperty.GetIndexParameters().Length > 0)
            {
                continue;
            }

            Func<object, object?>? sourceGetter = null;
            Type? sourcePropertyType = null;

            // Explicit configuration takes precedence over convention mapping.
            if (definition.MemberMappings.TryGetValue(
                destinationProperty.Name, out var memberMapping))
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
                        property.GetIndexParameters().Length == 0 &&
                        string.Equals(
                            property.Name,
                            destinationProperty.Name,
                            StringComparison.OrdinalIgnoreCase));

                if (sourceProperty == null)
                    continue;
                
                sourcePropertyType = sourceProperty.PropertyType;

                sourceGetter = CompilePropertyGetter(
                    sourceType, sourceProperty);
            }

            var destinationSetter = CompilePropertySetter(
                destinationType, destinationProperty);

            properties.Add(new PropertyMapPlan(
            sourcePropertyType!,
            destinationProperty.PropertyType,
            sourceGetter,
            destinationSetter));
        }

        return new MappingPlan(properties);
    }

    private static Func<object, object?> CompilePropertyGetter(
        Type sourceType,
        PropertyInfo property)
    {
        var sourceParameter = Expression.Parameter(typeof(object), "source");

        var typedSource = Expression.Convert(
            sourceParameter, sourceType);

        var propertyAccess = Expression.Property(
            typedSource, property);

        var result = Expression.Convert(
            propertyAccess, typeof(object));

        return Expression.Lambda<Func<object, object?>>(
            result, sourceParameter).Compile();
    }

    private static Func<object, object?> CompileSourceExpression(
        LambdaExpression sourceExpression)
    {
        var sourceParameter = Expression.Parameter(typeof(object), "source");

        var typedSource = Expression.Convert(
            sourceParameter,
            sourceExpression.Parameters[0].Type);

        var body = new ParameterReplaceVisitor(
            sourceExpression.Parameters[0],
            typedSource).Visit(sourceExpression.Body)!;

        var result = Expression.Convert(body, typeof(object));

        return Expression.Lambda<Func<object, object?>>(
            result, sourceParameter).Compile();
    }

    private static Action<object, object?> CompilePropertySetter(
        Type destinationType,
        PropertyInfo property)
    {
        var destinationParameter =
            Expression.Parameter(typeof(object), "destination");

        var valueParameter =
            Expression.Parameter(typeof(object), "value");

        var typedDestination = Expression.Convert(
            destinationParameter, destinationType);

        var typedValue = Expression.Convert(
            valueParameter, property.PropertyType);

        var assignment = Expression.Assign(
            Expression.Property(typedDestination, property),
            typedValue);

        return Expression.Lambda<Action<object, object?>>(
            assignment,
            destinationParameter,
            valueParameter).Compile();
    }

    private sealed class ParameterReplaceVisitor : ExpressionVisitor
    {
        private readonly ParameterExpression _source;
        private readonly Expression _replacement;

        public ParameterReplaceVisitor(
            ParameterExpression source,
            Expression replacement)
        {
            _source = source;
            _replacement = replacement;
        }

        protected override Expression VisitParameter(
            ParameterExpression node)
        {
            return node == _source
                ? _replacement
                : base.VisitParameter(node);
        }
    }
}