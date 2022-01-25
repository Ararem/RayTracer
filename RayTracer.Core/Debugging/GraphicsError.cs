#if DEBUG
using System.Collections.Concurrent;

namespace RayTracer.Core.Debugging;

/// <summary>
///  Base class for implementing an object that
/// </summary>
public abstract class GraphicsError
{
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
}
#endif