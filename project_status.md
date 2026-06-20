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
- **Mod加载器**: MelonLoader
- **目标框架**: net472

## 开发进度

### Phase 0: 项目设置
- [x] 游戏分析（引擎、架构、运行时）
- [x] Mod加载器选择（MelonLoader）
- [x] Tolk屏幕阅读器集成（从boz700908/tolk仓库下载）
- [x] .NET SDK 配置（.NET 8.0.422 + .NET Core 3.1.32）
- [x] GitHub 仓库创建
- [x] 项目结构搭建
- [x] MelonLoader 安装（v0.7.3 x64）
- [x] 反编译游戏代码（dotnet-ildasm，31个自定义类）
- [x] 首次编译测试（0警告0错误）
- [x] 游戏API文档（docs/game-api.md）
- [x] Naninovel版本确认（Naninovel.Common 2025.3.1546.19）

### Phase 1: 代码分析
- [ ] Tier 1: 结构概览
- [ ] Tier 1: 输入系统
- [ ] Tier 1: UI系统
- [ ] Tier 1: 状态管理
- [ ] Tier 1: 本地化
- [ ] Tier 2: 游戏机制
- [ ] Tier 2: 状态系统
- [ ] Tier 2: 事件系统

### Phase 2: 基础框架
- [ ] 主菜单导航
- [ ] 对话文本朗读
- [ ] 设置菜单
- [ ] 存档/读档

### Phase 3: 功能完善
- [ ] 完整菜单支持
- [ ] 游戏内UI导航
- [ ] 快捷键系统
- [ ] 调试模式

## 技术栈
- **语言**: C#
- **Mod框架**: MelonLoader + Harmony
- **屏幕阅读器**: Tolk
- **游戏引擎**: Unity + Naninovel

## 仓库地址
https://github.com/boz700908/HuXiangLianPian-Accessibility

## 开发模式
- Linux 工作区编译测试
- Windows 真机测试
- GitHub 代码同步

## 技术分析总结

### 游戏引擎
- **引擎**: Unity + Naninovel 视觉小说引擎
- **运行时**: Mono (64位)
- **资源管理**: Unity Addressables
- **角色动画**: Live2D

### 自定义代码分析
- **Assembly-CSharp.dll**: 62KB，31个自定义类
- **主要命名空间**:
  - (global): 基础UI控件
  - Huxiang.Naninovel: 游戏自定义Naninovel扩展
  - NananaGames.NaniExt.UI: Naninovel UI扩展
  - NananaGames.UI: UI相关
- **完整自定义类列表**:
  - **基础UI控件**:
    - ControlPanelSettingsButton: 控制面板设置按钮
    - ScriptableToggle: 可脚本化开关基类
    - RightClickButtonTrigger: 右键按钮触发器
    - ScrollWheelBacklog: 滚轮历史记录
    - GameSystemQuitButton: 游戏退出按钮
    - GameSystemTitleButton: 返回标题按钮
    - TitleSettingsButton: 标题设置按钮
  - **设置相关**:
    - SettingsUI: 设置菜单（继承GameSettingsMenu，含BackUIType属性）
    - GameSettingsResolutionToggle: 分辨率开关
    - GameSettingsScreenModeToggle: 屏幕模式开关
    - GameSettingsVoiceInterruptToggle: 语音中断开关
    - GameSettingsSkipModeToggle: 跳过模式开关
    - GameSettingsReturnButton: 设置返回按钮
  - **存档读档相关**:
    - NananaGames.UI.SaveLoadMenu: 存档读档菜单（实现ISaveLoadUI接口）
    - NananaGames.UI.SaveLoadSlot: 存档槽
    - NananaGames.UI.SaveLoadSlotsGrid: 存档槽网格
    - NananaGames.UI.SaveLoadGridPageToggle: 存档分页开关
    - NananaGames.UI.SaveLoadSwitchPanelButton: 切换保存/加载面板按钮
    - NananaGames.UI.SaveLoadMenuQuitButton: 存档菜单退出按钮
    - NananaGames.UI.SaveLoadMenuReturnButton: 存档菜单返回按钮
    - NananaGames.UI.SaveLoadMenuReturnTitleButton: 存档菜单返回标题按钮
  - **控制面板相关**:
    - NananaGames.UI.ControlPanelAutoPlayToggle: 自动播放开关
    - NananaGames.UI.ControlPanelSaveLoadButton: 存档读档按钮
    - NananaGames.UI.ControlPanelSkipToggle: 跳过开关
  - **其他**:
    - Huxiang.Naninovel.AutoCharacterAuthorImage: 自动角色作者图像
    - Huxiang.Naninovel.CharacterAvatarsConfiguration: 角色头像配置
    - NananaGames.UI.ScriptableToggle: 可脚本化开关
    - NananaGames.UI.SplashSequence: 启动画面序列
    - NananaGames.UI.TitleCGGalleryButton: 标题CG画廊按钮
    - NananaGames.NaniExt.UI.ButtonSfx: 按钮音效
- **关键发现**:
  - 游戏自定义代码很少，主要功能由Naninovel引擎提供
  - 存档读档菜单是自定义的（NananaGames.UI.SaveLoadMenu）
  - 设置菜单是自定义的（SettingsUI），添加了BackUIType属性
  - 使用了NananaGames的UI框架和Huxiang的扩展

### Naninovel核心API
- **Engine**: 引擎核心，通过 `Engine.GetService<T>()` 获取服务
- **TextPrinterManager**: 文本打印机管理器
- **ScriptPlayer**: 脚本播放器
- **UIManager**: UI管理器
- **InputManager**: 输入管理器
- **ChoiceHandlerManager**: 选项处理器管理器

### 配置管理
- 使用 MelonPreferences 进行配置管理
- 配置文件自动保存到 UserData/[ModName].cfg
- 支持游戏内设置菜单（Ctrl+F11打开）
- 所有设置可配置，不硬编码

### 路径配置
- 使用 Directory.Build.props 配置游戏路径
- 用户可修改 GameDir 变量指向自己的游戏目录
- 示例文件: Directory.Build.props.example
