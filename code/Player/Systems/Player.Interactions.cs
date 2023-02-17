namespace Sauna;

partial class Player : IInteractable
{
	InteractionOffset IInteractable.Offset => EyePosition + Camera.Rotation.Right * 25f;
	string IInteractable.DisplayTitle => (Client)?.Name ?? "Unknown";

	public Player()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player ply ) => true,
			Function = ( Player ply ) =>
			{
				using ( Prediction.Off() )
					Subtitles.Send(
						To.Multiple( new[] { ply.Client, Client } ),
						$"{ply.Client.Name} kisses {interactable.DisplayTitle}",
						wrapper: '*'
					);
			},
			Text = "Kiss 😘"
		} );
	}

	private Dictionary<InputButton, InteractionInfo> interactions = new();

	/// <summary>
	/// List of the current interactions available to the player.
	/// </summary>
	public IReadOnlyDictionary<InputButton, InteractionInfo> Interactions => interactions;

	/// <summary>
	/// The current available interactable.
	/// </summary>
	public IInteractable? Interactable { get; private set; }

	/// <summary>
	/// The distance that the player can interact from.
	/// </summary>
	public float InteractionDistance => 75f;

	/// <summary>
	/// The radius of the trace that is used for getting player's interactions.
	/// </summary>
	public float InteractionRadius => 15f;

	protected void InteractionSimulate( IClient cl )
	{
		if ( !Game.IsServer && cl != Game.LocalClient )
			return;

		var start = EyePosition;
		var dir = ViewAngles.ToRotation().Forward;
		var trace = Trace.Ray( new Ray( start, dir ), InteractionDistance )
			.Ignore( this )
			.WithoutTags( "trigger" )
			.Radius( InteractionRadius )
			.Run();

		interactions.Clear();
		if ( trace.Entity is IInteractable interactable )
		{
			Interactable = interactable;

			foreach ( var interaction in interactable.All )
			{
				var button = interaction.Key;
				var list = interaction.Value;

				foreach ( var info in list )
					if ( !info.Equals( default( InteractionInfo ) ) 
						&& info.Predicate( this ) )
					{
						interactions.Add( button, info );

						if ( Input.Pressed( button ) )
							info.Function( this );

						break;
					}
			}
		}
		else
			Interactable = null;
	}
}
