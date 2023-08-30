namespace Sauna;

[GameResource("Achievements", "cheevos", "Things you get when you do something.", Icon = "noise_aware")]
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
}
