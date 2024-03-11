using Sauna.SFX;
using static Sandbox.PhysicsContact;

namespace Sauna;

public enum WeaponType
{
	Melee,
	Ranged
}

public sealed class WeaponComponent : Component
{
	[Property, Category( "Parameters" )] public WeaponType Type { get; set; }
	[Property, Category( "Parameters" )] public float Range { get; set; } = 40f;
	[Property, Category( "Parameters" )] public float FireSpeed { get; set; } = 0.5f;
	[Property, Category( "Parameters" )] public InputMode Mode { get; set; } = InputMode.Pressed;
	[Property, Category( "Parameters" )] public DamageType DamageType { get; set; } = DamageType.Average;
	[Property, Category( "Parameters" )] public int Damage { get; set; } = 5;
	[Property, Category( "Parameters" )] public float HitForce { get; set; } = 300;

	[Property, Category( "Projectile" ), Sync, TargetSave] public int Ammo { get; set; }
	[Property, Category( "Projectile" )] public string ExitAttachment { get; set; }
	[Property, Category( "Projectile" )] public int Capacity { get; set; }
	[Property, Category( "Projectile" )] public float ReloadCooldown { get; set; }
	[Property, Category( "Projectile" )] public PrefabFile Ammunition { get; set; }

	[Property, Category( "Recoil" )] public bool HasRecoil { get; set; } = false;
	[Property, Category( "Recoil" ), ShowIf( "HasRecoil", true )] public RangedFloat StrengthRange { get; set; }

	[Property, Category( "Sounds" )] public SoundEvent FireSound { get; set; }
	[Property, Category( "Sounds" )] public SoundEvent EmptySound { get; set; }
	[Property, Category( "Sounds" )] public SoundEvent PostFireSound { get; set; }
	[Property, Category( "Sounds" )] public SoundEvent ReloadSound { get; set; }

	public Player Owner => Player.All.FirstOrDefault( x => x.ConnectionID == Network.OwnerId );

	private string _name;
	private TimeUntil _canFire;
	private TimeSince _lastReloaded;

	protected override void OnAwake()
	{
		UpdateName();
	}

	protected override void OnStart()
	{
		// Interactions
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Accessibility = AccessibleFrom.Hands,
			Description = Type == WeaponType.Ranged ? "Fire" : "Attack",
			Keybind = "mouse1",
			Cooldown = true,
			CooldownTime = FireSpeed,
			Action = Attack,
			ShowWhenDisabled = () => true,
			Disabled = () => Capacity > 0 && Ammo == 0,
			InputMode = Mode,
			Animation = Type == WeaponType.Ranged ? InteractAnimations.Shoot : InteractAnimations.Action
		} );

		interactions.AddInteraction( new Interaction()
		{
			Accessibility = AccessibleFrom.Hands,
			Description = "Aim",
			Keybind = "mouse2",
			Action = ( Player player, GameObject obj ) => player.AimState = true,
			Disabled = () => Type != WeaponType.Ranged,
			InputMode = InputMode.Down,
			Animation = InteractAnimations.None
		} );

		interactions.AddInteraction( new Interaction()
		{
			Accessibility = AccessibleFrom.Hands,
			Description = "Reload",
			Keybind = "next",
			Cooldown = true,
			CooldownTime = ReloadCooldown,
			Action = Reload,
			Disabled = Disabled,
			ShowWhenDisabled = () => Type == WeaponType.Ranged,
			InputMode = InputMode.Down,
			Animation = InteractAnimations.Reload
		} );
	}

	private bool Disabled()
	{
		if ( _lastReloaded < ReloadCooldown )
			return true;

		if ( Capacity == 0 || Ammo == Capacity )
			return true;

		var ammunition = Player.Local?.Inventory?.BackpackItems?.FirstOrDefault( x => x?.Prefab == Ammunition?.ResourcePath );
		if ( Ammunition != null && ammunition == null )
			return true;

		return false;
	}

	public void Attack( Player attacker, GameObject obj )
	{
		switch ( Type )
		{
			case WeaponType.Ranged:
				var shot = Fire( attacker );
				if ( shot )
				{
					Ammo--;
					_canFire = FireSpeed;

					// todo @ceitine: post fire sound (i.e. bolt)

					if ( HasRecoil )
						attacker.ApplyRecoil( new Angles( -1f, Game.Random.Float( -0.3f, 0.3f ), 0 ) * Game.Random.Float( StrengthRange.x, StrengthRange.y ) );
				}

				UpdateName();
				break;

			default:
				var swing = Swing( attacker );
				// todo @ceitine: add swing sound
				break;
		}
	}

	public void Reload( Player player, GameObject obj )
	{
		if ( _lastReloaded < ReloadCooldown )
			return;

		// Find ammunition.
		var ammunition = player.Inventory?.BackpackItems?.FirstOrDefault( x => x?.Prefab == Ammunition?.ResourcePath );
		if ( ammunition == null )
			return;

		// Reload magazine.
		Ammo++;
		UpdateName();
		TryPlaySound( ReloadSound );
		_lastReloaded = 0f;

		// Destroy ammunition object.
		player.Inventory.RemoveAmount( ammunition, 1 );
	}

	private bool Fire( Player shooter )
	{
		if ( !_canFire )
			return false;

		if ( Ammo <= 0 )
		{
			TryPlaySound( EmptySound );
			return false;
		}

		// Get ray
		if ( !shooter.AimState ) // weird ass pose if you shoot while not aiming
			shooter.ForceHoldTypeBroadcast( HoldType.Flashlight, 0.2f );

		var transform = string.IsNullOrWhiteSpace( ExitAttachment ) 
			? new Transform( shooter.ViewRay.Position, Rotation.LookAt( shooter.ViewRay.Forward ) )
			: shooter.GetAttachment( ExitAttachment );

		if ( !string.IsNullOrWhiteSpace( ExitAttachment ) && transform == global::Transform.Zero )
			Log.Warning( $"Exit attachment \"{ExitAttachment}\" not found." );

		var ray = new Ray( transform.Position, transform.Rotation.Forward );
		var tr = Scene.Trace.Ray( ray, Range )
			.IgnoreGameObjectHierarchy( shooter.GameObject )
			.Run();

		// Bullet trace
		var particle = LegacyParticles.Create( "particles/bullet_tracer.vpcf", transform, deleteTime: 1500 );
		particle.SetVector( 0, transform.Position );
		particle.SetVector( 1, tr.EndPosition );

		var target = tr.GameObject;
		if ( target != null )
		{
			if ( target.Components.TryGet<Rigidbody>( out var body ) )
				body.ApplyImpulseAt( tr.HitPosition, tr.Direction * HitForce );

			if ( target.Components.TryGet<HealthComponent>( out var health ) )
				health.Damage( Damage, DamageType, shooter.GameObject, tr.HitPosition, tr.Direction, HitForce );
		}

		TryPlaySound( FireSound );

		return true;
	}

	private bool Swing( Player attacker )
	{
		var tr = Scene.Trace.Ray( attacker.ViewRay, Range )
			.IgnoreGameObjectHierarchy( attacker.GameObject )
			.Run();

		var target = tr.GameObject;

		if ( target != null )
		{
			if ( target.Components.TryGet<Rigidbody>( out var body ) )
				body.ApplyImpulseAt( tr.HitPosition, tr.Direction * HitForce );

			if ( target.Components.TryGet<HealthComponent>( out var health ) )
				health.Damage( Damage, DamageType, attacker.GameObject, tr.HitPosition, tr.Direction, HitForce );
		}

		return true;
	}

	private void UpdateName()
	{
		var item = Components.Get<ItemComponent>( true );
		if ( item == null )
			return;

		if ( string.IsNullOrEmpty( _name ) ) _name = item.Name;

		switch ( Type )
		{
			case WeaponType.Ranged:
				item.Name = $"{_name} ({Ammo}/{Capacity})";
				break;

			default:
				break;
		}
	}

	private void TryPlaySound( SoundEvent @event )
	{
		if ( @event == null )
			return;

		BroadcastSound( @event.ResourceName );
	}

	[Broadcast]
	public void BroadcastSound( string file )
	{
		GameObject.PlaySound( file );
	}
}
