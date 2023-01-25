namespace Sauna;

partial class Player
{
	[Net, Change( "onExperience" )] private int experience { get; set; }
	public int Experience
	{ 
		get => experience; 
		set
		{
			if ( Game.IsClient ) return;

			experience = value;
		}
	}

	public float Progress => experience / 100f;

	void onExperience( int previous, int current )
		=> ExperienceDisplay.OnChange( current - previous );
}
