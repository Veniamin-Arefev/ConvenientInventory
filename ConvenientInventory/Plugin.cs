using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using PSS;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Wish;

namespace ConvenientInventory
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private const int inventoryMaxSlots = 50; // Inventory.maxSlots
        private const int hotBarMaxSlots = 10;

        private const string outlineComponentName = "favorite outline";

        private Harmony harmony = new Harmony(PluginInfo.PLUGIN_GUID);
        public static ManualLogSource logger;

        private static ConfigEntry<bool> isHotbarAlwaysFavorite;
        private static ConfigEntry<string> QuickStackKey;
        private static KeyCode QuickStackKeyCode;
        private static ConfigEntry<string> QuickSortKey;
        private static KeyCode QuickSortKeyCode;

        public static ConfigEntry<bool>[] favoriteSlots = new ConfigEntry<bool>[inventoryMaxSlots];
        public static Dictionary<ConfigEntry<bool>, int> configSlotToIndex = new Dictionary<ConfigEntry<bool>, int>();

        public void OnFavoritesSettingChanged(object sender, EventArgs eventArgs)
        {
            ConfigEntry<bool> configEntry = (ConfigEntry<bool>)sender;
            int slotIndex;
            configSlotToIndex.TryGetValue(configEntry, out slotIndex);
            if (Player.Instance)
            {
                ToggleOutline(Player.Instance.Inventory._slots[slotIndex].gameObject);
            }
        }

        public void OnQuickStackKeyChanged(object sender, EventArgs eventArgs)
        {
            ConfigEntry<string> configEntry = (ConfigEntry<string>)sender;
            if (Enum.TryParse<KeyCode>(configEntry.Value, out QuickStackKeyCode))
            {
                logger.LogInfo($"Changed QuickStackKey to {configEntry.Value}");
            }
            else
            {
                logger.LogError($"Failed to find key named {configEntry.Value}");
            }

        }

        public void OnQuickSortKeyChanged(object sender, EventArgs eventArgs)
        {
            ConfigEntry<string> configEntry = (ConfigEntry<string>)sender;
            if (Enum.TryParse<KeyCode>(configEntry.Value, out QuickSortKeyCode))
            {
                logger.LogInfo($"Changed QuickSortKey to {configEntry.Value}");
            }
            else
            {
                logger.LogError($"Failed to find key named {configEntry.Value}");
            }

        }


        private void Awake()
        {
            logger = this.Logger;
            try
            {
                isHotbarAlwaysFavorite = this.Config.Bind<bool>("General", "Hotbar always marked as favorite", true, "Hotbar cant be sorted or auto-stacked to chests");
                QuickStackKey = this.Config.Bind<string>("General", "Quick stack to nearby chests Key", KeyCode.G.ToString(), "Unity Keycode to quick stack to nearby chests");
                Enum.TryParse<KeyCode>(QuickStackKey.Value, out QuickStackKeyCode);
                QuickStackKey.SettingChanged += OnQuickStackKeyChanged;

                QuickSortKey = this.Config.Bind<string>("General", "Quick sort current inventory Key", KeyCode.Mouse2.ToString(), "Unity Keycode to quick sort your inventory");
                Enum.TryParse<KeyCode>(QuickSortKey.Value, out QuickSortKeyCode);
                QuickSortKey.SettingChanged += OnQuickSortKeyChanged;

                for (int i = 0; i < inventoryMaxSlots; i++)
                {
                    ConfigEntry<bool> currentSlotConfig = this.Config.Bind<bool>(
                        i < hotBarMaxSlots ? "Hotbar Slots" : "Inventory Slots",
                        $"Slot #{i + 1:00} marked favorite",
                        false,
                        "True means that slot will be favorite on game start");
                    currentSlotConfig.SettingChanged += OnFavoritesSettingChanged;
                    configSlotToIndex.Add(currentSlotConfig, i);
                    favoriteSlots[i] = currentSlotConfig;
                }
                harmony.PatchAll();

                Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} v{PluginInfo.PLUGIN_VERSION} is loaded!");
            }
            catch (Exception e)
            {
                logger.LogError($"{PluginInfo.PLUGIN_GUID} Awake failed: " + e);
            }
        }

        [HarmonyPatch(typeof(Inventory), "Sort")]
        [HarmonyPatch("Sort", typeof(int), typeof(int))]
        class HarmonyPatch_Sort
        {
            private static bool Prefix(Inventory __instance, int minSlot, int maxSlot)
            {
                if (__instance == Player.Instance.Inventory)
                {
                    int num = maxSlot - minSlot;
                    int skippedCount = 0;
                    List<SlotItemData> list = new List<SlotItemData>(num);
                    for (int i = minSlot; i < maxSlot; i++)
                    {
                        if (favoriteSlots[i].Value)
                        {
                            skippedCount += 1;
                            continue;
                        }
                        SlotItemData slotItemData = __instance.Items[i];
                        list.Add(new SlotItemData(slotItemData.item, slotItemData.amount, slotItemData.slotNumber, slotItemData.slot));
                    }

                    list = list.OrderBy((SlotItemData x) => (x.item.ID() == 0) ? 999999999999999L : (x.item.ID() * 10000 + x.amount)).ToList();
                    int offset = 0;
                    for (int j = 0; j < num - skippedCount; j++)
                    {
                        int num2 = minSlot + j + offset;
                        while (favoriteSlots[num2].Value)
                        {
                            offset += 1;
                            num2 = minSlot + j + offset;
                        }
                        SlotItemData slotItemData2 = list[j];
                        __instance.Items[num2].item = slotItemData2.item;
                        __instance.Items[num2].id = slotItemData2.item.ID();
                        __instance.Items[num2].amount = slotItemData2.amount;
                        __instance.SetupItemIcon(num2);
                    }
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(Inventory), "TransferSimilarToOtherInventory")]
        class HarmonyPatch_TransferSimilarToOtherInventory
        {
            //this is copy paste from original game code, ALWAYS check when updating to never version
            private static HashSet<Inventory> OriginalTransferSimilarToOtherInventory(Inventory mainInventory, Inventory otherInventory)
            {
                HashSet<int> hashSet = new HashSet<int>(); // ids that can be transfered to other
                HashSet<int> hashSet2 = new HashSet<int>(); // dont have any space for this id
                HashSet<Inventory> hashSet3 = new HashSet<Inventory>(); // need to update them
                foreach (SlotItemData item in otherInventory.Items)
                {
                    for (int num = Mathf.Min(mainInventory.maxSlots - 1, mainInventory.Items.Count - 1); num >= 0; num--)
                    {
                        //logger.LogError($"Get on index:{num}");
                        if (isHotbarAlwaysFavorite.Value && num < hotBarMaxSlots)
                        {
                            continue;
                        }
                        if (favoriteSlots[num].Value)
                        {
                            continue;
                        }
                        SlotItemData slotItemData = mainInventory.Items[num];
                        if (slotItemData.item != null && Database.ValidID(slotItemData.item.ID()) && item.item.Equals(slotItemData.item))
                        {
                            if (otherInventory.CanAcceptItem(slotItemData.item, slotItemData.amount, out var amountToAccept))
                            {
                                otherInventory.AddItem(slotItemData.item, amountToAccept, 0, sendNotification: false);
                                mainInventory.RemoveItemAt(num, amountToAccept);
                                hashSet3.Add(otherInventory);
                            }
                            hashSet.Add(item.item.ID());
                        }
                    }
                }
                foreach (SlotItemData item2 in mainInventory.Items)
                {
                    //logger.LogError($"Get2 on index:{item2.slotNumber}");
                    if (isHotbarAlwaysFavorite.Value && item2.slotNumber < hotBarMaxSlots)
                    {
                        continue;
                    }
                    if (item2.slotNumber < inventoryMaxSlots && favoriteSlots[item2.slotNumber].Value)
                    {
                        continue;
                    }
                    int id = item2.id;
                    if (id <= 0 || !hashSet.Contains(id) || hashSet2.Contains(id))
                    {
                        continue;
                    }
                    if (otherInventory.CanAcceptItem(item2.item, item2.amount, out var amountToAccept2))
                    {
                        if (amountToAccept2 < item2.amount)
                        {
                            hashSet2.Add(id);
                        }
                        otherInventory.AddItem(item2.item, amountToAccept2, 0, sendNotification: false);
                        mainInventory.RemoveItemAt(item2.slotNumber, amountToAccept2);
                        hashSet3.Add(otherInventory);
                    }
                    else
                    {
                        hashSet2.Add(id);
                    }
                }
                return hashSet3;
            }
            private static bool Prefix(Inventory __instance, ref Inventory otherInventory, ref HashSet<Inventory> __state)
            {
                //logger.LogError("TransferSimilarToOtherInventory Prefix: main logic + save state");
                bool foundChest = ChestManager.associatedChests.TryGetValue(otherInventory, out var chest);
                string targetChestName = chest?.chestName?.text;
                //logger.LogError($"Other inv name is {targetChestName}, found chest: {foundChest}");
                if (targetChestName is not null && targetChestName.StartsWith("~exc"))
                {
                    __state = new HashSet<Inventory>();
                }
                else
                {
                    __state = OriginalTransferSimilarToOtherInventory(__instance, otherInventory);
                }
                return false;
            }

            private static void Postfix(ref HashSet<Inventory> __result, HashSet<Inventory> __state)
            {
                //logger.LogError("TransferSimilarToOtherInventory Postfix: restore state");
                __result = __state;
            }
        }

        public static Component GenOutlineComponent(GameObject obj)
        {
            Component[] comps = obj.GetComponents(typeof(Outline));
            int component_index = Array.FindIndex(comps, comp => comp.name == outlineComponentName);
            return (component_index != -1 ? comps[component_index] : null);
        }
        public static void ToggleOutline(GameObject obj)
        {
            var old_outline = GenOutlineComponent(obj);
            if (old_outline != null)
            {
                Destroy(old_outline);
            }
            else
            {
                var outline = obj.AddComponent<Outline>();
                outline.useGraphicAlpha = false;
                outline.effectColor = new Color(1f, 0.84314f, 0f, 1f);
                outline.effectDistance = new Vector2(2, -2);
                outline.name = outlineComponentName;
            }
        }

        public static bool HandleSlotClick(Slot slot)
        {
            if (Input.GetKey("left alt") &&
                slot.slotNumber < inventoryMaxSlots &&
                slot.inventory == Player.Instance.Inventory &&
                Player.Instance.PlayerInventory._panels[0].gameObject.activeInHierarchy
                )
            {
                favoriteSlots[slot.slotNumber].Value ^= true;
                return false;
            }
            return true;
        }


        [HarmonyPatch(typeof(ItemIcon), "OnPointerDown")]
        class HarmonyPatch_ItemIcon_OnPointerDown
        {
            private static bool Prefix(ItemIcon __instance)
            {
                //logger.LogError($"ItemIcon click with number {__instance.slotIndex}");
                return HandleSlotClick(__instance.slot);
            }
        }

        [HarmonyPatch(typeof(Slot), "OnPointerDown")]
        class HarmonyPatch_Slot_OnPointerDown
        {
            private static bool Prefix(Slot __instance)
            {
                //logger.LogError($"Slot click with number {__instance.slotNumber}");
                return HandleSlotClick(__instance);
            }
        }

        [HarmonyPatch]
        class Patches
        {
            [HarmonyPrefix]
            [HarmonyPatch(typeof(Player), "Update")]
            public static void PlayerUpdatePostfix(ref Player __instance)
            {
                if (Player.Instance && Input.GetKeyDown(QuickSortKeyCode))
                {
                    logger.LogInfo("Sort player inventory");
                    Player.Instance.PlayerInventory.SortPlayerInventory();
                }
                if (Player.Instance && Input.GetKeyDown(QuickStackKeyCode))
                {
                    logger.LogInfo("Quick stack to nearby chests");
                    Player.Instance.PlayerInventory.TransferToNearbyChests();
                }
            }
        }

        [HarmonyPatch(typeof(PlayerInventory), "Initialize")]
        class HarmonyPatch_PlayerInventory_Initialize
        {
            private static void Postfix(PlayerInventory __instance)
            {
                //logger.LogError($"PlayerInventory Initialize");
                foreach (Slot slot in __instance._slots)
                {
                    int slotIndex = slot.slotNumber;
                    if (slotIndex < inventoryMaxSlots)
                    {
                        if (favoriteSlots[slotIndex].Value && GenOutlineComponent(slot.gameObject) == null)
                        {
                            ToggleOutline(slot.gameObject);
                        }
                    }
                }
            }
        }
    }
}
