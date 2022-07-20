using Eto.Forms;
using RayTracer.Core;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Serilog.Log;

namespace RayTracer.Display.Dev;

public class RenderJobTrackingTab : Panel
{
	private readonly StackLayout renderOptionLayout;
	private readonly Splitter    splitterContent;

	public RenderJobTrackingTab(string id)
	{
		ID = id;
		Content = splitterContent = new Splitter
		{
				ID = $"[Splitter] {ID}/Content"
		};

		{
			splitterContent.Panel1 = renderOptionLayout = new StackLayout
			{
					ID          = $"{splitterContent.ID}/RenderOptions",
					Orientation = Orientation.Vertical
			};
			Verbose("Creating property editors");
			foreach (PropertyInfo prop in typeof(RenderOptions).GetProperties())
			{
				PropertyEditorView editorView;
				Label              label = new() { ID = $"{prop.Name} label", Text = prop.Name, Style = Appearance.Styles.GeneralTextual };

				TableCell labelCell  = new(label);
				TableCell editorCell = new();
				tableLayout.Rows!.Add(new TableRow(labelCell, editorCell) { ScaleHeight = false });

				//Switch to create the editor
				if (prop.PropertyType == typeof(int))
				{
					editorView = new IntEditor(RenderOptions, prop, editorCell);
				}
				else if (prop.PropertyType == typeof(float))
				{
					editorView = new FloatEditor(RenderOptions, prop, editorCell);
				}
				else if (prop.PropertyType.IsEnum)
				{
					//Hey it's a little funky but it works
					editorView = (PropertyEditorView)Activator.CreateInstance(typeof(EnumEditor<>).MakeGenericType(prop.PropertyType), RenderOptions, prop, editorCell)!;
				}
				else
				{
					editorCell.Control = new Label { Text = $"{prop.PropertyType} not yet supported sorry!", ID = $"{prop.Name} error message" };
					continue;
				}

				Verbose("Created editor for property {Property}: {Editor}", prop, editorView);
				renderOptionsPropertyEditors.Add(editorView);
			}
		}
	}

	public  RenderOptions   RenderOptions { get; } = new();
	public  AsyncRenderJob? RenderJob     { get; } = null;
	private bool            isRendering   => RenderJob?.RenderCompleted == false;


#region Property Editors

	/// <summary>List of all the property editors for the <see cref="RenderOptions"/></summary>
	private readonly List<PropertyEditorView> renderOptionsPropertyEditors = new();

	/// <summary>Base class for an editor view for editing a property of a <see cref="RenderOptions"/> instance</summary>
	private abstract class PropertyEditorView : IDisposable
	{
		protected PropertyEditorView(RenderOptions target, PropertyInfo prop, TableCell tableCell)
		{
			Target    = target;
			Prop      = prop;
			TableCell = tableCell;
		}

		protected          RenderOptions Target    { get; }
		protected internal PropertyInfo              Prop      { get; }
		protected          TableCell                 TableCell { get; }

		/// <inheritdoc/>
		public abstract void Dispose();

		internal abstract void UpdateDisplayedFromTarget();
	}

	private void UpdateRenderOptionEditorsFromVariable()
	{
		Debug("Updating editor views from variables");
		for (int i = 0; i < renderOptionsPropertyEditors.Count; i++)
		{
			PropertyEditorView propertyEditorView = renderOptionsPropertyEditors[i];
			Verbose("Updating property view for property {Property}", propertyEditorView.Prop);
			propertyEditorView.UpdateDisplayedFromTarget();
		}
	}

	private sealed class IntEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public IntEditor(RenderOptions target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			const int min = int.MinValue;
			const int max = int.MaxValue;
			// if (typeof(RenderOptions).GetConstructors()[0].GetParameters().FirstOrDefault(p => p.Name == prop.Name)?.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
			// min = 0;

			Verbose("{Property}: Min = {Min}, Max = {Max}", prop, min, max);

			stepper = new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MaximumDecimalPlaces = 0, MinValue = min, MaxValue = max, ToolTip = $"Valid range is [{min}...{max}]", Style = Appearance.Styles.GeneralTextual };
			stepper.ValueChanged += (sender, _) =>
			{
				int value = (int)((NumericStepper)sender!).Value;
				Verbose("Property {Property} changed to {Value}", Prop, value);
				Prop.SetValue(Target, value);
			};
			TableCell.Control = stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (int)Prop.GetValue(Target)!;
		}

		public override void Dispose()
		{
			stepper.Dispose();
		}
	}

	private sealed class EnumEditor<T> : PropertyEditorView where T : struct, Enum
	{
		private readonly EnumDropDown<T> dropDown;

		/// <inheritdoc/>
		public EnumEditor(RenderOptions target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			Verbose("{Property}: Possible enum values are {Values}", prop, Enum.GetValues<T>());

			dropDown = new EnumDropDown<T> { ID = $"{Prop.Name} dropdown", Style = Appearance.Styles.GeneralTextual };
			dropDown.SelectedValueChanged += (sender, _) =>
			{
				T value = ((EnumDropDown<T>)sender!).SelectedValue;
				Verbose("Property {Property} changed to {Value}", Prop, value);
				Prop.SetValue(Target, value);
			};
			TableCell.Control = dropDown;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			dropDown.SelectedValue = (T)Prop.GetValue(Target)!;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			dropDown.Dispose();
		}
	}

	private sealed class FloatEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public FloatEditor(RenderOptions target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			//TODO: Infinities don't display properly for some reason
			const double min = double.NegativeInfinity;
			const double max = double.PositiveInfinity;
			// //It's a record so must have a backing field, don't have to worry about null refs
			// FieldInfo backingField = prop.DeclaringType!.GetField($"<{Prop.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
			// if ((Prop.GetCustomAttribute<NonNegativeValueAttribute>() ?? backingField.GetCustomAttribute<NonNegativeValueAttribute>()) is not null)
			//Honestly why the hell do I have to do this microsoft????
			//Just apply it to the field, property and parameter, please for the love of god
			// if (typeof(RenderOptions).GetConstructors()[0].GetParameters().FirstOrDefault(p => p.Name == prop.Name)?.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
			// min = 0f;

			Verbose("{Property}: Min = {Min}, Max = {Max}", prop, min, max);

			stepper = new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MaximumDecimalPlaces = 5, DecimalPlaces = 1, MinValue = min, MaxValue = max, ToolTip = $"Valid range is [{min}...{max}]", Style = Appearance.Styles.GeneralTextual };
			stepper.ValueChanged += (sender, _) =>
			{
				float value = (float)((NumericStepper)sender!).Value;
				Verbose("Property {Property} changed to {Value}", Prop, value);
				Prop.SetValue(Target, value);
			};
			TableCell.Control = stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (float)Prop.GetValue(Target)!;
		}

		/// <inheritdoc/>
		public override void Dispose()
		{
			stepper.Dispose();
		}
	}

#endregion
}