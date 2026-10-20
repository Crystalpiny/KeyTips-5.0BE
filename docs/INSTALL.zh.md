# 安装与验收（SPT 5.0BE）

**Key Tips 1.1.0（SPT 5.0BE 适配版）** —— 锁着的门告诉你它要哪把钥匙，用钥匙开门时告诉你还剩几次。
原作者 HiddenCirno（MIT，[sp-mod mod 页](https://sp-mod.com/mod/2798/key-tips)、[原仓库](https://github.com/HiddenCirno/KeyTips)）。

## 安装

- 把 `KeyTips.Client.dll` 和整个 `locales\` 目录一起放到 `<SPT客户端根>\BepInEx\plugins\KeyTips\`（目录自己建）。
  **`locales\` 必须和 dll 同级**，缺了它提示会显示英文键名（如 `door_locked`）。
- 纯客户端插件，无服务端部分，不需要动 `SPT_Runtime`。
- 换 dll 必须先完全退出游戏：BepInEx 只在启动时加载插件。

## 怎么确认生效

启动游戏后打开 `<SPT客户端根>\BepInEx\LogOutput.log`，搜 `KeyTips`，应出现 4 条
`补丁 <名字> 已启用，目标：...` 和最后一条：

```
KeyTips 1.1.0 loaded (SPT 5.0BE)：4/4 个补丁生效。
```

少于 4/4 时，日志里会写明哪个补丁失败、对应哪条提示不会出现。

## 进游戏怎么验

1. 找一个需要钥匙且上锁的门（例如 Customs 的钥匙门、Labs 的门禁），身上**不带**对应钥匙，按 F 交互
   → 屏幕上方弹「锁上了（需要 XXX）」，XXX 应是钥匙中文名（如「宿舍304钥匙」），不是 `5xxxxxxx` 这种 Id；
2. 刷卡门（变电站/实验室门禁）同样应弹「已锁定（需要 XXX）」；
3. 带上对应钥匙开门 → 弹「使用了 XXX (剩余 n/m)」；
4. 把某把钥匙用到最后一次 → 追加一条告警图标的「XXX 的所有可用次数已耗尽」；
5. 打开 F12 配置菜单 → `Language / 语言` 分组下应能选 English / 简体中文，切换后**重启游戏**生效。

## 怎么卸

删除整个 `BepInEx\plugins\KeyTips\` 目录，重启游戏。

## 已知说明与限制

- 本包**静态校验通过**（四个补丁目标在 5.0BE interop 里全部命中、游戏 API 引用 0 缺失、0 警告 0 错误编译），
  **实机验收待做**；
- bot 拿钥匙开门也会弹「XX 使用了 X」——这是上游 4.1 版的原有行为，移植未擅自改动；嫌吵可参考
  `docs/PORTING-NOTES.zh.md` 里的一行改法；
- 原版 `[BepInDependency("com.SPT.core", "4.1.0")]` 已改为 `com.sptushonka.core 5.0.0`（5.0BE 核心插件 GUID）；
- Fika 联机未测试。

## 重新编译（需 .NET 10 SDK）

```powershell
cd _src\KeyTips-5.0BE\src
$env:EFT5_ROOT = "D:\Path\To\SPT5.0BE"                                         # 5.0BE 安装根目录
& "$env:USERPROFILE\.dotnet\dotnet.exe" build -c Release -p:DeployToSPT=true
```

没有 `EFT5_ROOT`（或 `-p:GameRoot=`）时构建会报错并提示怎么传。
