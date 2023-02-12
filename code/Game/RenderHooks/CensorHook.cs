namespace Sauna;

public class CensorHook : RenderHook
{
	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterTransparent ) 
			return;

		Enabled = false; // Enable when penice.
		Order = 0;

		var entities = Game.Clients
			.Select( client => client.Pawn as ModelEntity );

		Graphics.GrabFrameTexture( "ColorTexture" );
		Graphics.GrabDepthTexture( "DepthTexture" );

		using var rt = RenderTarget.GetTemporary( 1, ImageFormat.None, ImageFormat.D24S8, -1 );
		Graphics.RenderTarget = rt;

		Graphics.Clear( Color.Black, false, true, true );

		foreach ( var entity in entities )
			if ( Game.LocalPawn != entity && entity.IsValid() && entity != null )
				censor( entity );

		Graphics.RenderTarget = null;
	}

	private void censor( ModelEntity entity )
	{
		var mat = Material.FromShader( "shaders/saunacensor.shader" );
		var pos = (entity.Position + Vector3.Up * 30).ToScreen() * new Vector3( Screen.Size, 1 );

		Graphics.Render( entity.SceneObject, material: mat );
	}
}
