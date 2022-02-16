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
public sealed class ImageRenderable : Renderable
{
	private static readonly IResampler DefaultResampler = KnownResamplers.Bicubic;

	/// <summary>
	///  Initializes a new instance of the <see cref="ImageRenderable"/> class.
	/// </summary>
	/// <param name="image">The image for rendering</param>
	public ImageRenderable(Image<Rgb24> image)
	{
		Image = image;
	}

	/// <summary>
	///  Gets the image width.
	/// </summary>
	public int Width => Image.Width;

	/// <summary>
	///  Gets the image height.
	/// </summary>
	public int Height => Image.Height;

	/// <summary>
	///  Gets or sets the render width of the canvas.
	/// </summary>
	public int? MaxWidth { get; set; }

	/// <summary>
	///  Gets or sets the render width of the canvas.
	/// </summary>
	public int PixelWidth { get; set; } = 2;

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
		if (PixelWidth < 0) throw new InvalidOperationException("Pixel width must be greater than zero.");

		int width = MaxWidth ?? Width;
		if (maxWidth < width * PixelWidth) return new Measurement(maxWidth, maxWidth);

		return new Measurement(width * PixelWidth, width * PixelWidth);
	}

	/// <inheritdoc/>
	protected override IEnumerable<Segment> Render(RenderContext context, int maxWidth)
	{
		Image<Rgb24>? image = Image;

		int width  = Width;
		int height = Height;

		// Got a max width?
		if (MaxWidth != null)
		{
			height = (int)((height * (float)MaxWidth.Value) / Width);
			width  = MaxWidth.Value;
		}

		// Exceed the max width when we take pixel width into account?
		if (width * PixelWidth > maxWidth)
		{
			height = (int)(height * (maxWidth / (float)(width * PixelWidth)));
			width  = maxWidth / PixelWidth;
		}

		// Need to rescale the pixel buffer?
		if ((width != Width) || (height != Height))
		{
			IResampler resampler = Resampler ?? DefaultResampler;
			image = image.Clone(); // Clone the original image
			image.Mutate(i => i.Resize(width, height, resampler));
		}

		Canvas canvas = new(width, height)
		{
				MaxWidth   = MaxWidth,
				PixelWidth = PixelWidth,
				Scale      = false
		};

		for (int y = 0; y < image.Height; y++)
			for (int x = 0; x < image.Width; x++)
				canvas.SetPixel(x, y, new Color(image[x, y].R, image[x, y].G, image[x, y].B));

		return ((IRenderable)canvas).Render(context, maxWidth);
	}
}