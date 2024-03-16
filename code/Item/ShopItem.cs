using Sauna.SFX;

namespace Sauna;

public class ShopItem : Component
{
	[Property]
	public PrefabFile Item { get; set; }

	[Property]
	public int Price { get; set; }

	private readonly SoundEvent _purchaseSound = ResourceLibrary.Get<SoundEvent>( "sounds/misc/purchase.sound" );
	private GameObject _iconWorldObject;

	protected override void OnStart()
	{
		GameObject.SetupNetworking();
		ResetName();

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
		LegacyParticles.Create( "particles/purchase.vpcf", GameObject.Transform.World, deleteTime: 1000 );
		if ( !receivedItem )
			o.Transform.Position = Transform.Position;
	}

	private bool IsParcel()
	{
		return Components.GetOrCreate<ModelRenderer>().Model.Name.Contains( "parcel" );
	}

	public void CreateWorldIcon()
	{
		_iconWorldObject?.Destroy();
		_iconWorldObject = new GameObject { Parent = GameObject };
		_iconWorldObject.Transform.LocalPosition = new Vector3( 0, 0, 5 );
		_iconWorldObject.Transform.LocalRotation = Rotation.FromPitch( 90 );
		_iconWorldObject.Components.GetOrCreate<Sandbox.WorldPanel>();
		_iconWorldObject.Components.GetOrCreate<IconWorldPanel>().Icon = Texture.Load( FileSystem.Mounted, PrefabLibrary.AsDefinition( Item ).GetComponent<ItemComponent>().Get<IconSettings>( "Icon" ).Path );
	}

	public void ResetName()
	{
		GameObject.Name = $"Buy {PrefabLibrary.AsDefinition( Item )?.GetComponent<ItemComponent>()?.Get<string>( "Name" ) ?? "Item"}";
	}
	public void ResetPrice()
	{
		Price = PrefabLibrary.AsDefinition( Item )?.GetComponent<ItemComponent>()?.Get<int>( "SellPrice" ) ?? Game.Random.Int( 10, 80 );
	}
}
