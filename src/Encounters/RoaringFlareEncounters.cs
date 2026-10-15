using ArknightsRoaringFlare.Monsters;
using Godot;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Encounters;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Rooms;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsRoaringFlare.Encounters;

// 怒号光明的当前战斗池：弱 3、普通 5、精英 2，Boss 只有塔露拉（A10 双 Boss 见 Integration.DoubleBossPatch）。
// 有效名单见 ModEntry.ActEncounterTypes；标记为旧遭遇的定义保留，但不再加入战斗池。
// 组成、总血量与对照原版的难度见 docs/设计文档.md。起手偏移（SetOpenings）与 docs/tools/opening_sim.py 的 OPENINGS 同步；
// 先遣与精锐巡逻队的普通纠察官共用完整循环，高级纠察官前三回合打盹；裂兽固定以 1/2/3 起手，族母固定召集开场。
// 同一场里的同种怪起手一律错开（模拟器硬约束，自检核对）。通常按从左到右出手；牵引的炮火中枢视觉居中、行动仍在最后。

// ---- 弱怪 ----

/// <summary>纠察巡逻队（先遣）：纠察官 ×2，分别以 4→1→2→3、3→2→4→1 循环。</summary>
public sealed class PatrolWeak : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override bool IsWeak => true;

	public override IEnumerable<EncounterTag> Tags => [FlareEncounterTags.Patrol];

	protected override IReadOnlyList<MonsterModel> Lineup => [M<InfectedPatrolOfficer>(), M<InfectedPatrolOfficer>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters)
	{
		((InfectedPatrolOfficer)monsters[0]).SetFixedCycle(
			InfectedPatrolOfficer.ArmMoveId, InfectedPatrolOfficer.NetMoveId,
			InfectedPatrolOfficer.LashMoveId, InfectedPatrolOfficer.ReloadShootMoveId);
		((InfectedPatrolOfficer)monsters[1]).SetFixedCycle(
			InfectedPatrolOfficer.ReloadShootMoveId, InfectedPatrolOfficer.LashMoveId,
			InfectedPatrolOfficer.ArmMoveId, InfectedPatrolOfficer.NetMoveId);
	}
}

/// <summary>裂兽 ×3，分别以撕咬、警戒、扑击开场，之后按三招循环。</summary>
public sealed class RavagersWeak : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override bool IsWeak => true;

	public override IEnumerable<EncounterTag> Tags => [FlareEncounterTags.Ravagers];

	protected override IReadOnlyList<MonsterModel> Lineup => [M<UrsusRavager>(), M<UrsusRavager>(), M<UrsusRavager>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 0, 1, 2);
}

/// <summary>旧遭遇（已退出战斗池）：R8-6，着铠术师单体，先施加咒法。</summary>
public sealed class ArmoredCasterWeak : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override bool IsWeak => true;

	public override IEnumerable<EncounterTag> Tags => [FlareEncounterTags.Casters];

	protected override IReadOnlyList<MonsterModel> Lineup => [M<UrsusArmoredCaster>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 0);
}

// ---- 普通 ----

/// <summary>纠察巡逻队（精锐）：纠察官 ×2 + 高级纠察官（前三回合打盹，站最右）。</summary>
public sealed class HuskVillageSearchNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override IEnumerable<EncounterTag> Tags => [FlareEncounterTags.Patrol];

	protected override IReadOnlyList<MonsterModel> Lineup =>
		[M<InfectedPatrolOfficer>(), M<InfectedPatrolOfficer>(), M<SeniorPatrolOfficer>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters)
	{
		((InfectedPatrolOfficer)monsters[0]).SetFixedCycle(
			InfectedPatrolOfficer.ArmMoveId, InfectedPatrolOfficer.NetMoveId,
			InfectedPatrolOfficer.LashMoveId, InfectedPatrolOfficer.ReloadShootMoveId);
		((InfectedPatrolOfficer)monsters[1]).SetFixedCycle(
			InfectedPatrolOfficer.ReloadShootMoveId, InfectedPatrolOfficer.LashMoveId,
			InfectedPatrolOfficer.ArmMoveId, InfectedPatrolOfficer.NetMoveId);
	}
}

/// <summary>裂兽群：族母独自以召集开场，三个裂兽槽在前，族母最后出手。</summary>
public sealed class RavagerPackNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override IEnumerable<EncounterTag> Tags => [FlareEncounterTags.Ravagers]; // 考虑可以移除这个tag

	public override EncounterAssetProfile AssetProfile => new(
		EncounterScenePath: RoaringFlareAssets.EncounterScene("ravager_pack"),
		ExtraAssetPaths: [ModelDb.Monster<UrsusRavager>().CreatureScenePath]);

	public override IReadOnlyList<string> Slots => RavagerMatriarch.FormationSlots;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<RavagerMatriarch>()];

	protected override IReadOnlyList<string> LineupSlots => [RavagerMatriarch.MatriarchSlot];

	protected override IEnumerable<MonsterModel> Candidates => [M<RavagerMatriarch>(), M<UrsusRavager>()];
}

/// <summary>旧遭遇（已退出战斗池）：R8-4 火种，一触即灭，突袭弩手 ×2（第一名速射，第二名冲击）。</summary>
public sealed class CrossbowmenNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<UrsusCrossbowman>(), M<UrsusCrossbowman>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 0, 1);
}

/// <summary>高级术士（strong）：高级着铠术师单独出场，源石法术开场；保留原遭遇模型 ID。</summary>
public sealed class CasterSquadNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override IEnumerable<EncounterTag> Tags => [FlareEncounterTags.Casters];

	protected override IReadOnlyList<MonsterModel> Lineup => [M<SeniorArmoredCaster>()];
}

/// <summary>旧遭遇（已退出战斗池）：R8-5 寒冷，来自知觉，突击者小队，突击者（先稳阵）+ 纠察官。</summary>
public sealed class CommandoSquadNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<UrsusCommando>(), M<InfectedPatrolOfficer>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 1, 1);
}

/// <summary>牵引的炮火（strong）：中枢居中以标定开场，两个小先兆者分列两侧并先于中枢行动。</summary>
public sealed class GuidedArtilleryNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override EncounterAssetProfile AssetProfile => new(
		EncounterScenePath: RoaringFlareAssets.EncounterScene("guided_artillery"),
		ExtraAssetPaths: [ModelDb.Monster<ArtilleryHarbinger>().CreatureScenePath]);
	public override IReadOnlyList<string> Slots => CoreHarbinger.FormationSlots;
	protected override IReadOnlyList<MonsterModel> Lineup => [M<CoreHarbinger>()];
	protected override IReadOnlyList<string> LineupSlots => [CoreHarbinger.CoreSlot];
	protected override IEnumerable<MonsterModel> Candidates => [M<CoreHarbinger>(), M<ArtilleryHarbinger>()];
}

/// <summary>R8-10 沉默的屠夫：帝国前锋精锐单体，弱敌遭遇（先远射，2→1 循环）；沿用旧模型 ID。</summary>
public sealed class SilentButcherNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	public override bool IsWeak => true;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<ImperialVanguard>()];
}

/// <summary>旧遭遇（已退出战斗池）：M8-7 恶言，报应不爽，宿主重装士兵（先重压）+ 萨卡兹宿主百夫长（先吟唱，站最右）。</summary>
public sealed class HostCenturionNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<HostHeavySoldier>(), M<SarkazHostCenturion>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 2, 2);
}

/// <summary>精英：半血队长先压阵，第三回合交接左/中/右三名援军；保留既有模型 ID。</summary>
public sealed class CommandoCaptainNormal : FlareEncounter
{
	public override RoomType RoomType => RoomType.Elite;
	public override EncounterAssetProfile AssetProfile => new(
		EncounterScenePath: RoaringFlareAssets.EncounterScene("commando_captain"),
		ExtraAssetPaths: [ModelDb.Monster<UrsusCommando>().CreatureScenePath, ModelDb.Monster<UrsusArmoredCaster>().CreatureScenePath, ModelDb.Monster<UrsusCrossbowman>().CreatureScenePath]);
	public override IReadOnlyList<string> Slots => CommandoCaptain.FormationSlots;
	protected override IReadOnlyList<MonsterModel> Lineup => [M<CommandoCaptain>()];
	protected override IReadOnlyList<string> LineupSlots => [CommandoCaptain.CaptainSlot];
	protected override IEnumerable<MonsterModel> Candidates => [M<CommandoCaptain>(), M<UrsusCommando>(), M<UrsusArmoredCaster>(), M<UrsusCrossbowman>()];
}

// ---- 精英 ----

/// <summary>WD-8 大雪将至（「遗尘漫步」）：“皇帝的利刃”，追猎者（先巡猎）。两阶段，沿用精英战音乐。</summary>
public sealed class EmperorsBladeElite : FlareEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<EmperorsBladeHunter>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 0);
}

/// <summary>百战精锐：帝国前锋百战精锐单体强敌，突刺开场；沿用旧模型 ID。</summary>
public sealed class VanguardVeteranElite : FlareEncounter
{
	public override RoomType RoomType => RoomType.Monster;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<VanguardVeteran>()];
}

/// <summary>旧遭遇（已退出战斗池）：R8-8 人心，向背无常，着铠术师（先咒法）+ 帝国炮火中枢先兆者（先标定）。</summary>
public sealed class CoreHarbingerElite : FlareEncounter
{
	public override RoomType RoomType => RoomType.Elite;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<UrsusArmoredCaster>(), M<CoreHarbinger>()];

	protected override void ConfigureMonsters(IReadOnlyList<MonsterModel> monsters) => SetOpenings(monsters, 0, 0);
}

// ---- Boss ----

/// <summary>
/// JT8-3 昂首，足践烈焰：塔露拉（附身的“不死的黑蛇”），两阶段（见 <see cref="Talulah"/>）。
/// 按场景标记点站位（照原版卵翼虫）：五个火球召唤槽在前（排成一竖列）、塔露拉在最后，召唤物先于黑蛇出手。
/// </summary>
public sealed class TalulahBoss : FlareEncounter
{
	public override RoomType RoomType => RoomType.Boss;

	// 地图节点用静态图（照原版知识恶魔）：原版在 BossNodePath 后拼 .png（剪影）与 _outline.png（描边）。
	public override string BossNodePath => RoaringFlareAssets.TalulahBossNodePrefix;

	public override MegaSkeletonDataResource? BossNodeSpineResource => null;

	// 中途才召唤的火球场景、引燃的卡面覆盖层与结算时的火焰特效要列进预载，否则召唤时“Asset not cached”并同步加载卡一下（苦难摇篮实机踩过）。
	public override EncounterAssetProfile AssetProfile =>
		RoaringFlareAssets.TalulahBossProfile([ModelDb.Monster<Fireball>().CreatureScenePath, NFireBurstVfx.scenePath,
			RoaringFlareAssets.AfflictionOverlay("ignited")]);

	// 镜头拉远照原版三体 Boss 与苦难摇篮爱国者（0.85、下移 30）；专属背景按这个镜头构图，下移超过约 33 会露出贴图上沿。
	public override float GetCameraScaling() => 0.85f;

	public override Vector2 GetCameraOffset() => Vector2.Down * 30f;

	// 专属战场背景：不用幕背景，改用 AssetProfile 里的背景根场景与图层目录。
	protected override bool UseActCombatBackground => false;

	public override IReadOnlyList<string> Slots => Talulah.FormationSlots;

	protected override IReadOnlyList<MonsterModel> Lineup => [M<Talulah>()];

	protected override IReadOnlyList<string> LineupSlots => [Talulah.BossSlot];

	protected override IEnumerable<MonsterModel> Candidates => [M<Talulah>(), M<Fireball>()];
}
