using JetBrains.Annotations;
using System.Numerics;

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
	public static float RandomFloat() => Random.Shared.NextSingle();

	/// <summary>
	///  Returns a random float in the specified range
	/// </summary>
	public static float RandomFloat(float min, float max) => min + ((max - min) * Random.Shared.NextSingle());

	/// <summary>
	///  Returns a random integer in the specified range
	/// </summary>
	public static int RandomInt(int min, int max) => Random.Shared.Next(min, max);

	/// <summary>
	///  Returns a float in the range [-1..1]
	/// </summary>
	public static float RandomPlusMinusOne() => (Random.Shared.NextSingle() * 2) - 1;

	/// <summary>
	///  Returns a vector within a unit cube
	/// </summary>
	public static Vector3 RandomInUnitCube() => new(RandomPlusMinusOne(), RandomPlusMinusOne(), RandomPlusMinusOne());

	/// <summary>
	///  Returns a vector on the surface of a unit sphere
	/// </summary>
	public static Vector3 RandomOnUnitSphere() => Vector3.Normalize(RandomInUnitCube());

	/// <summary>
	///  Returns a vector inside a unit sphere
	/// </summary>
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
	public static Vector2 RandomInUnitCircle()
	{
		while (true)
		{
			Vector2 p = new(RandomPlusMinusOne(), RandomPlusMinusOne());
			if (p.LengthSquared() >= 1) continue;
			return p;
		}
	}
}