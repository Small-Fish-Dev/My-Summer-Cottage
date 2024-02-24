namespace Sauna;

public class ItemComponent : Component
{
	[Property] public IconSettings Icon { get; set; }
	[Property] public string Name { get; set; }
	[Property] public string Description { get; set; }
	[Property] public int WeightInGrams { get; set; }

	public Texture IconTexture => Texture.Load( FileSystem.Mounted, $"ui/icons/{Icon.Guid}.png" );

	public static implicit operator ItemComponent( GameObject obj )
		=> obj.Components.Get<ItemComponent>();

	[Sync]
	public bool DrawingEnabled
	{
		get => GameObject.Enabled;
		set => GameObject.Enabled = value;
	}

	protected override void OnStart()
	{
		GameObject.NetworkSpawn();
		GameObject.Name = Name;
		Network.SetOwnerTransfer( OwnerTransfer.Takeover );
	}
}
