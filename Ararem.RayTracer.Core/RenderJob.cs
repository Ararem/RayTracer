//Debugging aid to help me compare when I change things with Hot Reload

// #define DEBUG_IGNORE_BUFFER_PREVIOUS

using Ararem.RayTracer.Core.Acceleration;
using Ararem.RayTracer.Core.Debugging;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using static Ararem.RayTracer.Core.MathUtils;
using static System.MathF;
using Log = Serilog.Log;

namespace Ararem.RayTracer.Core;

/// <summary>Class for rendering a <see cref="Scene"/>, using it's <see cref="Core.Scene.Camera"/>.</summary>
/// <remarks>Uses the rays generated by the <see cref="Core.Camera"/>, and objects in the <see cref="Scene"/> to create the output image</remarks>
[PublicAPI]
public sealed class RenderJob : IDisposable
{
	//TODO: Bloom would be quite fun. Might need to be a post-process after rendering complete
	/// <summary>Creates an async render job for a <paramref name="scene"/>, with configurable <paramref name="renderOptions"/></summary>
	/// <param name="scene">The scene containing the objects and camera for the render</param>
	/// <param name="renderOptions">
	///  Record containing options that affect how the resulting image is produced, such as resolution, multisample count or debug
	///  visualisations
	/// </param>
	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH")] //Buffer allocations
	public RenderJob(Scene scene, RenderOptions renderOptions)
	{
		ArgumentNullException.ThrowIfNull(scene);
		ArgumentNullException.ThrowIfNull(renderOptions);
		Log.Information("New RenderJob created with Scene={Scene} and Options={@RenderOptions}", scene, renderOptions);

		Image             = new Image<Rgb24>((int)renderOptions.RenderWidth, (int)renderOptions.RenderHeight);
		ImageBuffer       = Image.Frames.RootFrame!;
		RenderOptions     = renderOptions;
		rawColourBuffer   = new Colour[renderOptions.RenderWidth * renderOptions.RenderHeight];
		sampleCountBuffer = new int[renderOptions.RenderWidth    * renderOptions.RenderHeight];
		Scene             = scene;

		RenderStats = new RenderStats();

		//Assign access for all the components that need it
		#pragma warning disable CS0618
		foreach (Light light in Scene.Lights) light.Renderer = this;
		foreach (SceneObject sceneObject in Scene.SceneObjects)
		{
			sceneObject.Hittable.Renderer = this;
		}
		#pragma warning restore CS0618

		//Calculate the bounding boxes
		BvhTree = new BvhTree(scene, RenderStats);

		RenderTask = new Task(RenderInternal, TaskCreationOptions.LongRunning);
	}

	private static ArrayPool<HitRecord> PrevHitPool => ArrayPool<HitRecord>.Shared;

	/// <summary>The colour that's used whenever a given colour doesn't exist. E.g. when the bounce limit is exceeded, or a material doesn't scatter a ray</summary>
	public static Colour NoColour => Colour.Black;

	private void RenderInternal()
	{
		Log.Debug("Rendering start with CancellationToken {@CancellationToken}", cancellationToken);
		Stopwatch.Start();
		/*
		 * Due to how i've internally implemented the buffers and functions, it doesn't matter what order the pixels are rendered in
		 * It doesn't even matter if some pixels are rendered with different sample counts, since i'm using a multi-buffer approach to store the averaging data
		 * I'm doing a x->y->s nested loop approach, but you could also have `s` as the outer loop, or even render the pixels completely randomly..?????
		 * Also makes it easy to turn it into an async method with `System.Threading.Parallel`
		 */
		// for (int x = 0; x < renderOptions.Width; x++)
		// {
		// 	for (int y = 0; y < renderOptions.Height; y++)
		// 	{
		// 		for (int s = 0; s < renderOptions.Samples; s++)
		// 			RenderAndUpdatePixel(x, y, s);
		// 		Increment(ref truePixelsRendered);
		// 	}
		// }

		//Same comment from above applies, just different order
		//Values are also compressed into a single number, then unpacked after
		//I do this so that it's easier to parallelize the loop without nesting them too much (parallel nesting is probably bad)
		for (ulong pass = 0; RenderOptions.InfinitePasses || (pass < RenderOptions.Passes); pass++)
		{
			try
			{
				Parallel.For(
						0L, (long)RenderOptions.RenderWidth * (long)RenderOptions.RenderHeight, new ParallelOptions { MaxDegreeOfParallelism = (int)RenderOptions.ConcurrencyLevel, CancellationToken = cancellationToken }, () =>
						{
							Interlocked.Increment(ref RenderStats.ThreadsRunning);
							return this;
						}, //Gives us the state tracking the `this` reference, so we don't have to closure inside the body loop
						static (longI, loop, state) =>
						{
							ulong i = (ulong)longI; //Have to cast this here because microsoft won't create a ulong version of Parallel.For()???????
							(ulong x, ulong y) = Decompress2DIndex(i, state.RenderOptions.RenderWidth);
							Colour col = state.RenderPixelWithVisualisations(x, y);
							state.UpdateBuffers(x, y, col);
							Interlocked.Increment(ref state.RenderStats.PixelsRendered);

							return state;
						}, static state => { Interlocked.Decrement(ref state.RenderStats.ThreadsRunning); }
				);
			}
			catch (OperationCanceledException e)
			{
				Log.ForContext("OperationCancelledException", e, true).Information("Render cancelled during pass {Pass}", pass);
				break;
			}

			Interlocked.Increment(ref RenderStats.PassesRendered);
			Log.Debug("Finished pass {Pass}", pass);
		}

		Stopwatch.Stop();
		Log.Information("Rendering end");
	}

	/// <summary>
	///  Renders a single pixel with the coordinates (<paramref name="x"/>, <paramref name="y"/>). If debug visualisations are enabled, will return the pixel
	///  rendered by the visualiser, rather than the object's colour
	/// </summary>
	/// <remarks><paramref name="x"/> and <paramref name="y"/> coords start at the lower-left corner, moving towards the upper-right.</remarks>
	/// <param name="x">X coordinate of the pixel</param>
	/// <param name="y">Y coordinate of the pixel</param>
	private Colour RenderPixelWithVisualisations(ulong x, ulong y)
	{
		//To create some 'antialiasing' (SSAA maybe?), add a slight random offset to the uv coords
		float s = x, t = y;
		//Add up to half a pixel of offset randomness to the coords
		const float ssaaRadius = .5f; //I'm choosing .5 so that adjacent pixels exactly border each other.
		s += RandUtils.RandomPlusMinusOne() * ssaaRadius;
		t += RandUtils.RandomPlusMinusOne() * ssaaRadius;
		//Account for the fact that we want uv coords not pixel coords
		Ray viewRay = Scene.Camera.GetRay(s / RenderOptions.RenderWidth, t / RenderOptions.RenderHeight);

		//Switch depending on how we want to view the scene
		//Only if we don't have visualisations do we render the scene normally.
		switch (RenderOptions.DebugVisualisation)
		{
			case GraphicsDebugVisualisation.None:
				return CalculateRayColourLooped(viewRay);

			//`CalculateRayColourLooped` will do the intersection code for us, so if we're not using it we have to manually check
			//Note that (most of) these visualisations will not 'bounce' off the scene objects, only the first hit is counted

			case GraphicsDebugVisualisation.Normals:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				//Convert normal values [-1..1] to [0..1]
				Vector3 n = (hit.Normal + Vector3.One) / 2f;
				return (Colour)n;
			}
			case GraphicsDebugVisualisation.FaceDirection:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				//Outside is green, inside is red
				return hit.OutsideFace ? Colour.Green : Colour.Red;
			}
			//Render how far away the objects are from the camera
			/*
				const float a = 1f;
				float val;
				float z = hit.K - RenderOptions.KMin;

				// val = z / (RenderOptions.KMax - RenderOptions.KMin); //Inverse lerp k to [0..1]. Doesn't work when KMax is large (especially infinity)
				// val = MathF.Pow(MathF.E, -a * z); //Exponential
				// val   = 1f / ((a * z) + 1);                            //Reciprocal X. Get around asymptote by treating KMin as 0, and starting at x=1
				val = 1 - (MathF.Atan(a * z) * (2f / MathF.PI)); //Inverse Tan
				// val = MathF.Pow(MathF.E, -(a * z * z)); //Bell Curve
			 */
			case GraphicsDebugVisualisation.DistanceFromCamera_Close:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				float z = hit.K - RenderOptions.KMin;
				// float val = MathF.Pow(MathF.E, -.2f * z); //Exponential
				float val = Pow(E, -(0.01f * z * z)); //Bell Curve
				return new Colour(val);
			}
			case GraphicsDebugVisualisation.DistanceFromCamera_Mid:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				float z = hit.K - RenderOptions.KMin;
				// float val = MathF.Pow(MathF.E, -.02f * z); //Exponential
				float val = Pow(E, -(0.0001f * z * z)); //Bell Curve
				return new Colour(val);
			}
			case GraphicsDebugVisualisation.DistanceFromCamera_Far:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				float z = hit.K - RenderOptions.KMin;
				// float val = MathF.Pow(MathF.E, -.0008f * z); //Exponential
				float val = Pow(E, -(.000001f * z * z)); //Bell Curve
				return new Colour(val);
			}
			//Debug texture based on X/Y pixel coordinates
			case GraphicsDebugVisualisation.PixelCoordDebugTexture:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				return UniqueColourFromMaterialHash(hit.Material, Sin(x / 2f) * Sin(y / 2f) < 0);
			}
			case GraphicsDebugVisualisation.WorldCoordDebugTexture:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				return UniqueColourFromMaterialHash(hit.Material, Sin(hit.WorldPoint.X * 40f) * Sin(hit.WorldPoint.Y * 40f) * Sin(hit.WorldPoint.Z * 40f) < 0);
			}
			case GraphicsDebugVisualisation.LocalCoordDebugTexture:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				return UniqueColourFromMaterialHash(hit.Material, Sin(hit.LocalPoint.X * 40f) * Sin(hit.LocalPoint.Y * 40f) * Sin(hit.LocalPoint.Z * 40f) < 0);
			}
			case GraphicsDebugVisualisation.ScatterDirection:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				//Convert vector values [-1..1] to [0..1]
				Vector3 scat = hit.Material.Scatter(hit, ArraySegment<HitRecord>.Empty)?.Direction ?? Vector3.Zero;
				Vector3 n    = (scat + Vector3.One) / 2f;
				return (Colour)n;
			}
			case GraphicsDebugVisualisation.UVCoords:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				return new Colour(hit.UV.X, hit.UV.Y, 1);
			}
			case GraphicsDebugVisualisation.EstimatedLight:
			{
				if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is not {} hit) return NoColour;
				Colour sum     = Colour.Black;
				ulong  samples = RenderOptions.LightSampleCountHint;
				for (ulong i = 0; i < samples; i++)
				{
					foreach (Light light in Scene.Lights)
					{
						sum += light.CalculateLight(hit, out _);
					}
				}

				sum /= samples;

				return sum;
			}
			case GraphicsDebugVisualisation.BounceDepth:
			{
				int maxDepthReached = -1;                                //The highest depth value we reached
				int maxAllowedDepth = (int)RenderOptions.MaxBounceDepth; //The highest depth we can reach. Also the max index for `hitStateArray`
				Ray currentRay      = viewRay;                           //The ray we are currently calculating on. Make a copy so we don't modify the initial ray parameter

				//Reusing pools from ArrayPool should reduce memory (I was using `new Stack<...>()` before, which I'm sure isn't a good idea
				//This stores the hit state information, as well as what object was intersected with (at that hit)
				//Add 1 to the size so that we have 1:1 mapping for depth:index (since array indices start at 0 and we want maxAllowedDepthItems as the highest index)
				//Depth of 0 (no bounces, direct from camera) = index[0]
				HitRecord[] hitStateArray = PrevHitPool.Rent(maxAllowedDepth + 1);
				for (int currentDepth = 0;; currentDepth++)
				{
					// maxDepthReached = currentDepth; //Update the max depth

					//Ensure we don't go too deep
					if (currentDepth > maxAllowedDepth)
					{
						Interlocked.Increment(ref RenderStats.BounceLimitExceeded);
						break;
					}

					if (TryFindClosestHit(currentRay, RenderOptions.KMin, RenderOptions.KMax) is {} hit)
					{
						maxDepthReached             = currentDepth;                                //Update the max depth
						hitStateArray[currentDepth] = hit;                                         //Store the hit in the array
						ArraySegment<HitRecord> prevHits    = new(hitStateArray, 0, currentDepth); //Create the segment for the previous hits, which shouldn't include the current hit
						Ray?                    maybeNewRay = hit.Material.Scatter(hit, prevHits);
						if (maybeNewRay is null)
						{
							Interlocked.Increment(ref RenderStats.MaterialAbsorbedCount);
							//If the new ray is null, the material did not scatter (completely absorbed the light)
							//So it's impossible to have any future bounces, so quit the loop
							break;
						}
						else
						{
							Interlocked.Increment(ref RenderStats.MaterialScatterCount);
							//Otherwise, the material scattered, creating a new ray
							currentRay = (Ray)maybeNewRay;
						}
					}
					else
					{
						//No object was hit (at least not in the range), so return the skybox colour
						Interlocked.Increment(ref RenderStats.SkyRays);
						break;
					}
				}

				PrevHitPool.Return(hitStateArray);

				if (maxDepthReached == -1) return Colour.Purple;
				float val = 1 - Pow(E, -(maxDepthReached * maxDepthReached * Sqrt(maxAllowedDepth)));
				return Colour.Lerp(Colour.White, Colour.Blue * 0.02f, val);
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(RenderOptions.DebugVisualisation), RenderOptions.DebugVisualisation, "Wrong enum value for debug visualisation");
		}
	}


	/// <summary>
	///  Calculates a unique colour for a given material, using it's hash By setting `alternate` to true, we can get a secondary colour, which is useful when
	///  creating a checker texture
	/// </summary>
	private static Colour UniqueColourFromMaterialHash(Material m, bool alternate)
	{
		int        hash  = m.GetHashCode();
		Span<byte> bytes = stackalloc byte[sizeof(int)];
		BitConverter.TryWriteBytes(bytes, hash);

		int   o  = alternate ? 1 : 0;
		float rh = bytes[o + 0] / 255f;
		float gh = bytes[o + 1] / 255f;
		float bh = bytes[o + 2] / 255f;
		return new Colour(rh, gh, bh);
	}
	//
	// private Colour InitialCalculateRayColourRecursive(Ray ray)
	// {
	// 	HitRecord[]             array   = PrevHitPool.Shared.Rent(RenderOptions.MaxBounceDepth);
	// 	ArraySegment<HitRecord> segment = new(array, 0, 0);
	// 	Colour colour = InternalCalculateRayColourRecursive(ray, 0, segment);
	// 	PrevHitPool.Shared.Return(array);
	// 	return colour;
	// }
	//
	// /// <summary>Recursive function to calculate the colour for a ray</summary>
	// /// <param name="ray">The ray to calculate the colour from</param>
	// /// <param name="depth">
	// ///  The number of times the ray has bounced. If this is 0, then the ray has never bounced, and so we can assume it's the initial ray
	// ///  from the camera
	// /// </param>
	// /// <param name="prevHitsFromCamSegment">Array segment that contains the previous hits between the camera and the last hit</param>
	// /// <returns></returns>
	// private Colour InternalCalculateRayColourRecursive(Ray ray, int depth, ArraySegment<HitRecord> prevHitsFromCamSegment)
	// {
	// 	//Don't go too deep
	// 	if (depth > RenderOptions.MaxBounceDepth)
	// 	{
	// 		Interlocked.Increment(ref RenderStats.BounceLimitExceeded);
	// 		return NoColour;
	// 	}
	// 	//TODO: Depth tracking
	//
	// 	Interlocked.Increment(ref RenderStats.RayCount);
	// 	if (TryFindClosestHit(ray, RenderOptions.KMin, RenderOptions.KMax) is {} hit)
	// 	{
	// 		//See if the material scatters the ray
	// 		ArraySegment<Ray> newRays = hit.Material.Scatter(hit, prevHitsFromCamSegment);
	//
	// 		if ((newRays.Count == 0) || (newRays == default) || newRays.Array is null)
	// 		{
	// 			//If there are no new rays, the material did not scatter (completely absorbed the light)
	// 			//So it's impossible to have any future bounces, so quit and return black
	// 			Interlocked.Increment(ref RenderStats.MaterialAbsorbedCount);
	// 			ReturnSegment(newRays);
	// 			return NoColour;
	// 		}
	// 		else
	// 		{
	// 			//Otherwise, the material scattered, creating some new rays, render recursively again
	// 			Interlocked.Add(ref RenderStats.MaterialScatterCount, newRays.Count);
	// 			prevHitsFromCamSegment.Array![prevHitsFromCamSegment.Count] = hit;                                           //Update the hit buffer from this hit
	// 			ArraySegment<HitRecord> newSegment = new(prevHitsFromCamSegment.Array, 0, prevHitsFromCamSegment.Count + 1); //Extend the segment to include our new element
	//
	// 			//TODO: Make these segments and stuff a custom IDisposable struct, connected with LibArarem ObjectPool
	// 			//Here we calculate the future ray colours
	// 			ArraySegment<(Colour Colour, Ray ray)> futureRayInfo    = GetPooledSegment<(Colour Colour, Ray ray)>(newRays.Count);
	// 			for (int i = 0; i < futureRayInfo.Count; i++)
	// 			{
	// 				Colour future = InternalCalculateRayColourRecursive(newRays[i], depth + 1, newSegment);
	// 				futureRayInfo[i] = (future, newRays[i]);
	// 			}
	//
	// 			Colour colour =  hit.Material.CalculateColour(futureRayInfo, hit, prevHitsFromCamSegment);
	// 			ReturnSegment(futureRayInfo);
	// 			ReturnSegment(newRays);
	// 			return colour;
	// 		}
	// 	}
	// 	//No object was hit (at least not in the range), so return the skybox colour
	// 	else
	// 	{
	// 		Interlocked.Increment(ref RenderStats.SkyRays);
	// 		return Scene.SkyBox.GetSkyColour(ray);
	// 	}
	// }

	private Colour CalculateRayColourLooped(Ray initialRay)
	{
		int    maxDepthReached = -1;                                //The highest depth value we reached
		int    maxAllowedDepth = (int)RenderOptions.MaxBounceDepth; //The highest depth we can reach. Also the max index for `hitStateArray`
		Ray    currentRay      = initialRay;                        //The ray we are currently calculating on. Make a copy so we don't modify the initial ray parameter
		Colour finalColour;

		//Reusing pools from ArrayPool should reduce memory (I was using `new Stack<...>()` before, which I'm sure isn't a good idea
		//This stores the hit state information, as well as what object was intersected with (at that hit)
		//Add 1 to the size so that we have 1:1 mapping for depth:index (since array indices start at 0 and we want maxAllowedDepthItems as the highest index)
		//Depth of 0 (no bounces, direct from camera) = index[0]
		HitRecord[] hitStateArray = PrevHitPool.Rent(maxAllowedDepth + 1);
		for (int currentDepth = 0;; currentDepth++)
		{
			//Ensure we don't go too deep
			if (currentDepth > maxAllowedDepth)
			{
				Interlocked.Increment(ref RenderStats.BounceLimitExceeded);
				finalColour = NoColour;
				break;
			}

			if (TryFindClosestHit(currentRay, RenderOptions.KMin, RenderOptions.KMax) is {} hit)
			{
				maxDepthReached             = currentDepth;                                //Update the max depth
				hitStateArray[currentDepth] = hit;                                         //Store the hit in the array
				ArraySegment<HitRecord> prevHits    = new(hitStateArray, 0, currentDepth); //Create the segment for the previous hits, which shouldn't include the current hit
				Ray?                    maybeNewRay = hit.Material.Scatter(hit, prevHits);
				if (maybeNewRay is null)
				{
					Interlocked.Increment(ref RenderStats.MaterialAbsorbedCount);
					finalColour = NoColour;
					//If the new ray is null, the material did not scatter (completely absorbed the light)
					//So it's impossible to have any future bounces, so quit the loop
					break;
				}
				else
				{
					Interlocked.Increment(ref RenderStats.MaterialScatterCount);
					//Otherwise, the material scattered, creating a new ray
					currentRay = (Ray)maybeNewRay;
				}
			}
			else
			{
				//No object was hit (at least not in the range), so return the skybox colour
				Interlocked.Increment(ref RenderStats.SkyRays);
				finalColour = Scene.SkyBox.GetSkyColour(currentRay);
				break;
			}
		}

		if (maxDepthReached == -1) //We didn't hit any object at all
			return finalColour;    //So return the skybox colour directly (no materials to calculate and it would be broken anyway)

		ulong maxDepthUlong = (ulong) maxDepthReached;
		// ReSharper disable once UnusedParameter.Local
		RenderStats.RayDepthCounts.AddOrUpdate(maxDepthUlong, 1, (key, oldValue) => oldValue + 1);

		//Now go in reverse for the colour pass
		for (int currentDepth = maxDepthReached; currentDepth >= 0; currentDepth--)
		{
			HitRecord               hit      = hitStateArray[currentDepth];
			ArraySegment<HitRecord> prevHits = new(hitStateArray, 0, currentDepth); //Shouldn't include the current hit
			finalColour = hit.Material.CalculateColour(finalColour, currentRay, hit, prevHits);
			/*
			 * Important note about using currentRay:
			 * It's important that we use the last value from the intersection `for` loop, and update it after we calculate the colour, using the current hit's ray
			 *
			 * My previous code `finalColour = hit.Material.CalculateColour(finalColour, hitStateArray[currentDepth+1], hit, prevHits);`
			 * Would sometimes fail randomly with "Index Out of Bounds" whenever we reached the max depth allowed by maxAllowedDepth
			 * I realised it was due to the code trying to access the 'future ray' where one didn't exist.
			 *
			 * Assuming we reach our max bounces, in this loop, `currentDepth` is set to the max - 15
			 * This is fine when we access the current hit (it's the last element), but when we try to access the next one (for the future ray),
			 * It was rather intermittent, only failing when the maxAllowedDepth was 2^n-1.
			 * Then, when we try to access element 16, it doesn't exist (out of bounds)
			 * Since we're using an ArrayPool, it rounds array sizes up to the nearest power of 2
			 * So when we have a max depth of 15, the array size we request is 16, which we get
			 * For any other maxDepthAllowed, the array size is at least 1 larger than we need, so everything appears to be fine
			 * (We just access the ray of an uninitialized HitRecord and get a `Ray {Origin = Zero, Direction = Zero}`)
			 *
			 * So by keeping the value from the intersection loop (which isn't processed),
			 * We can use that instead for the last intersection, and update it reversely for the rest (neat eh?)
			 */
			currentRay  = hitStateArray[currentDepth].IncomingRay;
		}

		PrevHitPool.Return(hitStateArray);

		return finalColour;

		/*
			This is the original, recursive version of the CalculateRayColourLooped function.
			I some help by looking at the code on CUDA RayTracing by Roger Allen (see https://github.com/rogerallen/raytracinginoneweekendincuda/blob/ch07_diffuse_cuda/main.cu)

			/// <summary>
			///  Recursive function to calculate the given colour for a ray. Does not take into account debug visualisations.
			/// </summary>
			/// <param name="ray">The ray to calculate the colour from</param>
			/// <param name="bounces">
			///  The number of times the ray has bounced. If this is 0, then the ray has never bounced, and so we can assume it's the initial
			///  ray from the camera
			/// </param>
			private Colour CalculateRayColourRecursive(Ray ray, int bounces)
			{
				//Check ray magnitude is 1
				GraphicsValidator.CheckRayDirectionMagnitude(ref ray, camera);

				//Ensure we don't go too deep
				if (bounces > RenderOptions.MaxBounceDepth)
				{
					Interlocked.Increment(ref bounceLimitExceeded);
					return Colour.Black;
				}

				//Increment the current depth
				Interlocked.Increment(ref rawRayDepthCounts[bounces]);
				//And decrement the previous depth. This ensures only the final depth is counted
				if (bounces != 0) Interlocked.Decrement(ref rawRayDepthCounts[bounces - 1]);

				//Find the nearest hit along the ray
				Interlocked.Increment(ref rayCount);
				if (TryFindClosestHit(ray, out HitRecord? maybeHit, out Material? material))
				{
					HitRecord hit = (HitRecord)maybeHit!;
					//See if the material scatters the ray
					Ray?   maybeNewRay = material!.Scatter(hit);
					Colour futureBounces;

					if (maybeNewRay is null)
					{
						//If the new ray is null, the material did not scatter (completely absorbed the light)
						//So it's impossible to have any future bounces, so we know that they must be black
						Interlocked.Increment(ref raysAbsorbed);
						futureBounces = Colour.Black;
					}
					else
					{
						//Otherwise, the material scattered, creating a new ray, so calculate the future bounces recursively
						Interlocked.Increment(ref raysScattered);
						GraphicsValidator.CheckRayDirectionMagnitude(ref ray, material);
						futureBounces = CalculateRayColourRecursive((Ray)maybeNewRay, bounces + 1);
					}

					//Tell the material to do it's lighting stuff
					//By doing it this way, we can essentially do anything, and we don't have to do much in the camera itself
					//So we can have materials that emit light, ones that amplify light, ones that change the colour of the light, anything really
					//So we pass in the colour that we obtained from the future bounces, and let the material directly modify it to get the resulting colour
					Colour colour = futureBounces;
					material.CalculateColour(ref colour, hit);
					return colour;
				}
				//No object was hit (at least not in the range), so return the skybox colour
				else
				{
					Interlocked.Increment(ref skyRays);
					return skybox.GetSkyColour(ray);
				}
			}

			*/
	}

	/// <summary>
	///  Fast method that simply checks if there is any intersection along a given <paramref name="ray"/>, in the range specified by [<paramref name="kMin"/>
	///  ..<paramref name="kMax"/>]
	/// </summary>
	/// <remarks>
	///  Use this for simple shadow-like checks, to see if <i>anything</i> lies in between the light source and target point. Note that this will return
	///  <see langword="true"/> as soon as an intersection is hit, and does not take into account a material's properties (such as transparency), just
	///  geometry.
	/// </remarks>
	/// s
	public bool AnyIntersectionFast(Ray ray, float kMin, float kMax) => BvhTree.RootNode.FastTryHit(ray, kMin, kMax);

	/// <summary>Finds the closest intersection along a given <paramref name="ray"/></summary>
	/// <param name="ray">The ray to check for intersections</param>
	/// <param name="kMin">Lower bound for K along the ray</param>
	/// <param name="kMax">Upper bound for K along the ray</param>
	public HitRecord? TryFindClosestHit(Ray ray, float kMin, float kMax)
	{
		Interlocked.Increment(ref RenderStats.RayCount);
		//I love how simple this is
		//Like I just need to validate the result and that's it
		(SceneObject Object, HitRecord Hit)? maybeHit = BvhTree.TryHit(ray, kMin, kMax);
		if (maybeHit is not var (obj, hit)) return null;
		if (!GraphicsValidator.CheckVectorNormalized(hit.Normal))
		{
			Vector3 wrongNormal = hit.Normal;
			float   wrongMag    = wrongNormal.Length();

			Vector3 correctNormal = ray.Direction;
			float   correctMag    = correctNormal.Length();

			Log.Warning("HitRecord normal had incorrect magnitude, fixing. Correcting {WrongNormal} ({WrongMagnitude})	=>	{CorrectedNormal} ({CorrectedMagnitude})\nHit: {@Hit}\nObject: {@Object}", wrongNormal, wrongMag, correctNormal, correctMag, hit, obj);
			GraphicsValidator.RecordError(GraphicsErrorType.NormalsWrongMagnitude, obj);
		}

		if (!GraphicsValidator.CheckUVCoordValid(hit.UV))
		{
			Vector2 wrongUv     = hit.UV;
			Vector2 correctedUv = Vector2.Clamp(hit.UV, Vector2.Zero, Vector2.One);

			Log.Warning("HitRecord UV was out of range, fixing. Correcting {WrongUV}	=>	{CorrectedUV}\nHit: {@Hit}\nObject: {@Object}", wrongUv, correctedUv, hit, obj);
			GraphicsValidator.RecordError(GraphicsErrorType.UVInvalid, obj);
		}

		if (!GraphicsValidator.CheckValueRange(hit.K, kMin, kMax))
		{
			Log.Warning("Hittable K value was not in correct range, skipping object. K Value: {Value}, valid range is [{KMin}..{KMax}]\nHit: {@Hit}\nObject: {@Object}", hit.K, kMin, kMax, hit, obj);
			GraphicsValidator.RecordError(GraphicsErrorType.KValueNotInRange, obj);
			return null; //Skip because we can't consider it valid
		}

		return hit;
	}

	/// <summary>
	///  Updates the pixel and colour buffers for the pixel at (<paramref name="x"/>, <paramref name="y"/>), on the basis that that pixel was just rendered
	///  with a <paramref name="newSampleColour"/>, and the average needs to be updated
	/// </summary>
	/// <param name="x">X coordinate for the pixel (camera coords, left to right)</param>
	/// <param name="y">Y coordinate for the pixel (camera coords, bottom to top)</param>
	/// <param name="newSampleColour">Colour that the pixel was just rendered as</param>
	private void UpdateBuffers(ulong x, ulong y, Colour newSampleColour)
	{
		//We have to flip the y- value because the camera expects y=0 to be the bottom (cause UV coords)
		//But the image expects it to be at the top (Graphics APIs amirite?)
		//The (X,Y) we're given is camera coords
		y = RenderOptions.RenderHeight - y - 1;

		//NOTE: Although this may not be 'thread-safe' at first glance, we don't actually need to lock to safely access and change to array
		//Although multiple threads will be rendering and changing pixels, two passes can never render at the same time (see RenderInternal)
		//Passes (and pixels) are rendered sequentially, so there is no chance of a pixel being accessed by multiple threads at the same time.
		//In previous profiling sessions, locking was approximately 65% of the total time spent updating, with 78% of the time being this method call
		ulong i = Compress2DIndex(x, y, RenderOptions.RenderWidth);
		#if DEBUG_IGNORE_BUFFER_PREVIOUS
		sampleCountBuffer[i] = 1;
		rawColourBuffer[i] = newSampleColour;
		//Have to clamp the colour here or we get funky things in the image later
		Colour finalColour = newSampleColour;
		#else
		sampleCountBuffer[i]++;
		rawColourBuffer[i] += newSampleColour;
		Colour finalColour = rawColourBuffer[i] / sampleCountBuffer[i];
		#endif

		//Have to clamp the colour here or we get funky things in the image later
		finalColour = Colour.Clamp01(finalColour);
		//Sqrt for gamma=2 correction
		finalColour = Colour.Sqrt(finalColour);
		Rgb24 rgb24 = (Rgb24)finalColour;
		ImageBuffer[(int)x, (int)y] = rgb24;
	}

#region Internal state

	/// <summary><see cref="RayTracer.Core.Acceleration.BvhTree"/> that was constructed for this render's <see cref="Scene"/></summary>
	public BvhTree BvhTree { get; }

	/// <summary>Stopwatch used to time how long has elapsed since the rendering started</summary>
	public Stopwatch Stopwatch { get; } = new();

	/// <summary>Options used to render</summary>
	public RenderOptions RenderOptions { get; }

	/// <summary>Raw buffer containing denormalized colour values ([0..SampleCount])</summary>
	private readonly Colour[] rawColourBuffer;

	/// <summary>Buffer recording how many samples make up a given pixel, to create averages</summary>
	private readonly int[] sampleCountBuffer;

	/// <summary>
	///  Image buffer for the output image. Points to the <see cref="ImageFrameCollection{TPixel}.RootFrame"/> of the <see cref="Image"/> being
	///  rendered.
	/// </summary>
	//The reason I have pulled this out is because when trying to set an image pixel, it has to evaluate the ImageFrame each time, so cache it here
	public ImageFrame<Rgb24> ImageBuffer { get; }

	/// <summary>Image for the final render output</summary>
	public Image<Rgb24> Image { get; }

	/// <summary>The scene that is being rendered</summary>
	public Scene Scene { get; }

	/// <summary>Object containing statistics about the render, e.g. how many pixels have been rendered.</summary>
	public RenderStats RenderStats { get; }

#endregion

#region Async and Task-like awaitable implementation

	/// <summary>Whether this render job has completed rendering</summary>
	public bool RenderCompleted => RenderTask.IsCompleted;

	/// <summary>A <see cref="Task"/> that can be used to <see langword="await"/> the render job.</summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public readonly Task RenderTask;

	/// <summary>Gets the task awaiter for this instance</summary>
	public TaskAwaiter GetAwaiter() => RenderTask.GetAwaiter();

	/// <summary>Starts the render asynchronously and returns the task that can be used to await it.</summary>
	/// <param name="maybeCancellationToken">A <see cref="cancellationToken"/> that can be used to cancel the render operation, if required</param>
	/// <returns>A <see cref="Task"/> that represents the render operation</returns>
	/// <remarks>If the render has already been started, a new render is not started, and the existing render task is returned instead.</remarks>
	public Task StartOrGetRenderAsync(CancellationToken? maybeCancellationToken = null)
	{
		//Threadsafe way of only allowing this to be called once
		if (Interlocked.Exchange(ref started, this) == null)
		{
			if (maybeCancellationToken is not null)
				cancellationToken = (CancellationToken)maybeCancellationToken;
			RenderTask.Start();
		}
		else
		{
			Log.Warning("Render already started");
		}

		return RenderTask;
	}

	/// <summary>Cancellation token used to cancel the render job</summary>
	private CancellationToken cancellationToken = CancellationToken.None;

	/// <summary>Special fancy threadsafe flag for if render has been started yet</summary>
	private object? started = null;


	/// <inheritdoc/>
	public void Dispose()
	{
		RenderTask.Dispose();
	}

#endregion
}