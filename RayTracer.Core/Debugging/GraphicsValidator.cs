using JetBrains.Annotations;
using System.Collections.Concurrent;
using System.Numerics;

namespace RayTracer.Core.Debugging;

/// <summary>
///  Static class for validating graphics
/// </summary>
public static class GraphicsValidator
{
	private const float MagnitudeEqualityError = 0.01f;

#region Storing errors

	/// <summary>
	///  Dictionary that stores how many times each error type has occurred, on a per-object basis
	/// </summary>
	//TODO: Figure out how to make this publicly readonly, preferably without having to copy the dictionary
	public static readonly ConcurrentDictionary<GraphicsErrorType, ConcurrentDictionary<object, ulong>> Errors = new();

	// /// <summary>
	// /// Gets a readonly copy of the errors dictionary
	// /// </summary>
	// /// <returns></returns>
	// public static IReadOnlyDictionary<Type, IReadOnlyDictionary<SceneObject, ulong>> GetErrors()
	// {
	// 	return new ReadOnlyDictionary<Type, IReadOnlyDictionary<SceneObject, ulong>>(Errors)
	// 	//Because of some weird type covariance thing (due to the inner dictionary nesting)
	// 	//I can't just return the Errors dictionary directly, so make a copy
	// 	Dictionary<Type, IReadOnlyDictionary<SceneObject, ulong>> copy = new();
	// 	foreach ((Type type, ConcurrentDictionary<SceneObject, ulong> innerDict) in Errors) copy.Add(type,innerDict);
	// 	return copy;
	// }

	/// <summary>
	///  Records that a certain type of <paramref name="error"/> occurred on an <paramref name="erroringObject"/>
	/// </summary>
	public static void RecordError(GraphicsErrorType error, object erroringObject)
	{
		ArgumentNullException.ThrowIfNull(erroringObject);
		//Get the dictionary that stores how many times the error has occurred per object, for the current error type
		ConcurrentDictionary<object, ulong> objectCountMap = Errors.GetOrAdd(error, _ => new ConcurrentDictionary<object, ulong>());

		//Now do the same for the target object
		//Either we get the current count and increment it (if it's already occurred for that object)
		//Or we create a new key for the object and set it's error count to 1
		objectCountMap.AddOrUpdate(erroringObject, 1, (_, oldVal) => oldVal + 1);
	}

#endregion

#region Validation Methods

	/// <summary>
	///  Checks a given direction vector has a correct magnitude (of 1)
	/// </summary>
	/// <returns>
	///  <see langword="true"/> if the vector had a correct magnitude, else <see langword="false"/>. If false is returned, the vector needs to be
	///  normalized
	/// </returns>
	[Pure]
	public static bool CheckVectorNormalized(Vector3 direction) =>
			//Check that the magnitude is approx 1 unit
			//Don't have to sqrt it because 1 squared is 1
			!(Math.Abs(direction.LengthSquared() - 1f) > MagnitudeEqualityError);

	/// <summary>
	///  Checks a given UV coordinate is valid
	/// </summary>
	/// <returns>
	///  <see langword="true"/> if the UV was valid, else <see langword="false"/>. If false is returned, the UV coordinate needs to be
	///  corrected
	/// </returns>
	[Pure]
	public static bool CheckUVCoordValid(Vector2 uv) => CheckValueRange(uv.X, 0, 1) && CheckValueRange(uv.Y, 0, 1);

	/// <summary>
	///  Checks a given RGB colour value is valid
	/// </summary>
	/// <returns>
	///  <see langword="true"/> if the colour was valid, else <see langword="false"/>. If false is returned, the colour coordinate needs to be
	///  corrected (clamped)
	/// </returns>
	[Pure]
	public static bool CheckColourValid(Colour col) => CheckValueRange(col.R, 0, 1) && CheckValueRange(col.G, 0, 1) && CheckValueRange(col.B, 0, 1);

	/// <summary>
	///  Checks a value is in the correct range
	/// </summary>
	[Pure]
	public static bool CheckValueRange(float k, float kMin, float kMax) => (k >= kMin) && (k <= kMax);

#endregion
}