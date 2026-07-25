using HarmonyLib;
using Kitchen;
using KitchenData;

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
                return true; // not one of ours, let the original method run normally
            }

            return ThrowOutCupsSystem.CardActive; // only proceed if the card is active
        }
    }
}