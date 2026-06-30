using HarmonyLib;
using Naninovel;
using System;
using System.Reflection;

namespace HuXiangLianPian.Accessibility
{
    internal sealed class SaveLoadMenuPatcher : IDisposable
    {
        private readonly Harmony _harmony;

        public SaveLoadMenuPatcher()
        {
            _harmony = new Harmony("com.boz700908.HuXiangLianPianAccessibility.saveload");
        }

        public void Apply()
        {
            PatchMenuType("NananaGames.UI.SaveLoadMenu");
            PatchMenuType("Naninovel.UI.SaveLoadMenu");
        }

        public void Dispose()
        {
            try
            {
                _harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"卸载存读档菜单补丁时出错: {e.GetType().Name} - {e.Message}");
            }
        }

        private void PatchMenuType(string typeName)
        {
            try
            {
                Type menuType = AccessTools.TypeByName(typeName);
                if (menuType == null) return;

                MethodInfo loadMethod = AccessTools.Method(menuType, "HandleLoadSlotClicked", new[] { typeof(string) });
                MethodInfo saveMethod = AccessTools.Method(menuType, "HandleSaveSlotClicked", new[] { typeof(string), typeof(int) });
                MethodInfo loadPrefix = AccessTools.Method(typeof(SaveLoadMenuPatcher), nameof(GuardLoadSlotClicked));
                MethodInfo savePrefix = AccessTools.Method(typeof(SaveLoadMenuPatcher), nameof(GuardSaveSlotClicked));

                if (loadMethod != null && loadPrefix != null)
                {
                    _harmony.Patch(loadMethod, prefix: new HarmonyMethod(loadPrefix));
                    Main.Log.LogInfo($"已安装读档槽位防护补丁: {typeName}");
                }

                if (saveMethod != null && savePrefix != null)
                {
                    _harmony.Patch(saveMethod, prefix: new HarmonyMethod(savePrefix));
                    Main.Log.LogInfo($"已安装存档槽位防护补丁: {typeName}");
                }
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"安装存读档菜单补丁时出错: {typeName} - {e.GetType().Name} - {e.Message}");
            }
        }

        private static bool GuardSaveSlotClicked(string slotId)
        {
            try
            {
                if (!SaveLoadGuard.TryGetStateManager(out var stateManager, out var unavailableReason))
                {
                    ScreenReader.Say(unavailableReason);
                    Main.Log.LogWarning($"手动存档被阻止: {unavailableReason}");
                    return false;
                }

                if (stateManager.GameSlotManager.Saving || stateManager.GameSlotManager.Loading)
                {
                    ScreenReader.Say("存档读档正在进行，请稍候");
                    Main.Log.LogInfo("手动存档被阻止: 存档读档正在进行");
                    return false;
                }

                if (!SaveLoadGuard.CanSaveNow(out var blockedReason))
                {
                    ScreenReader.Say(blockedReason);
                    Main.Log.LogWarning($"手动存档被阻止: {blockedReason}");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"检查手动存档条件时出错: {e.GetType().Name} - {e.Message}");
                return true;
            }
        }

        private static bool GuardLoadSlotClicked(string slotId)
        {
            try
            {
                if (!SaveLoadGuard.TryGetStateManager(out var stateManager, out var unavailableReason))
                {
                    ScreenReader.Say(unavailableReason);
                    Main.Log.LogWarning($"手动读档被阻止: {unavailableReason}");
                    return false;
                }

                if (stateManager.GameSlotManager.Saving || stateManager.GameSlotManager.Loading)
                {
                    ScreenReader.Say("存档读档正在进行，请稍候");
                    Main.Log.LogInfo("手动读档被阻止: 存档读档正在进行");
                    return false;
                }

                if (string.IsNullOrEmpty(slotId) || !stateManager.GameSlotManager.SaveSlotExists(slotId))
                {
                    return true;
                }

                var state = stateManager.GameSlotManager.Load(slotId).GetAwaiter().GetResult();
                if (!SaveLoadGuard.IsGameStateLoadable(state))
                {
                    ScreenReader.Say("这个存档无效，无法读取");
                    Main.Log.LogWarning($"手动读档被阻止: {slotId} 播放位置无效 {SaveLoadGuard.FormatPlaybackSpot(state?.PlaybackSpot)}");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"检查手动读档条件时出错: {e.GetType().Name} - {e.Message}");
                return true;
            }
        }
    }
}
