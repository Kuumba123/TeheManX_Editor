using System;

namespace TeheManX_Editor.Forms;

public static class ColorTools
{
	// This is mostly the equivalent of:
	// floor(color * 8) + floor(color / 4)
	public static byte To24Bit(byte color)
	{
		return (byte)((color << 3) | (color >> 2));
	}

	// Same as above but for int.
	public static byte To24Bit(int color)
	{
		return (byte)((color << 3) | (color >> 2));
	}

	// Reverse the operation and round the result.
	public static byte To15Bit(double color)
	{
		double offset = Math.Floor(Math.Floor(color / 8.0) / 4.0);
		return (byte)Math.Round((color - offset) / 8.0);
	}
}