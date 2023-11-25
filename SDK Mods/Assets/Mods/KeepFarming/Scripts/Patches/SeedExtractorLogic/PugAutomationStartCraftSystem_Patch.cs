﻿using System;
using CoreLib.Util.Extensions;
using HarmonyLib;
using KeepFarming.Components;
using PugAutomation;
using Unity.Entities;
using UnityEngine;

namespace KeepFarming
{
    [HarmonyPatch]
    public static class PugAutomationStartCraftSystem_Patch
    {
        [HarmonyPatch(typeof(PugAutomationStartCraftSystem), nameof(OnCreateForCompiler))]
        [HarmonyPostfix]
        public static void OnCreateForCompiler(PugAutomationStartCraftSystem __instance)
        {
            KeepFarmingMod.Log.LogInfo("Patching PugAutomationStartCraftSystem");
            var oldQuery = __instance.GetValue<EntityQuery>("PugAutomationStartCraftSystem_LambdaJob_1_Query");
            var queryDesc = oldQuery.GetEntityQueryDesc();
            queryDesc.None = queryDesc.None.AddToArray(ComponentType.ReadOnly<SeedExtractorCD>());
            
            var query = __instance.EntityManager.CreateEntityQuery(queryDesc);
            query.SetChangedVersionFilter(new[]
            {
                ComponentType.ReadOnly<CraftingCD>(),
                ComponentType.ReadOnly<ContainedObjectsBuffer>()
            });

            __instance.SetValue("PugAutomationStartCraftSystem_LambdaJob_1_Query", query);
        }
    }
}