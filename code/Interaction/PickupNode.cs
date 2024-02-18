namespace Sauna;

public static class PickupNode
{
	[ActionGraphNode( "sauna.inventory" ), Pure]
	[Title( "Pickup item" ), Group( "Sauna" ), Icon( "exposure_plus_1" )]
	public static void Pickup( Player interactor, GameObject obj )
	{
		interactor.Inventory.GiveItem( obj );
	}
}
