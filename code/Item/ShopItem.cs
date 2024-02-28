namespace Sauna;

public class ShopItem : Component, Component.ExecuteInEditor
{
	[Property]
	public string Name { get; set; }

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

		GameObject.Name = $"{Name} €{Price}";
		Network.SetOwnerTransfer( OwnerTransfer.Takeover );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "item.purchase",
			Action = PurchaseItem,
			Keybind = "use",
			Description = $"Purchase €{Price}",
			Disabled = () => Player.Local.Money < Price,
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
