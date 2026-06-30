namespace HuXiangLianPian.Accessibility
{
    internal static class ConfirmationMessageTracker
    {
        private static string _pendingMessage;
        private static float _lastMessageTime;

        public static void SetPending(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            _pendingMessage = Normalize(message);
            _lastMessageTime = UnityEngine.Time.unscaledTime;

            if (Main.DebugMode)
            {
                DebugLogger.Log(LogCategory.Handler, "ConfirmationMessageTracker", $"捕获确认对话框消息: {_pendingMessage}");
            }
        }

        public static bool TryConsume(out string message)
        {
            message = string.Empty;

            if (string.IsNullOrWhiteSpace(_pendingMessage))
            {
                return false;
            }

            if (UnityEngine.Time.unscaledTime - _lastMessageTime > 5f)
            {
                _pendingMessage = null;
                return false;
            }

            message = _pendingMessage;
            _pendingMessage = null;
            return true;
        }

        private static string Normalize(string message)
        {
            return message
                .Replace("<br>", "\n")
                .Replace("<BR>", "\n")
                .Replace("<br/>", "\n")
                .Replace("<BR/>", "\n")
                .Trim();
        }
    }
}
