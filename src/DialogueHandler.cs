using System;
using UnityEngine;
using Naninovel;
using TMPro;

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
        private bool _subscribed = false;
        private string _lastAnnouncedText;
        private float _lastAnnounceTime;
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
                
                if (_textPrinterManager != null)
                {
                    _textPrinterManager.OnPrintFinished += OnPrintFinished;
                    _subscribed = true;
                    Main.Log.LogInfo("对话文本处理器已初始化，已订阅文本打印完成事件");
                }
                else
                {
                    Main.Log.LogWarning("无法获取ITextPrinterManager服务");
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
        #endregion

        #region Private Methods
        /// <summary>
        /// 文本打印完成时触发。
        /// </summary>
        private void OnPrintFinished(PrintMessageArgs args)
        {
            try
            {
                string text = string.Empty;
                string author = string.Empty;

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

                // 方法1：尝试从args.Printer获取文本（ITextPrinter接口）
                try
                {
                    var printerProperty = args.GetType().GetProperty("Printer", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    
                    if (printerProperty != null)
                    {
                        var printer = printerProperty.GetValue(args);
                        if (printer != null)
                        {
                            if (Main.DebugMode)
                            {
                                Main.Log.LogInfo($"  Printer类型: {printer.GetType().FullName}");
                            }

                            // 尝试获取Text属性
                            var textProp = printer.GetType().GetProperty("Text", 
                                System.Reflection.BindingFlags.Public | 
                                System.Reflection.BindingFlags.Instance | 
                                System.Reflection.BindingFlags.FlattenHierarchy);
                            if (textProp != null)
                            {
                                var textValue = textProp.GetValue(printer);
                                if (textValue != null && !string.IsNullOrEmpty(textValue.ToString()))
                                {
                                    text = textValue.ToString();
                                    if (Main.DebugMode)
                                    {
                                        Main.Log.LogInfo($"  从Printer.Text获取文本: {text}");
                                    }
                                }
                            }

                            // 如果没找到，尝试从GameObject中获取文本组件
                            if (string.IsNullOrEmpty(text))
                            {
                                var monoBehaviour = printer as MonoBehaviour;
                                if (monoBehaviour != null)
                                {
                                    var tmpText = monoBehaviour.GetComponentInChildren<TMP_Text>();
                                    if (tmpText != null && !string.IsNullOrEmpty(tmpText.text))
                                    {
                                        text = tmpText.text;
                                        if (Main.DebugMode)
                                        {
                                            Main.Log.LogInfo($"  从TMP_Text获取文本: {text}");
                                        }
                                    }
                                    if (string.IsNullOrEmpty(text))
                                    {
                                        var unityText = monoBehaviour.GetComponentInChildren<UnityEngine.UI.Text>();
                                        if (unityText != null && !string.IsNullOrEmpty(unityText.text))
                                        {
                                            text = unityText.text;
                                            if (Main.DebugMode)
                                            {
                                                Main.Log.LogInfo($"  从Text获取文本: {text}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    if (Main.DebugMode)
                    {
                        Main.Log.LogInfo("从Printer获取文本失败: " + e.Message);
                    }
                }

                // 方法2：尝试从消息的Text属性获取（作为后备）
                if (string.IsNullOrEmpty(text))
                {
                    var messageText = args.Message.Text;
                    if (!messageText.IsEmpty)
                    {
                        string messageTextStr = messageText.ToString();
                        // 检查是否是脚本路径（包含#和|等特殊字符）
                        if (!messageTextStr.Contains("#") && !messageTextStr.Contains("|"))
                        {
                            text = messageTextStr;
                            if (Main.DebugMode)
                            {
                                Main.Log.LogInfo($"  从Message.Text获取文本: {text}");
                            }
                        }
                        else if (Main.DebugMode)
                        {
                            Main.Log.LogInfo($"  Message.Text看起来是脚本路径，跳过: {messageTextStr}");
                        }
                    }
                }

                // 获取说话者
                if (args.Message.Author.HasValue)
                {
                    var authorLabel = args.Message.Author.Value.Label;
                    if (!authorLabel.IsEmpty)
                    {
                        author = authorLabel.ToString();
                    }
                }

                if (string.IsNullOrEmpty(text))
                {
                    if (Main.DebugMode)
                    {
                        Main.Log.LogWarning("  未能获取到对话文本");
                    }
                    return;
                }

                // 组合朗读文本
                string announceText = string.Empty;
                if (!string.IsNullOrEmpty(author))
                {
                    announceText = author + "：" + text;
                }
                else
                {
                    announceText = text;
                }

                // 防止频繁朗读
                if (Time.unscaledTime - _lastAnnounceTime < MIN_ANNOUNCE_INTERVAL &&
                    announceText == _lastAnnouncedText)
                {
                    return;
                }

                _lastAnnouncedText = announceText;
                _lastAnnounceTime = Time.unscaledTime;

                // 朗读文本
                ScreenReader.Say(announceText);

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
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_subscribed && _textPrinterManager != null)
            {
                _textPrinterManager.OnPrintFinished -= OnPrintFinished;
                _subscribed = false;
            }
        }
        #endregion
    }
}
