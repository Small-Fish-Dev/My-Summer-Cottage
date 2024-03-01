using Sauna.SFX;
using Sauna.UI;

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
			AntiAliasing = false,
			BackgroundColor = Color.Transparent,
			Position = position,
			Rotation = rotation,
			ZFar = 5000,
			FieldOfView = 60,
		};

		// Setup censoring.
		camera.OnRenderPostProcess = () =>
		{
			var shader = Material.FromShader( "shaders/saunaCensor.shader" );

			// Grab textures we're going to need.
			Graphics.GrabFrameTexture( "ColorTexture" );

			// Create RenderTarget for the censoring.
			using var rt = RenderTarget.GetTemporary( 1, ImageFormat.None, ImageFormat.D32FS8, MultisampleAmount.MultisampleScreen );
			Graphics.RenderTarget = rt;
			Graphics.Clear( Color.Black, clearColor: false );

			var targets = Scene.GetAllComponents<CensorComponent>();
			foreach ( var target in targets )
			{
				if ( target.Renderer == null )
					return;

				Graphics.Attributes.Set( "Width", 1f );
				Graphics.Render( target.Renderer.SceneObject, material: shader );
			}

			// Clear RenderTarget.
			Graphics.RenderTarget = null;
		};

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
		Log.Error( item.Position );
		Render( item.Texture, item.Position, item.Rotation );
		_queue.Remove( item );
		Player.HideHead = true;
	}

	[ConCmd]
	public static void CaptureBehind()
	{
		var xform = Player.Local.Transform.World;

		var center = Player.Local.Bounds.Center + Player.Local.Transform.World.Position;
		var pos = xform.Position + Vector3.Up * 60f + xform.Rotation.Forward * 150f + xform.Rotation.Left * 50f;

		Player.Local.CaptureMemory( "fuck ass bob", 200f );
	}
}
