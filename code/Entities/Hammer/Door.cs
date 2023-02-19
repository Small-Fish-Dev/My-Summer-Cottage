namespace Sauna;

public enum DoorState
{
	Close,
	Open,
	Closing,
	Opening
}

[HammerEntity]
[Solid]
public partial class Door : ModelEntity, IInteractable
{
	/// <summary>
	/// The state this door is currently in.
	/// </summary>
	[Net] public DoorState State { get; set; }

	private Vector3? hinge;
	private Rotation? rotation;

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

	/// <summary>
	/// The default untouched rotation of this door.
	/// </summary>
	public Rotation DefaultRotation
	{
		get
		{
			if ( rotation == null )
				rotation = Rotation;

			return rotation.Value;
		}
	}

	/// <summary>
	/// The original position of this door.
	/// </summary>
	public Vector3 DefaultPosition { get; private set; }

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
		DefaultPosition = Position;
	}

	[Event.Tick.Server]
	private void tick()
	{
		// Don't tick if we're static.
		if ( State == DoorState.Open || State == DoorState.Close )
			return;

		var time = 0.75f; // In seconds.
		var amount = 100f;
		var defaultYaw = DefaultRotation.Yaw();
		var direction = State == DoorState.Opening
			? 1
			: -1;
		var targetYaw = direction == 1
			? amount
			: 0;

		// Check if we're already at the desired yaw.
		var targetRotation = DefaultRotation.Angles()
			.WithYaw( (defaultYaw + targetYaw) )
			.ToRotation();
		var difference = Rotation.Distance( targetRotation );

		if ( difference.AlmostEqual( 0, 5f ) )
		{
			State = State == DoorState.Opening
				? DoorState.Open
				: DoorState.Close;

			Rotation = Transform.Zero.Rotation;
			Position = DefaultPosition;

			Transform = Transform.RotateAround( Hinge, targetRotation );

			return;
		}

		// Prevent colliding with players.
		// TODO: Fix (very shit way of doing this)
		var trace = Trace.Box( Model.Bounds, Position, Position + Rotation.Forward * 5f * direction )
			.WithAnyTags( "player" )
			.Run();
		if ( trace.Hit )
			return;

		// Rotate around the hinge by a tiny amount every tick.
		var length = DefaultRotation.Angles()
			.WithYaw( (defaultYaw + direction * amount) * Time.Delta / time )
			.ToRotation();
		Transform = Transform.RotateAround( Hinge, length );
	}
}
