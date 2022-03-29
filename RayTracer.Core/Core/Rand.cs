using JetBrains.Annotations;
using System.Numerics;
using static RayTracer.Core.MathHelper;
using static System.MathF;
using static System.Numerics.Vector3;

namespace RayTracer.Core;
#pragma warning disable CA5394 //I don't care about security, I just want some random numbers
/// <summary>
///  A class for getting random numbers
/// </summary>
/// <remarks>Internally simply wraps a <see cref="Random"/></remarks>
[PublicAPI]
public static class Rand
{
	/// <summary>
	///  Returns a float in the range [0..1]
	/// </summary>
	/// <returns></returns>
	[MustUseReturnValue]
	public static float RandomFloat() => Random.Shared.NextSingle();

	/// <summary>
	///  Returns a random float in the specified range
	/// </summary>
	[MustUseReturnValue]
	public static float RandomFloat(float min, float max) => min + ((max - min) * Random.Shared.NextSingle());

	/// <summary>
	///  Returns a random integer in the specified range
	/// </summary>
	[MustUseReturnValue]
	public static int RandomInt(int min, int max) => Random.Shared.Next(min, max);

	/// <summary>
	///  Returns a float in the range [-1..1]
	/// </summary>
	[MustUseReturnValue]
	public static float RandomPlusMinusOne() => (Random.Shared.NextSingle() * 2) - 1;

	/// <summary>
	///  Returns a vector within a unit cube
	/// </summary>
	[MustUseReturnValue]
	public static Vector3 RandomInUnitCube() => new(RandomPlusMinusOne(), RandomPlusMinusOne(), RandomPlusMinusOne());

	/// <summary>
	///  Returns a vector on the surface of a unit sphere
	/// </summary>
	[MustUseReturnValue]
	public static Vector3 RandomOnUnitSphere() => Normalize(RandomInUnitCube());

	/// <summary>
	///  Returns a vector inside a unit sphere
	/// </summary>
	[MustUseReturnValue]
	public static Vector3 RandomInUnitSphere()
	{
		while (true)
		{
			Vector3 p = RandomInUnitCube();
			if (p.LengthSquared() >= 1) continue;
			return p;
		}
	}

	/// <summary>
	///  Returns a vector that has (X,Y) such that X and Y are within a circle of radius 1
	/// </summary>
	[MustUseReturnValue]
	public static Vector2 RandomInUnitCircle()
	{
		float theta = RandomFloat(0, 2 * PI);
		float r     = Sqrt(RandomFloat());
		(float x, float y) = SinCos(theta);
		return new Vector2(r * x, r * y);
	}

	/// <summary>
	///  Returns a random colour, where the output colour has RGB values in the range [0...1]
	/// </summary>
	[MustUseReturnValue]
	public static Colour RandomColour() => RandomColour(Colour.Black, Colour.White);

	/// <summary>
	///  Returns a random colour, where the output colour is somewhere between the RGB values of the <paramref name="min"/> and <paramref name="max"/> values
	/// </summary>
	/// <param name="min">Lowest RGB values allowed</param>
	/// <param name="max">Highest RGB values allowed</param>
	[MustUseReturnValue]
	public static Colour RandomColour(Colour min, Colour max) =>
			new(
					Lerp(min.R, max.R, RandomFloat()),
					Lerp(min.G, max.G, RandomFloat()),
					Lerp(min.B, max.B, RandomFloat())
			);
}