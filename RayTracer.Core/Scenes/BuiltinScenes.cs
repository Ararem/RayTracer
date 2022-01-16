using RayTracer.Core.Graphics;

namespace RayTracer.Core.Scenes;

/// <summary>
/// Static class that contains a list of pre-made scenes
/// </summary>
public static class BuiltinScenes
{
	/// <summary>
	/// Simple scene with a single sphere at (0, 0, 0)
	/// </summary>
	public static readonly Scene Sphere = new("Sphere Scene", new Camera(),new SceneObject[0]);

	/// <summary>
	/// Simple scene with two spheres at (+-1, 0, 0)
	/// </summary>
	public static readonly Scene TwoSpheres = new("Two Spheres", new Camera(),new SceneObject[0]);
}