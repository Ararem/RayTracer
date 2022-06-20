using RayTracer.Core;
using System.Numerics;

namespace RayTracer.Impl.Lights;

/// <inheritdoc cref="SimpleLightBase"/>
public class DiffuseSphereLight : SimpleLightBase
{
	/// <summary>
	/// How large of an area around the light's position should be considered as a valid point on the light's surface
	/// </summary>
	public float DiffusionRadius { get; init; }

	/// <inheritdoc />
	public override Vector3 ChooseIntersectTestPosition(HitRecord hit) => Position + (RandUtils.RandomInUnitSphere() * DiffusionRadius);
}