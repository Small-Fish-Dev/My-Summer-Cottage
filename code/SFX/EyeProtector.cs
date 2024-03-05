namespace Sauna.SFX;

public class EyeProtector : Component, Component.ExecuteInEditor
{
	private IDisposable hook;
	private static Material shader = Material.FromShader( "shaders/saunaCensor.shader" );

	public static void Render( SceneCamera camera, SceneObject specific = null )
	{
		// Grab textures we're going to need.
		Graphics.GrabFrameTexture( "ColorTexture" );
		
		// Create RenderTarget for the censoring.
		using var rt = RenderTarget.GetTemporary( 1, ImageFormat.None, ImageFormat.D32FS8, MultisampleAmount.MultisampleScreen );
		Graphics.RenderTarget = rt;
		Graphics.Clear( Color.Black, clearColor: false );

		if ( specific != null )
			Censor( specific );
		else
		{
			var targets = Game.ActiveScene?.GetAllComponents<CensorComponent>();
			if ( targets != null )
				foreach ( var target in targets )
				{
					var obj = target?.Renderer?.SceneObject;
					if ( obj == null )
						continue;

					Censor( obj );
				}
		}

		// Clear RenderTarget.
		Graphics.RenderTarget = null;
	}

	private static void Censor( SceneObject obj )
	{
		if ( !obj.RenderingEnabled )
			return;

		Graphics.Attributes.Set( "Width", 1f );
		Graphics.Render( obj, material: shader );
	}

	protected override void OnEnabled()
	{
		hook?.Dispose();

		var camera = Components.Get<CameraComponent>( FindMode.InSelf );
		hook = camera.AddHookAfterTransparent( "Censoring", 10, ( SceneCamera cam ) => Render( cam ) );
	}

	protected override void OnDisabled()
	{
		hook?.Dispose();
		hook = null;
	}
}
