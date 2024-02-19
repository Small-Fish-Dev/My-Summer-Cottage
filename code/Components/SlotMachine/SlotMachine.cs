namespace Sauna;

public sealed class SlotMachine : Component
{
	public const float ANGLE_STEP = 22.5f;
	public const int COUNT = 8;
	public const float ROLL_TIME = 5f;
	public const float WAIT_TIME = 1f;

	[Property] public SkinnedModelRenderer Model { get; set; }

	[Sync] public int Money { get; set; }
	[Sync] public int Bet { get; set; }
	[Sync] public bool Rolling { get; set; }
	[Sync] public Result RollResult { get; set; }

	/// <summary>
	/// Data structure for storing a slot machine roll.
	/// </summary>
	public struct Result
	{
		public int a;
		public int b;
		public int c;

		public int this[int index]
		{
			get
			{
				return index switch
				{
					1 => a,
					2 => b,
					3 => c,
					_ => 0,
				};
			}
		}

		public override string ToString()
		{
			return $"{a}, {b}, {c}";
		}
	}

	TimeSince sinceResult;
	int showCount;
	bool rollingPrevious = false;


	private void WaitForWin()
	{
		if ( !Rolling || sinceResult < ROLL_TIME + WAIT_TIME )
			return;

		Rolling = false;
		Money += 10;
	}

	protected override void OnUpdate()
	{
		// Calculate win.
		WaitForWin();

		// Check if we should be updating at all.
		if ( (!Rolling || sinceResult > ROLL_TIME) && rollingPrevious == Rolling )
			return;

		// Which slot results can we show already?
		showCount = MathX.FloorToInt( sinceResult / (ROLL_TIME / 3) ).Clamp( 0, 3 );

		for ( int i = 1; i <= 3; i++ )
		{
			var boneIndex = i + 1;
			var target = showCount >= i
				? RollResult[i] * ANGLE_STEP
				: sinceResult * 1000f;

			var fixedRotation = Transform.Rotation
				* Rotation.FromAxis( Vector3.Right, ANGLE_STEP / 2f + target )
				* Rotation.From( -90f, -90f, 0 );

			var transform = Model.SceneModel.GetBoneWorldTransform( boneIndex )
				.WithRotation( fixedRotation );

			Model.SceneModel.SetBoneWorldTransform( boneIndex, transform );
		}

		rollingPrevious = Rolling;
	}
}
