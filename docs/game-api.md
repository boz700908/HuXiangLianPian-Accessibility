# 狐想恋翩 - 游戏 API 文档

## 概述
- **游戏:** 痴情妹妹纱雪的兄控日记 Demo（狐想恋翩-妹妹篇）
- **引擎:** Unity + Naninovel
- **运行时:** Mono (64位)
- **架构:** 64-Bit
- **开发者:** StationWorks / TamaMako gaming
- **游戏内部名称:** HuXiangLianPian

---

## 1. 引擎服务访问点

Naninovel 使用服务模式，通过 `Engine.GetService<TService>()` 静态方法获取各种引擎服务。

### 核心服务

- `Engine.GetService<IScriptPlayer>()` - 脚本播放器
  - 功能：控制游戏脚本的播放、暂停、跳转
  - 重要属性：`PlayedScript`, `PlaybackSpot`
  - 重要方法：`Play()`, `Stop()`, `PreloadAndPlay()`

- `Engine.GetService<ITextPrinterManager>()` - 文本打印机管理器
  - 功能：管理所有文本打印机
  - 重要方法：`GetPrinter(string id)`, `GetDefaultPrinter()`

- `Engine.GetService<IUIManager>()` - UI 管理器
  - 功能：管理所有 UI 面板
  - 重要方法：`GetUI<TUI>()`, `ShowUI()`, `HideUI()`

- `Engine.GetService<ISaveSlotManager<GameStateMap>>()` - 存档管理器
  - 功能：管理游戏存档
  - 重要方法：`SaveAsync()`, `LoadAsync()`, `Exists()`

- `Engine.GetService<IInputManager>()` - 输入管理器
  - 功能：管理游戏输入

---

## 2. 游戏按键绑定

**注意：这些是游戏默认使用的按键，Mod 不应冲突。**

### 通用操作
- **鼠标左键 / 空格 / Enter**: 推进对话
- **Ctrl**: 跳过已读文本
- **Tab**: 自动阅读模式
- **Esc**: 打开/关闭菜单

### 菜单导航
- **方向键**: 导航菜单项
- **Enter**: 确认选择
- **Esc**: 返回上一级

### 其他
- **S**: 快速存档
- **L**: 快速读档
- **F1**: 帮助（Mod 可使用）

---

## 3. Mod 可安全使用的按键

### 预留无障碍 Mod 按键
- **F1**: 帮助/快捷键说明
- **F2-F12**: 可用于 Mod 功能
- **Tab**: （游戏已用，需注意）
- **方向键**: （游戏已用，需注意）

### 建议 Mod 按键
- **F1**: 显示 Mod 帮助
- **F2**: 切换调试模式
- **F3**: 切换语音速度
- **数字键 1-9**: 快速功能

---

## 4. UI 系统

### UI 接口命名空间
所有 UI 接口都在 `Naninovel.UI` 命名空间下。

### 内置 UI 列表

**标题菜单 (Title UI)**
- 接口: `ITitleUI`
- 功能: 游戏主菜单
- 包含: 开始游戏、继续、设置、退出

**设置菜单 (Settings UI / Config UI)**
- 接口: `ISettingsUI` 或 `IConfigUI`
- 功能: 游戏设置
- 包含: 音量、文本速度、自动阅读速度等

**存档/读档菜单 (Save-Load UI)**
- 接口: `ISaveLoadUI`
- 功能: 存档和读档
- 包含: 存档槽列表、分页

**历史记录面板 (Backlog Panel)**
- 接口: `IBacklogUI`
- 功能: 查看对话历史

**CG 画廊 (CG Gallery)**
- 接口: `ICGGalleryUI`
- 功能: 查看已解锁的 CG

**Tips**
- 接口: `ITipsUI`
- 功能: 查看游戏术语解释

**控制面板 (Control Panel)**
- 功能: 切换自动阅读、跳过文本等

### 文本打印机 (Text Printers)

**接口:** `ITextPrinter`
- 属性:
  - `Text`: 当前显示的文本
  - `AuthorName`: 说话者名称
  - `IsPrinting`: 是否正在打印文本
- 方法:
  - `PrintAsync()`: 打印文本
  - `AppendTextAsync()`: 追加文本

---

## 5. 游戏机制 - 功能目录

### 对话系统
- 核心接口: `ITextPrinter`
- 文本显示: 逐字打印效果
- 说话者名称: 通过 `AuthorName` 属性
- 文本速度: 可配置

### 选项系统 (Choice Handlers)
- 功能: 显示选项供玩家选择
- 接口: `IChoiceHandler`
- 选项数量: 可变

### 存档系统
- 接口: `ISaveSlotManager<GameStateMap>`
- 存档槽: 多个存档槽
- 快速存档: 支持
- 自动存档: 支持

### 角色系统
- 接口: `ICharacterManager`
- 角色显示: Live2D 或 Sprite
- 表情变化: 支持

### 背景系统
- 接口: `IBackgroundManager`
- 背景切换: 支持过渡效果

---

## 6. 状态和通知

### 游戏状态
- 脚本播放状态: 通过 `IScriptPlayer` 获取
- 当前播放位置: `PlaybackSpot`

### 通知系统
- UI 通知: 通过 UI 管理器

---

## 7. 音频系统

- 音频管理器: `Engine.GetService<IAudioManager>()`
- BGM: 背景音乐
- SFX: 音效
- 语音: 角色语音（ASMR）

---

## 8. 存档和加载

- 存档方法: `ISaveSlotManager.SaveAsync(slotId, gameState)`
- 读档方法: `ISaveSlotManager.LoadAsync(slotId)`
- 存档位置: 游戏数据目录下的 Saves 文件夹
- 设置存档: 独立的设置存档槽

---

## 9. Harmony 补丁事件钩子

### UI 事件（最佳补丁点）

**文本显示**
- `ITextPrinter.PrintAsync()` - 当文本打印时
- 建议: Postfix，用于朗读新显示的文本

**菜单打开/关闭**
- UI 面板的 `Show()` / `Hide()` 方法
- 建议: Postfix，用于通知菜单状态变化

**选项显示**
- `IChoiceHandler` 的相关方法
- 建议: Postfix，用于朗读选项

### 输入事件
- 输入处理的相关方法
- 建议: Prefix，用于拦截或重定向输入

### 更新循环
- `MonoBehaviour.Update()` - 每帧更新
- 注意: 谨慎使用，避免性能问题

---

## 10. 本地化

- 游戏语言: 简体中文
- Naninovel 本地化: 支持 Managed Text 功能
- 文本管理: 通过脚本文件 (.nani)

---

## 11. 代码示例

### 获取引擎服务
```csharp
// 获取脚本播放器
var scriptPlayer = Engine.GetService<IScriptPlayer>();

// 获取 UI 管理器
var uiManager = Engine.GetService<IUIManager>();

// 获取文本打印机管理器
var printerManager = Engine.GetService<ITextPrinterManager>();
```

### 获取当前文本
```csharp
var printerManager = Engine.GetService<ITextPrinterManager>();
var defaultPrinter = printerManager.GetDefaultPrinter();
string currentText = defaultPrinter.Text;
string authorName = defaultPrinter.AuthorName;
```

### 检查 UI 是否打开
```csharp
var uiManager = Engine.GetService<IUIManager>();
var titleUI = uiManager.GetUI<ITitleUI>();
bool isTitleVisible = titleUI.Visible;
```

### Harmony 补丁示例
```csharp
[HarmonyPatch(typeof(SomeNaninovelClass), "SomeMethod")]
class SomeMethodPatch
{
    static void Postfix()
    {
        // 在原始方法之后执行
        ScreenReader.Say("文本已更新");
    }
}
```

---

## 12. 已知问题和解决方案

- **Naninovel 版本**: 需要确认具体版本
- **自定义代码**: 游戏自定义代码很少（Assembly-CSharp.dll 仅 62KB）
- **Live2D**: 游戏使用 Live2D 角色，可能影响无障碍

---

## 13. 待分析区域

需要进一步研究的内容：
- [ ] Naninovel 具体版本号
- [ ] 游戏自定义代码的详细分析
- [ ] 输入系统的具体实现
- [ ] 菜单导航的详细流程
- [ ] 存档系统的具体结构
- [ ] ASMR 语音的播放机制
- [ ] Addressables 资源加载方式

---

## 修改历史
- **2026-06-20**: 初始分析，基于 Naninovel 官方文档
- **2026-06-20**: 添加游戏基本信息和引擎服务概述
