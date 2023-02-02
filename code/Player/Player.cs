namespace Sauna;

public partial class Player : AnimatedEntity
{
	public Vector3 EyePosition { get; set; }

	public override void Spawn()
	{
		SetModel( "models/guy/guy.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );
		Tags.Add( "player" );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;

		Position = Entity.All.OfType<SpawnPoint>().FirstOrDefault().Position;
	}

	public Vector3 GetEyePosition()
	{
		var bone = GetBoneTransform( "head", true );
		return bone.Position;
	}

	public override void Simulate( IClient cl )
	{
		EyePosition = GetEyePosition();

		// Simulate the player's movement.
		MoveSimulate( cl );
		InteractionSimulate( cl );
	}

	public override void FrameSimulate( IClient cl )
	{
		EyePosition = GetEyePosition();
		SetAnimParameter( "move_x", WishVelocity.Length );
		Camera.Position = EyePosition;
		Camera.Rotation = ViewAngles.ToRotation();

		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 70f );
		Camera.FirstPersonViewer = this;
	}
}
