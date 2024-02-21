using Sandbox;
using Sauna.Event;

[Icon( "flash_on" )]
[Category( "Events" )]
public sealed class EventComponent : Component
{
	[Property]
	public List<EventTrigger> Triggers { get; set; }

	[Property]
	public Action<GameObject> Event { get; set; }


	protected override void OnStart()
	{
		foreach ( var trigger in Triggers )
			trigger.OnTrigger += Event;
	}

	protected override void OnUpdate()
	{

	}
}
