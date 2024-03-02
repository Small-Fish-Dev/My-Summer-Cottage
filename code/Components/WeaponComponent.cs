namespace Sauna;

public enum WeaponType
{
	Melee,
	Ranged
}

public sealed class WeaponComponent : Component
{
	[Property, Category( "Parameters" )] public WeaponType Type { get; set; }

	[Property, Category( "Projectile" ), Sync] public int Ammo { get; set; }
	[Property, Category( "Projectile" )] public int Capacity { get; set; }
	[Property, Category( "Projectile" )] public PrefabFile Ammunition { get; set; }

	protected override void OnStart()
	{
		// Interactions
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			HoldOnly = true,
			Description = "Fire",
			Keybind = "mouse1",
			Action = Attack,
		} );

		interactions.AddInteraction( new Interaction()
		{
			HoldOnly = true,
			Description = "Reload",
			Keybind = "next",
			Action = Reload,
			Disabled = () => Type != WeaponType.Ranged || Ammo >= Capacity, // todo @ceitine: doesn't  
			ShowWhenDisabled = true
		} );
	}
	
	public void Attack( Player shooter, GameObject obj )
	{
		switch ( Type )
		{
			case WeaponType.Ranged:
				Fire( shooter );
				break;

			default:
				throw new NotImplementedException();
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

		// todo @ceitine: play reload anim / sound

		// Destroy ammunition object.
		player.Inventory.ClearItem( ammunition );
		ammunition.GameObject?.Destroy();
	}

	private bool Fire( Player shooter )
	{
		if ( Ammo <= 0 )
		{
			// todo @ceitine: play empty gun click
			return false;
		}

		var tr = Scene.Trace.Ray( shooter.ViewRay, 5000f )
			.IgnoreGameObjectHierarchy( shooter.GameObject )
			.Run();

		var target = tr.GameObject?.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors );
		if ( target != null )
			target.SetRagdoll( true, duration: 0.5f, spin: 30 );

		// todo @ceitine: proper shoot sound
		var sound = Sound.Play( "stop" );
		sound.Position = Transform.Position;
		sound.Decibels = 120;
		sound.ListenLocal = false;

		Ammo--;

		return true;
	}
}
