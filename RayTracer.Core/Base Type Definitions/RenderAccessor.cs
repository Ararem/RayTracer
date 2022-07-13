using JetBrains.Annotations;

namespace RayTracer.Core;

/// <summary>
///  Base class defining an object that requires access to a <see cref="AsyncRenderJob">renderer</see>. You probably shouldn't be implementing this
///  directly, as the implementation and usage only works with internal classes
/// </summary>
[PublicAPI]
public abstract class RenderAccessor
{
	/// <summary>Accessor for the current renderer</summary>
	/// <remarks>
	///  <b>
	///   <i>
	///    Although there is a <c>set</c> accessor, it shouldn't be used outside of <see cref="AsyncRenderJob"/> (hence being marked obsolete). Don't use
	///    this please unless you have to, I'm planning on redoing this but I haven't gotten around to it
	///   </i>
	///  </b>
	/// </remarks>
	public virtual AsyncRenderJob Renderer
	{
		get;
		[Obsolete($"Setting the renderer should not be done manually outside of {nameof(AsyncRenderJob)}")]
		set;
	} = null!; //Set from AsyncRenderJob
}