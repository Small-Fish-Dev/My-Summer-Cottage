using static Sauna.BusStop;

namespace Sauna;

public sealed class BusStop : Component
{
	public enum Stop
	{
		Suburbs,
		City
	}

	[Property]
	[JsonInclude]
	public Stop Type { get; set; } = Stop.Suburbs;

	[Property]
	[JsonIgnore]
	public GameObject Car { get; set; }

	[Property]
	[JsonInclude]
	public List<GameObject> Waypoints { get; set; }

	[Property]
	public int Price = 5;

	List<Vector3> _waypoints;

	public Vector3 TaxiPosition => Transform.World.PointToWorld( Vector3.Forward * 120f );

	public class Taxi
	{
		public Player Player;
		public GameObject Car;
		public BusStop From;
		public BusStop To;
		public float Speed;
		public int CurrentWaypoint = -1;
		public SoundHandle DriveSound;

		public Taxi() { }
		public Taxi( Player player, GameObject car, BusStop from, BusStop to, float speed )
		{
			Player = player;
			Car = car;
			From = from;
			To = to;
			Speed = speed;
		}
	}

	private readonly SoundEvent _driveSound = ResourceLibrary.Get<SoundEvent>( "sounds/misc/car_constant_drive.sound" );
	private readonly List<Taxi> _activeTaxis = new();

	protected override void OnStart()
	{
		base.OnStart();

		var interactions = Components.GetOrCreate<Interactions>();

		interactions.AddInteraction
			(
				new Interaction()
				{
					Identifier = $"taxi.travel.{Type}",
					Keybind = "use",
					Description = $"Travel to {(Type == Stop.Suburbs ? "city" : "suburbs")} ({Price}mk)",
					Action = ( Player player, GameObject target ) =>
					{
						CreateCar( player, FindBusStop( Type == Stop.Suburbs ? Stop.City : Stop.Suburbs ), 1000f );
					},
					Disabled = () => Player.Local.Money < Price || _activeTaxis.Any( x => x.Player == Player.Local ),
					ShowWhenDisabled = () => true,
					Animation = InteractAnimations.None,
					InteractDistance = 150f,
				}
			);

		_waypoints = new();
		_waypoints = _waypoints.Concat( Waypoints.Select( x => x.Transform.Position ) ).ToList();
		_waypoints.Add( FindBusStop( Type == Stop.Suburbs ? Stop.City : Stop.Suburbs ).TaxiPosition );
	}

	protected override void OnUpdate()
	{
		var toRemove = new List<Taxi>();

		foreach ( var taxi in _activeTaxis )
		{
			if ( taxi.CurrentWaypoint < _waypoints.Count )
			{
				if ( taxi.DriveSound is null || taxi.DriveSound.IsStopped )
					taxi.DriveSound = taxi.Car.PlaySound( _driveSound );

				var currentEndPosition = taxi.CurrentWaypoint == -1 ? taxi.Player.Transform.Position : _waypoints[taxi.CurrentWaypoint];

				var startPosition = taxi.Car.Transform.Position + Vector3.Up * 2000f;
				var endPosition = taxi.Car.Transform.Position + Vector3.Down * 2000f;

				var trace = Scene.Trace.Ray( startPosition, endPosition )
					.Size( 3f )
					.IgnoreGameObjectHierarchy( taxi.Car )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				var oldRotation = taxi.Car.Transform.Rotation;

				taxi.Car.Transform.Position = trace.Hit ? trace.HitPosition - Vector3.Up * 25f : taxi.Car.Transform.Position;

				var travelDirection = (currentEndPosition - taxi.Car.Transform.Position).Normal;
				taxi.Car.Transform.Rotation = Rotation.LookAt( travelDirection, Vector3.Up );

				startPosition = taxi.Car.Transform.Position + Vector3.Up * 2000f;
				endPosition = taxi.Car.Transform.Position + Vector3.Down * 2000f;

				trace = Scene.Trace.Ray( startPosition, endPosition )
					.Size( 3f )
					.IgnoreGameObjectHierarchy( taxi.Car )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				taxi.Car.Transform.Position = trace.Hit ? trace.HitPosition - Vector3.Up * 25f : taxi.Car.Transform.Position;

				var front = taxi.Car.Transform.World.PointToWorld( Vector3.Forward * 50f );
				var frontTrace = Scene.Trace.Ray( front + Vector3.Up * 500f, front + Vector3.Down * 500f )
					.Size( 3f )
					.IgnoreGameObjectHierarchy( taxi.Car )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				var back = taxi.Car.Transform.World.PointToWorld( Vector3.Backward * 50f );
				var backTrace = Scene.Trace.Ray( back + Vector3.Up * 500f, back + Vector3.Down * 500f )
					.Size( 3f )
					.IgnoreGameObjectHierarchy( taxi.Car )
					.WithoutTags( "player", "trigger", "npc" )
					.Run();

				var desiredDirection = (frontTrace.Hit ? frontTrace.HitPosition : frontTrace.EndPosition) - (backTrace.Hit ? backTrace.HitPosition : backTrace.EndPosition);
				taxi.Car.Transform.Rotation = Rotation.LookAt( desiredDirection, Vector3.Up );
				taxi.Car.Transform.Rotation = Rotation.Lerp( oldRotation, taxi.Car.Transform.Rotation, Time.Delta * 6f );

				taxi.Car.Transform.Position += taxi.Car.Transform.Rotation.Forward * taxi.Speed * Time.Delta;

				taxi.Player.Components.Get<HealthComponent>().StunnedBy = DamageType.Nothing;
				taxi.Player.RagdollDisable = true;

				if ( taxi.Car.Transform.Position.Distance( currentEndPosition ) <= 150f )
				{
					taxi.CurrentWaypoint++;
				}

				if ( taxi.CurrentWaypoint >= 0 )
				{
					taxi.Player.Transform.Position = taxi.Car.Transform.Position + Vector3.Up * 100f;
					taxi.Player.BlockInputs = true;

					foreach ( var renderer in taxi.Player.Components.GetAll<SkinnedModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
						renderer.Enabled = false;

					foreach ( var renderer in taxi.Player.Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
						renderer.Enabled = false;

					taxi.Player.ForceHidePenoid = true;

					if ( taxi.Player.Components.TryGet<MoveHelper>( out var moveHelper ) )
					{
						moveHelper.Enabled = false;
					}
				}
			}
			else
			{
				taxi.Car.Destroy();
				toRemove.Add( taxi );

				taxi.Player.Transform.Position = taxi.To.TaxiPosition;
				taxi.Player.BlockInputs = false;

				foreach ( var renderer in taxi.Player.Components.GetAll<SkinnedModelRenderer>( FindMode.DisabledInSelfAndDescendants ) )
					renderer.Enabled = true;

				foreach ( var renderer in taxi.Player.Components.GetAll<ModelRenderer>( FindMode.DisabledInSelfAndDescendants ) )
					renderer.Enabled = true;

				taxi.Player.ForceHidePenoid = false;

				if ( taxi.Player.Components.TryGet<MoveHelper>( out var moveHelper, FindMode.EverythingInSelfAndChildren ) )
				{
					moveHelper.Enabled = true;
				}

				DisableRagdollAfter( taxi.Player );
			}
		}

		foreach ( var taxi in toRemove )
		{
			_activeTaxis.Remove( taxi );
		}
	}

	async void DisableRagdollAfter( Player player )
	{
		await Task.Delay( 1000 );

		player.RagdollDisable = false;
		player.Components.Get<HealthComponent>().StunnedBy = DamageType.Mild;
	}

	public BusStop FindBusStop( Stop type )
	{
		return Scene.GetAllComponents<BusStop>().Where( x => x.Type == type ).FirstOrDefault();
	}

	public void CreateCar( Player player, BusStop destination, float timeToTravel )
	{
		if ( _activeTaxis.Any( x => x.Player == player ) ) return;

		var randomDirection = Rotation.FromYaw( Game.Random.Float( 360f ) ).Forward;
		var randomPosition = TaxiPosition + randomDirection * 2000f;

		var startPos = randomPosition.WithZ( TaxiPosition.z + 2000f );
		var endPos = randomPosition.WithZ( TaxiPosition.z - 2000f );

		var groundTrace = Game.ActiveScene.Trace.Ray( startPos, endPos )
			.Size( 5f )
			.WithoutTags( "player", "npc", "trigger" )
			.Run();

		var car = Car.Clone( groundTrace.Hit ? groundTrace.HitPosition : destination.TaxiPosition, Transform.Rotation );
		car.NetworkMode = NetworkMode.Object;
		car.NetworkSpawn();

		var newTaxi = new Taxi( player, car, this, destination, timeToTravel );
		_activeTaxis.Add( newTaxi );

		if ( Game.Random.Float( 100 ) <= 40f )
		{
			if ( PrefabLibrary.TryGetByPath( "prefabs/npcs/elk_female_actor.prefab", out var prefab ) )
			{
				var deer = SceneUtility.GetPrefabScene( prefab.Prefab ).Clone();

				if ( deer != null )
				{
					deer.Transform.Position = _waypoints[4];
					deer.Components.Get<NPC>().NextIdle = 999f;
				}
			}
		}

		player.TakeMoney( Price );
	}
}
