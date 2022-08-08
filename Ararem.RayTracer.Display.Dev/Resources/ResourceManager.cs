using Eto.Drawing;
using static Serilog.Log;

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
			Verbose("App icon (png) path: ({Path})", iconPathPng);
			AppIconPng    = Icon.FromResource(iconPathPng);
			AppIconPng.ID = "App Icon";
			Verbose("App icon (png) loaded");
		}
		Debug("Initialised Resource Manager");
	}

	public static Icon AppIconPng { get; }
}