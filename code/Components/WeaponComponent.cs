namespace Sauna;

public enum WeaponType
{
	Melee,
	Ranged
}

public sealed class WeaponComponent : Component
{
	[Property, Category( "Parameters" )] public WeaponType Type { get; set; }
	[Property, Category( "Parameters" )] public float FireSpeed { get; set; } = 0.5f;
	[Property, Category( "Parameters" )] public InputMode Mode { get; set; } = InputMode.Pressed;

	[Property, Category( "Projectile" ), Sync, TargetSave] public int Ammo { get; set; }
	[Property, Category( "Projectile" )] public int Capacity { get; set; }
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
			Action = (Player player, GameObject obj) => player.AimState = true,
			Disabled = () => Type != WeaponType.Ranged,
			InputMode = InputMode.Down,
			Animation = InteractAnimations.None
		} );

		interactions.AddInteraction( new Interaction()
		{
			Accessibility = AccessibleFrom.Hands,
			Description = "Reload",
			Keybind = "next",
			Action = Reload,
			Disabled = Disabled,
			ShowWhenDisabled = () => Type == WeaponType.Ranged,
			Animation = InteractAnimations.None
		} );
	}

	private bool Disabled()
	{
		if ( Capacity == 0 || Ammo > 0 )
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
					TryPlaySound( FireSound );

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
		// Find ammunition.
		var ammunition = player.Inventory?.BackpackItems?.FirstOrDefault( x => x?.Prefab == Ammunition?.ResourcePath );
		if ( ammunition == null )
			return;

		// Reload magazine.
		Ammo = Capacity;
		UpdateName();
		TryPlaySound( ReloadSound );

		// Destroy ammunition object.
		player.Inventory.ClearItem( ammunition );
		ammunition.GameObject?.Destroy();
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

		var tr = Scene.Trace.Ray( shooter.ViewRay, 5000f )
			.IgnoreGameObjectHierarchy( shooter.GameObject )
			.Run();

		var target = tr.GameObject?.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors );
		if ( target != null )
			target.SetRagdoll( true, duration: 2.5f, spin: 30 );

		return true;
	}

	private bool Swing( Player attacker )
	{
		var tr = Scene.Trace.Ray( attacker.ViewRay, 5000f )
			.IgnoreGameObjectHierarchy( attacker.GameObject )
			.Run();

		var target = tr.GameObject?.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors );
		if ( target != null )
			target.SetRagdoll( true, duration: 0.5f, spin: 30 );

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

		var transform = GameObject.Parent.Transform;
		BroadcastSound( @event.ResourceName, transform.Position, transform.Rotation );
	}

	[Broadcast] 
	public void BroadcastSound( string file, Vector3 pos, Rotation rot )
	{
		var sound = Sound.Play( file );
		sound.ListenLocal = false;
		sound.Position = pos;
		sound.Rotation = rot;
	}
}
