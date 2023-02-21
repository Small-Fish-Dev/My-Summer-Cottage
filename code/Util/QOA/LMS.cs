namespace QOA;

/// <summary>
/// Least mean squares filter.
/// </summary>
internal class LMS
{
	internal readonly int[] History = new int[4];

	internal readonly int[] Weights = new int[4];

	internal void Assign( LMS source )
	{
		Array.Copy( source.History, 0, History, 0, 4 );
		Array.Copy( source.Weights, 0, Weights, 0, 4 );
	}

	internal int Predict()
	{
		return ( History[0] * Weights[0] +
			History[1] * Weights[1] +
			History[2] * Weights[2] +
			History[3] * Weights[3] ) >> 13;
	}

	internal void Update( int sample, int residual )
	{
		var delta = residual >> 4;

		Weights[0] += History[0] < 0
			? -delta
			: delta;

		Weights[1] += History[1] < 0
			? -delta
			: delta;

		Weights[2] += History[2] < 0
			? -delta
			: delta;

		Weights[3] += History[3] < 0
			? -delta
			: delta;

		History[0] = History[1];
		History[1] = History[2];
		History[2] = History[3];
		History[3] = sample;
	}
}
