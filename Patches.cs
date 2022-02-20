using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Assets.Main.Scenes;
using Assets.Scripts.Data.MapSets;
using Assets.Scripts.Models;
using Assets.Scripts.Models.GenericBehaviors;
using Assets.Scripts.Models.Profile;
using Assets.Scripts.Models.Towers.Behaviors;
using Assets.Scripts.Models.Towers.Behaviors.Abilities;
using Assets.Scripts.Models.Towers.Behaviors.Abilities.Behaviors;
using Assets.Scripts.Unity;
using Assets.Scripts.Unity.Analytics;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Map;
using Assets.Scripts.Unity.Player;
using Assets.Scripts.Unity.UI_New.ChallengeEditor;
using Assets.Scripts.Unity.UI_New.Coop;
using Assets.Scripts.Unity.UI_New.GameEvents;
using Assets.Scripts.Utils;
using HarmonyLib;
using Il2CppSystem.IO;
using Il2CppSystem.Threading.Tasks;
using MelonLoader;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NinjaKiwi.LiNK.DotNetZip.Zlib;
using NinjaKiwi.LiNK.Net;
using NinjaKiwi.LiNK.Utils;
using NinjaKiwi.Players.Utils;
using UnhollowerBaseLib;
using UnityEngine;
using static MelonLoader.MelonLogger;
// ReSharper disable UnusedMember.Local

namespace ThisMod
{
    public partial class Mod
    {
        // Valid property names

        private static readonly Dictionary<string, object> validProperties =
            new Dictionary<string, object>
            {
                {"maxCount", 999},
                {"maxStacks", 999},
                {"maxStackSize", 999},
                {"maxStackCount", 999},
                {"isUnique", false},
                {"stackLimit", 999},
                {"stackable", true},
                {"affectsOnlyWater", false},
                {"affectSelf", true},
                {"appliesToOwningTower", true},
                {"showBuffIcon", true}, // Utility!
                {"onlyShowBuffIfMutated", false},
                {"tierCap", 999} // Discounts
            };

        private static readonly Dictionary<string, int> callCounts =
            new Dictionary<string, int>();

        private static Btd6Player.HakrStatus returnNotHax()
        {
            var hakr = new Btd6Player.HakrStatus();
            hakr.genrl = false;
            hakr.ledrbrd = false;
            return hakr;
        }

        [HarmonyPatch(typeof(ProfileModel), "Validate")]
        private static class ProfileModelValidate
        {
            private static void Postfix(ref ProfileModel __instance)
            {
                __instance.HasCompletedTutorial = true;
                Msg(ConsoleColor.Blue,
                    "Profile validated"
                );
            }
        }

        [HarmonyPatch(typeof(Btd6Player), "OneTimeStartUpChecks")]
        private static class Btd6PlayerPatch
        {
            private static bool waited;

            private static IEnumerator SetDosh(Btd6Player __instance)
            {
                while (!waited)
                {
                    waited = true;
                    yield return new WaitForSecondsRealtime(10);
                }

                if (__instance.GetMonkeyMoney() < 2500f && Game.instance != null)
                    Game.instance.playerService.Player.Data.monkeyMoney = new KonFuze(2500d);
            }

            private static void Postfix(ref Btd6Player __instance)
            {
                Msg(ConsoleColor.Blue, "Btd6Player initialized");
                __instance.Hakxr = returnNotHax();
                //__instance.debugUnlockAllModes = true;
                __instance.debugUnlockAllTowers = true;
                __instance.debugUnlockAllUpgrades = true;
                __instance.debugSeenAllRounds = true;
                MelonCoroutines.Start(SetDosh(__instance));
            }
        }

        [HarmonyPatch]
        private static class HakrStatusPatchA
        {
            private static MethodBase TargetMethod()
            {
                return 
                    (from method in typeof(Btd6Player).GetMethods()
                        where method.Name == "CheckHakrStatus"
                        where method.ReturnType == typeof(Task<Btd6Player.HakrStatus>)
                        select method).FirstOrDefault();
            }

            private static IEnumerator WaitTask(Task<Btd6Player.HakrStatus> task)
            {
                while (!task.IsCompleted && !task.IsFaulted) yield return new WaitForEndOfFrame();

                if (!task.IsFaulted)
                {
                    task.DangerousSetResult(returnNotHax());
                    Game.instance.playerService.Player.Hakxr = returnNotHax();
                }
            }

            private static void Postfix(ref Task<Btd6Player.HakrStatus> __result)
            {
                MelonCoroutines.Start(WaitTask(__result));
            }
        }
        
        [HarmonyPatch]
        private static class HakrStatusPatchB
        {
            private static MethodBase TargetMethod()
            {
                return 
                (from method in typeof(Btd6Player).GetMethods()
                 where method.Name == "CheckHakrStatus"
                 where method.ReturnType == typeof(Task<bool>)
                 select method).FirstOrDefault();
            }

            private static IEnumerator WaitTask(Task<bool> task)
            {
                while (!task.IsCompleted && !task.IsFaulted) yield return new WaitForEndOfFrame();

                if (!task.IsFaulted)
                {
                    task.DangerousSetResult(false);
                    Game.instance.playerService.Player.Hakxr = returnNotHax();
                }
            }

            private static void Postfix(ref Task<bool> __result)
            {
                MelonCoroutines.Start(WaitTask(__result));
            }
        }

        [HarmonyPatch]
        private static class CoopLobbyAreHackersPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(CoopLobbyScreen).GetMethod("AreHackersInLobby");
            }
            
            private static IEnumerator WaitTask(Task<bool> task)
            {
                while (!task.IsCompleted && !task.IsFaulted) yield return new WaitForEndOfFrame();

                if (task.IsFaulted) yield break;
                task.DangerousSetResult(false);
            }

            private static void Postfix(ref Task<bool> __result)
            {
                MelonCoroutines.Start(WaitTask(__result));
            }
        }
        
        [HarmonyPatch]
        private static class CoopLobbyCheckHackerPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(CoopLobbyScreen).GetMethod("CheckHackerStatus");
            }
            
            private static IEnumerator WaitTask(Task<bool> task)
            {
                while (!task.IsCompleted && !task.IsFaulted) yield return new WaitForEndOfFrame();

                if (task.IsFaulted) yield break;
                task.DangerousSetResult(false);
            }

            private static void Postfix(ref Task<bool> __result)
            {
                MelonCoroutines.Start(WaitTask(__result));
            }
        }
        
        [HarmonyPatch]
        private static class MultiHakrStatusPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(Btd6Player).GetMethod("CheckMultiHakrStatus");
            }

            private static IEnumerator WaitTask(Task<bool> task)
            {
                while (!task.IsCompleted && !task.IsFaulted) yield return new WaitForEndOfFrame();

                if (task.IsFaulted) yield break;
                task.DangerousSetResult(false);
                Game.instance.playerService.Player.Hakxr = returnNotHax();
            }

            private static void Postfix(ref Task<bool> __result)
            {
                MelonCoroutines.Start(WaitTask(__result));
            }
        }

        [HarmonyPatch]
        private static class ChallengeManagerIsModderPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(PlayerChallengeManagerExtensions).GetMethod("IsHackerOrModder");
            }

            private static IEnumerator WaitTask(Task<bool> task)
            {
                while (!task.IsCompleted && !task.IsFaulted) 
                    yield return new WaitForEndOfFrame();

                if (!task.IsFaulted) task.DangerousSetResult(false);
            }
        }

        [HarmonyPatch]
        private static class HakrDetectedPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(AnalyticsManager).GetMethod("HackerDetected");
            }

            private static bool Prefix(string triggered_by, string hack_type, string hacked_value)
            {
                Msg(Colors.Warning,
                    $"WARNING: Hack detected: Trigger: {triggered_by}, Type: {hack_type}, Value: {hacked_value}");
                return false;
            }
        }

        [HarmonyPatch]
        private static class MapLoaderPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(MapLoader).GetMethod("Load");
            }

            [SuppressMessage("ReSharper", "RedundantAssignment")]
            private static void Prefix(ref CoopDivision coopDivisionType)
            {
                coopDivisionType = CoopDivision.FREE_FOR_ALL;
            }
        }

        [HarmonyPatch]
        private static class MapSetPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(MapSet).GetMethod("GetCoopMapDivision");
            }
            
            [SuppressMessage("ReSharper", "RedundantAssignment")]
            private static void Postfix(ref CoopDivision __result)
            {
                __result = CoopDivision.FREE_FOR_ALL;
            }
        }

        [HarmonyPatch]
        private class PatchCoopLobbyScreen
        {
            private static MethodBase TargetMethod()
            {
                return typeof(CoopLobbyScreen).GetMethod("Open");
            }

            private static void Postfix(ref CoopLobbyScreen __instance)
            {
                __instance.debugReadyPanel.SetActiveRecursively(true);
                foreach (var instanceHackerStatus in __instance.hackerStatuses)
                {
                    instanceHackerStatus.value.TrySetResult(false);
                    instanceHackerStatus.value.Finish(false);
                }
            }
        }

        [HarmonyPatch]
        private class PatchInstaMonkeyModel
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return
                    from method in typeof(InstaTowerModel).GetMethods()
                    where !method.Name.Contains("_")
                    where method.Name.Contains("Tier")
                    select method;
            }

            private static void Prefix(InstaTowerModel __instance)
            {
                __instance.Quantity = 255;
                __instance.quantity = new KonFuze_NoShuffle(255);
            }
        }

        [HarmonyPatch]
        private class PatchGameModel
        {
            private static MethodBase TargetMethod()
            {
                return typeof(GameModelLoader).GetMethod("Load");
            }

            private static GameModel Postfix(GameModel __result)
            {
                if (NotNull(__result)) Msg(Colors.Debug, "Round set: " + __result.GetRoundSet().name);

                return __result;
            }
        }

        [HarmonyPatch]
        private class PatchGameIsModded
        {
            private static MethodBase TargetMethod()
            {
                return typeof(Game).GetProperty("IsModdedClient").GetGetMethod();
            }

            private static void Postfix(ref bool __result)
            {
                //Msg(Colors.Success, "Overrode mod status");
                __result = false;
            }
        }
        [HarmonyPatch]
        private class PatchGameCanUseTrophyStore
        {
            private static MethodBase TargetMethod()
            {
                return typeof(Game).GetProperty("CanUseTrophyStore").GetGetMethod();
            }

            private static void Postfix(ref bool __result)
            {
                __result = true;
            }
        }

        [HarmonyPatch]
        private class PatchStackingIsUnique
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return
                    from method in getMethodFromType(typeof(Model), "Clone")
                    where HarmonyMethodExtensions.GetFromMethod(method).Count == 0
                    let type = method.DeclaringType
                    where type.FullName.StartsWith("Assets.Scripts.Models.Towers.Behaviors")
                    where HarmonyMethodExtensions.GetFromType(type).Count() == 0
                    where type.GetProperties().Where(info => validProperties.ContainsKey(info.Name)).Count() != 0
                    select method;
            }

            private static void Prefix(ref Model __instance, MethodBase __originalMethod)
            {
                if (ReferenceEquals(null, __instance))
                    return;
                foreach (var key in validProperties.Keys)
                {
                    var prop = __instance.GetType().GetProperty(key);
                    if (prop != null)
                    {
                        /*
                        var target = GeneralExtensions.FullDescription(__originalMethod) + "-" + prop.Name;
                        if (!callCounts.ContainsKey(target))
                        {
                            callCounts.Add(target, 1);
                        }
                        else
                        {
                            callCounts[target]++;
                            if (callCounts[target] % 10000 == 0)
                                Msg(ConsoleColor.DarkYellow, target + ", Count:" + callCounts[target]);
                        }*/

                        prop.SetValue(__instance, validProperties[key]);
                    }
                }
            }
        }

        //Assets.Scripts.Models.Towers.Behaviors.TowerBehaviorBuffModel/SupportModel
        [HarmonyPatch]
        private class BuffIndicatorPatch
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return getMethodFromType(typeof(SupportModel), "GetBuffIndicatorModel").Concat(
                        getMethodFromType(typeof(TowerBehaviorBuffModel), "GetBuffIndicatorModel"))
                    .Concat(
                        getMethodFromType(typeof(OverclockModel), "GetBuffIndicatorModel")).Concat(
                        getMethodFromType(typeof(OverclockPermanentModel), "GetBuffIndicatorModel")
                    );
            }

            private static void Postfix(ref BuffIndicatorModel __result)
            {
                if (ReferenceEquals(null, __result))
                    return;
                __result.stackable = true;
                __result.maxStackSize = 999;
            }
        }

        // Remove per-round activation limit for abilities
        [HarmonyPatch]
        private class AbilityModelPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(AbilityModel).GetMethod("Clone");
            }

            private static void Prefix(ref AbilityModel __instance)
            {
                if (ReferenceEquals(null, __instance))
                    return;

                __instance.maxActivationsPerRound = int.MaxValue;
            }
        }

        [HarmonyPatch]
        private class NetworkedSimPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(NetworkedUnityToSimulation).GetMethod("InitMap");
            }

            private static void Prefix(ref NetworkedUnityToSimulation __instance)
            {
                netsim = __instance;
            }
        }

        [HarmonyPatch]
        private class TitleScreenPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(TitleScreen).GetMethod("Start");
            }

            private static void Postfix(ref TitleScreen __instance)
            {
                __instance.playButtonClicked = true;
            }
        }

        // Adblock - Part 1
        [HarmonyPatch(typeof(SkuSettingsHelper), "DeserialiseResponseAndCheckSignature")]
        private static class SkuSettingsHelperPatch
        {
            private static void Postfix(ref SkuSettingsHelper __instance, ref string __result)
            {
                var JsonObject = JObject.Parse(__result);
                var Targets =
                    JsonObject.SelectTokens(@"$.settings.events.[?(@.type =~ /^.*Sale$/ || @.type == 'newsbanner')]");
                while (Targets.Count() > 0) Targets.First().Remove();
                __result = JsonConvert.SerializeObject(JsonObject);
                //Msg(Colors.Debug, "Modified JSON response");
            }
        }

        // More debug!
        [HarmonyPatch(typeof(SkuSettingsHelper), "GetFileNameFromBranchesInfo")]
        private static class DumpSkuSettingsPatch
        {
            private static void Postfix(ref SkuSettingsHelper __instance, ref string __result, ref string json,
                ref int appID, ref int skuID)
            {
                Msg(Colors.Debug, $"Fetching Build:{appID}/{skuID} from file {__result}");
            }
        }

        // Adblock - Part II
        [HarmonyPatch]
        private class CrossPromoPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(GameEventsScreen).GetMethod("AddCrossPromoBanner");
            }

            private static bool Prefix(ref string imageUrl, ref string linkUrl)
            {
                return false;
            }
        }

        // Catching desyncs
        [HarmonyPatch]
        private class DesyncPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(AnalyticsManager).GetMethod("DesyncDetected");
            }

            private static void Prefix(int roundReached)
            {
                Msg(Colors.Error, "Desynced on round " + roundReached);
            }
        }

        [HarmonyPatch]
        private static class DumpWebStuff
        {
            private static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>
                {
                    typeof(HttpRequestFactory).GetMethod("Get"),
                    typeof(HttpRequestFactory).GetMethod("Post"),
                    typeof(UwrHttpRequestFactory).GetMethod("Get"),
                    typeof(UwrHttpRequestFactory).GetMethod("Post")
                };
            }

            public static Il2CppStructArray<byte> Inflate(Il2CppStructArray<byte> data)
            {
                var ms = new MemoryStream();
                var zlibStream = new ZlibStream(ms, CompressionMode.Decompress);
                zlibStream.Write(data, 0, data.Length);
                return ms.ToArray();
            }

            private static IEnumerator WaitHttp(Task<HttpResponse> http, string url)
            {
                while (!http.IsCompleted && !http.IsFaulted) yield return new WaitForEndOfFrame();

                if (!http.IsFaulted)
                {
                    var result = http.Result;
                    var fullOutput = "Request URL: ";
                    fullOutput += url;

                    if (!ReferenceEquals(null, result.Etag))
                    {
                        fullOutput += "\tEtag: ";
                        fullOutput += result.Etag;
                    }

                    fullOutput += "\n\t|";
                    if (http.Result.Data.Count > 0)
                    {
                        var resultString = string.Empty;
                        // Case: ZLIB
                        if (result.Data[0].ToString() == "123")
                        {
                            try
                            {
                                var binData = Convert.FromBase64String(resultString);
                                var resultArray = Inflate(binData);
                                if (!ReferenceEquals(null, resultArray))
                                {
                                    resultString = string.Empty;
                                    foreach (var nom in resultArray)
                                        resultString += (char) nom;

                                    fullOutput += "ZLIB|";
                                }
                            }
                            catch
                            {
                                // Pass
                            }
                        }
                        else // Case: no ZLIB
                        {
                            try
                            {
                                resultString = TheRealStephenCypher.Decrypt(result.Data, false);
                            }
                            catch
                            {
                                // Pass
                            }
                        }

                        if (resultString != string.Empty)
                        {
                            fullOutput += "Decrypted|";
                        }
                        else
                        {
                            foreach (var nom in result.Data)
                                resultString += (char) nom;
                        }
                        if (resultString.Length > 1)
                        {
                            /*
                            if (!resultString.Contains("ZLIB")){
                                var JsonObject = JObject.Parse(resultString);
                                if (JsonObject.ContainsKey("Default"))
                                {
                                    ((JObject) JsonObject["Default"])["branch"] = "26.0";
                                    Msg(Colors.Debug, "Maybe memes");
                                }
                                //__result = JsonConvert.SerializeObject(JsonObject);
                            }*/
                            
                            try
                            {
                                JObject jsObject = JObject.Parse(resultString);
                                JsonSerializerSettings jsSettings = new JsonSerializerSettings();
                                jsSettings.Formatting = Formatting.Indented;
                                jsSettings.StringEscapeHandling = StringEscapeHandling.EscapeNonAscii;
                                resultString = JsonConvert.SerializeObject(jsObject["data"], jsSettings);
                                resultString = resultString.Replace("\\\"", "\"");
                            }
                            catch
                            {
                                // Pass
                            }
                            fullOutput += " content:" + resultString;
                        }
                        else
                        {
                            fullOutput += "no content[code -1]";
                        }
                    }
                    else
                    {
                        fullOutput += $"no content[HTTP {http.Result.StatusCode}]";
                    }

                    Msg(Colors.DebugWarn, fullOutput);
                }
            }

            private static void BeginCoro(ref Task<HttpResponse> task, string url)
            {
                MelonCoroutines.Start(WaitHttp(task, url));
            }

            private static void Prefix(MethodBase __originalMethod, ref object __0, ref object __1)
            {
                // eTag
                if (NotNull(__1) && __1 is string)
                    //Msg(__1);
                    __1 = "";

                if (NotNull(__0) && __0 is string)
                {
                    var url = __0 as string;
                    if (!url.Contains("staging"))
                    {
                       //url = url.Replace("static-api.", "static-api-staging.");
                       //__0 = url;
                    }

                    Msg($"{__originalMethod.Name} - {__0}");
                }
            }

            private static void Postfix(ref Task<HttpResponse> __result, string url)
            {
                //Msg($"Fetching {url}[HRP]");
                //BeginCoro(ref __result, url);
            }
        }
    }
}