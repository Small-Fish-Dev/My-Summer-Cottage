namespace Sauna;

[Icon( "calendar_month" )]
public class TaskMaster : Component
{
	[Property]
	public List<SaunaTask> CurrentTasks { get; set; }

	protected override void OnUpdate()
	{
		foreach ( var task in CurrentTasks )
		{
			if ( !task.Completed )
			{
				var currentSubtasks = task.Subtasks.Where( x => x.SubtaskOrder == task.CurrentSubtaskOrder );

				foreach ( var subtask in currentSubtasks )
				{
					if ( subtask.EvaluateOnTick != null )
						subtask.CurrentAmount = subtask.EvaluateOnTick.Invoke( Player.Local );

					subtask.Completed = subtask.CurrentAmount >= subtask.AmountToComplete;
				}

				if ( task.Subtasks.All( x => x.Completed ) )
				{
					task.Completed = true;
					Log.Info( "TASK COMPLETED!!" );
				}
			}
		}
	}
}
