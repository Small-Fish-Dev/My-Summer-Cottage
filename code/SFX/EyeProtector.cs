namespace Sauna.SFX;

// TODO: fix
public class EyeProtector : Component, Component.ExecuteInEditor
{
	/*private IDisposable hook;
	private RenderAttributes attributes = new RenderAttributes();

	public void Render( SceneCamera camera )
	{
		var shader = Material.FromShader( "shaders/saunaDither.shader" );

		Graphics.GrabFrameTexture( renderAttributes: attributes );
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
	}*/
}
