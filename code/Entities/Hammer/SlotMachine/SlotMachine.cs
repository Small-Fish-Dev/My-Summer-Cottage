namespace Sauna;

/// <summary>
/// Slot machine entity for awesome gambling entertainment!
/// </summary>
[HammerEntity]
[EditorModel( "models/slot_machine/slot_machine.vmdl" )]
public partial class SlotMachine : ModelEntity, IInteractable
{
	public const float ANGLE_STEP = 22.5f;
	public const int COUNT = 8;
	public const float ROLL_TIME = 5f;
	public const float WAIT_TIME = 1f;

	string IInteractable.DisplayTitle => "🎰 Pelikone";
	InteractionOffset IInteractable.Offset => CollisionWorldSpaceCenter - Position;
	Color IInteractable.DisplayColor => Color.Green.Darken( 0.25f );

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

	/// <summary>
	/// Are we currently rolling?
	/// </summary>
	[Net, Predicted]
	public bool Rolling { get; set; } = false;

	/// <summary>
	/// Current money inside of the slot machine.
	/// </summary>
	[Net, Predicted]
	public int Money { get; set; } = 10;

	/// <summary>
	/// The amount we are betting.
	/// </summary>
	[Net, Predicted]
	public int Bet { get; set; } = 5;

	/// <summary>
	/// The result of our lastest roll.
	/// </summary>
	[Net]
	public Result RollResult { get; set; }

	TimeSince sinceResult;
	int showCount;

	SlotsDisplay money;
	SlotsDisplay bet;

	public SlotMachine()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( "use", new()
		{
			Predicate = ( Player pawn ) => !Rolling,
			Function = ( Player pawn ) =>
			{
				if ( Money < Bet )
					return;

				Rolling = true;
				Money -= Bet;

				if ( Game.IsServer )
					RollResult = new Result
					{
						a = new Random().Next( 0, 7 ),
						b = new Random().Next( 0, 7 ),
						c = new Random().Next( 0, 7 )
					};

				sinceResult = 0;
				showCount = 0;
			},
			Text = "Roll"
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/slot_machine/slot_machine.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

		Tags.Add( "solid" );
	}

	public override void ClientSpawn()
	{
		money = new( this, "display1", "Money" );
		bet = new( this, "display2", "Bet" );
	}

	[GameEvent.Tick]
	private void Tick()
	{
		DebugOverlay.Text( $"Rolling: {Rolling}\nMoney: {Money}\nRoll: {RollResult}", Position );

		// Calculate win.
		if ( Game.IsServer )
		{
			if ( !Rolling || sinceResult < ROLL_TIME + WAIT_TIME )
				return;

			Rolling = false;
			Money += 10;

			return;
		}

		// Check if we should be updating at all.
		if ( !Rolling || sinceResult > ROLL_TIME )
			return;

		// Which slot results can we show already?
		showCount = MathX.FloorToInt( sinceResult / (ROLL_TIME / 3) ).Clamp( 0, 3 );

		for ( int i = 1; i <= 3; i++ )
		{
			var boneIndex = i + 2;
			var target = showCount >= i 
				? RollResult[i] * ANGLE_STEP
				: sinceResult * 1000f;

			var fixedRotation = Rotation
				* Rotation.FromAxis( Vector3.Right, ANGLE_STEP / 2f + target ) 
				* Rotation.From( -90f, -90f, 0 );

			var transform = GetBoneTransform( boneIndex )
				.WithRotation( fixedRotation );
			
			SetBoneTransform( boneIndex, transform, true );
		}
	}
}
