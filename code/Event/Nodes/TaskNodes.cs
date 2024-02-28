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

	/// <summary>
	/// Remove a task from the player's current tasks
	/// </summary>
	/// <param name="taskToRemove"></param>
	/// <param name="player"></param>
	[ActionGraphNode( "event.removetaskfrom" )]
	[Title( "Remove Task From" ), Group( "Events" ), Icon( "event_busy" )]
	public static void RemoveTaskFrom( SaunaTask taskToRemove, Player player )
	{
		if ( player != null )
			TaskMaster.RemoveTask( taskToRemove, player );
	}

	/// <summary>
	///Remove a task from all of the players current tasks
	/// </summary>
	/// <param name="taskToRemove"></param>
	[ActionGraphNode( "event.removetaskfromeveryone" )]
	[Title( "Remove Task From Everyone" ), Group( "Events" ), Icon( "event_busy" )]
	public static void RemoveTaskFromEveryone( SaunaTask taskToRemove )
	{
		TaskMaster.RemoveEveryoneTask( taskToRemove );
	}
}
