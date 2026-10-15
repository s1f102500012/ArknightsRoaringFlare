using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsRoaringFlare.Powers;

/// <summary>每次施加独立倒计时；持有者出牌或结束回合时减一，归零结算可被格挡的伤害。</summary>
public sealed class CollapsingDreadPower : ModPowerTemplate
{
	private const int DreadDamage = 16;
	private bool _detonated;

	public override PowerAssetProfile AssetProfile => RoaringFlareAssets.PowerIcon("hunter_sovereign_power");
	public override PowerType Type => PowerType.Debuff;
	public override PowerStackType StackType => PowerStackType.Counter;
	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;
	protected override IEnumerable<DynamicVar> CanonicalVars => [new DynamicVar("Damage", DreadDamage)];

	public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) =>
		cardPlay.Card.Owner == Owner.Player ? Tick(choiceContext) : Task.CompletedTask;

	public override Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants) =>
		participants.Contains(Owner) ? Tick(choiceContext) : Task.CompletedTask;

	private async Task Tick(PlayerChoiceContext choiceContext)
	{
		if (_detonated || Owner.IsDead || !Owner.Powers.Contains(this) || Amount <= 0)
		{
			return;
		}

		Flash();
		await PowerCmd.ModifyAmount(choiceContext, this, -1, null, null);
		if (Amount <= 0)
		{
			_detonated = true;
			// ModifyAmount 在零层时移除这个实例；伤害仍由本次倒计时结算一次。
			await CreatureCmd.Damage(choiceContext, Owner, DynamicVars["Damage"].BaseValue, ValueProp.Unpowered, null, null);
		}
	}
}
