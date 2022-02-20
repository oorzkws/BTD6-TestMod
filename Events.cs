using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Assets.Scripts.Simulation.Towers.Behaviors;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.UI_New;
using Assets.Scripts.Unity.UI_New.Main;
using Assets.Scripts.Unity.UI_New.Settings;
using Assets.Scripts.Utils;
using UnityEngine;
using static MelonLoader.MelonLogger;
using Object = System.Object;

namespace ThisMod
{
    public partial class Mod
    {
        #region Events

        public override void OnSceneWasLoaded(int level, string sceneName)
        {
            netsim = null;
            if (NotNull(Game.instance))
                if (NotNull(Game.instance.playerService) &&
                    NotNull(Game.instance.playerService.Player))
                    Game.instance.playerService.Player.Hakxr = returnNotHax();
            base.OnSceneWasLoaded(level, sceneName);
        }

        private static void AdjustSpeed(float mod)
        {
            var instance = getInGame();
            if (instance != null && !TimeManager.coopPaused && !TimeManager.gamePaused)
            {
                var oldspeed = Math.Max(TimeManager.timeScaleWithoutNetwork, TimeManager.networkScale);
                var newspeed = Math.Max(oldspeed * mod, 0.25f);
                Msg(ConsoleColor.Cyan, "Speed * " + mod + " = " + newspeed);
                TimeManager.networkScale = newspeed;
                TimeManager.timeScaleWithoutNetwork = newspeed;
                TimeManager.maxSimulationStepsPerUpdate = newspeed;
                if (netsim == null) return;
                netsim.SetDeclaredTimeScale(newspeed);
                netsim.SetSimulationSpeed();
            }
            else
            {
                Msg("Custom hotkeys can only be used ingame!");
            }
        }

        private static void SendNextRaceRound()
        {
            var instance = getInGame();
            if (instance != null && instance.bridge != null && instance.bridge.simulation != null)
            {
                var sim = instance.bridge;
                var currentRound = sim.GetCurrentRound();
                if (netsim != null)
                {
                    netsim.SetRound(currentRound + 1);
                    netsim.StartRaceRound();
                }
                else
                {
                    sim.SetRound(currentRound + 1);
                    sim.StartRaceRound();
                }

                Msg("Sending round " + (sim.GetCurrentRound() + 1));
            }
            else
            {
                Msg("Custom hotkeys can only be used ingame!");
            }
        }

        private static void ToggleDebugView()
        {
            var instance = getInGame();
            if (instance != null && instance.bridge != null && instance.inGameData != null)
            {
                Msg(ConsoleColor.Blue, "debugOptions set");
                instance.bridge.simulation.debugOptions = !instance.bridge.simulation.debugOptions;
                instance.inGameData.includeDebugControls = !instance.inGameData.includeDebugControls;
            }
        }

        private static void DumpParagonData()
        {
            var instance = getInGame();
            if (instance != null && instance.bridge != null)
            {
                var paragons = new Dictionary<string,ParagonTower.InvestmentInfo>();
                foreach (TowerToSimulation ttss in instance.bridge.ttss)
                {
                    if (ttss.IsParagon)
                    {
                        paragons.Add($"{ttss.Def._name}.{ttss.id} [Degree: {ttss.GetParagonDegreeMutator().degree}]", ttss.GetCurrentParagonInvestment());
                    }
                }
                if (NotNull(paragons) && paragons.Count > 0)
                {
                    foreach (KeyValuePair<string, ParagonTower.InvestmentInfo> kvp in paragons)
                    {
                        string outputString = String.Empty;
                        ParagonDegreeDataModel pddm = instance.bridge.Model.paragonDegreeDataModel;
                        outputString += $"Tower: {kvp.Key}\n";
                        outputString += $"Pops: {kvp.Value.powerFromPops}pts / {pddm.maxPowerFromPops}pts\n";
                        outputString += $"Money: {kvp.Value.powerFromMoneySpent}pts / {pddm.maxPowerFromMoneySpent}pts\n";
                        outputString += $"Tier 5 Sacrifices: {kvp.Value.powerFromTier5Count}pts / {pddm.maxPowerFromTier5Count}pts\n";
                        outputString += $"Tier <5 Sacrifices: {kvp.Value.powerFromNonTier5Tiers}pts / {pddm.maxPowerFromNonTier5Count}pts\n";
                        outputString += $"Total: {kvp.Value.totalInvestment}pts / {pddm.MaxInvestment}pts";
                        Msg(Colors.Debug, outputString);
                    }
                }
                else
                {
                    Msg(Colors.DebugWarn, "No paragons found :(");
                }
            }

        }

        private static void Win()
        {
            var instance = getInGame();
            if (instance != null && instance.bridge != null)
            {
                instance.bridge.Win();
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            foreach (KeyCode key in Enum.GetValues(typeof(KeyCode)))
            {
                if (!Input.GetKeyUp(key))
                    continue;
                switch (key)
                {
                    case KeyCode.KeypadPlus:
                        AdjustSpeed(2f);
                        break;
                    case KeyCode.KeypadMinus:
                        AdjustSpeed(0.5f);
                        break;
                    case KeyCode.KeypadEnter:
                        SendNextRaceRound();
                        break;
                    case KeyCode.KeypadMultiply:
                        ToggleDebugView();
                        break;
                    case KeyCode.KeypadPeriod:
                        DumpParagonData();
                        break;
                    case KeyCode.KeypadDivide:
                        Win();
                        break;
                }
            }
        }

        #endregion
    }
}