using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;

namespace RayTracer.Display;

//I had to modify this from the Spectre.Console one because it wouldn't let me use Rgb24, and wouldn't let me pass in the image myself

/// <summary>
///  Represents a renderable image.
/// </summary>
[PublicAPI]
public sealed class CustomImageRenderable : Renderable
{
	/// <summary>
	///  Initializes a new instance of the <see cref="CustomImageRenderable"/> class.
	/// </summary>
	/// <param name="image">The image for rendering</param>
	public CustomImageRenderable(Image<Rgb24> image)
	{
		Image = image;
	}

	/// <summary>
	///  Gets the maximum width allowed (in chars)
	/// </summary>
	public int? MaxConsoleWidth { get; set; }

	/// <summary>
	///  Gets or sets the <see cref="IResampler"/> that should
	///  be used when scaling the image. Defaults to bicubic sampling.
	/// </summary>
	public IResampler? Resampler { get; set; }

	/// <summary>
	///  Image that is to be rendered
	/// </summary>
	public Image<Rgb24> Image { get; }

	/// <inheritdoc/>
	protected override Measurement Measure(RenderContext context, int maxWidth)
	{
		//Since the pixel resolution is 2x the console, account for it
		int consoleWidth = MaxConsoleWidth ?? (Image.Width + 1) / 2;
		if (consoleWidth > maxWidth) return new Measurement(maxWidth, maxWidth);

		return new Measurement(consoleWidth, consoleWidth);
	}

	/// <inheritdoc/>
	protected override IEnumerable<Segment> Render(RenderContext context, int maxWidth)
	{
		Image<Rgb24>? image = Image;

		//Start of with the largest possible width, then shrink to constraints later
		int   conWidth = image.Width;
		float aspect   = (float)image.Width / image.Height;

		// Got a max width?
		if (MaxConsoleWidth != null) conWidth = MaxConsoleWidth.Value;
		// Do we exceed the max width when we take pixel width into account?
		if (conWidth > maxWidth) conWidth = maxWidth;

		//Find the height from the width and aspect ratio
		int conHeight = (int)(conWidth / aspect);

		// Need to rescale the pixel buffer?
		if ((conWidth != image.Width) || (conHeight != image.Height))
		{
			IResampler resampler = Resampler ?? KnownResamplers.Bicubic;
			image = image.Clone(); // Clone the original image
			image.Mutate(i => i.Resize(conWidth * 2, conHeight * 2, resampler));
		}

		//Now loop over the resized image. Since we have 2x vertical resolution, we skip every 2nd row
		string upperPixel = new('▀', 1);
		for (int y = 0; y < image.Height; y += 2)
		{
			for (int x = 0; x < image.Width; x++)
				yield return new Segment(
						upperPixel,
						new Style(
								new Color(image[x, y].R,     image[x, y].G,     image[x, y].B),
								new Color(image[x, y + 1].R, image[x, y + 1].G, image[x, y + 1].B)
						)
				);
			yield return Segment.LineBreak;
		}
	}
}