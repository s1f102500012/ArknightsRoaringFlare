using ArknightsRoaringFlare.Vfx;
using ArknightsRoaringFlare.Powers;
using Godot;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Random;

namespace ArknightsRoaringFlare.Monsters;

/// <summary>
/// 飞行期间累计失去指定比例的最大生命后落地并击晕；标定的着弹点独立延迟结算。
/// 飞行/着陆共用原作音效和特效，数值与行动规则见 docs/设计文档.md。
/// </summary>
public abstract class HarbingerBase : FlareMonster
{
	public const string MarkMoveId = "MARK_MOVE";
	public const string BombardMoveId = "BOMBARD_MOVE";
	public const string RapidFireMoveId = "RAPID_FIRE_MOVE";
	public const string ModeBranchId = "HARBINGER_MODE";
	public const string FlightBranchId = "HARBINGER_FLIGHT";
	internal const int StandardFlightPercent = 25;
	private const string GroundTrigger = "HarbingerGround";
	private const float GroundedDropPixels = 45f;
	private bool _groundedVisual;
	private Vector2? _flyingBodyPosition;

	protected abstract int InitialFlightPercent { get; }
	protected abstract int MarkAmount { get; }
	protected bool IsFlying => Creature.HasPower<HarbingerFlightPower>();

	// 原作音效（audio_data.json：enemy_1112_emppnt.attack 标定收尾 e_atk_aim、开火 e_atk_energyshotcast、炮弹命中 e_imp_grenade_n）。
	protected override FlareSound SoundFor(string slot) => slot switch
	{
		RangedHitSfx => new(On: "e_atk_aim"),
		_ => new(On: "e_atk_energyshotcast", Hit: "e_imp_grenade_n"),
	};

	// 原作特效（enemy_1112_emppnt：开火 enemy_emppnt_attack_01_start_02 在炮口（第 11 根骨骼 C_Muzzle），炮弹
	// projectile_enemy_emppnt 拖尾 enemy_emppnt_attack_01_trail、落点 enemy_emppnt_attack_01_hit；预警准星与光柱
	// enemy_emppnt_attack_01_start 放在落点，自身的扫描 enemy_emppnt_scanning_buff）。Graphic 缩放 0.19×0.24（比士兵小，
	// 按纵向 0.24 换算），HitPoint (0, 0.6)。炮弹是抛射（原作弹道带高度）。
	protected override AkFxRig Rig => new(0.24f, new Godot.Vector2(0f, 0.6f));

	protected override IEnumerable<string> ExtraEffects => ["enemy_emppnt_scanning_buff", "enemy_emppnt_attack_01_start"];

	protected override FlareFx FxFor(string slot) => slot switch
	{
		RangedHitSfx => default,
		_ => new(
			// 原作挂点组件：_start_02 在单位根节点（无人机的根节点在它投下阴影的地面上），炮弹从 C_Muzzle（11 号）射出。
			Start: [new("enemy_emppnt_attack_01_start_02", FxAnchor.Feet, Delay: 0.3f)],
			Hit: [new("enemy_emppnt_attack_01_hit", FxAnchor.Feet)],
			Projectile: "enemy_emppnt_attack_01_trail",
			ProjectileBone: 11,
			ProjectileArc: 1.2f),
	};

	public override IReadOnlyList<string> RequiredAnimations => ["Idle", AttackAnimation, "Die"];

	// 攻击动画 OnAttack 事件的时刻（docs/spine_import.md）。
	protected override float AttackHitDelay => 0.333f;

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await GainFlight(InitialFlightPercent);
	}

	protected Task GainFlight(int percent) =>
		ApplyPower<HarbingerFlightPower>([Creature], HarbingerFlightPower.AmountForMaxHp(Creature.MaxHp, percent));

	internal void SetFlightVisual(bool flying)
	{
		AssertMutable();
		if (_groundedVisual == !flying)
		{
			return;
		}

		_groundedVisual = !flying;
		var node = NCombatRoom.Instance?.GetCreatureNode(Creature);
		if (node is null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}

		Node2D body = node.Visuals.Body;
		_flyingBodyPosition ??= body.Position;
		body.Position = _flyingBodyPosition.Value + (flying ? Vector2.Zero : Vector2.Down * GroundedDropPixels);
		node.SetAnimationTrigger(flying ? CreatureAnimator.idleTrigger : GroundTrigger);
	}

	protected async Task MarkMove(IReadOnlyList<Creature> targets)
	{
		PlaySound(RangedHitSfx);
		PlayFx(new FxCue("enemy_emppnt_scanning_buff", FxAnchor.Feet, Tilt: true, StopAfter: 2f));
		foreach (Creature target in targets)
		{
			PlayFxOn(target, new FxCue("enemy_emppnt_attack_01_start", FxAnchor.Feet, StopAfter: 2.6f, Tune: GroundDecal));
		}

		await ApplyPower<ArtilleryMarkPower>(targets, MarkAmount);
	}

	protected abstract string SelectFlightMove(Rng rng);

	protected MonsterState CreateFlightBranch() => new FlightBranch(this);

	protected ConditionalBranchState CreateModeBranch(MonsterState flight, MoveState grounded)
	{
		ConditionalBranchState mode = new(ModeBranchId);
		mode.AddState(flight, () => IsFlying);
		mode.AddState(grounded, () => !IsFlying);
		return mode;
	}

	// 先过滤零权重和上一招，再交给原版同步 RNG；避免 RandomBranchState 在 RNG=0 时选中零权重首项。
	protected static string ChooseFlightMove(Creature owner, Rng rng, string? previous, params (string Id, float Weight)[] choices)
	{
		RandomBranchState branch = new("HARBINGER_WEIGHTED");
		branch.States = choices.Where(choice => choice.Weight > 0 && choice.Id != previous)
			.Select(choice => new RandomBranchState.StateWeight
			{
				stateId = choice.Id,
				repeatType = MoveRepeatType.CanRepeatForever,
				weightLambda = () => choice.Weight,
			}).ToList();
		return branch.GetNextState(owner, rng);
	}

	internal async Task RestoreFlight()
	{
		if (Creature.IsDead || IsFlying)
		{
			return;
		}

		await GainFlight(StandardFlightPercent);
		// 中枢视觉居中、行动顺序仍在最后；下一玩家回合统一选招时恢复飞行招式，尚未执行的击晕保留。
	}

	private sealed class FlightBranch(HarbingerBase owner) : MonsterState
	{
		public override string Id => FlightBranchId;
		public override bool ShouldAppearInLogs => false;
		public override string GetNextState(Creature creature, Rng rng) => owner.SelectFlightMove(rng);
		public override void RegisterStates(Dictionary<string, MonsterState> states) => states.Add(Id, this);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		if (controller.BoundObject is Node2D body)
		{
			_flyingBodyPosition = body.Position;
			if (_groundedVisual)
			{
				body.Position += Vector2.Down * GroundedDropPixels;
			}
		}

		AnimState idle = new("Idle", isLooping: true);
		// Die 不设置后继，播完保留坠地末帧；着陆期间攻击和待机触发器都无法覆盖它。
		AnimState grounded = new("Die");
		CreatureAnimator animator = new(_groundedVisual ? grounded : idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle, () => !_groundedVisual);
		animator.AddAnyState(CreatureAnimator.attackTrigger, new AnimState(AttackAnimation) { NextState = idle }, () => !_groundedVisual);
		animator.AddAnyState(GroundTrigger, grounded);
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState("Die"));
		return animator;
	}
}

/// <summary>三招随机且不连用，全部实际执行后装填一次；着陆期间速射，重启保留本轮记录。</summary>
public sealed class ArtilleryHarbinger : HarbingerBase
{
	public const string DisruptMoveId = "DISRUPT_MOVE";
	public const string ReloadMoveId = "RELOAD_MOVE";
	private const int DisruptDazed = 3;
	private const int BombardFrail = 1;
	private const int ReloadStrength = 10;
	private const int RapidBlock = 8;
	private const int AllFlightMoves = 0b111;
	private int _usedFlightMoves;
	private string? _previousFlightMove;

	public override string SceneName => "artillery_harbinger";

	public override int MinInitialHp => ToughValue(62, 59);

	public override int MaxInitialHp => ToughValue(66, 63);
	protected override int InitialFlightPercent => StandardFlightPercent;
	protected override int MarkAmount => 9;
	private int BombardDamage => DeadlyValue(11, 10);
	private int RapidDamage => DeadlyValue(9, 7);

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState mark = new(MarkMoveId, targets => FlightMove(MarkMoveId, 0b001, () => MarkMove(targets)), new DebuffIntent());
		MoveState disrupt = new(DisruptMoveId, targets => FlightMove(DisruptMoveId, 0b010, () => AddStatusCards<Dazed>(targets, DisruptDazed)), new StatusIntent(DisruptDazed));
		MoveState bombard = new(BombardMoveId, targets => FlightMove(BombardMoveId, 0b100, () => BombardMove(targets)), new SingleAttackIntent(BombardDamage), new DebuffIntent());
		MoveState reload = new(ReloadMoveId, ReloadMove, new BuffIntent());
		MoveState rapid = new(RapidFireMoveId, RapidFireMove, new SingleAttackIntent(RapidDamage), new DefendIntent());
		MonsterState flight = CreateFlightBranch();
		ConditionalBranchState mode = CreateModeBranch(flight, rapid);
		foreach (MoveState move in new[] { mark, disrupt, bombard, reload, rapid })
		{
			move.FollowUpState = mode;
		}

		return Build([mark, disrupt, bombard, reload, rapid, flight, mode], flight);
	}

	protected override string SelectFlightMove(Rng rng) => _usedFlightMoves == AllFlightMoves
		? ReloadMoveId
		: ChooseFlightMove(Creature, rng, _previousFlightMove, (MarkMoveId, 1), (DisruptMoveId, 1), (BombardMoveId, 1));

	private Task FlightMove(string id, int usedBit, Func<Task> action)
	{
		_previousFlightMove = id;
		_usedFlightMoves |= usedBit;
		return action();
	}

	private async Task BombardMove(IReadOnlyList<Creature> targets)
	{
		await Strike(BombardDamage, vfx: HeavyBluntVfx, sfx: BombHitSfx);
		await ApplyPower<FrailPower>(targets, BombardFrail);
	}

	private async Task ReloadMove(IReadOnlyList<Creature> targets)
	{
		_usedFlightMoves = 0;
		_previousFlightMove = null;
		BuffCue();
		await ApplyStrengthToSelf(ReloadStrength);
	}

	private async Task RapidFireMove(IReadOnlyList<Creature> targets)
	{
		await Strike(RapidDamage, vfx: BluntVfx, sfx: BombHitSfx);
		await GainBlock(RapidBlock);
	}
}

/// <summary>标定开场，按小怪数量选择飞行招式；落地后速射、修复、重启。</summary>
public sealed class CoreHarbinger : HarbingerBase
{
	public const string MaintainMoveId = "MAINTAIN_MOVE";
	public const string CallMoveId = "CALL_MOVE";
	public const string RepairMoveId = "REPAIR_MOVE";
	public const string RestartMoveId = "RESTART_MOVE";
	public const string CoreSlot = "core";
	public static readonly IReadOnlyList<string> HarbingerSlots = ["harbinger1", "harbinger2"];
	public static readonly IReadOnlyList<string> FormationSlots = [.. HarbingerSlots, CoreSlot];
	private static readonly int[] RestartFlightPercents = [StandardFlightPercent, 20, 10];
	private const int MaintainBlock = 20;
	private const int MaintainVigor = 5;
	private const int RapidHits = 2;
	private const int RepairBlock = 20;
	private const int RepairHealPercent = 10;
	private const int RepairWeak = 2;
	private int _restartCount;
	private string? _previousFlightMove;

	public override string SceneName => "core_harbinger";

	public override int MinInitialHp => ToughValue(155, 152);

	public override int MaxInitialHp => ToughValue(159, 156);
	protected override int InitialFlightPercent => 50;
	protected override int MarkAmount => 20;
	private int CallDamage => DeadlyValue(12, 11);
	private int RapidDamage => DeadlyValue(5, 4);
	private int LivingHarbingers => CombatState.Enemies.Count(c => c.IsAlive && c.Monster is ArtilleryHarbinger);
	private string? FreeHarbingerSlot => HarbingerSlots.FirstOrDefault(slot => CombatState.Enemies.All(c => c.SlotName != slot));

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState mark = new(MarkMoveId, targets => FlightMove(MarkMoveId, () => MarkMove(targets)), new DebuffIntent());
		MoveState maintain = new(MaintainMoveId, targets => FlightMove(MaintainMoveId, MaintainMove), new DefendIntent(), new BuffIntent());
		MoveState call = new(CallMoveId, targets => FlightMove(CallMoveId, CallMove), new SingleAttackIntent(CallDamage), new SummonIntent());
		MoveState rapid = new(RapidFireMoveId, targets => Strike(RapidDamage, RapidHits, BluntVfx, BombHitSfx), new MultiAttackIntent(RapidDamage, RapidHits));
		MoveState repair = new(RepairMoveId, RepairMove, new DefendIntent(), new HealIntent(), new DebuffIntent());
		MoveState restart = new(RestartMoveId, RestartMove, new BuffIntent());
		MonsterState flight = CreateFlightBranch();
		ConditionalBranchState mode = CreateModeBranch(flight, rapid);
		mark.FollowUpState = maintain.FollowUpState = call.FollowUpState = mode;
		rapid.FollowUpState = repair;
		repair.FollowUpState = restart;
		restart.FollowUpState = mode;
		return Build([mark, maintain, call, rapid, repair, restart, flight, mode], mark);
	}

	protected override string SelectFlightMove(Rng rng)
	{
		int count = LivingHarbingers;
		bool canSummon = count < HarbingerSlots.Count && FreeHarbingerSlot is not null;
		if (count == 0 && canSummon)
		{
			return CallMoveId;
		}

		return ChooseFlightMove(Creature, rng, _previousFlightMove,
			(MarkMoveId, 1), (MaintainMoveId, 1), (CallMoveId, canSummon ? 2 : 0));
	}

	private Task FlightMove(string id, Func<Task> action)
	{
		_previousFlightMove = id;
		return action();
	}

	private async Task MaintainMove()
	{
		BuffCue();
		await GainBlock(MaintainBlock);
		await ApplyPower<VigorPower>([Creature], MaintainVigor);
	}

	private async Task CallMove()
	{
		await Strike(CallDamage, vfx: HeavyBluntVfx, sfx: BombHitSfx);
		if (Creature.IsDead || LivingHarbingers >= HarbingerSlots.Count || FreeHarbingerSlot is not { } slot)
		{
			return;
		}

		ArtilleryHarbinger harbinger = (ArtilleryHarbinger)ModelDb.Monster<ArtilleryHarbinger>().ToMutable();
		await CreatureCmd.Add(harbinger, CombatState, slotName: slot);
	}

	private async Task RepairMove(IReadOnlyList<Creature> targets)
	{
		BuffCue();
		await GainBlock(RepairBlock);
		await CreatureCmd.Heal(Creature, Creature.MaxHp * RepairHealPercent / 100m);
		await ApplyPower<WeakPower>(targets, RepairWeak);
	}

	private async Task RestartMove(IReadOnlyList<Creature> targets)
	{
		BuffCue();
		int flightPercent = RestartFlightPercents[_restartCount];
		_restartCount = Math.Min(_restartCount + 1, RestartFlightPercents.Length - 1);
		_previousFlightMove = null;
		await GainFlight(flightPercent);
		foreach (ArtilleryHarbinger ally in CombatState.Enemies.Where(c => c.IsAlive).Select(c => c.Monster).OfType<ArtilleryHarbinger>().ToList())
		{
			await ally.RestoreFlight();
		}
	}
}
