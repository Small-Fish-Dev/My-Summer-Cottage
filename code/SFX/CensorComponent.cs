namespace Sauna.SFX;

[Description( "Add this component to everything you want to be censored!" )]
public sealed class CensorComponent : Component
{
	public ModelRenderer Renderer { get; set; }

	protected override void OnStart()
	{
		Renderer = Components.Get<ModelRenderer>( true );
	}
}
