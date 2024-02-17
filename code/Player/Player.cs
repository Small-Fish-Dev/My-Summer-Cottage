namespace Sauna;

public partial class Player : Component
{
	protected CameraComponent Camera;
	protected SkinnedModelRenderer Model;
	protected BoxCollider Collider;

	protected override void OnStart()
	{
		Camera = Components.Get<CameraComponent>( FindMode.EverythingInSelfAndDescendants );
		Camera.Enabled = !IsProxy;

		Model = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		Collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndDescendants );
	}

	protected override void OnUpdate()
	{
		if ( !IsProxy )
		{
			UpdateAngles();
			Transform.Rotation = new Angles( 0, EyeAngles.yaw, 0 );
		}

		UpdateAnimation();
	}

	protected override void OnFixedUpdate()
	{
		if ( IsProxy )
			return;

		UpdateMovement();
	}

	protected override void OnPreRender()
	{
		if ( IsProxy )
			return;

		UpdateCamera();
	}
}
