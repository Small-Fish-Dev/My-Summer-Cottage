namespace Sauna;

partial class Player
{
	/// <summary>
	/// The actual state of the achievements, stored for progression, Server-side.
	/// </summary>
	public Dictionary<AchievementId, Achievement> Achievements { get; private set; }

	/// <summary>
	/// Client-side independent access to the achievement definitions.
	/// </summary>
	private AchievementList _cheevos;
	private string _achievementFile => string.Format( "achievements-{0}.json", Client.SteamId );

	[SaunaEvent.OnSpawn]
	private void LoadAchievementsClient( Player player )
	{
		if ( player != this )
			return;

		if ( Game.IsServer )
		{
			Achievements = new();

			// Load the player's progress and apply it to our player.
			var loadedProgress = FileSystem.Data.ReadJson<Dictionary<AchievementId, Achievement>>( _achievementFile );

			foreach ( var cheevo in Sauna.Instance.AchievementDefinitions.List )
			{
				var currentValue = 0f;
				if ( loadedProgress.TryGetValue( cheevo.Id, out var progress ) )
					currentValue = progress.CurrentValue;

				Achievements.Add( cheevo.Id, new Achievement()
				{
					Id = cheevo.Id,
					MaxValue = cheevo.MaxValue,
					CurrentValue = currentValue
				} );
			}

			// Client doesn't need the this list, just the definitions.
			return;
		}

		if ( !ResourceLibrary.TryGet( "assets/achievements/base_achievements.cheevos", out _cheevos ) )
			throw new Exception( $"Failed to load achievements for client: {Client.SteamId} : {Client.Name} : {this}!" );
	}

	public void ProgressAchievement( AchievementId id, float amount = 1.0f )
	{
		Game.AssertServer();

		var cheevo = Achievements[id];
		if ( cheevo.IsUnlocked )
			return;

		var def = Sauna.Instance.AchievementDefinitions.List.Where( x => x.Id == id ).FirstOrDefault();
		if ( def is null )
			throw new Exception( "We fucked up bigly." );

		Achievements[id].CurrentValue += amount;

		// Save every time it is progressed.
		// This might be a problem for amount of steps taken, distance travelled, etc.
		SaveAchievements();

		if ( Achievements[id].CurrentValue < def.MaxValue )
		{
			// TODO: Notify client to show a progress bar toast.
			// (At intervals, every 1/3rd or something).
			return;
		}

		Log.Info( $"Player: {this} unlocked achievement: {id}" );
		UnlockAchievementClient( To.Single( this ), (int)id );
	}

	[ClientRpc]
	private void UnlockAchievementClient( int id )
	{
		var cheevoFlag = (AchievementId)id;
		var cheevo = _cheevos.List.Where( x => x.Id == cheevoFlag ).FirstOrDefault();
		if ( cheevo is not null )
			AchievementToaster.Instance.Toast( cheevo );
	}

	public void ResetAchievements()
	{
		Game.AssertServer();

		foreach ( var kvp in Achievements )
		{
			var cheevo = kvp.Value;
			if ( !cheevo.IsUnlocked )
				continue;

			cheevo.CurrentValue = 0;
		}

		SaveAchievements();
	}

	// TODO
	private void SaveAchievements()
	{
		Game.AssertServer();
		
		//FileSystem.Data.WriteJson( _achievementFile, Achievements );
	}

	[ConCmd.Admin( "reset_achievements" )]
	public static void ResetCheevos()
	{
		if ( ConsoleSystem.Caller.Pawn is not Player pawn )
			return;

		pawn.ResetAchievements();
	}
}
