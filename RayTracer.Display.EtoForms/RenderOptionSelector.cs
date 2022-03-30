using Eto.Drawing;
using Eto.Forms;
using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace RayTracer.Display.EtoForms;

/// <summary>
/// Panel that allows modification of the settings of a <see cref="RenderOptions"/> instance
/// </summary>
internal sealed class RenderOptionSelector : Panel, INotifyPropertyChanged
{
	public RenderOptionSelector()
	{
		Options = RenderOptions.Default;
		//Update the sub views whenever someone changes the properties of our render options
		PropertyChanged += (_, _) => UpdateSubViews();
		TableLayout tableLayout = new(){Padding = 10, Spacing = new Size(10, 5)};
		Content = tableLayout;
		//Loop over each property in RenderOptions and create an editor for it
		//TODO: Perhaps tooltips or something similar
		foreach (PropertyInfo prop in typeof(RenderOptions).GetProperties())
		{
			PropertyEditorView view;
			Label              label = new() { ID = $"{prop.Name} label", Text = prop.Name,  };

			TableCell labelCell  = new(label);
			TableCell editorCell = new();
			tableLayout.Rows!.Add(new TableRow(labelCell, editorCell){ScaleHeight = false});

			//Switch to create the editor
			if (prop.PropertyType == typeof(int))
			{
				view = new IntEditor(this, prop, editorCell);
			}
			else if (prop.PropertyType == typeof(float))
			{
				view = new FloatEditor(this, prop, editorCell);
			}
			else if (prop.PropertyType.IsEnum)
			{
				//Hey it's a little funky but it works
				view = (PropertyEditorView)Activator.CreateInstance(typeof(EnumEditor<>).MakeGenericType(prop.PropertyType), this, prop, editorCell)!;
			}
			else
			{
				editorCell.Control = new Label { Text = $"{prop.PropertyType} not yet supported sorry!" };
				continue;
			}

			propertyEditors.Add(view);
		}
		tableLayout.Rows!.Add(new TableRow()); //Add empty row so that scaling looks nice (last row sizes to fil gap)

		UpdateSubViews();
	}

#region Property Editors

	private sealed class IntEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public IntEditor(RenderOptionSelector target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			int       min          = int.MinValue;
			int       max          = int.MaxValue;
			if (typeof(RenderOptions).GetConstructors()[0].GetParameters().FirstOrDefault(p=>p.Name ==prop.Name)?.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
				min = 0;

			stepper              =  new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MaximumDecimalPlaces = 0, MinValue = min, MaxValue = max };
			stepper.ValueChanged += (sender, _) => Prop.SetValue(Target.Options, (int)((NumericStepper)sender!).Value);
			TableCell.Control    =  stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (int)Prop.GetValue(Target.Options)!;
		}
	}
	private sealed class EnumEditor<T> : PropertyEditorView
	{
		private readonly EnumDropDown<T> dropDown;

		/// <inheritdoc/>
		public EnumEditor(RenderOptionSelector target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			dropDown                      =  new EnumDropDown<T> { ID = $"{Prop.Name} dropdown"};
			dropDown.SelectedValueChanged += (sender, _) => Prop.SetValue(Target.Options, ((EnumDropDown<T>)sender!).SelectedValue);
			TableCell.Control             =  dropDown;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			dropDown.SelectedValue = (T)Prop.GetValue(Target.Options)!;
		}
	}

	private sealed class FloatEditor : PropertyEditorView
	{
		private readonly NumericStepper stepper;

		/// <inheritdoc/>
		public FloatEditor(RenderOptionSelector target, PropertyInfo prop, TableCell tableCell) : base(target, prop, tableCell)
		{
			float     min          = float.MinValue;
			float     max          = float.MaxValue;
			// //It's a record so must have a backing field, don't have to worry about null refs
			// FieldInfo backingField = prop.DeclaringType!.GetField($"<{Prop.Name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;
			// if ((Prop.GetCustomAttribute<NonNegativeValueAttribute>() ?? backingField.GetCustomAttribute<NonNegativeValueAttribute>()) is not null)
			//Honestly why the hell do I have to do this microsoft????
			//Just apply it to the field, property and parameter, please for the love of god
			if (typeof(RenderOptions).GetConstructors()[0].GetParameters().FirstOrDefault(p=>p.Name==prop.Name)?.GetCustomAttribute<NonNegativeValueAttribute>() is not null)
				min = 0f;

			stepper              =  new NumericStepper { ID = $"{Prop.Name} stepper", Increment = 1, MinValue = min, MaxValue = max };
			stepper.ValueChanged += (sender, _) => Prop.SetValue(Target.Options, (float)((NumericStepper)sender!).Value);
			TableCell.Control    =  stepper;
		}

		/// <inheritdoc/>
		internal override void UpdateDisplayedFromTarget()
		{
			stepper.Value = (float)Prop.GetValue(Target.Options)!;
		}
	}

#endregion


#region Boilerplate

	private readonly List<PropertyEditorView> propertyEditors = new();

	private abstract class PropertyEditorView
	{
		protected PropertyEditorView(RenderOptionSelector target, PropertyInfo prop, TableCell tableCell)
		{
			Target    = target;
			Prop      = prop;
			TableCell = tableCell;
		}

		protected RenderOptionSelector Target    { get; }
		protected PropertyInfo         Prop      { get; }
		protected TableCell            TableCell { get; }

		internal abstract void UpdateDisplayedFromTarget();
	}

	private void UpdateSubViews()
	{
		foreach (PropertyEditorView propertyEditorView in propertyEditors) propertyEditorView.UpdateDisplayedFromTarget();
	}

	public readonly RenderOptions Options;

	public event PropertyChangedEventHandler? PropertyChanged;

	[NotifyPropertyChangedInvocator]
	public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

#endregion
}