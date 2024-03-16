namespace Sauna;

public class FishRecord
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

	private static readonly string[] fishingCaptions = new[]
	{
		"Caught a big one today! A whopping %wkg...",
		"My first %wkg %s. Proud of this one, will share with wife.",
		"I caught a %s that weighs %wkg? Me? This is honestly just crazy.",
		"My biggest %s yet, good day to be alive."
	};

	public void OnFishCaught( PrefabFile fish, int weight )
	{
		var path = fish.ResourcePath;
		var date = DateTime.UtcNow;

		var definition = fish.AsDefinition();
		if ( definition == null )
			return;

		var trash = definition.GetComponent<Fish>().Get<bool>( "IsTrash" );
		if ( trash )
			return;

		var species = definition.GetComponent<ItemComponent>().Get<string>( "Name" );

		if ( FishesCaught.TryGetValue( path, out var record ) )
		{
			record.Count++;
			if ( record.MaxWeight < weight )
			{
				record.MaxWeight = weight;
				record.MaxWhen = date;

				var range = definition.GetComponent<Fish>().Get<RangedFloat>( "WeightRange" );
				if ( weight >= range.y * 0.3f ) // Has to be atleast 30% of max weight.
				{
					var caption = Game.Random.FromArray( fishingCaptions )
						.Replace( "%w", (record.MaxWeight / 1000f).ToString() )
						.Replace( "%s", species );
					var delay = Game.Random.Int( 0, 400 );

					GameTask.RunInThreadAsync( async () =>
					{
						await GameTask.Delay( delay );
						CaptureMemory( caption, "big_catch" );
					} );
				}
			}
		}
		else
		{
			FishesCaught[path] = new FishRecord { Count = 1, MaxWeight = weight, MaxWhen = date };
			NotificationManager.Popup( "New catch!", $"{species} has been added to your collection", Color.FromBytes( 14, 89, 159 ), "/ui/hud/fish_collection.png", null );
		}
	}
}
