using Sandbox;

[Icon( "event" )]
[EditorHandle( "/textures/gizmo/event.png" )]
[Category( "Events" )]
public sealed class EventComponent : Component, Component.ExecuteInEditor
{
	[Property]
	public List<EventTrigger> Triggers { get; set; }

	[Property]
	public Action<GameObject> Event { get; set; }

	protected override void DrawGizmos()
	{
		if ( Game.IsEditor )
		{
			var shouldShow = GameManager.ActiveScene == GameObject || Gizmo.IsSelected;
			// The components inside are enabled if you're inside of the prefab or you have the prefab selected

			foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( component != this )
					component.Enabled = shouldShow;
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
