using System.Buffers;

namespace RayTracer.Core;

/// <summary>Helper class for pooling <see cref="ArraySegment{T}"/>s. Useful for creating temporary fixed-size collections in a performant manner.</summary>
public static class ArraySegmentPool
{
	public static ArraySegment<T> GetPooledSegment<T>(int length)
	{
		T[] array = ArrayPool<T>.Shared.Rent(length);
		return new ArraySegment<T>(array, 0, length);
	}

	public static void ReturnSegment<T>(ArraySegment<T> segmentToReturn)
	{
		T[] array = segmentToReturn.Array ?? throw new InvalidOperationException("Segment did not contain a reference to a non-null array");
		ArrayPool<T>.Shared.Return(array, true);
	}

	public static ArraySegment<T> SegmentFromSingle<T>(T value)
	{
		ArraySegment<T> seg = GetPooledSegment<T>(1);
		seg[0] = value;
		return seg;
	}
}