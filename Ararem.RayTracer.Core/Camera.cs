using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace Ararem.RayTracer.Core;

/// <summary>Represents a camera that is used to render a <see cref="Scene"/></summary>
/// <remarks>This class handles the creation of view rays for each pixel, which renderers then use to create the scene image</remarks>
/// <param name="LensRadius">Radius of the artificially simulated lens (for DOF Blur)</param>
/// <param name="Horizontal">Unknown Vector</param>
/// <param name="Vertical">Unknown Vector</param>
/// <param name="LowerLeftCorner">Unknown Vector</param>
/// <param name="LookFrom">Where the camera looks from (it's position)</param>
/// <param name="U">Unknown Vector</param>
/// <param name="V">Unknown Vector</param>
/// <param name="LookTowards">Point the camera should point towards - this will be the focus of the camera</param>
/// <param name="UpVector">
///  Vector direction the camera considers 'upwards'. Use this to rotate the camera around the central view ray (lookFrom -> lookToward) - inverting this
///  is like rotating the camera upside-down
/// </param>
/// <param name="VerticalFov">Angle in degrees for the vertical field of view</param>
/// <param name="AspectRatio">Aspect ratio of the camera (width/height)</param>
/// <param name="LensRadius">Radius of the simulated lens. Larger values increase blur</param>
/// <param name="FocusDistance">Distance from the camera at which rays are perfectly in focus</param>
//TODO: Aspect ratio in the camera is really funky. I would prefer to store it (cached) in RenderOptions/RenderJob, and then multiply the uv coords before passing into GetRay()
public sealed record Camera(Vector3 LookFrom, Vector3 LookTowards, Vector3 LookDirection, Vector3 UpVector, float VerticalFov, float AspectRatio,
							float FocusDistance, //Properties that are nice for access later (these are passed into hte helper factory function)
							float LensRadius, Vector3 Horizontal, Vector3 Vertical, Vector3 LowerLeftCorner, Vector3 U, Vector3 V //Actually important properties for GetRay()
)
{
	/// <summary>Gets the world-space ray that corresponds to the given pixel's <paramref name="u"/><paramref name="v"/> coordinate</summary>
	/// <param name="u">The UV coordinate of the pixel</param>
	/// <param name="v">The UV coordinate of the pixel</param>
	/// <remarks>It is expected that the <paramref name="u"/><paramref name="v"/> coordinates are normalized to the range [0..1] for the X and Y values</remarks>
	public Ray GetRay(float u, float v)
	{
		Vector2 rand      = RandUtils.RandomInUnitCircle() * LensRadius;
		Vector3 offset    = (U * rand.X)                                          + (V * rand.Y);
		Vector3 origin    = LookFrom                                              + offset;
		Vector3 direction = (LowerLeftCorner + (u * Horizontal) + (v * Vertical)) - origin;
		return new Ray(origin, Normalize(direction));
	}

	/// <summary>Factory method for creating a camera</summary>
	/// <param name="lookFrom">Position the camera is located at - where it looks from</param>
	/// <param name="lookTowards">Point the camera should point towards - this will be the focus of the camera</param>
	/// <param name="upVector">
	///  Vector direction the camera considers 'upwards'. Use this to rotate the camera around the central view ray (lookFrom -> lookToward) - inverting this
	///  is like rotating the camera upside-down
	/// </param>
	/// <param name="verticalFov">Angle in degrees for the vertical field of view</param>
	/// <param name="aspectRatio">Aspect ratio of the camera (width/height)</param>
	/// <param name="lensRadius">Radius of the simulated lens. Larger values increase blur</param>
	/// <param name="focusDistance">Distance from the camera at which rays are perfectly in focus</param>
	/// <exception cref="ArithmeticException">
	///  Thrown when vector arithmetic returns invalid results because the camera's <paramref name="upVector"/> has the same direction as the forward vector.
	///  To fix this, simply modify the <paramref name="lookFrom"/>, <paramref name="lookTowards"/> or <paramref name="upVector"/> so that the
	///  <paramref name="upVector"/> points in a different direction to the direction of <paramref name="lookFrom"/> -> <paramref name="lookTowards"/>. So
	///  ensure that <c>Cross(UpVector, LookFrom - LookTowards) != Zero</c> before calling this constructor
	/// </exception>
	public static Camera Create(Vector3 lookFrom, Vector3 lookTowards, Vector3 upVector, float verticalFov, float aspectRatio, float lensRadius, float focusDistance)
	{
		//Have to ensure it's normalized because this is a direction-type vector
		upVector = Normalize(upVector);

		float theta          = verticalFov * (PI / 180f);
		float h              = Tan(theta / 2f);
		float viewportHeight = 2f          * h;
		float viewportWidth  = aspectRatio * viewportHeight;

		//Magic that lets us position and rotate the camera
		Vector3 lookDir             = Normalize(lookFrom - lookTowards);
		if (Cross(upVector, lookDir) == Zero)
			throw new ArithmeticException("Camera cannot point in the same direction as its 'up' vector (Cross(CameraUpVector, LookDirection) must != Zero)");
		Vector3 u = Normalize(Cross(upVector, lookDir));
		Vector3 v = Cross(lookDir, u);

		Vector3 horizontal      = viewportWidth  * u * focusDistance;
		Vector3 vertical        = viewportHeight * v * focusDistance;
		Vector3 lowerLeftCorner = lookFrom - (horizontal / 2) - (vertical / 2) - (focusDistance * lookDir);

		return new Camera(lookFrom, lookTowards, lookDir, upVector, verticalFov, aspectRatio, focusDistance, lensRadius, horizontal, vertical, lowerLeftCorner, u, v);
	}
}