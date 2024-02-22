namespace Sauna;

public partial class SlotMachine : Component
{
	#region Constants
	public const float ANGLE_STEP = 22.5f;
	public const int COUNT = 8;
	public const float ROLL_TIME = 5f;
	public const float WAIT_TIME = 1f;

	public const int DEFAULT_BET = 5;
	#endregion

	[Property] public SkinnedModelRenderer Model { get; set; }
	[Sync, Property] public BetFlag BetFlags { get; set; }
	[Sync, Property] public int Money { get; set; }

	public int Bet => BitOperations.PopCount( (uint)BetFlags ) * DEFAULT_BET;

	[Sync] public bool Rolling { get; set; }
	[Sync] public Result RollResult { get; set; }

	TimeSince sinceResult;
	int showCount;

	[Flags]
	public enum BetFlag : byte
	{
		None = 0,
		First = 1 << 0,
		Second = 1 << 1,
		Third = 1 << 2,

		All = First | Second | Third
	}

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

	public void TryRoll()
	{
		if ( BetFlags == BetFlag.None || Money < Bet )
			return;

		Rolling = true;
		Money -= Bet;

		RollResult = new Result
		{
			a = new Random().Next( 0, 7 ),
			b = new Random().Next( 0, 7 ),
			c = new Random().Next( 0, 7 )
		};

		sinceResult = 0;
		showCount = 0;
	}

	private void UpdateBodygroups()
	{
		for ( int i = 0; i < 3; i++ )
		{
			var val = BetFlags.HasFlag( (BetFlag)(1 << i) ) ? 1 : 0;
			Model?.SetBodyGroup( 8 + i, val );
			Model?.SetBodyGroup( i == 0 ? 2 : i == 1 ? 1 : 1 + i, val ); // THANKS GROD BERT!!
		}
	}

	public void InsertCoin( Player player )
	{
		// todo: play coin sounds
		if ( player.TakeMoney( 1 ) )
			Money++;
	}

	public void Cashout( Player player )
	{
		player.Money += Money;
		Money = 0;
		// todo: play coin sounds
	}

	public void ToggleBetFlag( byte line )
	{
		var flags = (byte)BetFlags;
		BetFlags = (BetFlag)(flags ^ (1 << (line - 1)));

		UpdateBodygroups();
	}

	private void CheckForWin()
	{
		if ( !Rolling || sinceResult < ROLL_TIME + WAIT_TIME )
			return;

		Rolling = false;
		Money += CalculateWin();

		// todo: Actually calculate win based on slot results.
		// todo: Play sounds on win etc.
	}

	protected override void OnStart()
	{
		UpdateBodygroups();
	}

	protected override void OnUpdate()
	{
		// Calculate win.
		CheckForWin();
	}

	protected override void OnPreRender()
	{
		// Which slot results can we show already?
		showCount = MathX.FloorToInt( sinceResult / (ROLL_TIME / 3) ).Clamp( 0, 3 );
		for ( int i = 1; i <= 3; i++ )
		{
			var boneIndex = i + 1;
			var target = showCount >= i || !Rolling
				? RollResult[i] * ANGLE_STEP
				: sinceResult * 1000f;

			var fixedRotation = Transform.Rotation
				* Rotation.FromAxis( Vector3.Right, ANGLE_STEP / 2f + target )
				* Rotation.From( -90f, -90f, 0 );

			var transform = Model.SceneModel.GetBoneWorldTransform( boneIndex )
				.WithRotation( fixedRotation );

			Model.SceneModel.SetBoneWorldTransform( boneIndex, transform );
		}
	}
}
