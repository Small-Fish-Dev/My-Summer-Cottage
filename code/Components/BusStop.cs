using Sandbox;
namespace Sauna;

public sealed class BusStop : Component
{
	public enum Stop
	{
		Suburbs,
		City
	}

	[Property]
	public Stop Type { get; set; } = Stop.Suburbs;

	[Property]
	public GameObject Car { get; set; }

	public struct Taxi
	{
		public Player Player;
		public GameObject Car;
		public BusStop From;
		public BusStop To;
		public TimeUntil Arrival;

		public Taxi() { }
		public Taxi( Player player, GameObject car, BusStop from, BusStop to, float timeToTravel )
		{
			Player = player;
			Car = car;
			From = from;
			To = to;
			Arrival = timeToTravel;
		}
	}

	[Sync]
	public NetList<Taxi> Taxis { get; set; } = new();

	protected override void OnFixedUpdate()
	{

	}

	// TODO Network?
	public void CreateCar( Player player, BusStop destination, float timeToTravel )
	{
		var car = Car.Clone( Transform.Position, Transform.Rotation );
		car.NetworkMode = NetworkMode.Object;
		car.NetworkSpawn();

		var newTaxi = new Taxi( player, car, this, destination, timeToTravel );
		Taxis.Add( newTaxi );
	}
}
