using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using RayTracer.Display.TerminalDotGUI;
using Terminal.Gui;

//Init the application so it's ready to run
Application.Init();

Toplevel top = Application.Top;

//First we have to confirm which render settings we have
SettingsConfirmer confirmer = new();
Application.UseSystemConsole = true; //BUG: Problem with linux (unix?) console, clicks itself
Application.Run(confirmer);
RenderOptions renderOptions = confirmer.Options;
Scene?        scene         = confirmer.Scene;
if (scene is null) return; //Quit if user cancelled

//Next we start up the render and display the progress

Application.Shutdown();


#region Implementations

#endregion