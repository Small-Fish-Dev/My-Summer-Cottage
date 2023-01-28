namespace Sauna;

partial class Player : IInteractable
{
	string IInteractable.DisplayTitle => (Client)?.Name ?? "Unknown";

	public Player()
	{
		var interactable = this as IInteractable;

		interactable.AddInteraction( InputButton.Use, new()
		{
			Predicate = ( Player ply ) => true,
			Function = ( Player ply ) => Log.Error( "kisses u mwah" ),
			Text = "kiss 😘😘😚😚"
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

		var start = Position + Vector3.Up * CollisionBox.Maxs.z;
		var dir = ViewAngles.ToRotation().Forward;
		var trace = Trace.Ray( new Ray( start, dir ), InteractionDistance )
			.Ignore( this )
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
					if ( interactions.ContainsKey( button ) )
						break;
					else if ( info.Predicate( this ) )
					{
						interactions.Add( button, info );

						if ( Input.Pressed( button ) )
							info.Function( this );
					}
			}
		}
		else
			Interactable = null;
	}
}
