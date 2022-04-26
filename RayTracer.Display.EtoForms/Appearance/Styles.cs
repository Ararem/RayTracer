using Eto;
using Eto.Drawing;
using Eto.Forms;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using static Serilog.Log;

namespace RayTracer.Display.EtoForms.Appearance;

public static class Styles
{
	public const int GeneralFontSize = 10;
	internal static void RegisterStyles()
	{
		Information("Registering styles");

		Verbose("Registering style {Style}", AppTitle);
		Style.Add(AppTitle, static (Label control) =>
		{
			control.Font      = new Font(FontFamilies.Sans, 40, FontStyle.Bold);
			control.TextColor = new Color(1f, 1f, 1f);
		});

		Verbose("Registering style {Style}", GeneralTextual);
		Style.Add(GeneralTextual, static (CommonControl control) =>
		{
			control.Font = new Font(FontFamilies.Sans, GeneralFontSize);
		});

		Verbose("Registering style {Style}", ConsistentTextWidth);
		Style.Add(ConsistentTextWidth, static (CommonControl control) =>
		{
			control.Font = new Font(FontFamilies.Monospace, GeneralFontSize);
		});
	}

	/// <summary>
	/// Style for the title of the application
	/// </summary>
	public const string AppTitle = nameof(AppTitle);
	/// <summary>
	/// General textual control; e.g. button, label, description
	/// </summary>
	public const string GeneralTextual = nameof(AppTitle);
	/// <summary>
	/// Consistent text width - monospace font
	/// </summary>
	public const string ConsistentTextWidth = nameof(AppTitle);
}