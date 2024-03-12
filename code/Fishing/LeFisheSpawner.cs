namespace Sauna;

public class LeFisheSpawner : Component, Component.ITriggerListener
{
	private class VirtualFish
	{
		public PrefabFile Fish;
		public int Weight;
		public float MinimumDepth;
		public TimeUntil ShouldDie;
		public TimeUntil ShouldChangeTarget;
		public TimeUntil ShouldPullBobber;
		public Bobber TargetBobber;
	}

	[Property] public List<PrefabFile> Fishes { get; set; }
	[Property] public float MinimumDepth = 10;
	[Property] public int FishAmount = 20;

	/// <summary>
	/// Time in seconds
	/// </summary>
	public const float PoolUpdateRate = 30f;

	/// <summary>
	/// Time in seconds until the fish dies
	/// </summary>
	public static RangedFloat FishTimeToLive = new RangedFloat( 5, 60 );

	/// <summary>
	/// Time in seconds until the fish takes the bait
	/// </summary>
	public static RangedFloat FishBaitChooseTime = new RangedFloat( 5, 20 );

	/// <summary>
	/// Time in seconds until the fish needs to watch a Subway Surfer gameplay video
	/// </summary>
	public static RangedFloat FishAttentionSpan = new RangedFloat( 2, 7 );

	/// <summary>
	/// Time in seconds that defines how often a fish would pull the bobber of interest
	/// </summary>
	public static RangedFloat FishBobberPullPeriod = new RangedFloat( 0.5f, 1.5f );

	private WaterComponent _water;

	// private List<BBox> _debugFailedCells = new();
	private (float minimumDepth, PrefabFile fish)[] _fishesByDepth;

	private Dictionary<Bobber, VirtualFish> _bobbers = new();
	private List<VirtualFish> CurrentFishes { get; set; } = new();

	private (float Probability, PrefabFile Prefab)[] _fishesByProbability;

	private TimeUntil _shouldUpdatePool = 0;

	/// <summary>
	/// Sum of all the probabilities. Used for the weighted random choice.
	/// </summary>
	private float _totalProbability;

	private float _maxFishDepth;

	protected override void OnStart()
	{
		_water = Components.Get<WaterComponent>();
		if ( _water is null )
			throw new Exception( "This component should be used only on the water volumes" );

		// Sort the fish by depth
		_fishesByDepth = Fishes
			.OrderBy( x => x.AsDefinition().GetComponent<Fish>().Get<float>( "MinimumWaterDepth" ) )
			.Select( x => (x.AsDefinition().GetComponent<Fish>().Get<float>( "MinimumWaterDepth" ), x) )
			.ToArray();
		_maxFishDepth = _fishesByDepth[^1].minimumDepth;

		// TODO: oh my god i want to kill myself real bad
		_fishesByProbability = Fishes
			.OrderBy( x => x.AsDefinition().GetComponent<Fish>().Get<float>( "Rarity" ) )
			.Select( x => (1 - x.AsDefinition().GetComponent<Fish>().Get<float>( "Rarity" ), x) )
			.ToArray();
		_totalProbability =
			_fishesByProbability.Sum( x => x.Probability );
	}

	protected override void OnUpdate()
	{
		using ( Gizmo.Scope() )
		{
			Gizmo.Draw.Color = Color.Blue;
			Gizmo.Draw.IgnoreDepth = true;
			Gizmo.Draw.LineBBox( _water.Bounds );
			Gizmo.Draw.IgnoreDepth = false;
		}

		if ( !Game.IsPlaying )
			return;

		// Update the fish pool only if we have any bobbers
		if ( _bobbers.Count > 0 )
		{
			if ( _shouldUpdatePool )
			{
				var fishesToGenerate = FishAmount - CurrentFishes.Count;
				for ( var i = 0; i < fishesToGenerate; i++ )
				{
					var fishPrefab = GetRandomFish();
					if ( fishPrefab is null )
						continue;

					var fishDefinition = fishPrefab.AsDefinition().GetComponent<Fish>();
					var fishWeight = (int)fishDefinition
						.Get<RangedFloat>( "WeightRange" )
						.GetValue();
					var fishDepth = fishDefinition
						.Get<float>( "MinimumWaterDepth" );
					CurrentFishes.Add( new VirtualFish
					{
						Fish = fishPrefab,
						Weight = fishWeight,
						MinimumDepth = fishDepth,
						ShouldDie = FishTimeToLive.GetValue(),
						ShouldChangeTarget = FishBaitChooseTime.GetValue(),
						ShouldPullBobber = 0,
						TargetBobber = null
					} );
				}

				_shouldUpdatePool = PoolUpdateRate;
			}

			// Remove the invalid bobbers
			foreach ( var fish in CurrentFishes.Where( x =>
				         !x.TargetBobber.IsValid() || !_bobbers.ContainsKey( x.TargetBobber ) ) )
			{
				FishClearTarget( fish );
			}

			var freeBobbers = _bobbers.Where( kv => kv.Value is null ).Select( kv => kv.Key ).ToList();
			// This is where the fishes actually think
			foreach ( var fish in CurrentFishes )
			{
				if ( fish.TargetBobber.IsValid() )
				{
					// If the bobber does not exist or is already pulled by some other fish
					if ( !_bobbers.TryGetValue( fish.TargetBobber, out var fishBobber ) || fishBobber != fish )
					{
						fish.TargetBobber = null;
					}
					else if ( !fish.ShouldChangeTarget && fish.ShouldPullBobber )
					{
						// TODO: a hardcoded constant!
						// TODO: @ubre @luke @everyone a ripple effect
						fish.TargetBobber.Rigidbody.Velocity += Vector3.Down * fish.Weight * 0.01f;
						fish.ShouldPullBobber = FishBobberPullPeriod.GetValue();
					}
				}

				// We need to check it once again in case it became invalid in the previous if statement
				if ( fish.ShouldChangeTarget )
				{
					// If we already have a target, just let it go
					if ( fish.TargetBobber.IsValid() )
					{
						FishClearTarget( fish );
						if ( fish.TargetBobber.IsValid() )
							freeBobbers.Add( fish.TargetBobber );

						fish.ShouldChangeTarget = FishBaitChooseTime.GetValue();
					}
					// Else choose a new target
					else if ( freeBobbers.Count > 0 )
					{
						var newTarget = freeBobbers.OrderBy( _ => Guid.NewGuid() )
							.FirstOrDefault( bobber => BobberGetDepth( bobber ) >= fish.MinimumDepth );
						if ( newTarget.IsValid() )
						{
							freeBobbers.Remove( newTarget );
							FishSetTarget( fish, newTarget );

							fish.ShouldChangeTarget = FishAttentionSpan.GetValue();
						}
					}
				}
			}

			foreach ( var fish in CurrentFishes.Where( x => x.ShouldDie ) )
			{
				FishClearTarget( fish );
			}
		}

		// Always clear the pool, though
		CurrentFishes.RemoveAll( x => x.ShouldDie );
	}

	public void OnTriggerEnter( Collider other )
	{
		if ( other.Tags.Has( "bobber" ) )
		{
			TryAddBobber( other.GameObject.Components.Get<Bobber>() );
		}
	}

	public void OnTriggerExit( Collider other )
	{
		if ( other.Tags.Has( "bobber" ) )
		{
			var bobber = other.GameObject.Components.Get<Bobber>();
			if ( !_bobbers.ContainsKey( bobber ) )
				return;

			foreach ( var virtualFish in CurrentFishes.Where( virtualFish => virtualFish.TargetBobber == bobber ) )
			{
				FishClearTarget( virtualFish );
			}

			_bobbers.Remove( bobber );
		}
	}

	private void TryAddBobber( Bobber bobber )
	{
		var skyTrace = Scene.Trace.PhysicsTrace
			.Body( bobber.Rigidbody.PhysicsBody, bobber.Transform.Position + Vector3.Up * 500 )
			.Run();
		if ( skyTrace.Hit )
			return;

		_bobbers.Add( bobber, null );
	}

	private float BobberGetDepth( Bobber bobber )
	{
		var depthTrace = Scene.Trace.PhysicsTrace
			.Body( bobber.Rigidbody.PhysicsBody, bobber.Transform.Position + Vector3.Down * _maxFishDepth )
			.Run();
		return depthTrace.Distance;
	}

	public void PullOutFish( GameObject bobberGameObject )
	{
		var bobber = bobberGameObject.Components.Get<Bobber>();

		if ( !_bobbers.TryGetValue( bobber, out var fish ) || fish?.Fish == null )
			return;

		var fishInstance = SceneUtility.GetPrefabScene( fish.Fish ).Clone();
		fishInstance.NetworkMode = NetworkMode.Object;
		fishInstance.NetworkSpawn();

		var from = bobber.Transform.Position;
		var to = bobber.Rod.Owner.Transform.Position;
		var dist = from.Distance( to );
		var velocity = (to - from).Normal * dist + Vector3.Up * 400f;

		if ( fishInstance.Components.TryGet<Rigidbody>( out var rigidbody ) )
		{
			fishInstance.Transform.Position = bobber.Transform.Position;
			rigidbody.Velocity = velocity;
		}

		var fishComponent = fishInstance.Components.Get<Fish>();
		fishComponent.AssignWeight( fish.Weight );

		if ( !IsProxy )
			Player.Local.OnFishCaught( fish.Fish, fish.Weight );
	}

	/// <summary>
	/// Get a random fish
	/// </summary>
	/// <returns>null if the cell has no fish</returns>
	private PrefabFile GetRandomFish()
	{
		if ( _fishesByProbability.Length == 0 )
			return null;

		var prob = Game.Random.NextDouble() * _totalProbability;
		var fish = _fishesByProbability[0].Prefab;

		for ( int i = 0; prob > 0 && i < _fishesByProbability.Length; i++ )
		{
			var pair = _fishesByProbability[i];
			fish = pair.Prefab;

			prob -= pair.Probability;
			if ( prob <= 0 )
				break;
		}

		return fish;
	}

	private void FishSetTarget( VirtualFish fish, Bobber bobber )
	{
		_bobbers[bobber] = fish;
		fish.TargetBobber = bobber;
	}

	private void FishClearTarget( VirtualFish fish )
	{
		if ( !fish.TargetBobber.IsValid() )
			return;

		_bobbers[fish.TargetBobber] = null;
		fish.TargetBobber = null;
	}
}
