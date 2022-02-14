using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;

namespace RayTracer.Core.Scenes;

/// <summary>
///  An object that is present in a <see cref="Scene"/>.
/// </summary>
/// <param name="Name">The name of this object</param>
/// <param name="Hittable">The mesh used for calculating intersections with this object (it's geometry)</param>
public sealed record SceneObject(
		string       Name,
		Hittable     Hittable,
		MaterialBase Material
)
{
	/// <inheritdoc/>
	public override string ToString() => $"'{Name}' {{Shape: {Hittable}, Material: {Material}}})";
}