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

            if (!isOurTea)
            {
                return true;
            }

            if (ThrowOutCupsSystem.CardActive)
            {
                ThrowOutCupsSystem.PendingDirtyCups.Add(new ThrowOutCupsSystem.PendingDirtyCup
                {
                    Group = details.Group,
                    TableSet = details.TableSet,
                    HasSeenEating = false
                });

                Mod.LogInfo($"[GateDirtyCupsPatch] Registered pending dirty cup for Group={details.Group}, TableSet={details.TableSet}.");
            }

            return false;
        }
    }

    [HarmonyPatch(typeof(InteractionSystem), "<OnUpdate>b__24_0")]
    internal static class DiagnoseInteractionCrashPatch
    {
        static Exception Finalizer(object __instance, Exception __exception)
        {
            if (__exception is MissingMethodException)
            {
                Mod.LogInfo($"[DiagnoseInteractionCrashPatch] Suppressed MissingMethodException in InteractionSystem subclass: {__instance.GetType().FullName}");
                return null;
            }

            return __exception;
        }
    }
}