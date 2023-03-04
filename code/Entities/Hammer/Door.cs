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
	public Transform DefaultTransform { get; private set; }

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

				Sound.FromEntity( "sounds/door/door_creak.sound", this );
			},
			TextFunction = () => State == DoorState.Open || State == DoorState.Opening ? "Close" : "Open"
		} );
	}

	public override void Spawn()
	{
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		DefaultTransform = Transform;
		Tags.Add( "solid" );
	}

	[Event.Tick.Server]
	private void tick()
	{
		// Don't tick if we're static.
		if ( State == DoorState.Open || State == DoorState.Close )
			return;

		var time = 0.25f; // In seconds.
		var amount = 100f;
		var defaultAngles = DefaultTransform.Rotation.Angles();
		var direction = State == DoorState.Opening
			? 1
			: -1;
		var targetYaw = direction == 1
			? amount
			: 0;

		// Check if we're already at the desired yaw.
		var targetRotation = defaultAngles
			.WithYaw( targetYaw )
			.ToRotation();
		var inversed = Rotation * DefaultTransform.Rotation.Inverse;
		var difference = inversed.Distance( targetRotation );

		if ( difference.AlmostEqual( 0, 1f ) )
		{
			State = State == DoorState.Opening
				? DoorState.Open
				: DoorState.Close;

			Transform = DefaultTransform;
			Transform = Transform.RotateAround( Hinge, targetRotation );

			if ( State == DoorState.Close )
			{
				Sound.FromEntity( "sounds/wood/wooden_impact.sound", this );
			}

			return;
		}

		// Prevent colliding with players.
		var trace = Trace.Body( PhysicsBody, Position + Rotation.Right * 4f * direction )
			.WithAnyTags( "player" )
			.Run();

		if ( trace.Hit )
			return;

		// Rotate around the hinge by a tiny amount every tick.
		var rot = Rotation.Lerp( inversed, targetRotation, 1f / time * Time.Delta );
		Transform = DefaultTransform;
		Transform = Transform.RotateAround( Hinge, rot );
	}
}
