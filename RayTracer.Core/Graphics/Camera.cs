using RayTracer.Core.Scenes;
using System.Numerics;
using static System.Numerics.Vector3;
using static System.MathF;

namespace RayTracer.Core.Graphics;

/// <summary>
///  Represents a camera that is used to render a <see cref="Scene"/>
/// </summary>
/// <remarks>
///  This class handles the creation of view rays for each pixel, which the <see cref="Renderer"/> then uses to create the scene image
/// </remarks>
public sealed class Camera
{
	/// <summary>
	///  How far away from the camera should the optimal focus point be (affects blur)
	/// </summary>
	public readonly float FocusDistance;

	/// <summary>
	///  The horizontal field-of-view angle (in degrees)
	/// </summary>
	public readonly float HorizontalFov;

	/// <summary>
	///  How large the simulated lens should be (affects blur)
	/// </summary>
	/// <seealso cref="FocusDistance"/>
	public readonly float LensRadius;

	/// <summary>
	///  The vertical field-of-view angle (in degrees)
	/// </summary>
	public readonly float VerticalFov;


	public Camera(Vector3 lookFrom, Vector3 lookTowards, Vector3 upVector, float lensRadius, float verticalFov, float aspectRatio, float focusDistance)
	{
		LookFrom      = lookFrom;
		LookTowards   = lookTowards;
		UpVector      = upVector;
		LensRadius    = lensRadius;
		VerticalFov   = verticalFov;
		HorizontalFov = verticalFov * aspectRatio;
		FocusDistance = focusDistance;

		float theta          = VerticalFov * (PI / 180f);
		float h              = Tan(theta / 2f);
		float viewportHeight = 2f * h;
		float viewportWidth  = aspectRatio * viewportHeight;

		//Magic that lets us position and rotate the camera
		W = Normalize(LookFrom - LookTowards);
		if (Cross(UpVector, W) == Zero)
			throw new ArithmeticException("Camera cannot point in the same direction as its 'up' vector (Cross(CameraUpVector, LookDirection) must != Zero)");
		U = Normalize(Cross(UpVector, W));
		V = Cross(W, U);

		Horizontal      = FocusDistance * viewportWidth * U;
		Vertical        = FocusDistance * viewportHeight * V;
		LowerLeftCorner = LookFrom - (Horizontal / 2) - (Vertical / 2) - (FocusDistance * W);
	}

	/// <summary>
	///  Gets the world-space ray that corresponds to the given pixel's <paramref name="uv"/> coordinate
	/// </summary>
	/// <param name="uv">The UV coordinate of the pixel</param>
	/// <remarks>It is expected that the <paramref name="uv"/> coordinate is normalized to the range [0..1] for the X and Y values</remarks>
	public Ray GetRay(Vector2 uv)
	{
		if (Vector2.Clamp(Vector2.Zero, Vector2.One, uv) != uv)
			throw new ArgumentOutOfRangeException(nameof(uv), uv, "UV coordinates are only accepted in the range [0..1]");
		return new Ray(Zero, new Vector3(uv, 0));
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