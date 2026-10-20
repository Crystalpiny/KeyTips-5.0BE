using System;
using Diz.LanguageExtensions;
using EFT;
using EFT.Communications;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;

namespace KeyTips.Patches
{
    /// <summary>四个补丁共用的取词与弹提示。</summary>
    internal static class KeyTipsFeedback
    {
        /// <summary>钥匙模板 Id → 显示名；游戏里没有对应词条时退回原始 Id。</summary>
        public static string KeyName(string keyId)
        {
            string localizationKey = keyId + " Name";
            string name = LocalizationManager.Instance.LocalizedValue(localizationKey);
            return string.IsNullOrEmpty(name) || name == localizationKey ? keyId : name;
        }

        public static void Show(string message, ENotificationIconType iconType)
        {
            // 5.0BE 只剩这一个 5 参重载（末两位为 Color? 文本颜色 / bool rawText）。
            NotificationManager.DisplayMessageNotification(
                message, ENotificationDurationType.Default, iconType, default, false);
        }

        /// <summary>门与刷卡门的 UnlockOperation 后置：上游两处逻辑完全相同，合并到这里。</summary>
        public static void OnKeyUsed(KeyComponent key, Player player, ref Option<UnlockResult> result)
        {
            if (result.Failed || result.Value == null || !result.Value.Succeed || player == null)
            {
                return;
            }

            string keyName = KeyName(key.Template.KeyId);
            int maxUsage = key.Template.MaximumNumberOfUsage;
            string usageTip = maxUsage > 0
                ? string.Format("key_and_keycard_usage".i18n(), maxUsage - key.NumberOfUsages, maxUsage)
                : string.Empty;

            if (player.IsYourPlayer)
            {
                Show(string.Format("key_and_keycard_using".i18n(), keyName, usageTip), ENotificationIconType.Default);

                if (maxUsage > 0 && key.NumberOfUsages >= maxUsage)
                {
                    Show(string.Format("key_and_keycard_broken".i18n(), keyName), ENotificationIconType.Alert);
                }
            }
            else
            {
                string nickname = player.Profile.Info.Nickname ?? "Nikita Buyanov";
                Show(string.Format("teammate_key_and_keycard_using".i18n(), nickname, keyName, usageTip),
                    ENotificationIconType.Default);
            }
        }
    }

    /// <summary>1) 普通门/需钥匙的可交互物：菜单被锁住时提示需要的钥匙。</summary>
    [HarmonyPatch(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions),
        new[] { typeof(GamePlayerOwner), typeof(WorldInteractiveObject) })]
    internal static class DoorLockedNoticePatch
    {
        [HarmonyPostfix]
        private static void Postfix(WorldInteractiveObject worldInteractiveObject, ref AvailableInteractionState __result)
        {
            if (worldInteractiveObject.DoorState != EDoorState.Locked
                || string.IsNullOrEmpty(worldInteractiveObject.KeyId))
            {
                return;
            }

            if (__result == null || string.IsNullOrEmpty(__result.Error))
            {
                return;
            }

            KeyTipsFeedback.Show(
                string.Format("door_locked".i18n(), KeyTipsFeedback.KeyName(worldInteractiveObject.KeyId)),
                ENotificationIconType.Alert);
        }
    }

    /// <summary>2) 刷卡门：锁住时提示需要的钥匙卡。</summary>
    [HarmonyPatch(typeof(InteractionContextHelper), nameof(InteractionContextHelper.GetAvailableActions),
        new[] { typeof(GamePlayerOwner), typeof(KeycardDoor), typeof(bool) })]
    internal static class KeyCardDoorLockedNoticePatch
    {
        [HarmonyPostfix]
        private static void Postfix(KeycardDoor door, ref AvailableInteractionState __result)
        {
            if (door.DoorState != EDoorState.Locked || string.IsNullOrEmpty(door.KeyId))
            {
                return;
            }

            KeyTipsFeedback.Show(
                string.Format("keycard_door_locked".i18n(), KeyTipsFeedback.KeyName(door.KeyId)),
                ENotificationIconType.Alert);
        }
    }

    /// <summary>3) 普通门用钥匙成功：提示剩余可用次数，用尽时告警。</summary>
    [HarmonyPatch(typeof(WorldInteractiveObject), nameof(WorldInteractiveObject.UnlockOperation))]
    internal static class DoorOpenNoticePatch
    {
        [HarmonyPostfix]
        private static void Postfix(KeyComponent key, Player player, ref Option<UnlockResult> __result)
        {
            KeyTipsFeedback.OnKeyUsed(key, player, ref __result);
        }
    }

    /// <summary>4) 刷卡门同上。</summary>
    [HarmonyPatch(typeof(KeycardDoor), nameof(KeycardDoor.UnlockOperation))]
    internal static class KeyCardDoorOpenNoticePatch
    {
        [HarmonyPostfix]
        private static void Postfix(KeyComponent key, Player player, ref Option<UnlockResult> __result)
        {
            KeyTipsFeedback.OnKeyUsed(key, player, ref __result);
        }
    }
}
