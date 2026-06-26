using System;
using System.Reflection;
using Naninovel;
using TMPro;
using UnityEngine;

namespace HuXiangLianPian.Accessibility
{
    /// <summary>
    /// 对话文本处理器。
    /// 监听Naninovel的文本打印事件，通过屏幕阅读器朗读对话内容。
    /// </summary>
    public class DialogueHandler : IDisposable
    {
        #region Fields
        private ITextPrinterManager _textPrinterManager;
        private IScriptPlayer _scriptPlayer;
        private IStateManager _stateManager;
        private bool _subscribed = false;
        private bool _stateSubscribed = false;
        private string _lastAnnouncedText;
        private float _lastAnnounceTime;
        private int _pendingLoadAnnounceFrames = -1;
        private const float MIN_ANNOUNCE_INTERVAL = 0.1f; // 最小朗读间隔，防止刷屏
        #endregion

        #region Public Methods
        /// <summary>
        /// 初始化对话处理器，订阅文本打印事件。
        /// </summary>
        public void Initialize()
        {
            try
            {
                _textPrinterManager = Engine.GetService<ITextPrinterManager>();
                _scriptPlayer = Engine.GetService<IScriptPlayer>();
                _stateManager = Engine.GetService<IStateManager>();

                if (_textPrinterManager != null)
                {
                    _textPrinterManager.OnPrintStarted += OnPrintStarted;
                    _textPrinterManager.OnPrintFinished += OnPrintFinished;
                    _subscribed = true;
                    Main.Log.LogInfo("对话文本处理器已初始化，已订阅文本打印开始/完成事件");
                }
                else
                {
                    Main.Log.LogWarning("无法获取ITextPrinterManager服务");
                }

                if (_scriptPlayer != null)
                {
                    _scriptPlayer.OnCommandExecutionStart += OnCommandExecutionStart;
                    _scriptPlayer.OnCommandExecutionFinish += OnCommandExecutionFinish;
                    Main.Log.LogInfo("对话诊断已订阅脚本命令开始/完成事件");
                }
                else
                {
                    Main.Log.LogWarning("无法获取IScriptPlayer服务，脚本命令诊断不可用");
                }

                if (_stateManager != null && !_stateSubscribed)
                {
                    _stateManager.OnGameLoadFinished += OnGameLoadFinished;
                    _stateSubscribed = true;
                    Main.Log.LogInfo("对话处理器已订阅读档完成事件");
                }
                else if (_stateManager == null)
                {
                    Main.Log.LogWarning("无法获取IStateManager服务，读档后朗读兜底不可用");
                }
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"初始化对话处理器时出错: {e.GetType().Name} - {e.Message}");
            }
        }

        /// <summary>
        /// 朗读当前对话状态。
        /// </summary>
        public void AnnounceStatus()
        {
            ScreenReader.Say("对话界面");
        }

        public void Update()
        {
            if (_pendingLoadAnnounceFrames < 0) return;

            _pendingLoadAnnounceFrames--;
            if (_pendingLoadAnnounceFrames > 0) return;

            _pendingLoadAnnounceFrames = -1;
            AnnounceLatestPrintedMessage("读档完成");
        }

        public void AnnounceCurrentPrintedMessage(string reason = "当前对话")
        {
            AnnounceLatestPrintedMessage(reason);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 文本打印开始时触发。只用于诊断，不朗读。
        /// </summary>
        private void OnPrintStarted(PrintMessageArgs args)
        {
            LogPrintEvent("OnPrintStarted", args);
        }

        /// <summary>
        /// 文本打印完成时触发。
        /// </summary>
        private void OnPrintFinished(PrintMessageArgs args)
        {
            try
            {
                LogPrintEvent("OnPrintFinished", args);

                string announceText = BuildAnnouncementText(args.Message.Text, args.Message.Author);

                // 调试：输出args的详细信息
                if (Main.DebugMode)
                {
                    Main.Log.LogInfo("=== 对话打印事件触发 ===");
                    Main.Log.LogInfo($"  Message.Text: {args.Message.Text}");
                    Main.Log.LogInfo($"  Message.Text.IsEmpty: {args.Message.Text.IsEmpty}");
                    Main.Log.LogInfo($"  Message.Author.HasValue: {args.Message.Author.HasValue}");
                    if (args.Message.Author.HasValue)
                    {
                        Main.Log.LogInfo($"  Message.Author.Value.Id: {args.Message.Author.Value.Id}");
                        Main.Log.LogInfo($"  Message.Author.Value.Label: {args.Message.Author.Value.Label}");
                    }
                }

                if (string.IsNullOrEmpty(announceText))
                {
                    if (Main.DebugMode)
                    {
                        Main.Log.LogWarning("  未能获取到对话文本");
                    }
                    return;
                }

                AnnounceText(announceText);

                if (Main.DebugMode)
                {
                    DebugLogger.Log(LogCategory.Handler, "DialogueHandler", "朗读对话: " + announceText);
                }
            }
            catch (Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning("处理文本打印事件时出错: " + e.GetType().Name + " - " + e.Message);
                    Main.Log.LogWarning("堆栈跟踪: " + e.StackTrace);
                }
            }
        }

        private void OnCommandExecutionStart(Command command)
        {
            LogCommandEvent("OnCommandExecutionStart", command);
        }

        private void OnCommandExecutionFinish(Command command)
        {
            LogCommandEvent("OnCommandExecutionFinish", command);
        }

        private void OnGameLoadFinished(GameSaveLoadArgs args)
        {
            if (Main.DebugMode)
            {
                Main.Log.LogInfo($"读档完成，准备朗读当前对话: Slot={args.SlotId}, Quick={args.Quick}");
            }

            _pendingLoadAnnounceFrames = 20;
        }

        private void AnnounceLatestPrintedMessage(string reason)
        {
            try
            {
                if (_textPrinterManager == null)
                {
                    _textPrinterManager = Engine.GetService<ITextPrinterManager>();
                }

                if (_textPrinterManager == null) return;

                ITextPrinterActor preferredPrinter = null;
                foreach (var printer in _textPrinterManager.Actors)
                {
                    if (printer == null || printer.Messages == null || printer.Messages.Count == 0)
                    {
                        continue;
                    }

                    if (preferredPrinter == null || printer.Id == _textPrinterManager.DefaultPrinterId)
                    {
                        preferredPrinter = printer;
                    }
                }

                if (preferredPrinter == null || preferredPrinter.Messages.Count == 0)
                {
                    if (Main.DebugMode)
                    {
                        Main.Log.LogInfo($"{reason}: 没有找到可朗读的当前对话");
                    }
                    return;
                }

                var message = preferredPrinter.Messages[preferredPrinter.Messages.Count - 1];
                string announceText = BuildAnnouncementText(message.Text, message.Author);
                if (string.IsNullOrEmpty(announceText))
                {
                    return;
                }

                AnnounceText(announceText, force: true);
                if (Main.DebugMode)
                {
                    DebugLogger.Log(LogCategory.Handler, "DialogueHandler", $"{reason}朗读当前对话: {announceText}");
                }
            }
            catch (Exception e)
            {
                if (Main.DebugMode)
                {
                    Main.Log.LogWarning($"读档后朗读当前对话时出错: {e.GetType().Name} - {e.Message}");
                }
            }
        }

        private string BuildAnnouncementText(LocalizableText textValue, MessageAuthor? authorValue)
        {
            string text = string.Empty;
            string author = string.Empty;

            if (!textValue.IsEmpty)
            {
                text = textValue;
                if (Main.DebugMode)
                {
                    Main.Log.LogInfo($"  从Message.Text解析正文: {text}");
                }
            }

            if (authorValue.HasValue)
            {
                var authorLabel = authorValue.Value.Label;
                if (!authorLabel.IsEmpty)
                {
                    author = authorLabel;
                }
                else
                {
                    author = authorValue.Value.Id;
                }
            }

            if (string.IsNullOrEmpty(text)) return string.Empty;
            return !string.IsNullOrEmpty(author) ? author + "：" + text : text;
        }

        private void AnnounceText(string announceText, bool force = false)
        {
            if (!force &&
                Time.unscaledTime - _lastAnnounceTime < MIN_ANNOUNCE_INTERVAL &&
                announceText == _lastAnnouncedText)
            {
                return;
            }

            _lastAnnouncedText = announceText;
            _lastAnnounceTime = Time.unscaledTime;
            ScreenReader.Say(announceText);
        }

        private void LogPrintEvent(string eventName, PrintMessageArgs args)
        {
            if (!Main.DebugMode) return;

            try
            {
                string text = args.Message.Text;
                string authorId = args.Message.Author.HasValue ? args.Message.Author.Value.Id : string.Empty;
                string authorLabel = args.Message.Author.HasValue ? args.Message.Author.Value.Label.ToString() : string.Empty;
                string printerId = args.Printer != null ? args.Printer.Id : "null";
                string printerType = args.Printer != null ? args.Printer.GetType().FullName : "null";

                Main.Log.LogInfo($"[DIALOGUE_DIAG] {eventName}");
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   Printer: {printerId} ({printerType})");
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   Append: {args.Append}, Speed: {args.Speed}");
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   Text.IsEmpty: {args.Message.Text.IsEmpty}");
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   Text: {text}");
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   Author.Id: {authorId}");
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   Author.Label: {authorLabel}");
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"[DIALOGUE_DIAG] 记录打印事件失败: {e.GetType().Name} - {e.Message}");
            }
        }

        private void LogCommandEvent(string eventName, Command command)
        {
            if (!Main.DebugMode) return;

            try
            {
                if (command == null)
                {
                    Main.Log.LogInfo($"[DIALOGUE_DIAG] {eventName}: null");
                    return;
                }

                Type commandType = command.GetType();
                Main.Log.LogInfo($"[DIALOGUE_DIAG] {eventName}: {commandType.FullName}");

                if (commandType.FullName == "Naninovel.Commands.PrintText")
                {
                    LogCommandField(command, commandType, "Text");
                    LogCommandField(command, commandType, "PrinterId");
                    LogCommandField(command, commandType, "AuthorId");
                    LogCommandField(command, commandType, "AuthorLabel");
                    LogCommandField(command, commandType, "Append");
                    LogCommandField(command, commandType, "WaitForInput");
                    LogCommandField(command, commandType, "RevealSpeed");
                }
            }
            catch (Exception e)
            {
                Main.Log.LogWarning($"[DIALOGUE_DIAG] 记录脚本命令失败: {e.GetType().Name} - {e.Message}");
            }
        }

        private void LogCommandField(Command command, Type commandType, string fieldName)
        {
            FieldInfo field = commandType.GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            if (field == null)
            {
                Main.Log.LogInfo($"[DIALOGUE_DIAG]   {fieldName}: <field missing>");
                return;
            }

            object parameter = field.GetValue(command);
            Main.Log.LogInfo($"[DIALOGUE_DIAG]   {fieldName}: {DescribeCommandParameter(parameter)}");
        }

        private string DescribeCommandParameter(object parameter)
        {
            if (parameter == null) return "null";

            try
            {
                Type type = parameter.GetType();
                PropertyInfo valueProperty = type.GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                object value = valueProperty != null ? valueProperty.GetValue(parameter, null) : null;

                PropertyInfo hasValueProperty = type.GetProperty("HasValue", BindingFlags.Public | BindingFlags.Instance);
                object hasValue = hasValueProperty != null ? hasValueProperty.GetValue(parameter, null) : null;

                if (value != null && hasValue != null)
                {
                    return $"{value} (HasValue={hasValue})";
                }
                if (value != null)
                {
                    return value.ToString();
                }

                return parameter.ToString();
            }
            catch (Exception e)
            {
                return $"<failed: {e.GetType().Name} - {e.Message}>";
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_subscribed && _textPrinterManager != null)
            {
                _textPrinterManager.OnPrintStarted -= OnPrintStarted;
                _textPrinterManager.OnPrintFinished -= OnPrintFinished;
                _subscribed = false;
            }
            if (_scriptPlayer != null)
            {
                _scriptPlayer.OnCommandExecutionStart -= OnCommandExecutionStart;
                _scriptPlayer.OnCommandExecutionFinish -= OnCommandExecutionFinish;
            }
            if (_stateSubscribed && _stateManager != null)
            {
                _stateManager.OnGameLoadFinished -= OnGameLoadFinished;
                _stateSubscribed = false;
            }
        }
        #endregion
    }
}
