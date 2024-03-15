using Sandbox;
using Sauna;
using System.Xml.Linq;

public sealed class Stove : Component
{
	public SkinnedModelRenderer Model { get; set; }

	[Sync]
	public bool HasWood { get; set; } = false;

	[Sync]
	public bool HasWater { get; set; } = false;

	[Sync]
	public bool IsRunning { get; set; } = false;

	[Property]
	public EventAreaTrigger PlayersInArea { get; set; }

	[Property]
	public DoorComponent Door { get; set; }

	[Property]
	public GameObject FloorSteam { get; set; }

	[Property]
	public GameObject Steam { get; set; }

	[Property]
	public GameObject Light { get; set; }

	public TimeUntil StopWorking;

	protected override void OnStart()
	{
		GameObject.SetupNetworking();

		Model = Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndChildren );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = $"stove.wood_put_in",
			Action = ( Player interactor, GameObject obj ) => PlaceWood( interactor ),
			Keybind = "use2",
			Description = "Insert split log",
			Disabled = () => HasWood || !Player.Local.Inventory.BackpackItems.Any( x => x.IsValid() && x.Name == "Split Log" ),
			InteractDistance = 120,
			ShowWhenDisabled = () => false,
			Accessibility = AccessibleFrom.World
		} );

		interactions.AddInteraction( new Interaction()
		{
			Identifier = $"stove.begin_sauna",
			Action = ( Player interactor, GameObject obj ) => BeginSauna( interactor ),
			Keybind = "use",
			Description = "Begin sauna",
			Disabled = () => !CanSauna().Item1,
			DynamicText = () => SaunaDescription(),
			InteractDistance = 120,
			ShowWhenDisabled = () => true,
			Accessibility = AccessibleFrom.World
		} );
	}

	protected override void OnFixedUpdate()
	{
		Model.Set( "b_open", !HasWood );

		if ( Steam != null )
			Steam.Enabled = IsRunning;

		if ( Light != null )
			Light.Enabled = IsRunning;

		if ( FloorSteam != null )
			FloorSteam.Enabled = IsRunning && (Door.IsValid() && Door.State == DoorState.Close);

		GameObject.Name = $"Stove{(HasWater && HasWood && !IsRunning ? " (Ready)" : (HasWater ? "" : $" (Needs{(HasWood ? "" : " wood and")} water)"))}";

		if ( StopWorking && IsRunning )
		{
			HasWater = false;
			HasWood = false;
			IsRunning = false;
		}
	}

	void PlaceWood( Player interactor )
	{
		HasWood = true;
		interactor.Inventory.BackpackItems
			.Where( x => x.IsValid() && x.Name == "Split Log" )
			.FirstOrDefault()
			.Destroy();
	}

	(bool, string) CanSauna()
	{
		if ( IsRunning ) return (false, "Running...");
		if ( !HasWood ) return (false, "There is no wood!");
		if ( !HasWater ) return (false, "There is no water!");
		if ( Door != null && Door.State != DoorState.Close ) return (false, "The front door is open!");
		if ( PlayersInArea != null && PlayersInArea.ObjectsInside.Count < Player.All.Count() ) return (false, "Everyone must be inside!");
		if ( PlayersInArea != null )
		{
			foreach ( var obj in PlayersInArea.ObjectsInside )
			{
				if ( obj.Components.TryGet<Player>( out var player ) )
				{
					if ( player.HasShirt )
						return (false, "Everyone must take their shirt off!");
				}
			}
		}

		return (true, "Start the sauna.");
	}

	string SaunaDescription()
	{
		var canSauna = CanSauna();

		if ( !canSauna.Item1 )
			return canSauna.Item2;
		else
			return $"Start the sauna.";
	}

	void BeginSauna( Player interactor )
	{
		IsRunning = true;
		StopWorking = 120;
		BeginSaunaDelay( interactor );
	}

	async void BeginSaunaDelay( Player interactor )
	{
		await Task.Delay( 1000 );
		interactor.AddExperience( 100 );
	}
}
