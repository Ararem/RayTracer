using RayTracer.Core.Graphics;

namespace RayTracer.Core.Scenes;

/// <summary>
/// A scene that contains objects, which can be rendered by a <see cref="Camera"/>
/// </summary>
/// <param name="Name"></param>
/// <param name="Objects"></param>
public record Scene(string Name, Camera Camera, SceneObject[] Objects)
{
	/// <inheritdoc />
	public override string ToString() => $"{Name} - {Objects.Length} objects";
}