namespace RayTracer.Core;

/// <summary>
/// Base class for a record that needs access to a renderer
/// </summary>
public abstract record RenderAccessor
{
	/// <summary>
	/// Accessor for the current rendered
	/// </summary>
	protected AsyncRenderJob Renderer { get; private set; } = null!; //Yes I'm setting it to null but it should always be set before any methods are called (except ctor)

	//Used by AsyncRenderJob to assign itself
	internal void SetRenderer(AsyncRenderJob renderer)
	{
		Renderer = renderer;
	}
}