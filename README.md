# Key Tips — SPT 5.0BE 移植版

锁着的门告诉你它要哪把钥匙；用钥匙开门时告诉你还剩几次；钥匙用到最后一次会红字告警。

本仓库是 **[Key Tips 1.1.0](https://sp-mod.com/mod/2798/key-tips) 到 SPT 5.0BE（IL2CPP 客户端）的移植版**。
原作作者 **[HiddenCirno](https://github.com/HiddenCirno/KeyTips)**（MIT License），原作为 SPT 4.1.x
（Mono 客户端 / BepInEx 5 / netstandard2.1）构建。本移植只动编译体系与 IL2CPP 适配，
四个补丁的语义、提示文案和语言包机制都与上游一致。功能想法全部是原作者的，
如果这个 mod 帮到你，请去 [原 mod 页面](https://sp-mod.com/mod/2798/key-tips) 点赞致谢。

## 功能

| 场景 | 提示 |
|---|---|
| 对着需要钥匙的门/铁门按交互键，但身上没有对应钥匙 | `锁上了（需要 <钥匙名>）` |
| 刷卡门被锁 | `已锁定（需要 <卡名>）` |
| 用钥匙成功开门 | `使用了 <钥匙名> (剩余 n/m)` |
| 钥匙用掉最后一次 | ` <钥匙名> 的所有可用次数已耗尽`（告警图标） |
| 队友（含 AI）用钥匙开门 | `<昵称> 使用了 <钥匙名> (剩余 n/m)` |

内置中英文语言包（`locales\en-us.json`、`locales\zh-cn.json`），F12 配置菜单可切换，改语言需重启游戏。

## 形态

纯客户端插件，无服务端部分：`BepInEx\plugins\KeyTips\KeyTips.Client.dll` + `locales\`。
四个 Harmony **Postfix**（只追加提示，不改游戏原行为）：

1. `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, WorldInteractiveObject)`
2. `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, KeycardDoor, bool)`
3. `WorldInteractiveObject.UnlockOperation(KeyComponent, Player, WorldInteractiveObject)`
4. `KeycardDoor.UnlockOperation(KeyComponent, Player, WorldInteractiveObject)`

## 构建

需要 .NET 10 SDK（游戏自带运行时 10.0.12，`Il2CppInterop.Runtime` 按 net10 编译，插件必须同框架），
并且手上要有一份 SPT 5.0BE 安装目录 —— 编译只读它的 `BepInEx\core` 与 `BepInEx\interop`，
不打包任何游戏文件。用 `EFT5_ROOT` 环境变量或 `-p:GameRoot=` 指过去（仓库里不写死任何人的本机路径）：

```powershell
cd _src\KeyTips-5.0BE\src
$env:EFT5_ROOT = "D:\Path\To\SPT5.0BE"                                        # 方式一：环境变量
& "$env:USERPROFILE\.dotnet\dotnet.exe" build -c Release                       # 只编译
& "$env:USERPROFILE\.dotnet\dotnet.exe" build -c Release -p:DeployToSPT=true   # 编译并拷进 plugins\KeyTips

# 方式二：按次传参，效果同上
& "$env:USERPROFILE\.dotnet\dotnet.exe" build -c Release -p:GameRoot="D:\Path\To\SPT5.0BE"
```

没给 `GameRoot`/`EFT5_ROOT` 时构建会直接报错并告诉你怎么传；`interop` 程序集若不存在，
先用 SPT Launcher 启动一次游戏生成它们。

## 下载

直接用（解压到 SPT 5.0BE 根目录即可）：
**[Releases](https://github.com/Crystalpiny/KeyTips-5.0BE/releases)** → `KeyTips-1.1.0-SPT5.0BE.zip`
（含 `KeyTips.Client.dll` + `locales\`，纯客户端，无服务端部分）

## 与 4.1.x 原版的差异

见 [docs/PORTING-NOTES.zh.md](docs/PORTING-NOTES.zh.md)。要点：`BaseUnityPlugin`/`Awake` →
`BasePlugin`/`Load()`、依赖 GUID 换成 `com.sptushonka.core` 5.0.0、
`NotificationManager.DisplayMessageNotification` 补 5.0BE 新增的两个尾参、
`LocalizationManager._instance` → `.Instance`、语言包改用托管 `System.Text.Json`。

## 状态

**静态校验通过，实机验收待做**（本次移植没有进战局跑过）。请按
[docs/INSTALL.zh.md](docs/INSTALL.zh.md) 的清单验证。

## 许可

MIT。`LICENSE` 保留上游原文（Copyright © HiddenCirno）；SPT 5.0BE 移植部分的归属与致谢见上，
本项目不改变上游许可条款。
