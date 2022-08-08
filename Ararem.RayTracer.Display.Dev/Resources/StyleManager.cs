using Eto;
using Eto.Drawing;
using Eto.Forms;
using JetBrains.Annotations;
using System;
using System.Linq;
using System.Reflection;
using static Serilog.Log;

namespace Ararem.RayTracer.Display.Dev.Resources;

/// <summary>
///  Static class that contains definitions for built-in styles. To reference a style, you should use it's name:
///  <c>myControl.Style = nameof(StyleManager.Bold);</c>
/// </summary>
/// <remarks>
///  Most styles should work for most widgets, but if a style isn't working when it should (e.g. <see cref="StyleManager.Heading"/> on a <see cref="GroupBox"/>) due
///  to mismatched base types, you can try using the `Force_XXX` style instead. This will attempt to dynamically access the relevant members of the type
///  to assign them. This will only work in specific scenarios, where the target type (i.e. <see cref="GroupBox"/>) has a definition for a property that
///  should be styled (i.e. <see cref="GroupBox.Font"/>), but the standard style (i.e. <see cref="StyleManager.Heading"/>) only works for a different base type (i.e.
///  <see cref="CommonControl"/>). In this example, since the <see cref="GroupBox"/> has a "Font" property, we can use the <see cref="StyleManager.Force_Heading"/>
///  style to dynamically (forcefully) change the property (since it accepts any object of type <see cref="Widget"/>).
/// </remarks>
[PublicAPI]
public static class StyleManager
{
	/// <summary>Standard size of the font for most styles</summary>
	public const int GeneralFontSize = 11;

	/// <summary>
	/// Font size for heading text
	/// </summary>
	public const int HeadingFontSize = 20;

	static StyleManager()
	{
		Debug("Initializing Style Manager");
		MethodInfo genericAddStyleMethod = ((Action<string, StyleWidgetHandler<Widget>>)Style.Add).Method.GetGenericMethodDefinition();
		//Loop over all public, static properties that are style handlers for widgets
		//The reflection stuff is a bit funky, but so are generics so  ¯\_(ツ)_/¯
		foreach (PropertyInfo prop in typeof(StyleManager).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType.IsGenericType && (p.PropertyType.GetGenericTypeDefinition() == typeof(StyleWidgetHandler<>))))
		{
			string styleName = prop.Name;
			Verbose("Found style {StyleName}", styleName);
			object? styleHandler = prop.GetValue(null);
			if (styleHandler is null)
			{
				Warning("Style property {Property} was null", prop);
				continue;
			}

			genericAddStyleMethod.MakeGenericMethod(prop.PropertyType.GenericTypeArguments).Invoke(null, new[] { styleName, styleHandler });
		}

		Debug("Initialized Style Manager");
	}

	/// <summary>
	/// Style for a <see cref="Label"/> for the app title
	/// </summary>
	public static StyleWidgetHandler<Label> AppTitle => static control =>
	{
		control.Font      = new Font(FontFamilies.Sans, 40, FontStyle.Bold);
		control.TextColor = new Color(1f, 1f, 1f);
	};

	/// <inheritdoc cref="AppTitle"/>
	/// <remarks>Only use if <see cref="AppTitle"/> does not work</remarks>
	public static StyleWidgetHandler<Widget> Force_AppTitle => static control =>
	{
		((dynamic)control).Font      = new Font(FontFamilies.Sans, 40, FontStyle.Bold);
		((dynamic)control).TextColor = new Color(1f, 1f, 1f);
	};

	public static StyleWidgetHandler<CommonControl> General         => static control => { control.Font          = new Font(FontFamilies.Sans,      GeneralFontSize); };
	public static StyleWidgetHandler<CommonControl> Heading         => static control => { control.Font          = new Font(FontFamilies.Sans,      HeadingFontSize, FontStyle.Bold, FontDecoration.Underline); };
	public static StyleWidgetHandler<CommonControl> Italic          => static control => { control.Font          = new Font(FontFamilies.Sans,      GeneralFontSize, FontStyle.Italic); };
	public static StyleWidgetHandler<CommonControl> Bold            => static control => { control.Font          = new Font(FontFamilies.Sans,      GeneralFontSize, FontStyle.Bold); };
	public static StyleWidgetHandler<CommonControl> Underline       => static control => { control.Font          = new Font(FontFamilies.Sans,      GeneralFontSize, FontStyle.None, FontDecoration.Underline); };
	public static StyleWidgetHandler<CommonControl> Monospace       => static control => { control.Font          = new Font(FontFamilies.Monospace, GeneralFontSize); };
	public static StyleWidgetHandler<Widget>        Force_General   => static widget => { ((dynamic)widget).Font = new Font(FontFamilies.Sans,      GeneralFontSize); };
	public static StyleWidgetHandler<Widget>        Force_Heading   => static widget => { ((dynamic)widget).Font = new Font(FontFamilies.Sans,      HeadingFontSize, FontStyle.Bold, FontDecoration.Underline); };
	public static StyleWidgetHandler<Widget>        Force_Italic    => static widget => { ((dynamic)widget).Font = new Font(FontFamilies.Sans,      GeneralFontSize, FontStyle.Italic); };
	public static StyleWidgetHandler<Widget>        Force_Bold      => static widget => { ((dynamic)widget).Font = new Font(FontFamilies.Sans,      GeneralFontSize, FontStyle.Bold); };
	public static StyleWidgetHandler<Widget>        Force_Underline => static widget => { ((dynamic)widget).Font = new Font(FontFamilies.Sans,      GeneralFontSize, FontStyle.None, FontDecoration.Underline); };
	public static StyleWidgetHandler<Widget>        Force_Monospace => static widget => { ((dynamic)widget).Font = new Font(FontFamilies.Monospace, GeneralFontSize); };
	public static Padding                           DefaultPadding  => new(5, 5);
	public static Size                              DefaultSpacing  => new(10, 10);
}