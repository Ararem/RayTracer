using RayTracer.Core.Graphics;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using System.Collections.Concurrent;
using System.Numerics;
using static RayTracer.Core.Debugging.GraphicsErrorType;

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
	/// </summary>
	/// <param name="error"></param>
	/// <param name="erroringObject"></param>
	public static void RecordError(GraphicsErrorType error, object erroringObject)
	{
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
	///  Ensures a given ray's direction has a correct magnitude (of 1)
	/// </summary>
	public static void CheckRayDirectionMagnitude(ref Ray r, Material source)
	{
		if (Math.Abs(r.Direction.LengthSquared() - 1f) > MagnitudeEqualityError)
		{
			RecordError(RayDirectionWrongMagnitude, source);
			r = r with { Direction = Vector3.Normalize(r.Direction) };
		}
	}

	/// <summary>
	///  Ensures a given ray's direction has a correct magnitude (of 1)
	/// </summary>
	public static void CheckRayDirectionMagnitude(ref Ray r, Camera source)
	{
		if (Math.Abs(r.Direction.LengthSquared() - 1f) > MagnitudeEqualityError)
		{
			RecordError(RayDirectionWrongMagnitude, source);
			r = r with { Direction = Vector3.Normalize(r.Direction) };
		}
	}

	/// <summary>
	///  Ensures a <see cref="HitRecord"/>'s <see cref="HitRecord.Normal"/> has a magnitude of 1
	/// </summary>
	public static void CheckNormalMagnitude(ref HitRecord hit, Hittable source)
	{
		//Check that the normal magnitude is approx 1 unit
		//Don't have to sqrt it because 1 squared is 1
		if (Math.Abs(hit.Normal.LengthSquared() - 1f) > MagnitudeEqualityError)
		{
			RecordError(NormalsWrongMagnitude, source);
			hit = hit with
			{
					Normal = Vector3.Normalize(hit.Normal)
			};
		}
	}

	/// <summary>
	///  Validates a <see cref="HitRecord"/>'s K value is in the correct range
	/// </summary>
	public static void CheckKValueRange(ref HitRecord hit, RenderOptions options, Hittable source)
	{
		if (hit.K < options.KMin)
		{
			RecordError(KValueNotInRange, source);
			hit = hit with { K = options.KMin };
		}
		else if (hit.K > options.KMax)
		{
			RecordError(KValueNotInRange, source);
			hit = hit with { K = options.KMax };
		}
	}

#endregion
}