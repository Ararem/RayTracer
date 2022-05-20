using RayTracer.Core;

namespace RayTracer.Impl.Textures;

/// <summary>Texture that uses a custom function to obtain the colour values</summary>
public class CustomFunctionTexture : Texture
{
	/// <summary>Texture that uses a custom function to obtain the colour values</summary>
	/// <param name="getColourFunction">Function that gets the <see cref="Colour"/> for the texture</param>
	public CustomFunctionTexture(Func<HitRecord, Colour> getColourFunction)
	{
		GetColourFunction = getColourFunction;
	}

	/// <summary>Function that gets the <see cref="Colour"/> for the texture</summary>
	public Func<HitRecord, Colour> GetColourFunction { get; }

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit) => GetColourFunction(hit);
}

/// <summary>Texture that uses a custom function to obtain the colour values, with a state parameter</summary>
public class CustomFunctionTexture<T> : Texture
{
	/// <summary>Texture that uses a custom function to obtain the colour values, with a state parameter</summary>
	/// <param name="getColourFunction">Function that gets the <see cref="Colour"/> for the texture</param>
	/// <param name="state">State object passed into <see cref="GetColourFunction"/></param>
	public CustomFunctionTexture(Func<HitRecord, T, Colour> getColourFunction, T state)
	{
		GetColourFunction = getColourFunction;
		State             = state;
	}

	/// <summary>Function that gets the <see cref="Colour"/> for the texture</summary>
	public Func<HitRecord, T, Colour> GetColourFunction { get; }

	/// <summary>State object passed into <see cref="GetColourFunction"/></summary>
	public T State { get; }

	/// <inheritdoc/>
	public override Colour GetColour(HitRecord hit) => GetColourFunction(hit, State);
}