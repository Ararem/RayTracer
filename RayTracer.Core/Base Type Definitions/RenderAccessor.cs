namespace RayTracer.Core;

/// <summary>
///  Interface defining an object that requires access to a renderer
/// </summary>
public abstract class RenderAccessor
{
	/// <summary>
	///  Accessor for the current renderer
	/// </summary>
	/// <remarks>
	///  Although there is a <c>set</c> accessor, it shouldn't be used outside of <see cref="AsyncRenderJob"/> (hence being marked obsolete)
	/// </remarks>
	public AsyncRenderJob Renderer
	{
		get;
		[Obsolete($"Setting the renderer should not be done manually outside of {nameof(AsyncRenderJob)}")]
		internal set;
	} = null!; //Set from AsyncRenderJob
}