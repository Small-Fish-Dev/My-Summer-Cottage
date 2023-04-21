namespace Sauna;

/// <summary>
/// Workaround entity for spawning prefabs in the map.
/// </summary>
[HammerEntity]
[Text( "Prefab Spawner" )]
public partial class PrefabSpawner : Entity
{
	/// <summary>
	/// The prefab's name.
	/// </summary>
	[Property, ResourceType( ".prefab" )]
	public string Prefab { get; set; }

	public override void Spawn()
	{
		Transmit = TransmitType.Never;

		// Failed to create prefab.
		if ( !PrefabLibrary.TrySpawn<BaseItem>( Prefab, out var entity ) )
		{
			Log.Error( $"Failed to create map prefab \"{Prefab}\"." );
			Delete();

			return;
		}

		// Copy some properties over.
		entity.Transform = Transform;
		entity.Parent = Parent;
		entity.FromMap = true;

		// Delete spawner, we don't need it to exist.
		Delete();
	}
}
