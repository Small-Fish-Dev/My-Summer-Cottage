namespace Sauna;

public class IconRenderer : Panel
{
	/// <summary>
	/// All of the rendered item icons.
	/// </summary>
	public static IReadOnlyDictionary<IconSettings, Texture> All => all;

	private static Dictionary<IconSettings, Texture> all;
	private static bool finished;

	const int TEXTURE_SIZE = 256;

	public IconRenderer()
	{
		finished = false;
		all = PrefabLibrary.FindByComponent<ItemComponent>()
			.Select( prefab => prefab
				.GetComponent<ItemComponent>()
				.Get<IconSettings>( "Icon" )
			)
			.ToDictionary(
				r => r,
				r => Texture.CreateRenderTarget()
					.WithSize( TEXTURE_SIZE )
					.Create( "icon" )
			);
	}

	Texture RenderIcon( IconSettings settings, Texture renderTarget )
	{
		// Post Processing
		void pp()
		{
			var attributes = new RenderAttributes();
			Graphics.GrabFrameTexture( "FrameTexture", attributes );

			var shader = Material.FromShader( "shaders/item_icon.shader" );
			Graphics.Blit( shader, attributes );
		}

		// Scene Setup
		var world = new SceneWorld();
		var camera = new SceneCamera()
		{
			World = world,
			AmbientLightColor = Color.White,
			OnRenderPostProcess = pp,
			AntiAliasing = false,
			BackgroundColor = Color.Transparent,
			Position = Vector3.Forward * 50f,
			Rotation = Rotation.From( 0, 180, 0 ),
			FieldOfView = 60,
			Size = TEXTURE_SIZE
		};

		_ = new SceneObject( world, settings.Model )
		{
			Position = settings.Position,
			Rotation = settings.Rotation
		};

		// Render to texture.
		Graphics.RenderToTexture( camera, renderTarget );

		// Discard world and return data.
		world?.Delete();
		camera?.Dispose();

		return renderTarget;
	}

	public override void DrawBackground( ref RenderState state )
	{
		// Delete when done.
		if ( finished )
		{
			if ( !IsDeleting )
				Delete( true );

			return;
		}

		finished = true;

		// Render each ItemDefinition's icon.
		foreach ( var (settings, renderTarget) in all )
			all[settings] = RenderIcon( settings, renderTarget );
	}
}
