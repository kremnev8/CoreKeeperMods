﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using CoreLib;
using CoreLib.Submodules.ChatCommands;
using HarmonyLib;
using UnityEngine;

namespace ChatCommands.Chat.Commands;

public class GiveCommandHandler : IChatCommandHandler
{
    public CommandOutput Execute(string[] parameters)
    {
        if (parameters.Length == 0)
        {
            return new CommandOutput("Please enter item name", Color.red);
        }

        if (parameters.Length > 3 && parameters[0] == "food")
        {
            return GiveFood(parameters);
        }

        return NormalGive(parameters);
    }

    private CommandOutput GiveFood(string[] parameters)
    {
        int count = 1;
        int nameArgCount = parameters.Length;
        if (nameArgCount > 1 && int.TryParse(parameters[^1], out int val))
        {
            count = val;
            nameArgCount--;
        }

        string args = parameters.Take(nameArgCount).TakeLast(nameArgCount - 1).Join(null, " ");
        if (!args.Contains('+'))
        {
            return new CommandOutput("Plus sign '+' is missing. Please use it to separate item names!", Color.red);
        }

        string[] itemName = args.Split(" + ");
        if (itemName.Length > 2)
        {
            return new CommandOutput("Too many items in input. Please provide only two items", Color.red);
        }

        CommandOutput output1 = CommandUtil.ParseItemName(itemName[0], out ObjectID item1);
        if (item1 == ObjectID.None)
            return output1;
        CommandOutput output2 = CommandUtil.ParseItemName(itemName[1], out ObjectID item2);
        if (item2 == ObjectID.None)
            return output2;

        if (!PugDatabase.HasComponent<CookingIngredientCD>(item1))
        {
            return new CommandOutput($"{item1} is not a cooking ingredient!", Color.red);
        }

        if (!PugDatabase.HasComponent<CookingIngredientCD>(item2))
        {
            return new CommandOutput($"{item2} is not a cooking ingredient!", Color.red);
        }
        
        CookingIngredientCD ingredient1 = PugDatabase.GetComponent<CookingIngredientCD>(item1);

        AddToInventory(ingredient1.turnsIntoFood, count, int.Parse($"{(int)item1}{(int)item2}"));
        return $"Successfully added {count} {ingredient1.turnsIntoFood}";
    }

    private CommandOutput NormalGive(string[] parameters)
    {
        int count = 1;
        int variation = 0;
        int nameArgCount = parameters.Length;
        if (nameArgCount > 1 && int.TryParse(parameters[^1], out int val))
        {
            count = val;
            nameArgCount--;
        }

        if (nameArgCount > 1 && int.TryParse(parameters[^2], out val))
        {
            variation = count;
            count = val;
            nameArgCount--;
        }

        string fullName = parameters.Take(nameArgCount).Join(null, " ");
        fullName = fullName.ToLower();

        CommandOutput output = CommandUtil.ParseItemName(fullName, out ObjectID objectID);
        if (objectID == ObjectID.None)
            return output;

        AddToInventory(objectID, count, variation);
        return $"Successfully added {count} {objectID}, variation {variation}";
    }

    private void AddToInventory(ObjectID objId, int amount, int variation)
    {
        PlayerController player = GameManagers.GetMainManager().player;
        if (player == null) return;
        InventoryHandler handler = player.playerInventoryHandler;
        if (handler == null) return;
        handler.CreateItem(0, objId, amount, player.WorldPosition, variation);
    }

    public string GetDescription()
    {
        return
            "Use /give to give yourself any item. \n" +
            "/give {itemName} [count] [variation]\n" +
            "The count parameter defaults to 1. Variation defaults to 0\n" +
            "/give food {item1} + {item2} [count] Add any food. First item is used as a base ingredient.";
    }

    public string[] GetTriggerNames()
    {
        return new[] { "give" };
    }
}