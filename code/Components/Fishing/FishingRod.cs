namespace Sauna;

public sealed class FishingRod : Component
{
	[Sync] public bool Casted { get; set; }

	protected override void OnStart()
	{
		var interactions = Components.GetOrCreate<Interactions>();

		interactions.AddInteraction( new Interaction
		{
			HoldOnly = true,
			Action = ( Player player, GameObject obj ) =>
			{
				Casted = !Casted;
			},
			DynamicText = () => Casted ? "Pull back" : "Cast",
			Keybind = "mouse1"
		} );
	}
}
