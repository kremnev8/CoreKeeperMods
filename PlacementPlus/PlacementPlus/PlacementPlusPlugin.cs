using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using CoreLib;
using HarmonyLib;
using Rewired;
using UnhollowerBaseLib;
using UnhollowerRuntimeLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PlacementPlus
{
    [BepInPlugin(MODGUID, MODNAME, VERSION)]
    [BepInDependency(CoreLib.CoreLib.GUID)]
    public class PlacementPlusPlugin : BasePlugin
    {
        public const string MODNAME = "Placement Plus";

        public const string MODGUID = "org.kremnev8.plugin.PlacementPlus";

        public const string VERSION = "1.1.2";

        public const string CHANGE_ORIENTATION = "PlacementPlus_ChangeOrientation";
        public const string ROTATE = "PlacementPlus_Rotate";

        public const string INCREASE_SIZE = "PlacementPlus_IncreaseSize";
        public const string DECREASE_SIZE = "PlacementPlus_DecreaseSize";
        
        public const string FORCEADJACENT = "PlacementPlus_ForceAdjacent";

        public static ManualLogSource logger;
        public new static ConfigFile Config;

        public static AssetBundle bundle;

        private static Il2CppReferenceArray<Object> m_iconSprites;

        #region Excludes

        public static HashSet<ObjectID> defaultExclude = new HashSet<ObjectID>()
        {
            ObjectID.WoodenWorkBench,
            ObjectID.TinWorkbench,
            ObjectID.IronWorkBench,
            ObjectID.ScarletWorkBench,
            ObjectID.OctarineWorkbench,
            ObjectID.FishingWorkBench,
            ObjectID.JewelryWorkBench,
            ObjectID.AdvancedJewelryWorkBench,

            ObjectID.Furnace,
            ObjectID.SmelterKiln,

            ObjectID.GreeneryPod,
            ObjectID.Carpenter,
            ObjectID.AlchemyTable,
            ObjectID.TableSaw,

            ObjectID.CopperAnvil,
            ObjectID.TinAnvil,
            ObjectID.IronAnvil,
            ObjectID.ScarletAnvil,
            ObjectID.OctarineAnvil,

            ObjectID.ElectronicsTable,
            ObjectID.RailwayForge,
            ObjectID.PaintersTable,
            ObjectID.AutomationTable,
            ObjectID.CartographyTable,
            ObjectID.SalvageAndRepairStation,
            ObjectID.DistilleryTable,

            ObjectID.ElectricityGenerator,
            ObjectID.WoodDoor,
            ObjectID.StoneDoor,
            ObjectID.ElectricalDoor,

            ObjectID.Minecart,
            ObjectID.Boat,
            ObjectID.SpeederBoat,
        };

        public static HashSet<ObjectID> userExclude = new HashSet<ObjectID>()
        {
            ObjectID.InventoryChest,
            ObjectID.InventoryLarvaHiveChest,
            ObjectID.InventoryMoldDungeonChest,
            ObjectID.InventoryAncientChest,
            ObjectID.InventorySeaBiomeChest,
            ObjectID.Torch,
            ObjectID.Campfire,
            ObjectID.DecorativeTorch1,
            ObjectID.DecorativePot,
            ObjectID.PlanterBox,
            ObjectID.Pedestal,
            ObjectID.StonePedestal,
            ObjectID.RuinsPedestal,
            ObjectID.Lamp,
            ObjectID.Sprinkler,
        };
        
        #endregion

        public static ConfigEntry<string> excludeString;
        public static ConfigEntry<int> maxSize;
        public static ConfigEntry<KeyMode> forceKeyMode; 

        public override void Load()
        {
            logger = Log;
            BepInPlugin metadata = MetadataHelper.GetMetadata(this);
            Config = new ConfigFile(Path.Combine(Paths.ConfigPath, "PlacementPlus", "PlacementPlus.cfg"), true, metadata);

            maxSize = Config.Bind("General", "MaxBrushSize", 7, new ConfigDescription("Max range the brush will have", new AcceptableValueRange<int>(3, 9)));

            forceKeyMode = Config.Bind("General", "ForceRotationKeyMode", KeyMode.HOLD, "Which mode should 'Force adjacent belt rotation' keybind use");
            
            excludeString = Config.Bind("General", "ExcludeItems", userExclude.Join(),
                "List of comma delimited items to automatically disable the area placement feature. You can reference 'ItemIDs.txt' file for all existing item ID's");

            ParseConfigString();
            WriteReferenceFile();

            RewiredKeybinds.AddKeybind(CHANGE_ORIENTATION, "Change Orientation", KeyboardKeyCode.C);
            RewiredKeybinds.AddKeybind(INCREASE_SIZE, "Increase Size", KeyboardKeyCode.KeypadPlus);
            RewiredKeybinds.AddKeybind(DECREASE_SIZE, "Decrease Size", KeyboardKeyCode.KeypadMinus);
            RewiredKeybinds.AddKeybind(ROTATE, "Rotate", KeyboardKeyCode.V);
            RewiredKeybinds.AddKeybind(FORCEADJACENT, "Force adjacent belt rotation", KeyboardKeyCode.LeftControl);

            string pluginfolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            bundle = AssetBundle.LoadFromFile($"{pluginfolder}/placementplusbundle");

            m_iconSprites = bundle.LoadAssetWithSubAssets("Assets/PlacementPlus/Textures/arrow_cursor.png", Il2CppType.Of<Sprite>());

            AddComponent<UpdateMono>();

            Harmony harmony = new Harmony(MODGUID);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            logger.LogInfo("Placement Plus mod is loaded!");
        }

        public static Sprite GetSprite(int index)
        {
            if (m_iconSprites == null || m_iconSprites[index] == null)
            {
                m_iconSprites = bundle.LoadAssetWithSubAssets("Assets/PlacementPlus/Textures/arrow_cursor.png", Il2CppType.Of<Sprite>());
            }

            return m_iconSprites[index].Cast<Sprite>();
        }


        private static void ParseConfigString()
        {
            string itemsNoSpaces = excludeString.Value.Replace(" ", "");
            if (string.IsNullOrEmpty(itemsNoSpaces)) return;

            string[] split = itemsNoSpaces.Split(',');
            userExclude.Clear();
            foreach (string item in split)
            {
                try
                {
                    ObjectID itemEnum = (ObjectID)Enum.Parse(typeof(ObjectID), item);
                    if (itemEnum is ObjectID.Drill or ObjectID.MechanicalArm or ObjectID.ConveyorBelt) continue;

                    userExclude.Add(itemEnum);
                }
                catch (ArgumentException)
                {
                    logger.LogWarning($"Error parsing item name! Item '{item}' is not a valid item name!");
                }
            }
        }

        private static void WriteReferenceFile()
        {
            using FileStream stream = File.OpenWrite(Path.Combine(Paths.ConfigPath, "PlacementPlus", "ItemIDs.txt"));

            byte[] text = Encoding.UTF8.GetBytes(
                "#This file contains all known item ID's.\n#You can use this file as a reference while configuring 'AreaPlacement.cfg'\n");
            stream.Write(text, 0, text.Length);

            string[] allItems = Enum.GetNames(typeof(ObjectID));
            foreach (string item in allItems)
            {
                if (item.Equals("None") || item.Equals("LARGEST_ID")) continue;

                byte[] info = Encoding.UTF8.GetBytes("\n" + item);
                stream.Write(info, 0, info.Length);
            }
        }
    }
}