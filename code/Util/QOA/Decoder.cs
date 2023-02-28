namespace QOA;

public class Decoder : Base
{
	/// <summary>
	/// A boolean determining if the decoded data was valid.
	/// </summary>
	public bool Valid { get; private set; }

	/// <summary
	/// >Returns the file length in samples per channel.
	/// </summary>
	public int SampleCount => totalSamples;

	/// <summary>
	/// A boolean determining if all the samples have been read.
	/// </summary>
	public bool Ended => positionSamples >= totalSamples;

	private MemoryStream stream;
	private BinaryReader reader;

	private int buffer;
	private int bufferBits;

	private int totalSamples;
	private int positionSamples;

	private int maxFrameBytes => 8 + Channels * 2064;

	/// <summary>
	/// Initializes a QOA decoder from an array of bytes.
	/// </summary>
	/// <param name="data"></param>
	public Decoder( byte[] data )
	{
		stream = new( data );
		reader = new( stream );

		Valid = readHeader();
	}

	private bool readLMS( int[] result )
	{
		for ( int i = 0; i < 4; i++ )
		{
			var hi = readByte();
			if ( hi < 0 )
				return false;

			var lo = readByte();
			if ( lo < 0 )
				return false;

			result[i] = ((hi ^ 128) - 128) << 8 | lo;
		}

		return true;
	}

	private int readByte()
		=> reader.ReadByte();

	private int readBits( int bits )
	{
		while ( bufferBits < bits )
		{
			var b = readByte();
			if ( b < 0 )
				return -1;

			buffer = buffer << 8 | b;
			bufferBits += 8;
		}

		bufferBits -= bits;

		var result = buffer >> bufferBits;
		buffer &= (1 << bufferBits) - 1;

		return result;
	}

	private bool readHeader()
	{
		if ( readByte() != 'q' || readByte() != 'o' || readByte() != 'a' || readByte() != 'f' )
			return false;

		bufferBits = buffer = 0;
		totalSamples = readBits( 32 );

		if ( totalSamples <= 0 )
			return false;

		FrameHeader = readBits( 32 );
		if ( FrameHeader <= 0 )
			return false;
		positionSamples = 0;

		return Channels > 0
			&& Channels <= 8
			&& SampleRate > 0;
	}

	/// <summary>
	/// Reads and decodes a frame.
	/// </summary>
	public int ReadSamples( short[] buffer )
	{
		if ( positionSamples > 0 && readBits( 32 ) != FrameHeader )
			return -1;

		var samplesCount = readBits( 16 );
		if ( samplesCount <= 0 || samplesCount > 5120 || samplesCount > totalSamples - positionSamples )
			return -1;

		var slices = (samplesCount + 19) / 20;
		if ( readBits( 16 ) != 8 + Channels * (16 + slices * 8) )
			return -1;

		var lmses = new LMS[8];
		for ( int _i0 = 0; _i0 < 8; _i0++ )
			lmses[_i0] = new LMS();

		for ( int c = 0; c < Channels; c++ )
		{
			if ( !readLMS( lmses[c].History ) || !readLMS( lmses[c].Weights ) )
				return -1;
		}
		
		for ( int sampleIndex = 0; sampleIndex < samplesCount; sampleIndex += 20 )
		{
			for ( int c = 0; c < Channels; c++ )
			{
				var scaleFactor = readBits( 4 );
				
				if ( scaleFactor < 0 )
					return -1;
				scaleFactor = ScaleFactors[scaleFactor];

				var sampleOffset = sampleIndex * Channels + c;
				for ( int s = 0; s < 20; s++ )
				{
					var quantized = readBits( 3 );
					if ( quantized < 0 )
						return -1;

					if ( sampleIndex + s >= samplesCount )
						continue;

					var dequantized = Dequantize( quantized, scaleFactor );
					var reconstructed = Math.Clamp( lmses[c].Predict() + dequantized, -32768, 32767 );

					lmses[c].Update( reconstructed, dequantized );
					buffer[sampleOffset] = (short)reconstructed;
					sampleOffset += Channels;
				}
			}
		}

		positionSamples += samplesCount;
		return samplesCount;
	}

	/// <summary>
	/// Seeks to the given time offset.
	/// </summary>
	public void SeekToSample( int position )
	{
		var frame = position * 2 / 5120;
		stream.Seek( frame == 0 ? 12 : 8 + frame * maxFrameBytes, SeekOrigin.Begin );
		positionSamples = frame * 5120;
	}
}
