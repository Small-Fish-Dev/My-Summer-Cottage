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
			Log.Info( task.Name );
		}
	}
}
