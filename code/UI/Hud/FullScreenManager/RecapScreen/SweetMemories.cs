namespace Sauna;

public struct SweetMemory
{
	public Texture Texture;
	public Vector3 Position;
	public Rotation Rotation;
	public string Caption;
}

public class SweetMemories : Panel
{
	public const int RESOLUTION = 512;

	public static IReadOnlyList<SweetMemory> All => _all;
	private static List<SweetMemory> _all = new();
	private static List<SweetMemory> _queue = new();

	/// <summary>
	/// Captures a sweet memory based on position and rotation.
	/// </summary>
	/// <param name="caption"></param>
	/// <param name="position"></param>
	/// <param name="rotation"></param>
	/// <param name="lookAt"></param>
	/// <returns></returns>
	[Icon( "camera") ]
	public static SweetMemory Capture( string caption, Vector3 position, Rotation rotation, Vector3? lookAt = null )
	{
		var memory = new SweetMemory()
		{
			Texture = Texture.CreateRenderTarget()
				.WithSize( RESOLUTION )
				.Create( $"sweetmemory_{_all.Count}" ),

			Position = position,
			Rotation = (lookAt.HasValue ? Rotation.LookAt( lookAt.Value - position ) : Rotation.Identity) * rotation,

			Caption = caption,
		};

		_all.Add( memory );
		_queue.Add( memory );
		return memory;
	}

	/// <summary>
	/// Clears all the sweet memories.
	/// </summary>
	public static void Clear()
		=> _all.Clear();

	void Render( Texture texture, Vector3 position, Rotation rotation )
	{
		// Scene Setup
		var camera = new SceneCamera()
		{
			World = Scene.SceneWorld,
			AmbientLightColor = Color.White,
			BackgroundColor = Color.Transparent,
			Position = position,
			Rotation = rotation,
			ZFar = 5000,
			FieldOfView = 60,
		};

		// Setup censoring.
		camera.OnRenderPostProcess = () => SFX.EyeProtector.Render( camera );

		// Render to texture.
		Graphics.RenderToTexture( camera, texture );

		camera.Dispose();
	}

	public override void DrawBackground( ref RenderState state )
	{
		if ( _queue == null || _queue.Count == 0 )
			return;

		// Render
		var item = _queue.First();
		Player.HideHead = false;
		Render( item.Texture, item.Position, item.Rotation );
		_queue.Remove( item );
		Player.HideHead = true;
	}

	[ConCmd]
	public static void DebugMemory()
		=> Player.Local.CaptureMemory( Game.Random.FromArray( new string[] {
			"a day filled with joy,, NOT!",
			"mfw i wanna kill myself", 
			"i fucking hate this place get me out of here please",
			"i wish my life was more of this"
		} ), 200f );
}
