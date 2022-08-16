using Ararem.RayTracer.Core;
using Eto.Forms;
using JetBrains.Annotations;
using LibArarem.Core.Logging;
using Serilog;
using System;
using System.Buffers;
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
			const string decimalFormat  = "n2"; //float, double, decimal
			const string integralFormat = "n0"; //int, long, ulong, short
			const string percentFormat  = "P";

			const string metresUnit  = " m";
			const string degreesUnit = " °";
			const string noUnit      = "";
			const string ratioUnit   = " px/px";
			const string pixelsUnit  = " px";

			const string noStatAvailable    = "N/A";
			const string infiniteStatString = "∞";

			static string Vec<TState>(TState? t, [RequireStaticDelegate] Func<TState, Vector3> getValue)
			{
				return t is null ? noStatAvailable : getValue(t).ToString(decimalFormat);
			}

			static string Int<TState, TNumber>(TState? t, [RequireStaticDelegate] Func<TState, TNumber> getValue, string unit) where TNumber : IBinaryInteger<TNumber>
			{
				return (t is null ? noStatAvailable : getValue(t).ToString(integralFormat, null)) + unit;
			}

			static string Float<TState, TNumber>(TState? t, [RequireStaticDelegate] Func<TState, TNumber> getValue, string unit) where TNumber : IFloatingPoint<TNumber>
			{
				return (t is null ? noStatAvailable : getValue(t).ToString(integralFormat, null)) + unit;
			}

			//Total can be null if there is no upper bound
			static string Percentage<TState, TNumber>(TState? t, [RequireStaticDelegate] Func<TState, (TNumber Amount, TNumber? Total)> getValue)
					where TNumber : struct, INumber<TNumber>
			{
				string val;
				if (t is null)
				{
					val = noStatAvailable;
				}
				else
				{
					(TNumber tAmount, TNumber? maybeTTotal) = getValue(t);
					if (maybeTTotal is not {} tTotal || TNumber.IsInfinity(tTotal))
					{
						val = $"1/{infiniteStatString}%";
					}
					else
					{
						float amount   = ((IConvertible)tAmount).ToSingle(null);
						float total    = ((IConvertible)tTotal).ToSingle(null);
						float fraction = amount / total;
						val = fraction.ToString(percentFormat, null);
					}
				}

				return $"({val})";
			}

			static string String<TState>(TState? t, [RequireStaticDelegate] Func<TState, string> getValue)
			{
				return t is null ? noStatAvailable : getValue(t);
			}

			static string Bool<TState>(TState? t, [RequireStaticDelegate] Func<TState, bool> getValue)
			{
				return t is null ? noStatAvailable : getValue(t).ToString();
			}

			static string Enum<TState, TEnum>(TState? t, [RequireStaticDelegate] Func<TState, TEnum> getValue) where TEnum : struct, Enum
			{
				return t is null ? noStatAvailable : getValue(t).ToString();
			}

		#endregion

			RenderJob? job = ParentJobPanel.RenderJob;
			Stopwatch  sw  = Stopwatch.StartNew();
			if (job is null) //If renderJob is null, we have no stats
			{
				currentStats = previousStats = null;
			}
			else
			{
				//Sets everything from previous update to zero if this is the first update (and therefore currentStats is also null)
				previousStats = currentStats is null ? new RenderStats() : new RenderStats(currentStats);
				currentStats  = job.RenderStats;
			}

			Layout.Clear();
			CheckBox toggleExtendedInfoCheck = new() { Checked = extendedInfo };
			toggleExtendedInfoCheck.CheckedChanged += (_, _) => extendedInfo = toggleExtendedInfoCheck.Checked ?? false;
			Layout.AddRow("Show Extended Info", toggleExtendedInfoCheck);
			Layout.BeginScrollable(padding: DefaultPadding, spacing: DefaultSpacing, border: BorderType.None);

			//Passes
			{
				AddSectionTitleLabel("Progress");
				RenderStats? stats = job?.RenderStats;
				Layout.AddRow(
						"Raw Pixels Rendered",
						$"{Int(stats, static s => s.PixelsRendered, pixelsUnit)}\n{Percentage(job, static delegate(RenderJob job) { return (job.RenderStats.PixelsRendered, job.RenderOptions.InfinitePasses ? null : (ulong?)job.RenderOptions.RenderWidth * job.RenderOptions.RenderHeight * job.RenderOptions.Passes); })}"
				);
			}

			{
				if (!extendedInfo) goto Skip;
				AddSectionTitleLabel("Depth");
				if (job is not null)
				{
					ulong   max    = job.RenderOptions.MaxBounceDepth;
					ulong[] depths = ArrayPool<ulong>.Shared.Rent((int)max);
					for (ulong i = 0; i < max; i++)
					{
						depths[i] = job.RenderStats.RayDepthCounts.TryGetValue(i, out ulong value) ? value : 0;
					}

					for (int i = 0; i < (int)max; i++)
					{
						Layout.AddRow(i.ToString(integralFormat), depths[i].ToString(integralFormat));
					}

					ArrayPool<ulong>.Shared.Return(depths);
				}

			Skip: ;
			}

			//Scene
			{
				AddSectionTitleLabel("Scene");
				Scene? scene = job?.Scene;
				Layout.AddRow("Name",         String(scene, static s => s.Name));
				Layout.AddRow("Object Count", Int(scene, static s => s.SceneObjects.Length, noUnit));
				Layout.AddRow("Light Count",  Int(scene, static s => s.Lights.Length,       noUnit));
				{
					Camera? cam = scene?.Camera;
					Layout.AddRow("Camera", "Position:",     cam is {} ? $"{Vec(cam, static c => c.LookFrom)} => {Vec(cam, static c => c.LookTowards)}" : noStatAvailable);
					Layout.AddRow(null,     "Vertical Fov:", Float(cam, static c => c.VerticalFov, degreesUnit));
					Layout.AddRow(null,     "Aspect:",       Float(cam, static c => c.AspectRatio, ratioUnit));
					Layout.AddRow(
							null, "Lens:",
							cam is {} ? $"{Float(cam, static c => c.LensRadius, metresUnit)} radius\n{Float(cam, static c => c.FocusDistance, metresUnit)} focus" : noStatAvailable
					);
					if (extendedInfo)
					{
						Layout.AddRow(null, "Forward:",         Vec(cam, static c => c.LookDirection));
						Layout.AddRow(null, "Up:",              Vec(cam, static c => c.UpVector));
						Layout.AddRow(null, "Horizontal:",      Vec(cam, static c => c.Horizontal));
						Layout.AddRow(null, "Vertical:",        Vec(cam, static c => c.Vertical));
						Layout.AddRow(null, "LowerLeftCorner:", Vec(cam, static c => c.LowerLeftCorner));
						Layout.AddRow(null, "UV:",              $"U: {Vec(cam, static c => c.U)}\nV: {Vec(cam, static c => c.V)}");
					}
				}
			}

			//Renderer
			{
				AddSectionTitleLabel("Renderer");
				Layout.AddRow("Live Threads", Int(job, static j => j.RenderStats.ThreadsRunning, noUnit));
				Layout.AddRow("Completed",    Bool(job, static j => j.RenderCompleted));
				Layout.AddRow("Status",       Enum(job, static j => j.RenderTask.Status));
			}

			//Rays
			{
				//Scatter
				//Absorb
				//Exceeded
				//Sky
				//Total
				//Max Depth?
			}
			//BVH Heirarchy
			{
				//Num nodes?
				//AABB Misses
				//Hittable Misses
				//Hittable Intersections
			}
			/*
			 * These are ignored since they're shown in the RenderControllerPanel
			 * KMin/KMax
			 * Bounce depth
			 * Debug Visualisation
			 * Image width/height
			 */

			Layout.EndScrollable();
			Layout.Create();

			Invalidate(true); //Mark for redraw
			log.Verbose("Stats updated in {Elapsed:#00.000 'ms'}", sw.Elapsed.TotalMilliseconds);

			void AddSectionTitleLabel(string title)
			{
				Label label = new() { Text = title, Style = nameof(Force_Bold) };
				Layout.AddSeparateColumn(label);
			}
		}
	}
}