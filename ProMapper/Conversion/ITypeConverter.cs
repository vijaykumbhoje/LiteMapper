namespace ProMapper.Conversion;

/// <summary>
/// Defines a contract for converting an object from one type
/// to another using a custom conversion implementation.
/// </summary>
/// <typeparam name="TSource">
/// The source type of the value to be converted.
/// This type is contravariant.
/// </typeparam>
/// <typeparam name="TDestination">
/// The destination type produced by the conversion.
/// This type is covariant.
/// </typeparam>
/// <remarks>
/// Implement this interface to provide custom conversion logic
/// for source and destination type combinations.
///
/// Custom type converters can be registered with
/// <see cref="Configuration.MapperConfiguration"/> and are used
/// by ProMapper during mapping operations when a matching
/// converter is available.
/// </remarks>
public interface ITypeConverter<in TSource, out TDestination>
{
    /// <summary>
    /// Converts a value from the specified source type
    /// to the destination type.
    /// </summary>
    /// <param name="source">
    /// The source value to convert.
    /// </param>
    /// <returns>
    /// The converted value of type <typeparamref name="TDestination"/>.
    /// </returns>
    TDestination Convert(TSource source);
}