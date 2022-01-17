using RayTracer.Core.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace RayTracer.Core.Graphics;

/// <summary>
///  Static class for rendering a <see cref="Scene"/>, using it's <see cref="Scene.Camera"/>.
/// </summary>
/// <remarks>
///  Uses the rays generated by the <see cref="Camera"/>, and objects in the <see cref="Scene"/> to create the output image
/// </remarks>
public static class Renderer
{
	/// <summary>
	///  Renders a specified <paramref name="scene"/>
	/// </summary>
	/// <param name="scene">The scene to render an image of</param>
	/// <param name="options">Options to modify how the image/scene is rendered</param>
	/// <returns>An <see cref="Image{TPixel}"/> of the scene</returns>
	public static Image<Rgb24> Render(Scene scene, RenderOptions options)
	{
		(_, Camera cam, SceneObject[] objects) = scene;
		Image<Rgb24> image = new(options.Width, options.Height);
		for (int x = 0; x < options.Width; x++)
		{
			for (int y = 0; y < options.Height; y++)
			{
				//Get the view ray from the camera
				Ray r = cam.GetRay((float)x / options.Width, (float)y / options.Height);

				float t   = 0.5f * (r.Direction.Y + 1);
				Rgb24 col = new(ToByte((1 - t) + (0.5f * t)), ToByte((1 - t) + (0.7f * t)), ToByte((1 - t) + (1f * t)));
				//Loop over the objects to see if we hit anything
				foreach (SceneObject sceneObject in objects)
					if (sceneObject.Hittable.Hit(r))
						col = new Rgb24(255, 0, 0);

				image[x, y] = col;
			}
		}

		return image;
	}

	/// <summary>
	///  Converts a float in the range [0..1] to a byte ([0..255])
	/// </summary>
	public static byte ToByte(this float f) => (byte)(255f * f);
}