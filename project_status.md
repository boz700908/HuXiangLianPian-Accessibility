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
