using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using KeyTips.Patches;

namespace KeyTips
{
    /// <summary>
    /// Key Tips（SPT 5.0BE 移植版）插件入口。
    /// 原作 HiddenCirno/KeyTips 1.1.0（MIT）：https://sp-mod.com/mod/2798/key-tips
    /// 形态与上游一致 —— 四个 Harmony Postfix，不改游戏原行为；无服务端部分。
    /// </summary>
    [BepInPlugin(PluginsInfo.GUID, PluginsInfo.NAME, PluginsInfo.VERSION)]
    [BepInDependency("com.sptushonka.core", "5.0.0")]
    public sealed class KeyTipsPlugin : BasePlugin
    {
        public override void Load()
        {
            LocaleManager.Initialize(Config, Log);

            var harmony = new Harmony(PluginsInfo.GUID);
            var patchTypes = new List<Type>
            {
                typeof(DoorLockedNoticePatch),
                typeof(KeyCardDoorLockedNoticePatch),
                typeof(DoorOpenNoticePatch),
                typeof(KeyCardDoorOpenNoticePatch),
            };

            var applied = 0;
            foreach (var type in patchTypes)
            {
                try
                {
                    var patched = harmony.CreateClassProcessor(type).Patch();
                    applied++;
                    Log.LogInfo($"补丁 {type.Name} 已启用，目标：{string.Join(", ", patched.ConvertAll(m => m.FullDescription()))}");
                }
                catch (Exception ex)
                {
                    Log.LogError($"补丁 {type.Name} 启用失败（对应提示不会出现）：{ex}");
                }
            }

            Log.LogInfo($"{PluginsInfo.NAME} {PluginsInfo.VERSION} loaded (SPT 5.0BE)：{applied}/{patchTypes.Count} 个补丁生效。");
        }
    }
}
