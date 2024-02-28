using Sandbox;
using Sandbox.Utility;
using Sauna;
using Sauna.Event;

public static partial class TaskNodes
{
	/// <summary>
	/// Add a task to the player's current tasks
	/// </summary>
	/// <param name="taskToAdd"></param>
	/// <param name="player"></param>
	[ActionGraphNode( "event.assigntaskto" )]
	[Title( "Assign New Task To" ), Group( "Events" ), Icon( "edit_calendar" )]
	public static void AssignTaskTo( SaunaTask taskToAdd, Player player )
	{
		if ( player != null )
			TaskMaster.AssignNewTask( taskToAdd, player );
	}

	/// <summary>
	/// Add a task to all of the players current tasks
	/// </summary>
	/// <param name="taskToAdd"></param>
	[ActionGraphNode( "event.assigntasktoeveryone" )]
	[Title( "Assign Everyone New Task" ), Group( "Events" ), Icon( "edit_calendar" )]
	public static void AssignTaskToEveryone( SaunaTask taskToAdd )
	{
		TaskMaster.AssignEveryoneNewTask( taskToAdd );
	}
}
