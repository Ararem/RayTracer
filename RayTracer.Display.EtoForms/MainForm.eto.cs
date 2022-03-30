using Eto.Drawing;
using Eto.Forms;
using RayTracer.Display.EtoForms.Appearance;
using System.Reflection;

namespace RayTracer.Display.EtoForms;

sealed partial class MainForm
{
	private RenderOptionSelector selector;

	private void InitializeComponent()
	{
		string title = $"RayTracer.Display - v{Assembly.GetEntryAssembly()!.GetName().Version}";
		Title       = title;
		MinimumSize = new Size(200, 200);
		Padding     = 10;

		selector = new RenderOptionSelector();
		Content = new StackLayout
		{
				Items =
				{
						new StackLayoutItem(new Label { Text       = title, Style = KnownStyles.TitleText }, HorizontalAlignment.Center),
						new StackLayoutItem(new GroupBox { Content = selector },                             HorizontalAlignment.Stretch, true)
				}
		};

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
		Icon    = Icon.FromResource("RayTracer.Display.EtoForms.Appearance.icon.png");
		ToolBar = null; //new ToolBar { Items = { clickMe } };
	}
}