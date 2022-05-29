using JetBrains.Annotations;
using RayTracer.Core;
using System.Numerics;
using static System.MathF;

namespace RayTracer.Impl.Lights;

//TODO: Cone/Spot lights
/// <summary>
///  Simple light that can be coloured, positioned and have it's brightness falloff adjusted. Supports overriding for diffuse lights that cover a
///  volume
/// </summary>
public class SimpleLight : Light
{
	/// <summary>Colour of the emitted light</summary>
	public Colour Colour { get; init; } = Colour.White;

	/// <summary>The 3D position of the light in world-space</summary>
	public Vector3 Position { get; init; }

	/// <summary>How large of a radius the light should illuminate</summary>
	/// <remarks>
	///  The exact way this is used depends on what function is used to calculate attenuation (see <see cref="DistanceAttenuationFunc"/>). By convention
	///  however, the light <i>should</i> be significantly attenuated after this point to the point where it is not noticeable. For certain functions (such as <see cref="LinearDistanceAttenuation"/>, it is necessary to set the <see cref="CutoffRadius"/> as well, since the function goes below zero)
	/// </remarks>
	public float AttenuationRadius { get; init; } = 1f;

	/// <summary>
	/// The radius at which the light does not illuminate anymore. After this distance, the light will be forcibly clamped to <see cref="RayTracer.Core.Colour.Black"/>. This will be necessary to set for <see cref="DistanceAttenuationFunc">attenuation delegates</see> that go below zero or only work in the range [0..<see cref="AttenuationRadius"/>]
	/// </summary>
	public float CutoffRadius { get; init; } = float.PositiveInfinity;

	/// <summary>Returns an position in world-space, that is used to check if there is an intersection between the hit and the returned point.</summary>
	/// <param name="hit">Information about the hit that will be checked. May be useful for biasing towards the closest point</param>
	[PublicAPI]
	public virtual Vector3 ChooseIntersectTestPosition(HitRecord hit) => Position;

	/// <inheritdoc/>
	public override Colour CalculateLight(HitRecord hit)
	{
		//Choose a point to test for intersection
		//Only applicable to diffuse lights, but doesn't harm point lights
		Vector3 pointToCheck = ChooseIntersectTestPosition(hit);

		//Check intersection
		bool intersection = CheckIntersection(hit, pointToCheck, out Ray shadowRay, out float distance);
		if (intersection) return Colour.Black;      //Another object blocks the light ray
		if (distance               > CutoffRadius) return Colour.Black; //Outside the radius of the light

		Colour colour = Colour;

		//Account for how much the surface points towards our light
		float dot        = Vector3.Dot(shadowRay.Direction, hit.Normal);
		if (dot < 0) dot = -dot; //Backfaces give negative dot product
		// dot = MathUtils.Lerp(1, dot, SurfaceDirectionImportance);
		colour *= dot;

		//Also account for distance attenuation
		float distScale      = DistanceAttenuationFunc(this, distance);
		distScale =  Max(distScale, 0); //Clamp in case the function goes below 0
		colour    *= distScale;

		return colour;
	}

	// /// <summary>
	// /// Float parameter that controls how important it is for the
	// /// </summary>
	// public float SurfaceDirectionImportance { get; init; }


#region Attenuation things

	/// <summary>Attenuation goes from [<c>0..1</c>] linearly for <c>d=[0..AttenuationRadius]</c></summary>
	[PublicAPI]
	public static DistanceAttenuationDelegate LinearDistanceAttenuation() => static (_, normDist) => 1 - normDist;

	/// <summary>Returns an attenuation delegate that drops off using a power.</summary>
	/// <param name="power">The power the function is raised to - controls the sharpness of the dropoff</param>
	/// <param name="stayHighInitially">
	///  Whether the function should initially stay high before sharply dropping off, or if it should drop off initially then
	///  stay relatively stable.
	/// </param>
	/// <footer>
	///  <a href="https://www.desmos.com/calculator/6ijnkp3k36">Demo on desmos</a>
	/// </footer>
	[PublicAPI]
	public static DistanceAttenuationDelegate PowerDistanceAttenuation(float power, bool stayHighInitially = false)
	{
		// ReSharper disable CommentTypo
		if (!stayHighInitially)
			return (_, normDist) => Pow(1 - normDist, power); //\left(1-x\right)^{z}
		else
			return (_, normDist) => 1 - Pow(normDist, power); //1-\left(x\right)^{z}
		// ReSharper restore CommentTypo
	}

	/// <summary>Simplified form of the logistics curve</summary>
	/// <param name="steepness">Steepness of the curve</param>
	/// <param name="midpoint">Midpoint of the curve</param>
	/// <footer>
	///  <a href="https://www.desmos.com/calculator/knlg7ab2t0">Demo on desmos</a>
	/// </footer>
	[PublicAPI]
	public static DistanceAttenuationDelegate LogisticsCurveDistanceAttenuation(float midpoint, float steepness) => (_, normDist) => 1f / (1 + Pow(E, steepness * (normDist - midpoint)));

	/// <summary>Standard form of the logistics curve</summary>
	/// <param name="l">Maximum value of the curve</param>
	/// <param name="k">Steepness of the curve</param>
	/// <param name="x0">Midpoint of the curve</param>
	/// <footer>
	///  <a href="https://www.desmos.com/calculator/knlg7ab2t0">Demo on desmos</a>
	/// </footer>
	[PublicAPI]
	public static DistanceAttenuationDelegate LogisticsCurveDistanceAttenuation(float l, float k, float x0) => (_, normDist) => l / (1f + Pow(E, -k * (normDist - x0)));

	/// <inheritdoc cref="DistanceAttenuationDelegate"/>
	public DistanceAttenuationDelegate DistanceAttenuationFunc { get; init; } = LinearDistanceAttenuation();

	/// <summary>Delegate used to calculate how much the intensity of the light should be attenuated at a given <paramref name="normalisedDistance"/></summary>
	/// <param name="light">Light object</param>
	/// <param name="normalisedDistance">Normalized <c>[0...1]</c> distance between the light and the point</param>
	public delegate float DistanceAttenuationDelegate(SimpleLight light, float normalisedDistance);

#endregion
}