using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using System.Reflection;
using Terminal.Gui;
using Terminal.Gui.TextValidateProviders;
using static Terminal.Gui.Dim;

namespace RayTracer.Display.Terminal.GUI;

/// <summary>
///  TopLevel class that is used to confirm the settings that will be used to render the app
/// </summary>
internal sealed class SettingsConfirmer : Toplevel
{
	public SettingsConfirmer()
	{
		Id = "Settings Confirmer TopLevel";
		//Create all the UI elements here
		Window mainWin = new("Confirm Render settings")
		{
				//Window covers the whole screen
				X  = 0, Y = 0, Width = Fill(), Height = Fill(),
				Id = "Settings Confirmer Window"
		};
		//Generate all the property inputs here
		View?          prevContainer = null;
		PropertyInfo[] props         = typeof(RenderOptions).GetProperties();
		int            maxNameLength = props.Max(p => p.Name.Length);
		foreach (PropertyInfo propInfo in props)
		{
			string name = propInfo.Name;
			var containerView = new View
			{
					X      = 0,
					Y      = prevContainer is null ? 0 : Pos.Bottom(prevContainer), //Try place below the previous property layout
					Width  = Fill(),
					Height = 1,
					Id     = $"'{name}' container view"
			};

			Label nameLabel = new($"{name}:") { X = 0, Y = 0, Width = 15, Height = 1, Id = $"{name} name label" };
			containerView.Add(nameLabel);
			if (propInfo.PropertyType == typeof(int))
			{
				int min        = int.MinValue, max = int.MaxValue;
				int defaultVal = (int)propInfo.GetValue(RenderOptions.Default)!;
				//If the value can't be neg, set the min to 0
				if (propInfo.GetCustomAttribute(typeof(NonNegativeValueAttribute)) is not null) min = 0;
				if (propInfo.GetCustomAttribute<ValueRangeAttribute>() is { } range) (min, max)     = ((int)range.From, (int)range.To);

				//Create the text field that only allows numbers to be input
				TextValidateField entryField = new(IntValidator)
				{
						Text = defaultVal.ToString(), Id = $"'{name}' (int) entry field"
				};
				//Whenever the user finishes inputting, ensure it's in our valid range
				entryField.Leave += _ =>
				{
					//If we didn't manage to parse the value as an int, reset it to the default
					if (!int.TryParse(entryField.Text.ToString(), out int v)) entryField.Text = defaultVal.ToString();
					if (v < min) entryField.Text                                              = (v = min).ToString();
					if (v > max) entryField.Text                                              = (v = max).ToString();
					//Now we've got a valid value, update the property on the instance
					propInfo.SetValue(Options, v);
				};
				containerView.Add(entryField);
			}
			else if (propInfo.PropertyType == typeof(float))
			{
				float min        = float.MinValue, max = float.MaxValue;
				float defaultVal = (float)propInfo.GetValue(RenderOptions.Default)!;
				//If the value can't be neg, set the min to 0
				if (propInfo.GetCustomAttribute(typeof(NonNegativeValueAttribute)) is not null) min = 0;
				if (propInfo.GetCustomAttribute<ValueRangeAttribute>() is { } range) (min, max)     = ((float)range.From, (float)range.To);

				//Create the text field that only allows numbers to be input
				TextValidateField entryField = new(FloatValidator)
				{
						Text = defaultVal.ToString(), Id = $"'{name}' (float) entry field"
				};
				//Whenever the user finishes inputting, ensure it's in our valid range
				entryField.Leave += _ =>
				{
					//If we didn't manage to parse the value as an int, reset it to the default
					if (!float.TryParse(entryField.Text.ToString(), out float v)) entryField.Text = defaultVal.ToString();
					if (v < min) entryField.Text                                                  = (v = min).ToString();
					if (v > max) entryField.Text                                                  = (v = max).ToString();
					//Now we've got a valid value, update the property on the instance
					propInfo.SetValue(Options, v);
				};
				containerView.Add(entryField);
			}
			else if (propInfo.PropertyType == typeof(bool))
			{
				CheckBox toggle = new()
				{
						Id      = $"'{name}' (bool) toggle",
						Checked = (bool)propInfo.GetValue(Options)!,
						Text    = "Tezt"
				};
				toggle.Toggled += b => propInfo.SetValue(Options, b);
				containerView.Add(toggle);
			}
			else
			{
				containerView.Add(
						new Label($"ERROR: Type {propInfo.PropertyType} unsupported")
						{
								Id = $"Prop {name} error label"
						}
				);
			}

			mainWin.Add(containerView);
			//Position the value subview correctly
			containerView.Subviews[1].X      = Pos.Right(nameLabel);
			containerView.Subviews[1].Y      = 0;
			containerView.Subviews[1].Height = 1;
			containerView.Subviews[1].Width  = maxNameLength;

			prevContainer = containerView;
		}

		Add(mainWin);

		//TODO: Quit button, with validation
		mainWin.Add(new Button("Confirm Settings") { X = 0, Y = Pos.Bottom(mainWin) - 1, Height = 1 });
	}

	//NOTE: If these providers are reused, we get problems with the input fields all showing the same result (due to shared provider state)
	//	So we have to make the properties return a new instance each time to avoid this
	private static ITextValidateProvider FloatValidator => new TextRegexProvider(@"^-?0*?((?'integer'\d+)|(?'PreFloat'\d+\.\d*)|(?'PostFloat'\.\d+)|(?'Infinity'∞))$") { ValidateOnInput = true };

	private static ITextValidateProvider IntValidator => new TextRegexProvider(@"^-?0*?(\d+)$") { ValidateOnInput = true };

	public RenderOptions Options { get; } = RenderOptions.Default; //Start with default values
	public Scene?        Scene   { get; } = null;                  //Can only be null if the user quits without selecting the scene (i hope)
}