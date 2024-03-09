namespace Sauna.Fishing;

public class LeFisheSpawner : Component
{
	[Property] public List<PrefabFile> Fishes { get; set; }
	[Property] public float GridCellSize = 128;
	[Property] public float MinimumDepth = 10;
	[Property] public int FishesPerDepth = 2;
	[Property] public float FishesDepthIncrement = 10;

	private WaterComponent _water;
	// private List<BBox> _debugFailedCells = new();
	private List<(float minimumDepth, PrefabFile fish)> _fishesSorted;

	protected override void OnStart()
	{
		_water = Components.Get<WaterComponent>();
		if ( _water is null )
			throw new Exception( "This component should be used only on the water volumes" );

		// Sort the fish by depth
		_fishesSorted = Fishes
			.OrderBy( x => x.AsDefinition().GetComponent<Fish>().Get<float>( "MinimumWaterDepth" ) )
			.Select( x => (x.AsDefinition().GetComponent<Fish>().Get<float>( "MinimumWaterDepth" ), x) )
			.ToList();

		var countX = (int)Math.Ceiling( _water.Bounds.Size.x / GridCellSize );
		var countY = (int)Math.Ceiling( _water.Bounds.Size.y / GridCellSize );

		var begX = _water.Bounds.Mins.x;
		var begY = _water.Bounds.Mins.y;
		var waterTop = _water.Bounds.Maxs.z;

		for ( var x = 0; x < countX; x++ )
			for ( var y = 0; y < countY; y++ )
			{
				var center = new Vector3( begX + (x + 0.5f) * GridCellSize, begY + (y + 0.5f) * GridCellSize, waterTop );
				var bbox = new BBox( new Vector3( -GridCellSize / 2, -GridCellSize / 2, -MinimumDepth ),
					new Vector3( GridCellSize / 2, GridCellSize / 2, 0 ) );

				var skyTrace = Scene.Trace
					.Box( bbox, center, center + Vector3.Up * 100 )
					.Run();
				if ( skyTrace.Hit )
					continue;

				var depthTrace = Scene.Trace
					.Box( bbox, center, center + Vector3.Down * 100 )
					.Run();
				bbox = bbox.AddPoint( Vector3.Down * depthTrace.Distance );

				var depth = bbox.Size.z;
				var availableFish = _fishesSorted.TakeWhile( fish => fish.minimumDepth <= depth ).Select( fish => fish.fish ).ToList();
				// Let's not add cells that don't have any fishes
				if ( availableFish.Count == 0 )
					continue;

				var cellGameObject = new GameObject { Transform = { Position = center } };
				cellGameObject.Tags.Add( "fishing_cell" );

				var boxCollider = cellGameObject.Components.Create<BoxCollider>();
				boxCollider.Center = bbox.Size.z / 2 * Vector3.Down;
				boxCollider.Scale = bbox.Size;
				boxCollider.IsTrigger = true;

				var fishingCell = cellGameObject.Components.Create<FishingCell>();
				fishingCell.FishCount =
					FishesPerDepth * Math.Max( (int)Math.Floor( bbox.Size.z / FishesDepthIncrement ), 1 );

				fishingCell.AvailableFish = availableFish;

				cellGameObject.Name = $"Fishing Cell [{x},{y}]";
				cellGameObject.SetParent( GameObject );
			}
	}

	protected override void OnUpdate()
	{
		if ( !Game.IsPlaying )
			return;

		using ( Gizmo.Scope() )
		{
			Gizmo.Draw.Color = Color.Blue;
			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Draw.LineBBox( _water.Bounds );

			Gizmo.Draw.IgnoreDepth = false;
			//
			// 	Gizmo.Draw.Color = Color.Red;
			// 	foreach ( var sweep in _debugFailedCells )
			// 	{
			// 		Gizmo.Draw.LineBBox( sweep );
			// 	}
		}
	}
}
