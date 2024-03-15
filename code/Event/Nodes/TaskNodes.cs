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
		TaskMaster.AssignEveryoneNewTask( taskToAdd.ResourceId );
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
		TaskMaster.RemoveEveryoneTask( taskToRemove.ResourceId );
	}

	/// <summary>
	/// Reset a task on the player's current tasks
	/// </summary>
	/// <param name="taskToReset"></param>
	/// <param name="player"></param>
	[ActionGraphNode( "event.resettaskon" )]
	[Title( "Reset Task On" ), Group( "Events" ), Icon( "event_repeat" )]
	public static void ResetTaskOn( SaunaTask taskToReset, Player player )
	{
		if ( player != null )
			TaskMaster.ResetTask( taskToReset, player );
	}

	/// <summary>
	/// Reset a task on all of the players current tasks
	/// </summary>
	/// <param name="taskToReset"></param>
	[ActionGraphNode( "event.resettaskoneveryone" )]
	[Title( "Reset Task On Everyone" ), Group( "Events" ), Icon( "event_repeat" )]
	public static void ResetTaskOnEveryone( SaunaTask taskToReset )
	{
		TaskMaster.ResetEveryoneTask( taskToReset.ResourceId );
	}

	/// <summary>
	/// Get a random task with the given parameters
	/// </summary>
	/// <param name="taskType"></param>
	/// <param name="taskRarity"></param>
	[ActionGraphNode( "event.getrandomtask" )]
	[Title( "Get Random Task" ), Group( "Events" ), Icon( "calendar_month" )]
	public static SaunaTask GetRandomTask( TaskType taskType = TaskType.Any, TaskRarity taskRarity = TaskRarity.Any )
	{
		return SaunaTask.GetRandomTask( taskType, taskRarity );
	}
}
