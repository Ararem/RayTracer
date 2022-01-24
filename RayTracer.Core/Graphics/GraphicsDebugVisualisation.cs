namespace RayTracer.Core.Graphics;

/// <summary>
///  Enum for enabling graphical visualisations for debugging purposes
/// </summary>
public enum GraphicsDebugVisualisation
{
	/// <summary>
	///  No debug, render normally
	/// </summary>
	None = 0,

	/// <summary>
	///  Display object normals instead of the object's material
	/// </summary>
	/// <remarks>
	///  (R,G,B) correspond to the (X,Y,Z) axes respectively, and have been scaled into the range [0..1] (from [-1..1])
	/// </remarks>
	Normals,

	/// <summary>
	///  Display which direction a face points in (inside or outside)
	/// </summary>
	/// <remarks>Outside face is green, inside is red</remarks>
	FaceDirection
}