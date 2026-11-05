namespace LiteMapper.Conversion;

internal sealed class TypeConverterRegistry
{
    private readonly Dictionary<
        (Type Source, Type Destination),
        Func<object, object?>> _converters = new();

    private bool _isSealed;

    public void Register<TSource, TDestination>(
        ITypeConverter<TSource, TDestination> converter)
    {
        ArgumentNullException.ThrowIfNull(converter);

        if (_isSealed)
        {
            throw new InvalidOperationException(
                "Type converter configuration is already initialized.");
        }

        var key = (typeof(TSource), typeof(TDestination));

        if (!_converters.TryAdd(
            key,
            source => converter.Convert((TSource)source)))
        {
            throw new InvalidOperationException(
                $"A converter from '{typeof(TSource).Name}' to " +
                $"'{typeof(TDestination).Name}' is already registered.");
        }
    }

    public bool TryConvert(
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
            result = converter(source);
            return true;
        }

        result = null;
        return false;
    }

    public void Seal()
    {
        _isSealed = true;
    }
}