namespace LiteMapper.Conversion;

/// <summary>
/// Maintains a registry of custom type converters and provides
/// functionality to register, resolve, and execute type conversions.
/// </summary>
/// <remarks>
/// Converters are registered using source and destination type combinations.
///
/// The registry supports two converter registration approaches:
/// <list type="bullet">
/// <item>
/// <description>Registering an existing <see cref="ITypeConverter{TSource, TDestination}"/> instance.</description>
/// </item>
/// <item>
/// <description>Registering a converter implementation type that can be resolved through an <see cref="IServiceProvider"/> or instantiated using a parameterless constructor.</description>
/// </item>
/// </list>
///
/// Once sealed, the registry does not allow additional converter registrations.
/// </remarks>
internal sealed class TypeConverterRegistry
{
    private readonly Dictionary<
        (Type Source, Type Destination),
        Func<IServiceProvider?, object, object?>> _converters = new();

    private bool _isSealed;

    /// <summary>
    /// Registers an existing custom type converter instance
    /// for the specified source and destination types.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type handled by the converter.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The destination type produced by the converter.
    /// </typeparam>
    /// <param name="converter">
    /// The custom type converter instance used to perform conversions.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the specified converter is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a converter for the same source and destination
    /// type combination has already been registered or when the
    /// registry has been sealed.
    /// </exception>
    public void Register<TSource, TDestination>(
        ITypeConverter<TSource, TDestination> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        EnsureNotSealed();

        RegisterCore<TSource, TDestination>(
            (_, source) => converter.Convert((TSource)source));
    }

    /// <summary>
    /// Registers a custom type converter using its implementation type.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type handled by the converter.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The destination type produced by the converter.
    /// </typeparam>
    /// <typeparam name="TConverter">
    /// The concrete converter implementation type.
    /// Must implement <see cref="ITypeConverter{TSource, TDestination}"/>.
    /// </typeparam>
    /// <remarks>
    /// When a service provider is available, the converter is resolved
    /// through dependency injection using its corresponding
    /// <see cref="ITypeConverter{TSource, TDestination}"/> service registration.
    ///
    /// The resolved converter must be compatible with the specified
    /// <typeparamref name="TConverter"/> implementation type.
    ///
    /// When no service provider is available, the converter is created
    /// using <see cref="Activator.CreateInstance(Type)"/> and must have
    /// a parameterless constructor.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a converter for the same source and destination
    /// type combination has already been registered, when the registry
    /// has been sealed, when the converter cannot be resolved through
    /// dependency injection, when the resolved converter has an
    /// unexpected implementation type, or when the converter cannot
    /// be instantiated.
    /// </exception>
    public void RegisterType<TSource, TDestination, TConverter>()
        where TConverter : class, ITypeConverter<TSource, TDestination>
    {
        EnsureNotSealed();

        RegisterCore<TSource, TDestination>((serviceProvider, source) =>
        {
            ITypeConverter<TSource, TDestination> converter;

            if (serviceProvider != null)
            {
                converter = (ITypeConverter<TSource, TDestination>?)
                    serviceProvider.GetService(
                        typeof(ITypeConverter<TSource, TDestination>))
                    ?? throw new InvalidOperationException(
                        $"No service registered for converter " +
                        $"'{typeof(ITypeConverter<TSource, TDestination>).Name}'.");

                if (converter is not TConverter)
                {
                    throw new InvalidOperationException(
                        $"Expected converter '{typeof(TConverter).Name}', " +
                        $"but resolved '{converter.GetType().Name}'.");
                }
            }
            else
            {
                converter = (ITypeConverter<TSource, TDestination>?)
                    Activator.CreateInstance(typeof(TConverter))
                    ?? throw new InvalidOperationException(
                        $"Cannot create converter '{typeof(TConverter).Name}'. " +
                        "Register it with a service provider or provide a " +
                        "parameterless constructor.");
            }

            return converter.Convert((TSource)source);
        });
    }

    /// <summary>
    /// Registers a converter delegate for the specified source
    /// and destination type combination.
    /// </summary>
    /// <typeparam name="TSource">
    /// The source type handled by the converter.
    /// </typeparam>
    /// <typeparam name="TDestination">
    /// The destination type produced by the converter.
    /// </typeparam>
    /// <param name="converter">
    /// The delegate responsible for executing the conversion.
    /// It receives an optional service provider and the source value.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a converter for the same source and destination
    /// type combination has already been registered.
    /// </exception>
    private void RegisterCore<TSource, TDestination>(
        Func<IServiceProvider?, object, object?> converter)
    {
        var key = (typeof(TSource), typeof(TDestination));

        if (!_converters.TryAdd(key, converter))
        {
            throw new InvalidOperationException(
                $"A converter from '{typeof(TSource).Name}' to " +
                $"'{typeof(TDestination).Name}' is already registered.");
        }
    }

    /// <summary>
    /// Attempts to convert the specified source object to the
    /// requested destination type using a registered custom converter.
    /// </summary>
    /// <param name="serviceProvider">
    /// The optional service provider used to resolve converter
    /// implementations registered through dependency injection.
    /// </param>
    /// <param name="source">
    /// The source object to convert.
    /// </param>
    /// <param name="destinationType">
    /// The requested destination type.
    /// Nullable destination types are normalized to their underlying
    /// types when looking up registered converters.
    /// </param>
    /// <param name="result">
    /// When this method returns, contains the converted value if a
    /// matching converter was found; otherwise, null.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a matching converter was found
    /// and executed successfully; otherwise,
    /// <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Converter lookup uses the runtime type of the source object
    /// and the normalized destination type.
    ///
    /// If a matching converter is found, the converter is executed
    /// immediately using the supplied service provider.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the source object or destination type is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// May be thrown when the registered converter cannot be resolved
    /// or instantiated.
    /// </exception>
    public bool TryConvert(
        IServiceProvider? serviceProvider,
        object source,
        Type destinationType,
        out object? result)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destinationType);

        var targetType =
            Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        var key = (source.GetType(), targetType);

        if (_converters.TryGetValue(key, out var converter))
        {
            result = converter(serviceProvider, source);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Seals the converter registry to prevent further converter registrations.
    /// </summary>
    /// <remarks>
    /// Once sealed, the registry becomes immutable with respect to
    /// converter registration.
    /// Calling this method multiple times has no additional effect.
    /// </remarks>
    public void Seal()
    {
        _isSealed = true;
    }

    /// <summary>
    /// Ensures that the converter registry has not been sealed.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when attempting to register a converter after the
    /// registry has been sealed.
    /// </exception>
    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Type converter configuration is already initialized.");
        }
    }
}