namespace Sauna;

public static class GameSettings
{
	public enum Difficulty
	{
		Easy,
		Hard
	}

	public static Difficulty CurrentDifficulty = Difficulty.Easy;

	public static bool IsUsingFinnish => CurrentDifficulty == Difficulty.Hard
	                                     || Language.Current.Abbreviation == "fi";

	public static string GetLanguage => CurrentDifficulty == Difficulty.Hard ? "fi" : Language.Current.Abbreviation;
}
