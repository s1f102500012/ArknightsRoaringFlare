"""生成 17 个 PRTS 敌人的 creature_visuals 场景（assets/scenes/creature_visuals/<场景名>.tscn）。

  python3 tools/write_creature_scenes.py

几何参数都在 SCENES 表里，改完重跑即可。约定照姊妹模组 ArknightsChernobog：
- PRTS 骨骼面朝右，敌人要面朝左，所以 SpineSprite（Visuals）X 缩放取负；只翻模型，不翻整个生物节点（会连带翻转命中框/UI）。
- 缩放：默认 UNIFORM_SCALE，个别单位按 SCENE_SCALE_OVERRIDES 调整；表里的缩放列只是量 Bounds 时的基准。
- Visuals.position.x 把身体中线挪到 x=0；先兆者是飞行单位，骨架原点贴着机身底，整体上抬（position.y 为负）。
- Bounds 以 x=0 居中、底边在 y=0（地面）。高取 Idle 身体顶边（tools/measure_idle_bounds.gd 量的包围盒 +
  tools/body_extent.py 排除拖在身后的剑/披风/法杖），宽取身体宽 + 余量；包围盒含武器与特效，偏宽，不直接用。
- 标记点由 Bounds 推出：IntentPos=(0, -(h+45))，CenterPos=(0, -h/2)，OrbPos=(0, -0.7h)，TalkPos=(w/4, -0.85h)。
"""
from pathlib import Path

MOD = Path(__file__).resolve().parent.parent
OUT = MOD / "assets" / "scenes" / "creature_visuals"

# 场景名: (节点名, 资源目录, 文件名, 待机动画, 缩放, Visuals.x, Visuals.y, Bounds 宽, Bounds 高)
SCENES = {
    "black_snake": ("BlackSnake", "bsnake", "enemy_1515_bsnake", "Idle_A", 1.1, -1, 0, 250, 405),
    "emperors_blade_hunter": ("EmperorsBladeHunter", "empgrd", "enemy_1520_empgrd", "Idle", 1.0, -18, 0, 250, 450),
    "infected_patrol_officer": ("InfectedPatrolOfficer", "uoffcr", "enemy_1107_uoffcr", "Idle", 0.75, -5, 0, 155, 265),
    "senior_patrol_officer": ("SeniorPatrolOfficer", "uoffcr_2", "enemy_1107_uoffcr_2", "Idle", 0.825, -5.5, 0, 170, 292),
    "ursus_ravager": ("UrsusRavager", "uterer", "enemy_1108_uterer", "Idle", 0.75, 16, 0, 225, 205),
    "ravager_matriarch": ("RavagerMatriarch", "uterer_2", "enemy_1108_uterer_2", "Idle", 0.825, 17.6, 0, 248, 226),
    "ursus_crossbowman": ("UrsusCrossbowman", "uabone", "enemy_1109_uabone", "Idle", 0.75, -3, 0, 200, 260),
    "ursus_armored_caster": ("UrsusArmoredCaster", "uamord", "enemy_1110_uamord", "Idle", 0.75, 9, 0, 175, 260),
    "senior_armored_caster": ("SeniorArmoredCaster", "uamord_2", "enemy_1110_uamord_2", "Idle", 0.825, 9.9, 0, 193, 286),
    # 突击者/队长的待机动画在 PRTS 原文件里就拼作 Idile。
    "ursus_commando": ("UrsusCommando", "ucommd", "enemy_1111_ucommd", "Idile", 0.862, 19, 0, 190, 335),
    "commando_captain": ("CommandoCaptain", "ucommd_2", "enemy_1111_ucommd_2", "Idile", 0.948, 20.9, 0, 209, 368),
    "artillery_harbinger": ("ArtilleryHarbinger", "emppnt", "enemy_1112_emppnt", "Idle", 0.862, 0, -60, 340, 265),
    "core_harbinger": ("CoreHarbinger", "emppnt_2", "enemy_1112_emppnt_2", "Idle", 0.948, 0, -66, 374, 292),
    "imperial_vanguard": ("ImperialVanguard", "empace", "enemy_1113_empace", "Idle", 0.862, 0, 0, 230, 330),
    "vanguard_veteran": ("VanguardVeteran", "empace_2", "enemy_1113_empace_2", "Idle", 0.948, 0, 0, 253, 363),
    "host_heavy_soldier": ("HostHeavySoldier", "zomshd", "enemy_1061_zomshd", "Idle", 0.862, -26, 0, 256, 375),
    "sarkaz_host_centurion": ("SarkazHostCenturion", "rgrdmn", "enemy_1114_rgrdmn", "Idle", 0.862, -14, 0, 230, 335),
}

# 统一缩放（照明日方舟原作的相对大小）：原作所有敌人用同一套比例渲染（PRTS 的 SPINEDATA 只有骨骼文件名、没有逐个敌人的缩放），
# 相对大小就是骨骼本身的大小——黑蛇骨骼和纠察官差不多高（塔露拉在原作里是常人体型），无人机比士兵宽一倍。
# 默认单位乘到 UNIFORM_SCALE，不按普通/精英/组长/Boss 分档放大（之前的分档把黑蛇放大成纠察官的 1.5 倍以上、
# 无人机比士兵多放大 15%，打乱了原作比例）。上表的缩放是当初量 Bounds 时用的，偏移与 Bounds 按 UNIFORM_SCALE/表内缩放 同比换算。
# 0.75 是原来普通兵的缩放，纠察官约 263 高（原版邪教徒 250、红宝石劫匪 227–332）。量法与对照表见 docs/spine_import.md「尺寸」。
UNIFORM_SCALE = 0.75
HARBINGER_SCALE = UNIFORM_SCALE * 0.8
SCENE_SCALE_OVERRIDES = {"artillery_harbinger": HARBINGER_SCALE * 0.8, "core_harbinger": HARBINGER_SCALE}

TEMPLATE = """[gd_scene load_steps=3 format=3]

[ext_resource type="Script" path="res://src/Core/Nodes/Combat/NCreatureVisuals.cs" id="1_script"]
[ext_resource type="SpineSkeletonDataResource" path="res://ArknightsRoaringFlare/animations/monsters/arknights/{dir}/{file}_skel_data.tres" id="2_skel_data"]

[node name="{node}CreatureVisuals" type="Node2D"]
script = ExtResource("1_script")

[node name="Visuals" type="SpineSprite" parent="." owner="."]
unique_name_in_owner = true
skeleton_data_res = ExtResource("2_skel_data")
preview_skin = "default"
preview_animation = "{idle}"
position = Vector2({vx:g}, {vy:g})
scale = Vector2(-{scale:g}, {scale:g})

[node name="Bounds" type="Control" parent="." owner="."]
unique_name_in_owner = true
position = Vector2({bx}, {by})
size = Vector2({w}, {h})

[node name="IntentPos" type="Marker2D" parent="." owner="."]
unique_name_in_owner = true
position = Vector2(0, {intent})

[node name="CenterPos" type="Marker2D" parent="." owner="."]
unique_name_in_owner = true
position = Vector2(0, {center})

[node name="OrbPos" type="Marker2D" parent="." owner="."]
unique_name_in_owner = true
position = Vector2(0, {orb})

[node name="TalkPos" type="Marker2D" parent="." owner="."]
unique_name_in_owner = true
position = Vector2({tx}, {ty})
"""


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    for scene, (node, asset_dir, file_name, idle, scale, vx, vy, w, h) in SCENES.items():
        target_scale = SCENE_SCALE_OVERRIDES.get(scene, UNIFORM_SCALE)
        k = target_scale / scale
        scale, vx, vy = target_scale, round(vx * k, 1), round(vy * k, 1)
        w, h = round(w * k), round(h * k)
        text = TEMPLATE.format(
            dir=asset_dir, file=file_name, node=node, idle=idle, scale=scale, vx=vx, vy=vy,
            bx=-round(w / 2), by=-h, w=w, h=h,
            intent=-(h + 45), center=-round(h / 2), orb=-round(h * 0.7),
            tx=round(w / 4), ty=-round(h * 0.85),
        )
        (OUT / f"{scene}.tscn").write_text(text, encoding="utf-8", newline="\n")
        print(f"wrote {scene}.tscn")


if __name__ == "__main__":
    main()
