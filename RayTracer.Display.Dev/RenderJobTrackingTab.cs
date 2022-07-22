using Eto.Forms;
using RayTracer.Core;
using RayTracer.Core.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static RayTracer.Display.Dev.Appearance.Styles;
using static Serilog.Log;

namespace RayTracer.Display.Dev;

public class RenderJobTrackingTab : Panel
{
	private readonly DynamicLayout renderOptionLayout;
	private readonly Splitter      splitterContent;

	private Button toggleRenderStateButton;

	public RenderJobTrackingTab(string id)
	{
		ID = id;
		{
			Content = splitterContent = new Splitter
			{
					ID = $"{ID}/Content"
			};
		}

		DynamicGroup renderOptionGroup;
		{
			Verbose("Creating property editor panel");
			splitterContent.Panel1 = renderOptionLayout = new DynamicLayout { ID = $"{splitterContent.ID}/RenderOptions" };
			renderOptionGroup                          = renderOptionLayout.BeginGroup("Render Options", spacing: DefaultSpacing, padding: DefaultPadding);
			renderOptionLayout.BeginScrollable();

			HandleIntProperty(nameof(RenderOptions.RenderWidth),  1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.RenderHeight), 1, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.Passes),       1, int.MaxValue);
			HandleBoolProperty(nameof(RenderOptions.InfinitePasses));
			HandleIntProperty(nameof(RenderOptions.ConcurrencyLevel),     1, Environment.ProcessorCount);
			HandleIntProperty(nameof(RenderOptions.MaxBounceDepth),       0, int.MaxValue);
			HandleIntProperty(nameof(RenderOptions.LightSampleCountHint), 0, int.MaxValue);
			HandleFloatProperty(nameof(RenderOptions.KMin), 0f, float.PositiveInfinity);
			HandleFloatProperty(nameof(RenderOptions.KMax), 0f, float.PositiveInfinity);
			HandleEnumProperty<GraphicsDebugVisualisation>(nameof(RenderOptions.DebugVisualisation));
			UpdateEditorsCanBeModified();

			renderOptionLayout.Add(null, false, true); //Add empty row for nicer scaling
			renderOptionLayout.EndScrollable();
			renderOptionLayout.EndGroup();

			Verbose("Created property editors");
		}

		{
			Verbose("Creating toggle render button");

			Verbose("Created toggle render button");
		}

		{
			renderOptionLayout.Create();
			renderOptionGroup.GroupBox.Style = nameof(Force_Heading);
		}
	}

	/// <summary>Render options that affect how the <see cref="RenderJob"/> is rendered</summary>
	public RenderOptions RenderOptions { get; } = new();

	/// <summary>The current render job (if any)</summary>
	public AsyncRenderJob? RenderJob { get; } = null;

	private bool IsRendering => RenderJob?.RenderCompleted == false;

#region RenderOption property editing

	private void HandleIntProperty(string propertyName, int min, int max)
	{
		Verbose("Trying to add integer property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		string       validRangeTooltip     = $"Valid range is [{min}...{max}]";
		Label label = new()
		{
				ID      = $"{renderOptionLayout.ID}/{propertyName}.Label",
				Text    = propertyName,
				ToolTip = validRangeTooltip,
				Style   = nameof(Italic)
		};
		object? boxedPropertyValue = property.GetValue(RenderOptions);
		int     initialValue       = boxedPropertyValue as int? ?? throw new ArgumentException($"Expected an integer but got {boxedPropertyValue}");

		NumericStepper stepper = new()
		{
				//Can't assign a format string of "n0" because the comma breaks things once you get above 999 :(
				//TODO: Make an issue on ETO for that
				ID                   = $"{renderOptionLayout.ID}/{propertyName}.Stepper",
				Increment            = 1.0,
				MaximumDecimalPlaces = 0,
				MinValue             = min,
				MaxValue             = max,
				ToolTip              = validRangeTooltip,
				Style                = nameof(Monospace)
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
		renderOptionLayout.AddRow(label, renderOptionEditor.Control);
	}

	private void HandleBoolProperty(string propertyName)
	{
		Verbose("Trying to add boolean property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		Label label = new()
		{
				ID    = $"{renderOptionLayout.ID}/{propertyName}.Label",
				Text  = propertyName,
				Style = nameof(Italic)
		};
		object? boxedPropertyValue = property.GetValue(RenderOptions);
		bool    initialValue       = boxedPropertyValue as bool? ?? throw new ArgumentException($"Expected a bool but got {boxedPropertyValue}");

		CheckBox checkBox = new()
		{
				ID         = $"{renderOptionLayout.ID}/{propertyName}.CheckBox",
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
		renderOptionLayout.AddRow(label, renderOptionEditor.Control);
	}

	private void HandleFloatProperty(string propertyName, float min, float max)
	{
		Verbose("Trying to add float property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		string       validRangeTooltip     = $"Valid range is [{min}...{max}]";
		Label label = new()
		{
				ID      = $"{renderOptionLayout.ID}/{propertyName}.Label",
				Text    = propertyName,
				ToolTip = validRangeTooltip,
				Style   = nameof(Italic)
		};
		object? boxedPropertyValue = property.GetValue(RenderOptions);
		float   initialValue       = boxedPropertyValue as float? ?? throw new ArgumentException($"Expected a float but got {boxedPropertyValue}");

		NumericStepper stepper = new()
		{
				//Can't assign a format string of "n0" because the comma breaks things once you get above 999 :(
				//TODO: Make an issue on ETO for that
				ID                   = $"{renderOptionLayout.ID}/{propertyName}.Stepper",
				Increment            = 1.0,
				MaximumDecimalPlaces = 5,
				DecimalPlaces        = 5,
				MinValue             = min,
				MaxValue             = max,
				ToolTip              = validRangeTooltip,
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
		renderOptionLayout.AddRow(label, renderOptionEditor.Control);
	}

	private void HandleEnumProperty<T>(string propertyName) where T : Enum
	{
		Verbose("Trying to add enum property editor for {Property}", propertyName);
		PropertyInfo property              = typeof(RenderOptions).GetProperty(propertyName) ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName);
		MethodInfo   setMethod             = property.SetMethod                              ?? throw new MissingMemberException(nameof(Core.RenderOptions), propertyName + ".set");
		bool         canModifyWhileRunning = !setMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit));
		Label label = new()
		{
				ID    = $"{renderOptionLayout.ID}/{propertyName}.Label",
				Text  = propertyName,
				Style = nameof(Italic)
		};
		object? boxedPropertyValue = property.GetValue(RenderOptions);
		if (boxedPropertyValue is not T initialValue) throw new ArgumentException($"Expected an enum but got {boxedPropertyValue}");

		EnumDropDown<T> dropDown = new()
		{
				//Can't assign a format string of "n0" because the comma breaks things once you get above 999 :(
				//TODO: Make an issue on ETO for that
				ID    = $"{renderOptionLayout.ID}/{propertyName}.Dropdown",
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
		renderOptionLayout.AddRow(label, renderOptionEditor.Control);
	}

	private static void LogPropChanged<T>(PropertyInfo property, T newVal)
	{
		Verbose("Property {Property} changed => {NewValue}", property, newVal);
	}

	/// <summary>List containing all the render option editors</summary>
	private readonly List<RenderOptionEditor> renderOptionEditors = new();

	private sealed record RenderOptionEditor(CommonControl Control, bool CanModifyWhileRunning);

	/// <summary>Updates all the property editors, locking them if they can't be modified</summary>
	private void UpdateEditorsCanBeModified()
	{
		for (int i = 0; i < renderOptionEditors.Count; i++)
		{
			RenderOptionEditor editor           = renderOptionEditors[i];
			bool               shouldBeReadonly = !editor.CanModifyWhileRunning && IsRendering;
			if (editor.Control.GetType().GetProperty("ReadOnly", typeof(bool)) is {} readonlyProp)
				readonlyProp.SetValue(editor.Control, shouldBeReadonly);
			else editor.Control.Enabled = !shouldBeReadonly;
		}
	}

#endregion
}