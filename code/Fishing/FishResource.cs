namespace Sauna.Fishing;

public class FishResource : GameResource
{
	public string Name;
	public string Description;
	public Model Model;

	public float MinimumWaterDepth = 10;
	public RangedFloat Weight = new RangedFloat( 1, 10 );
	public int CostPerKilo = 1;
}
