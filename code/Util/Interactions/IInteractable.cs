namespace Sauna;

public interface IInteractable
{
	private readonly static Dictionary<IInteractable, Dictionary<InputButton, List<InteractionInfo>>> all = new();

	/// <summary>
	/// The displayed title of this interactable.
	/// </summary>
	public string DisplayTitle => "";

	/// <summary>
	/// The offset of this interactable's interaction hint.
	/// </summary>
	public InteractionOffset Offset => null;

	/// <summary>
	/// All interactions of this interactable
	/// </summary>
	public Dictionary<InputButton, List<InteractionInfo>> All => Get( this );

	/// <summary>
	/// Get all the interactions for an IInteractable.
	/// </summary>
	/// <param name="interactable"></param>
	/// <returns></returns>
	public static Dictionary<InputButton, List<InteractionInfo>> Get( IInteractable interactable )
	{
		if ( !all.TryGetValue( interactable, out var interactions ) )
			all.Add( interactable, interactions = new Dictionary<InputButton, List<InteractionInfo>>() );

		return interactions;
	}

	/// <summary>
	/// Adds a new interaction that is bound to a specific InputButton.
	/// </summary>
	/// <param name="button"></param>
	/// <param name="info"></param>
	public void AddInteraction( InputButton button, InteractionInfo info )
	{
		if ( !All.TryGetValue( button, out var interactions ) )
			All.Add( button, interactions = new List<InteractionInfo>() );

		interactions.Add( info );
	}
}
