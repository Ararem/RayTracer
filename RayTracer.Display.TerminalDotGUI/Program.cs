using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using RayTracer.Display.TerminalDotGUI;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using System.Diagnostics;
using Terminal.Gui;

//Init the application so it's ready to run
Application.Init();

Toplevel top = Application.Top;

//First we have to confirm which render settings we have
SettingsConfirmerApp confirmerApp = new();
Application.UseSystemConsole = true; //BUG: Problem with linux (unix?) console, clicks itself
Application.Run(confirmerApp);
RenderOptions renderOptions = confirmerApp.Options;
Scene?        scene         = confirmerApp.Scene;
if (scene is null) return; //Quit if user cancelled

//Next we start up the render and display the progress
AsyncRenderJob renderJob = new(scene, renderOptions);
RenderApp      renderApp = new(renderJob);
Application.Run(renderApp);


//Finalize everything
Image<Rgb24> image = renderJob.ImageBuffer;
//Save and open the image for viewing
image.Save(File.OpenWrite("image.png"), new PngEncoder());
Process.Start(
		new ProcessStartInfo
		{
				FileName  = "gwenview",
				Arguments = "\"image.png\"",
				//These flags stop the image display program's console from attaching to ours (because that's yuck!)
				UseShellExecute        = false,
				RedirectStandardError  = true,
				RedirectStandardInput  = true,
				RedirectStandardOutput = true
		}
)!.WaitForExit();


Application.Shutdown();