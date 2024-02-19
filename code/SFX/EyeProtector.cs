namespace Sauna.SFX;

public class EyeProtector : Component, Component.ExecuteInEditor
{
	private IDisposable hook;
	private Material shader = Material.FromShader( "shaders/saunaCensor.shader" );

	public void Render( SceneCamera camera )
	{
		// Grab textures we're going to need.
		Graphics.GrabFrameTexture( "ColorTexture" );
		
		// Create RenderTarget for the censoring.
		using var rt = RenderTarget.GetTemporary( 1, ImageFormat.None, ImageFormat.D32FS8, MultisampleAmount.MultisampleScreen );
		Graphics.RenderTarget = rt;
		Graphics.Clear( Color.Black, clearColor: false );

		var targets = Scene.GetAllComponents<CensorComponent>();
		foreach ( var target in targets )
			Censor( target );

		// Clear RenderTarget.
		Graphics.RenderTarget = null;
	}

	private void Censor( CensorComponent component )
	{
		if ( component.Renderer == null )
			return;

		Graphics.Attributes.Set( "Width", 1f );
		Graphics.Render( component.Renderer.SceneObject, material: shader );
	}

	protected override void OnEnabled()
	{
		hook?.Dispose();

		var camera = Components.Get<CameraComponent>( FindMode.InSelf );
		hook = camera.AddHookAfterTransparent( "Censoring", 10, Render );
	}

	protected override void OnDisabled()
	{
		hook?.Dispose();
		hook = null;
	}
}
