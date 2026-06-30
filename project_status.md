# 痴情妹妹纱雪的兄控日记 - 无障碍Mod 项目状态

## 游戏信息
- **游戏名称**: 痴情妹妹纱雪的兄控日记 Demo（狐想恋翩-妹妹篇）
- **Steam App ID**: 4105740 (Demo) / 3946250 (正式版)
- **引擎**: Unity + Naninovel
- **运行时**: Mono (64位)
- **开发者**: StationWorks / TamaMako gaming
- **游戏内部名称**: HuXiangLianPian
- **Windows安装路径**: C:\Program Files (x86)\Steam\steamapps\common\狐想恋翩-梦妹以求- Demo
- **游戏大小**: 约717MB
- **特点**: 国产美少女游戏，含五篇ASMR音声，无刀无虐

## Mod信息
- **Mod名称**: HuXiangLianPianAccessibility
- **命名空间**: HuXiangLianPian.Accessibility
- **作者**: boz700908
- **Mod加载器**: BepInEx 5.4.22.0 (x64)
- **目标框架**: net472
- **配置系统**: BepInEx ConfigFile

## 开发进度

### Phase 0: 项目设置
- [x] 游戏分析（引擎、架构、运行时）
- [x] Mod加载器选择（最初选择MelonLoader，后因中文目录问题切换到BepInEx）
- [x] Tolk屏幕阅读器集成（从boz700908/tolk仓库下载）
- [x] .NET SDK 配置
- [x] GitHub 仓库创建
- [x] 项目结构搭建
- [x] MelonLoader 安装（v0.7.3 x64，已弃用）
- [x] BepInEx 安装（v5.4.22.0 x64，当前使用）
- [x] 反编译工具安装（ilspycmd 8.2.0.7535）
- [x] 首次编译测试（MelonLoader版本，0警告0错误，已弃用）
- [x] BepInEx版本编译测试（0警告0错误）
- [x] 游戏API文档（docs/game-api.md）
- [x] Naninovel版本确认（Naninovel.Common 2025.3.1546.19）
- [x] dist分发目录（完整运行时文件，已提交到Git）

### Phase 1: 代码分析
- [x] Tier 1: 结构概览（Assembly-CSharp.dll 32个自定义类，Naninovel 972个类）
- [x] Tier 1: UI系统（IUIManager接口、20+个UI接口、完整UI列表）
- [x] Tier 1: 文本系统（ITextPrinterManager、OnPrintStarted/OnPrintFinished事件、PrintedMessage、LocalizableText）
- [x] Tier 1: 脚本播放器（IScriptPlayer接口、丰富的事件系统）
- [x] Tier 1: 输入系统（IInputManager、IInputSampler、输入绑定）
- [x] Tier 1: 状态管理（IStateManager、存档系统）
- [x] Tier 1: 选择支系统（IChoiceHandlerManager、UIChoiceHandler、ChoiceState）
- [x] Tier 1: 音频系统（IAudioManager）
- [x] Tier 1: 自定义变量系统（ICustomVariableManager）
- [x] Tier 1: 本地化（LocalizableText、仅中文）
- [x] Tier 2: 游戏机制（对话、选项、存档）
- [x] Tier 2: 状态系统（全局状态、设置状态、游戏状态）
- [x] Tier 2: 事件系统（丰富的C#事件，无需Harmony补丁）
- [x] Tier 2: 选项系统（ChoiceState、UIChoiceHandler）
- [x] Tier 2: 存档系统（IStateManager、ISaveSlotManager）
- [x] 游戏API文档详细更新（docs/game-api.md）

### Phase 2: 基础框架
- [x] 主菜单导航（MenuHandler.cs，分类处理不同UI元素）
- [x] 菜单打开/关闭检测（自动朗读菜单标题）
- [x] 存档槽位特殊适配（读取槽位编号、日期、文本）
- [x] 设置项特殊适配（读取开关标签和状态）
- [x] 从MelonLoader迁移到BepInEx（因中文目录问题）
- [x] 配置系统完整实现（Verbosity、AnnounceEmptyStates，支持Ctrl+F11设置菜单）
- [ ] 对话文本朗读
- [ ] 全局快捷键
- [ ] 存档/读档完整支持

### Phase 3: 功能完善
- [ ] 完整菜单支持
- [ ] 游戏内UI导航
- [ ] 快捷键系统
- [ ] 调试模式

## 技术栈
- **语言**: C#
- **Mod框架**: BepInEx 5.4.22.0 + Harmony
- **屏幕阅读器**: Tolk
- **游戏引擎**: Unity + Naninovel
- **反编译工具**: ilspycmd 8.2.0.7535

## 仓库地址
https://github.com/boz700908/HuXiangLianPian-Accessibility

## 开发模式
- Linux 工作区编译测试
- Windows 真机测试
- GitHub 代码同步

## 重要变更记录
### 2026-06-26: 重新反编译游戏自定义代码
- **原因**: 剧情文本不能正常朗读，需要重新核对游戏真实代码和 Naninovel 调用路径
- **已反编译程序集**:
  - `Assembly-CSharp.dll` -> `decompiled/Assembly-CSharp/`
  - `Live2D.Cubism.dll` -> `decompiled/Live2D.Cubism/`（第三方 Live2D SDK，用于排除非游戏逻辑）
- **自定义代码范围**:
  - `Assembly-CSharp.dll` 是主要游戏自定义逻辑，反编译后约 33 个文件
  - 重点类型包括 `Huxiang.Naninovel.AutoCharacterAuthorImage`、`NananaGames.UI.SaveLoadMenu`、`SaveLoadSlot`、设置/标题/控制面板按钮类
- **剧情朗读问题证据与结论**:
  - BepInEx 日志显示 `DialogueHandler` 已初始化并订阅 `ITextPrinterManager.OnPrintFinished`
  - 新增诊断日志后确认 `PrintText`、`OnPrintStarted`、`OnPrintFinished` 都会触发
  - Naninovel 反编译源码显示 `PrintText` 会调用 `TextPrinterManager.Print()`，而 `TextPrinterManager.Print()` 会触发 `OnPrintStarted` 和 `OnPrintFinished`
  - 根因：`LocalizableText.ToString()` 返回本地化键（如 `chapter00_01 #7.0 |#~742114ad|`），旧代码把它当成脚本路径跳过；实际正文需要通过 `LocalizableText` 到 `string` 的隐式转换解析
  - 已修复：剧情正文改为 `string text = args.Message.Text`，作者标签为空时回退到 `Author.Id`

### 2026-06-26: 修复 Esc 关闭菜单后的焦点卡死
- **问题**: 在菜单中按 Esc 后，游戏 UI 会隐藏菜单，但 Unity `EventSystem` 可能仍选中隐藏菜单里的旧对象（如 `DeleteButton`），导致后续键盘导航和朗读停在不可见元素上，表现为界面卡死
- **依据**: BepInEx 日志显示菜单关闭后仍出现 `MenuHandler` 处理隐藏菜单按钮的记录，随后 Unity/TMP 抛出 `NullReferenceException`
- **修复**:
  - 菜单关闭时清空 `EventSystem.current.currentSelectedGameObject`
  - 每帧检测当前选中对象是否已不可见，若不可见则清空焦点和滑块状态
- **说明**: 不拦截 Esc，不替换游戏原生 `Cancel` 逻辑，只清理 Mod 导航层遗留焦点
- **设置菜单补充修复**:
  - 根因：游戏自定义 `SettingsUI` 只记录 `BackUIType`，但没有重写 Naninovel 基类的 `HandleCancelInput()`；按 Esc 时基类只保存设置并隐藏设置菜单，不会像返回按钮那样恢复标题菜单
  - 已修复：`MenuHandler` 检测到菜单状态从 `Settings` 变为 `None` 时，如果设置菜单来自标题菜单且已隐藏，则重新显示标题菜单
  - 追加保护：当前菜单类型为 `None` 时不再处理 `EventSystem` 中遗留的旧选中对象，避免继续朗读隐藏设置控件
- **存档/读档菜单补充修复**:
  - 根因：Mod 的 F3/F4 直接设置 `ISaveLoadUI.Visible = true`，绕过了游戏自定义 `ControlPanelSaveLoadButton` 的上下文设置；原生按钮会设置 `SaveLoadMenu.BackUIType = Dialogue`、`PresentationMode` 并调用 `Show()`
  - 已修复：F3/F4 改为先设置 `PresentationMode` 和 `BackUIType = Dialogue`，隐藏暂停菜单后调用 `Show()`，让 Esc/返回按钮按游戏原生逻辑关闭到剧情界面
- **存档/读档图片按钮标签补全**:
  - 日志确认页码控件朗读为 `PageBtn_1` 等内部对象名，且切换存档/读档、上一页、下一页图片按钮缺少语义标签
  - 已补充 `LoadButton`、`SaveButton`、`PreviousPageButton`、`NextPageButton`、`SaveLoadSwitchPanelButton` 的中文映射
  - 已为 `PageBtn_N` 增加规则解析，朗读为“第 N 页，开启/关闭”
- **声音设置页面导航修复**:
  - 问题：切换到 SOUND 标签后，设置菜单没有重新打开，旧的 Explicit 导航仍指向 Setting 标签页控件，方向键无法进入声音页滑块
  - 依据：日志显示设置菜单打开时只执行一次 `FixNavigation`，并且 `activeInHierarchy` 仍能扫描到隐藏标签页下的 Slider/Toggle，无法判断当前视觉可见页
  - 已修复：检测 `SoundToggle` 状态变化后重新计算设置菜单导航，并在导航过滤中排除 CanvasGroup 透明或不可交互的隐藏控件
- **键盘导航模型优化**:
  - 问题：旧导航算法按屏幕空间寻找最近控件，同时给上下左右都绑定焦点目标；复杂布局中方向键会横跳，左右键也会离开滑块
  - 已修复：菜单导航改为线性顺序，上/下键浏览控件；不再为左/右设置焦点目标，让 Slider 保留左右键调值行为
  - 设置菜单优先顺序：标签页、当前页滑块/开关、关闭/回标题/退出等底部按钮
  - 对没有固定对象名的滑块（如多个 `UI_VoiceSlider`）增加附近标题文本解析，避免读出内部对象名
- **存档/读档切换后导航修复**:
  - 问题：在存档/读档界面点击 `SaveButton` 或 `LoadButton` 后，当前按钮会随面板切换隐藏，`EventSystem` 焦点被清空后没有重新落到新面板控件
  - 已修复：检测 `SaveLoadMenu.PresentationMode` 变化后重新计算导航；若当前焦点无效，自动选中新面板的首个可见槽位
- **读档后当前对话朗读兜底**:
  - 问题：快速读档或从标题读档可能恢复到已经打印完成、正在等待输入的文本状态，不一定重新触发 `OnPrintFinished`
  - 已修复：订阅 `IStateManager.OnGameLoadFinished`，读档完成后延迟数帧读取当前 `ITextPrinterActor.Messages` 最后一条并朗读
- **存档/读档菜单后续修复**:
  - 问题：切换到另一个面板后切换按钮可能隐藏；返回剧情后若没有新的 `PrintText`，剧情朗读可能停住
  - 已修复：在存档/读档菜单保持当前面板对应切换按钮可见；菜单关闭回剧情后主动朗读当前文本打印器最后一条消息
  - 导航顺序调整：存档槽位后紧跟该槽位的删除按钮，再进入下一个槽位；页码/翻页/切换/返回按钮排在槽位区域之后

### 2026-06-21: 完善Mod配置系统
- **配置项**:
  - Verbosity（详细程度）：0=最小, 1=正常, 2=详细
  - AnnounceEmptyStates（播报空状态）：是否播报空列表/空库存等
- **游戏内设置菜单**:
  - 快捷键：Ctrl+F11 打开/关闭设置菜单
  - 导航：上下方向键切换选项，左右方向键修改值
  - 自动保存：关闭菜单时自动保存配置到文件
- **配置文件路径**: BepInEx/config/com.boz700908.HuXiangLianPianAccessibility.cfg

### 2026-06-21: 从MelonLoader切换到BepInEx
- **原因**: 游戏目录是中文的（"狐想恋翩-梦妹以求- Demo"），MelonLoader处理中文目录会乱码，导致Mod无法正常加载
- **切换内容**:
  - 项目文件从MelonLoader引用改为BepInEx引用
  - Main.cs从MelonMod改为BaseUnityPlugin
  - 日志系统从MelonLogger改为BepInEx的ManualLogSource
  - 配置系统从MelonPreferences改为BepInEx ConfigFile
  - 编译输出从Mods/目录改为BepInEx/plugins/目录
  - dist目录更新为BepInEx版本
  - INSTALL.md更新为BepInEx版本
- **BepInEx版本**: 5.4.22.0 x64

## 技术分析总结

### 游戏引擎
- **引擎**: Unity + Naninovel 视觉小说引擎
- **运行时**: Mono (64位)
- **资源管理**: Unity Addressables
- **角色动画**: Live2D
- **Naninovel类数量**: 972个类

### 自定义代码分析
- **Assembly-CSharp.dll**: 62KB，31个自定义类
- **主要命名空间**:
  - (global): 基础UI控件
  - Huxiang.Naninovel: 游戏自定义Naninovel扩展
  - NananaGames.NaniExt.UI: Naninovel UI扩展
  - NananaGames.UI: UI相关

### Naninovel核心API（已确认）

#### 1. 引擎服务访问
通过 `Engine.GetService<TService>()` 获取所有服务：
- `IUIManager` - UI管理器
- `ITextPrinterManager` - 文本打印机管理器
- `IScriptPlayer` - 脚本播放器
- `IInputManager` - 输入管理器
- `IChoiceHandlerManager` - 选项管理器
- `IAudioManager` - 音频管理器
- `ICustomVariableManager` - 自定义变量管理器
- `ILocalizationManager` - 本地化管理器
- 等等...

#### 2. 文本打印系统（核心无障碍功能）
**ITextPrinterManager 关键事件：**
- `OnPrintStarted` - 打印开始时触发
- `OnPrintFinished` - 打印完成时触发

**PrintMessageArgs 包含：**
- Printer - 打印机
- Message - 消息内容（Text + Author）
- Append - 是否追加
- Speed - 打印速度

**重要特性：**
- `LocalizableText` 支持隐式转换为 `string`，可直接使用
- 无需Harmony补丁，直接订阅C#事件即可实现文本朗读

#### 3. UI系统
**IUIManager 接口：**
- `GetUI<T>()` - 获取指定类型的UI
- `AnyModal` - 是否有模态UI打开
- `AddModalUI` / `RemoveModalUI` - 模态UI管理

**24个UI接口（Naninovel.UI命名空间）：**
- `ITitleUI` - 标题界面
- `ISettingsUI` - 设置界面
- `ISaveLoadUI` - 存档/读档界面
- `IBacklogUI` - 对话历史
- `IPauseUI` - 暂停界面
- `IConfirmationUI` - 确认对话框
- `IToastUI` - 提示消息
- `ITipsUI` - 提示/小贴士
- `ICGGalleryUI` - CG画廊
- 等等...

#### 4. 脚本播放器
**IScriptPlayer 事件：**
- `OnPlay` / `OnStop` - 播放/停止
- `OnCommandExecutionStart` / `OnCommandExecutionFinish` - 命令执行
- `OnSkip` - 跳过状态变化
- `OnAutoPlay` - 自动播放状态变化
- `OnWaitingForInput` - 等待输入状态变化

#### 5. 输入系统
**IInputManager 接口：**
- `GetSampler(string bindingName)` - 获取输入采样器
- `InputMode` - 输入模式
- `OnInputModeChanged` - 输入模式变化事件

### 关键发现
1. **事件驱动架构**：Naninovel有丰富的C#事件系统，大部分功能可以通过订阅事件实现，无需Harmony补丁
2. **文本朗读简单**：通过 `OnPrintFinished` 事件可以轻松实现对话文本朗读
3. **UI接口完整**：24个标准UI接口，可通过 `IUIManager.GetUI<T>()` 获取
4. **自定义代码少**：游戏自定义代码很少，主要功能由Naninovel引擎提供，Mod开发主要针对Naninovel标准API
5. **LocalizableText友好**：支持隐式转换为string，处理文本非常方便
6. **中文目录支持**：必须使用BepInEx，MelonLoader不支持中文目录会乱码

### 配置管理
- 使用 BepInEx ConfigFile 进行配置管理
- 配置文件自动保存到 BepInEx/config/com.boz700908.HuXiangLianPianAccessibility.cfg
- 已实现的配置项：
  - Verbosity（详细程度）：0=最小, 1=正常, 2=详细
  - AnnounceEmptyStates（播报空状态）：是否播报空列表/空库存等
- 支持游戏内设置菜单（Ctrl+F11打开，方向键导航，左右键修改值）
- 所有设置可配置，不硬编码

### 路径配置
- 使用 Directory.Build.props 配置游戏路径
- 用户可修改 GameDir 变量指向自己的游戏目录
- 示例文件: Directory.Build.props.example

### 反编译工具
- **工具**: ilspycmd 8.2.0.7535
- **运行时**: .NET 6.0.36
- **安装位置**: /home/user/.dotnet/tools/ilspycmd
- **输出目录**: decompiled/（已在.gitignore中）
# 2026-06-30: 屏幕阅读器输出默认不打断
- 根因：`ScreenReader.Say` 的默认参数是 `interrupt = true`，所有未显式传参的朗读都会中断当前屏幕阅读器输出。
- 修复：`ScreenReader.Say` 默认改为 `interrupt = false`，运行代码中的现有调用全部变为不打断输出；文档示例同步改为默认排队，只有明确紧急场景才显式传 `true`。

# 2026-06-30: 标题界面存读档导航与确认框朗读
- 根因：从标题界面进入读档菜单时，`ITitleUI.Visible` 仍为 true；旧检测顺序在识别到 `ISaveLoadUI.Visible` 后又被 Title 覆盖，导致 SaveLoad 导航优化没有执行。
- 修复：调整菜单检测优先级，先记录 Title，再让 Settings/SaveLoad/Backlog/Confirmation 覆盖；标题界面进入的存读档菜单现在也使用同一套线性导航、面板切换刷新和删除按钮顺序。
- 根因：删除存档确认内容通过 `ConfirmationPanel.Confirm(string message)` 入参传入，`ConfirmationPanel` 不公开保存消息；仅扫描 UI 子文本不可靠。
- 修复：新增运行时 Harmony 补丁捕获 `Confirm(string message)` 入参，确认框出现时由 `MenuHandler` 优先朗读捕获到的原始消息。

# 2026-06-30: 防止游戏未加载完成时快速存读档损坏
- 现象：用户在游戏还没加载完成时触发快速存档，随后快速读档后没有可朗读对话。
- 根因：`Main.QuickSave()` 直接调用 Naninovel `IStateManager.QuickSave()`，引擎允许保存当前 `PlaybackSpot`；早期状态下生成的快速存档包含空 `scriptPath`，例如 `{"scriptPath":"","lineIndex":0,"inlineIndex":0}`。
- 本地处理：已将坏档 `GameQuickSave001.nson` 备份为 `GameQuickSave001.invalid-20260630.nson`，并把前一个有效快速存档提升为新的 `GameQuickSave001.nson`。
- 修复：快速存档现在要求引擎已初始化、剧情已开始、播放位置包含有效脚本路径，并阻止在标题、设置、存读档、历史、确认框界面中快速存档；快速读档会先验证目标快速存档的播放位置，坏档不再读取。

# 2026-06-30: 手动存读档同样防止无效剧情位置
- 现象：存读档菜单中的手动槽位仍可在剧情未就绪时保存，或读取 `PlaybackSpot` 无效的坏档。
- 根因：游戏自定义 `NananaGames.UI.SaveLoadMenu` 的槽位点击直接调用 `stateManager.SaveGame(slotId)` / `stateManager.LoadGame(slotId)`，不经过 `Main.QuickSave()` / `Main.QuickLoad()`。
- 修复：新增 `SaveLoadMenuPatcher` 和共享 `SaveLoadGuard`；手动存档在引擎未就绪、剧情未开始、播放位置无效或其它存读档正在进行时会被阻止；手动读档会在菜单隐藏和游戏重置前预读并验证目标存档的播放位置，坏档会朗读“这个存档无效，无法读取”并留在菜单中。
- 同步调整：启动提示 `mod_loaded` 去掉“按F1查看帮助”。

# 2026-06-30: Steam Deck 手柄与明眼玩家可视反馈
- 新增启动可视提示：每次启动显示一次键盘和手柄常规操作方法，包括上下选择、左右调节、确认/返回、F1-F4 和 R3 快捷。
- 新增当前选中项描边高亮：`MenuHandler` 在焦点变化时给当前 `Selectable` 的实际图形组件加高对比描边，焦点清空或菜单关闭时恢复。
- 新增 `GamepadHandler`：支持 Steam Deck / Xbox 风格手柄输入检测、轴输入防抖、菜单导航兜底和 R3 快捷模式。
- R3 快捷模式：上快速存档、下快速读档、左打开存档菜单、右打开读档菜单，B 或再次 R3 取消；动作继续复用既有 `SaveLoadGuard`。
- 调试日志：首次检测到手柄按钮、左摇杆/方向轴、可能的右摇杆轴时写入 BepInEx 日志，方便核对 Steam Input 映射。
