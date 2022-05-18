using RayTracer.Core;

namespace RayTracer.Impl.Textures;

/// <summary>
///  Texture that uses a custom function to obtain the colour values
/// </summary>
/// <param name="GetColourFunction">Function that gets the <see cref="Colour"/> for the texture</param>
public record CustomFunctionTexture(Func<HitRecord, Colour> GetColourFunction) : Texture
{
	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit) => GetColourFunction(hit);
}

/// <summary>
///  Texture that uses a custom function to obtain the colour values, with a state parameter
/// </summary>
/// <param name="GetColourFunction">Function that gets the <see cref="Colour"/> for the texture</param>
/// <param name="State">State object passed into <see cref="GetColourFunction"/></param>
public record CustomFunctionTexture<T>(Func<HitRecord, T, Colour> GetColourFunction, T State) : Texture
{
	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit) => GetColourFunction(hit, State);
}