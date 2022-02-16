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
using static RayTracer.Core.Graphics.GraphicsHelper;
using static System.Threading.Interlocked;

namespace RayTracer.Core.Graphics;

/// <summary>
///  Class for rendering a <see cref="Scene"/>, using it's <see cref="Scenes.Scene.Camera"/>.
/// </summary>
/// <remarks>
///  Uses the rays generated by the <see cref="Camera"/>, and objects in the <see cref="Scene"/> to create the output image
/// </remarks>
[UsedImplicitly(ImplicitUseKindFlags.Access, ImplicitUseTargetFlags.WithMembers)]
public sealed class AsyncRenderJob
{
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
		this.renderOptions           = renderOptions;
		rawColourBuffer              = new Colour[renderOptions.Width * renderOptions.Height];
		sampleCountBuffer            = new int[renderOptions.Width    * renderOptions.Height];
		taskCompletionSource         = new TaskCompletionSource<Image<Rgb24>>(this);
		TotalRawPixels               = (ulong)this.renderOptions.Width * (ulong)this.renderOptions.Height * (ulong)this.renderOptions.Samples;
		TotalTruePixels              = (ulong)this.renderOptions.Width * (ulong)this.renderOptions.Height;
		(_, camera, objects, skybox) = scene;
		Scene                        = scene;
		Stopwatch                    = Stopwatch.StartNew();
		Task.Run(RenderInternal);
	}

	private void RenderInternal()
	{
		/*
		 * Due to how i've internally implemented the buffers and functions, it doesn't matter what order the pixels are rendered in
		 * It doesn't even matter if some pixels are rendered with different sample counts, since i'm using a multi-buffer approach to store the averaging data
		 * I'm doing a x->y->s nested loop approach, but you could also have `s` as the outer loop, or even render the pixels completely randomly..?????
		 * Also makes it easy to turn it into an async method with `System.Threading.Parallel`
		 */
		for (int x = 0; x < renderOptions.Width; x++)
		{
			for (int y = 0; y < renderOptions.Height; y++)
			{
				for (int s = 0; s < renderOptions.Samples; s++)
					RenderAndUpdatePixel(x, y, s);
				Increment(ref truePixelsRendered);
			}
		}

		//Notify that the render is complete
		taskCompletionSource.SetResult(ImageBuffer);
	}

	private void RenderAndUpdatePixel(int x, int y, int s)
	{
		Colour col = RenderPixel(x, y);
		UpdateBuffers(x, y, col);
		Increment(ref rawPixelsRendered);
	}

	/// <summary>
	///  Renders a single pixel with the coordinates (<paramref name="x"/>, <paramref name="y"/>).
	/// </summary>
	/// <remarks>
	///	<paramref name="x"/> and <paramref name="y"/> coords start at the lower-left corner, moving towards the upper-right.
	/// </remarks>
	/// <param name="x">X coordinate of the pixel</param>
	/// <param name="y">Y coordinate of the pixel</param>
	private Colour RenderPixel(int x, int y)
	{
		//Get the view ray from the camera
		Ray ray = camera.GetRay((float)x / renderOptions.Width, (float)y / renderOptions.Height);

		//Check camera view ray magnitude is 1
		GraphicsValidator.CheckRayDirectionMagnitude(ref ray, camera);

		//Sky colour
		Colour col = skybox.GetSkyColour(ray);

		//Loop over the objects to see if we hit anything
		foreach (SceneObject sceneObject in objects)
			if (sceneObject.Hittable.TryHit(ray, renderOptions.KMin, renderOptions.KMax) is { } hit)
			{
				GraphicsValidator.CheckNormalMagnitude(ref hit, sceneObject);
				GraphicsValidator.CheckKValueRange(ref hit, renderOptions, sceneObject);

				switch (renderOptions.DebugVisualisation)
				{
					case GraphicsDebugVisualisation.Normals:
					{
						//Convert normal values [-1..1] to [0..1]
						Vector3 n = (hit.Normal + Vector3.One) / 2f;
						col = (Colour)n;
						break;
					}
					case GraphicsDebugVisualisation.FaceDirection:
					{
						//Convert normal values [-1..1] to [0..1]
						col = hit.OutsideFace ? Colour.Green : Colour.Red;
						break;
					}
					//Render the object normally
					case GraphicsDebugVisualisation.Depth:
					{
						//I have several ways for displaying the depth
						//Changing `a` affects how steep the curve is. Higher values cause a faster drop off
						//Have to ensure it's >0 or else all functions return 1
						const float a = .200f;
						// ReSharper disable once JoinDeclarationAndInitializer
						float t;
						float z = hit.K - renderOptions.KMin;

						// t   = z / (RenderOptions.KMax - RenderOptions.KMin); //Inverse lerp k to [0..1]. Doesn't work when KMax is large (especially infinity
						// t   = MathF.Pow(MathF.E, -a * z);                    //Exponential
						// t   = 1f / ((a * z) + 1);                            //Reciprocal X. Get around asymptote by treating KMin as 0, and starting at x=1
						// t   = 1 - (MathF.Atan(a * z) * (2f / MathF.PI));     //Inverse Tan
						t   = MathF.Pow(MathF.E, -(a * z * z)); //Bell Curve
						col = new Colour(t);
						break;
					}
					//Debug texture based on X/Y pixel coordinates
					case GraphicsDebugVisualisation.PixelCoordDebugTexture:
						col = MathF.Sin(x / 40f) * MathF.Sin(y / 20f) < 0 ? Colour.Black : Colour.Blue + Colour.Red;
						break;
					case GraphicsDebugVisualisation.ScatterDirection:
						TryFindClosestHit(ray, out HitRecord? maybeHit, out MaterialBase? maybeMaterial);
						if (maybeMaterial is { } mat)
						{
							//Convert vector values [-1..1] to [0..1]
							Vector3 scat = mat.Scatter((HitRecord)maybeHit!)?.Direction ?? -Vector3.One;
							Vector3 n    = (scat + Vector3.One) / 2f;
							col = (Colour)n;
						}

						break;
					case GraphicsDebugVisualisation.None:
					default:
						col = CalculateRayColourRecursive(ray, 0); //WORLD OR LOCAL RAY PROBLEMS REFLECT
						break;
				}
			}

		return col;
	}

	/// <summary>
	///  Recursive function to calculate the given colour for a ray
	/// </summary>
	/// <param name="ray">The ray to calculate the colour from</param>
	/// <param name="bounces">
	///  The number of times the ray has bounced. If this is 0, then the ray has never bounced, and so we can assume it's the initial
	///  ray from the camera
	/// </param>
	//TODO: Try figure out if it's possible to make this non-recursive somehow. Perhaps a `while` loop or something?
	private Colour CalculateRayColourRecursive(Ray ray, int bounces)
	{
		//Ensure we don't go too deep
		if (bounces > renderOptions.MaxBounces)
		{
			Increment(ref bounceLimitExceeded);
			return Colour.Black;
		}

		//TODO: Track how many times a given depth was reached
		//Find the nearest hit along the ray
		Increment(ref rayCount);
		if (TryFindClosestHit(ray, out HitRecord? maybeHit, out MaterialBase? material))
		{
			HitRecord hit = (HitRecord)maybeHit!;
			//See if the material scatters the ray
			Ray?   maybeNewRay = material!.Scatter(hit);
			Colour futureBounces;

			if (maybeNewRay is null)
			{
				//If the new ray is null, the material did not scatter (completely absorbed the light)
				//So it's impossible to have any future bounces, so we know that they must be black
				Increment(ref raysAbsorbed);
				futureBounces = Colour.Black;
			}
			else
			{
				//Otherwise, the material scattered, creating a new ray, so calculate the future bounces recursively
				Increment(ref raysScattered);
				futureBounces = CalculateRayColourRecursive((Ray)maybeNewRay, bounces + 1);
			}

			//Tell the material to do it's lighting stuff
			//By doing it this way, we can essentially do anything, and we don't have to do much in the camera itself
			//So we can have materials that emit light, ones that amplify light, ones that change the colour of the light, anything really
			//So we pass in the colour that we obtained from the future bounces, and let the material directly modify it to get the resulting colour
			Colour colour = futureBounces;
			material.DoColourThings(ref colour, hit, bounces);
			return colour;
		}
		//No object was hit (at least not in the range), so return the skybox colour
		else
		{
			return skybox.GetSkyColour(ray);
		}
	}

	[ContractAnnotation("=> true, maybeHit: notnull, material: notnull; => false, maybeHit: null, material:null")]
	private bool TryFindClosestHit(Ray ray, out HitRecord? maybeHit, out MaterialBase? material)
	{
		//TODO: Optimize in the future with BVH nodes or something. Probably don't need to bother putting this into the scene, just store it locally in the camera when ctor is called

		(SceneObject obj, HitRecord hit)? maybeClosest = null;
		float                             kMin         = renderOptions.KMin;
		float                             kMax         = renderOptions.KMax;
		foreach (SceneObject obj in objects)
		{
			//Try and hit the object
			maybeHit = obj.Hittable.TryHit(ray, kMin, kMax);
			//No point continuing if there was no hit
			if (maybeHit is not { } hit) continue;

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
			// (this would happen when several objects overlap exactly at a certain point)
			//Also due to the control flow, we know that the 'old' object is stored as `closest.obj`, and the 'current' is `obj`
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			else if (currentK == newK) GraphicsValidator.RecordError(GraphicsErrorType.ZFighting, (obj1: oldObj, obj2: obj));
		}

		//If we hit anything, set the variables, otherwise make them null
		if (maybeClosest is var (sceneObject, hitRecord))
		{
			maybeHit = hitRecord!;
			material = sceneObject.Material!;
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
		//TODO: If we remove the `readonly` restriction on `Colour`, we can use `Interlocked` to update the values atomically, no locking required
		//We have to flip the y- value because the camera expects y=0 to be the bottom (cause UV coords)
		//But the image expects it to be at the top (Graphics APIs amirite?)
		//The (X,Y) we're given is camera coords
		y = renderOptions.Height - y - 1;

		//Lock to prevent other threads from changing our pixel
		//TODO: Maybe track how many times we failed to instantly lock? Maybe timeout of 0 and then retry if fail timeout 0
		bufferLock.EnterWriteLock();
		int i = Compress2DIndex(x, y, renderOptions.Width, renderOptions.Height);
		sampleCountBuffer[i]++;
		rawColourBuffer[i] += colour;
		ImageBuffer[x, y]  =  (Rgb24)(rawColourBuffer[i] / sampleCountBuffer[i]);
		bufferLock.ExitWriteLock();
	}

#region Internal state

	private readonly Camera        camera;
	private readonly SkyBox        skybox;
	private readonly SceneObject[] objects;
	private readonly RenderOptions renderOptions;

	//TODO: Make a better locking system than locking the entire array
	private readonly ReaderWriterLockSlim bufferLock = new();

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

	private ulong truePixelsRendered = 0;

	/// <summary>
	///  How many pixels of the final image have been actually rendered, not including multisampled pixels
	/// </summary>
	public ulong TruePixelsRendered => truePixelsRendered;

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

	/// <summary>
	///  How many rays did not hit any objects, and hit the sky
	/// </summary>
	public ulong SkyRays { get; } = 0;

	/// <summary>
	///  How many 'raw' pixels need to be rendered (including multisampled pixels)
	/// </summary>
	public ulong TotalRawPixels { get; }

	/// <summary>
	///  How many 'true' pixels need to be rendered (not including multisampling)
	/// </summary>
	public ulong TotalTruePixels { get; }

	private ulong bounceLimitExceeded = 0;

	/// <summary>
	///  How times a ray was not rendered because the bounce count for that ray exceeded the limit specified by <see cref="RenderOptions.MaxBounces"/>
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