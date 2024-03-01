using Sauna.Event;

[Icon( "highlight_alt" )]
[Category( "Events" )]
public sealed class EventAreaTrigger : EventTrigger
{
	[Property]
	public Vector3 Offset { get; set; }

	[Property]
	public Vector3 Extents { get; set; }

	/// <summary>
	/// A list of prefabs that will trigger this area
	/// Leave empty if you just want to check tags
	/// Still need to match tags
	/// DOESN'T WORK IF PREFAB WAS UNLINKED/COLLAPSED
	/// </summary>
	[Property]
	public List<PrefabFile> TriggerPrefab { get; set; } = new List<PrefabFile>();

	/// <summary>
	/// Tags that trigger this area trigger (Any)
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

		foreach ( var found in find )
			if ( !ObjectsInside.Contains( found ) ) // Has entered just now
				CallTrigger( found );

		ObjectsInside = find.ToList();
	}

	public override void Clear()
	{
		ObjectsInside.Clear();
	}

	protected override void DrawGizmos()
	{
		if ( !Gizmo.IsSelected ) return;

		Gizmo.Draw.Color = Color.Cyan.WithAlpha( 0.4f );
		Gizmo.Draw.SolidBox( BBox );
		Gizmo.Draw.Color = Color.White.WithAlpha( 0.3f );
		Gizmo.Draw.LineBBox( BBox );
	}
}
