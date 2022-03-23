using RayTracer.Core.Graphics;

namespace RayTracer.Core;

public class MathHelper
{
	/// <summary>
	///  Blends between two values
	/// </summary>
	/// <param name="a">First value to blend</param>
	/// <param name="b">Second value to blend</param>
	/// <param name="t">How much to blend. Should be [0..1] for results in the range [<paramref name="a"/>..<paramref name="b"/>]</param>
	public static float Lerp(float a, float b, float t) => ((1f - t) * a) + (b * t);

	/// <summary>
	///  Finds how far between two values a given value is.
	/// </summary>
	public static float InverseLerp(float a, float b, float val) => (val - a) / (b - a);

	/// <summary>
	///  Remaps a number from one range to another
	/// </summary>
	/// <param name="iMin">Input range minimum</param>
	/// <param name="iMax">Input range maximum</param>
	/// <param name="oMin">Output range minimum</param>
	/// <param name="oMax">Output range maximum</param>
	/// <param name="val">Value to remap</param>
	/// <returns></returns>
	public static float Remap(float iMin, float iMax, float oMin, float oMax, float val)
		=> Lerp(oMin, oMax, InverseLerp(iMin, iMax, val));

	public static int Compress2DIndex(int x, int y, int width) => x + (y * width);

	public static (int X, int Y) Decompress2DIndex(int i, int width)
	{
		(int y, int x) = Math.DivRem(i, width);
		// int x = i % width;
		// int y = i / width;
		return (x, y);
	}
}