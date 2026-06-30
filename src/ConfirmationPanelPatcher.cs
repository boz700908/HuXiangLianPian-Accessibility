using HarmonyLib;
using System;
using System.Reflection;

namespace HuXiangLianPian.Accessibility
{
    internal sealed class ConfirmationPanelPatcher : IDisposable
    {
        private readonly Harmony _harmony;

        public ConfirmationPanelPatcher()
        {
            _harmony = new Harmony("com.boz700908.HuXiangLianPianAccessibility.confirmation");
        }

        public void Apply()
        {
            try
            {
                Type panelType = AccessTools.TypeByName("Naninovel.UI.ConfirmationPanel");
                MethodInfo confirmMethod = panelType != null
                    ? AccessTools.Method(panelType, "Confirm", new[] { typeof(string) })
                    : null;
                MethodInfo prefixMethod = AccessTools.Method(typeof(ConfirmationPanelPatcher), nameof(CaptureConfirmMessage));

                if (confirmMethod == null || prefixMethod == null)
                {
                    Main.Log.LogWarning("无法安装确认对话框朗读补丁，未找到 ConfirmationPanel.Confirm(string)");
                    return;
                }

                _harmony.Patch(confirmMethod, prefix: new HarmonyMethod(prefixMethod));
                Main.Log.LogInfo("确认对话框朗读补丁已安装");
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"安装确认对话框朗读补丁时出错: {e.GetType().Name} - {e.Message}");
            }
        }

        public void Dispose()
        {
            try
            {
                _harmony.UnpatchSelf();
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"卸载确认对话框朗读补丁时出错: {e.GetType().Name} - {e.Message}");
            }
        }

        private static void CaptureConfirmMessage(string message)
        {
            ConfirmationMessageTracker.SetPending(message);
        }
    }
}
