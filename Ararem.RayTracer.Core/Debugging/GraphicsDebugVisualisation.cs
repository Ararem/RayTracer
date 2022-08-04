namespace Ararem.RayTracer.Core.Debugging;

/// <summary>Enum for enabling graphical visualisations for debugging purposes</summary>
public enum GraphicsDebugVisualisation
{
	/// <summary>No debug, render normally</summary>
	None = 0,

	/// <summary>Display object normals instead of the object's material</summary>
	/// <remarks>(R,G,B) correspond to the (X,Y,Z) axes respectively, and have been scaled into the range [0..1] (from [-1..1])</remarks>
	Normals,

	/// <summary>Display which direction a face points in (inside or outside)</summary>
	/// <remarks>Outside face is green, inside is red</remarks>
	FaceDirection,

	/// <summary>Output a greyscale image based on how close the intersection is to the camera. Should be used in scenes with relatively small distances (e.g. a few units)</summary>
	DistanceFromCamera_Close,

	/// <summary>Output a greyscale image based on how close the intersection is to the camera. Should be used in scenes with relatively large distances (e.g. a few hundred units)</summary>
	DistanceFromCamera_Mid,

	/// <summary>Output a greyscale image based on how close the intersection is to the camera. Should be used in scenes with relatively large distances (e.g. a few hundred units)</summary>
	DistanceFromCamera_Far,

	/// <summary>UV coordinate output by the object's intersection code</summary>
	UVCoords,

	/// <summary>Whenever a ray hits an object, display a debug texture (useful for seeing if objects are visible in the scene)</summary>
	/// <remarks>Generates the colour from the pixel's coordinates, creating a checker pattern</remarks>
	PixelCoordDebugTexture,

	/// <summary>Display the object scatter direction. Similar to <see cref="Normals"/></summary>
	ScatterDirection,

	/// <summary>Whenever a ray hits an object, display a debug texture based on the point's position in local-space</summary>
	LocalCoordDebugTexture,

	/// <summary>Whenever a ray hits an object, display a debug texture based on the point's position in world-space</summary>
	WorldCoordDebugTexture,

	/// <summary>How much light is estimated to reach the hit. May be affected depending on what material is used and how that material handles lighting</summary>
	EstimatedLight,

	/// <summary>Undefined visualisation used for development and testing purposes, should not be used</summary>
	UndefinedTestVisualisation
	//TODO: Add how many bounces reached
}