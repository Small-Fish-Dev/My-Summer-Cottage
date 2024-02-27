namespace Sauna;

public sealed class WeaponComponent : Component
{
	protected override void OnStart() // todo: implement ammunition, needs prefab ref to ammunition type etc
	{
		// Interactions
		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			HoldOnly = true,
			Description = "Fire",
			Keybind = "attack1",
			Action = ( Player shooter, GameObject obj ) => Shoot( shooter ),
		} );
	}
	
	public void Shoot( Player shooter )
	{
		var tr = Scene.Trace.Ray( shooter.ViewRay, 5000f )
			.IgnoreGameObjectHierarchy( shooter.GameObject )
			.Run();

		var target = tr.GameObject?.Components.Get<Player>( FindMode.EverythingInSelfAndAncestors );
		if ( target != null )
			target.SetRagdoll( true, duration: 0.5f, spin: 30 );

		var sound = Sound.Play( "stop" );
		sound.Position = Transform.Position;
		sound.Decibels = 120;
		sound.ListenLocal = false;
	}
}
