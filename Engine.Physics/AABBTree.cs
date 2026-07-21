using System.Runtime.CompilerServices;

namespace Engine.Physics;

/// <summary>
/// Represents an immutable 3D Axis-Aligned Bounding Box (AABB) used for broad-phase spatial partitioning and collision testing.
/// </summary>
public readonly struct AABB
{
    /// <summary>
    /// The minimum corner bounds of the box in world coordinates.
    /// </summary>
    public readonly Vector3 Min;

    /// <summary>
    /// The maximum corner bounds of the box in world coordinates.
    /// </summary>
    public readonly Vector3 Max;

    /// <summary>
    /// Initializes a new instance of the <see cref="AABB"/> struct with specified minimum and maximum bounds.
    /// </summary>
    /// <param name="min">The minimum corner vector.</param>
    /// <param name="max">The maximum corner vector.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB(Vector3 min, Vector3 max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Computes the union of two bounding boxes to produce a new <see cref="AABB"/> that tightly encapsulates both.
    /// Uses SIMD vector operations for hardware acceleration.
    /// </summary>
    /// <param name="a">The first bounding box.</param>
    /// <param name="b">The second bounding box.</param>
    /// <returns>A new <see cref="AABB"/> enclosing both input boxes.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AABB Union(in AABB a, in AABB b) => new(
        Vector3.Min(a.Min, b.Min),
        Vector3.Max(a.Max, b.Max)
    );

    /// <summary>
    /// Tests whether this bounding box overlaps another bounding box on all three axes.
    /// </summary>
    /// <param name="other">The bounding box to test against.</param>
    /// <returns><see langword="true"/> if the boxes intersect or touch; otherwise, <see langword="false"/>.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Overlaps(in AABB other) =>
        (Min.X <= other.Max.X && Max.X >= other.Min.X) &&
        (Min.Y <= other.Max.Y && Max.Y >= other.Min.Y) &&
        (Min.Z <= other.Max.Z && Max.Z >= other.Min.Z);

    /// <summary>
    /// Calculates the total surface area of the bounding box, used as the primary cost metric in Surface Area Heuristic (SAH) calculations.
    /// </summary>
    public float SurfaceArea
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            Vector3 extents = Max - Min;
            return 2.0f * (extents.X * extents.Y + extents.Y * extents.Z + extents.Z * extents.X);
        }
    }
}

/// <summary>
/// Represents a single node within the <see cref="DynamicAABBTree"/>.
/// Serves as either an internal branch node containing child links or a leaf node holding user data.
/// </summary>
public struct Node
{
    /// <summary>
    /// The bounding volume enclosing this node (and all descendant nodes if a branch).
    /// </summary>
    public AABB AABB;

    /// <summary>
    /// The array index of the parent node in the tree pool, or -1 if this node is the root.
    /// </summary>
    public int Parent;

    /// <summary>
    /// The array index of the left child node, or the next free node index if currently allocated to the free list.
    /// </summary>
    public int Child1;

    /// <summary>
    /// The array index of the right child node, or -1 if this node is a leaf node.
    /// </summary>
    public int Child2;

    /// <summary>
    /// An integer user payload ID (e.g., entity or collider index) attached to this leaf, or -1 if an internal branch node.
    /// </summary>
    public int UserData;

    /// <summary>
    /// Gets a value indicating whether this node is a leaf containing user data (<see langword="true"/>) or an internal branch (<see langword="false"/>).
    /// </summary>
    public readonly bool IsLeaf
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => Child2 == -1;
    }
}