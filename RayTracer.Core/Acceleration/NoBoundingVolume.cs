using System.Runtime.CompilerServices;

namespace RayTracer.Core.Acceleration;

/// <summary>
/// Implementation of <see cref="BoundingVolume"/> for objects that do not have bounding volumes, such as infinite objects
/// </summary>
public record NoBoundingVolume : BoundingVolume
{
	/// <inheritdoc />
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Hit(Ray ray, float kMin, float kMax) => true;
}