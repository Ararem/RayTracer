namespace Ararem.RayTracer.Core;

/// <summary>A scene that contains objects, which can be rendered by a <see cref="Camera"/></summary>
/// <param name="Name">The name you wish to call your scene</param>
/// <param name="Camera">The camera that will be used to render the scene</param>
/// <param name="SceneObjects">Array containing all the objects in the scene</param>
/// <param name="Lights">Array containing all the light sources in the scene</param>
/// <param name="SkyBox">SkyBox (sky light) for the scene</param>
public sealed record Scene(string Name, Camera Camera, SceneObject[] SceneObjects, Light[] Lights, SkyBox SkyBox)
{
	/// <inheritdoc/>
	public override string ToString() => $"{Name} - {SceneObjects.Length} objects";
}