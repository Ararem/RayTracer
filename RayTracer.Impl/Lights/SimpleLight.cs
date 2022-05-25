using RayTracer.Core;
using System.Numerics;

namespace RayTracer.Impl.Lights;

//TODO: Cone/Spot lights
/// <summary>
/// Simple light that can be coloured, positioned and have it's brightness falloff adjusted. Supports overriding for diffuse lights that cover a volume
/// </summary>
public class SimpleLight : Light
{
	/// <summary>Colour of the emitted light</summary>
	public Colour Colour { get; init; } = Colour.White;

	/// <summary>The 3D position of the light in world-space</summary>
	public Vector3 Position { get; }

	/// <summary>How large of a radius the light should illuminate</summary>
	/// <remarks>
	///  The exact way this is used depends on what function is used to calculate attenuation (see <see cref="DistanceAttenuationFunc"/>). By convention however, the
	///  light <i>should</i> be significantly attenuated after this point to the point where it is not noticeable
	/// </remarks>
	public float Radius { get; init; } = 1f;

	// /// <summary>
	// /// Float parameter that controls how important it is for the
	// /// </summary>
	// public float SurfaceDirectionImportance { get; init; }


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
	public DistanceAttenuationDelegate DistanceAttenuationFunc { get; init; } = RealInverseSquareDistanceAttenuation();

	/// <summary>Delegate used to calculate how much the intensity of the light should be attenuated at a given <paramref name="distance"/></summary>
	public delegate float DistanceAttenuationDelegate(SimpleLight light, float distance);

#endregion

	/// <summary>
	/// Returns an position in world-space, that is used to check if there is an intersection between the hit and the returned point.
	/// </summary>
	/// <param name="hit">Information about the hit that will be checked. May be useful for biasing towards the closest point</param>
	public virtual Vector3 ChooseIntersectTestPosition(HitRecord hit) => Position;

	/// <inheritdoc />
	public override Colour CalculateLight(HitRecord hit)
	{
		//Choose a point to test for intersection
		//Only applicable to diffuse lights, but doesn't harm point lights
		Vector3 pointToCheck = ChooseIntersectTestPosition(hit);

		//Check intersection
		bool intersection = CheckIntersection(hit, pointToCheck, out Ray shadowRay);
		if(intersection) return Colour.Black;

		Colour colour    = Colour;

		//Account for how much the surface points towards our light
		float  dot       = Vector3.Dot(shadowRay.Direction, hit.Normal);
		if (dot < 0) dot = -dot;                                        //Backfaces give negative dot product
		// dot = MathUtils.Lerp(1, dot, SurfaceDirectionImportance);
		colour *= dot;

		//Also account for distance attenuation
		float dist      = Vector3.Distance(hit.WorldPoint, pointToCheck);
		float distScale = DistanceAttenuationFunc(this, dist);
		colour *= distScale;

		return colour;
	}
}