namespace Sauna.UI.Hud2.Core;

/// <summary>
/// Panel that renders a prefab
/// </summary>
public class PrefabPanel : Panel
{
	private static readonly Dictionary<string, GameObject> ExistingPrefabRoots = new();
	
	private GameObject PrefabRoot { get; set; }
	private CameraComponent PrefabCamera { get; set; }
	public string PrefabPath { get; private set; }

	public string TextureName { get; private set; } = "PrefabPanel" + Random.Shared.Next( 0, 999 );
	private Texture _texture;
	
	public override void SetProperty( string name, string value )
	{
		base.SetProperty( name, value );

		if ( name is not ("src" or "path") || value == PrefabPath )
			return;

		PrefabPath = value;
		ReloadPrefab();
	}

	private void ReloadPrefab()
	{
		// HACK: this breaks hotloading
		ExistingPrefabRoots.Clear();
        
		if ( ExistingPrefabRoots.TryGetValue( PrefabPath, out var existingPrefabRoot ) )
		{
			PrefabRoot = existingPrefabRoot;
			PrefabRoot.Enabled = true;
		}
		else
		{
			// Find & load prefab
			if ( !ResourceLibrary.TryGet<PrefabFile>( PrefabPath, out var prefabFile ) )
				throw new Exception( $"Couldn't find PrefabPanel prefab! ({PrefabPath})" );

			// Create object
			PrefabRoot = SceneUtility.GetPrefabScene( prefabFile ).Clone();
			if ( PrefabRoot == null )
				throw new Exception( $"Couldn't create PrefabPanel prefab instance! ({PrefabPath})" );

			ExistingPrefabRoots[PrefabPath] = PrefabRoot;
		}
		
		// Find camera
		PrefabCamera = PrefabRoot.GetAllObjects( false )
			.Select( v => v.Components.Get<CameraComponent>() )
			.SingleOrDefault( v => v != null );
		if ( PrefabCamera == null )
			throw new Exception( $"Couldn't find PrefabPanel prefab camera! ({PrefabPath})" );

		PrefabCamera.Priority = -999;
		PrefabCamera.IsMainCamera = false;
	}

	protected override void OnAfterTreeRender( bool firstTime )
	{
		base.OnAfterTreeRender( firstTime );

		if ( Box.Rect.Width == 0 || Box.Rect.Height == 0 )
			return;

		if ( _texture == null || _texture.Size != Box.Rect.Size )
		{
			// Create new render target
			_texture = Texture.CreateRenderTarget( TextureName, ImageFormat.RGBA8888, Box.Rect.Size );
		}

		PrefabCamera.RenderToTexture( _texture );

		Style.BackgroundImage = _texture;

		StateHasChanged();
	}

	public override void OnDeleted()
	{
		base.OnDeleted();

		PrefabRoot.Enabled = false;
	}
}
