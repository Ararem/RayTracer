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
public abstract class SimpleLightBase : Light
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

	/// <summary>Returns a ray and two distance bounds, that are used to check if there is an intersection between the hit and the light source</summary>
	/// <param name="hit">Information about the hit that will be checked. May be useful for biasing towards the closest point</param>
	/// <remarks>Can be overridden for lights that cover a volume, e.g. area and diffuse lights</remarks>
	/// <returns>A tuple containing a <see cref="Ray"/> and two <c>k</c> values that are the (distance) bounds along the ray. If there is an intersection in the scene, and the <see cref="HitRecord.K"/> value is &gt; kMin and &lt; kMax, the hit is considered shadowed. The <c>kMax</c> value should be equal to the distance between the point on the hit object, and the closest intersection between the surface of the light source and the ray.</returns>
	[PublicAPI]
	protected abstract (Ray ray, float kMin, float kMax) GetShadowRayForHit(HitRecord hit);

	/// <inheritdoc/>
	public override Colour CalculateLight(HitRecord hit, out Ray shadowRay, bool calculateRawColour = false)
	{
		//Check intersection
		(shadowRay, float kMin, float kMax) = GetShadowRayForHit(hit);
		bool intersection = Renderer.AnyIntersectionFast(shadowRay, kMin, kMax);
		if (intersection) return Colour.Black;        //Another object blocks the light ray

		if (calculateRawColour) return Colour; //Skip calculating the attenuation things
		if (kMax > CutoffRadius) return Colour.Black; //Since kMax is the distance to the light, check if it's outside the radius of the light

		Colour colour = Colour;

		//Also account for distance attenuation
		float distScale      = DistanceAttenuationFunc(this, kMax / AttenuationRadius);
		distScale =  Max(distScale, 0); //Clamp in case the function goes below 0
		colour    *= distScale;

		return colour;
	}

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
	/// <param name="steepness">Steepness of the curve. Note that this should be <i>positive</i> despite the fact that the curve is an inverted logistics curve (it is translated negatively internally) </param>
	/// <param name="midpoint">Midpoint of the curve</param>
	/// <footer>
	///  <a href="https://www.desmos.com/calculator/knlg7ab2t0">Demo on desmos</a>
	/// </footer>
	[PublicAPI]
	public static DistanceAttenuationDelegate LogisticsCurveDistanceAttenuation(float midpoint = .5f, float steepness = 16f) => (_, normDist) => 1f / (1 + Pow(E, steepness * (normDist - midpoint)));

	/// <summary>Standard form of the logistics curve</summary>
	/// <param name="l">Maximum value of the curve</param>
	/// <param name="k">Steepness of the curve. Should be negative</param>
	/// <param name="x0">Midpoint of the curve</param>
	/// <footer>
	///  <a href="https://www.desmos.com/calculator/knlg7ab2t0">Demo on desmos</a>
	/// </footer>
	[PublicAPI]
	public static DistanceAttenuationDelegate LogisticsCurveDistanceAttenuation(float l, float k, float x0) => (_, normDist) => l / (1f + Pow(E, -k * (normDist - x0)));

	/// <summary>
	/// Attenuation decays exponentially with distance; <c>y=e^(-ax)</c>
	/// </summary>
	/// <param name="a">How fast the decay is. Higher values increase dropoff speed. Should be &gt;0</param>
	public static DistanceAttenuationDelegate ExponentialDecayDistanceAttenuation(float a) => (_, normDist) => Pow(E, a * -normDist);

	/// <inheritdoc cref="DistanceAttenuationDelegate"/>
	public DistanceAttenuationDelegate DistanceAttenuationFunc { get; init; } = ExponentialDecayDistanceAttenuation(5);

	/// <summary>Delegate used to calculate how much the intensity of the light should be attenuated at a given <paramref name="normalisedDistance"/></summary>
	/// <param name="light">Light object</param>
	/// <param name="normalisedDistance">Normalized <c>[0...1]</c> distance between the light and the point</param>
	public delegate float DistanceAttenuationDelegate(SimpleLightBase light, float normalisedDistance);

#endregion

	public static (Ray ray, float kMin, float kMax) DefaultGetShadowRayForHit(Vector3 hitPos, Vector3 lightPos)
	{
		Ray r = Ray.FromPoints(hitPos, lightPos); //The ray goes [hit object's point] ==> [point inside AABB]

		//I don't think the bounds matter too much
		const float kMin = 0.01f;
		float       kMax = Vector3.Distance(hitPos, lightPos) -kMin;

		return (r, kMin, kMax);
	}
}