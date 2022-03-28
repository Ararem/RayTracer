using JetBrains.Annotations;
using RayTracer.Core.Debugging;
using RayTracer.Core.Environment;
using RayTracer.Core.Hittables;
using RayTracer.Core.Materials;
using RayTracer.Core.Scenes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static RayTracer.Core.MathHelper;

namespace RayTracer.Core.Graphics;

/// <summary>
///  Class for rendering a <see cref="Scene"/>, using it's <see cref="Scenes.Scene.Camera"/>.
/// </summary>
/// <remarks>
///  Uses the rays generated by the <see cref="Camera"/>, and objects in the <see cref="Scene"/> to create the output image
/// </remarks>
public sealed class AsyncRenderJob
{
	//TODO: Allow cancelling of the render job partway through
	/// <summary>
	///  Creates an async render job for a <paramref name="scene"/>, with configurable <paramref name="renderOptions"/>
	/// </summary>
	/// <param name="scene">The scene containing the objects and camera for the render</param>
	/// <param name="renderOptions">
	///  Record containing options that affect how the resulting image is produced, such as resolution, multisample count or debug
	///  visualisations
	/// </param>
	public AsyncRenderJob(Scene scene, RenderOptions renderOptions)
	{
		ArgumentNullException.ThrowIfNull(scene);
		ArgumentNullException.ThrowIfNull(renderOptions);
		ImageBuffer                  = new Image<Rgb24>(renderOptions.Width, renderOptions.Height);
		RenderOptions                = renderOptions;
		rawColourBuffer              = new Colour[renderOptions.Width * renderOptions.Height];
		sampleCountBuffer            = new int[renderOptions.Width    * renderOptions.Height];
		taskCompletionSource         = new TaskCompletionSource<Image<Rgb24>>(this);
		TotalRawPixels               = (ulong)RenderOptions.Width * (ulong)RenderOptions.Height * (ulong)RenderOptions.Passes;
		TotalTruePixels              = RenderOptions.Width        * RenderOptions.Height;
		(_, camera, objects, skybox) = scene;
		Scene                        = scene;
		Stopwatch                    = new Stopwatch();
		rawRayDepthCounts            = new ulong[renderOptions.MaxDepth + 1]; //+1 because we can also have 0 bounces

		Task.Run(RenderInternal);
	}

	private void RenderInternal()
	{
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
					0, TotalTruePixels,
					new ParallelOptions { MaxDegreeOfParallelism = RenderOptions.ConcurrencyLevel },
					() => this, //Gives us the state tracking the `this` reference and which pass we're in
					static (i, _, state) =>
					{
						Interlocked.Increment(ref state.threadsRunning);
						(int x, int y) = Decompress2DIndex(i, state.RenderOptions.Width);
						Colour col = state.RenderPixelWithVisualisations(x, y);
						state.UpdateBuffers(x, y, col);
						Interlocked.Increment(ref state.rawPixelsRendered);
						Interlocked.Decrement(ref state.threadsRunning);
						return state;
					},
					static _ => { }
			);

			Interlocked.Increment(ref passesRendered);
		}

		//Notify that the render is complete
		taskCompletionSource.SetResult(ImageBuffer);
		Stopwatch.Stop();
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
		//Get the view ray from the camera
		Ray ray;
		{
			//To create some 'antialiasing' (SSAA maybe?), add a slight random offset to the uv coords
			float s = x, t = y;
			//Add up to 1 pixel of offset randomness to the coords
			const float ssaaRadius = 1f;
			s += Rand.RandomPlusMinusOne() * ssaaRadius;
			t += Rand.RandomPlusMinusOne() * ssaaRadius;
			//Account for the fact that we want uv coords not pixel coords
			ray = camera.GetRay(s / RenderOptions.Width, t / RenderOptions.Height);
		}

		//Switch depending on how we want to view the scene
		//Only if we don't have visualisations do we render the scene normally.
		if (RenderOptions.DebugVisualisation == GraphicsDebugVisualisation.None) return CalculateRayColourRecursive(ray, 0);

		//`CalculateRayColourRecursive` will do the intersection code for us, so if we're not using it we have to manually check
		//Note that these visualisations will not 'bounce' off the scene objects.
		if (TryFindClosestHit(ray, out HitRecord? maybeHit, out Material? maybeMaterial))
		{
			HitRecord hit = (HitRecord)maybeHit!;
			// ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
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
					const float a = .200f;
					// ReSharper disable once JoinDeclarationAndInitializer
					float t;
					float z = hit.K - RenderOptions.KMin;

					// t   = z / (RenderOptions.KMax - RenderOptions.KMin); //Inverse lerp k to [0..1]. Doesn't work when KMax is large (especially infinity
					// t   = MathF.Pow(MathF.E, -a * z);                    //Exponential
					// t   = 1f / ((a * z) + 1);                            //Reciprocal X. Get around asymptote by treating KMin as 0, and starting at x=1
					// t   = 1 - (MathF.Atan(a * z) * (2f / MathF.PI));     //Inverse Tan
					t = MathF.Pow(MathF.E, -(a * z * z)); //Bell Curve
					return new Colour(t);
				}
				//Debug texture based on X/Y pixel coordinates
				case GraphicsDebugVisualisation.PixelCoordDebugTexture:
					return MathF.Sin(x / 40f) * MathF.Sin(y / 20f) < 0 ? Colour.Black : Colour.Blue + Colour.Red;
				case GraphicsDebugVisualisation.ScatterDirection:
					if (maybeMaterial is { } mat)
					{
						//Convert vector values [-1..1] to [0..1]
						Vector3 scat = mat.Scatter((HitRecord)maybeHit)?.Direction ?? -Vector3.One;
						Vector3 n    = (scat + Vector3.One) / 2f;
						return (Colour)n;
					}
					else
					{
						return Colour.Black;
					}
			}
		}

		//No object was intersected with, return black
		return Colour.Black;
	}

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

	[ContractAnnotation("=> true, maybeHit: notnull, material: notnull; => false, maybeHit: null, material:null")]
	private bool TryFindClosestHit(Ray ray, out HitRecord? maybeHit, out Material? material)
	{
		//TODO: Optimize in the future with BVH nodes or something. Probably don't need to bother putting this into the scene, just store it locally in the camera when ctor is called

		(SceneObject obj, HitRecord hit)? maybeClosest = null;
		float                             kMin         = RenderOptions.KMin;
		float                             kMax         = RenderOptions.KMax;
		foreach (SceneObject obj in objects)
		{
			//Try and hit the object
			maybeHit = obj.Hittable.TryHit(ray, kMin, kMax);
			//No point continuing if there was no hit
			if (maybeHit is not { } hit) continue;

			GraphicsValidator.CheckNormalMagnitude(ref hit, obj.Hittable);
			GraphicsValidator.CheckKValueRange(ref hit, RenderOptions, obj.Hittable);
			//If it's the first hit, or it's closer, update the variable
			if (maybeClosest is not var (oldObj, oldHit)) //Check first hit (because it's null)
			{
				maybeClosest = (obj, hit);
				continue;
			}

			float currentK = oldHit.K;
			float newK     = hit.K;
			if (newK < currentK) //Check if closer
				maybeClosest = (obj, hit);

			//This shouldn't happen, but just a little debug check to make sure nothing funky happens if the K's are the (exact) same
			//Because I'm using `<` instead of `<=`, this means that the object we first iterate will be prioritized if multiple hittables have the same distance values
			//(this would happen when several objects overlap exactly at a certain point)
			//Also due to the control flow, we know that the 'old' object is stored as `closest.obj`, and the 'current' is `obj`
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			else if (currentK == newK) GraphicsValidator.RecordError(GraphicsErrorType.ZFighting, (Old: oldObj, New: obj));
		}

		//If we hit anything, set the variables, otherwise make them null
		if (maybeClosest is var (sceneObject, hitRecord))
		{
			maybeHit = hitRecord;
			material = sceneObject.Material;
			return true;
		}
		else
		{
			material = null;
			maybeHit = null;
			return false;
		}
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
		sampleCountBuffer[i]++;
		rawColourBuffer[i] += colour;
		ImageBuffer[x, y]  =  (Rgb24)(rawColourBuffer[i] / sampleCountBuffer[i]);
	}

#region Internal state

	private readonly Camera        camera;
	private readonly SkyBox        skybox;
	private readonly SceneObject[] objects;

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

#endregion

#region Statistics

	/// <summary>
	///  How many pixels have been rendered, including multisampled pixels
	/// </summary>
	public ulong RawPixelsRendered => rawPixelsRendered;

	private ulong rawPixelsRendered = 0;

	private int passesRendered = 0;

	/// <summary>
	///  How many passes have been rendered
	/// </summary>
	public int PassesRendered => passesRendered;

	private ulong raysScattered = 0;

	/// <summary>
	///  How many rays were scattered in the scene
	/// </summary>
	public ulong RaysScattered => raysScattered;

	private ulong raysAbsorbed = 0;

	/// <summary>
	///  How many rays were absorbed in the scene
	/// </summary>
	public ulong RaysAbsorbed => raysAbsorbed;

	private ulong skyRays = 0;

	/// <summary>
	///  How many rays did not hit any objects, and hit the sky
	/// </summary>
	public ulong SkyRays => skyRays;

	/// <summary>
	///  How many 'raw' pixels need to be rendered (including multisampled pixels)
	/// </summary>
	public ulong TotalRawPixels { get; }

	/// <summary>
	///  How many 'true' pixels need to be rendered (not including multisampling)
	/// </summary>
	public int TotalTruePixels { get; }

	private ulong bounceLimitExceeded = 0;

	/// <summary>
	///  How times a ray was not rendered because the bounce count for that ray exceeded the limit specified by
	///  <see cref="Graphics.RenderOptions.MaxDepth"/>
	/// </summary>
	public ulong BounceLimitExceeded => bounceLimitExceeded;

	private ulong rayCount = 0;

	/// <summary>
	///  How many rays were rendered so far (scattered, absorbed, etc)
	/// </summary>
	public ulong RayCount => rayCount;

	/// <summary>
	///  Stopwatch used to time how long has elapsed since the rendering started
	/// </summary>
	public Stopwatch Stopwatch { get; }

	private ulong[] rawRayDepthCounts;

	/// <summary>
	///  A list that contains the number of times a ray 'finished' at a certain depth. The depth corresponds to the index, where [0] is no bounces, [1] is 1
	///  bounce, etc.
	/// </summary>
	public IReadOnlyList<ulong> RawRayDepthCounts => rawRayDepthCounts;

	/// <summary>
	///  How many threads are currently rendering pixels
	/// </summary>
	public int ThreadsRunning => threadsRunning;

	private int threadsRunning;

#endregion

#region Task-like awaitable implementation

	/// <summary>
	///  Whether this render job has completed rendering
	/// </summary>
	public bool RenderCompleted => taskCompletionSource.Task.IsCompleted;

	/// <summary>
	///  Internal object used for task-like awaiting
	/// </summary>
	private readonly TaskCompletionSource<Image<Rgb24>> taskCompletionSource;

	/// <summary>
	///  Gets the task awaiter for this instance
	/// </summary>
	public TaskAwaiter<Image<Rgb24>> GetAwaiter() => taskCompletionSource.Task.GetAwaiter();

#endregion
}