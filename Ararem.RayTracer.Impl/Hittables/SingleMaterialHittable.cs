using Ararem.RayTracer.Core;
using Ararem.RayTracer.Impl.Builtin;

namespace Ararem.RayTracer.Impl.Hittables;

/// <summary>Base type for a <see cref="Hittable"/> that wants to have a single material</summary>
/// <remarks>Use this if you're lazy like me <c>:P</c></remarks>
public abstract class SingleMaterialHittable : Hittable
{
	/// <summary>Material used to render this object instance</summary>
	public Material Material { get; init; } = BuiltinMaterials.DefaultDiffuseMaterial;

	/// <inheritdoc/>
	public override RenderJob Renderer
	{
		get => base.Renderer;
		set
		{
			Material.Renderer = value;
			base.Renderer     = value;
		}
	}
}