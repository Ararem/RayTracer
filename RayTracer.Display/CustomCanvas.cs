using Spectre.Console;
using Spectre.Console.Rendering;

namespace RayTracer.Display;

/// <summary>
///  Represents a renderable canvas.
/// </summary>
public sealed class CustomCanvas : Renderable
{
	private readonly Color?[,] pixels;

	/// <summary>
	///  Initializes a new instance of the <see cref="CustomCanvas"/> class.
	/// </summary>
	/// <param name="coxelsWide">The canvas width.</param>
	/// <param name="coxelsHigh">The canvas height.</param>
	public CustomCanvas(int coxelsWide, int coxelsHigh)
	{
		if (coxelsWide < 1) throw new ArgumentException("Must be > 1", nameof(coxelsWide));

		if (coxelsHigh < 1) throw new ArgumentException("Must be > 1", nameof(coxelsHigh));

		CoxelsWide = coxelsWide;
		CoxelsHigh = coxelsHigh;

		pixels = new Color?[CoxelsWide * 2, CoxelsHigh * 2];
	}

	/// <summary>
	///  Gets the width of the canvas.
	/// </summary>
	public int CoxelsWide { get; }

	/// <summary>
	///  Gets the height of the canvas.
	/// </summary>
	public int CoxelsHigh { get; }

	/// <summary>
	///  Gets or sets the render width of the canvas.
	/// </summary>
	public int? MaxConsoleWidth { get; set; }

	/// <summary>
	///  Gets or sets the pixel width.
	/// </summary>
	public int PixelWidth => 2;

	/// <summary>
	///  Sets a pixel with the specified color in the canvas at the specified location.
	/// </summary>
	/// <param name="x">The X coordinate for the pixel.</param>
	/// <param name="y">The Y coordinate for the pixel.</param>
	/// <param name="color">The pixel color.</param>
	/// <returns>The same <see cref="CustomCanvas"/> instance so that multiple calls can be chained.</returns>
	public void SetPixel(int x, int y, Color color)
	{
		pixels[x, y] = color;
	}

	/// <inheritdoc/>
	protected override Measurement Measure(RenderContext context, int maxWidth)
	{
		if (PixelWidth < 0) throw new InvalidOperationException("Pixel width must be greater than zero.");

		int width = MaxConsoleWidth ?? CoxelsWide;

		if (maxWidth < width * PixelWidth) return new Measurement(maxWidth, maxWidth);

		return new Measurement(width * PixelWidth, width * PixelWidth);
	}

	/// <inheritdoc/>
	protected override IEnumerable<Segment> Render(RenderContext context, int maxWidth)
	{
		if (PixelWidth < 0) throw new InvalidOperationException("Pixel width must be greater than zero.");

		string pixel  = new('▀', PixelWidth);
		int    width  = CoxelsWide;
		int    height = CoxelsHigh;

		// Got a max width?
		if (MaxConsoleWidth != null)
		{
			height = (int)((height * (float)MaxConsoleWidth.Value) / CoxelsWide);
			width  = MaxConsoleWidth.Value;
		}

		// Exceed the max width when we take pixel width into account?
		if (width * PixelWidth > maxWidth)
		{
			height = (int)(height * (maxWidth / (float)(width * PixelWidth)));
			width  = maxWidth / PixelWidth;

			// If it's not possible to scale the canvas sufficiently, it's too small to render.
			if (height == 0) yield break;
		}

		for (int y = 0; y < height; y += 2)
		{
			for (int x = 0; x < width; x += 2)
			{
				Color? color = pixels[x, y];
				if (color != null)
					yield return new Segment(pixel, new Style(background: color));
				else
					yield return new Segment(pixel);
			}

			yield return Segment.LineBreak;
		}
	}
}