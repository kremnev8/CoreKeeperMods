﻿using System;
using CoreLib.Submodules.ModSystem;
using PugAutomation;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace InfiniteOreBoulder
{
    public class InfiniteOreBoulderSystem : MonoBehaviour, IPseudoServerSystem
    {
        internal World serverWorld;
        
        private float waitTime;
        private const float refreshTime = 5;
        
        public InfiniteOreBoulderSystem(IntPtr ptr) : base(ptr) { }

        public void OnServerStarted(World world)
        {
            serverWorld = world;
        }

        public void OnServerStopped()
        {
            serverWorld = null;
        }

        private void Update()
        {
            if (serverWorld == null) return;
            
            waitTime -= Time.deltaTime;

            if (waitTime <= 0)
            {
                EntityQuery query =
                    serverWorld.EntityManager.CreateEntityQuery(
                        ComponentType.ReadOnly<DropsLootWhenDamagedCD>(),
                        ComponentType.ReadOnly<PugAutomationCD>(),
                        ComponentType.ReadOnly<MineableDamageDecreaseCD>(),
                        ComponentType.ReadOnly<HealthCD>());

                NativeArray<Entity> result = query.ToEntityArray(Allocator.Temp);

                foreach (Entity entity in result)
                {
                    HealthCD healthCd = serverWorld.EntityManager.GetComponentData<HealthCD>(entity);
                    
                    if (healthCd.health < healthCd.maxHealth - 1000)
                    {
                        healthCd.health = healthCd.maxHealth;
                        serverWorld.EntityManager.SetComponentData(entity, healthCd);
                    }
                }
                
                waitTime = refreshTime;
            }
        }
    }
}