namespace Sauna;

public class EyeProtector : RenderHook
{
	public override void OnStage( SceneCamera target, Stage stage )
	{
		if ( stage != Stage.AfterTransparent )
			return;

		Order = 1;

		// Grab textures we're going to need.
		Graphics.GrabFrameTexture( "ColorTexture" );
		Graphics.GrabDepthTexture( "DepthTexture" );

		// Create RenderTarget for the censoring.
		using var rt = RenderTarget.GetTemporary( 1, ImageFormat.None, ImageFormat.D24S8, -1 );
		Graphics.RenderTarget = rt;
		Graphics.Clear( Color.Black, false, true, true );

		// Actual effect rendered on penoids.
		var players = Game.Clients.Select( cl => cl.Pawn as Player );
		foreach ( var player in players )
		{
			var penoid = (player.Ragdoll != null 
				? (player.Ragdoll.Children.FirstOrDefault( ent => (ent as ModelEntity)?.GetModelName() == "models/guy/penoid.vmdl" ) as AnimatedEntity)?.SceneObject
				: player.Penoid?.SceneObject) as SceneModel;

			if ( penoid == null || !penoid.IsValid() )
				continue;

			drawCensoring( penoid );
		}

		// Clear RenderTarget.
		Graphics.RenderTarget = null;
	}

	private void drawCensoring( SceneModel obj )
	{
		var mat = Material.FromShader( "shaders/saunaCensor.shader" );
		
		Graphics.Attributes.Set( "Width", 0.5f );
		Graphics.Render( obj, material: mat );
	}
}
