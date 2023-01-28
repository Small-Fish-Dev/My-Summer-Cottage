namespace Sauna;

public partial class Player : AnimatedEntity
{
	public override void Spawn()
	{
		SetModel( "models/guy/guy.vmdl" );
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, CollisionBox.Mins, CollisionBox.Maxs );

		EnableDrawing = true;
		EnableHideInFirstPerson = true;
		EnableShadowInFirstPerson = true;
	}

	public override void Simulate( IClient cl )
	{
		// Simulate the player's movement.
		MoveSimulate( cl );
		InteractionSimulate( cl );
	}

	public override void FrameSimulate( IClient cl )
	{
		Camera.Position = Position + Vector3.Up * CollisionBox.Maxs.z;
		Camera.Rotation = ViewAngles.ToRotation();

		Camera.FieldOfView = Screen.CreateVerticalFieldOfView( 70f );
		Camera.FirstPersonViewer = this;
	}
}
