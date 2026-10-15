# 怒号光明（ArknightsRoaringFlare）

《杀戮尖塔2》第三幕变体，主题是《明日方舟》主线第八章“怒号光明”。对标原版第三幕“荣耀”，与荣耀随机出现；
装有 ActLikeIt2 时改为在第三幕开始前与荣耀一同供选择。

- 前置：[RitsuLib](https://steamcommunity.com/sharedfiles/filedetails/?id=3747602295)（0.6.2 及以上）。不依赖苦难摇篮，两个模组可以同时装。
- 可选前置：ActLikeIt2（全程反射接入，不装也能玩，构建也不需要它）。
- 设计、数值与对照原版的难度见 [设计文档](docs/设计文档.md)；调研原文在 `docs/research/`。

## 内容

- 战斗池：保留已重做的弱怪 3、普通 5、精英 2，共 10 个非 Boss 遭遇。牵引的炮火为中枢先兆者与最多两名援军，按飞行/着陆模式切换。以原版荣耀的 3 弱怪、9 普通、3 精英为目标，还缺普通 4、精英 1。敌人以乌萨斯军为主，使用 PRTS 的原版 Spine 动画。
- 精英“突击者队长”：半血开场，第三回合撤退并召唤三名援军从右侧跑入；提前击倒队长使三名援军各获得士气低迷2，永久失去2力量。
- 精英“皇帝的利刃”，追猎者：取自活动「遗尘漫步」WD-8，两形态，国度与坍缩恐惧。
- Boss “不死的黑蛇”：引燃、噬咬、焦土、引爆；第一次倒下后三回合不息恶火召唤火球，重生后召唤能量聚合体；
  配乐《交锋》《塔露拉·雅特利亚斯》（塞壬唱片）。
- 音效与战斗特效取自明日方舟原作资源：特效由 `tools/ak_vfx/` 从原作 Unity 预制体转成描述，`src/Vfx/` 按原作的粒子参数与着色器公式在游戏里重现。
- A10 的双 Boss：本幕只有塔露拉一个 Boss，从荣耀的 Boss 里补一个先打，塔露拉留到最后（`src/Integration/DoubleBossPatch.cs`）。
- 战斗背景（暮色下的乌萨斯冻原）、地图底图与休息处（雪夜营地）是本模组自己的画，由 `art/` 下的 Codex 流水线生成；
  参考图只用原版游戏资源导出物，不用任何 AI 生成图（避免画面出现数字涟漪），原版导出物不入库。
- 第一版沿用荣耀的事件与先古之民，第八章事件放后续版本。

## 构建与验证

```bash
tools/build_and_deploy.sh
```

- 游戏程序集优先取工作区 `HextechRunes/versioned-dll-backups/<版本>/game-refs`，没有时用本机游戏安装目录；可用 `STS2_GAME_APP`、`ROARINGFLARE_REFS_ROOT`、`RitsuLibRoot` 覆盖。
- `ROARINGFLARE_DEPLOY=0` 只打包到 `dist/`，不部署到游戏 mods 目录。
- 离线自检：`tools/offline_selftest.sh [--with-actlikeit2] [--rebuild]`，APFS 克隆游戏 + 隔离 HOME + `--force-steam=off`，不碰真实游戏与存档。
- 开局伤害模拟：`docs/tools/opening_sim.py`（本模组）与 `docs/tools/vanilla_glory.py`（原版荣耀），引擎与口径见 `docs/tools/sim_core.py`。
  遭遇战的 `SetOpenings` 必须与模拟器的 `OPENINGS` 一致，自检会把游戏里实际生成的起手打进日志。
- 弱点破坏定向回归：`python3 tools/test_weakness_break.py`（需要 .NET 9 SDK，可用 `--dotnet /path/to/dotnet`）。主动跨越生命阈值，执行精锐与能力的生产源码，检查 A0/A10 突刺基础伤害、活力奖励及常规循环续接；外部游戏 API 使用测试替身，验证范围见 [测试说明](tests/WeaknessBreakRegression/README.md)。
- 本地化由 `tools/write_loc.py` 生成，改文案改脚本再跑一遍。
- 特效预览：`tools/ak_vfx/preview.py <配置>` 在离线克隆里开游戏窗口，摆怪物、播特效、截帧拼图（配置格式见脚本说明）。

## 许可

源代码、构建脚本、本地化文本与为本模组原创的美术以 MIT 许可发布（见 [LICENSE](LICENSE)）；
《明日方舟》角色与 Spine 动画、原作音效与特效素材、塞壬唱片的配乐、《杀戮尖塔2》本体资源不在此列，见 [NOTICE](NOTICE.md)。
