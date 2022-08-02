using Eto.Drawing;

namespace Ararem.RayTracer.Display.Dev.Resources;

public static class ResourceManager
{
	static ResourceManager()
	{
		Debug("Initialising Resource Manager");
		// Assembly resourceAssembly = Assembly.GetExecutingAssembly();
		// Debug("Assembly for resources is {Assembly}", resourceAssembly);
		{
			const string iconPathPng = "Ararem.RayTracer.Display.Dev.Resources.icon.png";
			AppIconPng    = Icon.FromResource(iconPathPng);
			AppIconPng.ID = "App Icon";
			Verbose("App Icon (Png) ({Path}): {Icon}", iconPathPng, AppIconPng );
		}
		Debug("Initialised Resource Manager");
	}
	public static Icon AppIconPng { get; }
}