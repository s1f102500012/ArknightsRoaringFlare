# Spine 资源导入记录（18 个明日方舟敌人）

> 状态：18 个单位都已转成 Spine 4.2.43 并写好 creature_visuals 场景，PCK 里 headless 加载全部通过（骨架加载、贴图非空、动画齐全），时间轴扫描无需修复。**未进游戏实机看**：朝向、缩放、站位、Bounds 与标记点都只按包围盒推算，没有截图校对。

## 资源与场景一览

资源目录：`assets/animations/monsters/arknights/<后缀>/<file>.{skel,atlas,png}` + `.skel.import` / `.atlas.import` + `<file>_skel_data.tres`，导入产物 `assets/_imported/<file>.{skel.spskel,atlas.spatlas}`（技能脚本 `prepare_prts_spine_asset.py` 的写法，`_skel_data.tres` 直接引用 `_imported` 里的资源）。场景：`assets/scenes/creature_visuals/<场景名>.tscn`，由 `tools/write_creature_scenes.py` 生成，几何参数集中在它的 `SCENES` 表。

| 场景名 | 中文 | 后缀目录 | Spine | 贴图 | 动画数 | 待机 | 缩放 | Visuals 偏移 | Bounds（宽×高） | Idle 附件包围盒（场景坐标，含武器特效） | 验证 |
|---|---|---|---|---|---|---|---|---|---|---|---|
| black_snake | “不死的黑蛇” | bsnake | 4.2.43 | 512² | 19 | `Idle_A` | 1.1 | (-1, 0) | 250×405 | x -148..437, y -427..6 | OK |
| emperors_blade_hunter | “皇帝的利刃”，追猎者 | empgrd | 4.2.43 | 512² | 10 | `Idle` | 1.0 | (-18, 0) | 250×450 | x -144..266, y -469..6 | OK |
| infected_patrol_officer | 感染者纠察官 | uoffcr | 4.2.43 | 256² | 6 | `Idle` | 0.75 | (-5, 0) | 155×265 | x -144..83, y -265..2 | OK |
| senior_patrol_officer | 感染者高级纠察官 | uoffcr_2 | 4.2.43 | 256² | 6 | `Idle` | 0.825 | (-5.5, 0) | 170×292 | x -158..92, y -291..3 | OK |
| ursus_ravager | 乌萨斯裂兽 | uterer | 4.2.43 | 256² | 7 | `Idle` | 0.75 | (16, 0) | 225×205 | x -138..123, y -204..4 | OK |
| ravager_matriarch | 乌萨斯裂兽族母 | uterer_2 | 4.2.43 | 256² | 7 | `Idle` | 0.825 | (17.6, 0) | 248×226 | x -152..135, y -224..4 | OK |
| ursus_crossbowman | 乌萨斯突袭弩手 | uabone | 4.2.43 | 512² | 7 | `Idle` | 0.75 | (-3, 0) | 200×260 | x -195..159, y -274..1 | OK |
| ursus_armored_caster | 乌萨斯着铠术师 | uamord | 4.2.43 | 256² | 7 | `Idle` | 0.75 | (9, 0) | 175×260 | x -86..84, y -258..2 | OK |
| senior_armored_caster | 乌萨斯高级着铠术师 | uamord_2 | 4.2.43 | 256² | 7 | `Idle` | 0.825 | (9.9, 0) | 193×286 | x -94..93, y -283..2 | OK |
| ursus_commando | 乌萨斯突击者 | ucommd | 4.2.43 | 512² | 6 | `Idile` | 0.862 | (19, 0) | 190×335 | x -309..115, y -338..0 | OK |
| commando_captain | 乌萨斯突击者队长 | ucommd_2 | 4.2.43 | 512² | 6 | `Idile` | 0.948 | (20.9, 0) | 209×368 | x -340..126, y -372..0 | OK |
| artillery_harbinger | 帝国炮火先兆者 | emppnt | 4.2.43 | 512² | 7 | `Idle` | 0.862 | (0, -60) | 340×265 | x -303..309, y -265..-60 | OK |
| core_harbinger | 帝国炮火中枢先兆者 | emppnt_2 | 4.2.43 | 512² | 7 | `Idle` | 0.948 | (0, -66) | 374×292 | x -334..340, y -291..-66 | OK |
| imperial_vanguard | 帝国前锋精锐 | empace | 4.2.43 | 512² | 9 | `Idle` | 0.862 | (0, 0) | 230×330 | x -206..131, y -327..2 | OK |
| vanguard_veteran | 帝国前锋百战精锐 | empace_2 | 4.2.43 | 512² | 9 | `Idle` | 0.948 | (0, 0) | 253×363 | x -227..145, y -360..3 | OK |
| host_heavy_soldier | 宿主重装士兵 | zomshd | 4.2.43 | 512² | 7 | `Idle` | 0.862 | (-26, 0) | 256×375 | x -160..149, y -374..5 | OK |
| sarkaz_host_centurion | 萨卡兹宿主百夫长 | rgrdmn | 4.2.43 | 512² | 5 | `Idle` | 0.862 | (-14, 0) | 230×335 | x -224..171, y -355..5 | OK |

- 场景结构照姊妹模组：根 `Node2D` 挂 `res://src/Core/Nodes/Combat/NCreatureVisuals.cs`；`Visuals`（SpineSprite，`unique_name_in_owner`，`skeleton_data_res` 指 `res://ArknightsRoaringFlare/animations/monsters/arknights/<后缀>/<file>_skel_data.tres`，`preview_animation` = 待机动画）；`Bounds`（Control）与 `IntentPos` / `CenterPos` / `OrbPos` / `TalkPos`（Marker2D）。
- 朝向：PRTS 骨骼面朝右（预览里攻击都往右出），SpineSprite X 缩放取负，敌人面朝左；只翻 Visuals。
- 缩放档：普通 0.75，精英（突击者、先兆者、前锋、宿主重装、百夫长）0.862，`_2` 变体在本体上 ×1.1（偏移、Bounds 同比）。塔露拉、黑蛇 1.1，追猎者 1.0，屏幕身高约 400–450，接近姊妹模组爱国者（Bounds 高 461、附件包围盒高约 515）。黑蛇与塔露拉同缩放，方便互换外观。
- Bounds：以 x=0 居中、底边在地面 y=0。高取 Idle 身体顶边：附件包围盒顶边，但排除拖在身后的剑、披风、法杖、喷气背包管道；宽取身体宽加约 50 的余量。Visuals 的 x 偏移把身体中线挪到 x=0。身体范围用 `tools/body_extent.py` 从 Spine 导出的 Idle 帧量：按“不透明像素数超过最大列 40% 的列”取身体列。附件包围盒（上表最后一列）含武器和特效，所以比 Bounds 宽。
- 标记点：`IntentPos=(0, -(h+45))`、`CenterPos=(0, -h/2)`、`OrbPos=(0, -0.7h)`、`TalkPos=(w/4, -0.85h)`。这是从姊妹模组场景反推出的比例，那边的单位都符合。
- 先兆者是飞行单位，骨架原点贴着机身底部，所以整体上抬 60（`_2` 抬 66），Bounds 仍然落地，血条在地面。
- 突击者、队长的长矛和弩手的喷气背包让包围盒很宽，Bounds 只框身体。

## 尺寸（统一缩放，照原作相对大小）

原作所有敌人用同一套比例渲染：PRTS 的 SPINEDATA 只有骨骼文件名，没有逐个敌人的缩放，相对大小就是骨骼本身的大小。
默认单位缩放为0.75（`tools/write_creature_scenes.py` 的 `UNIFORM_SCALE`）。2026-10-15按实机反馈，小先兆者先独立缩小20%，随后三架再统一缩小20%；最终小先兆者0.48、中枢0.60。中枢视觉居中，两侧小怪直接在槽位出现，编队整体右移。偏移、Bounds与标记点由生成脚本同步换算；失去飞行时保持`Die`末帧并下移45像素，重新获得飞行恢复原高度及`Idle`。
第一版分档（普通 0.75、精英 0.862、组长 ×1.1、黑蛇 1.1）把黑蛇放大成纠察官的 1.5 倍、无人机多放大了 15%，用户实机反馈偏大。

统一缩放后用游戏本体加 `-s` 脚本逐个渲染量的可见高度（脚底到头顶）：
- 纠察官 263（组长变体同骨骼大小）；术师 259；弩手 268；裂兽 207。
- 突击者 302；前锋 297；重装兵 328；百夫长 313；追猎者 343。
- 黑蛇 279（原作里塔露拉是常人体型）；先兆者无人机高 228（悬浮，含离地）、宽 496（约为士兵的两倍）。

原版参照：铁甲战士 330，人形普通怪 230–330。黑蛇比原版 Boss（中位 488）小得多，这是照原作比例的取舍。
塔露拉战的标记点按这个大小摆：标记点基本就是屏幕坐标（实机 1670 时大剑出画），黑蛇待机时大剑向右后方伸出约 280，
标记放在 1560，右边留约 80 给攻击动作。

## 时间轴扫描（3.8 → 4.2 关键帧时间倒退）

`tools/spine_units.py scan`：把仓库里转换后的 4.2 骨骼用 4.2.43 导出 JSON，找关键帧时间不单调的时间轴；去掉 time/curve 后每帧内容都相同的，记为“同态”（时间倒退只是重复同一状态，画面不变）。

- 17 个单位：没有时间倒退的时间轴。黑蛇有 310 根骨骼，转换后也是干净的。
- 塔露拉 `Skill_2`：`slots/F_R_Weapon/attachment` 时间为 `[0, 0.3667, 0]`，三帧都是附件 `F_R_Weapon`（就是这个插槽的 setup 附件），属于同态，不影响画面，不修。这条在 3.8.99 原件导出的 JSON 里就存在，不是转换引入的。
- 结论：不需要用 `fix_spine_timelines.py` 的方法修任何骨骼。

## 动作判读

用 Spine 4.2.43 按 3–6 fps 导出整段 PNG 帧拼成联系表来看（`tools/spine_units.py preview`）。注意：Spine PNG 导出的第 0 帧缩放异常，比其余帧大一圈；游戏里 headless 量的第 0 帧包围盒与整段一致，所以只是导出工具的问题。

### “不死的黑蛇”（重点）

- **`_A` / `_B` 是两种形态**，所有成对动画（Idle/Move/Attack/Skill_1/Skill_2）都是同一动作的两个版本。
  - `_A`：站得直，长裙及踝，大剑斜拖在身后，围巾火焰飘着，表情平静（第一条命）。
  - `_B`：身体前倾半蹲，眼睛发红，裙摆更短更破，姿态更凶（第二条命）。
  - 依据：`Reborn_1_End` 的最后一帧落成 `_B` 的前倾姿势；`Reborn_1_Start` 从 `_A` 的站姿开始。所以 **A = 复活前，B = 第一次复活后**。
- `Reborn_1_Start`（3.13s）：从 A 站姿开始，把剑插地，单膝跪下，0.67–2.0s 一直跪着，2.33s 跃起，2.67s 落地，3.0s 半透明淡出，最后一帧完全不可见。音频事件 `reborn_1_1/2/3`。
- `Reborn_1_Idle`（0s，单帧）：**全透明**，所有附件都隐藏，即复活等待期“人不在场”。
- `Reborn_1_End`（2.77s）：第 0 帧不可见，从上方落下（0.33–1.33s 空中翻身挥剑），2.0s 落地，最后停在 **B 形态** 的前倾待机姿势。
- `Reborn_2_Start`（5.33s）：从 B 姿势开始，0.33–2.67s 剑拖地低姿态蓄势，3.0–3.67s 跃起后把剑竖直下刺，4.0s 空中翻转，4.33–5.0s 倒在地上，最后一帧不可见。
- `Reborn_2_Idle`（0s，单帧）：**没有任何附件**，导出图是 1×1 空图，同样是“不在场”。
- `Reborn_2_End`（1.23s）：第 0 帧不可见，从上方落下，最后半蹲、剑竖插在身前，接近 `Reborn_1_Start` 跪地前的姿势。它不是 A 或 B 的待机首帧，接回待机时会靠 0.1s 默认混合过渡，有一下姿势跳变，需要实机看。
- `Attack_A/B`（2.33s，OnAttack @1.033）：大剑转身横扫，1.0s 前后剑贴地横斩（命中帧）。
- `Skill_1_A/B`（1.97s，OnAttack @1.167）：左手抬起施法，剑不动；火焰特效在方舟里由 OnPlayEffect/程序生成，骨骼里没有，所以这里看起来只是一个手势。
- `Skill_2_A/B`（1.97s，OnAttack @1.033）：左手向前推掌，也是施法动作，和 Skill_1 的区别是手的方向（前推 vs 上举）。
- `Skill_3`（2.33s，OnAttack @0.9，没有 A/B 之分）：跃起，双手举剑，在空中下劈后落地（0.67–1.67s 腾空）。是最大的动作，适合当大招。
- `Die`（2.67s）：插剑跪地，2.33s 起淡出，最后完全消失。
- `Move_A/B`：行走循环，每 0.9s 一个 `OnPlayEffect`（脚下火焰）。`Default`：setup 姿势。
- 对应方舟机制（推测）：第一条命用 A，被击倒后播 `Reborn_1_Start → Reborn_1_Idle（不在场）→ Reborn_1_End`，换 B；第二次“不息恶火”用 `Reborn_2_*`。`Reborn_2_End` 之后接 A 还是 B 骨骼没写死，要按设计决定。

### 塔露拉

> 塔露拉本人的骨骼（enemy_1503_talula）已从模组移除：Boss 改为只有黑蛇两阶段，塔露拉怪物直接用 black_snake 场景。下面两节留作判读记录。

- `Idle`（3.0s）：大剑斜拖身后，围巾飘动。
- `Attack_1`（2.0s，OnAttack @1.0）：剑不动，左手前伸施法，对应方舟里**不被阻挡时的远程法术攻击**。
- `Attack_2`（2.33s，OnAttack @1.333）：把剑往上抡，1.33s 横斩并带刀光拖影，对应**被阻挡时的近战攻击**。
- `Skill_1`（2.0s，OnAttack @1.0）：动作几乎和 `Attack_1` 一样（左手施法），推测是“日冕”。
- `Skill_2`（3.67s，OnAttack @2.567）：举剑、转身、连续大幅挥砍，2.5s 后剑高举再斜劈，推测是“烈焚灼息”。
- `Die`（5.0s）：举剑后剑竖插地面，4.5s 起溶解消散，最后只剩一缕烟。
- `Move`：行走，两个 `OnPlayEffect`。

### “皇帝的利刃”，追猎者

- `Idle`（1.67s）：右手持红刃刀垂在身侧，左拳收着。
- `Attack01`（1.07s，OnAttack @0.6）：左拳向前直击（**拳击**）。
- `Attack02`（1.87s，OnAttack @0.767）：压低身体，刀向前**突刺**，刀尖伸得很远。
- `Skill_01`（1.77s，OnAttack @1.067）：和 Attack01 类似的前伸拳击，动作更慢更重。
- `Skill_02_Begin`（1.8s，OnAttack @1.167）：**把刀丢在地上**（0.33s），然后前倾伸手。
- `Skill_02_Loop`（1.53s）：半蹲、双手前探的持续姿势，手里没刀，可当“蓄力/擒抱”的循环。
- `Skill_02_End`（2.4s）：起身，2.17s 重新拿起刀，回到待机。
- `Die`（2.5s）：刀落地，跪下伸手，2.17s 淡出。
- 研究文档里说的 Begin/Loop/End“重生”三段式，在骨骼里实际是丢刀→空手蓄势→捡刀，不像复活。当转阶段演出也能用，但画面上是“卸下武器”。

### 其他单位（每单位一句）

- 纠察官（含高级）：`Attack` 弯刀竖劈；`Combat` 弯刀横扫（另一套攻击，事件同为 0.5）；`Die` 向前扑倒。
- 裂兽（含族母）：熊形野兽背着装置；`Attack` 前扑撕咬（0.4）；`Die` 趴倒。三段式移动。
- 突袭弩手：背着大型喷气背包；`Attack` 端弩射击（0.533）；`Combat` 抡弩近身砸（0.5）；`Start`（1.5s）是**从空中降落**的入场动画，前 0.33s 不可见；`Die` 后仰倒地。
- 着铠术师（含高级）：兜帽法师；`Attack` 单手前推施法（0.633）；`Die` 倒地。
- 突击者（含队长）：长矛重兵；`Attack` 举矛过顶再前刺（0.567）；`Combat` 矛平端前刺（0.6）；`Die` 跪倒、矛落地。**待机名 `Idile`**。
- 炮火先兆者（含中枢）：四旋翼无人机；`Attack` 机身轻微前倾发射（0.333，方舟里是 3 秒后落地的炮击，弹道不在骨骼里）；`Die` 机身倾斜下坠；`Move_Start`/`Move`/`Move_End` 只是姿态变化。
- 前锋精锐（含百战）：持短矛+飞镖的重装兵；`Attack_1_Start`（0.33s 架矛）→ `Attack_1_Loop`（1.5s，前冲突刺，OnAttack 0.5）→ `Attack_1_End`（0.33s 收势）；`Attack_1_Idle` 是架矛待战的循环；`Attack_2`（2.07s，OnAttack 1.033）跃起举矛下砸。`Die` 跪倒、矛落地。`Idle` 本身长 6.67s，里面有几次小动作。
- 宿主重装士兵：举大盾；`Attack` 盾牌猛击（0.467）；`Die` 连盾带人向前倒。
- 萨卡兹宿主百夫长：拿大剑的骨甲宿主；`Attack`（3.5s，OnAttack 1.133）先把剑挥到地面，0.67s 起竖剑于身前（有尘土粒子）。持剑姿势一直保持到 2.5s，2.67–3.0s 再挥回待机。实际命中只有 1.133 一次，后半段是收势，**一段攻击要占 3.5 秒，偏长**。`Die` 跪倒。没有 `Move_Begin/End`，只有 `Move_Loop`。

## 工具

- `tools/spine_units.py`：单位清单，子命令 `info` / `import` / `scan` / `preview`，中间产物在 `.build/spine_work/`。
- `tools/spine_info_markdown.py`：把 `info` 结果排成本文件的动画表。
- `tools/write_creature_scenes.py`：生成 18 个场景（几何参数表）。
- `tools/measure_idle_bounds.gd`：游戏引擎 headless 量 Idle 附件包围盒（`SpineSkeleton.get_bounds`）。必须等 SpineSprite 跑过几帧再调用，否则会触发 godot-cpp 越界断言，进程直接退出。
- `tools/body_extent.py`：从 Idle 帧 PNG 估身体范围。
- `tools/check_spine_scenes.sh`：对 PCK 跑 `validate_creature_scenes.gd` 和 `measure_idle_bounds.gd`。

## 本机环境的坑（这次遇到的）

- **Spine 3.8.99 不认 `-e json` 关键字**：x86 旧版启动器会启动失败（`Error running x86 launcher` / `Invalid launcher response (empty)`），启动器随即删掉 `~/Library/Application Support/Spine/updates/3.8.99`，重新下载又失败，这个版本就从本机消失了。已经两次用 `Spine -u 3.8.99 -i <skel>` 让启动器重新下载恢复。3.8 导出 JSON 必须传导出设置文件（`spine_units.py` 已这样做）。技能脚本只用 4.2.43 做 `-e binary`，不受影响。
- 3.8.99 导入偶发卡死（0% CPU 不退出），`spine_units.py` 给每次 Spine 调用加了 300s 超时和重试。
- PRTS CDN 有个解析出的 IP 不通，Python urllib 会卡在 SYN_SENT；改用 curl 带超时和重试下载。`import` 子命令复用技能脚本的转换和导入文件写法，但原件取自本地缓存，不走技能脚本的 urllib 下载。
- 转换后的骨骼图片路径指向 PRTS 作者机器（`E:/SpineInput/`），直接导出 PNG 全是 MISSING；预览前要先用图集拆单图，再改 JSON 的 `skeleton.images`。

## 取数方法

- 原件：`https://torappu.prts.wiki/assets/enemy_spine/<folder>/<file>.{skel,atlas,png}`，18 个单位 folder 与 file 同名，全部是 **Spine 3.8.99**。
- 动画名、时长、事件：Spine 3.8.99 把 `.skel` 导入成工程再导出 JSON（未经过 4.2 转换），时长取动画内所有关键帧的最大时刻，事件时刻取 `events` 时间轴。脚本 `tools/spine_units.py info`，中间产物在 `.build/spine_work/`。
- 时长 0.000 的动画（`Default`、黑蛇 `Reborn_*_Idle`）是单帧姿势，没有关键帧动画，循环播放就是静止。

## 写代码要注意的名字

- **乌萨斯突击者 / 队长的待机动画叫 `Idile`**（PRTS 原文件拼写错误，不是 `Idle`），代码里映射待机时按原样写。
- 纠察官、突袭弩手、突击者有 `Combat` 动画，与 `Attack` 时长相同、事件时刻几乎相同（突击者 0.567 vs 0.600）；在方舟里是“远程武器攻击平民/斗士塔露拉”的另一套攻击动作，可当第二种攻击用。
- 裂兽、着铠术师、宿主重装士兵的移动是 `Move_Begin` / `Move_Loop` / `Move_End` 三段；突袭弩手只有 `Move_Loop`，另有 `Start`（1.5s，“从战场中降落”的入场）；先兆者是 `Move_Start` / `Move` / `Move_End`。
- 帝国前锋的攻击分两种：`Attack_1_Start` → `Attack_1_Loop`（OnAttack @0.5）→ `Attack_1_End`，以及 `Attack_1_Idle`（架势待机）；`Attack_2` 是单段（OnAttack @1.033）。待机 `Idle` 长 6.667s。
- 追猎者动画名是 `Attack01` / `Attack02` / `Skill_01` / `Skill_02_Begin|Loop|End`（数字两位、无下划线分隔，和其他单位不同）。
- `_2` 变体与本体动画名单完全一致，但 `.skel` 都不是逐字节相同（只换图集的情况这里没有），所以每个变体各自一套骨骼文件。
- 贴图：纠察官、裂兽、着铠术师（含 `_2`）是 256×256，其余 512×512，全部单页图集。

## 动画清单（Spine 3.8.99 原件）

### “不死的黑蛇” — `black_snake`

- 源文件：`enemy_1515_bsnake`（PRTS 3.8.99，2422 KB，骨骼 310，插槽 165，皮肤 default，图集页 enemy_1515_bsnake.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/bsnake/enemy_1515_bsnake_skel_data.tres`
- 事件定义：OnAttack, OnPlayAudio, OnPlayEffect

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack_A` | 2.333 | OnAttack @ 1.033 |
| `Attack_B` | 2.333 | OnAttack @ 1.033 |
| `Default` | 0.000 | — |
| `Die` | 2.667 | — |
| `Idle_A` | 9.600 | — |
| `Idle_B` | 9.600 | — |
| `Move_A` | 3.600 | OnPlayEffect @ 0.033；OnPlayEffect @ 0.933；OnPlayEffect @ 1.833；OnPlayEffect @ 2.733 |
| `Move_B` | 3.600 | OnPlayEffect @ 0.033；OnPlayEffect @ 0.933；OnPlayEffect @ 1.833；OnPlayEffect @ 2.733 |
| `Reborn_1_End` | 2.767 | OnPlayAudio @ 0.000（`reborn_1_4`）；OnPlayAudio @ 0.867（`reborn_1_5`） |
| `Reborn_1_Idle` | 0.000 | — |
| `Reborn_1_Start` | 3.133 | OnPlayAudio @ 0.000（`reborn_1_1`）；OnPlayAudio @ 0.967（`reborn_1_2`）；OnPlayAudio @ 2.433（`reborn_1_3`） |
| `Reborn_2_End` | 1.233 | OnPlayAudio @ 0.000（`reborn_2_4`） |
| `Reborn_2_Idle` | 0.000 | — |
| `Reborn_2_Start` | 5.333 | OnPlayAudio @ 0.000（`reborn_2_1`）；OnPlayAudio @ 2.933（`reborn_2_2`）；OnPlayAudio @ 4.000（`reborn_2_3`） |
| `Skill_1_A` | 1.967 | OnAttack @ 1.167 |
| `Skill_1_B` | 1.967 | OnAttack @ 1.167 |
| `Skill_2_A` | 1.967 | OnAttack @ 1.033 |
| `Skill_2_B` | 1.967 | OnAttack @ 1.033 |
| `Skill_3` | 2.333 | OnAttack @ 0.900 |

### 塔露拉 — `talulah`

> 塔露拉本人的骨骼（enemy_1503_talula）已从模组移除：Boss 改为只有黑蛇两阶段，塔露拉怪物直接用 black_snake 场景。下面两节留作判读记录。

- 源文件：`enemy_1503_talula`（PRTS 3.8.99，201 KB，骨骼 97，插槽 50，皮肤 default，图集页 enemy_1503_talula.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/talula/enemy_1503_talula_skel_data.tres`
- 事件定义：OnAttack, OnPlayEffect

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack_1` | 2.000 | OnAttack @ 1.000 |
| `Attack_2` | 2.333 | OnAttack @ 1.333 |
| `Default` | 0.000 | — |
| `Die` | 5.000 | — |
| `Idle` | 3.000 | — |
| `Move` | 2.500 | OnPlayEffect @ 0.533；OnPlayEffect @ 1.833 |
| `Skill_1` | 2.000 | OnAttack @ 1.000 |
| `Skill_2` | 3.667 | OnAttack @ 2.567 |

### “皇帝的利刃”，追猎者 — `emperors_blade_hunter`

- 源文件：`enemy_1520_empgrd`（PRTS 3.8.99，236 KB，骨骼 80，插槽 58，皮肤 default，图集页 enemy_1520_empgrd.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/empgrd/enemy_1520_empgrd_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack01` | 1.067 | OnAttack @ 0.600 |
| `Attack02` | 1.867 | OnAttack @ 0.767 |
| `Default` | 0.000 | — |
| `Die` | 2.500 | — |
| `Idle` | 1.667 | — |
| `Move` | 1.400 | — |
| `Skill_01` | 1.767 | OnAttack @ 1.067 |
| `Skill_02_Begin` | 1.800 | OnAttack @ 1.167 |
| `Skill_02_End` | 2.400 | — |
| `Skill_02_Loop` | 1.533 | — |

### 感染者纠察官 — `infected_patrol_officer`

- 源文件：`enemy_1107_uoffcr`（PRTS 3.8.99，56 KB，骨骼 53，插槽 24，皮肤 default，图集页 enemy_1107_uoffcr.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uoffcr/enemy_1107_uoffcr_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.000 | OnAttack @ 0.500 |
| `Combat` | 1.000 | OnAttack @ 0.500 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 1.333 | — |
| `Move` | 0.833 | — |

### 感染者高级纠察官 — `senior_patrol_officer`

- 源文件：`enemy_1107_uoffcr_2`（PRTS 3.8.99，56 KB，骨骼 53，插槽 24，皮肤 default，图集页 enemy_1107_uoffcr_2.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uoffcr_2/enemy_1107_uoffcr_2_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.000 | OnAttack @ 0.500 |
| `Combat` | 1.000 | OnAttack @ 0.500 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 1.333 | — |
| `Move` | 0.833 | — |

### 乌萨斯裂兽 — `ursus_ravager`

- 源文件：`enemy_1108_uterer`（PRTS 3.8.99，64 KB，骨骼 34，插槽 16，皮肤 default，图集页 enemy_1108_uterer.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uterer/enemy_1108_uterer_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.000 | OnAttack @ 0.400 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 2.667 | — |
| `Move_Begin` | 0.333 | — |
| `Move_End` | 0.333 | — |
| `Move_Loop` | 0.600 | — |

### 乌萨斯裂兽族母 — `ravager_matriarch`

- 源文件：`enemy_1108_uterer_2`（PRTS 3.8.99，64 KB，骨骼 34，插槽 16，皮肤 default，图集页 enemy_1108_uterer_2.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uterer_2/enemy_1108_uterer_2_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.000 | OnAttack @ 0.400 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 2.667 | — |
| `Move_Begin` | 0.333 | — |
| `Move_End` | 0.333 | — |
| `Move_Loop` | 0.600 | — |

### 乌萨斯突袭弩手 — `ursus_crossbowman`

- 源文件：`enemy_1109_uabone`（PRTS 3.8.99，46 KB，骨骼 43，插槽 24，皮肤 default，图集页 enemy_1109_uabone.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uabone/enemy_1109_uabone_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.000 | OnAttack @ 0.533 |
| `Combat` | 1.000 | OnAttack @ 0.500 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 1.000 | — |
| `Move_Loop` | 1.000 | — |
| `Start` | 1.500 | — |

### 乌萨斯着铠术师 — `ursus_armored_caster`

- 源文件：`enemy_1110_uamord`（PRTS 3.8.99，69 KB，骨骼 61，插槽 24，皮肤 default，图集页 enemy_1110_uamord.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uamord/enemy_1110_uamord_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.200 | OnAttack @ 0.633 |
| `Default` | 0.000 | — |
| `Die` | 0.833 | — |
| `Idle` | 2.000 | — |
| `Move_Begin` | 0.167 | — |
| `Move_End` | 0.167 | — |
| `Move_Loop` | 1.000 | — |

### 乌萨斯高级着铠术师 — `senior_armored_caster`

- 源文件：`enemy_1110_uamord_2`（PRTS 3.8.99，69 KB，骨骼 61，插槽 24，皮肤 default，图集页 enemy_1110_uamord_2.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/uamord_2/enemy_1110_uamord_2_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.200 | OnAttack @ 0.633 |
| `Default` | 0.000 | — |
| `Die` | 0.833 | — |
| `Idle` | 2.000 | — |
| `Move_Begin` | 0.167 | — |
| `Move_End` | 0.167 | — |
| `Move_Loop` | 1.000 | — |

### 乌萨斯突击者 — `ursus_commando`

- 源文件：`enemy_1111_ucommd`（PRTS 3.8.99，60 KB，骨骼 68，插槽 35，皮肤 default，图集页 enemy_1111_ucommd.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/ucommd/enemy_1111_ucommd_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.167 | OnAttack @ 0.567 |
| `Combat` | 1.167 | OnAttack @ 0.600 |
| `Default` | 0.000 | — |
| `Die` | 0.900 | — |
| `Idile` | 1.333 | — |
| `Move` | 0.833 | — |

### 乌萨斯突击者队长 — `commando_captain`

- 源文件：`enemy_1111_ucommd_2`（PRTS 3.8.99，60 KB，骨骼 68，插槽 35，皮肤 default，图集页 enemy_1111_ucommd_2.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/ucommd_2/enemy_1111_ucommd_2_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.167 | OnAttack @ 0.567 |
| `Combat` | 1.167 | OnAttack @ 0.600 |
| `Default` | 0.000 | — |
| `Die` | 0.900 | — |
| `Idile` | 1.333 | — |
| `Move` | 0.833 | — |

### 帝国炮火先兆者 — `artillery_harbinger`

- 源文件：`enemy_1112_emppnt`（PRTS 3.8.99，26 KB，骨骼 15，插槽 15，皮肤 default，图集页 enemy_1112_emppnt.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/emppnt/enemy_1112_emppnt_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.167 | OnAttack @ 0.333 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 2.000 | — |
| `Move` | 2.000 | — |
| `Move_End` | 0.333 | — |
| `Move_Start` | 0.333 | — |

### 帝国炮火中枢先兆者 — `core_harbinger`

- 源文件：`enemy_1112_emppnt_2`（PRTS 3.8.99，26 KB，骨骼 15，插槽 15，皮肤 default，图集页 enemy_1112_emppnt_2.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/emppnt_2/enemy_1112_emppnt_2_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.167 | OnAttack @ 0.333 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 2.000 | — |
| `Move` | 2.000 | — |
| `Move_End` | 0.333 | — |
| `Move_Start` | 0.333 | — |

### 帝国前锋精锐 — `imperial_vanguard`

- 源文件：`enemy_1113_empace`（PRTS 3.8.99，122 KB，骨骼 37，插槽 32，皮肤 default，图集页 enemy_1113_empace.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/empace/enemy_1113_empace_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack_1_End` | 0.333 | — |
| `Attack_1_Idle` | 1.333 | — |
| `Attack_1_Loop` | 1.500 | OnAttack @ 0.500 |
| `Attack_1_Start` | 0.333 | — |
| `Attack_2` | 2.067 | OnAttack @ 1.033 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 6.667 | — |
| `Move` | 1.333 | — |

### 帝国前锋百战精锐 — `vanguard_veteran`

- 源文件：`enemy_1113_empace_2`（PRTS 3.8.99，180 KB，骨骼 37，插槽 32，皮肤 default，图集页 enemy_1113_empace_2.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/empace_2/enemy_1113_empace_2_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack_1_End` | 0.333 | — |
| `Attack_1_Idle` | 1.333 | — |
| `Attack_1_Loop` | 1.500 | OnAttack @ 0.500 |
| `Attack_1_Start` | 0.333 | — |
| `Attack_2` | 2.067 | OnAttack @ 1.033 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 6.667 | — |
| `Move` | 1.333 | — |

### 宿主重装士兵 — `host_heavy_soldier`

- 源文件：`enemy_1061_zomshd`（PRTS 3.8.99，47 KB，骨骼 32，插槽 17，皮肤 default，图集页 enemy_1061_zomshd.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/zomshd/enemy_1061_zomshd_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 1.000 | OnAttack @ 0.467 |
| `Default` | 0.000 | — |
| `Die` | 0.900 | — |
| `Idle` | 2.000 | — |
| `Move_Begin` | 0.200 | — |
| `Move_End` | 0.233 | — |
| `Move_Loop` | 1.200 | — |

### 萨卡兹宿主百夫长 — `sarkaz_host_centurion`

- 源文件：`enemy_1114_rgrdmn`（PRTS 3.8.99，33 KB，骨骼 29，插槽 21，皮肤 default，图集页 enemy_1114_rgrdmn.png）
- 资源：`res://ArknightsRoaringFlare/animations/monsters/arknights/rgrdmn/enemy_1114_rgrdmn_skel_data.tres`
- 事件定义：OnAttack

| 动画 | 时长 (s) | 事件 |
|---|---|---|
| `Attack` | 3.500 | OnAttack @ 1.133 |
| `Default` | 0.000 | — |
| `Die` | 1.000 | — |
| `Idle` | 1.000 | — |
| `Move_Loop` | 1.667 | — |

