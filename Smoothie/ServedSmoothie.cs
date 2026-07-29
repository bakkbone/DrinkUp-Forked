using ApplianceLib.Api.Prefab;
using Kitchen;
using KitchenData;
using KitchenDrinksMod.Util;
using KitchenLib.Customs;
using KitchenLib.Utils;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace KitchenDrinksMod.Smoothie
{
    public class ServedSmoothie : CustomItemGroup<SmoothieItemGroupView>, IColorblindLabelPositionOverride, IColorblindLabelVisibilityOverride, IColorblindLabelSizeOverride
    {
        public override string UniqueNameID => "Served Smoothie";
        public Vector3 ColorblindLabelPosition => new(0, 0.3f, 0);
        public bool ColorblindLabelVisibleWhenColorblindEnabled => true;
        public bool ColorblindLabelVisibleWhenColorblindDisabled => true;
        public Vector2 ColorblindLabelOffsetMinAdjust => new(-0.3f, 0);
        public Vector2 ColorblindLabelOffsetMaxAdjust => new(0.3f, 0);
        public override GameObject Prefab => Prefabs.Create("ServedSmoothie");
        public override bool CanContainSide => true;
        public override int RewardOverride => 7;
        public override List<ItemGroup.ItemSet> Sets => new()
        {
            new ItemGroup.ItemSet
            {
                Items = new()
                {
                    Refs.Cup
                },
                IsMandatory = true,
                Min = 1,
                Max = 1,
            },
            new ItemGroup.ItemSet
            {
                Items = SmoothieIngredients.BaseIngredients
                    .Select(ingredient => ingredient.BlendedEquivalent)
                    .ToList(),
                Min = SmoothieIngredients.BaseIngredients.Count,
                Max = SmoothieIngredients.BaseIngredients.Count,
                IsMandatory = true,
            },
            new ItemGroup.ItemSet
            {
                Items = SmoothieIngredients.LiquidIngredients
                    .Select(ingredient => ingredient.BlendedEquivalent)
                    .ToList(),
                Min = 1,
                Max = 1,
                IsMandatory = true,
            },
            new ItemGroup.ItemSet
            {
                Items = SmoothieIngredients.FillerIngredients
                    .Select(ingredient => ingredient.BlendedEquivalent)
                    .ToList(),
                Min = 2,
                Max = 2,
                RequiresUnlock = true,
                IsMandatory = true,
            }
        };
        public override List<IItemProperty> Properties => new()
        {
            new CPreventItemMerge
            {
                Condition = MergeCondition.NoMerge
            }
        };

        public override void OnRegister(ItemGroup itemGroup)
        {

        }

        public override void SetupPrefab(GameObject prefab)
        {
            PrefabBuilder.AttachCup(prefab, Mod.Bundle.LoadAsset<Material>("drinkup_smoothie_liquid"), true);
            prefab.ApplyMaterialToChild("Cup(Clone)/Model/Straw", MaterialUtils.GetMaterialArray("drinkup_orange_straw"));

            // Setup the ItemGroupView
            prefab.GetComponent<SmoothieItemGroupView>()?.Setup(prefab);
        }
    }

    public class SmoothieItemGroupView : ItemGroupView, IItemSpecificView
    {
        public struct LiquidColor
        {
            public Item Item;
            public Color Color;
        }

        public struct ColourBlindLabel
        {
            public Item Item;
            public string Text;
            public string Icon;

            public string Label => Mod.ColorblindUseText ? Text : Icon;
        }

        public List<LiquidColor> ColorModifiers;
        public List<ColourBlindLabel> ColorblindLabels;
        public GameObject Liquid;
        public Dictionary<int, Color> ColorModifierMap = new();

        public void Setup(GameObject prefab)
        {
            ComponentGroups = new()
            {

            };

            ColorblindLabels = SmoothieIngredients.AllIngredients
                .Select(ingredient => new ColourBlindLabel
                {
                    Item = ingredient.BlendedEquivalent,
                    Text = ingredient.ColorblindLabel,
                    Icon = ingredient.IconString
                })
                .ToList();

            Liquid = prefab.GetChild("Cup(Clone)/Model/Liquid");
        }

        private void RebuildColorMap()
        {
            // See KitchenDrinksMod.Smoothie.SmoothieItemGroups.BlenderCupItemGroupView.RebuildColorMaps
            // for the full explanation. Confirmed via the [SmoothieDebug][IDTable] cross-reference
            // that the served item's components are represented entirely by blended-equivalent IDs
            // (e.g. milk's raw ID never appears once served, only its BlendedEquivalent ID does) -
            // so both are included here for safety/consistency with the jug, but in practice only
            // the blended-equivalent branch ever matches for this particular view.
            ColorModifiers = SmoothieIngredients.AllIngredients
                .SelectMany(ingredient => {
                    var list = new List<LiquidColor>();
                    if (ingredient.Item != null)
                    {
                        list.Add(new LiquidColor { Item = ingredient.Item, Color = ingredient.Color });
                    }
                    if (ingredient.BlendedEquivalent != null)
                    {
                        list.Add(new LiquidColor { Item = ingredient.BlendedEquivalent, Color = ingredient.Color });
                    }
                    return list;
                })
                .ToList();

            ColorModifierMap = ColorModifiers.ToDictionary(el => el.Item.ID, el => el.Color);
        }

        public override void PerformUpdate(int item_id, ItemList components, bool is_order = false)
        {
            var componentsStr = new System.Text.StringBuilder();
            foreach (var c in components) { componentsStr.Append(c).Append(','); }
            Mod.LogInfo($"[SmoothieDebug][Served] PerformUpdate CALLED. item_id={item_id} components=[{componentsStr}]");
            try
            {
                RebuildColorMap();
                Mod.LogInfo($"[SmoothieDebug][Served] ColorModifierMap keys=[{string.Join(",", ColorModifierMap.Keys)}]");
            }
            catch (System.Exception e)
            {
                Mod.LogInfo($"[SmoothieDebug][Served] EXCEPTION in RebuildColorMap: {e}");
            }
            try
            {
            if (SubviewPrefab != null)
            {
                if (Subview == null)
                {
                    Subview = Instantiate(SubviewPrefab).GetComponent<ItemGroupView>();
                    Transform transform = Subview.transform;
                    transform.parent = SubviewContainer.transform;
                    transform.localScale = Vector3.one;
                    transform.localPosition = Vector3.zero;
                }
                Subview.PerformUpdate(item_id, components);
            }
            if (DrawComponents == null)
            {
                Setup();
            }
            foreach (KeyValuePair<int, ComponentGroup> keyValuePair in DrawComponents)
            {
                if (keyValuePair.Value.Objects != null && keyValuePair.Value.Objects.Count > 0)
                {
                    foreach (GameObject gameObject in keyValuePair.Value.Objects)
                    {
                        gameObject.SetActive(false);
                    }
                }
                else
                {
                    keyValuePair.Value.GameObject.SetActive(false);
                }
            }
            AddComponent(item_id);
            foreach (int num in components)
            {
                AddComponent(num);
            }
            if (ColourblindLabel != null && ComponentLabels != null)
            {
                var stringBuilder = new StringBuilder();
                foreach (ColourBlindLabel colourBlindLabel in ColorblindLabels)
                {
                    foreach (int num2 in components)
                    {
                        if (colourBlindLabel.Item.ID == num2)
                        {
                            stringBuilder.Append(colourBlindLabel.Label);
                        }
                    }
                }
                ColourblindLabel.text = stringBuilder.ToString();
            }

            List<Color> colors = new();
            foreach (var num in components)
            {
                if (ColorModifierMap.TryGetValue(num, out var color))
                {
                    colors.Add(color);
                }
            }

            var liquidRenderer = Liquid.GetComponent<MeshRenderer>();
            Mod.LogInfo($"[SmoothieDebug][Served] colors.Count={colors.Count} Liquid={(Liquid == null ? "NULL" : Liquid.name)} liquidRenderer.sharedMaterial={(liquidRenderer == null || liquidRenderer.sharedMaterial == null ? "NULL" : liquidRenderer.sharedMaterial.name)}");
            if (colors.Count == 0)
            {
                Liquid.SetActive(false);
            }
            else
            {
                var res = new Color(0, 0, 0, 0);
                foreach (var color in colors)
                {
                    res += color;
                }
                var finalColor = res / colors.Count;

                Material mat = new(liquidRenderer.sharedMaterial);
                mat.SetColor("_Color0", finalColor);

                Liquid.SetActive(true);

                liquidRenderer.materials = new Material[] { mat, mat, mat, mat };
            }
            }
            catch (System.Exception e)
            {
                Mod.LogInfo($"[SmoothieDebug][Served] EXCEPTION in rest of PerformUpdate: {e}");
            }
        }
    }
}
