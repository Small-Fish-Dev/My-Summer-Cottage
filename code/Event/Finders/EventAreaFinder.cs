using Sandbox;
using Sauna.Event;

[Icon( "crop_din" )]
[Category( "Events" )]
public sealed class EventAreaFinder : EventTrigger
{
	[Property]
	public Vector3 Offset { get; set; }

	[Property]
	public Vector3 Extents { get; set; }

	/// <summary>
	/// A list of prefabs that will be included in this area
	/// Leave empty if you just want to check tags
	/// Still need to match tags
	/// DOESN'T WORK IF PREFAB WAS UNLINKED/COLLAPSED
	/// </summary>
	[Property]
	public List<PrefabFile> TriggerPrefab { get; set; } = new List<PrefabFile>();

	/// <summary>
	/// Tags that are included in this area (Any)
	/// </summary>
	[Property]
	public TagSet TagSet { get; set; }

	/// <summary>
	/// Get all objects inside of this area
	/// </summary>
	public List<GameObject> ObjectsInside { get; set; } = new();

	public BBox BBox => new BBox( Offset - Extents / 2f, Offset + Extents / 2f );
	public BBox WorldBBox => BBox.Transform( GameObject.Transform.World );

	public override bool IsPolled { get; set; } = true;

	public override void PolledMethod()
	{
		var find = Scene.FindInPhysics( WorldBBox )
			?.Where( x => x.Tags.HasAny( TagSet ) )
			?.Where( x => TriggerPrefab.Count() == 0 || TriggerPrefab.Any( prefab => prefab.ResourcePath == x.PrefabInstanceSource ) );

		if ( find != null )
			ObjectsInside = find.ToList();
	}

	protected override void OnAwake()
	{
		base.OnAwake();
		Tags.Set( "trigger", true );
	}

	public override void Clear()
	{
		ObjectsInside.Clear();
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected && !Gizmo.IsChildSelected ) return;

		Gizmo.Draw.Color = Color.Yellow.WithAlpha( 0.2f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
