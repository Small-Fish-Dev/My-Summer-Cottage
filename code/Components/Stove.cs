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

	public bool IsRunning { get; set; }

	protected override void OnStart()
	{
		Model = Components.Get<SkinnedModelRenderer>( FindMode.EnabledInSelfAndChildren );

		var interactions = Components.GetOrCreate<Interactions>();
		interactions.AddInteraction( new Interaction()
		{
			Identifier = $"stove.wood_put_in",
			Action = ( Player interactor, GameObject obj ) => PlaceWood( interactor ),
			Keybind = "mouse2",
			Description = "Insert split log",
			Disabled = () => HasWood || !Player.Local.Inventory.BackpackItems.Any( x => x.IsValid() && x.Name == "Split Log" ),
			InteractDistance = 120,
			ShowWhenDisabled = () => false,
			Accessibility = AccessibleFrom.World
		} );
	}

	protected override void OnFixedUpdate()
	{
		Model.Set( "b_open", !HasWood );

		GameObject.Name = $"Stove{(HasWater && HasWater ? " (Ready)" : (HasWater ? "" : $" (Needs{(HasWood ? "" : " wood and")} water)"))}";
	}

	void PlaceWood( Player interactor )
	{
		HasWood = true;
		interactor.Inventory.BackpackItems
			.Where( x => x.IsValid() && x.Name == "Split Log" )
			.FirstOrDefault()
			.Destroy();
	}
}
