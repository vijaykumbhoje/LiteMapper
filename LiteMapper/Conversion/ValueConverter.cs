using System.Globalization;

namespace LiteMapper.Conversion;

/// <summary>
/// Provides built-in value conversion functionality for converting
/// values between compatible .NET types.
/// </summary>
/// <remarks>
/// This class handles common type conversions required during
/// object mapping operations.
///
/// Supported conversions include:
/// <list type="bullet">
/// <item>
/// <description>Direct assignment when the source value is compatible with the destination type.</description>
/// </item>
/// <item>
/// <description>Conversions to nullable and non-nullable destination types.</description>
/// </item>
/// <item>
/// <description>String and numeric conversions to enumeration types.</description>
/// </item>
/// <item>
/// <description>String conversions to <see cref="Guid"/>.</description>
/// </item>
/// <item>
/// <description>Standard conversions supported by <see cref="IConvertible"/>.</description>
/// </item>
/// </list>
///
/// Culture-invariant conversion is used for standard convertible types
/// to ensure consistent results regardless of the current application culture.
/// </remarks>
internal static class ValueConverter
{
    /// <summary>
    /// Converts the specified value to the requested destination type
    /// using the built-in conversion strategies.
    /// </summary>
    /// <param name="value">
    /// The source value to convert. Can be null.
    /// </param>
    /// <param name="destinationType">
    /// The destination type to which the source value should be converted.
    /// Nullable destination types are normalized to their underlying types
    /// when performing conversion.
    /// </param>
    /// <returns>
    /// The converted value compatible with the destination type,
    /// or null when the source value is null.
    /// </returns>
    /// <remarks>
    /// The conversion process follows these steps:
    /// <list type="number">
    /// <item>
    /// <description>Handles null source values.</description>
    /// </item>
    /// <item>
    /// <description>Normalizes nullable destination types to their underlying types.</description>
    /// </item>
    /// <item>
    /// <description>Returns the original value when it is already assignable to the destination type.</description>
    /// </item>
    /// <item>
    /// <description>Handles enumeration conversions.</description>
    /// </item>
    /// <item>
    /// <description>Handles GUID conversions.</description>
    /// </item>
    /// <item>
    /// <description>Performs standard conversions using <see cref="Convert.ChangeType(object, Type, IFormatProvider)"/>.</description>
    /// </item>
    /// </list>
    ///
    /// Standard conversions use <see cref="CultureInfo.InvariantCulture"/>
    /// to avoid dependencies on the current application culture.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when the destination type is null.
    /// </exception>
    /// <exception cref="FormatException">
    /// Thrown when the source value cannot be interpreted in the
    /// requested destination format, such as an invalid numeric
    /// or GUID string.
    /// </exception>
    /// <exception cref="InvalidCastException">
    /// Thrown when the requested conversion is not supported by
    /// the source or destination type.
    /// </exception>
    /// <exception cref="OverflowException">
    /// Thrown when the converted value exceeds the supported range
    /// of the destination type.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no supported conversion strategy exists between
    /// the source and destination types.
    /// </exception>
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