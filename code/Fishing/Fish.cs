namespace Sauna;

public class Fish : Component
{
	[Property][Range( 0, 1 )] public float Rarity { get; set; }
	[Property] public bool IsTrash { get; set; }

	[Property] public float MinimumWaterDepth { get; set; } = 10;

	/// <summary>
	/// The fish species' weight range in grams.
	/// </summary>
	[Property, HideIf( "IsTrash", true )] public RangedFloat WeightRange { get; set; } = new RangedFloat( 100, 10000 );
	[Property, HideIf( "IsTrash", true )] public int CostPerKilo { get; set; } = 1;
	[Sync, TargetSave] public int WeightInGrams
	{
		get => Components.Get<ItemComponent>( FindMode.EverythingInSelfAndParent )?.WeightInGrams ?? (int)WeightRange.x;
		set
		{
			var item = Components.Get<ItemComponent>( FindMode.EverythingInSelfAndParent );
			if ( item != null )
			{
				item.WeightInGrams = value;
				item.SellPrice = (int)(value / 1000f * CostPerKilo + 0.5f);
			}
		}
	}

	/// <summary>
	/// Assigns a weight to the fishs' ItemComponent in grams.
	/// Also updates the sell price based on CostPerKilo.
	/// </summary>
	/// <param name="weight"></param>
	public void AssignWeight( int weight )
	{
		if ( IsTrash )
			return;

		WeightInGrams = weight;
	}
}
