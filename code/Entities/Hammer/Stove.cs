namespace Sauna;

[HammerEntity]
[EditorModel( "models/stove/stove.vmdl" )]
public partial class Stove : AnimatedEntity, IInteractable
{
	/// <summary>
	/// Determines if the stove is open or not.
	/// </summary>
	[Net, Predicted]
	public bool Open { get; set; } = false;

	/// <summary>
	/// The temperature volume this stove is closest to.
	/// </summary>
	public TemperatureVolume Volume
	{
		get
		{
			if ( volume == null || !volume.IsValid() )
				volume = Entity.All
					.OfType<TemperatureVolume>()
					.FirstOrDefault( volume => volume.WorldSpaceBounds.Contains( Position ) );

			return volume;
		}
	}
	private TemperatureVolume volume;

	string IInteractable.DisplayTitle => "Kiuas";
	InteractionOffset IInteractable.Offset => Rotation.Backward * 15f + Vector3.Up * 30f;

	public Stove()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( "use", new()
		{
			Predicate = ( Player pawn ) => true,
			Function = ( Player pawn ) =>
			{
				Open = !Open;
				SetAnimParameter( "b_open", Open );

				if ( Game.IsServer )
					pawn.ProgressAchievement( AchievementId.SaunaFurnaceFirstTime );
			},
			TextFunction = () => Open
				? "Close"
				: "Open",
		} );
	}

	public override void Spawn()
	{
		SetModel( "models/stove/stove.vmdl" );
		SetupPhysicsFromModel( PhysicsMotionType.Static );

		Tags.Add( "solid" );
	}
}
