namespace Sauna;

public class ItemComponent : Component
{
	[Property] public IconSettings Icon { get; set; }
	[Property] public string Name { get; set; }
	[Property] public string Description { get; set; }
	[Property] public float Weight { get; set; }

	public Texture Texture => Texture.Load( FileSystem.Mounted, $"ui/icons/{Icon.Guid}.png" );

	public static implicit operator ItemComponent( GameObject obj )
		=> obj.Components.Get<ItemComponent>();

	public bool DrawingEnabled
	{
		get => GameObject.Components.Get<ModelRenderer>( true ).Enabled;
		set => GameObject.Components.Get<ModelRenderer>( true ).Enabled = value;
	}

	protected override void OnStart()
	{
		GameObject.Name = Name;
	}
}
