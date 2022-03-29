using Eto.Drawing;
using Eto.Forms;

namespace RayTracer.Display.EtoForms;

public sealed class MainForm : Form
{
	public MainForm()
	{
		Title       = $"RayTracer v{typeof(Core.Scenes.Scene).Assembly.GetName().Version} - Render Selector - Eto.Forms";
		MinimumSize = new Size(200,  200);
		Size        = new Size(1280, 720);

		Content = new StackLayout
		{
				Padding = 0,
				Items =
				{
						new Label{Text = "I'ma text box"},
						"Hello World!",
						"Testing"
						// add more controls here
				}
		}; 

		// create a few commands that can be used for the menu and toolbar
		Command quitCommand = new() { MenuText = "Quit", Shortcut = Application.Instance!.CommonModifier | Keys.Q };
		quitCommand.Executed += (sender, e) => Application.Instance.Quit();

		Command aboutCommand = new() { MenuText = "About" };
		aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

		// create menu
		Menu = new MenuBar
		{
				Items =
				{
						// File submenu
						// new SubMenuItem { Text = "&File", Items = {  } },
						// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
						// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
				ApplicationItems =
				{
						// application (OS X) or file menu (others)
						// new ButtonMenuItem { Text = "&Preferences..." }
				},
				QuitItem  = quitCommand,
				AboutItem = aboutCommand
		};

		ToolBar = new ToolBar { Items = {  } };
	}
}