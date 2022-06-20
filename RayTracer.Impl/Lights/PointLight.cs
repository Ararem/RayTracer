using RayTracer.Core;
using System.Numerics;

namespace RayTracer.Impl.Lights;

/// <inheritdoc />
public sealed class PointLight : SimpleLightBase
{
	/// <inheritdoc />
	public override Vector3 ChooseIntersectTestPosition(HitRecord hit) => Position;
}