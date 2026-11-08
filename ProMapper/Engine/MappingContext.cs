using System.Runtime.CompilerServices;

namespace ProMapper.Engine;

/// <summary>
/// Maintains the execution context for an individual mapping operation,
/// including mapped object tracking and recursive mapping depth management.
/// </summary>
/// <remarks>
/// The mapping context helps ProMapper handle circular object references
/// and prevent excessively deep recursive mapping operations.
///
/// Mapped objects are tracked using their source object reference and
/// destination type combination. This allows previously mapped objects
/// to be reused instead of creating duplicate destination instances.
///
/// Each mapping operation should maintain its own mapping context.
/// </remarks>
internal sealed class MappingContext
{
    private readonly Dictionary<MappingKey, object> _mappedObjects =
        new(new MappingKeyComparer());

    /// <summary>
    /// Gets the current recursive mapping depth.
    /// </summary>
    /// <value>
    /// The number of active mapping operations in the current recursion chain.
    /// </value>
    public int CurrentDepth { get; private set; }

    /// <summary>
    /// Gets the maximum permitted recursive mapping depth.
    /// </summary>
    /// <value>
    /// The maximum mapping depth, which defaults to 64.
    /// </value>
    /// <remarks>
    /// Mapping operations exceeding this depth limit throw an
    /// <see cref="InvalidOperationException"/>.
    /// </remarks>
    public int MaxDepth { get; } = 64;

    /// <summary>
    /// Attempts to retrieve an existing destination object previously
    /// mapped from the specified source object.
    /// </summary>
    /// <param name="source">
    /// The source object whose mapped destination is being requested.
    /// </param>
    /// <param name="destinationType">
    /// The destination type associated with the previous mapping operation.
    /// </param>
    /// <param name="destination">
    /// When this method returns, contains the previously mapped destination
    /// object if found; otherwise, null.
    /// </param>
    /// <returns>
    /// <see langword="true"/> if a previously mapped destination object
    /// exists for the specified source reference and destination type;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    /// <remarks>
    /// Object lookup uses reference equality for source objects rather
    /// than their value-based equality implementation.
    /// </remarks>
    public bool TryGetMappedObject(
        object source,
        Type destinationType,
        out object? destination)
    {
        return _mappedObjects.TryGetValue(
            new MappingKey(source, destinationType),
            out destination);
    }

    /// <summary>
    /// Registers a source and destination object pair in the mapping context.
    /// </summary>
    /// <param name="source">
    /// The original source object.
    /// </param>
    /// <param name="destinationType">
    /// The destination type associated with the mapping.
    /// </param>
    /// <param name="destination">
    /// The destination object created for the source object.
    /// </param>
    /// <remarks>
    /// Registered object pairs can be retrieved later to preserve
    /// object references and handle circular object relationships
    /// during recursive mapping operations.
    ///
    /// Registering an existing key replaces its previously stored
    /// destination object.
    /// </remarks>
    public void Register(
        object source,
        Type destinationType,
        object destination)
    {
        _mappedObjects[new MappingKey(source, destinationType)] = destination;
    }

    /// <summary>
    /// Enters a new recursive mapping operation and increments
    /// the current mapping depth.
    /// </summary>
    /// <remarks>
    /// The current depth is checked against <see cref="MaxDepth"/>.
    /// If the maximum depth is exceeded, the depth counter is restored
    /// before an exception is thrown.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the current mapping depth exceeds the maximum
    /// permitted mapping depth.
    /// </exception>
    public void Enter()
    {
        CurrentDepth++;

        if (CurrentDepth > MaxDepth)
        {
            CurrentDepth--;

            throw new InvalidOperationException(
                $"Maximum mapping depth of {MaxDepth} was exceeded.");
        }
    }

    /// <summary>
    /// Exits the current recursive mapping operation and decrements
    /// the current mapping depth.
    /// </summary>
    /// <remarks>
    /// This method should be called after a successful
    /// <see cref="Enter"/> operation, typically within a finally block
    /// to ensure that the depth counter is restored even when
    /// a mapping operation fails.
    /// </remarks>
    public void Exit()
    {
        CurrentDepth--;
    }

    /// <summary>
    /// Represents a unique mapping key consisting of a source object
    /// reference and its associated destination type.
    /// </summary>
    /// <param name="Source">
    /// The source object reference.
    /// </param>
    /// <param name="DestinationType">
    /// The destination type associated with the mapping.
    /// </param>
    private readonly record struct MappingKey(
        object Source,
        Type DestinationType);

    /// <summary>
    /// Provides reference-based equality comparison for mapping keys.
    /// </summary>
    /// <remarks>
    /// Source objects are compared using reference equality rather than
    /// their overridden equality implementations.
    ///
    /// Destination types are compared using standard type equality.
    /// Hash codes are generated using the source object's runtime
    /// identity hash code and the destination type.
    /// </remarks>
    private sealed class MappingKeyComparer : IEqualityComparer<MappingKey>
    {
        /// <summary>
        /// Determines whether two mapping keys represent the same
        /// source object reference and destination type.
        /// </summary>
        /// <param name="x">The first mapping key.</param>
        /// <param name="y">The second mapping key.</param>
        /// <returns>
        /// <see langword="true"/> if both keys reference the same source
        /// object and destination type; otherwise, <see langword="false"/>.
        /// </returns>
        public bool Equals(MappingKey x, MappingKey y)
        {
            return ReferenceEquals(x.Source, y.Source) &&
                   x.DestinationType == y.DestinationType;
        }

        /// <summary>
        /// Returns the hash code for the specified mapping key.
        /// </summary>
        /// <param name="obj">
        /// The mapping key whose hash code is required.
        /// </param>
        /// <returns>
        /// A hash code based on the source object's runtime identity
        /// and destination type.
        /// </returns>
        public int GetHashCode(MappingKey obj)
        {
            return HashCode.Combine(
                RuntimeHelpers.GetHashCode(obj.Source),
                obj.DestinationType);
        }
    }
}