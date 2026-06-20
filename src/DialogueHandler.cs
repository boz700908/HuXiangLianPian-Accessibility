using System;
using UnityEngine;
using Naninovel;

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
                    _textPrinterManager.OnPrintStarted += OnPrintStarted;
                    _subscribed = true;
                    Main.Log.LogInfo("对话文本处理器已初始化，已订阅文本打印事件");
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
        /// 文本开始打印时触发。
        /// </summary>
        private void OnPrintStarted(PrintMessageArgs args)
        {
            try
            {
                // 获取文本内容
                string text = args.Message.Text.ToString();
                if (string.IsNullOrEmpty(text)) return;

                // 获取说话者
                string author = string.Empty;
                if (args.Message.Author.HasValue)
                {
                    author = args.Message.Author.Value.Label.ToString();
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
                }
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_subscribed && _textPrinterManager != null)
            {
                _textPrinterManager.OnPrintStarted -= OnPrintStarted;
                _subscribed = false;
            }
        }
        #endregion
    }
}
