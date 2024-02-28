namespace Sauna;

[Icon( "calendar_month" )]
public class TaskMaster : Component
{
	[Property]
	public List<SaunaTask> CurrentTasks { get; set; }

	protected override void OnStart()
	{
		foreach ( var task in CurrentTasks ) // Tasks persist between sessions?
			task.Reset();
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
}
