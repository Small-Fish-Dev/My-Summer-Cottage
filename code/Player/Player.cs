using Sandbox.Diagnostics;
using Sauna.Components;
using Sauna.UI;
using static EventComponent;

namespace Sauna;

public partial class Player : Component, Component.ExecuteInEditor
{
	[Sync] public string Firstname { get; set; }
	[Sync] public string Lastname { get; set; }
	[Sync] public bool HidePenoid { get; set; }
	[Sync] public bool ForceHidePenoid { get; set; } = false;
	[Sync] public float PenoidSize { get; set; }

	/// <summary>
	/// Actually means sitting, but I thought it'd be funny if it said shitting instead.
	/// </summary>
	[Sync]
	public Transform? Shitting
	{
		get => _shitting;
		set
		{
			if ( value == null && this == Local && Seat.Target.IsValid() && Seat.Target.Network.IsOwner )
				Seat.Target.Network.DropOwnership();

			_shitting = value;
		}
	}
	private Transform? _shitting;

	[Property, Sync, Category( "Parameters" )]
	public int Money { get; set; } = 37;

	[Property, Sync, Category( "Appearance" )]
	[Range( 0f, 1f, 0.05f )]
	public float Fatness { get; set; } = 0f;

	[Property, Sync, Category( "Appearance" )]
	[Range( -100f, 100f, 1f )]
	public float Height { get; set; } = 0f;

	[Property, Sync, Category( "Appearance" )]
	public Color SkinColor
	{
		get => _skinColor;
		set
		{
			_skinColor = value;
			_updateSkin = true;
		}
	}

	[Sync, MakeDirty]
	public bool HasShirt
	{
		get
		{
			if ( !Inventory.IsValid() || Inventory == null ) return false;

			foreach ( var item in Inventory.EquippedItems )
			{
				if ( item is ItemEquipment equipped )
				{
					if ( equipped.Slot == EquipSlot.Body )
						return true;
				}
			}

			return false;
		}
	}

	public SkinnedModelRenderer Penoid { get; private set; }

	Color _skinColor;
	HiddenBodyGroup _hideBodygroups;
	bool _updateSkin = true;

	[Sync]
	public HiddenBodyGroup HideBodygroups
	{
		get => _hideBodygroups;
		set
		{
			_hideBodygroups = value;

			if ( Renderer == null )
				return;

			Renderer.SetBodyGroup( "head", _hideBodygroups.HasFlag( HiddenBodyGroup.Head ) ? 1 : 0 );
			Renderer.SetBodyGroup( "torso", _hideBodygroups.HasFlag( HiddenBodyGroup.Torso ) ? 1 : 0 );
			Renderer.SetBodyGroup( "hands", _hideBodygroups.HasFlag( HiddenBodyGroup.Hands ) ? 1 : 0 );
			Renderer.SetBodyGroup( "legs", _hideBodygroups.HasFlag( HiddenBodyGroup.Legs ) ? 1 : 0 );
			Renderer.SetBodyGroup( "feet", _hideBodygroups.HasFlag( HiddenBodyGroup.Feet ) ? 1 : 0 );
		}
	}

	/// <summary>
	/// Block both inputs and mouse aiming
	/// </summary>
	[Sync]
	public bool BlockMovements { get; set; } = false;

	bool _blockMouseAim = false;

	/// <summary>
	/// Block mouse aiming
	/// </summary>
	[Sync]
	public bool BlockMouseAim
	{
		get => BlockMovements || _blockMouseAim;
		set => _blockMouseAim = value;
	}

	bool _blockInputs = false;

	/// <summary>
	/// Block inputs (Like WASD, Pissing, Left/Right click)
	/// </summary>
	[Sync]
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

	public TimeSince LastPiss { get; set; } = 0f;

	[Sync] public bool IsPissing { get; set; }

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

	public bool IsEncumbered => Inventory.Weight > Inventory.MAX_WEIGHT_IN_GRAMS;

	public Inventory Inventory { get; private set; }
	protected CameraComponent Camera;
	public SkinnedModelRenderer Renderer;
	protected BoxCollider Collider;
	protected ParticleConeEmitter PissEmitter;
	protected ParticleEffect PissParticles;

	public Transform GetAttachment( string attachment, bool world = true )
		=> Renderer.GetAttachment( attachment, world ) ?? global::Transform.Zero;

	/// <summary>
	/// Size of penoid shaped object in centimeters.
	/// </summary>
	public float Endowment => PenoidSize
							  * GetAttachment( "penoid_start", false ).Position
								  .Distance( GetAttachment( "penoid_max", false ).Position )
							  * 2.54f; // Inches to centimeters.

	/// <summary>
	/// Get the transform of the penoid.
	/// </summary>
	/// <returns></returns>
	public Transform GetPenoidTransform()
	{
		if ( Penoid == null || !Penoid.IsValid ) // Something went wrong, penoid is invalid.
			return default;

		// Size of penice.
		var morph = Penoid?.SceneModel?.Morphs.Get( "size" ) ?? 0f;

		// Size of smallest possible penice.
		var smallAttachment = GetAttachment( "penoid_min", false );

		// Size of biggest possible penice.
		var bigAttachment = GetAttachment( "penoid_max", false );

		// Lerp between the two values to get actual position of penice tip.
		return global::Transform.Lerp( smallAttachment, bigAttachment, morph, true );
	}

	protected override void DrawGizmos()
	{
	}

	protected override void OnStart()
	{
		if ( !Game.IsPlaying || Scene == GameObject )
			return;

		Network.SetOrphanedMode( NetworkOrphaned.Destroy );

		// Components
		Camera = Components.Get<CameraComponent>( FindMode.EverythingInSelfAndDescendants );
		Camera.GameObject.Enabled = !IsProxy;

		Inventory = Components.Get<Inventory>( FindMode.EverythingInSelfAndDescendants );

		Renderer = Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelfAndDescendants );
		Penoid = GameObject.Children.FirstOrDefault( x => x.Name == "Penoid" )?.Components?.Get<SkinnedModelRenderer>();
		Collider = Components.Get<BoxCollider>( FindMode.EverythingInSelfAndDescendants );

		PissEmitter = Components.Get<ParticleConeEmitter>( FindMode.EverythingInSelfAndDescendants );
		PissParticles = Components.Get<ParticleEffect>( FindMode.EverythingInSelfAndDescendants );

		if ( !IsProxy ) // Load save.
		{
			HideHead = true;
			Setup( this );
		}

		// Footsteps
		Renderer.OnFootstepEvent += OnFootstep;
		Renderer.OnGenericEvent += OnJumpEvent;
	}

	protected override void OnUpdate()
	{
		if ( !Game.IsPlaying )
			return;

		if ( !IsProxy )
		{
			UpdateAngles();
			Transform.Rotation = new Angles( 0, EyeAngles.yaw, 0 );
			HidePenoid = !ForceHidePenoid && Inventory.EquippedItems[(int)EquipSlot.Legs] != null;
			HoldType = (Inventory.EquippedItems[(int)EquipSlot.Hand] as ItemEquipment)?.HoldType ?? HoldType.Idle;
		}

		if ( _updateSkin && Renderer != null && Penoid != null )
		{
			Renderer.Tint = SkinColor;
			Penoid.Tint = SkinColor;

			_updateSkin = false;
		}

		UpdateAnimation();

		if ( Penoid is not null )
		{
			Penoid.Enabled = !ForceHidePenoid && !HidePenoid;
		}

		// todo: fix ://
		// Renderer.Set( "penoid", PenoidSize );
	}

	protected override void OnFixedUpdate()
	{
		if ( !Game.IsPlaying )
			return;

		if ( PissEmitter != null )
		{
			PissEmitter.Enabled = IsPissing;
			var pissRot = EyeAngles.WithRoll( 0 ).WithYaw( 0 );
			var pissHeightAdjust = (Height * 0.01f) * 9;

			PissEmitter.GameObject.Transform.Local = new Transform( GetAttachment( "boner", false ).Position, pissRot.WithPitch( (pissRot.pitch - (40 + pissHeightAdjust)).Clamp( -80, 50 ) ) );

			if ( PissParticles != null )
			{
				foreach ( var particle in PissParticles.Particles )
				{
					if ( particle.HitTime > 0 )
					{
						PissingPosition = particle.HitPos;
						PissingOn = Scene.FindInPhysics( new Sphere( particle.HitPos, 5f ) ).FirstOrDefault();
						LastPiss = 0f;
					}
				}
			}
		}

		if ( IsRagdolled )
			FollowRagdoll();

		if ( IsProxy )
			return;

		IsPissing = !BlockInputs && Input.Down( InputAction.Piss ) && !HidePenoid;

		if ( Shitting == null )
			UpdateMovement();
		else if ( Shitting.HasValue && (Input.AnalogMove.Length > 0.1f || Input.Pressed( InputAction.Jump )) )
			Shitting = null;

		if ( Input.Pressed( InputAction.Ragdoll ) && CanRagdoll )
		{
			Shitting = null;
			SetRagdoll( !IsRagdolled );
		}

		UpdateInteractions();
	}

	public void Respawn()
	{
		var foundSpawners = Scene.GetAllComponents<PlayerSpawner>();

		if ( foundSpawners.Any() )
		{
			var randomSpawner = Game.Random.FromList( foundSpawners.ToList() );
			Transform.Position = randomSpawner.Transform.Position;
			Transform.Rotation = randomSpawner.Transform.Rotation;
		}
		else
			Transform.Position = Transform.Position;
	}

	[ConCmd( "Respawn" )]
	static void RespawnCommand()
	{
		Player.Local.Respawn();
	}

	public bool TakeMoney( int amount )
	{
		if ( Money < amount )
			return false;

		Money -= amount;
		return true;
	}

	public bool GiveMoney( int amount )
	{
		Money += amount;
		return true;
	}

	[Icon( "camera" )]
	public SweetMemory CaptureMemory( string caption, string identifier = null, float distance = 100f,
		int maxTries = 20 )
	{
		var center = Bounds.Center + Transform.Position + Vector3.Up * 10f;
		var position = center + Transform.Rotation.Backward * distance; // Default
		var rotation = Rotation.FromRoll( Game.Random.Int( -15, 15 ) );

		for ( int i = 0; i < maxTries; i++ )
		{
			var dir = Vector3.Random;
			dir = dir.WithZ( dir.z.Clamp( 0.3f, 0.9f ) ); // Clamped on z-axis so it doesn't go too low.

			var from = Transform.Position + dir * distance;
			var ray = new Ray( from, (center - from).Normal );

			var tr = Scene.Trace.Ray( ray, distance )
				.WithoutTags( "trigger" )
				.Run();

			if ( tr.GameObject == GameObject )
			{
				position = ray.Position;
				break;
			}
		}

		return SweetMemories.Capture( caption, position, rotation, center, identifier );
	}

	protected override void OnPreRender()
	{
		if ( !Game.IsPlaying )
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
	}

	/// <summary>
	/// Fades to black
	/// </summary>
	/// <param name="startingTransition"></param>
	/// <param name="blackTransition"></param>
	/// <param name="endingTransition"></param>
	public void BlackScreen( float startingTransition = 2f, float blackTransition = 2f, float endingTransition = 1f )
	{
		if ( IsProxy ) return;

		var gameObject = Hud.Instance.GameObject;

		if ( gameObject == null ) return;

		var blackScreen = gameObject.Components.Create<BlackScreen>();
		blackScreen.StartingTransition = startingTransition;
		blackScreen.BlackTransition = blackTransition;
		blackScreen.EndingTransition = endingTransition;
		blackScreen.Start();
	}

	[Broadcast( NetPermission.OwnerOnly )]
	public void PlayNoklaSound()
	{
		GameObject.PlaySound( "sounds/phone/nokla_notif.sound" );
	}
}
