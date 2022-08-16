using Ararem.RayTracer.Core.Acceleration;
using System.Collections.Concurrent;

namespace Ararem.RayTracer.Core;

/// <summary>Class that stores statistics about a render</summary>
//Yeah I'm using ulongs everywhere, just cause ints aren't large enough on large images, and it's easier to have them all the same
//TODO: Refactor this to be a struct
public sealed class RenderStats
{
	/// <summary>How many threads are currently rendering the scene</summary>
	public ulong ThreadsRunning = 0;

	/// <summary>Constructor for creating a new <see cref="RenderStats"/> object</summary>
	public RenderStats()
	{
	}

	/// <summary>Copy constructor</summary>
	public RenderStats(RenderStats original)
	{
		PixelsRendered     = original.PixelsRendered;
		PassesRendered        = original.PassesRendered;
		MaterialScatterCount  = original.MaterialScatterCount;
		MaterialAbsorbedCount = original.MaterialAbsorbedCount;
		AabbMisses            = original.AabbMisses;
		HittableMisses        = original.HittableMisses;
		HittableIntersections = original.HittableIntersections;
		SkyRays               = original.SkyRays;
		BounceLimitExceeded   = original.BounceLimitExceeded;
		RayCount              = original.RayCount;
		RayDepthCounts     = original.RayDepthCounts;
		ThreadsRunning        = original.ThreadsRunning;
	}

	/// <inheritdoc/>
	public override string ToString() => $"{nameof(PixelsRendered)}: {PixelsRendered}, {nameof(PassesRendered)}: {PassesRendered}, {nameof(MaterialScatterCount)}: {MaterialScatterCount}, {nameof(MaterialAbsorbedCount)}: {MaterialAbsorbedCount}, {nameof(AabbMisses)}: {AabbMisses}, {nameof(HittableMisses)}: {HittableMisses}, {nameof(HittableIntersections)}: {HittableIntersections}, {nameof(SkyRays)}: {SkyRays}, {nameof(BounceLimitExceeded)}: {BounceLimitExceeded}, {nameof(RayCount)}: {RayCount}, {nameof(RayDepthCounts)}: {RayDepthCounts}, {nameof(ThreadsRunning)}: {ThreadsRunning}";

#region Pixels & Passes

	/// <summary>How many pixels have been rendered, including multisampled pixels</summary>
	public ulong PixelsRendered = 0;

	/// <summary>How many passes have been rendered</summary>
	public ulong PassesRendered = 0;

#endregion

#region Materials

	/// <summary>How many rays were scattered (bounced off materials) in the scene</summary>
	public ulong MaterialScatterCount = 0;

	/// <summary>The number of times one of the materials in the scene absorbed a light ray (and did not scatter)</summary>
	public ulong MaterialAbsorbedCount = 0;

#endregion

#region Hittables

	/// <summary>How many times the <see cref="AxisAlignedBoundingBox"/> was missed by a <see cref="Ray"/></summary>
	public ulong AabbMisses = 0;

	/// <summary>
	///  How many times a <see cref="Ray"/> intersected with an <see cref="AxisAlignedBoundingBox"/> but did not hit the enclosed
	///  <see cref="Hittable"/>
	/// </summary>
	public ulong HittableMisses = 0;

	/// <summary>How many times a <see cref="Hittable"/> was intersected by a <see cref="Ray"/></summary>
	public ulong HittableIntersections = 0;

#endregion

#region Rays

	/// <summary>How many rays did not hit any objects, and hit the sky</summary>
	public ulong SkyRays = 0;

	/// <summary>
	///  How times a ray was not rendered because the bounce count for that ray exceeded the limit specified by
	///  <see cref="RenderOptions.MaxBounceDepth"/>
	/// </summary>
	public ulong BounceLimitExceeded = 0;

	/// <summary>How many rays were rendered so far (scattered, absorbed, etc)</summary>
	public ulong RayCount = 0;

	/// <summary>
	///  A dictionary that contains the number of times a ray 'finished' at a certain depth. The depth corresponds to the index, where [0] is no bounces (direct cam->hit), [1] is 1
	///  bounce (cam->obj1->obj2), etc.
	/// </summary>
	public readonly ConcurrentDictionary<ulong, ulong> RayDepthCounts = new(Environment.ProcessorCount,32); //Assume the user will use all threads for the concurrency level

#endregion
}