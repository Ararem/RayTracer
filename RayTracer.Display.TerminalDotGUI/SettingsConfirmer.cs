using JetBrains.Annotations;
using RayTracer.Core.Graphics;
using RayTracer.Core.Scenes;
using System.Reflection;
using Terminal.Gui;
using Terminal.Gui.TextValidateProviders;
using static Terminal.Gui.Dim;

namespace RayTracer.Display.TerminalDotGUI;

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
		View?          prevContainer  = null;
		PropertyInfo[] props          = typeof(RenderOptions).GetProperties();
		Dim            maxNameLength  = props.Max(p => p.Name.Length);
		Dim            maxValueLength = maxNameLength;
		foreach (PropertyInfo propInfo in props)
		{
			string name = propInfo.Name;
			Type   type = propInfo.PropertyType;
			var containerView = new View
			{
					X      = 0,
					Y      = prevContainer is null ? 0 : Pos.Bottom(prevContainer), //Try place below the previous property layout
					Width  = Fill(),
					Height = 1,
					Id     = $"'{name}' container view"
			};

			Label nameLabel = new($"{name}:") { X = 0, Y = 0, Width = maxNameLength, Height = 1, Id = $"{name} name label" };
			containerView.Add(nameLabel);

		#region Property value view implementation

			if (type == typeof(int))
			{
				int min        = int.MinValue, max = int.MaxValue;
				int defaultVal = (int)propInfo.GetValue(RenderOptions.Default)!;
				//If the value can't be neg, set the min to 0
				if (propInfo.GetCustomAttribute(typeof(NonNegativeValueAttribute)) is not null) min = 0;
				if (propInfo.GetCustomAttribute<ValueRangeAttribute>() is { } range) (min, max)     = ((int)range.From, (int)range.To);

				//Create the text field that only allows numbers to be input
				TextValidateField entryField = new(IntValidator)
				{
						Text   = defaultVal.ToString(), Id = $"'{name}' (int) entry field",
						X      = Pos.Right(nameLabel),
						Y      = 0,
						Height = 1,
						Width  = maxNameLength
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
			else if (type == typeof(float))
			{
				float min        = float.MinValue, max = float.MaxValue;
				float defaultVal = (float)propInfo.GetValue(RenderOptions.Default)!;
				//If the value can't be neg, set the min to 0
				if (propInfo.GetCustomAttribute(typeof(NonNegativeValueAttribute)) is not null) min = 0;
				if (propInfo.GetCustomAttribute<ValueRangeAttribute>() is { } range) (min, max)     = ((float)range.From, (float)range.To);

				//Create the text field that only allows numbers to be input
				TextValidateField entryField = new(FloatValidator)
				{
						Text   = defaultVal.ToString(), Id = $"'{name}' (float) entry field",
						X      = Pos.Right(nameLabel),
						Y      = 0,
						Height = 1,
						Width  = maxNameLength
				};
				//Whenever the user finishes inputting, ensure it's in our valid range, then update property
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
			else if (type == typeof(bool))
			{
				CheckBox toggle = new()
				{
						Id      = $"'{name}' (bool) toggle",
						Checked = (bool)propInfo.GetValue(Options)!,
						Text    = propInfo.GetValue(Options)!.ToString()
				};
				//Whenever value changed, update property and display
				toggle.Toggled += b =>
				{
					//For some reason the bool is the previous state, so flip it to get the current
					b ^= true;
					propInfo.SetValue(Options, b);
					toggle.Text = propInfo.GetValue(Options)!.ToString();
				};
				containerView.Add(toggle);
			}
			else if (type.IsAssignableTo(typeof(Enum)))
			{
				ComboBox combo  = new() { Id = $"'{name}' enum combobox" };
				Array    values = Enum.GetValues(type);
				combo.SetSource(values);
				combo.SelectedItemChanged += args => { propInfo.SetValue(Options, values.GetValue(args.Item)); };
				//HACK: Doesn't seem to be a way to directly change the selected item, gotta reflect it.
				//Set the selected item to be the current value of the property
				object currentValue = propInfo.GetValue(Options)!;
				typeof(ComboBox).GetMethod("SetValue", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(combo, new[] { currentValue });
				containerView.Add(combo);
			}
			else
			{
				containerView.Add(
						new Label($"ERROR: Type {type} unsupported")
						{
								Id = $"'{name}' ({type}) error label"
						}
				);
			}

		#endregion

			mainWin.Add(containerView);
			prevContainer = containerView;
		}

		//Add scene selector
		ComboBox sceneSelect = new()
		{
				Id     = "Scene Selector ComboBox",
				Text   = "Select a scene to render",
				X      = 0, Y = Pos.Bottom(prevContainer),
				Height = Fill(),
				Width  = Fill()
		};
		sceneSelect.SetSource(BuiltinScenes.GetAll().ToList());
		sceneSelect.SelectedItemChanged += args => Scene = (Scene)args.Value;

		//Set the correct widths for the name and value views
		foreach (View view in mainWin.Subviews)
		{
		}

		//TODO: Quit button, with validation
		Button confirmButton = new("Confirm Settings") { X = 0, Y = Pos.Bottom(mainWin) - 3, Height = 1 };
		confirmButton.Clicked += () =>
		{
			if (MessageBox.Query("Confirm", "Are you sure you wish to confirm these settings?", "Yes", "No") == 0)
				RequestStop();
		};
		mainWin.Add(confirmButton);
		Add(mainWin);
	}

	//NOTE: If these providers are reused, we get problems with the input fields all showing the same result (due to shared provider state)
	//	So we have to make the properties return a new instance each time to avoid this
	private static ITextValidateProvider FloatValidator => new TextRegexProvider(@"^-?0*?((?'integer'\d+)|(?'PreFloat'\d+\.\d*)|(?'PostFloat'\.\d+)|(?'Infinity'∞))$") { ValidateOnInput = true };

	private static ITextValidateProvider IntValidator => new TextRegexProvider(@"^-?0*?(\d+)$") { ValidateOnInput = true };

	public RenderOptions Options { get; }              = RenderOptions.Default; //Start with default values
	public Scene?        Scene   { get; private set; } = null;                  //Can only be null if the user quits without selecting the scene (i hope)
}