using RayTracer.Core.Environment;

namespace RayTracer.Core;

/// <summary>
///  A scene that contains objects, which can be rendered by a <see cref="Camera"/>
/// </summary>
/// <param name="Name"></param>
/// <param name="Objects"></param>
public record Scene(string Name, Camera Camera, SceneObject[] Objects, SkyBox SkyBox)
{
	/// <inheritdoc/>
	public override string ToString() => $"{Name} - {Objects.Length} objects";
}