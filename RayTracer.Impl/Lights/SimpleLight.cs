using RayTracer.Core;
using System.Numerics;

namespace RayTracer.Impl.Lights;

//TODO: Cone/Spot lights
public class SimpleLight : Light
{
	/// <summary>Colour of the emitted light</summary>
	public Colour Colour { get; }

	/// <summary>The 3D position of the light in world-space</summary>
	public Vector3 Position { get; }

	/// <summary>How large of a radius the light should illuminate</summary>
	/// <remarks>
	///  The exact way this is used depends on what function is used to calculate attenuation (see <see cref="DistanceAttenuationFunc"/>). By convention however, the
	///  light <i>should</i> be significantly attenuated after this point to the point where it is not noticeable
	/// </remarks>
	public float Radius { get; }


#region Attenuation things

	/// <summary>Attenuation goes from [<c>0..1</c>] linearly for <c>d=[0..Radius]</c></summary>
	public static DistanceAttenuationDelegate LinearDistanceAttenuation() => static delegate(SimpleLight light, float distance)
	{
		//Simple linear y=mx+c curve going through (0,1) and (Radius, 0)
		float attenuation = 1 - (distance / light.Radius);
		attenuation = MathF.Max(attenuation, 0); //Make sure it stays above 0 so we don't get -ve light
		return attenuation;
	};

	/// <summary>Inverse square <c>y = a/([x+b]^2 + c)</c>. Not very realistic but easier to make it look nice</summary>
	/// <footer>
	///  <a href="https://www.desmos.com/calculator/tewo922hrz">See desmos graph for visualisation</a> <br/>
	///  <a href="https://www.desmos.com/calculator/tk2nat0ywx">Alternate graph with regression</a>
	/// </footer>
	public static DistanceAttenuationDelegate InverseSquareDistanceAttenuation(float a, float b, float c = 0f)
	{
		if (a < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(a), a, "`a` must be > 0");
		}

		if (b < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(b), b, "`b` must be > 0");
		}

		if (c < 0)
		{
			throw new ArgumentOutOfRangeException(nameof(c), c, "`c` must be > 0");
		}

		return (_, distance) => a / (MathF.Pow(distance + b, 2) + c);
	}

	/// <summary>Physically accurate attenuation that follows 1/(x^2). Will result in extreme brightness very close to the light (small distance) as the function approaches infinity</summary>
	/// <returns></returns>
	public static DistanceAttenuationDelegate RealInverseSquareDistanceAttenuation() => static (_, distance) => 1f / (distance * distance);

	/// <inheritdoc cref="DistanceAttenuationDelegate"/>
	public DistanceAttenuationDelegate DistanceAttenuationFunc { get; }

	/// <summary>Delegate used to calculate how much the intensity of the light should be attenuated at a given <paramref name="distance"/></summary>
	public delegate float DistanceAttenuationDelegate(SimpleLight light, float distance);

#endregion
}