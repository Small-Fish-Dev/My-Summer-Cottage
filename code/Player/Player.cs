namespace Sauna;

public enum WindDirections
{
	South,
	South_East,
	East,
	North_East,
	North,
	North_West,
	West,
	South_West
}

public partial class Player : Component, Component.ExecuteInEditor
{
	[Property, Sync, Category( "Parameters" )]
	public int Money { get; set; }

	[Property, Sync, Category( "Appearance" )]
	[Range( 0f, 1f, 0.05f )]
	public float Fatness { get; set; } = 0f;

	public WindDirections FacedDirection => (WindDirections)((EyeAngles.Normal.yaw + 45f / 2 + 180) % 360 / 45f);
	public string StringDirection => FacedDirection.ToString().Replace( '_', ' ' );

	protected CameraComponent Camera;
	protected Inventory Inventory;
	protected SkinnedModelRenderer Model;
	protected BoxCollider Collider;

	protected override void DrawGizmos()
	{
	}

	protected override void OnStart()
	{
		if ( !GameManager.IsPlaying )
			return;

		// Components
		Camera = Components.Get<CameraComponent>( FindMode.EverythingInSelfAndDescendants );
		Camera.Enabled = !IsProxy;

		Inventory = Components.Get<Inventory>( FindMode.EverythingInSelfAndDescendants );

		Model = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		Collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndDescendants );

		// Footsteps
		Model.OnFootstepEvent += OnFootstep;
	}

	protected override void OnUpdate()
	{
		if ( !GameManager.IsPlaying )
			return;

		if ( !IsProxy )
		{
			UpdateAngles();
			Transform.Rotation = new Angles( 0, EyeAngles.yaw, 0 );
		}

		UpdateAnimation();
	}

	protected override void OnFixedUpdate()
	{
		if ( !GameManager.IsPlaying )
			return;

		if ( IsProxy )
			return;

		UpdateMovement();
		UpdateInteractions();
	}

	protected override void OnPreRender()
	{
		if ( !GameManager.IsPlaying )
			return;

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
