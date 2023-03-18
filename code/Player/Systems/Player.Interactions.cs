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
				Eventlogger.Send( To.Multiple( new[] { ply.Client, Client } ), new Eventlogger.Component[] 
				{
					new ( $"{ply.Client.Name} destroyed {Client.Name}!" )
				} );

				if ( Game.IsServer && Effects.Get<Unconscious>() == null )
				{
					var effect = Effects.Apply<Unconscious>( 2f );
					effect.Force = 1000f * (Position - ply.Position).Normal;
				}
			},
			Text = "Destroy 👿"
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

	private void interactionSimulate( IClient cl )
	{
		// Don't keep on going if the player is ragdolled.
		if ( Ragdoll != null && Ragdoll.IsValid )
			return;

		if ( !Game.IsServer && cl != Game.LocalClient )
			return;

		var trace = Trace.Ray( ViewRay, InteractionDistance )
			.Ignore( this )
			.WithoutTags( "trigger" )
			.Radius( InteractionRadius )
			.RunAll()
			?.FirstOrDefault( trace => trace.Entity is IInteractable interactable && interactable.Enabled );

		interactions.Clear();
		if ( trace?.Entity is IInteractable interactable )
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
						{
							SetAnimParameter( "use", true );
							info.Function( this );

							// Don't let anything interfere with the interaction.
							Input.ClearButton( button );
						}

						break;
					}
			}
		}
		else
			Interactable = null;
	}
}
