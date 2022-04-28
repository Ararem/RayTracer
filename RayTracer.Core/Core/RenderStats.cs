using RayTracer.Core.Acceleration;
using RayTracer.Core.Hittables;

namespace RayTracer.Core;

/// <summary>
///  Struct that stores statistics about a render
/// </summary>
public struct RenderStats
{
	//TODO: Redo how rays are categorized, this doesn't seem very accurately named
	//TODO: Also BVH stats

#region Pixels & Passes

	/// <summary>
	///  How many pixels have been rendered, including multisampled pixels
	/// </summary>
	public ulong RawPixelsRendered = 0;

	/// <summary>
	///  How many passes have been rendered
	/// </summary>
	public int PassesRendered = 0;

	/// <summary>
	///  How many 'raw' pixels need to be rendered (including multisampled pixels)
	/// </summary>
	public ulong TotalRawPixels { get; }

	/// <summary>
	///  How many 'true' pixels need to be rendered (not including multisampling)
	/// </summary>
	public int TotalTruePixels { get; }

#endregion

#region Materials

	/// <summary>
	///  How many rays were scattered (bounced off materials) in the scene
	/// </summary>
	public ulong MaterialScatterCount = 0;

	/// <summary>
	///  The number of times one of the materials in the scene absorbed a light ray (and did not scatter)
	/// </summary>
	public ulong MaterialAbsorbedCount = 0;

#endregion

#region Hittables

	/// <summary>
	/// How many times the <see cref="AxisAlignedBoundingBox"/> was missed by a <see cref="Ray"/>
	/// </summary>
	public ulong AabbMisses = 0;

	/// <summary>
	/// How many times a <see cref="Ray"/> intersected with an <see cref="AxisAlignedBoundingBox"/> but did not hit the enclosed <see cref="Hittable"/>
	/// </summary>
	public ulong HittableMisses = 0;

	/// <summary>
	/// How many times a <see cref="Hittable"/> was intersected by a <see cref="Ray"/>
	/// </summary>
	public ulong HittableIntersections = 0;
#endregion

#region Rays

	/// <summary>
	///  How many rays did not hit any objects, and hit the sky
	/// </summary>
	public ulong SkyRays = 0;

	/// <summary>
	///  How times a ray was not rendered because the bounce count for that ray exceeded the limit specified by
	///  <see cref="Core.RenderOptions.MaxDepth"/>
	/// </summary>
	public ulong BounceLimitExceeded = 0;

	/// <summary>
	///  How many rays were rendered so far (scattered, absorbed, etc)
	/// </summary>
	public ulong RayCount = 0;

	/// <summary>
	///  A list that contains the number of times a ray 'finished' at a certain depth. The depth corresponds to the index, where [0] is no bounces, [1] is 1
	///  bounce, etc.
	/// </summary>
	public readonly ulong[] RawRayDepthCounts = Array.Empty<ulong>();

#endregion

	/// <summary>
	///  How many threads are currently rendering the scene
	/// </summary>
	public int ThreadsRunning = 0;

	internal RenderStats(ulong[] rawRayDepthCounts, int totalTruePixels, ulong totalRawPixels)
	{
		RawRayDepthCounts = rawRayDepthCounts;
		TotalTruePixels   = totalTruePixels;
		TotalRawPixels    = totalRawPixels;
	}
}