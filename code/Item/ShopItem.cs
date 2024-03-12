using Sauna.SFX;

namespace Sauna;

public class ShopItem : Component
{
	[Property]
	public PrefabFile Item { get; set; }

	[Property]
	public int Price { get; set; }

	private readonly SoundEvent _purchaseSound = ResourceLibrary.Get<SoundEvent>( "sounds/misc/purchase.sound" );

	/// <summary>
	/// Does this interaction use bounds?
	/// </summary>
	[Property]
	public bool HasBounds { get; set; } = false;

	private LegacyParticles purchase;

	protected override void OnStart()
	{
		GameObject.SetupNetworking();

		GameObject.Name = $"Buy {PrefabLibrary.AsDefinition( Item ).GetComponent<ItemComponent>().Get<string>( "Name" )}";

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = "item.purchase",
			Action = PurchaseItem,
			Keybind = "use",
			Disabled = () => Player.Local.Money < Price || !Player.Local.Inventory.HasSpaceInBackpack(),
			DynamicText = () => Player.Local.Inventory.HasSpaceInBackpack() ? $"{Price}mk" : $"{Price}mk (Inventory full)",
			DynamicColor = () => Color.FromBytes( 107, 157, 15 ),
			ShowWhenDisabled = () => true,
			Sound = () => _purchaseSound,
		} );
		purchase = LegacyParticles.Create( "particles/purchase.vpcf", GameObject.Transform.World );
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
		purchase.replay();
		if ( !receivedItem )
			o.Transform.Position = Transform.Position;
	}
}
