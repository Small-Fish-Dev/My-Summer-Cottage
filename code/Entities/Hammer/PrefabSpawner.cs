namespace Sauna;

/// <summary>
/// Workaround entity for spawning prefabs in the map.
/// </summary>
[HammerEntity]
[EditorModel( "models/sbox_props/ceiling_halogen/ceiling_halogen.vmdl" )]
public partial class PrefabSpawner : Entity
{
	/// <summary>
	/// The prefab's name.
	/// </summary>
	[Property, ResourceType( ".prefab" )]
	public string Prefab { get; set; }

	public override void Spawn()
	{
		Transmit = TransmitType.Never;

		// Failed to create prefab.
		if ( !PrefabLibrary.TrySpawn<BaseItem>( Prefab, out var entity ) )
		{
			Log.Error( $"Failed to create map prefab \"{Prefab}\"." );
			Delete();

			return;
		}

		// Copy some properties over.
		entity.Transform = Transform;
		entity.Parent = Parent;
		entity.FromMap = true;

		foreach ( var tag in Tags.List )
			entity.Tags.Add( tag );

		// Delete spawner, we don't need it to exist.
		Delete();
	}

	/*private static Prefab gizmoPrefab;
	private static string gizmoPath;

	// Render prefab's model.
	public static void DrawGizmos( EditorContext context )
	{
		var path = context.Target.GetProperty( "Prefab" ).As.String;
		if ( path == null )
			return;
		
		// Hacky prefab reading.
		if ( path != gizmoPath )
		{
			var filepath = $"addons/sauna/{path}";
			if ( File.Exists( filepath ) )
			{
				var json = File.ReadAllText( filepath );
				gizmoPrefab = System.Text.Json.JsonSerializer.Deserialize<Prefab>( json );
			}
			else
				gizmoPrefab = null;

			gizmoPath = path;
			return;
		}

		// Check if prefab is valid.
		if ( gizmoPrefab == null )
		{
			Gizmo.Draw.IgnoreDepth = true;	
			Gizmo.Draw.Color = Color.Red;
			Gizmo.Draw.Text( $"Invalid prefab: {path}", Transform.Zero );

			return;
		}

		// Get model and draw it.
		var model = gizmoPrefab.Root.GetValue<Model>( "Model" );
		if ( model == null )
			return;

		Gizmo.Draw.Model( model );
	}*/
}
