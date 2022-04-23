using JetBrains.Annotations;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core;

/// <summary>
///  Represents a camera that is used to render a <see cref="Scene"/>
/// </summary>
/// <remarks>
///  This class handles the creation of view rays for each pixel, which renderers then use to create the scene image
/// </remarks>
[PublicAPI]
//TODO: .ToString() impl
public sealed class Camera
{
	/// <summary>
	///  Focus distance of the simulated lens (how far away rays will be perfectly in focus)
	/// </summary>
	public readonly float FocusDistance;

	/// <summary>
	///  The horizontal field-of-view angle (in degrees)
	/// </summary>
	public readonly float HorizontalFov;

	/// <summary>
	///  Radius of the simulated lens. Larger values increase blur
	/// </summary>
	public readonly float LensRadius;

	/// <summary>
	///  The vertical field-of-view angle (in degrees)
	/// </summary>
	public readonly float VerticalFov;

	/// <summary>
	///  Constructor for creating a camera
	/// </summary>
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
	///  <paramref name="upVector"/> points in a different direction to the direction of <paramref name="lookFrom"/> -> <paramref name="lookTowards"/>.
	///  So ensure that <c>Cross(UpVector, LookFrom - LookTowards) != Zero</c> before calling this constructor
	/// </exception>
	public Camera(Vector3 lookFrom, Vector3 lookTowards, Vector3 upVector, float verticalFov, float aspectRatio, float lensRadius, float focusDistance)
	{
		//Have to ensure it's normalized because this is a direction-type vector
		upVector      = Normalize(upVector);
		LookFrom      = lookFrom;
		LookTowards   = lookTowards;
		UpVector      = upVector;
		VerticalFov   = verticalFov;
		HorizontalFov = verticalFov / aspectRatio;
		FocusDistance = focusDistance;
		LensRadius    = lensRadius;

		float theta          = VerticalFov * (PI / 180f);
		float h              = Tan(theta / 2f);
		float viewportHeight = 2f          * h;
		float viewportWidth  = aspectRatio * viewportHeight;

		//Magic that lets us position and rotate the camera
		W = Normalize(LookFrom - LookTowards);
		if (Cross(UpVector, W) == Zero)
			throw new ArithmeticException("Camera cannot point in the same direction as its 'up' vector (Cross(CameraUpVector, LookDirection) must != Zero)");
		U = Normalize(Cross(UpVector, W));
		V = Cross(W, U);

		Horizontal      = viewportWidth  * U * FocusDistance;
		Vertical        = viewportHeight * V * FocusDistance;
		LowerLeftCorner = LookFrom - (Horizontal / 2) - (Vertical / 2) - (FocusDistance * W);
	}

	/// <summary>
	///  Gets the world-space ray that corresponds to the given pixel's <paramref name="u"/><paramref name="v"/> coordinate
	/// </summary>
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

#region Internal View-Ray Vectors

	/// <summary>
	///  Unknown vector
	/// </summary>
	public readonly Vector3 Horizontal;

	/// <summary>
	///  Where the camera is located in the world
	/// </summary>
	public readonly Vector3 LookFrom;

	/// <summary>
	///  The point that the camera looks at (where it is facing)
	/// </summary>
	public readonly Vector3 LookTowards;

	/// <summary>
	///  The world-space position of the lowermost, leftmost pixel
	/// </summary>
	public readonly Vector3 LowerLeftCorner;

	/// <summary>
	///  <para>
	///   The world-space direction to move in when increasing the X position in pixel-space.
	///  </para>
	///  <para>
	///   When increasing the X (horizontal) coordinate for a pixel (such as moving left to to right on the screen), this is the direction the view ray
	///   should move, in world space
	///  </para>
	/// </summary>
	public readonly Vector3 U;

	/// <summary>
	///  The vector that is considered 'upwards' to the camera
	/// </summary>
	public readonly Vector3 UpVector;

	/// <summary>
	///  The direction the view ray moves in when increasing a pixel's Y coordinate.
	/// </summary>
	public readonly Vector3 V;

	/// <summary>
	///  Unknown vector
	/// </summary>
	public readonly Vector3 Vertical;

	/// <summary>
	///  The normalized 'forward' direction. Essentially which direction the camera is facing
	/// </summary>
	public readonly Vector3 W;

#endregion
}