using Spectre.Console;
using Spectre.Console.Rendering;

namespace RayTracer.Display;

public class NewCustomCanvas : Renderable
{
	private readonly Color[,] pixels;

	public NewCustomCanvas(int copixelsWide, int copixelsHigh)
	{
		CopixelsWide = copixelsWide;
		CopixelsHigh = copixelsHigh;
		pixels       = new Color[copixelsWide * 2, copixelsHigh * 2];
	}

	public int CopixelsWide { get; }
	public int CopixelsHigh { get; }

	/// <summary>
	///  Gets or sets the render width of the canvas.
	/// </summary>
	public int? MaxConsoleWidth { get; set; }

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
		int width = MaxConsoleWidth ?? CopixelsWide;

		if (maxWidth < width) return new Measurement(maxWidth, maxWidth);

		return new Measurement(width, width);
	}

	/// <inheritdoc/>
	protected override IEnumerable<Segment> Render(RenderContext context, int maxWidth)
	{
		string upperPixel = new('▀', 1);
		int    width      = CopixelsWide;
		int    height     = CopixelsHigh;

		// Got a max width?
		if (MaxConsoleWidth != null)
		{
			height = (int)((height * (float)MaxConsoleWidth.Value) / CopixelsWide);
			width  = MaxConsoleWidth.Value;
		}

		// Exceed the max width when we take pixel width into account?
		if (width > maxWidth)
		{
			height = (int)(height * (maxWidth / (float)width));
			width  = maxWidth;

			// If it's not possible to scale the canvas sufficiently, it's too small to render.
			if (height == 0) yield break;
		}

		for (int y = 0; y < height; y += 2)
		{
			for (int x = 0; x < width; x++)
			{
				Color? colorUpper = pixels[x, y];
				Color? colorLower = pixels[x, y + 1];
				yield return new Segment(upperPixel, new Style(colorUpper, colorLower));
			}

			yield return Segment.LineBreak;
		}
	}
}