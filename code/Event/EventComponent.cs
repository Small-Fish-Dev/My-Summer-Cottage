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

	[Property]
	[Description( "This event component will only trigger once" )]
	public bool TriggerOnce { get; set; } = true;

	bool _triggered = false;

	/// <summary>
	/// Has this event component been triggered?
	/// </summary>
	public bool Triggered
	{
		get => _triggered;
		set
		{
			if ( TriggerOnce )
			{
				if ( value == true && Triggered == false )
				{
					foreach ( var trigger in Triggers )
					{
						trigger.OnTrigger -= Event;
						trigger.OnTrigger -= HasBeenTriggered;
					}
				}

				if ( value == false && Triggered == true )
				{
					foreach ( var trigger in Triggers )
					{
						trigger.OnTrigger += Event;
						trigger.OnTrigger += HasBeenTriggered;
					}
				}
			}

			_triggered = value;
		}
	}


	protected override void OnStart()
	{
		foreach ( var trigger in Triggers )
		{
			trigger.OnTrigger += Event;
			trigger.OnTrigger += HasBeenTriggered;
		}
	}

	void HasBeenTriggered( GameObject _ )
	{
		if ( TriggerOnce )
			Triggered = true;
	}

	protected override void OnEnabled()
	{
		if ( Triggered )
			Triggered = false; // Reset if it's reenabled
	}

	protected override void OnUpdate()
	{
	}
}
