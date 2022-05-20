namespace RayTracer.Core;

/// <summary>A class that defines a material that a <see cref="Hittable"/> can have</summary>
public abstract class Material : RenderAccessor
{
	/// <summary>Scatters an input ray, according to this material's properties</summary>
	/// <param name="hit">Information such as where the ray hit the object, surface normals, etc</param>
	/// <returns>A new ray, which represents the direction a light ray would be scattered in when bouncing off this material's surface</returns>
	/// <remarks>
	///  For a completely reflective material, the resulting ray would be 'flipped' around the surface <see cref="HitRecord.Normal"/>, and for a completely
	///  diffuse object, it would be in a random direction
	/// </remarks>
	public abstract Ray? Scatter(HitRecord hit);

	/// <summary>Function to override for when the material wants to do lighting calculations, based on the light from future rays</summary>
	/// <param name="colour">
	///  The colour information for the future bounces that were made. Modify this to vary how your material behaves
	///  colour-wise/lighting-wise
	/// </param>
	/// <param name="hit">Information such as where the ray hit, surface normals etc</param>
	/// <param name="previousHits"></param>
	/// <remarks>
	///  Use the <paramref name="hit"/> to evaluation world information, such as where on a texture map the point corresponds to, and make changes to the
	///  <paramref name="colour"/> using that information
	/// </remarks>
	/// <example>
	///  <para>
	///   A simple implementation that multiplies by the colour red, treating the object as completely red:
	///   <code>
	///  public override void DoColourThings(ref Colour colour, in MeshHit meshHit, int depth)
	///  {
	///  	colour *= Colour.Red;
	///  }
	///  </code>
	///  </para>
	///  <para>
	///   A simple implementation that adds blue light, simulating a blue light-emitting light-source:
	///   <code>
	///  public override void DoColourThings(ref Colour colour, in MeshHit meshHit, int depth)
	///  {
	///  	colour += Colour.Blue;
	///  }
	///  </code>
	///  </para>
	///  <para>
	///   A simple implementation that adds half-white light and multiplies red, simulating a dim white light-emitting object that reflects red light
	///   <code>
	///  public override void DoColourThings(ref Colour colour, in MeshHit meshHit, int depth)
	///  {
	/// 		//Only 30% white is added so it's not too bright, but all red is reflected
	///  	colour = (colour * Colour.Red) + (Colour.White * 0.3f);
	///  }
	///  </code>
	///  </para>
	/// </example>
	public abstract void DoColourThings(ref Colour colour, HitRecord hit, ArraySegment<(SceneObject sceneObject, HitRecord hitRecord)> previousHits);
}