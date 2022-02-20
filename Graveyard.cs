#if thesepatchesareexcluded
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Data.MapSets;
using Assets.Scripts.Models;
using Assets.Scripts.Models.Map;
using Assets.Scripts.Unity.Bridge;
using Assets.Scripts.Unity.Localization;
using Assets.Scripts.Unity.Map;
using Assets.Scripts.Unity.UI_New;
using Assets.Scripts.Unity.UI_New.InGame;
using Assets.Scripts.Utils;
using Harmony;
using Il2CppSystem.IO;
using Il2CppSystem.Threading.Tasks;
using MelonLoader;
using Ninjakiwi.DotNetZip.Zlib;
using Ninjakiwi.LiNK;
using Ninjakiwi.LiNK.Net;
using Ninjakiwi.LiNK.Utils;
using UnhollowerBaseLib;
using UnityEngine;
using Map = Assets.Scripts.Simulation.Track.Map;
using static MelonLoader.MelonLogger;


namespace ThisMod
{
    public class UnusedPatches
    {
        [HarmonyPatch]
        class UrlsExtensionsPatch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return
                    from method in typeof(UrlsExtensions).GetMethods()
                    where !method.Name.Contains("_")
                    where !method.IsConstructor
                    where !method.IsAbstract
                    where method.ReturnType == typeof(string)
                    select method;
            }

            static void Postfix(ref string __result)
            {
                Log(Mod.Colors.Debug, "Fetching " + __result);
            }
        }
        
        [HarmonyPatch]
        static class DumpWebStuff
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return new List<MethodBase>()
                {
                    typeof(HttpRequestFactory).GetMethod("Get"),
                    typeof(HttpRequestFactory).GetMethod("Post"),
                    typeof(UwrHttpRequestFactory).GetMethod("Get"),
                    typeof(UwrHttpRequestFactory).GetMethod("Post"),
                    typeof(WwwHttpRequestFactory).GetMethod("Get"),
                    typeof(WwwHttpRequestFactory).GetMethod("Post")
                };
            }
            public static Il2CppStructArray<byte> Inflate(Il2CppStructArray<byte> data)
            {
                var ms = new MemoryStream();
                var zlibStream = new ZlibStream(ms, CompressionMode.Decompress);
                zlibStream.Write(data, 0, data.Length);
                return ms.ToArray();
            }

            static IEnumerator WaitHttp(Task<HttpResponse> http, string url)
            {
                while (!http.IsCompleted && !http.IsFaulted)
                {
                    yield return new WaitForEndOfFrame();
                }

                if (!http.IsFaulted)
                {
                    HttpResponse result = http.Result;
                    string fullOutput = "Request URL: ";
                    fullOutput += url;

                    if (!ReferenceEquals(null, result.Etag))
                    {
                        fullOutput += "\tEtag: ";
                        fullOutput += result.Etag;
                    }
                    
                    fullOutput += "\n\t";

                    if (http.Result.Data.Count > 0)
                    {
                        string resultString = String.Empty;
                        if (TheRealStephenCypher.CanDecrypt(result.Data))
                        {
                            fullOutput += "|Decrypted| ";
                            resultString += TheRealStephenCypher.Decrypt(result.Data, false);
                        }
                        else
                        {
                            foreach (byte nom in result.Data)
                            {
                                resultString += (char) nom;
                            }
                        }
                        try
                        {
                            var binData = Convert.FromBase64String(resultString);
                            var resultArray = Inflate(binData);
                            if (!ReferenceEquals(null, resultArray))
                            {
                                resultString = String.Empty;
                                foreach (byte nom in resultArray)
                                {
                                    resultString += (char) nom;
                                }

                                fullOutput += "||ZLIB|| ";
                            }
                        }
                        catch{}

                        if (resultString.Length > 1)
                        {
                            
                            if (TheRealStephenCypher.CanDecrypt(resultString))
                            {
                                fullOutput +=
 "||Decrypted|| content:" + TheRealStephenCypher.Decrypt(resultString, false);
                            }
                            else
                            {
                                fullOutput += "content:" + resultString;
                            }
                            
                        }
                        else
                        {
                            fullOutput += "no content";
                        }
                    }
                    else
                    {
                        fullOutput += "no content";
                    }

                    Log(Mod.Colors.DebugWarn, fullOutput);
                }
            }

            static void BeginCoro(Task<HttpResponse> task, string url)
            {
                MelonCoroutines.Start(WaitHttp(task, url));
            }

            static void Postfix(Task<HttpResponse> __result, string url)
            {
                BeginCoro(__result, url);
            }
        }
        
        private static readonly Dictionary<string, string> LocaleOverrides =
            new Dictionary<string, string>
            {
                {"SniperMonkey Description", "Monkey's point and click adventure."}
            };

        [HarmonyPatch]
        private class LocalizationManagerPatch
        {
            private static MethodBase TargetMethod()
            {
                return typeof(LocalizationManager).GetMethod("Load");
            }

            private static IEnumerator ProcessLocalizationManager()
            {
                while (
                    ReferenceEquals(LocalizationManager.instance, null) ||
                    ReferenceEquals(LocalizationManager.instance.textTable, null) ||
                    ReferenceEquals(LocalizationManager.instance.textTable.entries, null)
                )
                    yield return new WaitForEndOfFrame();

                var clone = new Il2CppSystem.Collections.Generic.Dictionary<string, string>(LocalizationManager.instance
                    .textTable.count);

                foreach (var entry in LocalizationManager.instance.textTable)
                    //Log(Colors.Debug, "L:" + LocalizationManager.instance.activeLanguage + " || K:" + entry.key + " || V:" + entry.value);
                    try
                    {
                        if (!ReferenceEquals(LocaleOverrides[entry.Key], null))
                            clone[entry.Key] = LocaleOverrides[entry.Key];
                        else
                            clone[entry.Key] = entry.Value;
                    }
                    catch
                    {
                    }

                LocalizationManager.instance.textTable = clone;
            }

            private static void Postfix()
            {
                MelonCoroutines.Start(ProcessLocalizationManager());
            }
        }

        public override void OnLevelWasInitialized(int level)
        {
            base.OnLevelWasInitialized(level);
            if (!ReferenceEquals(DebugMenus.instance, null))
            {
                DebugMenus.instance.Destroy();
            }
            Log("OnLevelWasInitialized " + SceneManager.GetActiveScene().guid);
        }
    }
}
#endif

/*
case KeyCode.KeypadDivide:
    if (ReferenceEquals(null, getInGame()))
    {
        break;
    }
    if (ReferenceEquals(null, DebugMenus.instance))
    {
        
        var prefab = ResourceLoader.Load<GameObject>(
            "Assets/UI - New/InGameUi/Prefabs/DebugObjects.prefab"
        );
        DebugMenus instance = Object.Instantiate(prefab.GetComponentInChildren<DebugMenus>(),
            getInGame().transform); 
        instance.Initialise();
        instance.enabled = true;
        instance.debugPerfmanceStats.SetActiveRecursively(true);
        prefab.SetActiveRecursively(true);
                         
GameObject menu = Object.Instantiate(ResourceLoader.Load<GameObject>(
"Assets/UI - New/InGameUi/Prefabs/DebugObjects.prefab"
), getInGame().transform);
DebugMenus instance = menu.GetComponentInChildren<DebugMenus>();
instance.Initialise();
menu.SetActive(true);
instance.debugPerfmanceStats.SetActiveRecursively(true);
}
else
{
DebugMenus instance = new DebugMenus();
instance.Initialise();
instance.gameObject.SetActive(true);
instance.debugPerfmanceStats.SetActiveRecursively(true);
}
break;
*/

/*
       [HarmonyPatch]
       private class BuildRetrievePatch
       {
           static MethodBase TargetMethod()
           {
               return typeof(SkuSettingsHelper).GetMethod("RetrieveByBuild",new Type[]{typeof(string), typeof(float), typeof(bool), typeof(bool)});
           }

           static void Prefix(ref string buildName)
           {
               Msg(Colors.Error, buildName);
               buildName.Replace("25.1", "26.0");
           }
       }*/

// [HarmonyPatch]
// private class MD5Patch
// {
//     static MethodBase TargetMethod()
//     {
//         return typeof(HashUtilities).GetMethod("HexMD5", new Type[] {typeof(string)});
//     }
//
//     static void Prefix(ref string input)
//     {
//         if (input == "11_3525.0")
//         {
//             input = "11_3525.0";
//         }
//     }
//
//     static void Postfix(string __result, string input)
//     {
//         if (__result.ToLower().Contains("a26308"))
//         {
//             Msg(Colors.Error, $"MD5summed {input} to {__result}");
//         }
//     }
// }
// [HarmonyPatch]
// class UrlsExtensionsPatch
// {
//     static IEnumerable<MethodBase> TargetMethods()
//     {
//         return
//             from method in typeof(UrlsExtensions).GetMethods()
//             where !method.Name.Contains("_")
//             where !method.IsConstructor
//             where !method.IsAbstract
//             where method.ReturnType == typeof(string)
//             select method;
//     }
//
//     static void Postfix(ref object __result)
//     {
//         /*
//         if (IsNull(__result))
//         {
//             return;
//         }
//         if ($"{__result}" == "System.String[]")
//         {
//         }
//         else if($"{__result}" == "System.Collections.Generic.Dictionary`2[System.String,System.String]")
//         {
//         }
//         else
//         {
//             string url = __result as string;
//             if (!url.Contains("staging"))
//             {
//                 url = url.Replace("api.", "api-staging.");
//                 __result = url;
//             }
//             Msg(Mod.Colors.Debug, "Fetching " + __result);
//         }*/
//     }
// }


//        [HarmonyPatch]
//        static class DumpWebStuff
//        {
//            static IEnumerable<MethodBase> TargetMethods()
//            {
//                return new List<MethodBase>()
//                {
//                    typeof(HttpRequestFactory).GetMethod("Get"),
//                    typeof(HttpRequestFactory).GetMethod("Post"),
//                    typeof(UwrHttpRequestFactory).GetMethod("Get"),
//                    typeof(UwrHttpRequestFactory).GetMethod("Post"),
//                    typeof(WwwHttpRequestFactory).GetMethod("Get"),
//                    typeof(WwwHttpRequestFactory).GetMethod("Post")
//                };
//            }
//            public static Il2CppStructArray<byte> Inflate(Il2CppStructArray<byte> data)
//            {
//                var ms = new MemoryStream();
//                var zlibStream = new ZlibStream(ms, CompressionMode.Decompress);
//                zlibStream.Write(data, 0, data.Length);
//                return ms.ToArray();
//            }
//
//            static IEnumerator WaitHttp(Task<HttpResponse> http, string url)
//            {
//                while (!http.IsCompleted && !http.IsFaulted)
//                {
//                    yield return new WaitForEndOfFrame();
//                }
//
//                if (!http.IsFaulted)
//                {
//                    HttpResponse result = http.Result;
//                    string fullOutput = "Request URL: ";
//                    fullOutput += url;
//
//                    if (!ReferenceEquals(null, result.Etag))
//                    {
//                        fullOutput += "\tEtag: ";
//                        fullOutput += result.Etag;
//                    }
//                    
//                    fullOutput += "\n\t";
//                    
//                    if (http.Result.Data.Count > 0)
//                    {
//                        string resultString = String.Empty;
//                        if (TheRealStephenCypher.CanDecrypt(result.Data))
//                        {
//                            fullOutput += "|Decrypted| ";
//                            resultString += TheRealStephenCypher.Decrypt(result.Data, false);
//                        }
//                        else
//                        {
//                            foreach (byte nom in result.Data)
//                            {
//                                resultString += (char) nom;
//                            }
//                        }
//
//                        try
//                        {
//                            var binData = Convert.FromBase64String(resultString);
//                            var resultArray = Inflate(binData);
//                            if (!ReferenceEquals(null, resultArray))
//                            {
//                                resultString = String.Empty;
//                                foreach (byte nom in resultArray)
//                                {
//                                    resultString += (char) nom;
//                                }
//
//                                fullOutput += "||ZLIB|| ";
//                            }
//                        }
//                        catch
//                        {
//                            // Pass
//                        }
//
//                        if (resultString.Length > 1)
//                        {
//                            
//                            if (TheRealStephenCypher.CanDecrypt(resultString))
//                            {
//                                fullOutput +=
// "||Decrypted|| content:" + TheRealStephenCypher.Decrypt(resultString, false);
//                            }
//                            else
//                            {
//                                /*
//                                if (!resultString.Contains("ZLIB")){
//                                    var JsonObject = JObject.Parse(resultString);
//                                    if (JsonObject.ContainsKey("Default"))
//                                    {
//                                        ((JObject) JsonObject["Default"])["branch"] = "26.0";
//                                        Msg(Colors.Debug, "Maybe memes");
//                                    }
//                                    //__result = JsonConvert.SerializeObject(JsonObject);
//                                }*/
//                                
//                                fullOutput += "content:" + resultString;
//
//                            }
//                            
//                        }
//                        else
//                        {
//                            fullOutput += "no content[code -1]";
//                        }
//                    }
//                    else
//                    {
//                        fullOutput += $"no content[HTTP {http.Result.StatusCode}]";
//                    }
//
//                    Msg(Mod.Colors.DebugWarn, fullOutput);
//                }
//            }
//
//            static void BeginCoro(ref Task<HttpResponse> task, string url)
//            {
//                MelonCoroutines.Start(WaitHttp(task, url));
//            }
//
//            static void Prefix(MethodBase __originalMethod, ref object __0, ref object __1)
//            {
//                // eTag
//                if (NotNull(__1) && __1 is string)
//                {
//                    //Msg(__1);
//                    __1 = "";
//                }
//                
//                if (NotNull(__0) && __0 is string)
//                {
//                    string url = __0 as string;
//                    if (!url.Contains("staging"))
//                    {
//                        url = url.Replace("api.", "api-staging.");
//                        __0 = url;
//                    }
//                    Msg($"{__originalMethod.Name} - {__0}");
//                }
//            }
//
//            static void Postfix(ref Task<HttpResponse> __result, string url)
//            {
//                //Msg($"Fetching {url}[HRP]");
//                BeginCoro(ref __result, url);
//            }
//        }

//         [HarmonyPatch]
//        static class DumpWebStuff
//        {
//            static IEnumerable<MethodBase> TargetMethods()
//            {
//                return new List<MethodBase>()
//                {
//                    typeof(HttpRequestFactory).GetMethod("Get"),
//                    typeof(HttpRequestFactory).GetMethod("Post"),
//                    typeof(UwrHttpRequestFactory).GetMethod("Get"),
//                    typeof(UwrHttpRequestFactory).GetMethod("Post"),
//                    typeof(WwwHttpRequestFactory).GetMethod("Get"),
//                    typeof(WwwHttpRequestFactory).GetMethod("Post")
//                };
//            }
//            public static Il2CppStructArray<byte> Inflate(Il2CppStructArray<byte> data)
//            {
//                var ms = new MemoryStream();
//                var zlibStream = new ZlibStream(ms, CompressionMode.Decompress);
//                zlibStream.Write(data, 0, data.Length);
//                return ms.ToArray();
//            }
//
//            static IEnumerator WaitHttp(Task<HttpResponse> http, string url)
//            {
//                while (!http.IsCompleted && !http.IsFaulted)
//                {
//                    yield return new WaitForEndOfFrame();
//                }
//
//                if (!http.IsFaulted)
//                {
//                    HttpResponse result = http.Result;
//                    string fullOutput = "Request URL: ";
//                    fullOutput += url;
//
//                    if (!ReferenceEquals(null, result.Etag))
//                    {
//                        fullOutput += "\tEtag: ";
//                        fullOutput += result.Etag;
//                    }
//                    
//                    fullOutput += "\n\t";
//                    
//                    if (http.Result.Data.Count > 0)
//                    {
//                        string resultString = String.Empty;
//                        if (TheRealStephenCypher.CanDecrypt(result.Data))
//                        {
//                            fullOutput += "|Decrypted| ";
//                            resultString += TheRealStephenCypher.Decrypt(result.Data, false);
//                        }
//                        else
//                        {
//                            foreach (byte nom in result.Data)
//                            {
//                                resultString += (char) nom;
//                            }
//                        }
//
//                        try
//                        {
//                            var binData = Convert.FromBase64String(resultString);
//                            var resultArray = Inflate(binData);
//                            if (!ReferenceEquals(null, resultArray))
//                            {
//                                resultString = String.Empty;
//                                foreach (byte nom in resultArray)
//                                {
//                                    resultString += (char) nom;
//                                }
//
//                                fullOutput += "||ZLIB|| ";
//                            }
//                        }
//                        catch
//                        {
//                            // Pass
//                        }
//
//                        if (resultString.Length > 1)
//                        {
//                            
//                            if (TheRealStephenCypher.CanDecrypt(resultString))
//                            {
//                                fullOutput +=
// "||Decrypted|| content:" + TheRealStephenCypher.Decrypt(resultString, false);
//                            }
//                            else
//                            {
//                                /*
//                                if (!resultString.Contains("ZLIB")){
//                                    var JsonObject = JObject.Parse(resultString);
//                                    if (JsonObject.ContainsKey("Default"))
//                                    {
//                                        ((JObject) JsonObject["Default"])["branch"] = "26.0";
//                                        Msg(Colors.Debug, "Maybe memes");
//                                    }
//                                    //__result = JsonConvert.SerializeObject(JsonObject);
//                                }*/
//                                
//                                fullOutput += "content:" + resultString;
//
//                            }
//                            
//                        }
//                        else
//                        {
//                            fullOutput += "no content[code -1]";
//                        }
//                    }
//                    else
//                    {
//                        fullOutput += $"no content[HTTP {http.Result.StatusCode}]";
//                    }
//
//                    Msg(Mod.Colors.DebugWarn, fullOutput);
//                }
//            }
//
//            static void BeginCoro(ref Task<HttpResponse> task, string url)
//            {
//                MelonCoroutines.Start(WaitHttp(task, url));
//            }
//
//            static void Prefix(MethodBase __originalMethod, ref object __0, ref object __1)
//            {
//                // eTag
//                if (NotNull(__1) && __1 is string)
//                {
//                    //Msg(__1);
//                    __1 = "";
//                }
//                
//                if (NotNull(__0) && __0 is string)
//                {
//                    /*string url = __0 as string;
//                    if (!url.Contains("staging"))
//                    {
//                        url = url.Replace("api.", "api-staging.");
//                        __0 = url;
//                    }*/
//                    Msg($"{__originalMethod.Name} - {__0}");
//                }
//            }
//
//            static void Postfix(ref Task<HttpResponse> __result, string url)
//            {
//                //Msg($"Fetching {url}[HRP]");
//                //BeginCoro(ref __result, url);
//            }
//        }


// [HarmonyPatch]
// private class SimPatch
// {
//     private static MethodBase TargetMethod()
//     {
//         return typeof(UnityToSimulation).GetMethod("InitMap");
//     }
//
//     private static void Postfix(ref UnityToSimulation __instance)
//     {
//         /*
//         if (IsNull(InGame.instance.debugTowerGroupPlacer))
//         {
//             InGame.instance.debugTowerGroupPlacer =
//                 new DebugTowerGroupPlacer(TowerSelectionMenu.instance, __instance);
//             InGame.instance.debugTowerGroupPlacer.Process();
//             Msg(Colors.Success, "Added new debugTowerGroupPlacer");
//         }*/
//     }
// }
//
// [HarmonyPatch]
// private class GamePatch
// {
//     private static MethodBase TargetMethod()
//     {
//         return typeof(InGame).GetMethod("CheckAndGiftInsta");
//     }
//
//     private static void Postfix(InGame __instance)
//     {
//         int currentRound = __instance.bridge.GetCurrentRound();
//         int rewardRound = (currentRound + 10) - (currentRound % 10);
//         if(currentRound <= 100 || Math.Abs(currentRound-rewardRound) < 5)
//             return;
//         //__instance.nextInstaReward = (currentRound + 10) - (currentRound % 10);
//
//     }
// }


// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Reflection;
// using System.Runtime.InteropServices;
// using Assets.Scripts.Unity.Player;
// using Harmony;
// using MelonLoader;
// using UnityEngine;
// using static MelonLoader.MelonLogger;
//
// namespace ThisMod
// {
//     public partial class Mod
//     {
//         public static void DebugLogs()
//         {
//             switch (new TestClass().IsModified)
//             {
//                 case true:
//                     Msg(ConsoleColor.Green, "Canary says everything is OK");
//                     break;
//                 case false:
//                     Msg(ConsoleColor.Red, "Canary says shit's fucked!");
//                     break;
//             }
//         }
//
//         #region Debug Classes
//
//         private class TestClass
//         {
//             public bool IsModified
//             {
//                 get => false;
//                 [param: In] set => IsModified = value;
//             }
//         }
//
//         [HarmonyPatch(typeof(TestClass))]
//         [HarmonyPatch("IsModified")]
//         [HarmonyPatch(MethodType.Getter)]
//         private class TestPatch
//         {
//             private static bool Postfix(bool __instance)
//             {
//                 return true;
//             }
//         }
//
//         #endregion
//     }
// }