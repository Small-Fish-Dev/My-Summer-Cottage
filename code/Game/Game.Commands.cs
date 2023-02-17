﻿using Sandbox;

namespace Sauna;

partial class Sauna
{
	[ConCmd.Server( "spawn_model" )]
	public static void SpawnModel( string path )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn ) 
			return;

		var model = Model.Load( path );
		if ( model == null || model.Name == "models/dev/error.vmdl" )
		{
			Log.Error( $"Couldn't load the model: {path}" );
			return;
		}

		var ray = new Ray(
			pawn.Position + Vector3.Up * pawn.CollisionBox.Maxs.z,
			pawn.ViewAngles.ToRotation().Forward );

		var trace = Trace.Ray( ray, 500f )
			.Ignore( pawn )
			.Size( model.Bounds )
			.Run();

		var entity = new ModelEntity();
		entity.Model = model;
		entity.SetupPhysicsFromModel( PhysicsMotionType.Dynamic );
		entity.Position = trace.EndPosition;
	}

	[ConCmd.Server( "delete_entity" )]
	public static void DeleteEntity()
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		var ray = new Ray(
			pawn.Position + Vector3.Up * pawn.CollisionBox.Maxs.z,
			pawn.ViewAngles.ToRotation().Forward );

		var trace = Trace.Ray( ray, 500f )
			.Ignore( pawn )
			.Run();

		if ( trace.Entity == null || trace.Entity is WorldEntity )
		{
			Log.Error( $"Didn't find any entity to delete." );
			return;
		}

		trace.Entity?.Delete();

	}

	[ConCmd.Server( "spawn_entity" )]
	public static void SpawnEntity( string typeName )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		var type = GlobalGameNamespace.TypeLibrary.GetType( typeName );
		if ( !type.IsValid )
		{
			Log.Error( $"Couldn't find the type: {typeName}" );
			return;
		}

		var ray = new Ray(
			pawn.Position + Vector3.Up * pawn.CollisionBox.Maxs.z,
			pawn.ViewAngles.ToRotation().Forward );

		var trace = Trace.Ray( ray, 500f )
			.Ignore( pawn )
			.Run();

		var entity = type.Create<Entity>();
		if ( entity == null )
		{
			Log.Error( $"Failed to create the entity: {typeName}" );
			return;
		}

		entity.Position = trace.EndPosition;
	}

	[ConCmd.Server( "give_exp" )]
	public static void GiveExperience( int amount )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		pawn.Experience += amount;
	}

	[ConCmd.Server( "add_effect" )]
	public static void AddEffect( string effect, float duration = 5f, int stacks = 1 )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		var effectType = GlobalGameNamespace.TypeLibrary.GetType( effect );
		if ( effectType == null || !effectType.TargetType.IsSubclassOf( typeof ( BaseEffect ) ) )
		{
			Log.Error( $"Failed to find the effect: {effect}" );
			return;
		}

		pawn.Effects.Apply( effectType.TargetType, duration, stacks );
	}

	[ConCmd.Server( "test_subtitle" )]
	public static void TestSubtitle( string text, float time = 5f, char wrapper = '"' )
	{
		if ( ConsoleSystem.Caller?.Pawn is not Player pawn )
			return;

		Subtitles.Send( To.Everyone, text, time, wrapper: wrapper );
	}
}
