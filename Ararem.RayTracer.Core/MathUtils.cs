using JetBrains.Annotations;
using System.Numerics;

namespace Ararem.RayTracer.Core;

/// <summary>Helper class for math related functions</summary>
[PublicAPI]
public static class MathUtils
{
	//TODO: Fix with new INumeric interfaces
	/// <summary>Blends between two values</summary>
	/// <param name="a">First value to blend</param>
	/// <param name="b">Second value to blend</param>
	/// <param name="t">How much to blend. Should be [0..1] for results in the range [<paramref name="a"/>..<paramref name="b"/>]</param>
	[Pure]
	public static T Lerp<T>(T a, T b, T t) where T : IFloatingPoint<T> => a + (t * (b - a));

	/// <summary>Finds how far between two values a given value is.</summary>
	[Pure]
	public static T InverseLerp<T>(T a, T b, T val) where T : IFloatingPoint<T> => (val - a) / (b - a);

	/// <summary>Remaps a number from one range to another</summary>
	/// <param name="iMin">Input range minimum</param>
	/// <param name="iMax">Input range maximum</param>
	/// <param name="oMin">Output range minimum</param>
	/// <param name="oMax">Output range maximum</param>
	/// <param name="val">Value to remap</param>
	/// <returns></returns>
	[Pure]
	public static T Remap<T>(T iMin, T iMax, T oMin, T oMax, T val) where T : IFloatingPoint<T> => Lerp(oMin, oMax, InverseLerp(iMin, iMax, val));

	/// <summary>Compresses a 2d index into a 1d index. Useful for un-nesting loops</summary>
	[Pure]
	public static T Compress2DIndex<T>(T x, T y, T width) where T : IBinaryInteger<T> => x + (y * width);

	/// <summary>Decompresses an index made by (<see cref="Compress2DIndex{T}"/>)</summary>
	[Pure]
	public static (T X, T Y) Decompress2DIndex<T>(T i, T width) where T : IBinaryInteger<T>
	{
		T x = i % width;
		T y = i / width;
		return (x, y);
	}

	/// <summary>
	///  Safe version of mod operator that doesn't throw. Might give some (slightly) weird outputs, namely when <paramref name="y"/> == 0 (output is
	///  <paramref name="x"/>)
	/// </summary>
	/// <returns>
	///  <c>x % y</c>
	/// </returns>
	[Pure]
	public static T SafeMod<T>(T x, T y) where T : INumber<T>
	{
		if (x == y) return T.Zero;
		if (y == T.Zero) return x;
		return x % y;
	}
}