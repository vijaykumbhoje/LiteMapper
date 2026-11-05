using LiteMapper.Configuration;
using LiteMapper.Conversion;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteMapper.Engine;

internal sealed class MappingEngine
{
    private readonly MappingRegistry _registry;
    private readonly TypeConverterRegistry _converterRegistry;
    private readonly MappingPlanCache _planCache;

    private readonly IServiceProvider? _serviceProvider;

    public MappingEngine(
     MappingRegistry registry,
     TypeConverterRegistry converterRegistry,
     IServiceProvider? serviceProvider = null)
    {
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(converterRegistry);

        _registry = registry;
        _converterRegistry = converterRegistry;
        _serviceProvider = serviceProvider;

        _planCache = new MappingPlanCache(registry);
    }

    public object Map(
        object source,
        Type destinationType,
        object? destination = null)
    {
        return Map(source, destinationType, destination, null);
    }

    private object Map(
    object source,
    Type destinationType,
    object? destination,
    MappingContext? context)
    {
        ArgumentNullException.ThrowIfNull(source);

        // Reuse an existing destination if this source object
        // has already been mapped in the current object graph.
        if (context != null &&
            context.TryGetMappedObject(
                source, destinationType, out var existingDestination))
        {
            return existingDestination!;
        }

        var plan = _planCache.GetPlan(
            source.GetType(),
            destinationType);

        // Create a context only when reference tracking is needed.
        context ??= plan.RequiresReferenceTracking
            ? new MappingContext()
            : null;

        if (context != null)
            context.Enter();

        try
        {
            destination ??= Activator.CreateInstance(destinationType)
                ?? throw new InvalidOperationException(
                    $"Cannot create instance of {destinationType.Name}.");

            // Register before mapping properties to protect against cycles.
            if (context != null)
            {
                context.Register(source, destinationType, destination);
            }

            foreach (var property in plan.Properties)
            {
                var value = property.SourceGetter(source);

                // Preserve the existing destination value for null
                // source values targeting non-nullable value types.
                if (value == null &&
                    property.DestinationPropertyType.IsValueType &&
                    Nullable.GetUnderlyingType(
                        property.DestinationPropertyType) == null)
                {
                    continue;
                }

                object? convertedValue;

                if (property.CanAssignDirectly)
                {
                    convertedValue = value;
                }
                else
                {
                    convertedValue = MapValue(
                        value,
                        property.DestinationPropertyType,
                        context);
                }

                property.DestinationSetter(
                    destination,
                    convertedValue);
            }

            return destination;
        }
        finally
        {
            if (context != null)
                context.Exit();
        }
    }

    private object? MapValue(
     object? sourceValue,
     Type destinationType,
     MappingContext? context)
    {
        if (sourceValue == null)
            return null;

        var targetType =
            Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        var sourceType = sourceValue.GetType();

        // Direct assignment when the source is already compatible.
        if (targetType.IsAssignableFrom(sourceType))
            return sourceValue;

        // Try registered custom type converters.
        if (_converterRegistry.TryConvert(
    _serviceProvider,
    sourceValue,
    destinationType,
    out var convertedValue))
        {
            return convertedValue;
        }

        // Handle collections before attempting nested object mapping.
        if (sourceValue is IEnumerable sourceCollection &&
            sourceValue is not string &&
            TryGetCollectionElementType(
                targetType,
                out var destinationElementType))
        {
            return MapCollection(
                sourceCollection,
                targetType,
                destinationElementType,
                context);
        }

        // Handle nested object mapping.
        if (_registry.TryGet(sourceType, targetType, out var definition) &&
            definition != null)
        {
            return Map(sourceValue, targetType, null, context);
        }

        // Fall back to standard type conversion.
        return ValueConverter.ConvertValue(
            sourceValue,
            destinationType);
    }

    private object MapCollection(
    IEnumerable sourceCollection,
    Type destinationType,
    Type destinationElementType,
    MappingContext? context)
    {
        var mappedItems = new List<object?>();

        foreach (var item in sourceCollection)
        {
            mappedItems.Add(MapValue(item, destinationElementType, context));
        }

        // Arrays need to be created with their actual element type.
        if (destinationType.IsArray)
        {
            var array = Array.CreateInstance(
                destinationElementType,
                mappedItems.Count);

            for (var i = 0; i < mappedItems.Count; i++)
            {
                array.SetValue(mappedItems[i], i);
            }

            return array;
        }

        // For supported interfaces and List<T>, use List<T>.
        var listType = typeof(List<>).MakeGenericType(destinationElementType);
        var result = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in mappedItems)
        {
            result.Add(item);
        }

        if (!destinationType.IsAssignableFrom(listType))
        {
            throw new InvalidOperationException(
                $"Collection type '{destinationType.Name}' is not supported. " +
                "Use an array, List<T>, or a supported collection interface.");
        }

        return result;
    }

    private static bool TryGetCollectionElementType(
        Type collectionType,
        out Type elementType)
    {
        if (collectionType.IsArray)
        {
            elementType = collectionType.GetElementType()!;
            return true;
        }

        var enumerableType = collectionType
            .GetInterfaces()
            .Append(collectionType)
            .FirstOrDefault(type =>
                type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerableType != null)
        {
            elementType = enumerableType.GetGenericArguments()[0];

            var supportedDefinitions = new[]
            {
            typeof(List<>),
            typeof(IEnumerable<>),
            typeof(ICollection<>),
            typeof(IReadOnlyCollection<>),
            typeof(IReadOnlyList<>)
        };

            var isSupported = collectionType.IsGenericType &&
                supportedDefinitions.Contains(
                    collectionType.GetGenericTypeDefinition());

            if (isSupported)
                return true;
        }

        elementType = null!;
        return false;
    }
}
