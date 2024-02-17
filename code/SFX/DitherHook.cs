namespace Sauna.SFX;

public class DitherHook : Component, Component.ExecuteInEditor
{
	private IDisposable hook;
	private RenderAttributes attributes = new RenderAttributes();

	public void Render( SceneCamera camera )
	{
		var shader = Material.FromShader( "shaders/saunadither.shader" );

		Graphics.GrabFrameTexture( "ColorBuffer", renderAttributes: attributes );
		Graphics.Blit( shader, attributes );
	}

	protected override void OnEnabled()
	{
		hook?.Dispose();

		var camera = Components.Get<CameraComponent>( FindMode.InSelf );
		hook = camera.AddHookAfterUI( "Dither", 99, Render );
	}

	protected override void OnDisabled()
	{
		hook?.Dispose();
		hook = null;
	}
}
