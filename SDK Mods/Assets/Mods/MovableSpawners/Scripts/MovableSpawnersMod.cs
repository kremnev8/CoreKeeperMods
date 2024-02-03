﻿using System.Linq;
using PugMod;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace MovableSpawners
{
    public class MovableSpawnersMod : IMod
    {
        internal const string Textures = "Assets/Mods/MovableSpawners/Textures/";
        
        public const string VERSION = "1.0.0";
        public const string NAME = "Movable Spawners";
        private static LoadedMod modInfo;

        internal static AssetBundle AssetBundle => modInfo.AssetBundles[0];

        public void EarlyInit()
        {
            Debug.Log($"[{NAME}]: Mod version: {VERSION}");
            modInfo = GetModInfo(this);
            if (modInfo == null)
            {
                Debug.Log($"[{NAME}]: Failed to load {NAME}: mod metadata not found!");
                return;
            }

            if (modInfo.AssetBundles.Count == 0)
            {
                Debug.Log($"[{NAME}]: Failed to load {NAME}: Asset bundle missing!");
                return;
            }

            API.Authoring.OnObjectTypeAdded += EditSpawners;

            var platform = GetPlatformString();
            if (platform != null)
            {
                string directory = API.ModLoader.GetDirectory(modInfo.ModId);
                string ID = NAME.Replace(" ", "");
                string fileExtension = GetPlatformExtension(platform);
                bool success = BurstRuntime.LoadAdditionalLibrary($"{directory}/{ID}_burst_generated_{platform}.{fileExtension}");
                if (!success)
                    Debug.LogWarning($"[{NAME}]: Failed to load burst assembly");
            }
            Debug.Log($"[{NAME}]: Mod loaded successfully");
        }

        public static string GetPlatformString()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.WindowsPlayer:
                case RuntimePlatform.WindowsServer:
                    return "Windows";
                case RuntimePlatform.LinuxPlayer:
                case RuntimePlatform.LinuxServer:
                    return "Linux";
            }

            return null;
        }

        public static string GetPlatformExtension(string platform)
        {
            if (platform == "Windows")
                return "dll";
            if (platform == "Linux")
                return "so";
            return "";
        }
        
        public static LoadedMod GetModInfo(IMod mod)
        {
            return API.ModLoader.LoadedMods.FirstOrDefault(modInfo => modInfo.Handlers.Contains(mod));
        }

        private void EditSpawners(Entity entity, GameObject authoringdata, EntityManager entitymanager)
        {
            var entityData = authoringdata.GetComponent<EntityMonoBehaviourData>();
            if (entityData == null ||
                entityData.objectInfo.objectID != ObjectID.SummonArea) return;
            
            Debug.Log($"[{NAME}]: Editing {entityData.objectInfo.objectID}, {entityData.objectInfo.variation}");
            
            entityData.objectInfo.objectType = ObjectType.PlaceablePrefab;
            entityData.objectInfo.rarity = Rarity.Legendary;
            entityData.objectInfo.isStackable = false;
            entityData.objectInfo.prefabTileSize = new Vector2Int(3, 3);
            entityData.objectInfo.prefabCornerOffset = new Vector2Int(-1, -1);
            entityData.objectInfo.centerIsAtEntityPosition = true;
            
            entityData.objectInfo.smallIcon = AssetBundle.LoadAsset<Sprite>(Textures + "icon-small.png");
            entityData.objectInfo.icon = AssetBundle.LoadAsset<Sprite>(Textures + "icon-big.png");

            entitymanager.RemoveComponent<IndestructibleCD>(entity);
            entitymanager.RemoveComponent<NonHittableCD>(entity);
            entitymanager.AddComponent<SummonAreaIndestructibleStateCD>(entity);

            entitymanager.AddComponentData(entity, new PlaceableObjectCD()
            {
                canBePlacedOnPlayer = true,
                canBePlacedOnAnyWalkableTile = true,
                variationToPlace = entityData.objectInfo.variation
            });

            entitymanager.AddComponent<MineableCD>(entity);
            if (entitymanager.HasComponent<AlwaysDropVariationZeroCD>(entity))
            {
                entitymanager.RemoveComponent<AlwaysDropVariationZeroCD>(entity);
            }

            entitymanager.AddComponentData(entity, new DamageReductionCD(){
                 maxDamagePerHit = 1
            });

            entitymanager.AddComponentData(entity, new HealthRegenerationCD()
            {
                normHealthPerFifthSecond = 1,
                startHealDelay = 5
            });

            if (entitymanager.HasComponent<AllowHealthRegenerationInCombatCD>(entity))
            {
                entitymanager.RemoveComponent<AllowHealthRegenerationInCombatCD>(entity);
            }
            
            entitymanager.AddComponent<IsInCombatCD>(entity);
            entitymanager.AddComponent<AnimationCD>(entity);
            entitymanager.AddComponent<StateInfoCD>(entity);
            entitymanager.AddComponent<IdleStateCD>(entity);
            entitymanager.AddComponent<StunnedStateCD>(entity);
            entitymanager.AddComponent<TookDamageStateCD>(entity);
            entitymanager.AddComponent<DamageEffectCD>(entity);
            entitymanager.AddComponent<TriggerAnimationOnDeathCD>(entity);
        }

        public void Init()
        {
        }

        public void Shutdown()
        {
        }

        public void ModObjectLoaded(Object obj)
        {
        }

        public void Update()
        {
        }
    }
}