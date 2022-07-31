using Ararem.RayTracer.Core;
using Ararem.RayTracer.Core.Debugging;
using Ararem.RayTracer.Impl.Builtin;
using Eto.Containers;
using Eto.Drawing;
using Eto.Forms;
using SixLabors.ImageSharp.Memory;
using SixLabors.ImageSharp.PixelFormats;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using static LibArarem.Core.Logging.LogUtils;
using static Ararem.RayTracer.Display.Dev.Resources.StyleManager;
using static Serilog.Log;

namespace Ararem.RayTracer.Display.Dev;

// TODO: Why am i modifying an immutable type with reflection (*cough* RenderOptions *cough*)
public class RenderJobTrackingTab : Panel
{
	private readonly DynamicLayout           mainDynamicLayout;
	private          CancellationTokenSource renderJobCancellationTokenSource = new();

	//TODO: Save image button
	public RenderJobTrackingTab(string id)
	{
		DynamicGroup renderOptionsGroup;
		DynamicGroup renderBufferGroup;
		DynamicGroup renderStatsGroup;
		ID      = id;
		Padding = DefaultPadding;
		{
			Content = mainDynamicLayout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
		}
		//Split the layout into 3 horizontal groups - options, stats, image
		mainDynamicLayout.BeginHorizontal();

		{
			Verbose("Creating property editor panel");
			renderOptionsGroup = mainDynamicLayout.BeginGroup("Render Options", spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.BeginScrollable(spacing: DefaultSpacing, padding: DefaultPadding);

			HandleIntProperty(nameof(RenderOptions.RenderWidth),  1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.RenderHeight), 1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.Passes),       1, int.MaxValue);
			HandleBoolProperty(nameof(RenderOptions.InfinitePasses));
			HandleIntProperty(nameof(RenderOptions.ConcurrencyLevel),     1, Environment.ProcessorCount);
			HandleIntProperty(nameof(RenderOptions.MaxBounceDepth),       0, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.LightSampleCountHint), 1, int.MaxValue);
			HandleFloatProperty(nameof(RenderOptions.KMin), 0f, float.PositiveInfinity);
			HandleFloatProperty(nameof(RenderOptions.KMax), 0f, float.PositiveInfinity);
			HandleEnumProperty<GraphicsDebugVisualisation>(nameof(RenderOptions.DebugVisualisation));

			mainDynamicLayout.Add(null, yscale: true);
			Verbose("Created property editors");


			Verbose("Creating scene select dropdown");
			Label label = GetNameLabel("Scene");

			selectedSceneDropdown = new DropDown
			{
					ID    = $"{mainDynamicLayout.ID}/SelectedSceneDropdown",
					Style = nameof(Monospace)
			};
			selectedSceneDropdown.SelectedValueChanged += delegate
			{
				Scene newScene = (Scene)selectedSceneDropdown.SelectedValue;
				Verbose("Selected scene changed to {Scene}", newScene);
				SelectedScene = newScene;
			};
			//A bit funky how we do this, but it works I guess
			List<Scene> allScenes = BuiltinScenes.GetAll().ToList();
			selectedSceneDropdown.DataStore = allScenes;
			Scene initial = allScenes.First(s => string.Equals(s.Name, SelectedScene.Name, StringComparison.Ordinal));
			int   index   = allScenes.ToList().IndexOf(initial);
			selectedSceneDropdown.SelectedIndex = index;
			mainDynamicLayout.AddRow(label, selectedSceneDropdown);
			Verbose("Created scene select dropdown: {Dropdown}", selectedSceneDropdown);

			mainDynamicLayout.EndScrollable();

			Verbose("Creating toggle render button");
			toggleRenderStateButton = new Button
			{
					ID    = $"{mainDynamicLayout.ID}/ToggleRenderButton",
					Style = nameof(Bold),
					Text  = "[Toggle Render]"
			};
			mainDynamicLayout.AddCentered(toggleRenderStateButton);
			Command toggleRenderStateCommand = new(ToggleRenderButtonClicked)
			{
					ID = $"{toggleRenderStateButton.ID}.Command"
			};
			toggleRenderStateButton.Command = toggleRenderStateCommand;
			Verbose("Created toggle render button: {Control}", toggleRenderStateButton);

			mainDynamicLayout.EndGroup();
		}

		{
			Verbose("Creating render stats view");
			renderStatsGroup = mainDynamicLayout.BeginGroup("Render Stats", spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.BeginScrollable(spacing: DefaultSpacing, padding: DefaultPadding);
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.Add("Testasdasdasdasdasdasdasdasdasdasdasdaads");
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.Add("Test");
			mainDynamicLayout.EndScrollable();
			mainDynamicLayout.EndGroup();

			Verbose("Created render stats view");
		}

		{
			renderBufferGroup = mainDynamicLayout.BeginGroup("Render Buffer", spacing: DefaultSpacing, padding: DefaultPadding);
			previewImage      = new Bitmap(new Size(1, 1), PixelFormat.Format24bppRgb);
			previewImageView  = new DragZoomImageView { ID = "Preview Image View", ZoomButton = MouseButtons.Middle };
			mainDynamicLayout.Add(previewImageView);
			mainDynamicLayout.EndGroup();
		}

		mainDynamicLayout.EndHorizontal();

		{
			// Periodically update the previews using a timer
			//PERF: This creates quite a few allocations when called frequently
			//TODO: Perhaps PeriodicTimer or UITimer
			updatePreviewTimer = new Timer(static state => Application.Instance.Invoke((Action)state!), UpdateUi, UpdatePeriod, Timeout.Infinite);
		}

		{
			//Have to create the dynamic layouts before we try to access the instantiated controls, else they're null
			mainDynamicLayout.Create();
			renderOptionsGroup.GroupBox.Style = nameof(Force_Heading);
			renderStatsGroup.GroupBox.Style   = nameof(Force_Heading);
			renderBufferGroup.GroupBox.Style  = nameof(Force_Heading);
			UpdateUi();
		}
	}

	/// <summary>Render options that affect how the <see cref="RenderJob"/> is rendered</summary>
	public RenderOptions RenderOptions { get; } = new();

	/// <summary>Currently selected scene. If a render is running, then it's the one that's being rendered.</summary>
	public Scene SelectedScene { get; private set; } = BuiltinScenes.Demo;

	/// <summary>The current render job (if any)</summary>
	public AsyncRenderJob? RenderJob { get; private set; } = null;

	/// <summary>Called whenever the "Toggle Render" button is pressed</summary>
	/// <param name="sender"></param>
	/// <param name="eventArgs"></param>
	private void ToggleRenderButtonClicked(object? sender, EventArgs eventArgs)
	{
		TrackEvent(sender, eventArgs);
		bool? currentlyCompleted = RenderJob?.RenderCompleted;
		LogExpression(currentlyCompleted);
		switch (currentlyCompleted)
		{
			case false:
				Verbose("Render was running, cancelling and recreating task source {RenderJobCancellationTokenSource}", renderJobCancellationTokenSource);
				renderJobCancellationTokenSource.Cancel();
				renderJobCancellationTokenSource.Dispose();
				renderJobCancellationTokenSource = new CancellationTokenSource();
				RenderJob!.RenderTask.ContinueWith(_ => Application.Instance.Invoke(UpdateUi)); //We make sure the UI gets updated after the render completes, else the UI shows the wrong state
				break;
			case true:
				Verbose("Render was completed, creating new render with RenderOptions {@RenderOptions} and Scene {Scene}", RenderOptions, SelectedScene);
				RenderJob = new AsyncRenderJob(SelectedScene, RenderOptions);
				RenderJob.StartOrGetRenderAsync(renderJobCancellationTokenSource.Token);
				break;
			case null:
				Verbose("Render was  null, creating new render with RenderOptions {@RenderOptions} and Scene {Scene}", RenderOptions, SelectedScene);
				RenderJob = new AsyncRenderJob(SelectedScene, RenderOptions);
				RenderJob.StartOrGetRenderAsync(renderJobCancellationTokenSource.Token);
				break;
		}

		UpdateUi();
	}

#region RenderOption property editing

	private Label GetNameLabel(string propertyName, (object min, object max)? maybeRange = null) =>
			new()
			{
					ID            = $"{mainDynamicLayout.ID}/{propertyName}.Label",
					Text          = propertyName,
					ToolTip       = maybeRange is {} range ? $"Valid range is [{range.min}..{range.max}]" : null,
					Style         = nameof(Italic),
					TextAlignment = TextAlignment.Center
			};

	private void HandleIntProperty(string propertyName, int min, int max)
	{
		Verbose("Trying to add integer property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		object?      boxedPropertyValue    = property.GetValue(RenderOptions);
		int          initialValue          = boxedPropertyValue as int? ?? throw new ArgumentException($"Expected an integer but got {boxedPropertyValue}");
		Label        label                 = GetNameLabel(propertyName, (min, max));
		NumericStepper stepper = new()
		{
				//Can't assign a format string of "n0" because the comma breaks things once you get above 999 :(
				ID                   = $"{mainDynamicLayout.ID}/{propertyName}.Stepper",
				Increment            = 1.0,
				MaximumDecimalPlaces = 0,
				MinValue             = min,
				MaxValue             = max,
				Style                = nameof(Monospace),
				ToolTip              = label.ToolTip
		};
		stepper.ValueChanged += delegate
		{
			int newValue = (int)stepper.Value;
			LogPropChanged(property, newValue);
			property.SetValue(RenderOptions, newValue);
		};
		stepper.Value = initialValue;
		RenderOptionEditor renderOptionEditor = new(stepper, canModifyWhileRunning);
		renderOptionEditors.Add(renderOptionEditor);

		Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
	}

	private void HandleBoolProperty(string propertyName)
	{
		Verbose("Trying to add boolean property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));

		object? boxedPropertyValue = property.GetValue(RenderOptions);
		bool    initialValue       = boxedPropertyValue as bool? ?? throw new ArgumentException($"Expected a bool but got {boxedPropertyValue}");

		Label label = GetNameLabel(propertyName);
		CheckBox checkBox = new()
		{
				ID         = $"{mainDynamicLayout.ID}/{propertyName}.CheckBox",
				ThreeState = false,
				Style      = nameof(General)
		};
		checkBox.CheckedChanged += delegate
		{
			bool value = checkBox.Checked ?? throw new InvalidOperationException("CheckBox.Checked was null");
			LogPropChanged(property, value);
			property.SetValue(RenderOptions, value);
		};
		checkBox.Checked = initialValue;
		RenderOptionEditor renderOptionEditor = new(checkBox, canModifyWhileRunning);
		renderOptionEditors.Add(renderOptionEditor);

		Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
	}

	private void HandleFloatProperty(string propertyName, float min, float max)
	{
		Verbose("Trying to add float property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		object?      boxedPropertyValue    = property.GetValue(RenderOptions);
		float        initialValue          = boxedPropertyValue as float? ?? throw new ArgumentException($"Expected a float but got {boxedPropertyValue}");

		Label label = GetNameLabel(propertyName, (min, max));
		NumericStepper stepper = new()
		{
				ID                   = $"{mainDynamicLayout.ID}/{propertyName}.Stepper",
				Increment            = 1.0,
				MaximumDecimalPlaces = 5,
				DecimalPlaces        = 5,
				MinValue             = min,
				MaxValue             = max,
				ToolTip              = label.ToolTip,
				Style                = nameof(Monospace)
		};
		stepper.ValueChanged += delegate
		{
			float newVal = (float)stepper.Value;
			LogPropChanged(property, newVal);
			property.SetValue(RenderOptions, newVal);
		};
		stepper.Value = initialValue;
		RenderOptionEditor renderOptionEditor = new(stepper, canModifyWhileRunning);
		renderOptionEditors.Add(renderOptionEditor);

		Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
	}

	private void HandleEnumProperty<T>(string propertyName) where T : Enum
	{
		Verbose("Trying to add enum property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		Label        label                 = GetNameLabel(propertyName);
		object?      boxedPropertyValue    = property.GetValue(RenderOptions);
		if (boxedPropertyValue is not T initialValue) throw new ArgumentException($"Expected an enum but got {boxedPropertyValue}");

		EnumDropDown<T> dropDown = new()
		{
				ID    = $"{mainDynamicLayout.ID}/{propertyName}.Dropdown",
				Style = nameof(Monospace)
		};
		dropDown.SelectedValueChanged += delegate
		{
			T newVal = dropDown.SelectedValue;
			LogPropChanged(property, newVal);
			property.SetValue(RenderOptions, newVal);
		};
		dropDown.SelectedValue = initialValue;
		RenderOptionEditor renderOptionEditor = new(dropDown, canModifyWhileRunning);
		renderOptionEditors.Add(renderOptionEditor);

		Verbose("Property editor is {PropertyEditor}", renderOptionEditor);
		mainDynamicLayout.AddRow(label, renderOptionEditor.Control);
	}

	private static void LogPropChanged<T>(PropertyInfo property, T newVal)
	{
		Verbose("Property {Property} changed => {NewValue}", property, newVal);
	}

	/// <summary>List containing all the render option editors</summary>
	private readonly List<RenderOptionEditor> renderOptionEditors = new();

	private sealed record RenderOptionEditor(CommonControl Control, bool CanModifyWhileRunning);

#endregion

#region UI Controls

	private readonly Button   toggleRenderStateButton;
	private readonly DropDown selectedSceneDropdown;

	/// <summary>Target refreshes-per-second that we want</summary>
	private static int TargetRefreshRate => 1; //Fps

	/// <summary>Time (ms) between updates for our <see cref="TargetRefreshRate"/></summary>
	private static int UpdatePeriod => 1000 / TargetRefreshRate;

	/// <summary>Updates all the UI</summary>
	private void UpdateUi()
	{
		/*
		 * Note that we don't have to worry about locks or anything, since
		 * (A) - It's only called on the main thread
		 * (B) - The timer is only ever reset *after* everything's already been updated
		 */
		Stopwatch  totalSw  = Stopwatch.StartNew();
		Stopwatch  partSw  = Stopwatch.StartNew();
		Trace("[{Sender}] Updating UI", ID);

		//Enable/disable controls depending on if we are rendering or not
		partSw.Restart();
		{
			// Verbose("Updating whether property editors can be modified");
			for (int i = 0; i < renderOptionEditors.Count; i++)
			{
				RenderOptionEditor editor = renderOptionEditors[i];
				bool enabled = RenderJob is null or { RenderCompleted: true } //Render isn't running
							   || editor.CanModifyWhileRunning;               //Always allowed to modify
				if (editor.Control.GetType().GetProperty("ReadOnly", typeof(bool)) is {} readonlyProp)
				{
					bool shouldBeReadonly = !enabled;
					readonlyProp.SetValue(editor.Control, shouldBeReadonly);
					Trace("{Control}.Readonly set to {Value}", editor.Control, shouldBeReadonly);
				}
				else
				{
					editor.Control.Enabled = enabled;
					Trace("{Control}.Enabled set to {Value}", editor.Control, enabled);
				}
			}

			{
				bool enabled = RenderJob is null or { RenderCompleted: true };
				selectedSceneDropdown.Enabled = enabled;
				Trace("{Control}.Enabled set to {Value}", selectedSceneDropdown, enabled);
			}

			//Update the text for the toggle render state button
			{
				string newText = RenderJob?.RenderCompleted switch
				{
						null  => "Start render",
						false => "Stop render",
						true  => "Restart new render"
				};
				toggleRenderStateButton.Text = newText;
				Trace("{Control}.Text set to {NewValue}", toggleRenderStateButton, newText);
			}
		}
		Trace("[{Sender}] (Editors) Updated in {Elapsed:#00.000 'ms'}", ID, partSw.Elapsed.TotalMilliseconds);


		//Update the image display of the render buffer
		partSw.Restart();
		{
			Buffer2D<Rgb24>? srcRenderBuffer = RenderJob?.Image.Frames.RootFrame.PixelBuffer;
			if (srcRenderBuffer is not null)
			{
				int width = srcRenderBuffer.Width, height = srcRenderBuffer.Height;
				if ((previewImage.Width != width) || (previewImage.Height != height))
				{
					Trace("Recreating preview image ({OldValue}) to change size to {NewValue}", previewImage.Size, new Size(width, height));
					previewImageView.Image = previewImage = new Bitmap(width, height, PixelFormat.Format24bppRgb) { ID = $"{previewImageView.ID}.Bitmap" };
				}

				using BitmapData destPreviewImage = previewImage.Lock();
				IntPtr           destOffset       = destPreviewImage.Data;
				int              xSize            = previewImage.Width, ySize = previewImage.Height;
				for (int y = 0; y < ySize; y++)
				{
					//This code assumes the source and dest images are same bit depth and size
					//Otherwise here be dragons
					unsafe
					{
						Span<Rgb24> renderBufRow = srcRenderBuffer.DangerousGetRowSpan(y);
						void*       destPtr      = destOffset.ToPointer();
						Span<Rgb24> destRow      = new(destPtr, xSize);

						renderBufRow.CopyTo(destRow);
						destOffset += destPreviewImage.ScanWidth;
					}
				}
			}
		}
		Trace("[{Sender}] (Image) Updated in {Elapsed:#00.000 'ms'}", ID, partSw.Elapsed.TotalMilliseconds);

		//Update the statistics table
		{
			// UpdateStatsTable();
		}

		Trace("[{Sender}] Updated in {Elapsed:#00.000 'ms'}", ID, totalSw.Elapsed.TotalMilliseconds);
		updatePreviewTimer.Change(UpdatePeriod, Timeout.Infinite);
	}

	private readonly Timer updatePreviewTimer;

	/// <summary>The bitmap that holds the image that is displayed in the "render preview" section</summary>
	private Bitmap previewImage;

	/// <summary>Control that holds the <see cref="previewImage"/></summary>
	private readonly DragZoomImageView previewImageView;

#endregion

	// #error EXTRACT TO CUSTOM CLASSES FOR EACH GROUP REGION
}