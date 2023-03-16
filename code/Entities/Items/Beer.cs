namespace Sauna;

public partial class Beer : BaseItem
{
	public override string Model => "models/beer_bottle/beer.vmdl";
	public override string Title => "Viipuri-olut";

	public Beer()
	{
		var interactable = this as IInteractable;
	}
}
