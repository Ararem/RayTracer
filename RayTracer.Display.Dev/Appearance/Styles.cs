using Aardvark.Base;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using System.Linq;
using System.Reflection;
using static Serilog.Log;

namespace RayTracer.Display.Dev.Appearance;

public static class Styles
{
	/// <summary>Standard size of the font for most styles</summary>
	public const int GeneralFontSize = 10;
	public const int HeadingSize = 16;

	public static StyleWidgetHandler<Label> AppTitle => static control =>
	{
		control.Font      = new Font(FontFamilies.Sans, 40, FontStyle.Bold);
		control.TextColor = new Color(1f, 1f, 1f);
	};

	public static StyleWidgetHandler<CommonControl> General => static control => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize); };

	public static StyleWidgetHandler<CommonControl> Italic => static control => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize, FontStyle.Italic); };

	public static StyleWidgetHandler<CommonControl> Bold => static control => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize, FontStyle.Bold); };

	public static StyleWidgetHandler<CommonControl> Underline => static control => { control.Font = new Font(FontFamilies.Sans, GeneralFontSize, FontStyle.None, FontDecoration.Underline); };

	public static StyleWidgetHandler<CommonControl> Monospace => static control => { control.Font = new Font(FontFamilies.Monospace, GeneralFontSize); };
	public static Padding                           DefaultPadding => new(10, 10);
	public static Size                              DefaultSpacing => new(10, 10);

	internal static void RegisterStyles()
	{
		Information("Registering styles");

		MethodInfo addStyle = typeof(Style).GetMethod(nameof(Style.Add))!;
		foreach (PropertyInfo prop in typeof(Styles).GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.PropertyType == typeof(StyleWidgetHandler<>)))
		{
			string name = prop.Name;
			object? styleHandler = prop.GetValue(null);
			if(styleHandler is null) Warning("Style property {Property} was null", prop);
			Verbose("Found style {StyleName}", name);
			addStyle.Invoke(null, new[]{name, styleHandler});
		}
	}
}