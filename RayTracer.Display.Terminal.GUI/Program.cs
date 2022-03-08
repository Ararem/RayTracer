using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using RayTracer.Display.Terminal.GUI;
using Terminal.Gui;
using static Terminal.Gui.Dim;

//Init the application so it's ready to run
Application.Init();

Toplevel top = Application.Top;

//First we have to confirm which render settings we have
SettingsConfirmer confirmer = new();
Application.Run(confirmer);
RenderOptions renderOptions = confirmer.Options;
Scene         scene         = confirmer.Scene;


top.MenuBar = new MenuBar(new[] { new MenuBarItem("Title of this menu bar item", "Help text for this menu item", () => MessageBox.Query("MessageBox Title", "Message", "Button1", "Button2")) });
top.Add(top.MenuBar);

Window win = new("MyAppTitle")
{
		X     = 0, Y           = 0,
		Width = Fill(), Height = Fill()
};
top.Add(win);
Application.Run<SettingsConfirmer>();

Application.Shutdown();


#region Implementations

#endregion