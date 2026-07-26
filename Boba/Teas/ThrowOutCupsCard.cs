using Kitchen;
using KitchenData;
using KitchenLib.Customs;
using KitchenLib.Utils;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace KitchenDrinksMod.Boba
{
    public class ThrowOutCupsCard : CustomUnlockCard
    {
        public const RestaurantStatus RestaurantStatus = (RestaurantStatus)(-628800);

        public override UnlockGroup UnlockGroup => UnlockGroup.Dish;
        public override string UniqueNameID => "ThrowOutCupsCard";
        public override Unlock.RewardLevel ExpReward => Unlock.RewardLevel.Medium;
        public override DishCustomerChange CustomerMultiplier => DishCustomerChange.SmallDecrease;
        public override bool IsUnlockable => true;

        public override List<UnlockEffect> Effects => new()
        {
            new StatusEffect
            {
                Status = RestaurantStatus
            }
        };

        public override List<Unlock> HardcodedRequirements => new()
        {
            Refs.BobaDish
        };

        public override List<(Locale, UnlockInfo)> InfoList => new()
        {
            (Locale.English, LocalisationUtils.CreateUnlockInfo("Dirty Cups", "Customers leave behind their dirty boba cups which must be thrown out", "Hopefully you have a bin!"))
        };
    }

[UpdateAfter(typeof(GroupReceiveDrink))]
    public class ThrowOutCupsSystem : GameSystemBase
    {
        internal static bool CardActive = false;

        internal class PendingDirtyCup
        {
            public Entity Group;
            public Entity TableSet;
            public bool HasSeenEating;
        }

        internal static readonly List<PendingDirtyCup> PendingDirtyCups = new();

        private EntityQuery AllOrderedItems;

        protected override void Initialise()
        {
            AllOrderedItems = GetEntityQuery(new QueryHelper()
                .All(typeof(CWaitingForItem.Marker))
            );
        }

        protected override void OnUpdate()
        {
            CardActive = HasStatus(ThrowOutCupsCard.RestaurantStatus);

            ProcessPendingDirtyCups();

            if (!CardActive)
            {
                return;
            }

            using var orderedItems = AllOrderedItems.ToEntityArray(Allocator.Temp);
            foreach (var entity in orderedItems)
            {
                var buffer = EntityManager.GetBuffer<CWaitingForItem>(entity);
                for (int i = 0; i < buffer.Length; i++)
                {
                    var orderedItem = buffer[i];

                    if (orderedItem.ItemID == Refs.ServedBlackTea.ID || orderedItem.ItemID == Refs.ServedMatchaTea.ID || orderedItem.ItemID == Refs.ServedTaroTea.ID)
                    {
                        orderedItem.DirtItem = Refs.DirtyBobaCup.ID;
                    }

                    buffer[i] = orderedItem;
                }
            }
        }

        private void ProcessPendingDirtyCups()
        {
            if (PendingDirtyCups.Count == 0)
            {
                return;
            }

            for (int i = PendingDirtyCups.Count - 1; i >= 0; i--)
            {
                var pending = PendingDirtyCups[i];

                if (!EntityManager.Exists(pending.Group))
                {
                    PendingDirtyCups.RemoveAt(i);
                    continue;
                }

                bool hasEating = EntityManager.HasComponent<CGroupEating>(pending.Group);

                if (hasEating)
                {
                    var eating = EntityManager.GetComponentData<CGroupEating>(pending.Group);
                    if (eating.RemainingTime <= 0f)
                    {
                        SpawnDirtyCup(pending.TableSet);
                        PendingDirtyCups.RemoveAt(i);
                    }
                    else
                    {
                        pending.HasSeenEating = true;
                    }
                }
                else if (pending.HasSeenEating)
                {
                    SpawnDirtyCup(pending.TableSet);
                    PendingDirtyCups.RemoveAt(i);
                }
            }
        }

        private void SpawnDirtyCup(Entity tableSet)
        {
            var parts = EntityManager.GetBuffer<CTableSetParts>(tableSet);

            if (parts.Length == 0)
            {
                return;
            }

            CTableSetParts firstPart = parts[0];
            Entity dirtyCupEntity = EntityManager.CreateEntity();
            EntityManager.AddComponentData(dirtyCupEntity, new CCreateItem { ID = Refs.DirtyBobaCup.ID });
            EntityManager.AddComponentData(dirtyCupEntity, new CStoredBy { Storage = firstPart });
            EntityManager.GetBuffer<CItemStored>(firstPart).Add(new CItemStored { StoredItem = dirtyCupEntity });

            Mod.LogInfo($"[ThrowOutCupsSystem] Spawned dirty cup entity {dirtyCupEntity} after eating finished.");
        }
    }
}
