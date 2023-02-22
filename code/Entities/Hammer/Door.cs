namespace Sauna;

public enum DoorState
{
	Close,
	Open,
	Closing,
	Opening
}

[HammerEntity]
[Model]
public partial class Door : ModelEntity, IInteractable
{
	/// <summary>
	/// The state this door is currently in.
	/// </summary>
	[Net] public DoorState State { get; set; }
	public Transform DefaultTransfrom { get; private set; }

	private Vector3? hinge;

	/// <summary>
	/// The position of this door's hinge in worldspace.
	/// </summary>
	public Vector3 Hinge
	{
		get
		{
			if ( hinge == null )
				hinge = Position.WithZ( WorldSpaceBounds.Center.z ) + CollisionBounds.Maxs.WithZ( 0 );

			return hinge.Value;
		}
	}

	string IInteractable.DisplayTitle => "Ovi";

	public Door()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				if ( !Game.IsServer )
					return;

				State = State == DoorState.Open || State == DoorState.Opening
					? DoorState.Closing
					: DoorState.Opening;
			},
			TextFunction = () => State == DoorState.Open || State == DoorState.Opening ? "Close" : "Open"
		} );
	}

	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		DefaultTransfrom = Transform;
	}

	[Event.Tick.Server]
	private void tick()
	{
		// Don't tick if we're static.
		if ( State == DoorState.Open || State == DoorState.Close )
			return;

		var time = 0.25f; // In seconds.
		var amount = 100f;
		var defaultAngles = DefaultTransfrom.Rotation.Angles();
		var direction = State == DoorState.Opening
			? 1
			: -1;
		var targetYaw = direction == 1
			? amount
			: 0;

		// Check if we're already at the desired yaw.
		var targetRotation = defaultAngles
			.WithYaw( (defaultAngles.yaw + targetYaw) )
			.ToRotation();
		var difference = Rotation.Distance( targetRotation );

		if ( difference.AlmostEqual( 0, 1f ) )
		{
			State = State == DoorState.Opening
				? DoorState.Open
				: DoorState.Close;

			Transform = DefaultTransfrom;
			Transform = Transform.RotateAround( Hinge, targetRotation );

			return;
		}

		// Prevent colliding with players.
		var trace = Trace.Box( Model.Bounds, Position, Position + Rotation.Forward * 16f * direction )
			.WithAnyTags( "player" )
			.Run();

		if ( trace.Hit )
			return;

		// Rotate around the hinge by a tiny amount every tick.
		var rot = Rotation.Lerp( Rotation, targetRotation, 1f / time * Time.Delta );
		Transform = DefaultTransfrom;
		Transform = Transform.RotateAround( Hinge, rot );
	}
}
