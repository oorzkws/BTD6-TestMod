using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Unity.UI_New.InGame;
using TrackMap = Assets.Scripts.Simulation.Track;

namespace ThisMod
{
    public partial class Mod
    {
        public static InGame getInGame()
        {
            return InGame.instance;
        }

        public static Type[] getAllTypes()
        {
            Type[] types;
            try
            {
                types = typeof(InGame).Assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                types = e.Types;
            }

            return types;
        }

        public static IEnumerable<MethodBase> getMethodFromType(Type targetType, string methodName)
        {
            return
                from type in getAllTypes()
                where type != null && type.TypeInitializer != null
                where type.IsSubclassOf(targetType)
                where !type.IsAbstract
                from method in type.GetMethods()
                where method.Name == methodName
                select method;
        }

        public static bool IsNull(object obj)
        {
            return ReferenceEquals(null, obj);
        }

        public static bool NotNull(object obj)
        {
            return !ReferenceEquals(null, obj);
        }

        public static class Colors
        {
            public static ConsoleColor Success = ConsoleColor.Green;
            public static ConsoleColor Failure = ConsoleColor.DarkRed;
            public static ConsoleColor Error = ConsoleColor.DarkRed;
            public static ConsoleColor Debug = ConsoleColor.DarkCyan;
            public static ConsoleColor DebugWarn = ConsoleColor.Cyan;
            public static ConsoleColor Info = ConsoleColor.Blue;
            public static ConsoleColor Warning = ConsoleColor.DarkYellow;
        }
    }
}