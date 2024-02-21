using Sandbox;
using Sauna.Event;

[Icon( "event" )]
[EditorHandle( "/textures/gizmo/event.png" )]
[Category( "Events" )]
public sealed class EventComponent : Component, Component.ExecuteInEditor
{
	[Property]
	public List<EventTrigger> Triggers { get; set; }

	[Property]
	public Action<GameObject> Event { get; set; }

	bool _showToggle = false;

	protected override void DrawGizmos()
	{
		// It's done inside of here because this is when we can detect if it's been selected
		if ( Game.IsEditor )
		{
			var shouldShow = GameManager.ActiveScene == GameObject || Gizmo.IsSelected;
			// The components inside are enabled if you're inside of the prefab or you have the prefab selected

			if ( _showToggle != shouldShow )
			{
				foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
				{
					if ( component != this )
						component.Enabled = shouldShow;
				}

				_showToggle = shouldShow;
			}

		}
	}

	protected override void OnStart()
	{
		foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
		{
			if ( component != this )
				component.Enabled = true; // Make sure to enable back all the components in case they were disabled
		}

		foreach ( var trigger in Triggers )
		{
			trigger.OnTrigger += Event;
		}
	}

	protected override void OnUpdate()
	{

	}
}
