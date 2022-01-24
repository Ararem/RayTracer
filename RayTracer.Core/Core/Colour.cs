using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace RayTracer.Core;

/// <summary>
///  Represents an RGB colour
/// </summary>
[PublicAPI]
[SuppressMessage("Usage", "CA2225", MessageId = "Operator overloads have named alternates")]
public readonly struct Colour: IFormattable
{
	/// <summary>
	/// Red component
	/// </summary>
	public readonly float R;
	/// <summary>
	/// Green component
	/// </summary>
	public readonly float G;
	/// <summary>
	/// Blue component
	/// </summary>
	public readonly float B;

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

	/// <summary>
	///  Formats this colour as a string
	/// </summary>
	/// <param name="format">The format passed to each of the RGB components</param>
	/// <param name="formatProvider">Format provider for each component</param>
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

	public override string ToString() => ToString("G", CultureInfo.CurrentCulture);

	public string ToString(string? format) => ToString(format, CultureInfo.CurrentCulture);

#region Operators

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator +(Colour left, Colour right) => new(left.R + right.R, left.G + right.G, left.B + right.B);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator /(Colour left, Colour right) => new(left.R / right.R, left.G / right.G, left.B / right.B);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator /(Colour value1, float value2) => value1 / new Colour(value2);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator *(Colour left, Colour right) => new(left.R * right.R, left.G * right.G, left.B * right.B);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator *(Colour left, float right) => left * new Colour(right);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator *(float left, Colour right) => right * left;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator -(Colour left, Colour right) => new(left.R - right.R, left.G - right.G, left.B - right.B);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour operator -(Colour value) => Black - value;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour Abs(Colour value) => new(MathF.Abs(value.R), MathF.Abs(value.G), MathF.Abs(value.B));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour Clamp(Colour value1, Colour min, Colour max) => Min(Max(value1, min), max);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour Lerp(Colour value1, Colour value2, float amount) => (value1 * (1f - amount)) + (value2 * amount);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Colour Max(Colour value1, Colour value2) => new(value1.R > (double)value2.R ? value1.R : value2.R, value1.G > (double)value2.G ? value1.G : value2.G, value1.B > (double)value2.B ? value1.B : value2.B);

	public static Colour Min(Colour value1, Colour value2) => new(value1.R < (double)value2.R ? value1.R : value2.R, value1.G < (double)value2.G ? value1.G : value2.G, value1.B < (double)value2.B ? value1.B : value2.B);

	/// <summary>
	///  Converts a vector into a colour
	/// </summary>
	public static explicit operator Colour(Vector3 v) => new(v.X, v.Y, v.Z);

	/// <summary>
	/// Converts a floating-point <see cref="Color"/> to a byte <see cref="Rgb24"/>
	/// </summary>
	/// <param name="c"></param>
	/// <returns></returns>
	public static explicit operator Rgb24(Colour c) =>
			new(
					(byte)(byte.MaxValue * c.R),
					(byte)(byte.MaxValue * c.G),
					(byte)(byte.MaxValue * c.B)
			);

#endregion

#region Colours

	/// <summary>
	///  White (1,1,1)
	/// </summary>
	public static Colour White => new(1, 1, 1);

	/// <summary>
	///  Black (0,0,0)
	/// </summary>
	public static Colour Black => new(0, 0, 0);

	/// <summary>
	///  Red (1,0,0)
	/// </summary>
	public static Colour Red => new(1, 0, 0);

	/// <summary> Green (0,1,0) </summary>
	public static Colour Green => new(0, 1, 0);

	/// <summary>
	///  Blue (0,0,1)
	/// </summary>
	public static Colour Blue => new(0, 0, 1);

	/// <summary>
	///  Grey (0.5,0.5,0.5)
	/// </summary>
	public static Colour HalfGrey => new(0.5f);

#endregion
}