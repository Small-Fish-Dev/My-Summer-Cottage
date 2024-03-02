namespace Sauna;

public sealed class TinterComponent : Component, Component.ExecuteInEditor
{
	[Property] public ModelRenderer Renderer { get; set; }
	[Property] public Color Tint
	{
		get => _tint;
		set
		{
			_tint = value;

			var obj = Renderer?.SceneObject;
			if ( obj != null )
				obj.Attributes.Set( "g_flColorTint", _tint );
		}
	}
	
	private Color _tint;
}
