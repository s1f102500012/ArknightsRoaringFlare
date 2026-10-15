using ArknightsRoaringFlare.Monsters;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsRoaringFlare.Powers;

/// <summary>层数是剩余可承受的失血量；实际失血逐点扣除，归零落地并击晕，治疗不补充。</summary>
public sealed class HarbingerFlightPower : ModPowerTemplate
{
	private bool _grounded;

	public override PowerAssetProfile AssetProfile => RoaringFlareAssets.PowerIcon("last_stand_power");
	public override PowerType Type => PowerType.Buff;
	public override PowerStackType StackType => PowerStackType.Counter;

	internal static int AmountForMaxHp(int maxHp, int percent) => (int)decimal.Ceiling(maxHp * (decimal)percent / 100m);

	public override Task AfterApplied(Creature? applier, CardModel? cardSource)
	{
		if (Owner.Monster is HarbingerBase harbinger)
		{
			harbinger.SetFlightVisual(true);
		}
		return Task.CompletedTask;
	}

	public override Task AfterCurrentHpChanged(Creature creature, decimal delta)
	{
		if (creature != Owner || delta >= 0 || Owner.IsDead || _grounded || Amount <= 0 || !Owner.Powers.Contains(this))
		{
			return Task.CompletedTask;
		}

		decimal consumed = Math.Min(Amount, decimal.Ceiling(-delta));
		return PowerCmd.ModifyAmount(new ThrowingPlayerChoiceContext(), this, -consumed, null, null);
	}

	public override async Task AfterRemoved(Creature oldOwner)
	{
		if (oldOwner.IsDead || oldOwner.Monster is not HarbingerBase harbinger)
		{
			return;
		}

		harbinger.SetFlightVisual(false);
		if (Amount > 0 || _grounded)
		{
			return;
		}

		// ModifyAmount 归零时移除本实例，再在这里落地；超额伤害和后续钩子只触发一次击晕。
		_grounded = true;
		if (oldOwner.IsStunned)
		{
			harbinger.NextMove.FollowUpState = harbinger.MoveStateMachine!.States[HarbingerBase.ModeBranchId];
		}
		else
		{
			await CreatureCmd.Stun(oldOwner, HarbingerBase.ModeBranchId);
		}
	}
}
