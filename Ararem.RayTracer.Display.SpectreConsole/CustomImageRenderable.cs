using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Transforms;
using Spectre.Console;
using Spectre.Console.Rendering;
using Color = Spectre.Console.Color;

namespace RayTracer.Display.SpectreConsole;

//I had to modify this from the Spectre.Console one because it wouldn't let me use Rgb24, and wouldn't let me pass in the image myself

/// <summary>Represents a renderable image.</summary>
[PublicAPI]
public sealed class CustomImageRenderable : Renderable
{
	/// <summary>Initializes a new instance of the <see cref="CustomImageRenderable"/> class.</summary>
	/// <param name="image">The image for rendering</param>
	public CustomImageRenderable(Image<Rgb24> image)
	{
		Image = image;
	}

	/// <summary>Gets the maximum width allowed (in chars)</summary>
	public int? MaxConsoleWidth { get; set; }

	/// <summary>Gets or sets the <see cref="IResampler"/> that should be used when scaling the image. Defaults to bicubic sampling.</summary>
	public IResampler? Resampler { get; set; }

	/// <summary>Image that is to be rendered</summary>
	public Image<Rgb24> Image { get; }

	/// <inheritdoc/>
	protected override Measurement Measure(RenderContext context, int maxWidth)
	{
		//This don't work :(
		// (int w, int h) = CalcConSize(maxWidth);
		// return new Measurement(w, h);

		//Since the pixel resolution is 2x the console, account for it
		int consoleWidth = MaxConsoleWidth ?? (Image.Width + 1) / 2; //+1 Because if the pixel count is odd we round up
		if (consoleWidth > maxWidth) return new Measurement(maxWidth, maxWidth);

		return new Measurement(consoleWidth * 2, consoleWidth * 2);
	}

	[Pure]
	private (int width, int height) CalcConSize(int maxAllowedWidth)
	{
		//Start of with the largest possible width, then shrink to constraints later
		int   conWidth = (Image.Width + 1)  / 2;
		float aspect   = (float)Image.Width / Image.Height;

		// Got a max width?
		if (MaxConsoleWidth != null) conWidth = Math.Min(conWidth, MaxConsoleWidth.Value);
		// Do we exceed the max width when we take pixel width into account?
		conWidth = Math.Min(conWidth, maxAllowedWidth);

		//Find the height from the width and aspect ratio
		int conHeight = (int)Math.Floor(conWidth / aspect);

		return (conWidth, conHeight);
	}

	/// <inheritdoc/>
	protected override IEnumerable<Segment> Render(RenderContext context, int maxAllowedWidth)
	{
		Image<Rgb24> image = Image;

		(int conWidth, int conHeight) = CalcConSize(maxAllowedWidth);

		// Need to rescale the pixel buffer?
		if ((conWidth != image.Width) || (conHeight != image.Height))
		{
			IResampler resampler = Resampler ?? KnownResamplers.Bicubic!;
			image = image.Clone(); // Clone the original image
			image.Mutate(i => i.Resize(conWidth * 2, conHeight * 2, resampler));
		}

		//Now loop over the resized image. Since we have 2x vertical resolution, we skip every 2nd row
		for (int y = 0; y < image.Height; y += 2)
		{
			for (int x = 0; x < image.Width; x++)
				yield return new Segment(
						#if false //You can change if you use the upper or lower blocks, because sometimes one looks better than the other
						"▀",
						new Style(
								new Color(image[x, y].R,     image[x, y].G,     image[x, y].B),
								new Color(image[x, y + 1].R, image[x, y + 1].G, image[x, y + 1].B)
						)
						#else
						"▄",
						new Style(
								new Color(image[x, y + 1].R, image[x, y + 1].G, image[x, y + 1].B),
								new Color(image[x, y].R,     image[x, y].G,     image[x, y].B)
						)
						#endif
				);
			yield return Segment.LineBreak;
		}
	}
}