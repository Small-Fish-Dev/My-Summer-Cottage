namespace QOA;

// https://github.com/pfusik/qoa-ci/blob/master/transpiled/QOA.cs

/// <summary>
/// The base for the Quite Okay Audio format.
/// </summary>
public abstract class Base
{
	/// <summary> 
	/// Maximum number of channels supported by the format. 
	/// </summary>
	public const int MaxChannels = 8;

	/// <summary>
	/// Returns the number of audio channels.
	/// </summary>
	public int Channels => FrameHeader >> 24;

	/// <summary>
	/// Returns the sample rate in Hz.
	/// </summary>
	public int SampleRate => FrameHeader & 16777215;

	/// <summary>
	/// Maximum number of samples per frame.
	/// </summary>
	public const int MaxFrameSamples = 5120;

	protected static readonly ushort[] ScaleFactors = { 1, 7, 21, 45, 84, 138, 211, 304, 421, 562, 731, 928, 1157, 1419, 1715, 2048 };

	protected int FrameHeader;
	protected const int SliceSamples = 20;
	protected const int MaxFrameSlices = 256;

	protected int GetFrameBytes( int sampleCount )
	{
		int slices = (sampleCount + 19) / 20;
		return 8 + Channels * (16 + slices * 8);
	}

	protected static int Dequantize( int quantized, int scaleFactor )
	{
		var dequantized = default ( int );

		switch ( quantized >> 1 )
		{
			case 0:
				dequantized = (scaleFactor * 3 + 2) >> 2;
				break;

			case 1:
				dequantized = (scaleFactor * 5 + 1) >> 1;
				break;

			case 2:
				dequantized = (scaleFactor * 9 + 1) >> 1;
				break;

			default:
				dequantized = scaleFactor * 7;
				break;
		}

		return (quantized & 1) != 0 
			? -dequantized 
			: dequantized;
	}
}
