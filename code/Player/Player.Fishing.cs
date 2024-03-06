namespace Sauna;

public struct FishRecord
{
	/// <summary>
	/// How many fishes of this kind have been caught
	/// </summary>
	[JsonInclude] public int Count;

	/// <summary>
	/// Weight in grams
	/// </summary>
	[JsonInclude] public int MaxWeight;

	/// <summary>
	/// UTC Date
	/// </summary>
	[JsonInclude] public DateTime MaxWhen;
}

public partial class Player
{
	public Dictionary<string, FishRecord> FishesCaught = new();

	public void OnFishCaught( PrefabFile fish, int weight )
	{
		var path = fish.ResourcePath;
		var date = DateTime.UtcNow;
		if ( FishesCaught.TryGetValue( path, out var record ) )
		{
			record.Count++;
			if ( record.MaxWeight < weight )
			{
				record.MaxWeight = weight;
				record.MaxWhen = date;
			}
		}
		else
		{
			FishesCaught[path] = new FishRecord { Count = 1, MaxWeight = weight, MaxWhen = date };
		}
	}
}
