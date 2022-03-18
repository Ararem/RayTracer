namespace RayTracer.Core.Graphics;

public static class GraphicsHelper
{
	public static int Compress2DIndex(int x, int y, RenderOptions renderOptions) => x + (y * renderOptions.Width);

	public static (int X, int Y) Decompress2DIndex(int i, RenderOptions renderOptions)
	{
		int x = i % renderOptions.Width;
		int y = i / renderOptions.Width;
		return (x, y);
	}
}