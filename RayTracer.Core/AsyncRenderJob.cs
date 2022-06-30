//Debugging aid to help me compare when I change things with Hot Reload
// #define DEBUG_IGNORE_BUFFER_PREVIOUS

using JetBrains.Annotations;
using RayTracer.Core.Acceleration;
using RayTracer.Core.Debugging;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static RayTracer.Core.MathUtils;
using PrevHitPool = System.Buffers.ArrayPool<RayTracer.Core.HitRecord>;

namespace RayTracer.Core;

/// <summary>Class for rendering a <see cref="Scene"/>, using it's <see cref="Core.Scene.Camera"/>.</summary>
/// <remarks>Uses the rays generated by the <see cref="Core.Camera"/>, and objects in the <see cref="Scene"/> to create the output image</remarks>
[PublicAPI]
public sealed class AsyncRenderJob : IDisposable
{
	//TODO: Bloom would be quite fun. Might need to be a post-process after rendering complete
	/// <summary>Creates an async render job for a <paramref name="scene"/>, with configurable <paramref name="renderOptions"/></summary>
	/// <param name="scene">The scene containing the objects and camera for the render</param>
	/// <param name="renderOptions">
	///  Record containing options that affect how the resulting image is produced, such as resolution, multisample count or debug
	///  visualisations
	/// </param>
	[SuppressMessage("ReSharper.DPA", "DPA0003: Excessive memory allocations in LOH")] //Buffer allocations
	public AsyncRenderJob(Scene scene, RenderOptions renderOptions)
	{
		ArgumentNullException.ThrowIfNull(scene);
		ArgumentNullException.ThrowIfNull(renderOptions);
		Log.Debug("New AsyncRenderJob created with Scene={Scene} and Options={RenderOptions}", scene, renderOptions);

		Image             = new Image<Rgb24>(renderOptions.Width, renderOptions.Height);
		ImageBuffer       = Image.Frames.RootFrame!;
		RenderOptions     = renderOptions;
		rawColourBuffer   = new Colour[renderOptions.Width * renderOptions.Height];
		sampleCountBuffer = new int[renderOptions.Width    * renderOptions.Height];
		Scene             = scene;

		RenderStats = new RenderStats(renderOptions);

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

	/// <summary>The colour that's used whenever a given colour doesn't exist. E.g. when the bounce limit is exceeded, or a material doesn't scatter a ray</summary>
	public static Colour NoColour => Colour.Black;

	private void RenderInternal()
	{
		Log.Debug("Rendering start");
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
		for (int pass = 0; (RenderOptions.Passes == -1) || (pass < RenderOptions.Passes); pass++)
		{
			Parallel.For(
					0, RenderStats.TotalTruePixels,
					new ParallelOptions { MaxDegreeOfParallelism = RenderOptions.ConcurrencyLevel, CancellationToken = cancellationToken },
					() =>
					{
						Interlocked.Increment(ref RenderStats.ThreadsRunning);
						return this;
					}, //Gives us the state tracking the `this` reference, so we don't have to closure inside the body loop
					static (i, _, state) =>
					{
						(int x, int y) = Decompress2DIndex(i, state.RenderOptions.Width);
						Colour col = state.RenderPixelWithVisualisations(x, y);
						state.UpdateBuffers(x, y, col);
						Interlocked.Increment(ref state.RenderStats.RawPixelsRendered);

						return state;
					},
					static state => { Interlocked.Decrement(ref state.RenderStats.ThreadsRunning); }
			);

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
	private Colour RenderPixelWithVisualisations(int x, int y)
	{
		//To create some 'antialiasing' (SSAA maybe?), add a slight random offset to the uv coords
		float s = x, t = y;
		//Add up to half a pixel of offset randomness to the coords
		const float ssaaRadius = .3f;
		s += RandUtils.RandomPlusMinusOne() * ssaaRadius;
		t += RandUtils.RandomPlusMinusOne() * ssaaRadius;
		//Account for the fact that we want uv coords not pixel coords
		Ray viewRay = Scene.Camera.GetRay(s / RenderOptions.Width, t / RenderOptions.Height);

		//Switch depending on how we want to view the scene
		//Only if we don't have visualisations do we render the scene normally.
		if (RenderOptions.DebugVisualisation == GraphicsDebugVisualisation.None)
			return InitialCalculateRayColourRecursive(viewRay);

		//`CalculateRayColourLooped` will do the intersection code for us, so if we're not using it we have to manually check
		//Note that these visualisations will not 'bounce' off the scene objects, only the first hit is counted
		if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is { } hit)
			switch (RenderOptions.DebugVisualisation)
			{
				case GraphicsDebugVisualisation.Normals:
				{
					//Convert normal values [-1..1] to [0..1]
					Vector3 n = (hit.Normal + Vector3.One) / 2f;
					return (Colour)n;
				}
				case GraphicsDebugVisualisation.FaceDirection:
				{
					//Outside is green, inside is red
					return hit.OutsideFace ? Colour.Green : Colour.Red;
				}
				//Render how far away the objects are from the camera
				case GraphicsDebugVisualisation.DistanceFromCamera:
				{
					//I have several ways for displaying the depth
					//Changing `a` affects how steep the curve is. Higher values cause a faster drop off
					//Have to ensure it's >0 or else all functions return 1
					// ReSharper disable once UnusedVariable
					#pragma warning disable CS0219
					const float a = .00200f;
					#pragma warning restore CS0219
					// ReSharper disable once JoinDeclarationAndInitializer
					float val;
					float z = hit.K - RenderOptions.KMin;

					// val = z / (RenderOptions.KMax - RenderOptions.KMin); //Inverse lerp k to [0..1]. Doesn't work when KMax is large (especially infinity)
					val   = MathF.Pow(MathF.E, -a * z);                    //Exponential
					// val   = 1f / ((a * z) + 1);                            //Reciprocal X. Get around asymptote by treating KMin as 0, and starting at x=1
					// val   = 1 - (MathF.Atan(a * z) * (2f / MathF.PI));     //Inverse Tan
					// val = MathF.Pow(MathF.E, -(a * z * z)); //Bell Curve
					return new Colour(val);
				}
					static Colour RandomColourFromMaterialHash(Material m, bool offset){
						int        hash  = m.GetHashCode();
						Span<byte> bytes = stackalloc byte[sizeof(int)];
						BitConverter.TryWriteBytes(bytes, hash);

						int   o  = offset ? 1 : 0;
						float rh = bytes[o+0] /255f;
						float gh = bytes[o+1] /255f;
						float bh = bytes[o+2] /255f;
						return new Colour(rh, gh, bh);
					}
				//Debug texture based on X/Y pixel coordinates
				case GraphicsDebugVisualisation.PixelCoordDebugTexture:
					return RandomColourFromMaterialHash(hit.Material,MathF.Sin(x / 2f) * MathF.Sin(y / 2f) < 0) ;
				case GraphicsDebugVisualisation.WorldCoordDebugTexture:
					return RandomColourFromMaterialHash(hit.Material, MathF.Sin(hit.LocalPoint.X * 40f) * MathF.Sin(hit.LocalPoint.Y * 40f) * MathF.Sin(hit.LocalPoint.Z * 40f) < 0);
				case GraphicsDebugVisualisation.LocalCoordDebugTexture:
					return RandomColourFromMaterialHash(hit.Material, MathF.Sin(hit.LocalPoint.X * 40f) * MathF.Sin(hit.LocalPoint.Y * 40f) * MathF.Sin(hit.LocalPoint.Z * 40f) < 0);
				case GraphicsDebugVisualisation.ScatterDirection:
				{
					//Convert vector values [-1..1] to [0..1]
					Vector3 scat = hit.Material.Scatter(hit, ArraySegment<HitRecord>.Empty)?.Direction ?? -Vector3.One;
					Vector3 n    = (scat + Vector3.One) / 2f;
					return (Colour)n;
				}
				case GraphicsDebugVisualisation.None:
					break; //Shouldn't get to here
				case GraphicsDebugVisualisation.UVCoords:
					return new Colour(hit.UV.X, hit.UV.Y, 1);
				case GraphicsDebugVisualisation.EstimatedLightIntensity:
				{
					Colour sum = Colour.Black;
					foreach (Light light in Scene.Lights)
					{
						sum += light.CalculateLight(hit, out _);
					}

					return sum;
				}
				case GraphicsDebugVisualisation.UndefinedTestVisualisation:
				{
					Colour sum = Colour.Black;
					foreach (Light light in Scene.Lights)
					{
						sum += light.CalculateLight(hit, out _, true);
					}

					return sum;
				}
				default:
					throw new ArgumentOutOfRangeException(nameof(RenderOptions.DebugVisualisation), RenderOptions.DebugVisualisation, "Wrong enum value for debug visualisation");
			}

		//No object was intersected with
		return NoColour;
	}

	private Colour InitialCalculateRayColourRecursive(Ray ray)
	{
		HitRecord[]             array   = PrevHitPool.Shared.Rent(RenderOptions.MaxDepth);
		ArraySegment<HitRecord> segment = new(array, 0, 0);
		return InternalCalculateRayColourRecursive(ray, 0, segment);
	}

	/// <summary>Recursive function to calculate the colour for a ray</summary>
	/// <param name="ray">The ray to calculate the colour from</param>
	/// <param name="depth">
	///  The number of times the ray has bounced. If this is 0, then the ray has never bounced, and so we can assume it's the initial ray
	///  from the camera
	/// </param>
	/// <param name="prevHitsSegment">Array segment that contains the previous hits</param>
	/// <returns></returns>
	private Colour InternalCalculateRayColourRecursive(Ray ray, int depth, ArraySegment<HitRecord> prevHitsSegment)
	{
		//Don't go too deep
		if (depth > RenderOptions.MaxDepth)
		{
			Interlocked.Increment(ref RenderStats.BounceLimitExceeded);
			return NoColour;
		}
		//TODO: Depth tracking

		Interlocked.Increment(ref RenderStats.RayCount);
		if (TryFindClosestHit(ray, RenderOptions.KMin, RenderOptions.KMax) is {} hit)
		{
			//See if the material scatters the ray
			Ray? maybeNewRay = hit.Material.Scatter(hit, prevHitsSegment);

			if (maybeNewRay is not { } newRay)
			{
				//If the new ray is null, the material did not scatter (completely absorbed the light)
				//So it's impossible to have any future bounces, so quit and return black
				Interlocked.Increment(ref RenderStats.MaterialAbsorbedCount);
				return NoColour;
			}
			else
			{
				//Otherwise, the material scattered, creating a new ray, render recursively again
				Interlocked.Increment(ref RenderStats.MaterialScatterCount);
				prevHitsSegment.Array![prevHitsSegment.Count] = hit;                                                                 //Update the hit buffer from this hit
				ArraySegment<HitRecord> newSegment = new(prevHitsSegment.Array, 0, prevHitsSegment.Count + 1); //Extend the segment to include our new element
				Colour future = InternalCalculateRayColourRecursive(newRay, depth + 1, newSegment);
				return hit.Material.CalculateColour(future, hit, prevHitsSegment);
			}
		}
		//No object was hit (at least not in the range), so return the skybox colour
		else
		{
			Interlocked.Increment(ref RenderStats.SkyRays);
			return Scene.SkyBox.GetSkyColour(ray);
		}
	}

	private Colour CalculateRayColourLooped(Ray ray)
	{
		//Reusing pools from ArrayPool should reduce memory (I was using `new Stack<...>()` before, which I'm sure isn't a good idea
		//This stores the hit information, as well as what object was intersected with (at that hit)
		HitRecord[] materialHitArray = PrevHitPool.Shared.Rent(RenderOptions.MaxDepth + 1);
		Colour                                finalColour      = NoColour;
		//Loop for a max number of times equal to the depth
		//And map out the ray path (don't do any colours yet)
		//TODO: Fix this depth++/-- stuff, it's iffy
		int depth;
		for (depth = 0; depth < RenderOptions.MaxDepth; depth++)
		{
			Interlocked.Increment(ref RenderStats.RayCount);
			if (TryFindClosestHit(ray, RenderOptions.KMin, RenderOptions.KMax) is {} hit)
			{
				ArraySegment<HitRecord> prevHits = new(materialHitArray, 0, depth); //Shouldn't include the current hit
				//See if the material scatters the ray
				Ray? maybeNewRay = hit.Material.Scatter(hit, prevHits);

				if (maybeNewRay is null)
				{
					//If the new ray is null, the material did not scatter (completely absorbed the light)
					//So it's impossible to have any future bounces, so quit the loop
					Interlocked.Increment(ref RenderStats.MaterialAbsorbedCount);
					finalColour             = NoColour;
					materialHitArray[depth] = hit;
					depth++; //Have to counteract the `depth--` further down
					break;
				}
				else
				{
					//Otherwise, the material scattered, creating a new ray
					ray = (Ray)maybeNewRay;
					Interlocked.Increment(ref RenderStats.MaterialScatterCount);
					materialHitArray[depth] = hit;
				}
			}
			//No object was hit (at least not in the range), so return the skybox colour
			else
			{
				Interlocked.Increment(ref RenderStats.SkyRays);
				finalColour = Scene.SkyBox.GetSkyColour(ray);
				break;
			}
		}

		if (depth == RenderOptions.MaxDepth) Interlocked.Increment(ref RenderStats.BounceLimitExceeded);
		Interlocked.Increment(ref RenderStats.RawRayDepthCounts[depth]);

		//Now do the colour pass
		//Have to decrement depth here or we get index out of bounds because `depth++` is called on the exiting (last) iteration of the above for loop
		depth--;
		for (; depth >= 0; depth--)
		{
			HitRecord               hit      = materialHitArray[depth];
			ArraySegment<HitRecord> prevHits = new(materialHitArray, 0, depth); //Shouldn't include the current hit
			finalColour = hit.Material.CalculateColour(finalColour, hit, prevHits);
		}

		PrevHitPool.Shared.Return(materialHitArray);

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
				if (bounces > RenderOptions.MaxDepth)
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

			Log.Warning(
					"HitRecord normal had incorrect magnitude, fixing. Correcting {WrongNormal} ({WrongMagnitude})	=>	{CorrectedNormal} ({CorrectedMagnitude})\nHit: {@Hit}\nObject: {@Object}",
					wrongNormal, wrongMag, correctNormal, correctMag, hit, obj
			);
			GraphicsValidator.RecordError(GraphicsErrorType.NormalsWrongMagnitude, obj);
		}

		if (!GraphicsValidator.CheckUVCoordValid(hit.UV))
		{
			Vector2 wrongUv     = hit.UV;
			Vector2 correctedUv = Vector2.Clamp(hit.UV, Vector2.Zero, Vector2.One);

			Log.Warning(
					"HitRecord UV was out of range, fixing. Correcting {WrongUV}	=>	{CorrectedUV}\nHit: {@Hit}\nObject: {@Object}",
					wrongUv, correctedUv, hit, obj
			);
			GraphicsValidator.RecordError(GraphicsErrorType.UVInvalid, obj);
		}

		if (!GraphicsValidator.CheckValueRange(hit.K, kMin, kMax))
		{
			Log.Warning(
					"Hittable K value was not in correct range, skipping object. K Value: {Value}, valid range is [{KMin}..{KMax}]\nHit: {@Hit}\nObject: {@Object}",
					hit.K, kMin, kMax, hit, obj
			);
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
	private void UpdateBuffers(int x, int y, Colour newSampleColour)
	{
		//We have to flip the y- value because the camera expects y=0 to be the bottom (cause UV coords)
		//But the image expects it to be at the top (Graphics APIs amirite?)
		//The (X,Y) we're given is camera coords
		y = RenderOptions.Height - y - 1;

		//NOTE: Although this may not be 'thread-safe' at first glance, we don't actually need to lock to safely access and change to array
		//Although multiple threads will be rendering and changing pixels, two passes can never render at the same time (see RenderInternal)
		//Passes (and pixels) are rendered sequentially, so there is no chance of a pixel being accessed by multiple threads at the same time.
		//In previous profiling sessions, locking was approximately 65% of the total time spent updating, with 78% of the time being this method call
		int i = Compress2DIndex(x, y, RenderOptions.Width);
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
		ImageBuffer[x, y] = rgb24;
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