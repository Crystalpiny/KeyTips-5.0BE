# 移植说明：Key Tips 1.1.0 → SPT 5.0BE

原 mod：**Key Tips 1.1.0**（HiddenCirno，MIT，[sp-mod.com/mod/2798/key-tips](https://sp-mod.com/mod/2798/key-tips)、
[HiddenCirno/KeyTips](https://github.com/HiddenCirno/KeyTips)），为 SPT 4.1.x（Mono 客户端、netstandard2.1、
BepInEx 5 `BaseUnityPlugin`）构建。本仓库为 SPT 5.0BE（IL2CPP 客户端、BepInEx 6）移植版，
插件名 `KeyTips.Client.dll`，**GUID 保持上游不变**（`spark.hiddenhiraigi.keytips`）。

## 静态校验结果（2026-10-20，对象：`BepInEx\interop\Assembly-CSharp.dll`）

用 `_audit\ildump` 逐符号核对上游补丁用到的每个游戏成员：

| 上游用法 | 5.0BE 状态 | 处理 |
|---|---|---|
| `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, WorldInteractiveObject)` | ✅ 在 | 不变 |
| `InteractionContextHelper.GetAvailableActions(GamePlayerOwner, KeycardDoor, Boolean)` | ✅ 在 | 不变 |
| `EFT.UI.AvailableInteractionState.Error` | ✅ 在（引用类型，`ref __result` 语义不变） | 不变 |
| `WorldInteractiveObject.DoorState` / `.KeyId` | ✅ 属性与字段均在 | 不变 |
| `WorldInteractiveObject.KeycardDoor : Door : WorldInteractiveObject` | ✅ 继承链未变 | 不变（`UnlockOperation` 打两处，与原上游一致：基类方法虚表被派生类重写，只打基类不会命中刷卡门） |
| `WorldInteractiveObject.UnlockOperation` / `KeycardDoor.UnlockOperation` | ⚠️ 变成 3 参：`(KeyComponent key, Player player, WorldInteractiveObject wio)` | **无需改**：Harmony postfix 按参数名绑定，且 `key` / `player` 两个名字未变 |
| `Diz.LanguageExtensions.Option<UnlockResult>` | ✅ 在，5.0BE 为 **ValueType**（结构体）；`.Failed` / `.Value` 均可用 | `ref` 传递正好匹配结构体返回值 |
| `EFT.Interactive.UnlockResult.Succeed` | ✅ 在 | 不变 |
| `KeyComponent.NumberOfUsages` | ✅ 在 | 不变 |
| `KeyComponent.Template` → `IKeyComponentTemplate` | ✅ `.KeyId` / `.MaximumNumberOfUsage` 都在接口上 | 不变 |
| `Player.IsYourPlayer` / `.Profile.Info.Nickname` | ✅ 在（`Profile` → `ProfileInfo`） | 不变 |
| `EDoorState.Locked`、`ENotificationDurationType`、`ENotificationIconType` | ✅ 在 | 不变 |
| `NotificationManager.DisplayMessageNotification(...)` | ⚠️ 仍是 static，但只剩一个 **5 参** 重载：`(string, ENotificationDurationType, ENotificationIconType, Color? textColor, bool rawText)` | 原 4 参 + 末尾 `null` 的写法编译不过 → 补 `default, false` |
| `LocalizationManager._instance.LocalizedValue(string)` | ⚠️ 公开入口是属性 `Instance`（`LocalizedValue(string)` 签名不变） | 改为 `LocalizationManager.Instance` |
| `GameWorld.OnGameStarted` | ✅ 在 | 上游用它捕获的 `PluginsCore.CorrectPlayer/CorrectGameWorld/CorrectGroupId` **在 TipsPatch 里从未被使用** → 整段死代码删除 |

`Assembly-CSharp` 侧的符号引用由编译器全部解析通过（0 错误 0 警告），产出的 dll 只引用
`Assembly-CSharp` / `Il2Cppmscorlib` / `UnityEngine` 无关的 5 个程序集 + 托管 `System.Text.Json`，
未泄漏对游戏内 Newtonsoft 的依赖。

## 相对 4.1.x 原版源码的改动

**构建体系**

- `netstandard2.1` → `net10.0`；引用从 `EscapeFromTarkov_Data\Managed\*` 换成 `BepInEx\core` + `BepInEx\interop`。
- 上游 csproj 里的 `System.Windows.Forms`、`System.Drawing.Design`、`DissonanceVoip`、`ItemComponent.Types`、
  `Sirenix.Serialization` 等引用实际都没用到，删除。
- 加了 `ValidateGameRoot` 目标（指错目录会直接报错而不是给出难懂的编译错误）与 `-p:DeployToSPT=true` 部署目标。

**BepInEx 6 插件形态**

- `BaseUnityPlugin` + `Awake()` + `harmony.PatchAll()` → `BepInEx.Unity.IL2CPP.BasePlugin` + `Load()`。
- 空的 `Start()` / `Update()` / `OnGUI()` 删除（`BasePlugin` 不是 MonoBehaviour，这些不会被调用）。
- 加 `[BepInDependency("com.sptushonka.core", "5.0.0")]`（5.0BE 核心插件 GUID，原为 `com.SPT.core` 4.1.0）。
- 补丁改为 `harmony.CreateClassProcessor(type).Patch()` 逐个启用并各自 try/catch + 日志，
  与本地 OpenSesame BE / SearchOpenContainers 移植保持一致：单个补丁失败不影响其余三个，日志能直接看出
  生效了几个、目标方法是什么。

**语言包**

- 插件自己的 JSON 从游戏内 `Newtonsoft.Json` 改为托管 `System.Text.Json`。原因：interop 出来的
  `Newtonsoft.Json.dll` 是 IL2CPP 类型代理，`[JsonProperty]` 不是可用的特性类（CS0616），且 IL2CPP 版
  Newtonsoft 反射不了插件程序集里的托管 POCO。游戏自带 .NET 10 运行时有 `System.Text.Json`，实机可加载。
- 语言目录不存在时改为记一条 warning 并退回英文键名，不再让 `Directory.GetFiles` 抛异常拖垮整个插件加载。
- 错误日志从 `Console.WriteLine` 改为 BepInEx `ManualLogSource`（进 `LogOutput.log`）。
- 上游的 `ConfigurationManagerAttributes.cs`（一个没人用的空壳类）删除。

**未改动**

- 提示文案、语言包格式与内容、`i18n()` 语法糖、`LocalizationManager` 取钥匙名的回退逻辑、
  以及"普通门要 `__result.Error` 非空才提示 / 刷卡门不判 Error"的上游条件，全部保持原语义。

## 待实机确认（本次移植未进战局）

1. 启动日志出现 4 条 `补丁 <名字> 已启用` 与 `4/4 个补丁生效`；
2. 对需要钥匙且上锁的门按交互键 → 顶部弹出「锁上了（需要 X）」，X 是钥匙中文名而不是模板 Id；
3. 用钥匙成功开门 → 「使用了 X (剩余 n/m)」；用到最后一次 → 告警图标那条；
4. 刷卡门（实验室 / 变电站等）两条提示正常；
5. F12 配置菜单里 `Language Opinion / 语言设置` 能列出 English 与简体中文，切换后重启生效。

## 已知说明与限制

- **队友分支同样对 AI 触发**：`UnlockOperation` 的 postfix 不分玩家，SPT 里 bot 拿钥匙开门也会弹
  「XX 使用了 X」。这是上游 4.1 版的原有行为（上游代码里还留着 `//明天加个玩家兼容` 的 TODO），
  本移植保持忠实、未擅自加过滤。若实测嫌吵，只需在 `KeyTipsFeedback.OnKeyUsed` 的非本人分支前
  加一道 bot 判定（OpenSesame BE 的 `InteractionHelpers.IsInteractorABot` 可直接借）。
- **Fika 联机未测试**。
- IL2CPP 下 AOT 内联理论上会让 postfix 落空，但这四个目标都是 `InteractionContextHelper` / `UnlockOperation`
  这类不会被内联的大方法，且 OpenSesame BE 打的是同样两个 `GetAvailableActions` 重载。
