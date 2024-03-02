namespace Sauna;

public partial class Player
{
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
			Experience -= ExpPerLevel;
			Level++;
		}

		OnExperienceEarned?.Invoke( exp );
		if ( oldLevel != Level )
			OnLevelUp?.Invoke( Level );
	}
}
