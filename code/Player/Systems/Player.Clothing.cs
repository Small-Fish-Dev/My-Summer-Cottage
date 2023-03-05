namespace Sauna;

[Flags]
public enum ClothingSlot
{
	None = 0,
	Head = 1 << 0,
	Eyes = 1 << 1,
	Torso = 1 << 2,
	Legs = 1 << 3,
	Feet = 1 << 4,
}

partial class Player
{
	[Net] public ModelEntity Penoid { get; set; }

	/// <summary>
	/// Get the transform of the penoid.
	/// </summary>
	/// <returns></returns>
	public Transform GetPenoidTransform()
	{
		if ( Penoid == null || !Penoid.IsValid ) // Something went wrong, penoid is invalid.
			return default;

		// Size of penice.
		var morph = Morphs.Get( "size" );

		// Size of smallest possible penice.
		var smallAttachment = Penoid.GetAttachment( "penoid_min" );
		if ( smallAttachment == null )
			return default;

		// Size of biggest possible penice.
		var bigAttachment = Penoid.GetAttachment( "penoid_max" );
		if ( bigAttachment == null )
			return default;

		// Lerp between the two values to get actual position of penice tip.
		return Transform.Lerp( smallAttachment.Value, bigAttachment.Value, morph, true );
	}

	[Event( "OnSpawn" )]
	private static void createPenoid( Player player )
	{
		if ( !Game.IsServer )
			return;

		var penoid = new ModelEntity();
		penoid.SetModel( "models/guy/penoid.vmdl" );
		penoid.SetParent( player, true );
		penoid.EnableShadowCasting = false;
		penoid.EnableHideInFirstPerson = false;

		var rand = new Random( (int)(player.Client.SteamId % int.MaxValue) );
		player.Morphs.Set( "size", rand.Next( -20, 100 ) / 100f );
		player.Penoid = penoid;
	}
}
