using Sandbox;
using System.Threading.Tasks;

namespace Sauna;

[Icon( "calendar_month" )]
public class TaskMaster : Component
{
	[Property]
	public List<SaunaTask> CurrentTasks { get; set; }

	// TODO: THIS BUT FOR EVENTS

	public class TaskCompletion
	{
		[JsonInclude]
		public string Task { get; set; }
		[JsonInclude]
		public int TimesTriggered { get; set; }
		[JsonInclude]
		public int TimesCompleted { get; set; }

		public TaskCompletion( string task, int timesTriggered = 0, int timesCompleted = 0 )
		{
			Task = task;
			TimesTriggered = timesTriggered;
			TimesCompleted = timesCompleted;
		}
	}

	public struct SaunaTasksProgress
	{
		[JsonInclude]
		public List<TaskCompletion> Tasks = new();

		public SaunaTasksProgress() { }
	}

	public SaunaTasksProgress TasksCompletion { get; private set; } = new();

	protected override void OnStart()
	{
		LoadTasksCompletion();
	}

	internal void AddTaskCompletion( string taskPath, int timesTriggered = 0, int timesCompleted = 0 )
	{
		var newTaskCompletion = new TaskCompletion( taskPath, timesTriggered, timesCompleted );
		TasksCompletion.Tasks.Add( newTaskCompletion );
	}

	internal TaskCompletion InternalGetTaskCompletion( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksCompletion.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksCompletion.Tasks.Where( x => x.Task == taskPath ).First();
			foundTaskCompletion.TimesTriggered++;

			return foundTaskCompletion;
		}
		else
		{
			var newTaskCompletion = new TaskCompletion( taskPath, 0, 0 );
			TasksCompletion.Tasks.Add( newTaskCompletion );

			return newTaskCompletion;
		}
	}

	/// <summary>
	/// Get the current stats on that task
	/// </summary>
	/// <param name="task"></param>
	/// <returns></returns>
	public static TaskCompletion GetTaskCompletion( SaunaTask task )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return null;

		return taskMaster.InternalGetTaskCompletion( task );
	}

	internal void InternalTaskTriggered( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksCompletion.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksCompletion.Tasks.Where( x => x.Task == taskPath ).First();
			foundTaskCompletion.TimesTriggered++;
		}
		else
		{
			AddTaskCompletion( taskPath, 1, 0 );
		}
	}

	/// <summary>
	/// Increase that task's total triggered amount
	/// </summary>
	/// <param name="task"></param>
	public static void TaskTriggered( SaunaTask task )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		taskMaster.InternalTaskTriggered( task );
	}

	internal void InternalTaskCompleted( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksCompletion.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksCompletion.Tasks.Where( x => x.Task == taskPath ).First();
			foundTaskCompletion.TimesCompleted++;
		}
		else
		{
			AddTaskCompletion( taskPath, 0, 1 );
		}

	}

	/// <summary>
	/// Increase that task's total completed amount
	/// </summary>
	/// <param name="task"></param>
	public static void TaskCompleted( SaunaTask task )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		taskMaster.InternalTaskCompleted( task );
	}

	public void LoadTasksCompletion()
	{
		if ( FileSystem.Data.FileExists( "tasks.json" ) )
			TasksCompletion = FileSystem.Data.ReadJsonOrDefault<SaunaTasksProgress>( "tasks.json" );
		else
		{
			TasksCompletion = new();

			var allTasks = ResourceLibrary.GetAll<SaunaTask>();

			foreach ( var task in allTasks )
				AddTaskCompletion( task.ResourcePath );

			InternalSaveTasksCompletion();
		}
	}

	internal void InternalSaveTasksCompletion()
	{
		FileSystem.Data.WriteJson( "tasks.json", TasksCompletion );
	}

	/// <summary>
	/// Save the tasks current triggered and completion progress/amount
	/// </summary>
	public static void SaveTasksCompletion()
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		taskMaster.InternalSaveTasksCompletion();
	}

	protected override void OnFixedUpdate()
	{
		foreach ( var task in CurrentTasks )
		{
			if ( !task.Started ) // Start the task if it has been added
				task.Start();

			if ( !task.Completed )
			{
				var activeSubtasks = task.Subtasks.Where( x => x.SubtaskOrder == task.CurrentSubtaskOrder ); // Current active subtasks

				// Check if the subtasks have met the conditions (Previous subtask orders will always remain completed)
				foreach ( var subtask in activeSubtasks )
				{
					if ( subtask.EvaluateOnTick != null ) // Manually set current completion progress if we have an EvaluateOnTick
						subtask.CurrentAmount = subtask.EvaluateOnTick.Invoke( Player.Local );

					subtask.SetComplete( subtask.CurrentAmount >= subtask.AmountToComplete );
				}

				// If all subtasks have been completed, the task has been completed succesfully
				if ( task.Subtasks.All( x => x.Completed ) )
					task.Succeed();

				// Check for the fail condition every tick
				if ( task.FailConditionCheck != null )
				{
					var hasFailed = task.FailConditionCheck.Invoke( Player.Local );

					if ( hasFailed ) // Fail the task if the fail condition has been met
						task.Fail();
				}

				// Time limit fail condition
				if ( task.TimeLimited && task.TaskTimer )
					task.Fail();

				if ( activeSubtasks.All( x => x.Completed ) )
					task.CurrentSubtaskOrder++;
			}
		}
	}

	/// <summary>
	/// Let all the tasks know a trigger has been activated
	/// </summary>
	/// <param name="signalIdentifier"></param>
	/// <param name="triggerer"></param>
	public static void SubmitTriggerSignal( string signalIdentifier, Player triggerer )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		var allActiveTasks = taskMaster.CurrentTasks.Where( x => !x.Completed ); // Get active tasks

		foreach ( var task in allActiveTasks )
		{
			if ( !task.Global && triggerer != Player.Local ) continue; // If this task isn't global and the triggerer isn't our player, ignore it

			var activeSubtasks = task.Subtasks; // Current active subtasks

			foreach ( var subtask in activeSubtasks )
			{
				if ( subtask.TriggerSignal == signalIdentifier ) // If the given signal is the one we're looking for, increase the subtask's progress
					subtask.CurrentAmount++;
			}
		}
	}

	/// <summary>
	/// Assign a new task to the local player, doesn't work if the task already exists
	/// </summary>
	/// <param name="taskToAssign"></param>
	public static void AssignNewTask( SaunaTask taskToAssign )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		var sameTaskFound = taskMaster.CurrentTasks.Where( x => x.ResourceName == taskToAssign.ResourceName )?.Any() ?? false;

		if ( sameTaskFound ) return; // Bail if we have the same task already

		taskMaster.CurrentTasks.Add( taskToAssign ); // Add the task
	}

	/// <summary>
	/// Assign a new task to the local player, doesn't work if the task already exists
	/// </summary>
	/// <param name="filePath"></param>
	public static void AssignNewTask( string filePath )
	{
		if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
			AssignNewTask( foundTask );
	}

	[Broadcast( NetPermission.Anyone )]
	internal static void InternalAssignNewTask( SaunaTask taskToAssign, Guid playerId )
	{
		var player = Player.GetByID( playerId );

		if ( player != null )
			if ( Player.Local == player )
				AssignNewTask( taskToAssign );
	}

	[Broadcast( NetPermission.Anyone )]
	internal static void InternalAssignNewTask( string filePath, Guid playerId )
	{
		var player = Player.GetByID( playerId );

		if ( player != null )
			if ( Player.Local == player )
				if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
					AssignNewTask( foundTask );
	}

	/// <summary>
	/// Assign a new task to the specified player, doesn't work if the task already exists
	/// </summary>
	/// <param name="taskToAssign"></param>
	/// <param name="player"></param>
	public static void AssignNewTask( SaunaTask taskToAssign, Player player ) => InternalAssignNewTask( taskToAssign, player.ConnectionID );

	/// <summary>
	/// Assign a new task to the specified player, doesn't work if the task already exists
	/// </summary>
	/// <param name="filePath"></param>
	/// <param name="player"></param>
	public static void AssignNewTask( string filePath, Player player ) => InternalAssignNewTask( filePath, player.ConnectionID );

	/// <summary>
	/// Assign everyone in the server a new task
	/// </summary>
	/// <param name="taskToAssign"></param>
	[Broadcast( NetPermission.Anyone )]
	public static void AssignEveryoneNewTask( SaunaTask taskToAssign ) => AssignNewTask( taskToAssign );

	/// <summary>
	/// Assign everyone in the server a new task
	/// </summary>
	/// <param name="filePath"></param>
	[Broadcast( NetPermission.Anyone )]
	public static void AssignEveryoneNewTask( string filePath ) => AssignNewTask( filePath );

	/// <summary>
	/// Removes the found task from the local player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="taskToRemove"></param>
	public static void RemoveTask( SaunaTask taskToRemove )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		var sameTaskFound = taskMaster.CurrentTasks.Where( x => x.ResourceName == taskToRemove.ResourceName )?.FirstOrDefault() ?? null;

		if ( sameTaskFound == null ) return; // Bail if no task found

		taskMaster.CurrentTasks.Remove( sameTaskFound ); // Remove the task
	}

	/// <summary>
	/// Removes the found task from the local player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="filePath"></param>
	public static void RemoveTask( string filePath )
	{
		if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
			RemoveTask( foundTask );
	}

	[Broadcast( NetPermission.Anyone )]
	internal static void InternalRemoveTask( SaunaTask taskToRemove, Guid playerId )
	{
		var player = Player.GetByID( playerId );

		if ( player != null )
			if ( Player.Local == player )
				RemoveTask( taskToRemove );
	}

	[Broadcast( NetPermission.Anyone )]
	internal static void InternalRemoveTask( string filePath, Guid playerId )
	{
		var player = Player.GetByID( playerId );

		if ( player != null )
			if ( Player.Local == player )
				if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
					RemoveTask( foundTask );
	}

	/// <summary>
	/// Remove the task from the targetted player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="taskToRemove"></param>
	/// <param name="player"></param>
	public static void RemoveTask( SaunaTask taskToRemove, Player player ) => InternalRemoveTask( taskToRemove, player.ConnectionID );

	/// <summary>
	/// Remove the task from the targetted player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="filePath"></param>
	/// <param name="player"></param>
	public static void RemoveTask( string filePath, Player player ) => InternalRemoveTask( filePath, player.ConnectionID );

	/// <summary>
	/// Remove the task from everyone in the server
	/// </summary>
	/// <param name="taskToRemove"></param>
	[Broadcast( NetPermission.Anyone )]
	public static void RemoveEveryoneTask( SaunaTask taskToRemove ) => RemoveTask( taskToRemove );

	/// <summary>
	/// Remove the task from everyone in the server
	/// </summary>
	/// <param name="filePath"></param>
	[Broadcast( NetPermission.Anyone )]
	public static void RemoveEveryoneTask( string filePath ) => RemoveTask( filePath );

	/// <summary>
	/// Resets the found task for the local player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="taskToReset"></param>
	public static void ResetTask( SaunaTask taskToReset )
	{
		var taskMaster = GameManager.ActiveScene.GetAllComponents<TaskMaster>().FirstOrDefault(); // Find the task master

		if ( taskMaster == null ) return;

		var sameTaskFound = taskMaster.CurrentTasks.Where( x => x.ResourceName == taskToReset.ResourceName )?.FirstOrDefault() ?? null;

		if ( sameTaskFound == null ) return; // Bail if no task found

		sameTaskFound.Reset(); // Restart the task
	}

	/// <summary>
	/// Resets the found task for the local player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="filePath"></param>
	public static void ResetTask( string filePath )
	{
		if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
			ResetTask( foundTask );
	}

	[Broadcast( NetPermission.Anyone )]
	internal static void InternalResetTask( SaunaTask taskToReset, Guid playerId )
	{
		var player = Player.GetByID( playerId );

		if ( player != null )
			if ( Player.Local == player )
				ResetTask( taskToReset );
	}

	[Broadcast( NetPermission.Anyone )]
	internal static void InternalResetTask( string filePath, Guid playerId )
	{
		var player = Player.GetByID( playerId );

		if ( player != null )
			if ( Player.Local == player )
				if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
					ResetTask( foundTask );
	}

	/// <summary>
	/// Resets the found task for the targetted player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="taskToReset"></param>
	/// <param name="player"></param>
	public static void ResetTask( SaunaTask taskToReset, Player player ) => InternalResetTask( taskToReset, player.ConnectionID );

	/// <summary>
	/// Resets the found task for the targetted player, doesn't work if the task doesn't exists
	/// </summary>
	/// <param name="filePath"></param>
	/// <param name="player"></param>
	public static void ResetTask( string filePath, Player player ) => InternalResetTask( filePath, player.ConnectionID );

	/// <summary>
	/// Resets the task for everyone in the server
	/// </summary>
	/// <param name="taskToReset"></param>
	[Broadcast( NetPermission.Anyone )]
	public static void ResetEveryoneTask( SaunaTask taskToReset ) => ResetTask( taskToReset );

	/// <summary>
	/// Resets the task for everyone in the server
	/// </summary>
	/// <param name="filePath"></param>
	[Broadcast( NetPermission.Anyone )]
	public static void ResetEveryoneTask( string filePath ) => ResetTask( filePath );
}
