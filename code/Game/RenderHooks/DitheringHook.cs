namespace Sauna;

[SceneCamera.AutomaticRenderHook]
public class DitheringHook : RenderHook
{
	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterUI ) return;
		var mat = Material.FromShader( "shaders/saunadither.shader" );
		var attributes = new RenderAttributes();

		Graphics.GrabFrameTexture( "ColorBuffer", attributes );
		Graphics.RenderTarget = null;
		Graphics.Blit( mat, attributes );
	}
}
