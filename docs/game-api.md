# 狐想恋翩 - 游戏 API 文档

## 概述
- **游戏:** 痴情妹妹纱雪的兄控日记 Demo（狐想恋翩-妹妹篇）
- **引擎:** Unity + Naninovel（视觉小说引擎）
- **运行时:** Mono (64位)
- **架构:** 64-Bit
- **开发者:** StationWorks / TamaMako gaming
- **游戏内部名称:** HuXiangLianPian
- **Naninovel版本:** 基于Elringus.Naninovel.Runtime.dll (1.6MB)
- **自定义代码:** 很少（Assembly-CSharp.dll仅62KB，32个类）

---

## 1. 引擎服务访问点

Naninovel 使用服务模式，通过 `Engine` 静态类获取各种引擎服务。

### Engine 静态类
**命名空间:** `Naninovel`

**核心方法:**
- `Engine.GetService<TService>()` - 获取指定类型的服务
- `Engine.GetServiceOrErr<TService>()` - 获取服务，不存在则抛出错误
- `Engine.Initialized` - 引擎是否已初始化
- `Engine.Initializing` - 引擎是否正在初始化
- `Engine.DestroyToken` - 销毁令牌（CancellationToken）
- `Engine.Services` - 所有已注册服务的只读集合
- `Engine.RootObject` - 引擎根GameObject

**重要事件:**
- 初始化完成后才能访问服务

---

## 2. 文本打印系统（最核心）

### ITextPrinterManager
**命名空间:** `Naninovel`

**功能:** 管理所有文本打印机，控制文本显示

**属性:**
- `string DefaultPrinterId { get; set; }` - 默认打印机ID
- `float BaseRevealSpeed { get; set; }` - 基础显示速度
- `float BaseAutoDelay { get; set; }` - 基础自动播放延迟

**事件（无障碍开发最重要）:**
- `event Action<PrintMessageArgs> OnPrintStarted` - 文本开始打印时触发
- `event Action<PrintMessageArgs> OnPrintFinished` - 文本打印完成时触发

**方法:**
- `UniTask Print(string printerId, PrintedMessage message, bool append = false, float speed = 1f, AsyncToken token = default)` - 打印文本

---

### PrintedMessage
**命名空间:** `Naninovel`
**类型:** struct

**属性:**
- `LocalizableText Text` - 文本内容（可直接转string）
- `MessageAuthor? Author` - 说话者（可选）

**构造函数:**
- `PrintedMessage(LocalizableText text, MessageAuthor author = default)`

---

### LocalizableText
**命名空间:** `Naninovel`
**类型:** struct

**功能:** 可本地化的文本，支持隐式转换

**重要特性:**
- 可隐式转换为 `string`：`string plainText = localizableText;`
- 可从 `string` 隐式创建：`LocalizableText text = "你好";`

**属性:**
- `IReadOnlyList<LocalizableTextPart> Parts` - 文本部分
- `bool IsEmpty` - 是否为空

**方法:**
- `ToString()` - 转换为纯文本
- `FromPlainText(string)` - 从纯文本创建
- `Join(string separator, IReadOnlyList<LocalizableText> values)` - 连接多个文本

---

### MessageAuthor
**命名空间:** `Naninovel`
**类型:** struct

**属性:**
- `string Id` - 说话者ID
- `LocalizableText Label` - 说话者显示名称

---

### ITextPrinterActor
**命名空间:** `Naninovel`

**功能:** 单个文本打印机的接口

**属性:**
- `IReadOnlyList<PrintedMessage> Messages { get; set; }` - 已打印的消息列表
- `IReadOnlyList<MessageTemplate> Templates { get; set; }` - 消息模板
- `float RevealProgress { get; set; }` - 显示进度

**方法:**
- `void AddMessage(PrintedMessage message)` - 添加消息
- `void AppendText(LocalizableText text)` - 追加文本
- `UniTask Reveal(float delay, AsyncToken token = default)` - 显示文本

---

## 3. UI 系统

### IUIManager
**命名空间:** `Naninovel`

**功能:** 管理所有UI面板

**属性:**
- `string FontName { get; set; }` - 字体名称
- `int FontSize { get; set; }` - 字体大小
- `bool AnyModal { get; }` - 是否有模态UI打开

**事件:**
- `event Action<string> OnFontNameChanged` - 字体名称改变
- `event Action<int> OnFontSizeChanged` - 字体大小改变

**方法:**
- `T GetUI<T>() where T : class, IManagedUI` - 获取指定类型的UI
- `IManagedUI GetUI(string name)` - 获取指定名称的UI
- `bool HasUI<T>()` - 检查是否有指定类型的UI
- `bool HasUI(string name)` - 检查是否有指定名称的UI
- `void GetManagedUIs(ICollection<IManagedUI> managedUIs)` - 获取所有UI
- `UniTask<IManagedUI> AddUI(GameObject prefab, string name = null, string group = null)` - 添加UI
- `bool RemoveUI(IManagedUI managedUI)` - 移除UI
- `void SetUIVisibleWithToggle(bool visible, bool allowToggle = true)` - 切换UI可见性
- `void AddModalUI(IManagedUI managedUI)` - 添加模态UI
- `void RemoveModalUI(IManagedUI managedUI)` - 移除模态UI
- `void GetModalUIs(ICollection<IManagedUI> modalUIs)` - 获取所有模态UI
- `bool IsActiveModalUI(IManagedUI managedUI)` - 检查是否是活动模态UI
- `TMP_FontAsset GetFontAsset(string fontName)` - 获取字体资源

---

### IManagedUI（所有UI的基础接口）
**命名空间:** `Naninovel.UI`

**属性:**
- `bool Visible { get; set; }` - 是否可见
- `Camera RenderCamera { get; set; }` - 渲染相机
- `string ModalGroup { get; }` - 模态组
- `int SortingOrder { get; }` - 排序顺序

**事件:**
- `event Action<bool> OnVisibilityChanged` - 可见性改变时触发

**方法:**
- `UniTask Initialize()` - 初始化
- `UniTask ChangeVisibility(bool visible, float? duration = null, AsyncToken token = default)` - 改变可见性
- `void SetFont(TMP_FontAsset font)` - 设置字体
- `void SetFontSize(int size)` - 设置字体大小

---

### 内置 UI 接口列表
**命名空间:** `Naninovel.UI`

| 接口 | 功能 |
|------|------|
| `ITitleUI` | 标题菜单（主菜单） |
| `ISettingsUI` | 设置菜单 |
| `ISaveLoadUI` | 存档/读档菜单 |
| `IBacklogUI` | 历史记录面板 |
| `ICGGalleryUI` | CG画廊 |
| `ITipsUI` | 术语/提示 |
| `IPauseUI` | 暂停菜单 |
| `IConfirmationUI` | 确认对话框 |
| `ILoadingUI` | 加载界面 |
| `IMovieUI` | 视频播放 |
| `IRollbackUI` | 回滚界面 |
| `ISceneTransitionUI` | 场景过渡 |
| `IScriptNavigatorUI` | 脚本导航 |
| `IInputIndicator` | 输入指示器 |
| `IToastUI` | 提示/通知 |
| `IVariableInputUI` | 变量输入 |
| `IClickThroughPanel` | 点击穿透面板 |
| `IContinueInputUI` | 继续输入UI |
| `IExternalScriptsUI` | 外部脚本UI |
| `ILocalizableUI` | 可本地化UI |

---

### ISaveLoadUI
**命名空间:** `Naninovel.UI`

**属性:**
- `SaveLoadUIPresentationMode PresentationMode { get; set; }` - 显示模式（存档/读档）

**方法:**
- `SaveLoadUIPresentationMode GetLastLoadMode()` - 获取上次的加载模式

---

### IBacklogUI
**命名空间:** `Naninovel.UI`

**方法:**
- `void AddMessage(LocalizableText text, string authorId = null, PlaybackSpot? rollbackSpot = null, string voicePath = null)` - 添加消息到历史
- `void AppendMessage(LocalizableText text, string voicePath = null)` - 追加消息
- `void AddChoice(IReadOnlyList<BacklogChoice> choices)` - 添加选择到历史
- `void Clear()` - 清空历史

---

## 4. 输入系统

### IInputManager
**命名空间:** `Naninovel`

**功能:** 管理游戏输入

**属性:**
- `bool ProcessInput { get; set; }` - 是否处理输入
- `InputMode InputMode { get; set; }` - 输入模式

**事件:**
- `event Action<InputMode> OnInputModeChanged` - 输入模式改变时触发

**方法:**
- `IInputSampler GetSampler(string bindingName)` - 获取输入采样器
- `void AddBlockingUI(IManagedUI ui, params string[] allowedSamplers)` - 添加阻塞UI
- `void RemoveBlockingUI(IManagedUI ui)` - 移除阻塞UI
- `bool IsSampling(string bindingName)` - 检查是否正在采样

---

### IInputSampler
**命名空间:** `Naninovel`

**功能:** 单个输入绑定的采样器

**属性:**
- `InputBinding Binding { get; }` - 输入绑定
- `bool Enabled { get; set; }` - 是否启用
- `bool Active { get; }` - 是否激活（按下中）
- `float Value { get; }` - 当前值（0-1）
- `bool StartedDuringFrame { get; }` - 是否在本帧开始
- `bool EndedDuringFrame { get; }` - 是否在本帧结束

**事件:**
- `event Action OnStart` - 输入开始时触发
- `event Action OnEnd` - 输入结束时触发
- `event Action<float> OnChange` - 输入值改变时触发

**方法:**
- `void Activate(float value)` - 激活输入
- `void AddObjectTrigger(GameObject obj)` - 添加对象触发器
- `void RemoveObjectTrigger(GameObject obj)` - 移除对象触发器
- `CancellationToken GetNext()` - 获取下一次输入的令牌
- `CancellationToken InterceptNext(CancellationToken token = default)` - 拦截下一次输入

---

### 常用输入绑定名称
（需要进一步确认，以下是Naninovel常见的绑定）
- `Submit` - 确认/推进对话
- `Cancel` - 取消/返回
- `Skip` - 跳过文本
- `AutoPlay` - 自动播放
- `Save` - 快速存档
- `Load` - 快速读档
- `ToggleSettings` - 打开设置
- `ToggleBacklog` - 打开历史记录
- `PageNext` - 下一页
- `PagePrevious` - 上一页

---

## 5. 脚本播放器

### IScriptPlayer
**命名空间:** `Naninovel`

**功能:** 控制游戏脚本的播放

**属性:**
- `bool Playing { get; }` - 是否正在播放
- `bool Completing { get; }` - 是否正在完成
- `bool SkipActive { get; }` - 是否正在跳过
- `bool AutoPlayActive { get; }` - 是否自动播放
- `bool WaitingForInput { get; }` - 是否等待输入
- `PlayerSkipMode SkipMode { get; set; }` - 跳过模式
- `Script PlayedScript { get; }` - 当前播放的脚本
- `Command PlayedCommand { get; }` - 当前播放的命令
- `IReadOnlyCollection<Command> ExecutingCommands { get; }` - 正在执行的命令
- `PlaybackSpot PlaybackSpot { get; }` - 当前播放位置
- `ScriptPlaylist Playlist { get; }` - 播放列表
- `int PlayedIndex { get; }` - 已播放的索引
- `Stack<PlaybackSpot> GosubReturnSpots { get; }` - 子程序返回点栈
- `int PlayedCommandsCount { get; }` - 已播放的命令数

**事件:**
- `event Action<Script> OnPlay` - 开始播放脚本时
- `event Action<Script> OnStop` - 停止播放时
- `event Action<Command> OnCommandExecutionStart` - 命令开始执行时
- `event Action<Command> OnCommandExecutionFinish` - 命令执行完成时
- `event Action<bool> OnSkip` - 跳过模式改变时
- `event Action<bool> OnAutoPlay` - 自动播放改变时
- `event Action<bool> OnWaitingForInput` - 等待输入状态改变时

**方法:**
- `void Play(string scriptPath, int playlistIndex = 0)` - 播放脚本
- `void Resume(int? playlistIndex = null)` - 继续播放
- `void Stop()` - 停止播放
- `UniTask<bool> Rewind(int lineIndex)` - 回退到指定行
- `bool HasPlayed(string scriptPath, int? playlistIndex = null)` - 检查是否已播放
- `void SetSkipEnabled(bool enabled)` - 设置跳过
- `void SetAutoPlayEnabled(bool enabled)` - 设置自动播放
- `void SetWaitingForInputEnabled(bool enabled)` - 设置等待输入
- `void AddPreExecutionTask(Func<Command, UniTask> task)` - 添加预执行任务
- `void RemovePreExecutionTask(Func<Command, UniTask> task)` - 移除预执行任务
- `void AddPostExecutionTask(Func<Command, UniTask> task)` - 添加后执行任务
- `void RemovePostExecutionTask(Func<Command, UniTask> task)` - 移除后执行任务
- `UniTask Complete(Func<UniTask> onComplete = null)` - 完成当前命令

---

## 6. 选择支系统

### IChoiceHandlerManager
**命名空间:** `Naninovel`

**功能:** 管理选择支处理器

**属性:**
- `IResourceLoader<GameObject> ChoiceButtonLoader { get; }` - 选择按钮加载器

**方法:**
- `void PushPickedChoice(PlaybackSpot hostedAt, PlaybackSpot continueAt)` - 推送已选的选择
- `PlaybackSpot PopPickedChoice(PlaybackSpot hostedAt)` - 弹出已选的选择

---

### UIChoiceHandler
**命名空间:** `Naninovel`

**功能:** UI选择支处理器的默认实现

**属性:**
- `ChoiceHandlerPanel HandlerPanel { get; private set; }` - 选择面板
- `List<ChoiceState> Choices { get; }` - 选择列表

**方法:**
- `void AddChoice(ChoiceState choice)` - 添加选择

**事件:**
- `HandlerPanel.OnChoice` - 选择被点击时触发

---

### ChoiceState
**命名空间:** `Naninovel`
**类型:** struct

**属性:**
- `string Id` - 选择ID（GUID）
- `PlaybackSpot HostedAt` - 所在的播放位置
- `bool Nested` - 是否嵌套
- `LocalizableText Summary` - 选择摘要（显示的文本）
- `bool Locked` - 是否锁定
- `string ButtonPath` - 按钮路径
- `Vector2 ButtonPosition` - 按钮位置
- `bool OverwriteButtonPosition` - 是否覆盖按钮位置
- `string OnSelectScript` - 选择后执行的脚本
- `bool AutoPlay` - 是否自动播放

---

## 7. 状态管理器

### IStateManager
**命名空间:** `Naninovel`

**功能:** 管理游戏状态、存档、设置

**属性:**
- `GlobalStateMap GlobalState { get; }` - 全局状态
- `SettingsStateMap SettingsState { get; }` - 设置状态
- `ISaveSlotManager<GlobalStateMap> GlobalSlotManager { get; }` - 全局存档管理器
- `ISaveSlotManager<GameStateMap> GameSlotManager { get; }` - 游戏存档管理器
- `ISaveSlotManager<SettingsStateMap> SettingsSlotManager { get; }` - 设置存档管理器
- `bool QuickLoadAvailable { get; }` - 快速加载是否可用
- `bool AnyGameSaveExists { get; }` - 是否有任何游戏存档
- `bool RollbackInProgress { get; }` - 是否正在回滚

**事件:**
- `event Action<GameSaveLoadArgs> OnGameLoadStarted` - 游戏加载开始时
- `event Action<GameSaveLoadArgs> OnGameLoadFinished` - 游戏加载完成时
- `event Action<GameSaveLoadArgs> OnGameSaveStarted` - 游戏存档开始时
- `event Action<GameSaveLoadArgs> OnGameSaveFinished` - 游戏存档完成时
- `event Action OnResetStarted` - 重置开始时
- `event Action OnResetFinished` - 重置完成时
- `event Action OnRollbackStarted` - 回滚开始时
- `event Action OnRollbackFinished` - 回滚完成时

**方法:**
- `void AddOnGameSerializeTask(Action<GameStateMap> task)` - 添加游戏序列化任务
- `void RemoveOnGameSerializeTask(Action<GameStateMap> task)` - 移除游戏序列化任务
- `void AddOnGameDeserializeTask(Func<GameStateMap, UniTask> task)` - 添加游戏反序列化任务
- `void RemoveOnGameDeserializeTask(Func<GameStateMap, UniTask> task)` - 移除游戏反序列化任务
- `UniTask<GameStateMap> SaveGame(string slotId)` - 保存游戏
- `UniTask<GameStateMap> QuickSave()` - 快速存档
- `UniTask<GameStateMap> LoadGame(string slotId)` - 加载游戏
- `UniTask<GameStateMap> QuickLoad()` - 快速加载
- `UniTask SaveGlobal()` - 保存全局状态
- `UniTask SaveSettings()` - 保存设置
- `UniTask ResetState(params Func<UniTask>[] tasks)` - 重置状态

---

## 8. 音频系统

### IAudioManager
**命名空间:** `Naninovel`

**功能:** 管理游戏音频

**属性:**
- `AudioMixer AudioMixer { get; }` - 音频混合器
- `float MasterVolume { get; set; }` - 主音量
- `float BgmVolume { get; set; }` - BGM音量
- `float SfxVolume { get; set; }` - 音效音量
- `float VoiceVolume { get; set; }` - 语音音量
- `string VoiceLocale { get; set; }` - 语音语言
- `IResourceLoader AudioLoader { get; }` - 音频加载器
- `IResourceLoader VoiceLoader { get; }` - 语音加载器

**方法:**
- `UniTask ModifyBgm(string path, float volume, bool loop, float time, AsyncToken token = default)` - 修改BGM
- `UniTask ModifySfx(string path, float volume, bool loop, float time, AsyncToken token = default)` - 修改音效
- `UniTask PlaySfxFast(string path, float volume = 1f, string group = null, bool restart = true, bool additive = true)` - 快速播放音效
- `UniTask PlayBgm(string path, float volume = 1f, float fadeTime = 0f, bool loop = true, string introPath = null, string group = null, AsyncToken token = default)` - 播放BGM
- `UniTask StopBgm(string path, float fadeTime = 0f, AsyncToken token = default)` - 停止BGM
- `UniTask StopAllBgm(float fadeTime = 0f, AsyncToken token = default)` - 停止所有BGM
- `UniTask PlaySfx(string path, float volume = 1f, float fadeTime = 0f, bool loop = false, string group = null, AsyncToken token = default)` - 播放音效
- `UniTask StopSfx(string path, float fadeTime = 0f, AsyncToken token = default)` - 停止音效

---

## 9. 自定义变量系统

### ICustomVariableManager
**命名空间:** `Naninovel`

**功能:** 管理自定义变量

**属性:**
- `IReadOnlyCollection<CustomVariable> Variables { get; }` - 所有变量

**事件:**
- `event Action<CustomVariableUpdatedArgs> OnVariableUpdated` - 变量更新时触发

**方法:**
- `bool VariableExists(string name)` - 检查变量是否存在
- `CustomVariableValue GetVariableValue(string name)` - 获取变量值
- `void SetVariableValue(string name, CustomVariableValue value)` - 设置变量值
- `void ResetVariable(string name)` - 重置变量
- `void ResetAllVariables(CustomVariableScope? scope = null)` - 重置所有变量

---

## 10. 游戏自定义代码（Assembly-CSharp.dll）

游戏自定义代码很少，主要是UI扩展。

### 命名空间：（全局命名空间）
- `SettingsUI` - 设置UI（继承自GameSettingsMenu）
  - 新增：`BackUIType` 属性（返回标题/返回对话）
- `ControlPanelSettingsButton` - 控制面板设置按钮
- `TitleSettingsButton` - 标题设置按钮
- `GameSystemQuitButton` - 退出游戏按钮
- `GameSystemTitleButton` - 返回标题按钮
- `GameSettingsResolutionToggle` - 分辨率设置开关
- `GameSettingsScreenModeToggle` - 屏幕模式开关
- `GameSettingsVoiceInterruptToggle` - 语音中断开关
- `ScrollWheelBacklog` - 滚轮历史记录
- `RightClickButtonTrigger` - 右键按钮触发
- `ScriptableToggle` - 可脚本化开关
- `SplashSequence` - 启动画面序列

### 命名空间：NananaGames.UI
- `SaveLoadMenu` - 存档/读档菜单（继承自CustomUI，实现ISaveLoadUI）
  - 包含SavePanel和LoadPanel
  - 使用SaveLoadSlotsGrid显示存档槽位
  - 支持快速存档/读档
  - 有分页功能
- `SaveLoadSlot` - 存档槽位组件
- `SaveLoadSlotsGrid` - 存档槽位网格
- `ControlPanelAutoPlayToggle` - 自动播放开关
- `ControlPanelSkipToggle` - 跳过开关
- `ControlPanelSaveLoadButton` - 存档/读档按钮
- `GameSettingsSkipModeToggle` - 跳过模式开关
- `GameSettingsReturnButton` - 设置返回按钮
- `SaveLoadGridPageToggle` - 存档分页开关
- `SaveLoadMenuQuitButton` - 存档菜单退出按钮
- `SaveLoadMenuReturnButton` - 存档菜单返回按钮
- `SaveLoadMenuReturnTitleButton` - 存档菜单返回标题按钮
- `SaveLoadSwitchPanelButton` - 存档切换面板按钮
- `TitleCGGalleryButton` - 标题CG画廊按钮
- `ScriptableToggle` - 可脚本化开关

### 命名空间：NananaGames.NaniExt.UI
- `ButtonSfx` - 按钮音效

### 命名空间：Huxiang.Naninovel
- `AutoCharacterAuthorImage` - 自动角色作者图片
- `CharacterAvatarsConfiguration` - 角色头像配置

---

## 11. Harmony 补丁事件钩子（推荐）

### 文本朗读（最优先）
**最佳方案：使用事件而不是补丁**
```csharp
// 在Mod初始化时订阅事件
var printerManager = Engine.GetService<ITextPrinterManager>();
printerManager.OnPrintStarted += HandlePrintStarted;

void HandlePrintStarted(PrintMessageArgs args)
{
    // 获取文本和说话者
    string text = args.Message.Text; // 隐式转换为string
    string author = args.Message.Author?.Label ?? "";
    
    // 用屏幕阅读器朗读
    ScreenReader.Say($"{author}: {text}");
}
```

**备选：补丁方式**
- `UITextPrinter.PrintText()` 或类似方法 - Postfix

---

### UI 状态变化
**最佳方案：使用IManagedUI.OnVisibilityChanged事件**
```csharp
var uiManager = Engine.GetService<IUIManager>();
var titleUI = uiManager.GetUI<ITitleUI>();
titleUI.OnVisibilityChanged += (visible) => {
    if (visible) ScreenReader.Say("标题菜单");
};
```

**备选：补丁方式**
- `CustomUI.ChangeVisibility()` - Postfix
- `CustomUI.Show()` / `Hide()` - Postfix

---

### 选择支显示
**补丁点：**
- `UIChoiceHandler.AddChoice()` - Postfix，朗读新添加的选项
- `ChoiceHandlerPanel.HandleChoiceClicked()` - Prefix/Postfix，朗读选中的选项

---

### 输入拦截
**补丁点：**
- `InputManager.SampleInput()` - Prefix，用于拦截或重定向输入
- 或使用 `IInputSampler.OnStart` / `OnEnd` 事件

---

## 12. 代码示例

### 获取引擎服务
```csharp
// 确保引擎已初始化后再调用
if (!Engine.Initialized) return;

// 获取文本打印机管理器
var printerManager = Engine.GetService<ITextPrinterManager>();

// 获取UI管理器
var uiManager = Engine.GetService<IUIManager>();

// 获取脚本播放器
var scriptPlayer = Engine.GetService<IScriptPlayer>();

// 获取输入管理器
var inputManager = Engine.GetService<IInputManager>();

// 获取状态管理器
var stateManager = Engine.GetService<IStateManager>();
```

### 订阅文本打印事件
```csharp
var printerManager = Engine.GetService<ITextPrinterManager>();
printerManager.OnPrintStarted += (args) =>
{
    string text = args.Message.Text; // 自动转换为string
    string author = args.Message.Author.HasValue 
        ? args.Message.Author.Value.Label 
        : "";
    
    if (!string.IsNullOrEmpty(author))
    {
        ScreenReader.Say($"{author}：{text}");
    }
    else
    {
        ScreenReader.Say(text);
    }
};
```

### 检查UI是否可见
```csharp
var uiManager = Engine.GetService<IUIManager>();

// 检查标题UI是否可见
if (uiManager.HasUI<ITitleUI>())
{
    var titleUI = uiManager.GetUI<ITitleUI>();
    bool isVisible = titleUI.Visible;
}

// 检查是否有模态UI打开
bool hasModal = uiManager.AnyModal;
```

### 获取输入采样器
```csharp
var inputManager = Engine.GetService<IInputManager>();
var submitSampler = inputManager.GetSampler("Submit");

submitSampler.OnStart += () =>
{
    DebugLogger.Log("Submit键按下");
};
```

---

## 13. 已知问题和注意事项

1. **Naninovel版本**：需要确认具体版本号，不同版本API可能有差异
2. **自定义代码少**：游戏自定义代码很少（仅62KB），大部分功能都在Naninovel引擎中
3. **Live2D**：游戏使用Live2D角色，可能对无障碍有影响（但主要是视觉的）
4. **Addressables**：游戏使用Unity Addressables管理资源，文本和脚本可能通过Addressables加载
5. **仅中文**：游戏目前只支持简体中文，不需要考虑多语言
6. **事件 vs 补丁**：优先使用Naninovel提供的事件，比Harmony补丁更稳定可靠
7. **引擎初始化时机**：必须等Engine.Initialized为true后才能获取服务

---

## 14. 待进一步分析

需要进一步研究的内容：
- [ ] Naninovel 具体版本号
- [ ] PrintMessageArgs 的详细结构
- [ ] 游戏具体的输入绑定名称
- [ ] 存档槽位的具体数量和结构
- [ ] 设置菜单的具体选项
- [ ] ASMR语音的播放机制（是否走普通语音通道）
- [ ] 菜单导航的具体实现（Unity UI导航系统）
- [ ] Addressables资源加载的具体方式
- [ ] 游戏脚本文件的位置和格式

---

## 修改历史
- **2026-06-21**: Mod框架从MelonLoader切换到BepInEx（因中文目录问题）
  - 游戏API本身不受影响，文档内容仍然适用
  - Mod加载方式从MelonLoader的Mods/目录改为BepInEx的plugins/目录
- **2026-06-20**: 初始分析，基于 Naninovel 官方文档
- **2026-06-20**: 通过反编译获得详细API信息，全面更新文档
  - 添加了所有核心接口的详细方法和属性
  - 添加了文本打印系统的详细分析
  - 添加了UI系统的完整接口列表
  - 添加了输入系统、脚本播放器、选择支系统等详细分析
  - 添加了游戏自定义代码的完整列表
  - 添加了Harmony补丁推荐方案
