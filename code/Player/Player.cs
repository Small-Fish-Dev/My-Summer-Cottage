using Sandbox.Citizen;
using Sauna.UI;

namespace Sauna;

public partial class Player : Component, Component.ExecuteInEditor
{
	[Property, Sync, Category( "Parameters" )]
	public int Money { get; set; }

	[Property, Sync, Category( "Appearance" )]
	[Range( 0f, 1f, 0.05f )]
	public float Fatness { get; set; } = 0f;

	[Property, Sync, Category( "Appearance" )]
	[Range( -100f, 100f, 1f )]
	public float Height { get; set; } = 0f;

	/// <summary>
	/// Block both inputs and mouse aiming
	/// </summary>
	public bool BlockMovements { get; set; } = false;

	bool _blockMouseAim = false;

	/// <summary>
	/// Block mouse aiming
	/// </summary>
	public bool BlockMouseAim
	{
		get => BlockMovements || _blockMouseAim;
		set => _blockMouseAim = value;
	}

	bool _blockInputs = false;

	/// <summary>
	/// Block inputs (Like WASD, Pissing, Left/Right click)
	/// </summary>
	public bool BlockInputs
	{
		get => BlockMovements || _blockInputs;
		set => _blockInputs = value;
	}

	/// <summary>
	/// Whatever gameobject you're currently pissing on
	/// </summary>
	public GameObject PissingOn { get; set; }

	/// <summary>
	/// Where you're pissing on
	/// </summary>
	public Vector3 PissingPosition { get; set; }

	public bool IsPissing => !BlockInputs && Input.Down( "Piss" );

	public string FacedDirection => (int)((EyeAngles.Normal.yaw + 45f / 2 + 180) % 360 / 45f) switch
	{
		0 => "South",
		1 => "South East",
		2 => "East",
		3 => "North East",
		4 => "North",
		5 => "North West",
		6 => "West",
		7 => "South West",
		_ => "None"
	};

	public Inventory Inventory { get; private set; }
	protected CameraComponent Camera;
	protected SkinnedModelRenderer Model;
	protected BoxCollider Collider;
	protected ParticleConeEmitter PissEmitter;
	protected ParticleEffect PissParticles;
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

		PissEmitter = Components.Get<ParticleConeEmitter>( FindMode.EverythingInSelfAndDescendants );
		PissParticles = Components.Get<ParticleEffect>( FindMode.EverythingInSelfAndDescendants );

		// Footsteps
		Model.OnFootstepEvent += OnFootstep;
		Model.OnGenericEvent += OnJumpEvent;
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

		if ( PissEmitter != null )
		{
			PissEmitter.Enabled = IsPissing;

			if ( PissParticles != null )
			{
				foreach ( var particle in PissParticles.Particles )
				{
					if ( particle.HitTime > 0 )
					{
						PissingPosition = particle.HitPos;
						PissingOn = Scene.FindInPhysics( new Sphere( particle.HitPos, 5f ) ).First();
					}
				}
			}
		}
	}

	public bool TakeMoney( int amount )
	{
		if ( Money < amount )
			return false;

		Money -= amount;
		return true;
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
