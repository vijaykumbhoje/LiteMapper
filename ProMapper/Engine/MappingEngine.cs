using ProMapper.Configuration;
using ProMapper.Conversion;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace ProMapper.Engine;

/// <summary>
/// Executes object-to-object mappings using registered mapping definitions,
/// cached mapping plans, and custom type converters.
/// </summary>
/// <remarks>
/// The mapping engine handles direct property assignment, type conversion,
/// nested object mapping, and collection mapping. It also supports mapping
/// into existing destination objects and collections.
///
/// A <see cref="MappingContext"/> is created when reference tracking is
/// required by the mapping plan. This allows the engine to preserve object
/// references and handle circular object graphs.
/// </remarks>
internal sealed class MappingEngine
{
    private readonly MappingRegistry _registry;
    private readonly TypeConverterRegistry _converterRegistry;
    private readonly MappingPlanCache _planCache;

    private readonly IServiceProvider? _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="MappingEngine"/> class.
    /// </summary>
    /// <param name="registry">
    /// The registry containing configured object mapping definitions.
    /// </param>
    /// <param name="converterRegistry">
    /// The registry containing custom type converters.
    /// </param>
    /// <param name="serviceProvider">
    /// An optional service provider used to resolve dependencies required
    /// by custom type converters.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="registry"/> or
    /// <paramref name="converterRegistry"/> is null.
    /// </exception>
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

    /// <summary>
    /// Maps a source object to the specified destination type.
    /// </summary>
    /// <param name="source">
    /// The source object to map.
    /// </param>
    /// <param name="destinationType">
    /// The type of the destination object.
    /// </param>
    /// <param name="destination">
    /// An optional existing destination object to populate.
    /// When null, a new destination instance is created.
    /// </param>
    /// <returns>
    /// The mapped destination object.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a destination instance cannot be created.
    /// </exception>
    public object Map(
        object source,
        Type destinationType,
        object? destination = null)
    {
        return Map(source, destinationType, destination, null);
    }

    /// <summary>
    /// Maps a source object to a destination object using an optional
    /// mapping context.
    /// </summary>
    /// <param name="source">
    /// The source object to map.
    /// </param>
    /// <param name="destinationType">
    /// The type of the destination object.
    /// </param>
    /// <param name="destination">
    /// An optional existing destination instance to populate.
    /// </param>
    /// <param name="context">
    /// The mapping context used to track previously mapped objects and
    /// manage reference tracking across nested mappings.
    /// </param>
    /// <returns>
    /// The mapped destination object.
    /// </returns>
    /// <remarks>
    /// The method retrieves a cached mapping plan, creates a mapping context
    /// when reference tracking is required, and registers the destination
    /// before mapping its properties. Registering the destination before
    /// processing properties helps prevent infinite recursion when circular
    /// references are encountered.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a destination instance cannot be created.
    /// </exception>
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
                var existingValue = property.DestinationGetter(destination);

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

                if (value is IEnumerable &&
                      value is not string &&
                      TryGetCollectionElementType(
                          property.DestinationPropertyType,
                out _))
                {
                    convertedValue = MapValue(
                        value,
                        property.DestinationPropertyType,
                        context,
                        existingValue);
                }
                else if (property.CanAssignDirectly)
                {
                    convertedValue = value;
                }
                else
                {
                    convertedValue = MapValue(
                        value,
                        property.DestinationPropertyType,
                        context,
                        existingValue);
                }

                if (property.DestinationSetter != null)
                {
                    property.DestinationSetter(
                        destination,
                        convertedValue);
                }
            }

            return destination;
        }
        finally
        {
            if (context != null)
                context.Exit();
        }
    }

    /// <summary>
    /// Converts or maps a source value to the specified destination type.
    /// </summary>
    /// <param name="sourceValue">
    /// The source value to convert or map.
    /// </param>
    /// <param name="destinationType">
    /// The requested destination type.
    /// </param>
    /// <param name="context">
    /// The optional mapping context used for nested object mapping
    /// and reference tracking.
    /// </param>
    /// <param name="existingDestination">
    /// An optional existing destination value. For collection mappings,
    /// this value may be reused and populated when it is compatible
    /// and mutable.
    /// </param>
    /// <returns>
    /// The converted or mapped value, or null when the source value is null.
    /// </returns>
    /// <remarks>
    /// Conversion is attempted in the following order:
    /// <list type="number">
    /// <item>
    /// <description>Map compatible collection values into destination collections.</description>
    /// </item>
    /// <item>
    /// <description>Return values that can be assigned directly.</description>
    /// </item>
    /// <item>
    /// <description>Use a registered custom type converter.</description>
    /// </item>
    /// <item>
    /// <description>Map supported collection types.</description>
    /// </item>
    /// <item>
    /// <description>Perform nested object mapping when a mapping definition exists.</description>
    /// </item>
    /// <item>
    /// <description>Fall back to standard value conversion.</description>
    /// </item>
    /// </list>
    /// </remarks>
    private object? MapValue(
        object? sourceValue,
        Type destinationType,
        MappingContext? context,
        object? existingDestination = null)
    {
        if (sourceValue == null)
            return null;

        var targetType =
            Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        var sourceType = sourceValue.GetType();

        // If the source is an assignable collection, still create or populate
        // the destination collection instead of sharing the source instance.
        if (sourceValue is IEnumerable assignableCollection &&
            sourceValue is not string &&
            targetType.IsAssignableFrom(sourceType) &&
            TryGetCollectionElementType(
                targetType,
                out var assignableElementType))
        {
            return MapCollection(
                assignableCollection,
                targetType,
                assignableElementType,
                context,
                existingDestination);
        }

        // Direct assignment for other compatible types.
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
                context,
                existingDestination);
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

    /// <summary>
    /// Maps the elements of a source collection into a destination collection.
    /// </summary>
    /// <param name="sourceCollection">
    /// The source collection whose elements will be mapped.
    /// </param>
    /// <param name="destinationType">
    /// The destination collection type.
    /// </param>
    /// <param name="destinationElementType">
    /// The element type of the destination collection.
    /// </param>
    /// <param name="context">
    /// The optional mapping context used when mapping collection elements.
    /// </param>
    /// <param name="existingDestination">
    /// An optional existing destination collection that may be reused
    /// when it is compatible and mutable.
    /// </param>
    /// <returns>
    /// A mapped collection compatible with
    /// <paramref name="destinationType"/>.
    /// </returns>
    /// <remarks>
    /// Supported destination collection types include arrays,
    /// <see cref="HashSet{T}"/>, <see cref="Queue{T}"/>,
    /// <see cref="Stack{T}"/>, <see cref="List{T}"/>, and the supported
    /// collection interfaces.
    ///
    /// When a compatible mutable destination collection is provided,
    /// the engine attempts to clear and populate that collection.
    /// Stack elements are pushed in source enumeration order, so
    /// stack enumeration follows last-in-first-out ordering.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the requested destination collection type cannot
    /// be created using the supported collection implementations.
    /// </exception>
    private object MapCollection(
        IEnumerable sourceCollection,
        Type destinationType,
        Type destinationElementType,
        MappingContext? context,
        object? existingDestination)
    {
        var mappedItems = new List<object?>();

        foreach (var item in sourceCollection)
        {
            mappedItems.Add(
                MapValue(item, destinationElementType, context));
        }

        // Arrays
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

        // Reuse an existing mutable destination collection.
        if (existingDestination != null &&
            destinationType.IsInstanceOfType(existingDestination))
        {
            if (TryPopulateExistingCollection(
                existingDestination,
                mappedItems,
                destinationElementType))
            {
                return existingDestination;
            }
        }

        // HashSet<T> and ISet<T>
        if (IsSetCollection(destinationType))
        {
            var hashSetType = typeof(HashSet<>)
                .MakeGenericType(destinationElementType);

            if (!destinationType.IsAssignableFrom(hashSetType))
            {
                throw new InvalidOperationException(
                    $"Cannot create collection type '{destinationType.Name}'.");
            }

            var result = Activator.CreateInstance(hashSetType)!;

            var addMethod = hashSetType.GetMethod(
                "Add",
                new[] { destinationElementType })!;

            foreach (var item in mappedItems)
            {
                addMethod.Invoke(result, new[] { item });
            }

            return result;
        }

        // Queue<T>
        if (destinationType.IsGenericType &&
            destinationType.GetGenericTypeDefinition() == typeof(Queue<>))
        {
            var queueType = typeof(Queue<>)
                .MakeGenericType(destinationElementType);

            var queue = Activator.CreateInstance(queueType)!;

            var enqueueMethod = queueType.GetMethod("Enqueue")!;

            foreach (var item in mappedItems)
            {
                enqueueMethod.Invoke(queue, new[] { item });
            }

            return queue;
        }

        // Stack<T>
        if (destinationType.IsGenericType &&
            destinationType.GetGenericTypeDefinition() == typeof(Stack<>))
        {
            var stackType = typeof(Stack<>)
                .MakeGenericType(destinationElementType);

            var stack = Activator.CreateInstance(stackType)!;

            var pushMethod = stackType.GetMethod("Push")!;

            // Push in source order. Stack enumeration is LIFO.
            foreach (var item in mappedItems)
            {
                pushMethod.Invoke(stack, new[] { item });
            }

            return stack;
        }

        // List<T> and supported collection interfaces
        var listType = typeof(List<>)
            .MakeGenericType(destinationElementType);

        var list = (IList)Activator.CreateInstance(listType)!;

        foreach (var item in mappedItems)
        {
            list.Add(item);
        }

        if (!destinationType.IsAssignableFrom(listType))
        {
            throw new InvalidOperationException(
                $"Collection type '{destinationType.Name}' is not supported.");
        }

        return list;
    }

    /// <summary>
    /// Determines whether the specified collection type represents
    /// a set collection.
    /// </summary>
    /// <param name="collectionType">
    /// The collection type to inspect.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the type is
    /// <see cref="HashSet{T}"/> or <see cref="ISet{T}"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    private static bool IsSetCollection(Type collectionType)
    {
        if (!collectionType.IsGenericType)
            return false;

        var genericDefinition = collectionType.GetGenericTypeDefinition();

        return genericDefinition == typeof(HashSet<>) ||
               genericDefinition == typeof(ISet<>);
    }

    /// <summary>
    /// Attempts to determine the element type of a supported collection.
    /// </summary>
    /// <param name="collectionType">
    /// The collection type to inspect.
    /// </param>
    /// <param name="elementType">
    /// When this method returns <see langword="true"/>, contains the
    /// collection element type. Otherwise, contains null.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the specified type is an array or
    /// one of the supported generic collection types; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Supported generic collection definitions include
    /// <see cref="List{T}"/>, <see cref="IEnumerable{T}"/>,
    /// <see cref="ICollection{T}"/>, <see cref="IReadOnlyCollection{T}"/>,
    /// <see cref="IReadOnlyList{T}"/>, <see cref="HashSet{T}"/>,
    /// <see cref="ISet{T}"/>, <see cref="Queue{T}"/>, and
    /// <see cref="Stack{T}"/>.
    /// </remarks>
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
                typeof(IReadOnlyList<>),
                typeof(HashSet<>),
                typeof(ISet<>),
                typeof(Queue<>),
                typeof(Stack<>)
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

    /// <summary>
    /// Attempts to populate an existing destination collection with
    /// the supplied mapped items.
    /// </summary>
    /// <param name="destination">
    /// The existing destination collection to populate.
    /// </param>
    /// <param name="mappedItems">
    /// The mapped items to add to the destination collection.
    /// </param>
    /// <param name="elementType">
    /// The element type used to locate compatible collection methods
    /// through reflection.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the destination collection is
    /// successfully populated; otherwise, <see langword="false"/>
    /// when the collection does not expose a supported mutable
    /// collection interface or method.
    /// </returns>
    /// <remarks>
    /// The method supports mutable <see cref="IList"/> implementations
    /// and collections exposing compatible <c>Clear</c> and
    /// <c>Add</c>, <c>Enqueue</c>, or <c>Push</c> methods.
    ///
    /// Existing collection contents are cleared before the mapped
    /// items are added.
    /// </remarks>
    private static bool TryPopulateExistingCollection(
        object destination,
        List<object?> mappedItems,
        Type elementType)
    {
        var collectionType = destination.GetType();

        // List<T>
        if (destination is IList list)
        {
            if (list.IsReadOnly || list.IsFixedSize)
                return false;

            list.Clear();

            foreach (var item in mappedItems)
                list.Add(item);

            return true;
        }

        // HashSet<T>, Queue<T>, Stack<T>
        var clearMethod = collectionType.GetMethod(
            "Clear",
            Type.EmptyTypes);

        if (clearMethod == null)
            return false;

        var addMethod = collectionType.GetMethod(
            "Add",
            new[] { elementType });

        var enqueueMethod = collectionType.GetMethod(
            "Enqueue",
            new[] { elementType });

        var pushMethod = collectionType.GetMethod(
            "Push",
            new[] { elementType });

        if (addMethod == null &&
            enqueueMethod == null &&
            pushMethod == null)
        {
            return false;
        }

        clearMethod.Invoke(destination, null);

        if (addMethod != null)
        {
            foreach (var item in mappedItems)
                addMethod.Invoke(destination, new[] { item });

            return true;
        }

        if (enqueueMethod != null)
        {
            foreach (var item in mappedItems)
                enqueueMethod.Invoke(destination, new[] { item });

            return true;
        }

        if (pushMethod != null)
        {
            foreach (var item in mappedItems)
            {
                pushMethod.Invoke(destination, new[] { item });
            }

            return true;
        }

        return false;
    }
}