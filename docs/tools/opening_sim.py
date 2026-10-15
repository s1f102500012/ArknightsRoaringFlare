"""怒号光明遭遇战的开局伤害，与原版荣耀（vanilla_glory.py）同一口径对照（引擎与口径见 sim_core.py）。

用法：
  python3 opening_sim.py          逐场列出 A0 / A10 的期望与最大值，并和原版荣耀同类型的均值对照
  python3 opening_sim.py --search 为每场重新挑起手位置（按下面的 TARGET 打分），改阵容或数值后用

招式与数值同步 docs/设计文档.md 与实现；玩家不格挡、不攻击、不击杀敌人，只统计攻击伤害。
起手写在 OPENINGS 里，必须与遭遇战的 SetOpenings 或固定循环第一招一致（值 = 招式序号，从 0 开始）。
随机出招的怪第一招取 moves[偏移]，之后在其余招里等权随机、不连用（同苦难摇篮的 RandomMachine）。
只算攻击伤害：烈焚灼息（每回合结束扣血）、炮击标记延迟落地、国度这类伤害或削弱在 docs/战斗设计.md 里单独估算。
"""
import sys
from dataclasses import replace
from itertools import product

from sim_core import V, Move, Rand, cycle, random_kind, simulate, summarize
import vanilla_glory

A = Move  # 简写


def fixed(*moves):
    return ("cycle", moves)


def rand(*moves):
    return ("random", moves)


# 招式：(招式名, Move)。数值 (A0, A10)。
PATROL_MOVES = (("抛网", A()), ("抽打", A(V(10, 11), 1)),
                ("装填射击", A(V(3, 4), 4, self_str=V(1))), ("武装", A(V(6, 7), 1)))
SENIOR_DOZE_ROUNDS = 3
INTELLIGENCE_PER_OFFICER = V(1)
M = {
    # —— 纠察队 ——
    "纠察官": rand(*PATROL_MOVES),
    # 先遣与精锐巡逻队共用这两种循环，招式索引都按文档的 1/2/3/4。
    "先遣纠察官（左）": fixed(*PATROL_MOVES),
    "先遣纠察官（右）": fixed(*PATROL_MOVES),
    "高级纠察官": fixed(("打盹", A()), ("激怒", A(V(14, 16), 1)),
                          ("押运", A()), ("干扰", A(V(4, 6), 1))),
    # —— 裂兽 ——
    "裂兽": fixed(("撕咬", A(V(6, 8), 1)), ("警戒", A(self_str=V(2))), ("扑击", A(V(6, 7), 2))),
    "裂兽族母": fixed(("召集", A(summon="裂兽", summon_vigor=V(3, 5))), ("治愈", A()),
                        ("鼓舞", A(team_str=V(2))), ("震慑", A(V(13, 15), 1))),
    # —— 远程与术师 ——
    "突袭弩手": fixed(("速射", A(V(3, 4), 5)), ("冲击", A(V(11, 12), 1)), ("上弦", A(self_str=V(1)))),
    "着铠术师": fixed(("咒法", A()), ("火焰", A()), ("仪式准备", A()), ("仪式", A(V(5, 6), 2))),
    "高级着铠术师": fixed(("源石法术", A()), ("法术连击", A(V(3, 4), 5, self_str=V(1))),
                            ("仪式准备", A()), ("仪式", A(V(19, 20), 1))),
    # —— 重装与炮兵 ——
    "突击者": fixed(("重劈", A(V(14, 15), 1)), ("稳阵", A())),
    "突击者队长": fixed(("压阵", A(vuln=99)), ("冲锋", A(V(15, 17), 1)),
                          ("撤退", A(V(21, 23), 1, summon=("突击者", "着铠术师", "突袭弩手"), escape=True))),
    # 玩家不攻击，飞行不会消失。标定的独立延迟伤害、干扰塞牌另计；炮击正常结算攻击。
    "炮火先兆者": fixed(("标定", A()), ("干扰", A()), ("炮击", A(V(10, 11), 1)), ("装填", A(self_str=V(10)))),
    "炮火中枢先兆者": fixed(("标定", A()), ("维护", A(self_vigor=V(5))),
                              ("呼叫", A(V(11, 12), 1, summon="炮火先兆者"))),
    # 玩家不出手，弱点破坏与突刺不触发；精锐从远射起按 2→1 交替。百战精锐的激怒也不触发。
    "帝国前锋精锐": fixed(("战意", A(self_str=V(1, 2))), ("远射", A(V(3, 4), 3))),
    "帝国前锋百战精锐": fixed(("突刺", A(V(16, 18), 1)), ("远射", A(V(9, 10), 2)),
                             ("战意", A(vuln=3)), ("弹反", A(V(14, 15), 1))),
    # —— 宿主（第八章新单位，不与苦难摇篮重复） ——
    "萨卡兹宿主百夫长": fixed(("汲取", A(V(12, 13), 1)), ("群噬", A(V(6, 7), 2)), ("吟唱", A(self_str=V(2, 3)))),
    "宿主重装士兵": fixed(("盾击", A(V(14, 15), 1)), ("坚守", A()), ("重压", A(V(10, 11), 1))),
    # —— 精英：皇帝的利刃，追猎者（一形态；二形态在第一次倒下之后，开局 4 回合内一般打不到） ——
    "追猎者": fixed(("巡猎", A(V(22, 24), 1)), ("斩击", A(V(8, 9), 2)), ("坍缩恐惧", A())),
    "追猎者（二阶段）": fixed(("连斩", A(V(4, 5), 4)), ("强化", A(self_str=V(1))), ("坍缩恐惧", A())),
    # —— Boss：“不死的黑蛇”第一条命。点燃只在开局放一次（给玩家挂引燃），之后噬咬 → 焦土 → 引爆循环（见下方 KINDS 改写）；
    #      焦土另往手牌塞 2（3）张灼伤（每张留在手里的回合末受 2 点）、引燃让玩家打出的牌受伤，都取决于玩家怎么出牌，这里不计 ——
    #      点燃不攻击（开局意图只有侵蚀卡牌，用户 2026-10-09 要求）。
    "塔露拉": fixed(("点燃", A()), ("噬咬", A(V(11, 12), 2)), ("焦土", A(V(14, 15), 1)), ("引爆", A(V(18, 20), 1))),
}


def build_kinds() -> dict:
    return {name: (random_kind if mode == "random" else cycle)(list(moves)) for name, (mode, moves) in M.items()}


KINDS = build_kinds()
KINDS["着铠术师"].next["仪式"] = "仪式准备"
KINDS["高级着铠术师"].next["仪式"] = "法术连击"
KINDS["突击者队长"].summon_cap = 3
KINDS["突击者队长"].summon_front = True
KINDS["先遣纠察官（右）"].next = cycle([PATROL_MOVES[i] for i in (2, 1, 3, 0)]).next
KINDS["塔露拉"].next["引爆"] = "噬咬"  # 点燃只在开局


def gather_intelligence(mon, world, asc):
    officers = ("纠察官", "先遣纠察官（左）", "先遣纠察官（右）")
    living = [m.order for m in world if m.kind in officers]
    contributions = {source: amount for source, amount in mon.intelligence_contributions if source in living}
    for source in living:
        contributions[source] = contributions.get(source, 0) + INTELLIGENCE_PER_OFFICER[asc]
    strength = len(living) * INTELLIGENCE_PER_OFFICER[asc]
    return replace(mon, strength=mon.strength + strength, temporary_strength=sum(contributions.values()),
                   intelligence_contributions=tuple(sorted(contributions.items())))


KINDS["高级纠察官"].next = {
    "打盹": lambda mon, world: "打盹" if len(mon.history) < SENIOR_DOZE_ROUNDS else "激怒",
    "激怒": "干扰", "干扰": "押运", "押运": "激怒",
}
KINDS["高级纠察官"].end_turn = gather_intelligence


def matriarch_next(mon, world):
    count = sum(m.kind == "裂兽" for m in world)
    summon = (80, 50, 20, 0)[min(count, 3)]
    return Rand((("召集", summon, 0), ("治愈", (100 - summon) * 0.4, 0),
                 ("鼓舞", (100 - summon) * 0.3, 0), ("震慑", (100 - summon) * 0.3, 0)))


KINDS["裂兽族母"].next = {name: matriarch_next for name in KINDS["裂兽族母"].moves}
KINDS["裂兽族母"].summon_cap = 3
KINDS["裂兽族母"].summon_front = True
KINDS["裂兽族母"].summon_start = lambda world, kind: Rand((("撕咬", 1, 0), ("扑击", 1, 0)))


def core_harbinger_next(mon, world):
    count = sum(m.kind == "炮火先兆者" for m in world)
    if count == 0:
        return "呼叫"  # 无小怪时优先补充，允许连续呼叫。
    return Rand((("标定", 1, 1), ("维护", 1, 1), ("呼叫", 2 if count < 2 else 0, 1)))


def harbinger_next(mon, world):
    history = mon.history
    last_reload = max((i for i, name in enumerate(history) if name == "装填"), default=-1)
    if {"标定", "干扰", "炮击"}.issubset(history[last_reload + 1:]):
        return "装填"
    return Rand(tuple((name, 1, 1) for name in ("标定", "干扰", "炮击")))


KINDS["炮火中枢先兆者"].next = {name: core_harbinger_next for name in KINDS["炮火中枢先兆者"].moves}
KINDS["炮火中枢先兆者"].summon_cap = 2
KINDS["炮火中枢先兆者"].summon_front = True
KINDS["炮火中枢先兆者"].summon_start = lambda world, kind: Rand(tuple((name, 1, 0) for name in ("标定", "干扰", "炮击")))
KINDS["炮火先兆者"].next = {name: harbinger_next for name in KINDS["炮火先兆者"].moves}

# 只统计 ModEntry.ActEncounterTypes 中的当前战斗池，旧遭遇不计入类型均值。
# 阵容 = 种类列表，或种类列表的列表（随机阵容，等概率）。站位从左到右即出手顺序。
ENCOUNTERS = {
    "弱|纠察巡逻队（先遣）": ["先遣纠察官（左）", "先遣纠察官（右）"],
    "弱|裂兽": ["裂兽", "裂兽", "裂兽"],
    "普|纠察巡逻队（精锐）": ["先遣纠察官（左）", "先遣纠察官（右）", "高级纠察官"],
    "普|裂兽群": ["裂兽族母"],
    "普|高级术士": ["高级着铠术师"],
    "弱|沉默的屠夫": ["帝国前锋精锐"],
    "精|突击者队长": ["突击者队长"],
    "精|皇帝的利刃": ["追猎者"],
    "普|百战精锐": ["帝国前锋百战精锐"],
    "首|塔露拉": ["塔露拉"],
    "普|牵引的炮火": ["炮火中枢先兆者"],
}
# 每场的起手偏移：固定阵容按站位；随机阵容按种类（{种类: 偏移}，未列出的为 0）。
OPENINGS = {key: tuple(0 for _ in (v if not isinstance(v[0], list) else v[0])) for key, v in ENCOUNTERS.items()}
OPENINGS.update({
    "弱|纠察巡逻队（先遣）": (3, 2), "弱|裂兽": (0, 1, 2), "弱|沉默的屠夫": (1,),
    "普|纠察巡逻队（精锐）": (3, 2, 0), "普|裂兽群": (0,), "普|高级术士": (0,), "普|百战精锐": (0,),
    "精|突击者队长": (0,), "精|皇帝的利刃": (0,),
})
# 固定起手（不参与搜索）：中枢先标定；召唤的小先兆者在前三招中随机；塔露拉开场先点燃。
FIXED = {"炮火中枢先兆者": [0], "塔露拉": [0], "裂兽族母": [0], "高级纠察官": [0], "帝国前锋精锐": [1], "帝国前锋百战精锐": [0], "突击者队长": [0], "着铠术师": [0], "高级着铠术师": [0]}


def lineups_of(key):
    v = ENCOUNTERS[key]
    return v if isinstance(v[0], list) else [v]


def start_state(kind, offset):
    moves = M[kind][1]
    return moves[offset % len(moves)][0]


def run(key, openings, asc):
    dists = []
    for lu in lineups_of(key):
        offs = [openings.get(k, 0) for k in lu] if isinstance(openings, dict) else openings
        dists.append(simulate(KINDS, [(k, start_state(k, o)) for k, o in zip(lu, offs)], asc))
    return summarize(dists)


# 按原版荣耀同类型遭遇战 A0 的期望值定目标（vanilla_glory.py）：
#   原版 A0 第 1 回合 / 4 回合总伤：弱 15.0 / 84.3、普 9.9 / 82.2、精 25.0 / 98.4、首 22.7 / 83.0。
#   弱怪刻意压在原版之下（苦难摇篮的教训：弱怪战是一幕里最先遇到的，难度主要来自机制而不是出伤）。
#   first：第 1 回合期望上限；total：前 4 回合总伤期望的目标；peak：任一回合最大值上限。
TARGET = {
    "弱": {"first": 16, "total": 70, "peak": 30},
    "普": {"first": 10, "total": 80, "peak": 38},
    "精": {"first": 26, "total": 95, "peak": 42},
    "首": {"first": 15, "total": 80, "peak": 40},
}


def score(kind, s):
    t = TARGET[kind]
    over_first = max(0.0, s["mean"][0] - t["first"])
    over_peak = sum(max(0, x - t["peak"]) for x in s["max"])
    return over_first * 3 + over_peak * 2 + abs(s["total_mean"] - t["total"])


def search(key):
    kind = key.split("|")[0]
    if key in ("弱|纠察巡逻队（先遣）", "普|纠察巡逻队（精锐）", "弱|裂兽", "普|裂兽群"):
        openings = OPENINGS[key]
        s = run(key, openings, 0)
        return ((round(score(kind, s), 1), sum(1 for c in openings if c)), openings, s)
    lus = lineups_of(key)
    randomized = len(lus) > 1
    names = sorted({n for lu in lus for n in lu}) if randomized else lus[0]
    best = None
    for combo in product(*(FIXED.get(n) or range(len(M[n][1])) for n in names)):
        if not randomized and any(names[a] == names[b] and combo[a] == combo[b]
                                  for a in range(len(names)) for b in range(a + 1, len(names))):
            continue  # 同一场里的同种怪起手必须错开
        openings = dict(zip(names, combo)) if randomized else combo
        s = run(key, openings, 0)
        sc = (round(score(kind, s), 1), sum(1 for c in combo if c))
        if best is None or sc < best[0]:
            best = (sc, openings, s)
    return best


def fmt(xs):
    return "/".join(f"{x:.1f}".rstrip("0").rstrip(".") for x in xs)


def describe(key, openings):
    lu = lineups_of(key)[0]
    return " ".join(f"{k}:{start_state(k, o)}" for k, o in zip(lu, openings))


def report():
    vanilla = {0: vanilla_glory.run(0), 1: vanilla_glory.run(1)}
    for asc, label in ((0, "A0"), (1, "A10")):
        print(f"== 怒号光明 {label}（前 4 个敌方回合：期望 | 最大 | 总伤期望/最大）")
        groups: dict = {}
        for key in ENCOUNTERS:
            kind, title = key.split("|")
            s = run(key, OPENINGS[key], asc)
            groups.setdefault(kind, []).append(s)
            print(f"{kind} {title:8s} 期望 {fmt(s['mean']):24s} 最大 {fmt(s['max']):14s} 总 {s['total_mean']:5.1f}/{s['total_max']}")
        print(f"-- 类型均值（怒号光明 vs 原版荣耀 {label}）：第 1 回合期望 / 总伤期望")
        for kind, ss in groups.items():
            vs = [s for (k, _), s in vanilla[asc].items() if k == kind]
            ours = (sum(s["mean"][0] for s in ss) / len(ss), sum(s["total_mean"] for s in ss) / len(ss))
            base = (sum(s["mean"][0] for s in vs) / len(vs), sum(s["total_mean"] for s in vs) / len(vs))
            print(f"{kind}  {ours[0]:5.1f} / {ours[1]:5.1f}    原版 {base[0]:5.1f} / {base[1]:5.1f}")


if __name__ == "__main__":
    if "--search" in sys.argv:
        for key in ENCOUNTERS:
            sc, openings, s = search(key)
            print(f"{key:14s} {openings}  分 {sc[0]}  期望 {fmt(s['mean'])} 总 {s['total_mean']:.1f}  {describe(key, openings)}")
    else:
        report()
