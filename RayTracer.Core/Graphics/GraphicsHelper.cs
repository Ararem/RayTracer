namespace RayTracer.Core.Graphics;

public static class GraphicsHelper
{
	// ReSharper disable once UnusedParameter.Global
	public static int Compress2DIndex(int x, int y, int width, int height) => x + (y * width);

	public static (int X, int Y) Decompress2DIndex(int i, int width, int height)
	{
		int y = i / width;
		int x = i % height;
		return (x, y);
	}
}