using Sauna.SFX;

namespace Sauna;

public class ShopItem : Component
{
	[Property]
	public PrefabFile Item { get; set; }

	[Property]
	public int Price { get; set; }

	private readonly SoundEvent _purchaseSound = ResourceLibrary.Get<SoundEvent>( "sounds/misc/purchase.sound" );

	private LegacyParticles purchase;

	protected override void OnStart()
	{
		GameObject.SetupNetworking();
		GameObject.Name = $"Buy {PrefabLibrary.AsDefinition( Item )?.GetComponent<ItemComponent>()?.Get<string>( "Name" ) ?? "Item"}";

		if ( IsParcel() )
			CreateWorldIcon();

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

	private bool IsParcel()
	{
		return Components.GetOrCreate<ModelRenderer>().Model.Name.Contains( "parcel" );
	}

	private void CreateWorldIcon()
	{
		var iconWorldObject = new GameObject { Parent = GameObject };
		iconWorldObject.Transform.LocalPosition = new Vector3( 0, 0, 5 );
		iconWorldObject.Transform.LocalRotation = Rotation.FromPitch( 90 );
		iconWorldObject.Components.GetOrCreate<Sandbox.WorldPanel>();
		iconWorldObject.Components.GetOrCreate<IconWorldPanel>().Icon = Texture.Load( FileSystem.Mounted, PrefabLibrary.AsDefinition( Item ).GetComponent<ItemComponent>().Get<IconSettings>( "Icon" ).Path );
	}
}
