"""生成 assets/localization/{zhs,eng}/ 下的 acts / encounters / monsters / powers 表。

键名由类名经 RitsuLib 得出（ARKNIGHTS_ROARING_FLARE_<类别>_<类名大写下划线>），招式键去掉 _MOVE 后缀。
改文案改这里再跑一遍（python3 tools/write_loc.py），不要手改生成的 JSON。eng 是其余语言的回退，不能放中文。
"""
import json
import pathlib
import re

ROOT = pathlib.Path(__file__).resolve().parent.parent / 'assets' / 'localization'
PREFIX = 'ARKNIGHTS_ROARING_FLARE'


def slug(name: str) -> str:
    return re.sub(r'(?<=[a-z0-9])(?=[A-Z])', '_', name).upper()


ACT = {'title': ('怒号光明', 'Roaring Flare'), 'description': ('怒号光明', 'Roaring Flare')}

# 类名: (中文, 英文, 战败文案中文, 战败文案英文)
ENCOUNTERS = {
    'PatrolWeak': ('纠察巡逻队（先遣）', 'Patrol Squad (Advance)', '{character}倒在了[gold]{encounter}[/gold]的武器下。', '{character} fell to the weapons of the [gold]{encounter}[/gold].'),
    'RavagersWeak': ('裂兽', 'Ravagers', '{character}被[gold]{encounter}[/gold]撕碎了。', '{character} was torn apart by [gold]{encounter}[/gold].'),
    'ArmoredCasterWeak': ('着铠术师', 'Armored Caster', '{character}倒在了[gold]{encounter}[/gold]的法术之下。', '{character} fell to the arts of an [gold]{encounter}[/gold].'),
    'HuskVillageSearchNormal': ('纠察巡逻队（精锐）', 'Patrol Squad (Elite)', '{character}没能突破[gold]{encounter}[/gold]的封锁。', '{character} could not break through the [gold]{encounter}[/gold].'),
    'RavagerPackNormal': ('裂兽群', 'Ravager Pack', '{character}被[gold]{encounter}[/gold]撕碎了。', '{character} was torn apart by a [gold]{encounter}[/gold].'),
    'CrossbowmenNormal': ('突袭弩手', 'Raid Crossbowmen', '{character}倒在了[gold]{encounter}[/gold]的弩箭下。', '{character} fell to the bolts of the [gold]{encounter}[/gold].'),
    'CasterSquadNormal': ('高级术士', 'Senior Caster', '{character}倒在了[gold]{encounter}[/gold]的法术之下。', '{character} fell to the arts of a [gold]{encounter}[/gold].'),
    'CommandoSquadNormal': ('突击者小队', 'Commando Squad', '{character}被[gold]{encounter}[/gold]冲垮了。', '{character} was overrun by a [gold]{encounter}[/gold].'),
    'GuidedArtilleryNormal': ('牵引的炮火', 'Guided Artillery', '{character}没能躲开[gold]{encounter}[/gold]。', '{character} could not escape the [gold]{encounter}[/gold].'),
    'SilentButcherNormal': ('沉默的屠夫', 'The Silent Butcher', '{character}倒在了[gold]{encounter}[/gold]面前。', '{character} fell before [gold]{encounter}[/gold].'),
    'HostCenturionNormal': ('宿主百夫长', 'Host Centurion', '{character}被[gold]{encounter}[/gold]吞噬了。', '{character} was devoured by the [gold]{encounter}[/gold].'),
    'CommandoCaptainNormal': ('突击者队长', 'Commando Captain', '{character}被[gold]{encounter}[/gold]冲垮了。', '{character} was overrun by a [gold]{encounter}[/gold].'),
    'EmperorsBladeElite': ('皇帝的利刃', "The Emperor's Blade", '{character}倒在了[gold]{encounter}[/gold]的国度之中。', "{character} fell within the dominion of [gold]{encounter}[/gold]."),
    'VanguardVeteranElite': ('百战精锐', 'Veteran Vanguard', '{character}倒在了[gold]{encounter}[/gold]面前。', '{character} fell before the [gold]{encounter}[/gold].'),
    'CoreHarbingerElite': ('中枢先兆者', 'Core Harbinger', '{character}没能躲开[gold]{encounter}[/gold]的炮火。', '{character} could not escape the barrage of the [gold]{encounter}[/gold].'),
    'TalulahBoss': ('“不死的黑蛇”', 'Deathless Black Snake', '{character}被[gold]{encounter}[/gold]的烈焰吞没了。', '{character} was consumed by the flames of [gold]{encounter}[/gold].'),
}

# 类名: ((中文名, 英文名), {招式键: (中文, 英文)})
MONSTERS = {
    'InfectedPatrolOfficer': (('感染者纠察官', 'Infected Patrol Officer'),
                              {'NET': ('抛网', 'Cast Net'), 'LASH': ('抽打', 'Lash'),
                               'RELOAD_SHOOT': ('装填射击', 'Reload and Fire'), 'ARM': ('武装', 'Arm Up')}),
    'SeniorPatrolOfficer': (('感染者高级纠察官', 'Senior Patrol Officer'),
                            {'DOZE': ('打盹', 'Doze'), 'ENRAGE': ('激怒', 'Enrage'),
                             'ESCORT': ('押运', 'Escort'), 'DISRUPT': ('干扰', 'Disrupt')}),
    'UrsusRavager': (('乌萨斯裂兽', 'Ursus Ravager'),
                     {'BITE': ('撕咬', 'Bite'), 'ALERT': ('警戒', 'Alert'), 'POUNCE': ('扑击', 'Pounce')}),
    'RavagerMatriarch': (('乌萨斯裂兽族母', 'Ravager Matriarch'),
                         {'SUMMON': ('召集', 'Rally'), 'HEAL': ('治愈', 'Heal'), 'INSPIRE': ('鼓舞', 'Inspire'), 'INTIMIDATE': ('震慑', 'Intimidate')}),
    'UrsusCrossbowman': (('乌萨斯突袭弩手', 'Ursus Raid Crossbowman'),
                         {'WIND': ('上弦', 'Wind Up'), 'BOLT': ('冲击', 'Impact'), 'VOLLEY': ('速射', 'Volley')}),
    'UrsusArmoredCaster': (('乌萨斯着铠术师', 'Ursus Armored Caster'),
                           {'HEX': ('源石法术：咒法', 'Originium Arts: Hex'), 'FLAME': ('源石法术：火焰', 'Originium Arts: Flame'), 'PREPARE': ('仪式准备', 'Ritual Preparation'), 'RITUAL': ('仪式', 'Ritual')}),
    'SeniorArmoredCaster': (('乌萨斯高级着铠术师', 'Senior Armored Caster'),
                            {'ARTS_BOLT': ('源石法术', 'Originium Arts'), 'ARTS_COMBO': ('法术连击', 'Arts Combo'), 'PREPARE': ('仪式准备', 'Ritual Preparation'), 'RITUAL': ('仪式', 'Ritual')}),
    'UrsusCommando': (('乌萨斯突击者', 'Ursus Commando'),
                      {'CLEAVE': ('重劈', 'Cleave'), 'HOLD_LINE': ('稳阵', 'Hold the Line')}),
    'CommandoCaptain': (('乌萨斯突击者队长', 'Commando Captain'),
                        {'PRESS': ('压阵', 'Press On'), 'CHARGE': ('冲锋', 'Charge'), 'RETREAT': ('撤退', 'Retreat'), 'FALLEN_RETREAT': ('撤退', 'Retreat'), 'FALLEN_WAIT': ('倒地', 'Downed')}),
    'ArtilleryHarbinger': (('帝国炮火先兆者', 'Imperial Artillery Harbinger'),
                           {'MARK': ('标定', 'Mark Target'), 'DISRUPT': ('干扰', 'Disrupt'), 'BOMBARD': ('炮击', 'Bombard'),
                            'RELOAD': ('装填', 'Reload'), 'RAPID_FIRE': ('速射', 'Rapid Fire')}),
    'CoreHarbinger': (('帝国炮火中枢先兆者', 'Imperial Core Harbinger'),
                      {'MARK': ('标定', 'Mark Target'), 'MAINTAIN': ('维护', 'Maintenance'), 'CALL': ('呼叫', 'Call Reinforcements'),
                       'RAPID_FIRE': ('速射', 'Rapid Fire'), 'REPAIR': ('自我修复', 'Self-Repair'), 'RESTART': ('重启', 'Restart')}),
    'ImperialVanguard': (('帝国前锋精锐', 'Imperial Vanguard'),
                         {'LONG_SHOT': ('远射', 'Long Shot'), 'THRUST': ('突刺', 'Thrust'), 'RESOLVE': ('战意', 'Resolve')}),
    'VanguardVeteran': (('帝国前锋百战精锐', 'Imperial Vanguard Veteran'),
                        {'LONG_SHOT': ('远射', 'Long Shot'), 'THRUST': ('突刺', 'Thrust'), 'RESOLVE': ('战意', 'Resolve'), 'PARRY': ('弹反', 'Parry')}),
    'HostHeavySoldier': (('宿主重装士兵', 'Host Heavy Soldier'),
                         {'SHIELD_BASH': ('盾击', 'Shield Bash'), 'HOLD': ('坚守', 'Hold Fast'), 'CRUSH': ('重压', 'Crush')}),
    'SarkazHostCenturion': (('萨卡兹宿主百夫长', 'Sarkaz Host Centurion'),
                            {'SIPHON': ('汲取', 'Siphon'), 'DEVOUR': ('群噬', 'Devour'), 'CHANT': ('吟唱', 'Chant')}),
    'EmperorsBladeHunter': (('“皇帝的利刃”，追猎者', '"Emperor\'s Blade", Pursuer'),
                            {'PATROL': ('巡猎', 'Hunt'), 'SLASH': ('斩击', 'Slash'), 'DREAD': ('坍缩恐惧', 'Collapsing Dread'),
                             'REFORM': ('重整', 'Reform'), 'FORTIFY': ('强化', 'Fortify'), 'REND': ('连斩', 'Rending Blades'),
                             'SECOND_DREAD': ('坍缩恐惧', 'Collapsing Dread')}),
    'Talulah': (('“不死的黑蛇”', 'Deathless Black Snake'),
                {'IGNITE': ('点燃', 'Ignite'), 'SNAKE_BITE': ('噬咬', 'Serpent Bite'), 'DETONATE': ('引爆', 'Detonate'),
                 'SCORCH': ('焦土', 'Scorched Earth'), 'UNDYING_FLAME': ('不息恶火', 'Deathless Inferno'),
                 'FIRE_RAIN': ('火雨', 'Rain of Fire')}),
    'Fireball': (('火球', 'Fireball'),
                 {'EXPLODE': ('爆炸', 'Explode')}),
}

# 类名: (中文标题, 英文标题, 中文说明, 英文说明, 中文 smart, 英文 smart)
POWERS = {
    'TacticalRetreatPower': (
        '战术撤退',
        'Tactical Retreat',
        '这名敌人将要在[blue]{Amount}[/blue]回合后撤退。\n[red]最好不要让它活着离开……[/red]',
        'This enemy will retreat in [blue]{Amount}[/blue] turns.\n[red]Better not let it leave alive...[/red]',
        '这名敌人将要在[blue]{Amount}[/blue]回合后撤退。\n[red]最好不要让它活着离开……[/red]',
        'This enemy will retreat in [blue]{Amount}[/blue] turns.\n[red]Better not let it leave alive...[/red]',
    ),
    'LowMoralePower': (
        '士气低迷',
        'Low Morale',
        '这名敌人永久失去[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'This enemy permanently loses [blue]{Amount}[/blue] [gold]Strength[/gold].',
        '这名敌人永久失去[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'This enemy permanently loses [blue]{Amount}[/blue] [gold]Strength[/gold].',
    ),
    'OriginiumHealingPower': (
        '源石技艺：疗愈',
        'Originium Arts: Healing',
        '乌萨斯突袭弩手存活时，所有敌人每次造成未被格挡的伤害后，伤害来源恢复[blue]{Amount}[/blue]点生命。',
        'While the Ursus Raid Crossbowman lives, whenever an enemy deals unblocked damage, that enemy heals [blue]{Amount}[/blue] HP.',
        '[gold]{OwnerName}[/gold]存活时，所有敌人每次造成未被格挡的伤害后，恢复[blue]{Amount}[/blue]点生命。',
        'While the Ursus Raid Crossbowman lives, whenever an enemy deals unblocked damage, that enemy heals [blue]{Amount}[/blue] HP.',
    ),
    'OriginiumArmorPower': (
        '源石技艺：铠甲',
        'Originium Arts: Armor',
        '施法者存活时，所有敌人在玩家回合开始时获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。',
        'While the caster lives, all enemies gain [blue]{Amount}[/blue] [gold]Block[/gold] at the start of the player turn.',
        '[gold]{OwnerName}[/gold]存活时，所有敌人在玩家回合开始时获得[blue]{Amount}[/blue]点[gold]格挡[/gold]。',
        'While [gold]{OwnerName}[/gold] lives, all enemies gain [blue]{Amount}[/blue] [gold]Block[/gold] at the start of the player turn.',
    ),
    'OriginiumSymbiosisPower': (
        '源石技艺：共生',
        'Originium Arts: Symbiosis',
        '乌萨斯突击者存活时，会吸收其他敌人[blue]{Amount}%[/blue]的未被格挡伤害。',
        'While the Ursus Commando lives, it absorbs [blue]{Amount}%[/blue] of the unblocked damage dealt to other enemies.',
        '[gold]{OwnerName}[/gold]存活时，会吸收其他敌人[blue]{Amount}%[/blue]的未被格挡伤害。',
        'While the Ursus Commando lives, it absorbs [blue]{Amount}%[/blue] of the unblocked damage dealt to other enemies.',
    ),
    'OriginiumHexPower': (
        '源石技艺：咒法',
        'Originium Arts: Hex',
        '乌萨斯着铠术师存活时，你的所有[gold]技能牌[/gold]被[blue]{Amount}[/blue]层[gold]咒法[/gold]侵蚀。',
        'While the Ursus Armored Caster lives, all your [gold]Skills[/gold] are afflicted with [blue]{Amount}[/blue] [gold]Hex[/gold].',
        '乌萨斯着铠术师存活时，你的所有[gold]技能牌[/gold]被[blue]{Amount}[/blue]层[gold]咒法[/gold]侵蚀。',
        'While the Ursus Armored Caster lives, all your [gold]Skills[/gold] are afflicted with [blue]{Amount}[/blue] [gold]Hex[/gold].',
    ),
    'OriginiumFrostPower': (
        '源石技艺：霜冻',
        'Originium Arts: Frost',
        '乌萨斯高级着铠术师存活时，你的所有[gold]技能牌[/gold]被[gold]霜冻[/gold]侵蚀。',
        'While the Senior Armored Caster lives, all your [gold]Skills[/gold] are afflicted with [gold]Frost[/gold].',
        '乌萨斯高级着铠术师存活时，你的所有[gold]技能牌[/gold]被[gold]霜冻[/gold]侵蚀。',
        'While the Senior Armored Caster lives, all your [gold]Skills[/gold] are afflicted with [gold]Frost[/gold].',
    ),
    'OriginiumFlamePower': (
        '源石技艺：火焰',
        'Originium Arts: Flame',
        '你在本回合每打出1张[gold]攻击牌[/gold]，就往你的手牌中加入[blue]{Amount}[/blue]张[gold]灼伤[/gold]。',
        'Whenever you play an [gold]Attack[/gold] this turn, add [blue]{Amount}[/blue] [gold]Burn[/gold] to your hand.',
        '你在本回合每打出[blue]1[/blue]张[gold]攻击牌[/gold]，就往手牌中加入[blue]{Amount}[/blue]张[gold]灼伤[/gold]。',
        'Whenever you play an [gold]Attack[/gold] this turn, add [blue]{Amount}[/blue] [gold]Burn[/gold] to your hand.',
    ),
    'IntelligenceGatheringPower': (
        '情报获取',
        'Intelligence Gathering',
        '回合结束时，每有一名己方感染者纠察官存活，获得[gold]情报[/gold]和[gold]力量[/gold]。',
        'At the end of its turn, gains [gold]Intelligence[/gold] and [gold]Strength[/gold] for each living allied Infected Patrol Officer.',
        '回合结束时，每有一名[gold]感染者纠察官[/gold]存活，获得[blue]{Amount}[/blue]层[gold]情报[/gold]和[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'At the end of its turn, gains [blue]{Amount}[/blue] [gold]Intelligence[/gold] and [blue]{Amount}[/blue] [gold]Strength[/gold] for each living allied Infected Patrol Officer.',
    ),
    'IntelligencePower': (
        '情报', 'Intelligence',
        '获得[gold]力量[/gold]。感染者纠察官死亡时，失去对应的[gold]力量[/gold]。',
        'Grants [gold]Strength[/gold]. When an Infected Patrol Officer dies, lose the [gold]Intelligence[/gold] it contributed and the corresponding [gold]Strength[/gold].',
        '获得[blue]{Amount}[/blue]点[gold]力量[/gold]。感染者纠察官死亡时，失去其贡献的[gold]力量[/gold]。',
        'Grants [blue]{Amount}[/blue] [gold]Strength[/gold]. When an Infected Patrol Officer dies, lose the [gold]Intelligence[/gold] it contributed and the corresponding [gold]Strength[/gold].'),
    'TrappedPower': (
        '被困', 'Trapped',
        '本回合不能打出[gold]攻击牌[/gold]。',
        'Cannot play [gold]Attacks[/gold] this turn.',
        '本回合不能打出[gold]攻击牌[/gold]。',
        'Cannot play [gold]Attacks[/gold] this turn.'),
    'RendPower': (
        '撕裂', 'Rend',
        '每次对你造成未被格挡的攻击伤害时，你将在下回合失去[gold]力量[/gold]。',
        'Whenever it deals unblocked attack damage to you, lose [blue]{Amount}[/blue] [gold]Strength[/gold] next turn.',
        '每当[gold]{OwnerName}[/gold]对你造成未被格挡的攻击伤害时，你将在下回合失去[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'Whenever [gold]{OwnerName}[/gold] deals unblocked attack damage to you, lose [blue]{Amount}[/blue] [gold]Strength[/gold] next turn.'),
    'TraumaPower': (
        '创伤', 'Trauma',
        '本回合内失去[gold]力量[/gold]。',
        'Lose [blue]{Amount}[/blue] [gold]Strength[/gold] this turn.',
        '本回合内失去[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'Lose [blue]{Amount}[/blue] [gold]Strength[/gold] this turn.'),
    'PackInstinctPower': (
        '族群意识', 'Pack Instinct',
        '每当一只乌萨斯裂兽死亡时，自身获得[gold]力量[/gold]。',
        'Whenever an allied Ursus Ravager dies, gains [gold]Strength[/gold].',
        '每当一只[gold]乌萨斯裂兽[/gold]死亡时，自身获得[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'Whenever an allied Ursus Ravager dies, gains [blue]{Amount}[/blue] [gold]Strength[/gold].'),
    'FlareRebirthPower': (
        '重生',
        'Rebirth',
        '被击败时不会立刻死亡，之后进入下一阶段。',
        'When defeated, does not leave right away; it enters its next phase instead.',
        '被击败时不会死亡，下回合进入下一阶段。',
        'When defeated, does not leave right away; it enters its next phase instead ([blue]{Amount}[/blue] left).'),
    # 句式照原版适者生存（ADAPTABLE_POWER）与接续（REATTACH_POWER）。
    'BlackSnakeBodyPower': (
        '黑蛇之躯', 'Body of the Black Snake',
        '当这个怪物将要被击败时，它会消失并释放[gold]不息恶火[/gold]，在[blue]3[/blue]回合后复活变得更加强大。',
        'When this creature would be defeated, it instead vanishes and casts [gold]Deathless Inferno[/gold], reviving even stronger in [blue]3[/blue] turns.',
        '当[gold]{OwnerName}[/gold]将要被击败时，它会消失并释放[gold]不息恶火[/gold]，在[blue]3[/blue]回合后复活变得更加强大。',
        'When [gold]{OwnerName}[/gold] would be defeated, it instead vanishes and casts [gold]Deathless Inferno[/gold], reviving even stronger in [blue]3[/blue] turns.'),
    # 隐藏的复活机制（不显示），文案只为防止缺键。
    'TalulahRebirthPower': (
        '重生',
        'Rebirth',
        '被击败时不会死亡，之后进入下一阶段。',
        'When defeated, does not leave right away; it enters its next phase instead.',
        '被击败时不会死亡，下回合进入下一阶段。',
        'When defeated, does not leave right away; it enters its next phase instead.'),
    'IgnitionPower': (
        '引燃', 'Ignition',
        '你打出的牌会被侵蚀为[gold]引燃[/gold]，每打出一次增加层数。',
        'Cards you play are afflicted with [gold]Ignited[/gold], gaining stacks each time they are played.',
        '你打出的牌会被侵蚀为[gold]引燃[/gold]，每打出一次增加[blue]{Amount}[/blue]层。',
        'Cards you play are afflicted with [gold]Ignited[/gold], gaining [blue]{Amount}[/blue] stacks each time they are played.'),
    'DominionPower': (
        '国度',
        'Dominion',
        '每当“皇帝的利刃”，追猎者的生命值第一次降低到50%（第二阶段：75%、50%、25%）及以下时，本回合内使你攻击牌的{Energy:energyIcons()}费用增加[blue]{CostIncrease}[/blue]。',
        'Whenever the Pursuer first falls to or below 50% HP (second phase: 75%, 50%, 25%), your Attacks cost [blue]{CostIncrease}[/blue] more this turn.',
        '每当[gold]{OwnerName}[/gold]的生命值第一次降低到[blue]{Threshold1}[/blue]及以下时，本回合内使你攻击牌的{Energy:energyIcons()}费用增加[blue]{CostIncrease}[/blue]。',
        'Whenever [gold]{OwnerName}[/gold] first falls to or below [blue]{Threshold1}[/blue] HP, your Attacks cost [blue]{CostIncrease}[/blue] more this turn.'),
    'DominionCostPower': (
        '压制',
        'Dominion Suppression',
        '本回合内，攻击牌的{Energy:energyIcons()}费用增加[blue]{Amount}[/blue]。',
        'Your Attacks cost [blue]{Amount}[/blue] more this turn.',
        '本回合内，攻击牌的{Energy:energyIcons()}费用增加[blue]{Amount}[/blue]。',
        'Your Attacks cost [blue]{Amount}[/blue] more this turn.'),
    'CollapsingDreadPower': (
        '坍缩恐惧',
        'Collapsing Dread',
        '你每打出1张牌或者结束你的回合时，层数减1。\n层数为0时，将要受到[blue]{Damage}[/blue]点伤害。',
        'Whenever you play a card or end your turn, reduce this by 1.\nWhen it reaches 0, take [blue]{Damage}[/blue] damage.',
        '你每打出[blue]1[/blue]张牌或者结束你的回合时，层数减[blue]1[/blue]。\n层数归零时，将要受到[blue]{Damage}[/blue]点伤害。',
        'Whenever you play a card or end your turn, reduce this by 1.\nWhen it reaches 0, take [blue]{Damage}[/blue] damage.'),
    'HarbingerFlightPower': (
        '飞行', 'Flight',
        '当{OwnerName}再失去[blue]{Amount}[/blue]点生命值后，失去飞行并被击晕1回合。',
        'After {OwnerName} loses another [blue]{Amount}[/blue] HP, it loses Flight and is Stunned for 1 turn.',
        '当{OwnerName}再失去[blue]{Amount}[/blue]点生命值后，失去飞行并被击晕1回合。',
        'After {OwnerName} loses another [blue]{Amount}[/blue] HP, it loses Flight and is Stunned for 1 turn.'),
    'ArtilleryMarkPower': (
        '着弹点', 'Artillery Mark',
        '下个敌方回合结束时，炮弹会对你造成[blue]{Amount}[/blue]点伤害；即使先兆者已被击败，炮弹也会落地。每次标定独立计时。',
        'At the end of the next enemy turn, a shell deals [blue]{Amount}[/blue] damage to you, even if the Harbinger has been defeated. Each mark has its own timer.',
        '下个敌方回合结束时，炮弹会对你造成[blue]{Amount}[/blue]点伤害；即使先兆者已被击败，炮弹也会落地。每次标定独立计时。',
        'At the end of the next enemy turn, a shell deals [blue]{Amount}[/blue] damage to you, even if the Harbinger has been defeated. Each mark has its own timer.'),
    'BattleHardenedPower': (
        '身经百战', 'Battle Hardened',
        '每回合受到的所有未被格挡伤害都延迟至自身回合结束时结算。',
        'All unblocked damage it takes is delayed until the end of its turn.',
        '[gold]{OwnerName}[/gold]受到的所有未被格挡伤害都延迟至其回合结束时结算。\n（将要受到[blue]{PendingDamage}[/blue]点伤害。）',
        'All unblocked damage [gold]{OwnerName}[/gold] takes is delayed until the end of its turn. Currently [blue]{PendingDamage}[/blue] damage is pending.'),
    'LastStandPower': (
        '背水', 'Last Stand',
        '生命第一次降到一半以下时，获得[gold]力量[/gold]。',
        'The first time its HP falls below half, gains [gold]Strength[/gold].',
        '生命第一次降到一半以下时，获得[blue]{Amount}[/blue]点[gold]力量[/gold]。',
        'The first time its HP falls below half, gains [blue]{Amount}[/blue] [gold]Strength[/gold].'),
    'WeaknessBreakPower': (
        '弱点破坏', 'Weakness Break',
        '每当[gold]帝国前锋精锐[/gold]的生命值第一次降低到最大生命值的[blue]2/3[/blue]及以下时，被[gold]击晕[/gold]并获得[blue]{Amount}[/blue]点[gold]活力[/gold]。',
        'Whenever the Imperial Vanguard\'s HP first falls to or below [blue]2/3[/blue] of its maximum HP, it is [gold]Stunned[/gold] and gains [blue]{Amount}[/blue] [gold]Vigor[/gold].',
        '每当[gold]{OwnerName}[/gold]的生命值第一次降低到[blue]{Threshold}[/blue]及以下时，被[gold]击晕[/gold]并获得[blue]{Amount}[/blue]点[gold]活力[/gold]。',
        'Whenever [gold]{OwnerName}[/gold]\'s HP first falls to or below [blue]{Threshold}[/blue], it is [gold]Stunned[/gold] and gains [blue]{Amount}[/blue] [gold]Vigor[/gold].'),
    'PermanentRegenPower': (
        '永续再生', 'Endless Regeneration',
        '回合结束时回复[blue]3[/blue]点生命。',
        'At the end of its turn, heals [blue]3[/blue] HP.',
        '[gold]{OwnerName}[/gold]在回合结束时回复[blue]{Amount}[/blue]点生命。',
        '[gold]{OwnerName}[/gold] heals [blue]{Amount}[/blue] HP at the end of its turn.'),
    'SiphonPower': (
        '汲取', 'Siphon',
        '[gold]汲取[/gold]造成的未被格挡伤害，按[blue]150%[/blue]治疗自己。',
        '[gold]Siphon[/gold] heals it for [blue]150%[/blue] of the unblocked damage it deals.',
        '[gold]汲取[/gold]造成的未被格挡伤害，按[blue]{Amount}%[/blue]治疗自己。',
        '[gold]Siphon[/gold] heals it for [blue]{Amount}%[/blue] of the unblocked damage it deals.'),
}


# 卡牌侵蚀，类名: (中文标题, 英文标题, 中文说明, 英文说明, 中文卡面附加文字, 英文卡面附加文字)
# 措辞照原版流电（GALVANIZED）：“打出这张牌时受到X点伤害。” / 卡面“受到X点伤害。”
AFFLICTIONS = {
    'Frost': (
        '霜冻', 'Frost',
        '这张牌被霜冻，无法被打出。\n解冻的方法，相信你应该知道……',
        'This card is frozen and cannot be played.\nYou should know how to thaw it...',
        '这张牌被霜冻，无法被打出。\n解冻的方法，相信你应该知道……',
        'This card is frozen and cannot be played.\nYou should know how to thaw it...'),
    'Hex': (
        '咒法', 'Hex',
        '每当你打出这张牌，随机丢弃[blue]{Amount}[/blue]张手牌。',
        'Whenever you play this card, discard [blue]{Amount}[/blue] random cards from your hand.',
        '随机丢弃{Amount}张手牌。',
        'Discard {Amount} random cards.'),
    'Trapped': (
        '被困', 'Trapped',
        '本回合不能打出[gold]攻击牌[/gold]。',
        'Cannot play [gold]Attacks[/gold] this turn.',
        '本回合不能被打出。',
        'Cannot be played this turn.'),
    'Ignited': (
        '引燃', 'Ignited',
        '打出这张牌时受到[blue]{Amount}[/blue]点伤害。',
        'Take [blue]{Amount}[/blue] damage when this card is played.',
        '受到{Amount}点伤害。',
        'Take {Amount} damage.'),
}


def write(lang: str, table: str, data: dict) -> None:
    path = ROOT / lang / f'{table}.json'
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + '\n', encoding='utf-8', newline='\n')


for i, lang in enumerate(('zhs', 'eng')):
    act = f'{PREFIX}_ACT_ROARING_FLARE_ACT'
    write(lang, 'acts', {f'{act}.title': ACT['title'][i], f'{act}.description': ACT['description'][i]})

    encounters = {}
    for cls, texts in ENCOUNTERS.items():
        key = f'{PREFIX}_ENCOUNTER_{slug(cls)}'
        encounters[f'{key}.title'] = texts[i]
        encounters[f'{key}.loss'] = texts[2 + i]
    write(lang, 'encounters', encounters)

    monsters = {}
    for cls, (names, moves) in MONSTERS.items():
        key = f'{PREFIX}_MONSTER_{slug(cls)}'
        monsters[f'{key}.name'] = names[i]
        for move, titles in moves.items():
            monsters[f'{key}.moves.{move}.title'] = titles[i]
    write(lang, 'monsters', monsters)

    powers = {}
    for cls, texts in POWERS.items():
        key = f'{PREFIX}_POWER_{slug(cls)}'
        powers[f'{key}.title'] = texts[i]
        powers[f'{key}.description'] = texts[2 + i]
        powers[f'{key}.smartDescription'] = texts[4 + i]
    powers[f'{PREFIX}_POWER_DOMINION_POWER.secondFormDescription'] = (
        '每当[gold]{OwnerName}[/gold]的生命值第一次降低到[blue]{Threshold1}[/blue]、[blue]{Threshold2}[/blue]、[blue]{Threshold3}[/blue]及以下时，本回合内使你攻击牌的费用增加[blue]{CostIncrease}[/blue]。',
        'Whenever [gold]{OwnerName}[/gold] first falls to or below [blue]{Threshold1}[/blue], [blue]{Threshold2}[/blue], or [blue]{Threshold3}[/blue] HP, your Attacks cost [blue]{CostIncrease}[/blue] more this turn.'
    )[i]
    write(lang, 'powers', powers)

    afflictions = {}
    for cls, texts in AFFLICTIONS.items():
        key = f'{PREFIX}_AFFLICTION_{slug(cls)}'
        afflictions[f'{key}.title'] = texts[i]
        afflictions[f'{key}.description'] = texts[2 + i]
        afflictions[f'{key}.extraCardText'] = texts[4 + i]
    write(lang, 'afflictions', afflictions)

print('written', sorted(p.name for p in (ROOT / 'zhs').iterdir()))
