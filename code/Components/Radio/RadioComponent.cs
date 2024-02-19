namespace Sauna;

public sealed class RadioComponent : Component
{
	[Property] public bool On { get; set; }
	public string Title { get; set; } = "Testing";

	protected override void OnStart()
	{
		var interactions = Components.Create<Interactions>();

		// Toggle
		interactions.AddInteraction( new Interaction()
		{
			Action = ( Player interactor, GameObject obj ) =>
			{
				On = !On;
			},
			Keybind = "use",
			DynamicText = () => $"Toggle {(On ? "off" : "on")}",
		} );
	}
}
