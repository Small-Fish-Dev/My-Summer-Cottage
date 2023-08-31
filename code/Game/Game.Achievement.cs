namespace Sauna;

public enum AchievementId
{
	None = 0,
	SaunaFurnaceFirstTime,
	ExtinguishFirePiss,
	JumpAlot,
}

public class Achievement
{
	public AchievementId Id;
	public float CurrentValue { get; set; }

	public float MaxValue;

	public bool IsUnlocked => CurrentValue == MaxValue;
}

[GameResource( "Achievements", "cheevos", "Things you get when you do something.", Icon = "noise_aware" )]
public class AchievementList : GameResource
{
	public List<AchievementResource> List { get; set; }
}

public class AchievementResource
{
	public AchievementId Id { get; set; }
	public Texture Icon { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public float MaxValue { get; set; } = 1;
}

partial class Sauna
{
	public AchievementList AchievementDefinitions { get; private set; }

	private void LoadAchievements()
	{
		if ( !ResourceLibrary.TryGet<AchievementList>( "assets/achievements/base_achievements.cheevos", out var cheevos ) )
			throw new Exception( "Failed to load achievements Server-side!" );

		AchievementDefinitions = cheevos;
	}

}
