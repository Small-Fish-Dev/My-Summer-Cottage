namespace Sauna.Event.Flags;

public interface IFlagContainer
{
	public FlagContainerId ContainerId { get; set; }
	
	public byte? GetFlagValue( uint key );
	public bool SetFlagValue( uint key, byte value );

	public void Save();
	public void Load();
}
