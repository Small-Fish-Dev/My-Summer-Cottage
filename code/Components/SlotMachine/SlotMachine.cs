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

	[Sync, Property] public BetFlag BetFlags
	{
		get => _betflags;
		set
		{
			_betflags = value;
			UpdateBodygroups();
		}
	}
	private BetFlag _betflags;

	[Sync, Property] public int Money { get; set; }

	public int Bet => BitOperations.PopCount( (uint)BetFlags ) * DEFAULT_BET;

	[Sync] public bool Rolling { get; set; }
	[Sync] public Result RollResult { get; set; }
	[Sync] public WheelMode Wheels { get; set; }

	[Sync] TimeSince SinceResult { get; set; }
	[Sync] int ShowCount { get; set; }

	[Flags]
	public enum BetFlag : byte
	{
		None = 0,
		First = 1 << 0,
		Second = 1 << 1,
		Third = 1 << 2,

		All = First | Second | Third
	}

	#region Data structures
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

	public struct WheelMode
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
	#endregion

	public void TryRoll()
	{
		if ( Rolling || BetFlags == BetFlag.None || Money < Bet )
			return;

		Rolling = true;
		Money -= Bet;

		RollResult = new Result
		{
			a = Sandbox.Game.Random.Int( 0, 7 ),
			b = Sandbox.Game.Random.Int( 0, 7 ),
			c = Sandbox.Game.Random.Int( 0, 7 )
		};

		Wheels = new WheelMode
		{
			a = Sandbox.Game.Random.Int( 1, 3 ),
			b = Sandbox.Game.Random.Int( 1, 3 ),
			c = Sandbox.Game.Random.Int( 1, 3 ),
		};

		SinceResult = 0;
		ShowCount = 0;
	}

	private void UpdateBodygroups()
	{
		for ( int i = 0; i < 3; i++ )
		{
			var val = BetFlags.HasFlag( (BetFlag)(1 << i) ) ? 1 : 0;
			Model?.SetBodyGroup( 10 + i, val );
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
		if ( Rolling )
			return;

		var flags = (byte)BetFlags;
		BetFlags = (BetFlag)(flags ^ (1 << (line - 1)));
	}

	private void CheckForWin()
	{
		if ( !Rolling || SinceResult < ROLL_TIME + WAIT_TIME )
			return;

		Rolling = false;
		Money += CalculateWin();

		// todo: Actually calculate win based on slot results.
		// todo: Play sounds on win etc.
	}

	protected override void OnStart()
	{
		Wheels = new WheelMode { a = 1, b = 1, c = 1 };

		if ( !Network.Active )
			GameObject.NetworkSpawn();

		Network.SetOwnerTransfer( OwnerTransfer.Takeover );

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
		ShowCount = MathX.FloorToInt( SinceResult / (ROLL_TIME / 3) ).Clamp( 0, 3 );
		for ( int i = 1; i <= 3; i++ )
		{
			var boneIndex = i + 1;
			var target = ShowCount >= i || !Rolling
				? (RollResult[i] + 1) * ANGLE_STEP
				: SinceResult * 1000f;

			if ( ShowCount >= i || !Rolling )
				Model?.SetBodyGroup( 6 + i, (Wheels[i] - 1) * 2 );

			var fixedRotation = Transform.Rotation
				* Rotation.FromAxis( Vector3.Right, ANGLE_STEP / 2f + target )
				* Rotation.From( -90f, -90f, 0 );

			var transform = Model.SceneModel.GetBoneWorldTransform( boneIndex )
				.WithRotation( fixedRotation );

			Model.SceneModel.SetBoneWorldTransform( boneIndex, transform );
		}
	}
}
