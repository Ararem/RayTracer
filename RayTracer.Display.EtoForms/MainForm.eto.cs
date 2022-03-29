using Eto.Drawing;
using Eto.Forms;

namespace RayTracer.Display.EtoForms;

sealed partial class MainForm
{
	private void InitializeComponent()
	{
		Title       = "My Eto Form";
		MinimumSize = new Size(200, 200);
		Padding     = 10;

		Content = new StackLayout
		{
				Items =
				{
						"Hello World!"
						// add more controls here
				}
		};

		// create a few commands that can be used for the menu and toolbar
		Command clickMe = new() { MenuText = "Click Me!", ToolBarText = "Click Me!" };
		clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

		Command quitCommand = new() { MenuText = "Quit", Shortcut = Application.Instance!.CommonModifier | Keys.Q };
		quitCommand.Executed += (sender, e) => Application.Instance.Quit();

		Command aboutCommand = new() { MenuText = "About..." };
		aboutCommand.Executed += (sender, e) => new AboutDialog().ShowDialog(this);

		// create menu
		Menu = new MenuBar
		{
				Items =
				{
						// File submenu
						// new SubMenuItem { Text = "&File", Items = { clickMe } }
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

		// create toolbar			
		ToolBar = new ToolBar { Items = { clickMe } };
	}
}