//Debugging aid to help me compare when I change things with Hot Reload
// #define DEBUG_IGNORE_BUFFER_PREVIOUS

using RayTracer.Core.Acceleration;
using RayTracer.Core.Debugging;
using RayTracer.Core.Environment;
using RayTracer.Core.Hittables;
using Serilog;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using static RayTracer.Core.MathUtils;

namespace RayTracer.Core;

/// <summary>
///  Class for rendering a <see cref="Scene"/>, using it's <see cref="Core.Scene.Camera"/>.
/// </summary>
/// <remarks>
///  Uses the rays generated by the <see cref="Camera"/>, and objects in the <see cref="Scene"/> to create the output image
/// </remarks>
public sealed class AsyncRenderJob : IDisposable
{
	//TODO: Bloom would be quite fun. Might need to be a post-process after rendering complete
	/// <summary>
	///  Creates an async render job for a <paramref name="scene"/>, with configurable <paramref name="renderOptions"/>
	/// </summary>
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

		ImageBuffer                          = new Image<Rgb24>(renderOptions.Width, renderOptions.Height);
		RenderOptions                        = renderOptions;
		rawColourBuffer                      = new Colour[renderOptions.Width * renderOptions.Height];
		sampleCountBuffer                    = new int[renderOptions.Width    * renderOptions.Height];
		(_, camera, objects, lights, skybox) = scene;
		Scene                                = scene;

		RenderStats = new RenderStats(renderOptions);

		//Assign access for all the components that need it
		foreach (Light light in scene.Lights) light.SetRenderer(this);
		foreach (SceneObject sceneObject in scene.SceneObjects)
		{
			sceneObject.Material.SetRenderer(this);
			sceneObject.Hittable.SetRenderer(this);
		}

		//Calculate the bounding boxes
		bvhTree = new BvhTree(scene, RenderStats);

		RenderTask = new Task(RenderInternal, TaskCreationOptions.LongRunning);
	}

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
		for (int pass = 0; pass < RenderOptions.Passes; pass++)
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
	/// <remarks>
	///  <paramref name="x"/> and <paramref name="y"/> coords start at the lower-left corner, moving towards the upper-right.
	/// </remarks>
	/// <param name="x">X coordinate of the pixel</param>
	/// <param name="y">Y coordinate of the pixel</param>
	private Colour RenderPixelWithVisualisations(int x, int y)
	{
		//To create some 'antialiasing' (SSAA maybe?), add a slight random offset to the uv coords
		float s = x, t = y;
		//Add up to half a pixel of offset randomness to the coords
		const float ssaaRadius = .5f;
		s += RandUtils.RandomPlusMinusOne() * ssaaRadius;
		t += RandUtils.RandomPlusMinusOne() * ssaaRadius;
		//Account for the fact that we want uv coords not pixel coords
		Ray viewRay = camera.GetRay(s / RenderOptions.Width, t / RenderOptions.Height);

		if (!GraphicsValidator.CheckVectorNormalized(viewRay.Direction))
		{
			Vector3 wrongDir = viewRay.Direction;
			float   wrongMag = wrongDir.Length();

			viewRay = viewRay.WithNormalizedDirection();

			Vector3 correctDir = viewRay.Direction;
			float   correctMag = correctDir.Length();

			Log.Warning(
					"Camera initial view ray direction had incorrect magnitude, fixing. Correcting {WrongDirection} ({WrongMagnitude})	=>	{CorrectedDirection} ({CorrectedMagnitude}). Coords: {UvCoords} ({PixelCoords}). Camera: {@Camera}",
					wrongDir, wrongMag, correctDir, correctMag, (s, t), (x, y), camera
			);
			GraphicsValidator.RecordError(GraphicsErrorType.RayDirectionWrongMagnitude, camera);
		}

		//Switch depending on how we want to view the scene
		//Only if we don't have visualisations do we render the scene normally.
		if (RenderOptions.DebugVisualisation == GraphicsDebugVisualisation.None)
			return CalculateRayColourLooped(viewRay);

		//`CalculateRayColourLooped` will do the intersection code for us, so if we're not using it we have to manually check
		//Note that these visualisations will not 'bounce' off the scene objects, only the first hit is counted
		if (TryFindClosestHit(viewRay, RenderOptions.KMin, RenderOptions.KMax) is var (sceneObject, hit))
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
				case GraphicsDebugVisualisation.Depth:
				{
					//I have several ways for displaying the depth
					//Changing `a` affects how steep the curve is. Higher values cause a faster drop off
					//Have to ensure it's >0 or else all functions return 1
					// ReSharper disable once UnusedVariable
					const float a = .200f;
					// ReSharper disable once JoinDeclarationAndInitializer
					float val;
					float z = hit.K - RenderOptions.KMin;

					val = z / (RenderOptions.KMax - RenderOptions.KMin); //Inverse lerp k to [0..1]. Doesn't work when KMax is large (especially infinity)
					// val   = MathF.Pow(MathF.E, -a * z);                    //Exponential
					// val   = 1f / ((a * z) + 1);                            //Reciprocal X. Get around asymptote by treating KMin as 0, and starting at x=1
					// val   = 1 - (MathF.Atan(a * z) * (2f / MathF.PI));     //Inverse Tan
					// val = MathF.Pow(MathF.E, -(a * z * z)); //Bell Curve
					return new Colour(val);
				}
				//Debug texture based on X/Y pixel coordinates
				case GraphicsDebugVisualisation.PixelCoordDebugTexture:
					return MathF.Sin(x / 40f) * MathF.Sin(y / 40f) < 0 ? Colour.Black : Colour.Purple;
				case GraphicsDebugVisualisation.LocalCoordDebugTexture:
					return MathF.Sin(hit.LocalPoint.X  *40f) * MathF.Sin(hit.LocalPoint.Y *40f) * MathF.Sin(hit.LocalPoint.Z *40f) < 0 ? Colour.Black : Colour.Purple;
				case GraphicsDebugVisualisation.ScatterDirection:
				{
					//Convert vector values [-1..1] to [0..1]
					Vector3 scat = sceneObject.Material.Scatter(hit)?.Direction ?? -Vector3.One;
					Vector3 n    = (scat + Vector3.One) / 2f;
					return (Colour)n;
				}
				case GraphicsDebugVisualisation.None:
					break; //Shouldn't get to here
				case GraphicsDebugVisualisation.UVCoords:
					return new Colour(hit.UV.X, hit.UV.Y, 1);
				default:
					throw new ArgumentOutOfRangeException(nameof(RenderOptions.DebugVisualisation), RenderOptions.DebugVisualisation, "Wrong enum value");
			}

		//No object was intersected with, return black
		return Colour.Black;
	}

	private Colour CalculateRayColourLooped(Ray ray)
	{
		//Reusing pools from ArrayPool should reduce memory (I was using `new Stack<...>()` before, which I'm sure isn't a good idea
		//This stores the hit information, as well as what object was intersected with (at that hit)
		(SceneObject Object, HitRecord Hit)[] materialHitArray = ArrayPool<(SceneObject, HitRecord)>.Shared.Rent(RenderOptions.MaxDepth + 1);
		Colour                                finalColour      = Colour.Black;
		//Loop for a max number of times equal to the depth
		//And map out the ray path (don't do any colours yet)
		int depth;
		for (depth = 0; depth < RenderOptions.MaxDepth; depth++)
		{
			Interlocked.Increment(ref RenderStats.RayCount);
			if (TryFindClosestHit(ray, RenderOptions.KMin, RenderOptions.KMax) is var (sceneObject, maybeHit))
			{
				HitRecord hit = maybeHit;
				//See if the material scatters the ray
				Ray? maybeNewRay = sceneObject.Material.Scatter(hit);

				if (maybeNewRay is null)
				{
					//If the new ray is null, the material did not scatter (completely absorbed the light)
					//So it's impossible to have any future bounces, so quit the loop
					Interlocked.Increment(ref RenderStats.MaterialAbsorbedCount);
					finalColour = Colour.Black;
					break;
				}
				else
				{
					//Otherwise, the material scattered, creating a new ray, so calculate the future bounces recursively
					ray = (Ray)maybeNewRay;
					Interlocked.Increment(ref RenderStats.MaterialScatterCount);

					if (!GraphicsValidator.CheckVectorNormalized(ray.Direction))
					{
						Vector3 wrongDir = ray.Direction;
						float   wrongMag = wrongDir.Length();

						Vector3 correctDir = Vector3.Normalize(ray.Direction);
						float   correctMag = correctDir.Length();

						Log.Warning(
								"Material scatter ray direction had incorrect magnitude, fixing. Correcting {WrongDirection} ({WrongMagnitude})	=>	{CorrectedDirection} ({CorrectedMagnitude}). Ray: {Ray} HitRecord: {HitRecord}. Material: {@Material}",
								wrongDir, wrongMag, correctDir, correctMag, ray, hit, sceneObject.Material
						);
						GraphicsValidator.RecordError(GraphicsErrorType.RayDirectionWrongMagnitude, sceneObject.Material);

						ray = ray with { Direction = correctDir };
					}

					materialHitArray[depth] = (sceneObject, hit);
				}
			}
			//No object was hit (at least not in the range), so return the skybox colour
			else
			{
				Interlocked.Increment(ref RenderStats.SkyRays);
				finalColour = skybox.GetSkyColour(ray);
				if (!RenderOptions.HdrEnabled && !GraphicsValidator.CheckColourValid(finalColour))
				{
					Colour correctColour = Colour.Clamp(finalColour, Colour.Black, Colour.White);

					Log.Warning(
							"Skybox colour was out of range, fixing. Correcting {WrongColour}	=>	{CorrectedColour}. Ray: {Ray}. SkyBox: {SkyBox}",
							finalColour, correctColour, ray, skybox
					);
					GraphicsValidator.RecordError(GraphicsErrorType.ColourChannelOutOfRange, skybox);
				}

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
			//Make a copy of the final colour and let the lights and the material do their calculations
			Colour colour = finalColour;
			(SceneObject sceneObject, HitRecord hit) = materialHitArray[depth];
			ArraySegment<(SceneObject sceneObject, HitRecord hitRecord)> prevHits = new(materialHitArray, 0, depth); //Shouldn't include the current hit
			//This makes the lights have less of an effect the deeper they are
			//I find this makes dark scenes a little less noisy (especially cornell box), and makes it so that scenes don't get super bright when you render with a high depth
			//(Because otherwise the `+=lightColour` would just drown out the actual material's reflections colour after a few hundred bounces
			float depthScalar                              = 3f / (depth + 3);
			for (int i = 0; i < lights.Length; i++) colour += lights[i].CalculateLight(hit) * depthScalar;
			sceneObject.Material.DoColourThings(ref colour, hit, prevHits);

			//Now we have to check that the colour's in the SDR range (assuming that we don't have HDR enabled)
			if (!RenderOptions.HdrEnabled && !GraphicsValidator.CheckColourValid(colour))
			{
				Colour correctColour = Colour.Clamp(colour, Colour.Black, Colour.White);

				// Log.Warning("Material modified colour was out of range, fixing. Correcting {WrongColour}	=>	{CorrectedColour}. HitRecord: {HitRecord}. Material: {Material}", finalColour, colour, hit, sceneObject);
				colour = correctColour;
				GraphicsValidator.RecordError(GraphicsErrorType.ColourChannelOutOfRange, sceneObject);
			}

			finalColour = colour;
		}

		ArrayPool<(SceneObject, HitRecord)>.Shared.Return(materialHitArray);

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
					material.DoColourThings(ref colour, hit);
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
	public bool AnyIntersectionFast(Ray ray, float kMin, float kMax)
	{
		return TryFindClosestHit(ray, kMin, kMax) is not null;
		return bvhTree.RootNode.FastTryHit(ray, kMin, kMax);
	}

	/// <summary>
	///  Finds the closest intersection along a given <paramref name="ray"/>
	/// </summary>
	/// <param name="ray">The ray to check for intersections</param>
	/// <param name="kMin">Lower bound for K along the ray</param>
	/// <param name="kMax">Upper bound for K along the ray</param>
	public (SceneObject Object, HitRecord HitRecord)? TryFindClosestHit(Ray ray, float kMin, float kMax)
	{
		//I love how simple this is
		//Like I just need to validate the result and that's it
		(SceneObject Object, HitRecord Hit)? maybeHit = bvhTree.TryHit(ray, kMin, kMax);
		if (maybeHit is not var (obj, hit)) return null;
		if (!GraphicsValidator.CheckVectorNormalized(hit.Normal))
		{
			Vector3 wrongNormal = hit.Normal;
			float   wrongMag    = wrongNormal.Length();

			ray = ray.WithNormalizedDirection();

			Vector3 correctNormal = ray.Direction;
			float   correctMag    = correctNormal.Length();

			Log.Warning(
					"HitRecord normal had incorrect magnitude, fixing. Correcting {WrongNormal} ({WrongMagnitude})	=>	{CorrectedNormal} ({CorrectedMagnitude}). Hit: {HitRecord}. Hittable: {Material}",
					wrongNormal, wrongMag, correctNormal, correctMag, hit, obj.Hittable
			);
			GraphicsValidator.RecordError(GraphicsErrorType.NormalsWrongMagnitude, obj.Hittable);
		}

		if (!GraphicsValidator.CheckUVCoordValid(hit.UV))
		{
			Vector2 wrongUv     = hit.UV;
			Vector2 correctedUv = Vector2.Clamp(hit.UV, Vector2.Zero, Vector2.One);

			Log.Warning(
					"HitRecord UV was out of range, fixing. Correcting {WrongUV}	=>	{CorrectedUV}. Hit: {HitRecord}. Hittable: {Material}",
					wrongUv, correctedUv, hit, obj.Hittable
			);
			GraphicsValidator.RecordError(GraphicsErrorType.UVInvalid, obj.Hittable);
		}

		if (!GraphicsValidator.CheckValueRange(hit.K, kMin, kMax))
		{
			Log.Error(
					"Hittable K value was not in correct range, skipping object. K Value: {Value}, valid range is [{KMin}..{KMax}]. HitRecord: {@HitRecord}. Hittable: {@Hittable}",
					hit.K, kMin, kMax, hit, obj.Hittable
			);
			GraphicsValidator.RecordError(GraphicsErrorType.KValueNotInRange, obj.Hittable);
			return null; //Skip because we can't consider it valid
		}

		return (obj, hit);
	}

	/// <summary>
	///  Updates the pixel and colour buffers for the pixel at (<paramref name="x"/>, <paramref name="y"/>), on the basis that that pixel was just rendered
	///  with a <paramref name="colour"/>, and the average needs to be updated
	/// </summary>
	/// <param name="x">X coordinate for the pixel (camera coords, left to right)</param>
	/// <param name="y">Y coordinate for the pixel (camera coords, bottom to top)</param>
	/// <param name="colour">Colour that the pixel was just rendered as</param>
	private void UpdateBuffers(int x, int y, Colour colour)
	{
		//We have to flip the y- value because the camera expects y=0 to be the bottom (cause UV coords)
		//But the image expects it to be at the top (Graphics APIs amirite?)
		//The (X,Y) we're given is camera coords
		y = RenderOptions.Height - y - 1;

		//NOTE: Although this may not be 'thread-safe' at first glance, we don't actually need to lock to safely access and change to array
		//Although multiple threads will be rendering and changing pixels, two passes can never render at the same time (see RenderInternal)
		//Passes (and pixels) are rendered sequentially, so there is no chance of a pixel being accessed by multiple threads at the same time.
		//In previous profiles, locking was approximately 65% of the total time spent updating, with 78% of the time being this method call
		int i = Compress2DIndex(x, y, RenderOptions.Width);
		#if DEBUG_IGNORE_BUFFER_PREVIOUS
		sampleCountBuffer[i] = 1;
		rawColourBuffer[i]   = colour;
		//Have to clamp the colour here or we get funky things in the image later
		//Sqrt for gamma=2 correction
		ImageBuffer[x, y] = (Rgb24)Colour.Sqrt(Colour.Clamp01(colour));
		#else
		sampleCountBuffer[i]++;
		rawColourBuffer[i] += colour;
		//Have to clamp the colour here or we get funky things in the image later
		ImageBuffer[x, y] = (Rgb24)Colour.Sqrt(Colour.Clamp01(rawColourBuffer[i] / sampleCountBuffer[i]));
		#endif
	}

#region Internal state

	private readonly Camera        camera;
	private readonly SkyBox        skybox;
	private readonly SceneObject[] objects;
	private readonly Light[]       lights;
	private readonly BvhTree       bvhTree;

	/// <summary>
	///  Stopwatch used to time how long has elapsed since the rendering started
	/// </summary>
	public Stopwatch Stopwatch { get; } = new();

	/// <summary>
	///  Options used to render
	/// </summary>
	public RenderOptions RenderOptions { get; }

	/// <summary>
	///  Raw buffer containing denormalized colour values ([0..SampleCount])
	/// </summary>
	private readonly Colour[] rawColourBuffer;

	/// <summary>
	///  Buffer recording how many samples make up a given pixel, to create averages
	/// </summary>
	private readonly int[] sampleCountBuffer;

	/// <summary>
	///  Image buffer for the output image
	/// </summary>
	public Image<Rgb24> ImageBuffer { get; }

	/// <summary>
	///  The scene that is being rendered
	/// </summary>
	public Scene Scene { get; }

	/// <summary>
	///  Object containing statistics about the render, e.g. how many pixels have been rendered.
	/// </summary>
	public RenderStats RenderStats { get; }

#endregion

#region Async and Task-like awaitable implementation

	/// <summary>
	///  Whether this render job has completed rendering
	/// </summary>
	public bool RenderCompleted => RenderTask.IsCompleted;

	/// <summary>
	///  A <see cref="Task"/> that can be used to <see langword="await"/> the render job.
	/// </summary>
	// ReSharper disable once MemberCanBePrivate.Global
	public readonly Task RenderTask;

	/// <summary>
	///  Gets the task awaiter for this instance
	/// </summary>
	public TaskAwaiter GetAwaiter() => RenderTask.GetAwaiter();

	/// <summary>
	///  Starts the render asynchronously and returns the task that can be used to await it.
	/// </summary>
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

	/// <summary>
	///  Cancellation token used to cancel the render job
	/// </summary>
	private CancellationToken cancellationToken = CancellationToken.None;

	/// <summary>
	///  Special fancy threadsafe flag for if render has been started yet
	/// </summary>
	private object? started = null;


	/// <inheritdoc/>
	public void Dispose()
	{
		RenderTask.Dispose();
	}

#endregion
}