using System;
using System.Collections.Generic;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using MelonLoader;
using ThisMod;
using TrackMap = Assets.Scripts.Simulation.Track;
using static MelonLoader.MelonLogger;

// Note that the MelonLogger import is important to be able to use Msg();

// Meta
[assembly: MelonInfo(typeof(Mod), "Mod", "0.0.0", "null")]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace ThisMod
{
    public partial class Mod : MelonMod
    {
        public static HarmonyLib.Harmony harmony;
        public static List<MelonMod> melons;
        public static NetworkedUnityToSimulation netsim;
        public static DebugTowerGroupPlacer dbgtowers;
        public bool debugEnabled = true;

        public override void OnApplicationStart()
        {
            base.OnApplicationStart();
            harmony = HarmonyInstance;
            melons = MelonHandler.Mods;
            Msg(ConsoleColor.DarkCyan, "Initialized successfully [Built: " + BuildDate.ToLocalTime() + "]");
            Msg(ConsoleColor.DarkCyan, melons.Count + " active mods");
        }
    }
}