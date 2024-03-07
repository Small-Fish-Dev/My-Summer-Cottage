namespace Sauna.Fishing;

public class FishingCell : Component
{
	private class CellFish
	{
		public PrefabFile Fish;
		public int Weight;
		public TimeUntil ShouldDie;
		public TimeUntil ShouldChangeTarget;
		public TimeUntil ShouldPullBobber;
		public Bobber TargetBobber;
	}

	[Property] public List<PrefabFile> AvailableFish { get; set; }
	[Property] public int FishCount { get; set; }

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

	public TimeUntil ShouldUpdatePool = 0;

	public BoxCollider Collider { get; private set; }

	private (float Probability, PrefabFile Prefab)[] _fishesByProbability;

	/// <summary>
	/// Sum of all the probabilities. Used for the weighted random choice.
	/// </summary>
	private float _totalProbability;

	/// <summary>
	/// Used to scare away the fish.
	/// </summary>
	private bool _hasPlayersInside;

	private Dictionary<Bobber, CellFish> _bobbers = new();
	private List<CellFish> CurrentFishes { get; set; } = new();

	protected override void OnAwake()
	{
		if ( AvailableFish.Count == 0 )
			GameObject.Destroy();

		Collider = Components.Get<BoxCollider>();
		if ( Collider is null )
			throw new Exception( "Cannot find a collider" );

		// TODO: oh my god i want to kill myself real bad
		_fishesByProbability = AvailableFish
			.OrderBy( x => x.AsDefinition().GetComponent<Fish>().Get<float>( "Rarity" ) )
			.Select( x => (1 - x.AsDefinition().GetComponent<Fish>().Get<float>( "Rarity" ), x) ).ToArray();
		_totalProbability =
			_fishesByProbability.Sum( x => x.Probability );
	}

	protected override void OnUpdate()
	{
		if ( !Game.IsPlaying )
			return;

		// Update the fish pool only if we have any bobbers
		if ( _bobbers.Count > 0 )
		{
			if ( ShouldUpdatePool )
			{
				var fishesToGenerate = FishCount - CurrentFishes.Count;
				for ( var i = 0; i < fishesToGenerate; i++ )
				{
					var fishPrefab = GetRandomFish();
					var fishWeight = (int)fishPrefab.AsDefinition().GetComponent<Fish>()
						.Get<RangedFloat>( "WeightRange" )
						.GetValue();
					CurrentFishes.Add( new CellFish
					{
						Fish = fishPrefab,
						Weight = fishWeight,
						ShouldDie = FishTimeToLive.GetValue(),
						ShouldChangeTarget = FishBaitChooseTime.GetValue(),
						ShouldPullBobber = 0,
						TargetBobber = null
					} );
				}

				ShouldUpdatePool = PoolUpdateRate;
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
						Log.Info( "pull!" );
						// TODO: a hardcoded constant!
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
						Log.Info( "gave up" );
						FishClearTarget( fish );
						if ( fish.TargetBobber.IsValid() )
							freeBobbers.Add( fish.TargetBobber );

						fish.ShouldChangeTarget = FishBaitChooseTime.GetValue();
					}
					// Else choose a new target
					else if ( freeBobbers.Count > 0 )
					{
						var newTargetIndex = Game.Random.Next( 0, freeBobbers.Count );
						var newTarget = freeBobbers[newTargetIndex];
						freeBobbers.RemoveAt( newTargetIndex );
						FishSetTarget( fish, newTarget );

						fish.ShouldChangeTarget = FishAttentionSpan.GetValue();
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

		using ( Gizmo.Scope() )
		{
			Gizmo.Draw.Color = Color.Green;
			Gizmo.Draw.LineBBox( BBox.FromPositionAndSize( Collider.Center + Transform.Position, Collider.Scale ) );
		}
	}

	private void FishSetTarget( CellFish fish, Bobber bobber )
	{
		_bobbers[bobber] = fish;
		fish.TargetBobber = bobber;
	}

	private void FishClearTarget( CellFish fish )
	{
		if ( !fish.TargetBobber.IsValid() )
			return;

		_bobbers[fish.TargetBobber] = null;
		fish.TargetBobber = null;
	}

	protected override void OnFixedUpdate()
	{
		_hasPlayersInside = false;
		var bobbers = new Dictionary<Bobber, CellFish>();
		foreach ( var other in Collider.Touching )
		{
			if ( other.Tags.Has( "bobber" ) )
			{
				var bobber = other.Components.Get<Bobber>();
				if ( _bobbers.TryGetValue( bobber, out var fish ) )
					bobbers[bobber] = fish;
				else
					bobbers[bobber] = null;
				bobber.CurrentCell ??= this;
			}
			else if ( !_hasPlayersInside && other.Tags.Has( "player" ) )
			{
				// TODO: _collider.Touching will always think that a player is inside of the trigger even if they're no longer there
				// TODO: scare away the fish as soon as this bug is fixed
				// _hasPlayers = true;
				// Log.Info( "has players" );
			}
		}
		// Log.Info( string.Join( ", ", bobbers ));

		if ( bobbers.Count == 0 && _bobbers.Count > 0 )
			_bobbers.Clear();
		else if ( bobbers.Count > 0 )
			_bobbers = bobbers;
	}

	public void PullOutFish( GameObject bobberGameObject )
	{
		Log.Info( "pull out fish" );

		var bobber = bobberGameObject.Components.Get<Bobber>();

		if ( !_bobbers.TryGetValue( bobber, out var fish ) || fish?.Fish == null )
			return;

		var fishInstance = SceneUtility.GetPrefabScene( fish.Fish ).Clone();
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
		else if ( fishInstance.Components.TryGet<ModelPhysics>( out var physics ) )
		{
			fishInstance.Enabled = false;
			fishInstance.Transform.Position = bobber.Transform.Position;
			fishInstance.Enabled = true;

			// TODO: should probably calculate a parabolic trajectory
			physics.PhysicsGroup?.AddVelocity( velocity );
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
}
