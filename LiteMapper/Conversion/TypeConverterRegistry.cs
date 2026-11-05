using System.Collections.Concurrent;

namespace LiteMapper.Conversion;

internal sealed class TypeConverterRegistry
{
    private readonly Dictionary<
        (Type Source, Type Destination),
        Func<IServiceProvider?, object, object?>> _converters = new();

    private bool _isSealed;

    public void Register<TSource, TDestination>(
        ITypeConverter<TSource, TDestination> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);
        EnsureNotSealed();

        RegisterCore<TSource, TDestination>(
            (_, source) => converter.Convert((TSource)source));
    }

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

    public void Seal()
    {
        _isSealed = true;
    }

    private void EnsureNotSealed()
    {
        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Type converter configuration is already initialized.");
        }
    }
}