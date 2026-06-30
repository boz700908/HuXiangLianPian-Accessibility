using Naninovel;
using Naninovel.UI;

namespace HuXiangLianPian.Accessibility
{
    internal static class SaveLoadGuard
    {
        public static bool TryGetStateManager(out IStateManager stateManager, out string reason)
        {
            stateManager = null;

            if (!Engine.Initialized)
            {
                reason = "游戏还没有加载完成，存档读档暂不可用";
                return false;
            }

            stateManager = Engine.GetService<IStateManager>();
            if (stateManager == null)
            {
                reason = "存档读档功能不可用";
                return false;
            }

            reason = null;
            return true;
        }

        public static bool CanSaveNow(out string reason)
        {
            var scriptPlayer = Engine.GetService<IScriptPlayer>();
            if (scriptPlayer == null)
            {
                reason = "剧情还没有加载完成，不能存档";
                return false;
            }

            if (scriptPlayer.PlayedScript == null || !IsPlaybackSpotUsable(scriptPlayer.PlaybackSpot))
            {
                reason = "剧情还没有开始，不能存档";
                Main.Log?.LogWarning($"当前播放位置无效: {FormatPlaybackSpot(scriptPlayer.PlaybackSpot)}");
                return false;
            }

            var uiManager = Engine.GetService<IUIManager>();
            if (uiManager != null &&
                (IsUIVisible<ISettingsUI>(uiManager) ||
                 IsUIVisible<IBacklogUI>(uiManager) ||
                 IsUIVisible<IConfirmationUI>(uiManager) ||
                 IsUIVisible<ITitleUI>(uiManager)))
            {
                reason = "当前界面不能存档";
                return false;
            }

            reason = null;
            return true;
        }

        public static bool CanQuickSaveNow(out string reason)
        {
            if (!CanSaveNow(out reason))
            {
                return false;
            }

            var uiManager = Engine.GetService<IUIManager>();
            if (uiManager != null && IsUIVisible<ISaveLoadUI>(uiManager))
            {
                reason = "当前界面不能快速存档";
                return false;
            }

            return true;
        }

        public static bool IsGameStateLoadable(GameStateMap state)
        {
            return state != null && IsPlaybackSpotUsable(state.PlaybackSpot);
        }

        public static bool IsPlaybackSpotUsable(PlaybackSpot spot)
        {
            return spot.Valid && !string.IsNullOrEmpty(spot.ScriptPath);
        }

        public static string FormatPlaybackSpot(PlaybackSpot? spot)
        {
            if (!spot.HasValue) return "<null>";
            var value = spot.Value;
            return $"Script='{value.ScriptPath}', Line={value.LineIndex}, Inline={value.InlineIndex}, Valid={value.Valid}";
        }

        private static bool IsUIVisible<T>(IUIManager uiManager) where T : class, IManagedUI
        {
            try
            {
                return uiManager.GetUI<T>()?.Visible == true;
            }
            catch (System.Exception e)
            {
                Main.Log?.LogDebug($"检查UI可见性失败: {typeof(T).Name} - {e.Message}");
                return false;
            }
        }
    }
}
