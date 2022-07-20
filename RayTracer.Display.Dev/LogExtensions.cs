using Eto;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Reflection;

namespace RayTracer.Display.Dev;

internal class LogExtensions
{
	private static readonly ConcurrentDictionary<Type, PropertyInfo?> IdStringPropertyCache = new();

	internal static LoggerConfiguration AdjustConfig(LoggerConfiguration arg)
	{
		static PropertyInfo? GetIdProp(Type type)
		{
			return IdStringPropertyCache.GetOrAdd(type, static t => t.GetProperty(nameof(Widget.ID)));
		}

		return arg.Destructure.ByTransformingWhere<object>(
						static type => GetIdProp(type) is not null, //Check if we have the property cached in the dictionary
						static obj => GetIdProp(obj.GetType())?.GetValue(obj) ?? $"<ERROR: Type has no property {obj.GetType()}>"
				);
		;
	}
}