namespace Sauna;

/// <summary>
/// Workaround entity for spawning prefabs in the map.
/// </summary>
[HammerEntity]
[EditorModel( "models/slot_machine/slot_machine.vmdl" )]
public partial class SlotMachine : ModelEntity, IInteractable
{
	public struct Result
	{
		public int a;
		public int b;
		public int c;
	}

	public readonly static Result None = new Result { a = -1 };

	string IInteractable.DisplayTitle => "Pelikone";
	InteractionOffset IInteractable.Offset => CollisionWorldSpaceCenter - Position;

	[Net, Predicted]
	public bool Rolling { get; set; } = false;

	[Net, Predicted]
	public int Money { get; set; } = 10;

	[Net, Predicted]
	public int Bet { get; set; } = 5;

	[Net]
	public Result RollResult { get; set; }

	TimeSince sinceResult;
	Transform[] initialTransforms = new Transform[3];

	public override void Spawn()
	{
		SetModel( "models/slot_machine/slot_machine.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Dynamic );

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
						a = Game.Random.Int( 0, 7 ),
						b = Game.Random.Int( 0, 7 ),
						c = Game.Random.Int( 0, 7 )
					};

				sinceResult = 0;
			},
			Text = "Roll"
		} );
	}

	public override void OnNewModel( Model model )
	{
		base.OnNewModel( model );

		for ( int i = 1; i <= 3; i++ )
			initialTransforms[i - 1] = GetBoneTransform( i, false );
	}

	[GameEvent.Tick]
	private void Tick()
	{
		DebugOverlay.Text( $"Rolling: {Rolling}\nMoney: {Money}", Position );

		// Calculate win.
		if ( Game.IsServer )
		{
			if ( !Rolling || sinceResult < 5f )
				return;

			Rolling = false;
			Money += 10;

			return;
		}

		if ( !Rolling || initialTransforms == null )
			return;

		for ( int i = 1; i <= 3; i++ )
		{
			var initial = initialTransforms[i - 1];
			var transform = initial.RotateAround( 0, Rotation.From( 0, Time.Now, 0 ) );
			SetBoneTransform( i, transform, true );
		}
	}
}
