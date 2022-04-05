using Eto.Drawing;
using Eto.Forms;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using System;
using System.Reflection;
using System.Threading.Tasks;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms;

public sealed class MainForm : Form
{
	private readonly RenderOptionSelectorPanel? selectorPanel = null;
	private readonly Label                      titleLabel;
	private          AsyncRenderJob?            renderJob = null;

	public MainForm()
	{
		Verbose("MainForm.Ctor()");

		string title = $"RayTracer.Display - v{Assembly.GetEntryAssembly()!.GetName().Version}";
		Title = title;
		Verbose("Title is {Title}", Title);
		MinimumSize = new Size(200, 200);
		Verbose("Minimum app size set to {MinSize}", MinimumSize);
		Padding = 10;

		Verbose("Creating UI elements");
		titleLabel = new Label { Text = title, Font = new Font(FontFamilies.Sans!, 32f, FontStyle.Bold) };
		StackLayoutItem displayedWindowItem = new() { HorizontalAlignment = HorizontalAlignment.Stretch, Expand = true };
		StackLayoutItem titleItem           = new(titleLabel, HorizontalAlignment.Center);
		Content = new StackLayout
		{
				Items   = { titleItem, displayedWindowItem },
				Spacing = 10
		};


		Verbose("Generating commands");
		Command quitCommand = new() { MenuText = "Quit", Shortcut = Application.Instance!.CommonModifier | Keys.Q };
		quitCommand.Executed += (_, _) => Application.Instance.Quit();

		Command aboutCommand = new() { MenuText = "About..." };
		aboutCommand.Executed += (_, _) => new AboutDialog().ShowDialog(this);

		// create menu
		Verbose("Creating menu bar");
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
		const string iconPath = "RayTracer.Display.EtoForms.Appearance.icon.png";
		Verbose("Loading and setting icon from {IconPath}", iconPath);
		Icon = Icon.FromResource(iconPath);

		Verbose("Toolbar disabled");
		ToolBar = null; //new ToolBar { Items = { clickMe } };

		//We start off with an options selection panel, then once the user clicks the 'continue' button, we start the render and change it to the render progress panel
		Verbose("Initializing render options selector subview");
		selectorPanel               = new RenderOptionSelectorPanel((_, _) =>  StartRenderButtonClicked());
		displayedWindowItem.Control = selectorPanel;
	}

	//
	private void StartRenderButtonClicked()
	{
		//Assume that the sender is the same selector panel we have stored
		//Might be bad practice but hey who cares it's easier
		RenderOptions options = selectorPanel!.RenderOptions;
		Scene         scene   = selectorPanel.Scene;

		Information("Render start button clicked");
		Debug("Scene is {Scene}, Options are {Options}", scene, options);

		Debug("Creating render job");
		renderJob = new AsyncRenderJob(scene, options);
		Debug("Starting render job");
		Task renderTask = renderJob.TryStartAsync();
		TaskWatcher.Watch(renderTask, true);

		//Create the display panel
		//TODO: Honestly this is a really bad way to do it and I don't like it, but for some reason removing the children from the stack panel
		// does not work (some weird logical child not equal behaviour) so i gotta create a new layout instead :(
		Verbose("Creating render progress display panel");
		RenderProgressDisplayPanel displayPanel = new(renderJob);
		StackLayoutItem            titleItem    = new(titleLabel, HorizontalAlignment.Center);
		Content = new StackLayout
		{
				Items   = { titleItem, displayPanel },
				Spacing = 10
		};
	}
}