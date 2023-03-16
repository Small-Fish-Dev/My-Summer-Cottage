global using Sandbox;
global using Sandbox.Internal;
global using Sandbox.UI;
global using Sandbox.UI.Construct;
global using Sandbox.Effects;
global using Editor;
global using System;
global using System.IO;
global using System.Linq;
global using System.Threading.Tasks;
global using System.Collections.Generic;

namespace Sauna;

public partial class Sauna : BaseGameManager
{
	public static Sauna Instance { get; private set; }

	public Sauna()
	{
		Instance = this;

		if ( Game.IsClient )
		{
			_ = new HUD();
			LoadRenderHooks();
		}
	}

	public override void ClientJoined( IClient client )
	{
		// Create new pawn for new client.
		var player = new Player();
		client.Pawn = player;

		Event.Run( nameof( SaunaEvent.OnSpawn ), player );
	}

	public override void ClientDisconnect( IClient cl, NetworkDisconnectionReason reason )
	{
		// Remove the client's pawn on disconnect.
		cl.Pawn?.Delete();
		cl.Pawn = null;
	}

	public override void Simulate( IClient cl )
	{
		// Simulate the client's pawn.
		var player = cl.Pawn as Entity;
		if ( player != null && player.IsValid() && player.IsAuthority )
			player.Simulate( cl );
	}

	public override void FrameSimulate( IClient cl )
	{
		// Simulate the client's pawn on frame.
		var player = cl.Pawn as Entity;
		if ( player != null && player.IsValid() && player.IsAuthority )
			player.FrameSimulate( cl );
	}

	public override void Shutdown()
	{
		Instance?.Delete();
		Instance = null;
	}

	public override void PostLevelLoaded()
	{
		base.PostLevelLoaded();
	}

	public override void RenderHud()
	{
		base.RenderHud();
	}
}
