namespace Sauna;

public enum DoorState
{
	Close,
	Open,
	Closing,
	Opening
}

public class DoorComponent : Component
{
	[Sync, Property] public DoorState State { get; set; } = DoorState.Close;
	[Property] public float OpenTime { get; set; } = 0.25f;
	[Property] public float Angle { get; set; } = 100f;
	[Property] public Vector3 Pivot { get; set; }

	[HostSync] private Transform InitialTransform { get; set; }

	protected override void OnAwake()
	{
		InitialTransform = Transform.World;
	}

	protected override void OnStart()
	{
		if ( !Network.Active )
			GameObject.NetworkSpawn();

		Network.SetOwnerTransfer( OwnerTransfer.Takeover );
		Network.SetOrphanedMode( NetworkOrphaned.Host );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "door.open",
			Accessibility = AccessibleFrom.All,
			DynamicText = () => State == DoorState.Close ? "Open" : "Close",
			Keybind = "use",
			Cooldown = true,
			CooldownTime = 1f,
			InteractDistance = 100f,
			Action = ( Player player, GameObject obj ) => 
			{
				// todo: door sound
				State = State == DoorState.Close ? DoorState.Opening : DoorState.Closing;
			},
			ShowWhenDisabled = () => true,
			Disabled = () => State == DoorState.Closing || State == DoorState.Opening,
			Animation = InteractAnimations.Interact
		} );
	}

	protected override void DrawGizmos()
	{
		if ( GameObject != Scene )
			return;

		Gizmo.Draw.IgnoreDepth = true;

		var pivot = Pivot.WithZ( 0f );
		var dir = Vector3.Right * Transform.Rotation.Inverse * 20;

		Gizmo.Draw.Color = Color.Blue;
		Gizmo.Draw.LineThickness = 5;
		Gizmo.Draw.Line( pivot, pivot + dir );
		Gizmo.Draw.Color = Color.Red;
		Gizmo.Draw.Line( pivot, pivot - dir );

		if ( !Gizmo.HasSelected )
			return;

		using ( Gizmo.Scope( $"Door", new Transform( pivot, Rotation.Identity ) ) )
		{
			Gizmo.Hitbox.DepthBias = 0.01f;

			if ( Gizmo.Control.Position( "position", Vector3.Zero, out var pos ) )
				Pivot = Pivot + pos;
		}
	}

	protected override void OnUpdate()
	{
		if ( State == DoorState.Open || State == DoorState.Close )
			return;

		// todo take into account direction of player
		var direction = State == DoorState.Opening ? 1 : -1;
		var defaultAngles = InitialTransform.Rotation.Angles();
		var targetYaw = direction == 1
			? Angle
			: 0;

		var targetRotation = defaultAngles
			.WithYaw( targetYaw )
			.ToRotation();
		var inversed = Transform.Rotation * InitialTransform.Rotation.Inverse;
		var difference = inversed.Distance( targetRotation );

		if ( difference.AlmostEqual( 0, 1f ) )
		{
			State = State == DoorState.Opening
				? DoorState.Open
				: DoorState.Close;

			Transform.World = InitialTransform;
			Transform.World = Transform.World.RotateAround( Transform.Position + Pivot * InitialTransform.Rotation, targetRotation );

			if ( State == DoorState.Close )
			{
				// todo: play close sound
			}

			return;
		}

		// todo: Prevent colliding with players.

		// Rotate around the hinge by a tiny amount every tick.
		var rot = Rotation.Lerp( inversed, targetRotation, 1f / OpenTime * Time.Delta );
		Transform.World = InitialTransform;
		Transform.World = Transform.World.RotateAround( Transform.Position + Pivot * InitialTransform.Rotation, rot );
	}
}
