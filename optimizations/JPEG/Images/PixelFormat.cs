using System;

namespace JPEG.Images;

public struct PixelFormat : IEquatable<PixelFormat>
{
	private string Format;

	private PixelFormat(string format)
	{
		Format = format;
	}

	public static PixelFormat RGB => new(nameof(RGB));
	public static PixelFormat YCbCr => new(nameof(YCbCr));


    public bool Equals(PixelFormat other)
    {
        return string.Equals(Format, other.Format);
    }

    public override bool Equals(object obj)
    {
        if (obj.GetType() != this.GetType()) return false;
        return Equals((PixelFormat)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (Format != null ? Format.GetHashCode() : 0);
        }
    }

	public static bool operator == (PixelFormat a, PixelFormat b)
	{
		return a.Equals(b);
	}

	public static bool operator !=(PixelFormat a, PixelFormat b)
	{
		return !a.Equals(b);
	}

	public override string ToString()
	{
		return Format;
	}
}