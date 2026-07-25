using HarmonyLib;
using Kitchen;
using KitchenData;
using System;

namespace KitchenDrinksMod.Boba
{
    [HarmonyPatch(typeof(AddDirtItems), "HandleSatisfiedOrder")]
    internal static class GateDirtyCupsPatch
    {
        static bool Prefix(ref COrderAcceptance details)
        {
            bool isOurTea = details.DeliveredItem == Refs.ServedBlackTea.ID
                            || details.DeliveredItem == Refs.ServedMatchaTea.ID
                            || details.DeliveredItem == Refs.ServedTaroTea.ID;

            Mod.LogInfo($"[GateDirtyCupsPatch] HandleSatisfiedOrder for DeliveredItem={details.DeliveredItem}, isOurTea={isOurTea}, CardActive={ThrowOutCupsSystem.CardActive}");

            if (!isOurTea)
            {
                return true;
            }
            return ThrowOutCupsSystem.CardActive; // only proceed (spawn dirty cup) if the card is active
        }
    }

    // Diagnostic: identify which InteractionSystem subclass is throwing MissingMethodException
    // when handling the dirty cup transfer, and suppress it so it doesn't keep crash-looping
    // while we investigate. This does NOT fix the underlying issue - it's purely to gather
    // information and prevent disruption in the meantime.
    [HarmonyPatch(typeof(InteractionSystem), "<OnUpdate>b__24_0")]
    internal static class DiagnoseInteractionCrashPatch
    {
        static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception is MissingMethodException)
            {
                Mod.LogInfo($"[DiagnoseInteractionCrashPatch] Suppressed MissingMethodException in InteractionSystem subclass: {__instance.GetType().FullName}");
                return null; // suppress the exception
            }

            return __exception; // let anything else propagate normally
        }
    }
}