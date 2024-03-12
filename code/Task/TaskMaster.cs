using Microsoft.VisualBasic;
using Sauna.Event;
using Sauna.UI;

namespace Sauna;

[Icon( "live_help" )]
public class TaskMaster : Component
{
	private static TaskMaster _instance;

	public static IReadOnlyList<SaunaTask> ActiveTasks => _instance.CurrentTasks;

	[Property]
	public List<SaunaTask> CurrentTasks { get; set; }

	/// <summary>
	/// Get how many tasks the player has triggered so far (From 0 to 1) - Fetching this runs a check so don't overuse it
	/// </summary>
	public float TasksTriggered
	{
		get
		{
			var allTasks = TasksProgression.Tasks;
			float totalTasks = allTasks.Count();
			float triggeredTasks = allTasks.Where( x => x.TimesTriggered > 0 ).Count();

			return triggeredTasks / totalTasks;
		}
	}

	public static int IndexOf( SaunaTask task )
		=> _instance.CurrentTasks.IndexOf( task );

	/// <summary>
	/// Get how many tasks the player has completed so far (From 0 to 1) - Fetching this runs a check so don't overuse it
	/// </summary>
	public float TasksCompleted
	{
		get
		{
			var allTasks = TasksProgression.Tasks;
			float totalTasks = allTasks.Count();
			float triggeredTasks = allTasks.Where( x => x.TimesCompleted > 0 ).Count();

			return triggeredTasks / totalTasks;
		}
	}

	public class TaskCompletion
	{
		[JsonInclude]
		public string Task { get; set; }
		[JsonInclude]
		public int TimesTriggered { get; set; } = 0;
		[JsonInclude]
		public int TimesCompleted { get; set; } = 0;
		[JsonInclude]
		public bool CurrentlyActive { get; set; } = false;
		[JsonInclude]
		public int CurrentSubtaskOrder { get; set; } = 0;

		[JsonInclude]
		public Dictionary<string, int> Subtasks { get; set; } = new();

		public TaskCompletion( string task, int timesTriggered = 0, int timesCompleted = 0, bool currentlyActive = false, int currentSubtaskOrder = 0 )
		{
			Task = task;
			TimesTriggered = timesTriggered;
			TimesCompleted = timesCompleted;
			CurrentlyActive = currentlyActive;
			CurrentSubtaskOrder = currentSubtaskOrder;
		}
	}

	public struct SaunaTasksProgress
	{
		[JsonInclude]
		public List<TaskCompletion> Tasks = new();

		public SaunaTasksProgress() { }
	}

	public SaunaTasksProgress TasksProgression { get; private set; } = new();

	protected override void OnStart()
	{
		_instance = this;
		LoadTasksProgression();
	}
	protected override void OnDestroy()
	{
		var allTasks = ResourceLibrary.GetAll<SaunaTask>();

		foreach ( var task in allTasks )
		{
			task.Reset();
		}
	}

	public void AddTaskProgression( string taskPath, int timesTriggered = 0, int timesCompleted = 0 )
	{
		var newTaskCompletion = new TaskCompletion( taskPath, timesTriggered, timesCompleted );

		TasksProgression.Tasks.Add( newTaskCompletion );
	}

	/// <summary>
	/// Get the current stats on that task
	/// </summary>
	/// <param name="task"></param>
	/// <returns></returns>
	public TaskCompletion GetTaskProgression( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksProgression.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksProgression.Tasks.Where( x => x.Task == taskPath ).First();
			foundTaskCompletion.TimesTriggered++;

			return foundTaskCompletion;
		}
		else
		{
			var newTaskCompletion = new TaskCompletion( taskPath, 0, 0 );
			TasksProgression.Tasks.Add( newTaskCompletion );

			return newTaskCompletion;
		}
	}

	/// <summary>
	/// Update the tasks progression
	/// </summary>
	public void UpdateTaskProgression( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksProgression.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksProgression.Tasks.Where( x => x.Task == taskPath ).First();

			foreach ( var activeTask in CurrentTasks )
			{
				if ( activeTask.ResourcePath == taskPath && !task.Completed )
				{
					foundTaskCompletion.CurrentlyActive = true;
					foundTaskCompletion.CurrentSubtaskOrder = task.CurrentSubtaskOrder;
					foreach ( var subtask in task.Subtasks )
					{
						if ( foundTaskCompletion.Subtasks.ContainsKey( subtask.Description ) )
							foundTaskCompletion.Subtasks[subtask.Description] = subtask.CurrentAmount;
						else
							foundTaskCompletion.Subtasks.Add( subtask.Description, subtask.CurrentAmount );
					}
				}
			}

		}
		else
		{
			AddTaskProgression( taskPath );
		}
	}

	/// <summary>
	/// Increase that task's total triggered amount
	/// </summary>
	/// <param name="task"></param>
	public void TaskTriggered( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksProgression.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksProgression.Tasks.Where( x => x.Task == taskPath ).First();
			foundTaskCompletion.TimesTriggered++;
		}
		else
		{
			AddTaskProgression( taskPath, 1, 0 );
		}
	}

	/// <summary>
	/// Increase that task's total completed amount
	/// </summary>
	/// <param name="task"></param>
	public void TaskCompleted( SaunaTask task )
	{
		var taskPath = task.ResourcePath;
		var taskCompletionExists = TasksProgression.Tasks.Any( x => x.Task == taskPath );

		if ( taskCompletionExists )
		{
			var foundTaskCompletion = TasksProgression.Tasks.Where( x => x.Task == taskPath ).First();
			foundTaskCompletion.TimesCompleted++;
		}
		else
		{
			AddTaskProgression( taskPath, 0, 1 );
		}
	}

	public void LoadTasksProgression()
	{
		if ( FileSystem.Data.FileExists( "tasks.json" ) )
		{
			TasksProgression = FileSystem.Data.ReadJsonOrDefault<SaunaTasksProgress>( "tasks.json" );

			var activeTasks = TasksProgression.Tasks.Where( x => x.CurrentlyActive );

			foreach ( var activeTask in activeTasks )
			{
				var assignedTask = AssignNewTask( activeTask.Task );
				assignedTask.Started = !assignedTask.RunOnStartEverySession;
				assignedTask.CurrentSubtaskOrder = activeTask.CurrentSubtaskOrder;

				foreach ( var subtask in assignedTask.Subtasks )
				{
					var relativeSubtask = activeTask.Subtasks[subtask.Description];
					subtask.CurrentAmount = relativeSubtask;
				}
			}
		}
		else
		{
			TasksProgression.Tasks?.Clear();

			var allTasks = ResourceLibrary.GetAll<SaunaTask>();

			foreach ( var task in allTasks )
				AddTaskProgression( task.ResourcePath );

			SaveTasksProgression();
		}
	}

	/// <summary>
	/// Save the tasks current triggered and completion progress/amount
	/// </summary>
	public void SaveTasksProgression()
	{
		var allTasks = ResourceLibrary.GetAll<SaunaTask>();

		// If future updates contain new tasks or we're live adding newer ones, save those to the file too
		foreach ( var task in allTasks )
			if ( !TasksProgression.Tasks.Any( x => x.Task == task.ResourcePath ) )
				AddTaskProgression( task.ResourcePath );

		foreach ( var task in CurrentTasks )
		{
			if ( task.PersistThroughSessions )
				UpdateTaskProgression( task );
		}

		FileSystem.Data.WriteJson( "tasks.json", TasksProgression );
	}

	/// <summary>
	/// Reset the tasks progress
	/// </summary>
	public void ResetTasksProgression( bool save = true )
	{
		TasksProgression.Tasks?.Clear();

		var allTasks = ResourceLibrary.GetAll<SaunaTask>();

		foreach ( var task in allTasks )
			AddTaskProgression( task.ResourcePath );

		if ( save )
			SaveTasksProgression();
	}


	protected override void OnFixedUpdate()
	{
		foreach ( var task in CurrentTasks )
		{
			if ( !task.Started ) // Start the task if it has been added
				task.Start();

			if ( !task.Completed )
			{
				var activeSubtasks = task.ActiveSubtasks;

				// Check if the subtasks have met the conditions (Previous subtask orders will always remain completed)
				foreach ( var subtask in activeSubtasks )
				{
					if ( subtask.EvaluateOnTick != null ) // Manually set current completion progress if we have an EvaluateOnTick
						if ( Player.Local != null )
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
		Log.Info( signalIdentifier );
		if ( _instance != null )
		{
			var allActiveTasks = _instance.CurrentTasks.Where( x => !x.Completed ); // Get active tasks

			foreach ( var task in allActiveTasks )
			{
				if ( !task.Global && triggerer != Player.Local ) continue; // If this task isn't global and the triggerer isn't our player, ignore it

				var currentOrder = task.CurrentSubtaskOrder;
				var activeSubtasks = task.Subtasks
					.Where( x => x.SubtaskOrder == currentOrder ); // Current active subtasks

				foreach ( var subtask in activeSubtasks )
				{
					if ( subtask.TriggerSignal.Identifier == signalIdentifier ) // If the given signal is the one we're looking for, increase the subtask's progress
						subtask.CurrentAmount++;
				}
			}
		}

		var storyMaster = Game.ActiveScene.GetAllComponents<StoryMaster>().FirstOrDefault(); // Find the story master

		if ( storyMaster != null )
		{
			var allActiveScriptedEvents = storyMaster.CurrentSaunaDay.ScriptedEvents.Where( x => !x.Completed ); // Get all active scripted events

			foreach ( var scriptedEvent in allActiveScriptedEvents )
			{
				if ( scriptedEvent.SignalToComplete == signalIdentifier )
					scriptedEvent.Completed = true;
			}
		}

	}

	/// <summary>
	/// Assign a new task to the local player, doesn't work if the task already exists
	/// </summary>
	/// <param name="taskToAssign"></param>
	public static SaunaTask AssignNewTask( SaunaTask taskToAssign )
	{
		if ( _instance == null ) return null;

		var sameTaskFound = _instance.CurrentTasks.Where( x => x.ResourceName == taskToAssign.ResourceName )?.Any() ?? false;

		if ( sameTaskFound ) return _instance.CurrentTasks.Where( x => x.ResourceName == taskToAssign.ResourceName ).First(); // Bail if we have the same task already

		_instance.CurrentTasks.Add( taskToAssign ); // Add the task

		NotificationManager.Popup( taskToAssign );
		if ( PrimaryTask.Pinned is null && taskToAssign.IsPrimary )
			PrimaryTask.PinTask( taskToAssign );

		return taskToAssign;
	}

	/// <summary>
	/// Assign a new task to the local player, doesn't work if the task already exists
	/// </summary>
	/// <param name="filePath"></param>
	public static SaunaTask AssignNewTask( string filePath )
	{
		if ( ResourceLibrary.TryGet<SaunaTask>( filePath, out var foundTask ) )
			return AssignNewTask( foundTask );

		return null;
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
		if ( _instance == null ) return;

		var sameTaskFound = _instance.CurrentTasks.Where( x => x.ResourceName == taskToRemove.ResourceName )?.FirstOrDefault() ?? null;

		if ( sameTaskFound == null ) return; // Bail if no task found

		_instance.CurrentTasks.Remove( sameTaskFound ); // Remove the task
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
		if ( _instance == null ) return;

		var sameTaskFound = _instance.CurrentTasks.Where( x => x.ResourceName == taskToReset.ResourceName )?.FirstOrDefault() ?? null;

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
