namespace Sauna;

public class ShopItem : Component
{
	[Property]
	public PrefabFile Item { get; set; }

	[Property]
	public int Price { get; set; }

	/// <summary>
	/// Does this interaction use bounds?
	/// </summary>
	[Property]
	public bool HasBounds { get; set; } = false;

	protected override void OnStart()
	{
		if ( !Network.Active )
			GameObject.NetworkSpawn();

		GameObject.Name = $"Buy {PrefabLibrary.AsDefinition( Item ).GetComponent<ItemComponent>().Get<string>( "Name" )}";
		Network.SetOwnerTransfer( OwnerTransfer.Takeover );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "item.purchase",
			Action = PurchaseItem,
			Keybind = "use",
			Description = $"{Price}mk",
			Disabled = () => Player.Local.Money < Price,
			ShowWhenDisabled = () => true,
		} );
	}

	private void PurchaseItem( Player interactor, GameObject obj )
	{
		var o = SceneUtility.GetPrefabScene( Item ).Clone();
		var purchasedItem = o.Components.Get<ItemComponent>();
		if ( purchasedItem == null )
			return;

		if ( interactor.Money < Price )
			return;

		interactor.Money -= Price;
		var receivedItem = interactor.Inventory.GiveItem( purchasedItem );
		if ( !receivedItem )
			o.Transform.Position = Transform.Position;
	}
}
