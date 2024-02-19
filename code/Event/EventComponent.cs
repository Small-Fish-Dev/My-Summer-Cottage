using Sandbox;

[Icon( "event" )]
[EditorHandle( "/textures/gizmo/event.png" )]
public sealed class EventComponent : Component, Component.ExecuteInEditor
{
	protected override void DrawGizmos()
	{
		if ( Game.IsEditor )
		{
			foreach ( var component in Components.GetAll( FindMode.EverythingInSelfAndChildren ) )
			{
				if ( component != this )
					component.Enabled = GameManager.ActiveScene == GameObject || Gizmo.IsSelected;
			}
		}
	}

	protected override void OnUpdate()
	{

	}
}
