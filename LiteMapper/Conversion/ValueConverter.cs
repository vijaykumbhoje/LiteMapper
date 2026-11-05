using System.Globalization;

namespace LiteMapper.Conversion;

internal static class ValueConverter
{
    public static object? ConvertValue(object? value, Type destinationType)
    {
        ArgumentNullException.ThrowIfNull(destinationType);

        if (value == null)
        {
            // A null value cannot be assigned to a non-nullable value type.
            if (destinationType.IsValueType &&
                Nullable.GetUnderlyingType(destinationType) == null)
            {
                return null;
            }

            return null;
        }

        var targetType =
            Nullable.GetUnderlyingType(destinationType) ?? destinationType;

        var sourceType = value.GetType();

        if (targetType.IsAssignableFrom(sourceType))
            return value;

        // Handle enum destinations.
        if (targetType.IsEnum)
        {
            if (value is string enumText)
                return Enum.Parse(targetType, enumText, ignoreCase: true);

            var enumUnderlyingType = Enum.GetUnderlyingType(targetType);
            var numericValue = Convert.ChangeType(
                value, enumUnderlyingType, CultureInfo.InvariantCulture);

            return Enum.ToObject(targetType, numericValue!);
        }

        // Handle Guid destinations.
        if (targetType == typeof(Guid))
        {
            return Guid.Parse(value.ToString()!);
        }

        // Handle standard convertible types.
        if (value is IConvertible &&
            typeof(IConvertible).IsAssignableFrom(targetType))
        {
            return Convert.ChangeType(
                value, targetType, CultureInfo.InvariantCulture);
        }

        throw new InvalidOperationException(
            $"Cannot convert value of type '{sourceType.Name}' " +
            $"to '{destinationType.Name}'.");
    }
}