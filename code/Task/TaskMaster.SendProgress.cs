namespace Sauna;

// TODO: Clean this up in the future.
// We want to send down the active task data of the host to the clients.
public partial class TaskMaster
{
	public struct TaskProgress
	{
		[JsonInclude]
		public int ResourceId;
		[JsonInclude]
		public List<SubtaskProgress> Subtasks;
	}

	public struct SubtaskProgress
	{
		[JsonInclude]
		public string Description;
		[JsonInclude]
		public int CurrentAmount;
	}

	void INetworkListener.OnActive( Connection connection )
	{
		if ( Connection.Local.IsHost )
			_instance.SendCurrentTaskProgress( PackageTaskProgress( ActiveTasks ).Serialize() );
	}

	[Broadcast( NetPermission.HostOnly )]
	private void SendCurrentTaskProgress( byte[] data )
	{
		if ( Connection.Local.IsHost )
			return;

		var res = data.Deserialize<List<TaskProgress>>();

		foreach ( var taskProgress in res )
		{
			var taskToUpdate = CurrentTasks.FirstOrDefault( ( i ) => i.ResourceId == taskProgress.ResourceId );
			if ( taskToUpdate is null )
				continue;

			for ( int i = 0; i < taskToUpdate.Subtasks.Count; ++i )
			{
				taskToUpdate.Subtasks[i].CurrentAmount = taskProgress.Subtasks[i].CurrentAmount;
			}
		}
	}

	private static List<TaskProgress> PackageTaskProgress( IReadOnlyList<SaunaTask> tasks )
	{
		var data = new List<TaskProgress>();

		foreach ( var task in tasks )
		{
			var subtaskProgress = new List<SubtaskProgress>();
			foreach ( var subtask in task.Subtasks )
				subtaskProgress.Add( new SubtaskProgress() { Description = subtask.Description, CurrentAmount = subtask.CurrentAmount } );

			data.Add
			(
				new TaskProgress()
				{
					ResourceId = task.ResourceId,
					Subtasks = subtaskProgress
				}
			);
		}

		return data;
	}
}
