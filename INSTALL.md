# 安装说明

## 快速安装

1. 下载本仓库的所有文件
2. 将 `dist/` 目录下的所有文件复制到游戏根目录（与 `HuXiangLianPian.exe` 同目录）
3. 启动游戏，Mod会自动加载

## 文件说明

### 必须文件
- `version.dll` - MelonLoader 加载器
- `MelonLoader/` - MelonLoader 运行时文件
- `Mods/HuXiangLianPianAccessibility.dll` - 无障碍Mod主程序

### 屏幕阅读器支持（Tolk）
- `Tolk.dll` - Tolk 屏幕阅读器接口
- `nvdaControllerClient64.dll` - NVDA 屏幕阅读器支持
- `SAAPI64.dll` - 争渡读屏支持
- `byctrl-x64.dll` - 永德读屏支持
- `ZDSRAPI_x64.dll` - 中国盲文显示器支持
- `byctrl.conf` - 永德读屏配置
- `ZDSRAPI.ini` - 盲文显示器配置

## 系统要求

- Windows 10/11 64位
- 已安装屏幕阅读器（NVDA、争渡、永德等）
- 游戏已安装（Steam Demo或正式版）

## 注意事项

- 首次启动游戏时，MelonLoader会自动生成一些文件，请耐心等待
- 如果Mod没有加载，请检查MelonLoader的日志文件
- 游戏更新后可能需要重新安装Mod
