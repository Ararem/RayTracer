using RayTracer.Core;
using RayTracer.Impl.Materials;
using System.Reflection;

namespace RayTracer.Impl;

public static class BuiltinMaterials
{
	/// <summary>
	/// The default diffuse material found in most game engines - half grey completely diffuse
	/// </summary>
	public static Material DefaultDiffuseMaterial => new StandardMaterial(Colour.HalfGrey, 1f);

	/// <summary>Gets all the builtin materials</summary>
	public static IEnumerable<Material> GetAll()
	{
		return typeof(BuiltinMaterials)
				.GetProperties(BindingFlags.Public | BindingFlags.Static)
				.Where(p => p.PropertyType == typeof(Material))
				.Select(p => (Material)p.GetValue(null)!);
	}
}