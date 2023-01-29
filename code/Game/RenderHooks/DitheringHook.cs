namespace Sauna;

[SceneCamera.AutomaticRenderHook]
public class DitheringHook : RenderHook
{
	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterUI ) return;

		Order = 1;

		var mat = Material.FromShader( "shaders/saunadither.shader" );
		var attributes = new RenderAttributes();

		Graphics.GrabFrameTexture( "ColorBuffer", attributes );
		Graphics.Blit( mat, attributes );
	}
}
