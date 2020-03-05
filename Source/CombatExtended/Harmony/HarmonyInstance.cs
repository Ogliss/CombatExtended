using HarmonyLib;
using System;
using System.Reflection;

namespace CombatExtended.HarmonyCE
{
    internal class HarmonyInstance
    {
        internal static HarmonyInstance Create(string v)
        {
            throw new NotImplementedException();
        }

        internal void PatchAll(Assembly assembly)
        {
            throw new NotImplementedException();
        }

        internal void PatchAll(ConstructorInfo constructor, object p1, object p2, HarmonyMethod harmonyMethod)
        {
            throw new NotImplementedException();
        }

        internal void Patch(MethodInfo method, object p1, object p2, HarmonyMethod harmonyMethod)
        {
            throw new NotImplementedException();
        }

        internal void Patch(MethodBase method, object p1, object p2, HarmonyMethod harmonyMethod)
        {
            throw new NotImplementedException();
        }

        internal void Patch(MethodInfo methodInfo, object p, HarmonyMethod harmonyMethod)
        {
            throw new NotImplementedException();
        }
    }
}