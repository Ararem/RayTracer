using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Ararem.RayTracer.Core;

/// <summary>Represents an RGB colour</summary>
//P.S. (To America) - This is how you spell "color" the correct way
[PublicAPI]
[SuppressMessage("Usage", "CA2225", MessageId = "Operator overloads have named alternates")]
public readonly struct Colour : IFormattable
{
#region Fields and Ctors

	/// <summary>Red component</summary>
	public readonly float R;

	/// <summary>Green component</summary>
	public readonly float G;

	/// <summary>Blue component</summary>
	public readonly float B;

	/// <summary>Creates a colour from the RGB components</summary>
	public Colour(float r, float g, float b)
	{
		R = r;
		G = g;
		B = b;
	}

	/// <summary>
	///  Creates a greyscale colour where the <see cref="R"/>, <see cref="G"/>, <see cref="B"/> components are all the same, equal to the
	///  <paramref name="value"/>
	/// </summary>
	/// <param name="value">The value of the components</param>
	public Colour(float value) : this(value, value, value)
	{
	}

#endregion

#region Known Colours

	/// <summary>White (1, 1, 1)</summary>
	public static readonly Colour White = new(1, 1, 1);

	/// <summary>Black (0, 0, 0)</summary>
	public static readonly Colour Black = new(0, 0, 0);

	/// <summary>Red (1, 0, 0)</summary>
	public static readonly Colour Red = new(1, 0, 0);

	/// <summary> Green (0,1,0) </summary>
	public static readonly Colour Green = new(0, 1, 0);

	/// <summary>Blue (0, 0, 1)</summary>
	public static readonly Colour Blue = new(0, 0, 1);

	/// <summary>Grey (0.5, 0.5, 0.5)</summary>
	public static readonly Colour HalfGrey = new(0.5f);


	/// <summary>Purple (1, 0, 1)</summary>
	public static readonly Colour Purple = new(1, 0, 1);

	/// <summary>Yellow (1, 1, 0)</summary>
	public static readonly Colour Yellow = new(1, 1, 0);

	/// <summary>Orange (1, .5, 0)</summary>
	public static readonly Colour Orange = new(1, .5f, 0);

	/// <summary>Aqua (0, 1, 1)</summary>
	public static readonly Colour Aqua = new(0, 1, 1);

#endregion

#region Overrides

	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void CopyTo(float[] array) => CopyTo(array, 0);
	//
	// [MethodImpl(MethodImplOptions.AggressiveInlining)]
	// public void CopyTo(float[] array, int index)
	// {
	// 	if (array == null)
	// 		throw new ArgumentNullException(nameof(array));
	// 	if ((index < 0) || (index >= array.Length))
	// 		throw new ArgumentOutOfRangeException(nameof(index), SR.Format(SR.Arg_ArgumentOutOfRangeException, (object)index));
	// 	if (array.Length - index < 3)
	// 		throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, (object)index));
	// 	array[index]     = R;
	// 	array[index + 1] = G;
	// 	array[index + 2] = B;
	// }
	//
	// public void CopyTo(Span<float> destination)
	// {
	// 	if (destination.Length < 3)
	// 		ThrowHelper.ThrowArgumentException_DestinationTooShort();
	// 	Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
	// }
	//
	// public bool TryCopyTo(Span<float> destination)
	// {
	// 	if (destination.Length < 3)
	// 		return false;
	// 	Unsafe.WriteUnaligned(ref Unsafe.As<float, byte>(ref MemoryMarshal.GetReference(destination)), this);
	// 	return true;
	// }

	/// <summary>Formats this colour as a string</summary>
	/// <param name="format">The format passed to each of the RGB components</param>
	/// <param name="formatProvider">Format provider for each component</param>
	[Pure]
	public string ToString(string? format, IFormatProvider? formatProvider)
	{
		StringBuilder stringBuilder        = new();
		string        numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		stringBuilder.Append('(');
		stringBuilder.Append(R.ToString(format, formatProvider));
		stringBuilder.Append(numberGroupSeparator);
		stringBuilder.Append(' ');
		stringBuilder.Append(G.ToString(format, formatProvider));
		stringBuilder.Append(numberGroupSeparator);
		stringBuilder.Append(' ');
		stringBuilder.Append(B.ToString(format, formatProvider));
		stringBuilder.Append(')');
		return stringBuilder.ToString();
	}

	///<inheritdoc cref="Vector3.ToString(string, IFormatProvider)"/>
	[Pure]
	public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

	///<inheritdoc cref="Vector3.ToString(string)"/>
	[Pure]
	public string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

#endregion

#region Operators

	/// <summary>Adds the components of two colours together</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator +(Colour left, Colour right) => new(left.R + right.R, left.G + right.G, left.B + right.B);

	/// <summary>Divides one colour's components by an other's</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator /(Colour left, Colour right) => new(left.R / right.R, left.G / right.G, left.B / right.B);

	/// <summary>Divides a colour's components by a number</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator /(Colour value1, float value2) => value1 / new Colour(value2);

	/// <summary>Multiplies the components of two colours together</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator *(Colour left, Colour right) => new(left.R * right.R, left.G * right.G, left.B * right.B);

	/// <summary>Multiplies the components of a colour by a number</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator *(Colour left, float right) => left * new Colour(right);

	/// <summary>Multiplies the components of a colour by a number</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator *(float left, Colour right) => right * left;

	/// <summary>Subtracts the components of two colours together</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator -(Colour left, Colour right) => new(left.R - right.R, left.G - right.G, left.B - right.B);

	/// <summary>Inverts a colour</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour operator -(Colour value) => Black - value;

	/// <summary>Returns a colour with all it's components made positive</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour Abs(Colour value) => new(MathF.Abs(value.R), MathF.Abs(value.G), MathF.Abs(value.B));

	/// <summary>Clamps a colour's components between the range specified by the range of the other two colours</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour Clamp(Colour value1, Colour min, Colour max) => Min(Max(value1, min), max);

	/// <summary>Clamps a colour's components in the range [0...1]</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour Clamp01(Colour colour) => Clamp(colour, Black, White);

	/// <summary>Calculates the square root of a colour's channels</summary>
	public static Colour Sqrt(Colour colour) => new(MathF.Sqrt(colour.R), MathF.Sqrt(colour.G), MathF.Sqrt(colour.B));


	/// <summary>Computes the value of a colour's channels to a given power</summary>
	public static Colour Pow(Colour colour, float pow) => new(MathF.Pow(colour.R, pow), MathF.Pow(colour.G, pow), MathF.Pow(colour.B, pow));

	/// <summary>Linearly interpolates between two colours</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour Lerp(Colour value1, Colour value2, float amount) => (value1 * (1f - amount)) + (value2 * amount);

	/// <summary>Returns the maximum components of two colours</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Pure]
	public static Colour Max(Colour value1, Colour value2) => new(value1.R > (double)value2.R ? value1.R : value2.R, value1.G > (double)value2.G ? value1.G : value2.G, value1.B > (double)value2.B ? value1.B : value2.B);

	/// <summary>Returns the minimum components of two colours</summary>
	[Pure]
	public static Colour Min(Colour value1, Colour value2) => new(value1.R < (double)value2.R ? value1.R : value2.R, value1.G < (double)value2.G ? value1.G : value2.G, value1.B < (double)value2.B ? value1.B : value2.B);

	/// <summary>Converts a vector into a colour</summary>
	[Pure]
	public static explicit operator Colour(Vector3 v) => new(v.X, v.Y, v.Z);

	/// <summary>Converts a floating-point <see cref="Color"/> to a byte <see cref="Rgb24"/></summary>
	/// <param name="c"></param>
	/// <returns></returns>
	[Pure]
	public static explicit operator Rgb24(Colour c) =>
			new(
					(byte)(byte.MaxValue * c.R),
					(byte)(byte.MaxValue * c.G),
					(byte)(byte.MaxValue * c.B)
			);

	/// <summary>Deconstructs a colour into it's components</summary>
	/// <param name="r"></param>
	/// <param name="g"></param>
	/// <param name="b"></param>
	public void Deconstruct(out float r, out float g, out float b) => (r, g, b) = (R, G, B);

#endregion
}