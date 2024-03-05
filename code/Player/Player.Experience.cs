using Sauna.UI;

namespace Sauna;

public partial class Player
{
	[Property, Sync, Category( "Parameters" )]
	public int Experience
	{
		get => _experience;
		set => _experience = value.Clamp( 0, int.MaxValue );
	}

	[Property, Sync, Category( "Parameters" )]
	public int Level
	{
		get => _level;
		set => _level = value.Clamp( 0, 99 );
	}

	private int _level;
	private int _experience;

	public int ExpPerLevel => (int)Math.Floor( Math.Pow( 1.03, Level ) * 100 ) + 100;

	public static List<(int MinLevel, string Name)> Ranks = new()
	{
		(99, "Isäntä"),
		(80, "Sauna Pomo"),
		(60, "Kalannarraaja"),
		(40, "Iso Kala"),
		(20, "Kusihiisi"),
		(10, "Kivenpotkija"),
		(0, "Kalanruokaa")
	};

	public event Action<int> OnExperienceEarned;
	public event Action<int> OnLevelUp;

	public string GetRankName() => Ranks.First( rank => rank.MinLevel <= Level ).Name;

	public void AddExperience( int exp )
	{
		Experience += exp;
		
		var oldLevel = Level;
		while ( Experience >= ExpPerLevel )
		{
			if ( Level == 99 )
				break;
			Experience -= ExpPerLevel;
			Level++;
		}

		OnExperienceEarned?.Invoke( exp );
		if ( oldLevel != Level )
		{
			OnLevelUp?.Invoke( Level );
		}
	}
}
