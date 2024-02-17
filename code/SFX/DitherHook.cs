namespace Sauna.SFX;

public class DitherHook : Component, Component.ExecuteInEditor
{
	private IDisposable hook;
	private RenderAttributes attributes = new RenderAttributes();

	public void Render( SceneCamera camera )
	{
		var shader = Material.FromShader( "shaders/saunadither.shader" );

		Graphics.GrabFrameTexture( "ColorTexture", renderAttributes: attributes );
		Graphics.Blit( shader, attributes );
	}

	protected override void OnEnabled()
	{
		hook?.Dispose();

		var camera = Components.Get<CameraComponent>( FindMode.InSelf );
		hook = camera.AddHookAfterUI( "Dither", 20, Render );
	}

	protected override void OnDisabled()
	{
		hook?.Dispose();
		hook = null;
	}
}
