namespace RayTracer.Core;

/// <summary>
///  Interface defining an object that requires access to a renderer
/// </summary>
public interface IRenderAccessor
{
	/// <summary>
	///  Accessor for the current renderer
	/// </summary>
	public AsyncRenderJob Renderer { get; }

	/// <summary>
	/// Sets the renderer instance for access via <see cref="get_Renderer"/>
	/// </summary>
	/// <param name="renderer">Renderer to assign</param>
	/// <remarks>
	///  Although there is a <c>set</c> accessor, it shouldn't be used outside of <see cref="AsyncRenderJob"/> (hence being marked obsolete)
	/// </remarks>
	[Obsolete($"Setting the renderer should not be done manually outside of {nameof(AsyncRenderJob)}")]
	internal void SetRenderer(AsyncRenderJob renderer);
}