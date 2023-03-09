namespace Sauna;

partial class Player : IInteractable
{
	InteractionOffset IInteractable.Offset => (GetEyePosition() - Position).z * Vector3.Up + Camera.Rotation.Right * 15f;
	string IInteractable.DisplayTitle => (Client)?.Name ?? "Unknown";

	public Player()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player ply ) => true,
			Function = ( Player ply ) =>
			{
				Subtitles.Send(
					To.Multiple( new[] { ply.Client, Client } ),
					$"{ply.Client.Name} kisses {Client.Name}",
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
	public float InteractionRadius => 7.5f;

	protected void InteractionSimulate( IClient cl )
	{
		if ( !Game.IsServer && cl != Game.LocalClient )
			return;

		var trace = Trace.Ray( ViewRay, InteractionDistance )
			.Ignore( this )
			.WithoutTags( "trigger" )
			.Radius( InteractionRadius )
			.RunAll()
			.FirstOrDefault( trace => trace.Entity is IInteractable interactable && interactable.Enabled );

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
