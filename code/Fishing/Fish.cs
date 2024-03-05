namespace Sauna.Fishing;

public class Fish : Component
{
	[Property] [Range( 0, 1 )] public float Rarity { get; set; }

	[Property] public float MinimumWaterDepth = 10;
	[Property] public RangedFloat Weight = new RangedFloat( 1, 10 );
	[Property] public int CostPerKilo = 1;
}
