namespace Sauna;

[Flags]
public enum AchievementId
{
    None = 0,
    SaunaFurnaceFirstTime,
    ExtinguishFirePiss,
}

partial class Player
{
    [Net] public AchievementId UnlockedAchievements { get; private set; }
    private AchievementList _cheevos;

    [SaunaEvent.OnSpawn]
    private void LoadAchievementsClient(Player player)
    {
        if (Game.IsServer)
            return;

        if (player != this)
            return;

        if (!ResourceLibrary.TryGet("assets/achievements/base_achievements.cheevos", out _cheevos))
            throw new Exception("Failed to load achievements!");
    }

    public bool TryUnlockAchievement(AchievementId flag)
    {
        Game.AssertServer();

        if (UnlockedAchievements.HasFlag(flag))
            return false;

        UnlockedAchievements |= flag;
        Log.Info($"Player: {this} unlocked achievement: {flag}");
        UnlockAchievementClient(To.Single(this), (int)flag);
        return true;
    }

    [ClientRpc]
    private void UnlockAchievementClient(int flag)
    {
        var cheevoFlag = (AchievementId)flag;
        var cheevo = _cheevos.List.Where(x => x.Id == cheevoFlag).FirstOrDefault();
        if (cheevo is not null)
            AchievementToaster.Instance.Toast(cheevo);
    }

    [ConCmd.Admin("reset_cheevos")]
    public static void ResetCheevos()
    {
        if (ConsoleSystem.Caller.Pawn is not Player pawn)
            return;

        pawn.UnlockedAchievements = AchievementId.None;
    }

}
