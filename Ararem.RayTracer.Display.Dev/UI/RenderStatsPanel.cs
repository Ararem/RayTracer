using Ararem.RayTracer.Core;
using Eto.Forms;
using LibArarem.Core.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Ararem.RayTracer.Display.Dev.UI;

public sealed partial class RenderJobPanel
{
	/// <summary><see cref="Panel"/> class that displays the statistics of a <see cref="RenderJob"/></summary>
	private sealed class RenderStatsPanel : Panel
	{
		private readonly ILogger log;

		private RenderStats? currentStats = null, previousStats = null;

		private bool extendedInfo = false;

		public RenderStatsPanel(RenderJobPanel parentJobPanel)
		{
			ParentJobPanel = parentJobPanel;
			log            = LogUtils.WithInstanceContext(this);
		}

		/// <summary>The <see cref="RenderJobPanel"/> that contains this instance as a child object (aka the panel that created this panel)</summary>
		public RenderJobPanel ParentJobPanel { get; }

		/// <summary>Main <see cref="DynamicLayout"/> for this panel. Is the same as accessing <see cref="Panel.Content"/></summary>
		public DynamicLayout Layout { get; private set; }

		/// <inheritdoc/>
		protected override void OnPreLoad(EventArgs e)
		{
			log.TrackEvent(this, e);
			Content = Layout = new DynamicLayout
			{
					ID      = $"{ID}/Content",
					Spacing = DefaultSpacing,
					Padding = DefaultPadding
			};
			base.OnPreLoad(e);
		}

		public void Update()
		{
			/*
			 * TODO: When I refactor this, I would like it to be really nice and smooth
			 * I'm thinking for the architecture, have a list of
			 * Category:
			 *	|___ Value Name
			 */
		#region Format methods

			//TODO: Monad style for these?
			const string numberFormat = "n2";
			const string metresUnit   = " m";
			const string degreesUnit  = " °";
			const string noUnit       = "";
			const string ratioUnit    = " px/px";

			static string Vec(Vector3 v)
			{
				return v.ToString(numberFormat);
			}

			static string Int(int i)
			{
				return i.ToString(numberFormat);
			}

			static string Float(float f, string unit)
			{
				return f.ToString(numberFormat) + unit;
			}

		#endregion
			RenderJob?   job                    = ParentJobPanel.RenderJob;
			Stopwatch    sw                     = Stopwatch.StartNew();
			const string noStatsAvailableString = "N/A";
			if (job is null) //If renderJob is null, we have no stats
			{
				currentStats = previousStats = null;
			}
			else
			{
				//Sets everything from previous update to zero if this is the first update (and therefore currentStats is also null)
				previousStats = currentStats is null ? new RenderStats(job.RenderOptions) : new RenderStats(currentStats);
				currentStats  = job.RenderStats;
			}

			List<DynamicGroup> groups = new();

			Layout.Clear();
			CheckBox toggleExtendedInfoCheck = new() { Checked = extendedInfo };
			toggleExtendedInfoCheck.CheckedChanged += (_, _) => extendedInfo = toggleExtendedInfoCheck.Checked ?? false;
			Layout.AddRow("Show Extended Info", toggleExtendedInfoCheck);
			Layout.BeginScrollable(padding: DefaultPadding, spacing: DefaultSpacing);

			{
				Scene? scene = job?.Scene;
				groups.Add(Layout.BeginGroup("Scene", DefaultPadding, DefaultSpacing));
				Layout.AddRow("Name",    scene?.Name                           ?? noStatsAvailableString);
				Layout.AddRow("Objects", scene?.SceneObjects.Length.ToString() ?? noStatsAvailableString);
				Layout.AddRow("Lights",  scene?.Lights.Length.ToString()       ?? noStatsAvailableString);
				{
					Camera? cam = scene?.Camera;
					Layout.AddRow("Camera", "Position:",     cam is {} ? $"{Vec(cam.LookFrom)} => {Vec(cam.LookTowards)}" : noStatsAvailableString);
					Layout.AddRow(null,     "Vertical Fov:", cam is {} ? Float(cam.VerticalFov, degreesUnit) : noStatsAvailableString);
					Layout.AddRow(null,     "Aspect:",       cam is {} ? Float(cam.AspectRatio, ratioUnit) : noStatsAvailableString);
					Layout.AddRow(null,     "Lens:",         cam is {} ? $"Rad={Float(cam.LensRadius, metresUnit)} Dst={Float(cam.FocusDistance, metresUnit)}" : noStatsAvailableString);
					if (extendedInfo)
					{
						Layout.AddRow(null, "Forward:",         cam is {} ? Vec(cam.LookDirection) : noStatsAvailableString);
						Layout.AddRow(null, "Up:",              cam is {} ? Vec(cam.UpVector) : noStatsAvailableString);
						Layout.AddRow(null, "Horizontal:",      cam is {} ? Vec(cam.Horizontal) : noStatsAvailableString);
						Layout.AddRow(null, "Vertical:",        cam is {} ? Vec(cam.Vertical) : noStatsAvailableString);
						Layout.AddRow(null, "LowerLeftCorner:", cam is {} ? Vec(cam.LowerLeftCorner) : noStatsAvailableString);
						Layout.AddRow(null, "UV:",              cam is {} ? $"U={Vec(cam.U)}\nV={Vec(cam.V)}" : noStatsAvailableString);
					}
				}

				Layout.EndGroup();
			}

			Layout.EndScrollable();
			Layout.Create();

			//Apply styles
			for (int i = 0; i < groups.Count; i++) groups[i].GroupBox.Style = nameof(Force_Bold);

			Invalidate(true); //Mark for redraw
			log.Verbose("Stats updated in {Elapsed:#00.000 'ms'}", sw.Elapsed.TotalMilliseconds);

		}
	}
}