using RayTracer.Core.Acceleration;
using RayTracer.Core.Hittables;

namespace RayTracer.Core;

/// <summary>
///  Class that stores statistics about a render
/// </summary>
//Yeah I'm using long's everywhere, just cause `ints` aren't large enough on large images, and I still want negative support
public sealed class RenderStats
{
	/// <summary>
	///  How many threads are currently rendering the scene
	/// </summary>
	public int ThreadsRunning = 0;

	/// <summary>
	///  Constructor for creating a new <see cref="RenderStats"/> object
	/// </summary>
	/// <param name="options">Render options, used to assign and calculate some of the values</param>
	public RenderStats(RenderOptions options)
	{
		RawRayDepthCounts = new long[options.MaxDepth + 1]; //+1 because we can also have 0 bounces;
		TotalTruePixels   = options.Width * options.Height;
		TotalRawPixels    = options.Width * options.Height * options.Passes;
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	public RenderStats(RenderStats original)
	{
		RawPixelsRendered     = original.RawPixelsRendered;
		PassesRendered        = original.PassesRendered;
		MaterialScatterCount  = original.MaterialScatterCount;
		MaterialAbsorbedCount = original.MaterialAbsorbedCount;
		AabbMisses            = original.AabbMisses;
		HittableMisses        = original.HittableMisses;
		HittableIntersections = original.HittableIntersections;
		SkyRays               = original.SkyRays;
		BounceLimitExceeded   = original.BounceLimitExceeded;
		RayCount              = original.RayCount;
		RawRayDepthCounts     = original.RawRayDepthCounts;
		ThreadsRunning        = original.ThreadsRunning;
		TotalRawPixels        = original.TotalRawPixels;
		TotalTruePixels       = original.TotalTruePixels;
	}

	/// <inheritdoc/>
	public override string ToString() => $"{nameof(RawPixelsRendered)}: {RawPixelsRendered}, {nameof(PassesRendered)}: {PassesRendered}, {nameof(MaterialScatterCount)}: {MaterialScatterCount}, {nameof(MaterialAbsorbedCount)}: {MaterialAbsorbedCount}, {nameof(AabbMisses)}: {AabbMisses}, {nameof(HittableMisses)}: {HittableMisses}, {nameof(HittableIntersections)}: {HittableIntersections}, {nameof(SkyRays)}: {SkyRays}, {nameof(BounceLimitExceeded)}: {BounceLimitExceeded}, {nameof(RayCount)}: {RayCount}, {nameof(RawRayDepthCounts)}: {RawRayDepthCounts}, {nameof(ThreadsRunning)}: {ThreadsRunning}, {nameof(TotalRawPixels)}: {TotalRawPixels}, {nameof(TotalTruePixels)}: {TotalTruePixels}";

#region Pixels & Passes

	/// <summary>
	///  How many pixels have been rendered, including multisampled pixels
	/// </summary>
	public long RawPixelsRendered = 0;

	/// <summary>
	///  How many passes have been rendered
	/// </summary>
	public int PassesRendered = 0;

	/// <summary>
	///  How many 'raw' pixels need to be rendered (including multisampled pixels)
	/// </summary>
	public long TotalRawPixels { get; }

	/// <summary>
	///  How many 'true' pixels need to be rendered (not including multisampling)
	/// </summary>
	public int TotalTruePixels { get; }

#endregion

#region Materials

	/// <summary>
	///  How many rays were scattered (bounced off materials) in the scene
	/// </summary>
	public long MaterialScatterCount = 0;

	/// <summary>
	///  The number of times one of the materials in the scene absorbed a light ray (and did not scatter)
	/// </summary>
	public long MaterialAbsorbedCount = 0;

#endregion

#region Hittables

	/// <summary>
	///  How many times the <see cref="AxisAlignedBoundingBox"/> was missed by a <see cref="Ray"/>
	/// </summary>
	public long AabbMisses = 0;

	/// <summary>
	///  How many times a <see cref="Ray"/> intersected with an <see cref="AxisAlignedBoundingBox"/> but did not hit the enclosed <see cref="Hittable"/>
	/// </summary>
	public long HittableMisses = 0;

	/// <summary>
	///  How many times a <see cref="Hittable"/> was intersected by a <see cref="Ray"/>
	/// </summary>
	public long HittableIntersections = 0;

#endregion

#region Rays

	/// <summary>
	///  How many rays did not hit any objects, and hit the sky
	/// </summary>
	public long SkyRays = 0;

	/// <summary>
	///  How times a ray was not rendered because the bounce count for that ray exceeded the limit specified by
	///  <see cref="Core.RenderOptions.MaxDepth"/>
	/// </summary>
	public long BounceLimitExceeded = 0;

	/// <summary>
	///  How many rays were rendered so far (scattered, absorbed, etc)
	/// </summary>
	public long RayCount = 0;

	/// <summary>
	///  A list that contains the number of times a ray 'finished' at a certain depth. The depth corresponds to the index, where [0] is no bounces, [1] is 1
	///  bounce, etc.
	/// </summary>
	public readonly long[] RawRayDepthCounts;

#endregion
}