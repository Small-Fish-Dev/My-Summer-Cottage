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

	[Property]
	public EventAreaTrigger PlayersInArea { get; set; }

	[Property]
	public DoorComponent Door { get; set; }

	public bool IsRunning { get; set; }

	protected override void OnStart()
	{
		Model = Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndChildren );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = $"stove.wood_put_in",
			Action = ( Player interactor, GameObject obj ) => PlaceWood( interactor.ConnectionID ),
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
			Action = ( Player interactor, GameObject obj ) => BeginSauna( interactor.ConnectionID ),
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

		GameObject.Name = $"Stove{(HasWater && HasWood && !IsRunning ? " (Ready)" : (HasWater ? "" : $" (Needs{(HasWood ? "" : " wood and")} water)"))}";
	}

	[Broadcast( NetPermission.Anyone )]
	void PlaceWood( Guid interactor )
	{
		var player = Player.GetByID( interactor );

		if ( player.IsValid() )
		{
			HasWood = true;

			player.Inventory.BackpackItems
				.Where( x => x.IsValid() && x.Name == "Split Log" )
				.FirstOrDefault()
				.Destroy();
		}
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

	[Broadcast( NetPermission.Anyone )]
	void BeginSauna( Guid interactor )
	{
		var player = Player.GetByID( interactor );

		if ( player.IsValid() )
		{
			IsRunning = true;

			BeginSaunaDelay( player );
		}
	}

	async void BeginSaunaDelay( Player interactor )
	{
		await Task.Delay( 1000 );

		Player.Local.AddExperience( 100 );

		await Task.Delay( 10000 );

		IsRunning = false;
		HasWood = false;
		HasWood = false;
	}
}
