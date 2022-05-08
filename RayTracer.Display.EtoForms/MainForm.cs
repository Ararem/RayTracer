using Eto.Drawing;
using Eto.Forms;
using RayTracer.Core;
using RayTracer.Core.Debugging;
using Serilog;
using SixLabors.ImageSharp.Formats.Png;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms;
//TODO: Add some styling - custom fonts most important
public sealed class MainForm : Form
{
	private readonly RenderOptionSelectorPanel? selectorPanel = null;
	private readonly Label                      titleLabel;
	private          StackLayoutItem            displayedWindowItem;
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
		titleLabel          = new Label { Text                          = title, Style                        = Appearance.Styles.AppTitle};
		displayedWindowItem = new StackLayoutItem { HorizontalAlignment = HorizontalAlignment.Stretch, Expand = true };
		StackLayoutItem titleItem = new(titleLabel, HorizontalAlignment.Center);
		Content = new StackLayout
		{
				Items       = { titleItem, displayedWindowItem },
				Orientation = Orientation.Vertical,
				Spacing     = 10
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
		selectorPanel               = new RenderOptionSelectorPanel((_, _) => StartRenderButtonClicked());
		displayedWindowItem.Control = selectorPanel;
	}

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
		Task renderTask = renderJob.StartOrGetRenderAsync();
		TaskWatcher.Watch(renderTask, true);

		//Create the display panel
		//HACK: Honestly this is a really bad way to do it and I don't like it, but for some reason removing the children from the stack panel
		// does not work (some weird logical child not equal behaviour) so i gotta create a new layout instead :(
		Verbose("Creating render progress display panel");
		RenderProgressDisplayPanel displayPanel = new(renderJob);
		displayedWindowItem = new StackLayoutItem(displayPanel, HorizontalAlignment.Stretch, true);
		StackLayoutItem titleItem = new(titleLabel, HorizontalAlignment.Center);
		Content = new StackLayout
		{
				Items   = { titleItem, displayedWindowItem },
				Spacing = 10
		};

		//Once the render job is complete, save the image and open it up
		renderJob.GetAwaiter().OnCompleted(
				() =>
				{
					string path = Path.GetFullPath("./image.png");
					renderJob.ImageBuffer.Save(File.OpenWrite(path), new PngEncoder());
					Process.Start(
							new ProcessStartInfo
							{
									FileName  = "eog",
									Arguments = $"\"{path}\"",
									//These flags stop the image display program's console from attaching to ours (because that's yuck!)
									UseShellExecute        = false,
									RedirectStandardError  = true,
									RedirectStandardInput  = true,
									RedirectStandardOutput = true
							}
					);


					if (GraphicsValidator.Errors.IsEmpty)
					{
						Information("No Errors");
					}
					else
					{
						foreach ((GraphicsErrorType errorType, ConcurrentDictionary<object, ulong>? dict) in GraphicsValidator.Errors)
						{
							Warning("{ErrorType} Errors:", errorType);
							foreach ((object? obj, ulong count) in dict)
								Warning("\t{Object} = {Count}", obj, count);
						}
						Error("{@Dict}", GraphicsValidator.Errors);
					}
				}
		);
	}
}