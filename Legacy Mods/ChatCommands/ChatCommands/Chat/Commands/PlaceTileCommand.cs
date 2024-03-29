﻿using System;
using System.Linq;
using CoreLib;
using CoreLib.Submodules.ChatCommands;
using HarmonyLib;
using PugTilemap;
using Unity.Mathematics;
using UnityEngine;

namespace ChatCommands.Chat.Commands
{
    public class PlaceTileCommand : IChatCommandHandler
    {
        public CommandOutput Execute(string[] parameters)
        {
            PlayerController player = GameManagers.GetMainManager().player;
            if (player == null) return new CommandOutput("Internal error", Color.red);

            if (parameters.Length < 3)
            {
                return new CommandOutput("Not enough parameters, please check usage via /help placeTile!", Color.red);
            }

            int2 pos = CommandUtil.ParsePos(parameters, parameters.Length - 1, player, out CommandOutput? commandOutput);
            if (commandOutput != null)
                return commandOutput.Value;

            int leftArgs = parameters.Length - 2;

            if (leftArgs == 2)
            {
                if (Enum.TryParse(parameters[0], true, out Tileset tileset) &&
                    Enum.TryParse(parameters[1], true, out TileType tileType))
                {
                    return TryPlaceTile((int)tileset, tileType, player, pos);
                }
            }

            string fullName = parameters.Take(leftArgs).Join(null, " ");
            CommandOutput output = CommandUtil.ParseItemName(fullName, out ObjectID objectID);
            if (objectID == ObjectID.None)
                return output;

            return PlaceObjectID(objectID, player, pos);
        }

        public static CommandOutput TryPlaceTile(int tileset, TileType tileType, PlayerController player, int2 pos)
        {
            var tilesetData = TilesetTypeUtility.GetTileset(tileset);
            var layerName = TileTypeToLayerName.GetLayerName(tileType);
            var quadGenerator = tilesetData.GetDef(layerName);
            if (quadGenerator == null)
            {
                return new CommandOutput($"Tileset {tileset}, tileType: {tileType} does not exist!", Color.red);
            }

            player.playerCommandSystem.AddTile(pos, tileset, tileType);
            return "Tile placed.";
        }

        public static TileCD GetTileData(ObjectID objectID, out CommandOutput? commandOutput)
        {
            if (PugDatabase.HasComponent<TileCD>(objectID))
            {
                commandOutput = null;
                return PugDatabase.GetComponent<TileCD>(objectID);
            }

            commandOutput = new CommandOutput("This object is not a tile!", Color.red);
            return default;
        }

        public static CommandOutput PlaceObjectID(ObjectID objectID, PlayerController player, int2 pos)
        {
            TileCD tileData = GetTileData(objectID, out CommandOutput? commandOutput2);
            if (commandOutput2 != null)
                return commandOutput2.Value;

            player.playerCommandSystem.AddTile(pos, tileData.tileset, tileData.tileType);
            return "Tile placed.";
        }

        public string GetDescription()
        {
            return "Use /placeTile to place tiles into world" +
                   "\n /placeTile {tileset} {tileType} {x} {y}" +
                   "\n /placeTile {itemName} {x} {y}" +
                   "\nPosition can be relative, if '~' is added to beginning" +
                   "\nTileset defines set of tiles (Most of the time its a biome)" +
                   "\nTileType defines the kind of a tile: ground, wall, rail, etc.";
        }

        public string[] GetTriggerNames()
        {
            return new[] { "placeTile" };
        }
    }
}