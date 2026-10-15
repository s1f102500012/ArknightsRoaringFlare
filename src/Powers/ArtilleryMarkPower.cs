using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Scaffolding.Content;

namespace ArknightsRoaringFlare.Powers;

/// <summary>
/// 着弹点（挂在玩家身上，层数 = 炮弹伤害）：各次标定独立计时，下一个敌方回合结束时结算并移除。
/// PRTS：先兆者的普攻是向目标位置发射、3 秒后落地的弹道，使用缓存攻击力，发射者死了炮弹照样落地。
/// 所以先兆者在这期间被击杀时，炮弹在下一个敌方回合结束时由本能力自己落地（能被格挡）。挂上的那个敌方回合结束时不落地。
/// </summary>
public sealed class ArtilleryMarkPower : ModPowerTemplate
{
	public override PowerAssetProfile AssetProfile => RoaringFlareAssets.PowerIcon("artillery_mark_power");

	private sealed class Data
	{
		public bool Armed;
	}

	public override PowerType Type => PowerType.Debuff;

	public override PowerStackType StackType => PowerStackType.Counter;

	public override PowerInstanceType InstanceType => PowerInstanceType.Instanced;

	protected override object InitInternalData()
	{
		return new Data();
	}

	public override async Task AfterSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
	{
		if (side != CombatSide.Enemy || Owner.IsDead)
		{
			return;
		}

		Data data = GetInternalData<Data>();
		if (!data.Armed)
		{
			data.Armed = true;
			return;
		}

		Flash();
		await CreatureCmd.Damage(choiceContext, Owner, Amount, ValueProp.Unpowered, null, null, null);
		await PowerCmd.Remove(this);
	}
}
