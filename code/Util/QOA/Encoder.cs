namespace QOA;

#pragma warning disable CS0675
public class Encoder : Base
{
	/// <summary>
	/// A boolean determining if this encoder is valid.
	/// </summary>
	public bool Valid { get; private set; }

	private static readonly int[] writeFramereciprocals = { 65536, 9363, 3121, 1457, 781, 475, 311, 216, 156, 117, 90, 71, 57, 47, 39, 32 };
	private readonly byte[] writeFramequantTab = { 7, 7, 7, 5, 5, 3, 3, 1, 0, 0, 2, 2, 4, 4, 6, 6, 6 };

	private readonly LMS[] LMSes = new LMS[8];
	private MemoryStream stream;
	private BinaryWriter writer;

	/// <summary>
	/// Initializes a new QOA encoder from sample count and rate, and the amount of channels.
	/// </summary>
	/// <param name="sampleCount"></param>
	/// <param name="sampleRate"></param>
	/// <param name="channels"></param>
	public Encoder( int sampleCount, int sampleRate, int channels )
	{
		stream = new();
		writer = new( stream );

		for ( int i = 0; i < 8; i++ )
			LMSes[i] = new LMS();

		Valid = writeHeader( sampleCount, sampleRate, channels );
	}

	private bool writeHeader( int sampleCount, int sampleRate, int channels )
	{
		if ( sampleCount <= 0 || channels <= 0 || channels > 8 || sampleRate <= 0 || sampleRate >= 16777216 )
			return false;

		FrameHeader = channels << 24 | sampleRate;

		for ( int c = 0; c < channels; c++ )
		{
			Array.Clear( LMSes[c].History, 0, 4 );

			LMSes[c].Weights[0] = 0;
			LMSes[c].Weights[1] = 0;
			LMSes[c].Weights[2] = -8192;
			LMSes[c].Weights[3] = 16384;
		}

		long magic = 1903124838;
		return writeLong( magic << 32 | sampleCount );
	}

	private bool writeLMS( int[] a )
	{
		long a0 = a[0];
		long a1 = a[1];
		long a2 = a[2];

		return writeLong( a0 << 48 |
			(a1 & 65535) << 32 |
			(a2 & 65535) << 16 |
			(a[3] & 65535) );
	}

	private bool writeLong( long l )
	{
		// Do it this way because s&box has these whitelisted.
		var bytes = BitConverter.GetBytes( l );
		Array.Reverse( bytes );
		writer.Write( bytes );

		return true;
	}

	/// <summary>
	/// Gets the currently encoded data in bytes.
	/// </summary>
	public byte[] Data => stream.ToArray();

	/// <summary>
	/// Encodes and writes a samples.
	/// </summary>
	public bool WriteSamples( short[] samples, int samplesCount )
	{
		if ( samplesCount <= 0 || samplesCount > 5120 )
			return false;

		var header = (long)FrameHeader;
		if ( !writeLong( header << 32 | samplesCount << 16 | GetFrameBytes( samplesCount ) ) )
			return false;

		var channels = Channels;
		for ( int c = 0; c < channels; c++ )
		{
			if ( !writeLMS( LMSes[c].History ) || !writeLMS( LMSes[c].Weights ) )
				return false;
		}
		
		var lms = new LMS();
		var bestLMS = new LMS();

		for ( int sampleIndex = 0; sampleIndex < samplesCount; sampleIndex += 20 )
		{
			var sliceSamples = samplesCount - sampleIndex;
			if ( sliceSamples > 20 )
				sliceSamples = 20;

			for ( int c = 0; c < channels; c++ )
			{
				var bestError = 9223372036854775807;
				var bestSlice = (long)0;
				for ( int scaleFactor = 0; scaleFactor < 16; scaleFactor++ )
				{
					lms.Assign( LMSes[c] );

					var reciprocal = writeFramereciprocals[scaleFactor];
					var slice = (long)scaleFactor;
					var currentError = (long)0;
					for ( int s = 0; s < sliceSamples; s++ )
					{
						var sample = samples[(sampleIndex + s) * channels + c];
						var predicted = lms.Predict();
						var residual = sample - predicted;
						var scaled = (residual * reciprocal + 32768) >> 16;

						if ( scaled != 0 )
							scaled += scaled < 0 ? 1 : -1;

						if ( residual != 0 )
							scaled += residual > 0 ? 1 : -1;

						var quantized = writeFramequantTab[8 + Math.Clamp( scaled, -8, 8 )];
						var dequantized = Dequantize( quantized, ScaleFactors[scaleFactor] );
						var reconstructed = Math.Clamp( predicted + dequantized, -32768, 32767 );
						var error = sample - reconstructed;

						currentError += error * error;
						if ( currentError >= bestError )
							break;
						
						lms.Update( reconstructed, dequantized );
						slice = slice << 3 | quantized;
					}

					if ( currentError < bestError )
					{
						bestError = currentError;
						bestSlice = slice;
						bestLMS.Assign( lms );
					}
				}
				
				LMSes[c].Assign( bestLMS );
				bestSlice <<= (20 - sliceSamples) * 3;

				if ( !writeLong( bestSlice ) )
					return false;
			}
		}

		return true;
	}
}
#pragma warning restore CS0675
