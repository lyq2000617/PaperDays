# PaperDays

**把日历和每日计划放在桌面上。**  
**A calendar and daily planner for your Windows desktop.**

[中文介绍](#中文介绍) · [English](#english)

## 中文介绍

PaperDays 是一款 Windows 桌面日历应用，将日程、待办、国家与地区假日，以及可选的星座与民俗占卜组件放在桌面上。你可以选择透明玻璃或彩色皮肤，调整组件的位置和大小，让日常安排融入自己的桌面。

当前版本：**v0.58**。项目仍在持续改进，欢迎反馈实际使用中遇到的问题。

### 主要功能

- **日历与日程**：月视图、周视图、农历显示；添加和编辑日程，设置时间、提醒、颜色、圆点或 emoji 标识。
- **每日计划**：直接在桌面输入待办，标记完成或点击右侧 × 删除；查看全部记录，以及周、月回顾。
- **自由布局**：日历、每日计划、星座／占卜组件可分别显示或隐藏，支持移动和调整大小；隐藏一个组件后，其他组件不会自动占用它的位置。
- **外观与皮肤**：提供彩色皮肤和浅色、深色玻璃皮肤；支持桌面组件与铺满屏幕两种显示方式。玻璃皮肤可分别调整外围与内容透明度，也可选择日期起伏效果。
- **日历来源管理**：管理个人日历和多个国家／地区的假日，支持 ICS、JSON 日历导入及课表导入。
- **可选娱乐组件**：查看星座运势，或使用数字小六壬；详情页区分来源内容与应用补充解读。
- **桌面使用支持**：显示屏幕选择、托盘菜单、开机自启动、新手引导，以及简体中文、繁体中文、英文、韩文、日文界面。

### 快速开始

1. 下载发布压缩包，并**完整解压**到一个固定文件夹。
2. 运行 `PaperDays.exe`。请保留同目录下的配置文件及 `emoji`、`holidays` 等资源。
3. 首次启动时选择语言，再按新手引导完成基本设置。
4. 在“工具”中调整皮肤、显示屏幕和组件设置；双击日期添加日程，点击“添加待办”在桌面输入计划。

升级时，请先从系统托盘退出旧版，再运行新版。建议先通过应用的备份功能保存数据。若启用了开机自启动，请尽量保持程序文件夹的位置不变。

### 使用环境

- Windows 桌面环境。
- .NET Framework 4.5.2 或更高兼容版本（当前程序配置的运行时要求）。
- 获取最新假日、在线星座内容及机器翻译时需要网络；本地日程、待办与数字小六壬不依赖这些在线服务。

桌面嵌入、玻璃效果和多屏布局会受到 Windows 版本、显示缩放及桌面环境影响。目前尚未完成所有 Windows 版本与多显示器组合的实机验证。

### 数据与内容

日程、待办、设置和缓存保存在当前 Windows 用户的本地应用数据目录中。更换程序文件夹不会等同于删除这些数据。

在线假日、星座内容及部分翻译来自第三方服务，其可用性、完整性和更新时间取决于来源。翻译功能会将需要翻译的运势文本发送给翻译服务。来源未提供的运势项目不会被当作完整来源数据展示；额外建议会标注为应用补充。

星座与小六壬功能用于民俗娱乐和阅读参考，不代表真实预测。

### 从源码构建

项目使用 **C#、Windows Forms、System.Drawing 和 .NET Framework**。

在包含 `build.ps1` 的源码根目录打开 PowerShell：

```powershell
.\build.ps1
```

生成的程序为 `PaperDays.exe`。现有脚本使用 Windows 下的 64 位 .NET Framework C# 编译器：

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

请使用包含图标、配置和运行资源的完整源码包。构建脚本负责编译程序，不会自动下载或打包外部资源。

构建测试程序：

```powershell
.\build.ps1 -Test
```

生成 `PaperDays.Tests.exe`。例如，在独立测试目录运行待办检查：

```powershell
$testDir = Join-Path $env:TEMP 'PaperDays-readme-test'
Start-Process -FilePath '.\PaperDays.Tests.exe' -ArgumentList '--test57', $testDir -Wait
```

测试目录会写入测试数据及结果，请勿使用个人日历数据目录。测试名称保留引入时的版本编号；v0.58 的待办排列与删除检查也包含在 `--test57` 中。

### 反馈问题

提交 Issue 时，请尽量提供：程序版本、Windows 版本、显示缩放比例、屏幕数量、使用的皮肤、复现步骤，以及预期与实际表现。截图前请遮挡私人日程或个人信息。

### 许可证与第三方资源

项目源码的授权范围以仓库中的 `LICENSE` 文件为准；未提供许可证时，请勿默认按 MIT 等开源许可证使用。第三方资源遵循各自的授权说明，请保留随包提供的声明，例如 `holiday-cn-LICENSE.txt`。

---

## English

PaperDays is a Windows desktop calendar that brings events, tasks, national and regional holidays, and optional horoscope and traditional divination widgets onto your desktop. Choose a glass or colored theme, then arrange and resize the components to suit your workspace.

Current version: **v0.58**. The project is under active development, and feedback from everyday use is welcome.

### Features

- **Calendar and events** — Month and week views, Chinese lunar dates, event editing, reminders, colors, dots, and emoji markers.
- **Daily planner** — Enter tasks directly on the desktop, mark them complete, or delete them with the × button. Browse task records and weekly or monthly reviews.
- **Flexible layout** — Show, hide, move, and resize the calendar, planner, and horoscope/divination components separately. Hiding a component does not automatically expand the others into its space.
- **Themes and glass effects** — Colored themes and light/dark glass themes, with desktop-widget and full-screen layouts. Adjust outer-area and content transparency separately, with an optional date relief effect.
- **Calendar sources** — Manage personal calendars and holidays for multiple countries or regions; import ICS and JSON calendars, as well as class timetables.
- **Optional entertainment widgets** — Read horoscopes or use numerical Xiao Liu Ren, a traditional Chinese divination method. Detail views distinguish source material from additional app commentary.
- **Desktop conveniences** — Display selection, a system-tray menu, launch at sign-in, onboarding, and interfaces in Simplified Chinese, Traditional Chinese, English, Korean, and Japanese.

### Getting started

1. Download a release archive and **extract the entire package** into a permanent folder.
2. Run `PaperDays.exe`. Keep its configuration file and resource folders, including `emoji` and `holidays`, alongside the executable.
3. Select your language on first launch, then follow the onboarding guide.
4. Use **Tools** to configure themes, displays, and components. Double-click a date to add an event, or select **Add task** to type directly in the desktop planner.

Before upgrading, exit the previous version from the system tray. Back up your data through the app first. If launch at sign-in is enabled, keep the application folder in a stable location.

### Requirements

- A Windows desktop environment.
- .NET Framework 4.5.2 or a later compatible version, as specified by the current application configuration.
- Internet access for updated holidays, online horoscope content, and machine translation. Local events, tasks, and numerical Xiao Liu Ren do not depend on these services.

Desktop embedding, glass effects, and multi-monitor layouts can vary with Windows versions, display scaling, and desktop configuration. Not every Windows version or multi-monitor combination has been tested on physical hardware.

### Data and content

Events, tasks, settings, and caches are stored in the current Windows user's local application data directory. Moving the program folder does not itself remove this data.

Online holidays, horoscopes, and some translations rely on third-party services. Availability, completeness, and update times depend on those providers. Translation sends the horoscope text being translated to a translation service. Missing source content is not presented as a complete forecast; additional guidance is labeled as app commentary.

Horoscope and Xiao Liu Ren features are intended for cultural entertainment and reflection, not factual prediction.

### Building from source

PaperDays uses **C#, Windows Forms, System.Drawing, and .NET Framework**.

Open PowerShell in the source directory containing `build.ps1`:

```powershell
.\build.ps1
```

This produces `PaperDays.exe`. The script uses the 64-bit .NET Framework C# compiler at:

```text
%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
```

Use the complete source package, including the icon, configuration, and runtime resources. The build script compiles the application; it does not download or package external assets.

To build the test executable:

```powershell
.\build.ps1 -Test
```

For example, run the task checks in a dedicated test directory:

```powershell
$testDir = Join-Path $env:TEMP 'PaperDays-readme-test'
Start-Process -FilePath '.\PaperDays.Tests.exe' -ArgumentList '--test57', $testDir -Wait
```

Tests write data and results into that directory. Never point them at your personal calendar data. Test names retain their original version numbers; the v0.58 task-layout and deletion checks are included in `--test57`.

### Reporting issues

Please include the application version, Windows version, display scaling, number of monitors, selected theme, reproduction steps, and expected versus actual behavior. Remove private events and personal information from screenshots before sharing them.

### License and third-party assets

The repository's `LICENSE` file defines the terms for the project source. If no license is provided, do not assume an MIT or other open-source license. Third-party assets retain their respective terms; preserve their accompanying notices, including `holiday-cn-LICENSE.txt` where supplied.
# PaperDays
A customizable Windows desktop calendar and daily planner with glass themes.
