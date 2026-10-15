using ArknightsRoaringFlare.Powers;
using ArknightsRoaringFlare.Vfx;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Animation;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace ArknightsRoaringFlare.Monsters;

/// <summary>
/// 乌萨斯突击者（PRTS enemy_1111_ucommd），重装前排（PRTS：占用 2 个阻挡数）。
/// 重劈 → 稳阵；存活时为其他敌人分担一半未被格挡伤害。
/// </summary>
public sealed class UrsusCommando : FlareMonster
{
	public const string CleaveMoveId = "CLEAVE_MOVE";
	public const string HoldLineMoveId = "HOLD_LINE_MOVE";

	private const int HoldLineBlock = 33;
	private const int SymbiosisPercent = 50;

	// 攻击动画 OnAttack 事件的时刻（docs/spine_import.md）。
	protected override float AttackHitDelay => 0.567f;

	public override string SceneName => "ursus_commando";

	// 原作音效（audio_data.json：enemy_1111_ucommd.attack 起手 e_atk_sword_n，.combat 命中 e_imp_sword_h）。
	protected override FlareSound SoundFor(string slot) => slot switch
	{
		_ => new(Start: "e_atk_sword_n", Hit: "e_imp_sword_h"),
	};

	// 原作特效（enemy_1111_ucommd 的近战 Combat：刺光 enemy_ucommd_attack_01_start/_start_02 对到出手（OnAttack 0.567 秒），命中 enemy_ucommd_attack_01_hit）。
	// 原作挂点组件：_start 在单位根节点（脚下），_start_02 在 SpecialPoint0 即武器骨骼 C_Weapon_03（两套骨架都是 6 号），朝向取单位朝向（A=10，同根节点特效）。
	private const int WeaponBone = 6;

	protected override AkFxRig Rig => new(0.29f, new Godot.Vector2(0f, 1.6f));

	protected override FlareFx FxFor(string slot) => new(
		Start: [new("enemy_ucommd_attack_01_start", FxAnchor.Feet, Delay: 0.5f), new("enemy_ucommd_attack_01_start_02", FxAnchor.Bone, WeaponBone, 0.5f, Spin: false)],
		Hit: [new("enemy_ucommd_attack_01_hit")]);

	// 待机动画在 PRTS 原文件里就拼作 Idile。
	public override IReadOnlyList<string> RequiredAnimations => ["Idile", AttackAnimation, "Die", "Move"];

	public override int MinInitialHp => ToughValue(94, 91);
	public override int MaxInitialHp => ToughValue(98, 95);
	private int CleaveDamage => DeadlyValue(14, 12);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<OriginiumSymbiosisPower>([Creature], SymbiosisPercent);
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState cleave = new(CleaveMoveId, CleaveMove, new SingleAttackIntent(CleaveDamage));
		MoveState hold = new(HoldLineMoveId, HoldLineMove, new DefendIntent());
		cleave.FollowUpState = hold;
		hold.FollowUpState = cleave;
		return Machine([cleave, hold], cleave);
	}

	private Task CleaveMove(IReadOnlyList<Creature> targets) => Strike(CleaveDamage);

	private async Task HoldLineMove(IReadOnlyList<Creature> targets)
	{
		BuffCue();
		await GainBlock(HoldLineBlock);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		CreatureAnimator animator = BuildAnimator(controller, "Idile", "Die", (CreatureAnimator.attackTrigger, AttackAnimation));
		animator.AddAnyState(ReinforcementEntrance.RunTrigger, new AnimState("Move", isLooping: true));
		return animator;
	}
}

/// <summary>
/// 乌萨斯突击者队长：半血开场，第三回合撤退并交接三名援军。
/// </summary>
public sealed class CommandoCaptain : FlareMonster
{
	public const string ChargeMoveId = "CHARGE_MOVE";
	public const string RetreatMoveId = "RETREAT_MOVE";
	public const string FallenRetreatMoveId = "FALLEN_RETREAT_MOVE";
	public const string FallenWaitMoveId = "FALLEN_WAIT_MOVE";
	public const string PressMoveId = "PRESS_MOVE";

	private const int RetreatTurns = 3;
	private const int StartingHpPercent = 50;
	private const int PressDebuff = 99;
	private const int ReinforcementLowMorale = 2;
	public const string CaptainSlot = "captain";
	public static readonly IReadOnlyList<string> FormationSlots = ["commando", "caster", "crossbowman", CaptainSlot];
	private MoveState _fallenWait = null!;
	private MoveState _fallenRetreat = null!;
	private bool _reinforcementsCalled;
	private bool IsDowned => Creature.GetPower<TacticalRetreatPower>()?.IsDowned == true;
	private int TurnsRemaining => Creature.GetPower<TacticalRetreatPower>()?.Amount ?? RetreatTurns;

	// 攻击动画 OnAttack 事件的时刻（docs/spine_import.md）。
	protected override float AttackHitDelay => 0.567f;

	public override string SceneName => "commando_captain";

	// 原作音效（audio_data.json：enemy_1111_ucommd.attack 起手 e_atk_sword_n，.combat 命中 e_imp_sword_h）。
	protected override FlareSound SoundFor(string slot) => slot switch
	{
		_ => new(Start: "e_atk_sword_n", Hit: "e_imp_sword_h"),
	};

	// 原作特效（enemy_1111_ucommd 的近战 Combat：刺光 enemy_ucommd_attack_01_start/_start_02 对到出手（OnAttack 0.567 秒），命中 enemy_ucommd_attack_01_hit）。
	// 原作挂点组件：_start 在单位根节点（脚下），_start_02 在 SpecialPoint0 即武器骨骼 C_Weapon_03（两套骨架都是 6 号），朝向取单位朝向（A=10，同根节点特效）。
	private const int WeaponBone = 6;

	protected override AkFxRig Rig => new(0.29f, new Godot.Vector2(0f, 1.6f));

	protected override FlareFx FxFor(string slot) => new(
		Start: [new("enemy_ucommd_attack_01_start", FxAnchor.Feet, Delay: 0.5f), new("enemy_ucommd_attack_01_start_02", FxAnchor.Bone, WeaponBone, 0.5f, Spin: false)],
		Hit: [new("enemy_ucommd_attack_01_hit")]);

	// 待机动画在 PRTS 原文件里就拼作 Idile。
	public override IReadOnlyList<string> RequiredAnimations => ["Idile", AttackAnimation, "Die", "Move"];

	public override int MinInitialHp => ToughValue(167, 164);
	public override int MaxInitialHp => ToughValue(171, 168);
	private int ChargeDamage => DeadlyValue(17, 15);
	private int RetreatDamage => DeadlyValue(23, 21);

	public override async Task AfterAddedToRoom()
	{
		await base.AfterAddedToRoom();
		await ApplyPower<TacticalRetreatPower>([Creature], RetreatTurns);
		await CreatureCmd.SetCurrentHp(Creature, decimal.Floor(Creature.MaxHp * StartingHpPercent / 100m));
	}

	protected override MonsterMoveStateMachine GenerateMoveStateMachine()
	{
		MoveState press = new(PressMoveId, PressMove, new DebuffIntent());
		MoveState charge = new(ChargeMoveId, ChargeMove, new SingleAttackIntent(ChargeDamage));
		MoveState retreat = new(RetreatMoveId, RetreatMove, new SingleAttackIntent(RetreatDamage), new EscapeIntent(), new SummonIntent());
		_fallenWait = new(FallenWaitMoveId, _ => Task.CompletedTask, new UnknownIntent());
		_fallenRetreat = new(FallenRetreatMoveId, RetreatMove, new EscapeIntent(), new SummonIntent());
		ConditionalBranchState next = new("CAPTAIN_NEXT");
		next.AddState(_fallenRetreat, () => IsDowned && TurnsRemaining <= 1);
		next.AddState(_fallenWait, () => IsDowned);
		next.AddState(retreat, () => TurnsRemaining <= 1);
		next.AddState(charge, () => true);
		foreach (MoveState move in new[] { press, charge, retreat, _fallenWait, _fallenRetreat })
		{
			move.FollowUpState = next;
		}
		return Build([press, charge, retreat, _fallenWait, _fallenRetreat, next], press);
	}

	internal void FallBeforeRetreat()
	{
		SetMoveImmediate(TurnsRemaining <= 1 ? _fallenRetreat : _fallenWait, forceTransition: true);
		NCombatRoom.Instance?.GetCreatureNode(Creature)?.SetAnimationTrigger(CreatureAnimator.deathTrigger);
	}

	private async Task ChargeMove(IReadOnlyList<Creature> targets)
	{
		if (!IsDowned)
		{
			await Strike(ChargeDamage, vfx: HeavyBluntVfx, sfx: HeavyHitSfx);
		}
	}

	private async Task PressMove(IReadOnlyList<Creature> targets)
	{
		if (IsDowned)
		{
			return;
		}
		BuffCue();
		await ApplyPower<WeakPower>(targets, PressDebuff);
		await ApplyPower<FrailPower>(targets, PressDebuff);
		await ApplyPower<VulnerablePower>(targets, PressDebuff);
	}

	private async Task RetreatMove(IReadOnlyList<Creature> targets)
	{
		if (_reinforcementsCalled)
		{
			return;
		}
		_reinforcementsCalled = true;
		if (!IsDowned)
		{
			await Strike(RetreatDamage, vfx: HeavyBluntVfx, sfx: HeavyHitSfx);
		}
		if (CombatManager.Instance.IsOverOrEnding)
		{
			return;
		}
		// 撤退攻击的反伤也可能击倒队长，在攻击完整结算后读取结果。
		bool failedRetreat = IsDowned;
		await ReinforcementEntrance.Exit(Creature, failedRetreat);
		FlareMonster[] reinforcements =
		[
			(FlareMonster)ModelDb.Monster<UrsusCommando>().ToMutable(),
			(FlareMonster)ModelDb.Monster<UrsusArmoredCaster>().ToMutable(),
			(FlareMonster)ModelDb.Monster<UrsusCrossbowman>().ToMutable(),
		];
		for (int i = 0; i < reinforcements.Length; i++)
		{
			reinforcements[i].EnterFromRight = true;
			Creature ally = await CreatureCmd.Add(reinforcements[i], CombatState, slotName: FormationSlots[i]);
			if (failedRetreat)
			{
				await ApplyPower<LowMoralePower>([ally], ReinforcementLowMorale);
			}
		}
		// 队长的保留能力持续到援军全部加入，期间战斗不会提前结束。
		await CreatureCmd.Escape(Creature);
	}

	public override CreatureAnimator GenerateAnimator(MegaSprite controller)
	{
		AnimState idle = new("Idile", isLooping: true);
		CreatureAnimator animator = new(idle, controller);
		animator.AddAnyState(CreatureAnimator.idleTrigger, idle, () => !IsDowned);
		animator.AddAnyState(CreatureAnimator.attackTrigger, new AnimState(AttackAnimation) { NextState = idle }, () => !IsDowned);
		animator.AddAnyState(ReinforcementEntrance.RunTrigger, new AnimState("Move", isLooping: true), () => !IsDowned);
		animator.AddAnyState(CreatureAnimator.deathTrigger, new AnimState("Die"));
		return animator;
	}
}
