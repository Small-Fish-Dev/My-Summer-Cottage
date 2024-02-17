namespace Sauna;

public partial class Player : Component
{
	protected CameraComponent Camera;
	protected SkinnedModelRenderer Model;
	protected BoxCollider Collider;

	protected override void OnStart()
	{
		// Components
		Camera = Components.Get<CameraComponent>( FindMode.EverythingInSelfAndDescendants );
		Camera.Enabled = !IsProxy;

		Model = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		Collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndDescendants );

		// Footsteps
		Model.OnFootstepEvent += OnFootstep;
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

	private TimeSince lastStepped;
	private void OnFootstep( SceneModel.FootstepEvent e )
	{
		if ( lastStepped < 0.2f )
			return;

		var pos = Transform.Position;
		var tr = Scene.Trace.Ray( pos + Vector3.Up * 10, pos + Vector3.Down * 10 )
			.Radius( 1 )
			.WithoutTags( "trigger" )
			.IgnoreGameObjectHierarchy( GameObject )
			.Run();

		if ( !tr.Hit || tr.Surface == null )
			return;

		lastStepped = 0;

		var path = e.FootId == 0 
			? tr.Surface.Sounds.FootLeft 
			: tr.Surface.Sounds.FootRight;

		if ( string.IsNullOrEmpty( path ) )
			return;

		var sound = Sound.Play( path, tr.HitPosition + tr.Normal * 5 );
		sound.Volume *= e.Volume;
		sound.Update();
	}
}
