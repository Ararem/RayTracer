using JetBrains.Annotations;

namespace Ararem.RayTracer.Core;

/// <summary>Helper class for math related functions</summary>
[PublicAPI]
public static class MathUtils
{
	/// <summary>Blends between two values</summary>
	/// <param name="a">First value to blend</param>
	/// <param name="b">Second value to blend</param>
	/// <param name="t">How much to blend. Should be [0..1] for results in the range [<paramref name="a"/>..<paramref name="b"/>]</param>
	[Pure]
	public static float Lerp(float a, float b, float t) => a + (t * (b - a));

	/// <summary>Finds how far between two values a given value is.</summary>
	[Pure]
	public static float InverseLerp(float a, float b, float val) => (val - a) / (b - a);

	/// <summary>Remaps a number from one range to another</summary>
	/// <param name="iMin">Input range minimum</param>
	/// <param name="iMax">Input range maximum</param>
	/// <param name="oMin">Output range minimum</param>
	/// <param name="oMax">Output range maximum</param>
	/// <param name="val">Value to remap</param>
	/// <returns></returns>
	[Pure]
	public static float Remap(float iMin, float iMax, float oMin, float oMax, float val)
		=> Lerp(oMin, oMax, InverseLerp(iMin, iMax, val));

	/// <summary>Compresses a 2d index into a 1d index. Useful for un-nesting loops</summary>
	[Pure]
	public static int Compress2DIndex(int x, int y, int width) => x + (y * width);

	/// <summary>Decompresses an index made by (<see cref="Compress2DIndex"/>)</summary>
	[Pure]
	public static (int X, int Y) Decompress2DIndex(int i, int width)
	{
		(int y, int x) = Math.DivRem(i, width);
		// int x = i % width;
		// int y = i / width;
		return (x, y);
	}

	/// <summary>
	///  Safe version of mod operator that doesn't throw. Might give some (slightly) weird outputs, namely when <paramref name="y"/> == 0 (output is
	///  <paramref name="x"/>)
	/// </summary>
	/// <returns>
	///  <c>x % y</c>
	/// </returns>
	public static long SafeMod(long x, long y)
	{
		if (x == y) return 0;
		if (y == 0) return x;
		return x % y;
	}
}