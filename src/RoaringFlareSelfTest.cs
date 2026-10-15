using ArknightsRoaringFlare.Acts;
using ArknightsRoaringFlare.Encounters;
using ArknightsRoaringFlare.Monsters;
using ArknightsRoaringFlare.Powers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Bindings.MegaSpine;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Acts;
using MegaCrit.Sts2.Core.MonsterMoves;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Unlocks;

namespace ArknightsRoaringFlare;

/// <summary>
/// headless 自检：设置环境变量 ROARINGFLARE_SELFTEST=1 启动游戏时，在 ModelDb.Init 之后走一遍选幕、生成房间、
/// 战斗池与起手、A10 双 Boss 补位、Boss 与精英的阶段链、敌人模型与 Spine 场景，结果以 [ArknightsRoaringFlare][SelfTest] 前缀写日志。
/// 这些路径平时要到开局后才会触发，headless 加载验证覆盖不到。实机的表现（站位、缩放、动作时机）仍要进游戏看。
/// </summary>
internal static class RoaringFlareSelfTest
{
	private const string Prefix = $"[{ModEntry.ModId}][SelfTest]";
	private static int _failures;

	public static void InstallIfRequested(Harmony harmony)
	{
		if (System.Environment.GetEnvironmentVariable("ROARINGFLARE_SELFTEST") != "1")
		{
			return;
		}

		harmony.Patch(
			AccessTools.Method(typeof(ModelDb), nameof(ModelDb.Init)),
			postfix: new HarmonyMethod(typeof(RoaringFlareSelfTest), nameof(Run)));
		Log.Info($"{Prefix} Armed; will run after ModelDb.Init.");
	}

	private static void Run()
	{
		_failures = 0;
		RoaringFlareAct canonical = ModelDb.Act<RoaringFlareAct>();

		Check("ritsulib registration", () =>
		{
			const string prefix = "ARKNIGHTS_ROARING_FLARE_";
			List<(Type Type, string Entry)> registered =
			[
				(typeof(RoaringFlareAct), canonical.Id.Entry),
				.. ModEntry.ActEncounterTypes.Select(t => (t, ModelDb.GetId(t).Entry)),
				.. ModEntry.ModelTypes<FlareMonster>().Select(t => (t, ModelDb.GetId(t).Entry)),
				.. ModEntry.ModelTypes<PowerModel>().Select(t => (t, ModelDb.GetId(t).Entry)),
			];
			foreach ((Type type, string entry) in registered)
			{
				Require(entry.StartsWith(prefix), $"{type.Name} not registered through RitsuLib: {entry}");
				Log.Info($"[{ModEntry.ModId}][IdMap] {StringHelper.Slugify(type.Name)} {entry}");
			}

			return $"{registered.Count} models; act {canonical.Id.Entry}";
		});

		Check("act index group", () =>
		{
			// RitsuLib 按 AllowInRandomActList 过滤这一组：没装 ActLikeIt2 时本幕与荣耀同组，装了时退出（只经选幕界面出现）。
			IReadOnlyList<ActModel> group = ModelDb.ActsByIndex[RoaringFlareAct.ActIndex];
			string names = string.Join(", ", group.Select(a => $"{a.Id.Entry}(default={a.IsDefault})"));
			bool expectInGroup = !Integration.ActLikeIt2Bridge.IsLoaded;
			Require(group.Contains(canonical) == expectInGroup && group[0] is Glory, $"expect in group={expectInGroup}: {names}");
			return $"ActLikeIt2={!expectInGroup}: {names}";
		});

		Check("default list unchanged", () =>
		{
			string names = string.Join(", ", ActModel.GetDefaultList().Select(a => a.Id.Entry));
			Require(!names.Contains(canonical.Id.Entry), names);
			return names;
		});

		Check("random list rolls act", () =>
		{
			int hits = 0;
			const int rolls = 200;
			for (ulong seed = 1; seed <= rolls; seed++)
			{
				List<ActModel> acts = ActModel.GetRandomList(new Rng(seed), UnlockState.all, isMultiplayer: true).ToList();
				Require(acts.Count == 3, $"seed {seed} gave {acts.Count} acts");
				if (acts[RoaringFlareAct.ActIndex] == canonical)
				{
					hits++;
				}
			}

			bool viaFork = Integration.ActLikeIt2Bridge.IsLoaded;
			Require(viaFork ? hits == 0 : hits > 0 && hits < rolls, $"ActLikeIt2={viaFork} hits={hits}");
			return $"ActLikeIt2={viaFork}: act 3 = {canonical.Id.Entry} in {hits}/{rolls} multiplayer rolls";
		});

		Check("ActLikeIt2 fork registration", () =>
		{
			if (!Integration.ActLikeIt2Bridge.IsLoaded)
			{
				return "ActLikeIt2 not loaded (optional); skipped";
			}

			string? description = Integration.ActLikeIt2Bridge.RegisteredOptionDescription();
			Require(description != null, $"{canonical.Id.Entry} not registered for act {Integration.ActLikeIt2Bridge.ActNumber}");
			Require(description!.Length > 0 && !description.Contains(".description"), $"description '{description}'");
			return $"act {Integration.ActLikeIt2Bridge.ActNumber}: {description}";
		});

		Check("generate rooms", () =>
		{
			ActModel act = canonical.ToMutable();
			act.GenerateRooms(new Rng(42), UnlockState.all, isMultiplayer: false);
			Require(act.BossEncounter is TalulahBoss, $"boss {act.BossEncounter?.Id.Entry}");
			Require(!act.HasSecondBoss, "unexpected second boss before the double-boss patch");
			return $"boss={act.BossEncounter!.Id.Entry}, ancient={act.Ancient?.Id.Entry}, rooms={act.GetNumberOfRooms(false)}";
		});

		Check("A10 double boss", () =>
		{
			// 本幕只有塔露拉一个 Boss，原版补第二 Boss 会落空；补丁从荣耀 Boss 里抽一个先打，塔露拉放到最后。
			ActModel act = canonical.ToMutable();
			act.GenerateRooms(new Rng(42), UnlockState.all, isMultiplayer: false);
			Require(Integration.DoubleBossPatch.Apply(act, new Rng(7)), "patch did not apply");
			Require(act.SecondBossEncounter is TalulahBoss, $"second boss {act.SecondBossEncounter?.Id.Entry}");
			Require(ModelDb.Act<Glory>().AllBossEncounters.Contains(act.BossEncounter), $"first boss {act.BossEncounter.Id.Entry}");
			Require(!Integration.DoubleBossPatch.Apply(act, new Rng(8)), "patch applied twice");
			// 荣耀自己不受影响。
			ActModel glory = ModelDb.Act<Glory>().ToMutable();
			glory.GenerateRooms(new Rng(42), UnlockState.all, isMultiplayer: false);
			Require(!Integration.DoubleBossPatch.Apply(glory, new Rng(7)), "patched glory");
			return $"{act.BossEncounter.Id.Entry} then {act.SecondBossEncounter!.Id.Entry}";
		});

		Check("act encounter pools", () =>
		{
			int weak = canonical.AllWeakEncounters.Count();
			int regular = canonical.AllRegularEncounters.Count();
			int elite = canonical.AllEliteEncounters.Count();
			int boss = canonical.AllBossEncounters.Count();
			Require(weak == 3 && regular == 5 && elite == 2 && boss == 1, $"weak={weak} regular={regular} elite={elite} boss={boss}");
			Require(canonical.AllBossEncounters.Single() is TalulahBoss, canonical.AllBossEncounters.Single().Id.Entry);
			Require(canonical.BossDiscoveryOrder.SequenceEqual(canonical.AllBossEncounters), "boss discovery order");
			return $"weak={weak} regular={regular} elite={elite} boss={canonical.AllBossEncounters.Single().Id.Entry}";
		});

		foreach (EncounterModel encounter in canonical.AllEncounters)
		{
			Check($"act encounter {encounter.Id.Entry}", () =>
			{
				// 每场的起手招应与 docs/tools/opening_sim.py 的 OPENINGS 一致；同种怪起手必须错开。
				Require(encounter is FlareEncounter, "not a FlareEncounter");
				Require(encounter.Title.Exists(), "missing encounters loc title");
				string kind = encounter.RoomType switch { RoomType.Boss => "boss", RoomType.Elite => "elite", _ => encounter.IsWeak ? "weak" : "normal" };
				IReadOnlyList<(MonsterModel Monster, string? Slot)> lineup = ((FlareEncounter)encounter).CreateMonstersWithSlots(new Rng(1));
				IReadOnlyList<MonsterModel> monsters = lineup.Select(entry => entry.Monster).ToList();
				List<string> openings = [];
				foreach (FlareMonster monster in monsters.Cast<FlareMonster>())
				{
					monster.SetUpForCombat();
					openings.Add($"{monster.Id.Entry}:{monster.OpeningStateId}");
				}

				Require(openings.Count == openings.Distinct().Count(), $"duplicate openings: {string.Join(" + ", openings)}");
				string tags = string.Join(",", encounter.Tags.Select(t => (int)t));
				string slots = CheckEncounterSlots((FlareEncounter)encounter, lineup);
				return $"{kind} tags=[{tags}] hp={monsters.Sum(m => m.MinInitialHp)}-{monsters.Sum(m => m.MaxInitialHp)}: {string.Join(" + ", openings)}{slots}";
			});
		}

		Check("weak/normal escalation tags", () =>
		{
			(EncounterModel Weak, EncounterModel Normal)[] pairs =
			[
				(ModelDb.Encounter<PatrolWeak>(), ModelDb.Encounter<HuskVillageSearchNormal>()),
				(ModelDb.Encounter<RavagersWeak>(), ModelDb.Encounter<RavagerPackNormal>()),
			];
			foreach ((EncounterModel weak, EncounterModel normal) in pairs)
			{
				Require(weak.SharesTagsWith(normal), $"{weak.Id.Entry} / {normal.Id.Entry} share no tag");
			}

			Require(!ModelDb.Encounter<PatrolWeak>().SharesTagsWith(ModelDb.Encounter<RavagerPackNormal>()), "unrelated encounters share a tag");
			return $"{pairs.Length} pairs";
		});

		Check("combat background", () =>
		{
			// 经 RitsuLib 的 ActAssetProfile 从模组目录挑图层；任何一条路径落回原版命名空间都说明接线失效。
			BackgroundAssets assets = canonical.GenerateBackgroundAssets(new Rng(7));
			Require(assets.BgLayers.Count > 0, "no bg layers");
			foreach (string path in assets.AssetPaths)
			{
				Require(path.StartsWith(RoaringFlareAssets.Root), $"not in mod namespace: {path}");
				Require(ResourceLoader.Load<PackedScene>(path) != null, $"cannot load {path}");
			}

			Require(ResourceLoader.Exists(canonical.ChestSpineResourcePath), canonical.ChestSpineResourcePath);
			return $"{assets.BgLayers.Count} layers, fg={assets.FgLayer}";
		});

		Check("rest site", () =>
		{
			Require(canonical.RestSiteBackgroundPath.StartsWith(RoaringFlareAssets.Root), canonical.RestSiteBackgroundPath);
			Node root = ResourceLoader.Load<PackedScene>(canonical.RestSiteBackgroundPath).Instantiate();
			string rootType = root.GetType().Name;
			bool isControl = root is Control;
			root.Free();
			Require(isControl, rootType);
			return $"{canonical.RestSiteBackgroundPath} -> {rootType}";
		});

		Check("map backgrounds match MapBgColor", () =>
		{
			// 节点着色器和描边光晕都用 MapBgColor，它与纸面平均色不一致时每个节点外面会多一圈色晕。三段逐段核对。
			List<string> report = [];
			foreach (string path in new[] { canonical.MapTopBgPath, canonical.MapMidBgPath, canonical.MapBotBgPath })
			{
				Require(path.StartsWith(RoaringFlareAssets.Root), $"not in mod namespace: {path}");
				Texture2D? texture = ResourceLoader.Load<Texture2D>(path);
				Require(texture != null && texture.GetWidth() > 0, $"cannot load {path}");
				Image image = texture!.GetImage();
				if (image.IsCompressed())
				{
					image.Decompress();
				}

				double r = 0, g = 0, b = 0;
				int count = 0;
				for (int y = 0; y < image.GetHeight(); y += 8)
				{
					for (int x = 0; x < image.GetWidth(); x += 8)
					{
						Color pixel = image.GetPixel(x, y);
						if (pixel.A < 0.8f)
						{
							continue;
						}

						r += pixel.R;
						g += pixel.G;
						b += pixel.B;
						count++;
					}
				}

				Color paper = new((float)(r / count), (float)(g / count), (float)(b / count));
				Color bg = canonical.MapBgColor;
				float diff = Math.Max(Math.Abs(paper.R - bg.R), Math.Max(Math.Abs(paper.G - bg.G), Math.Abs(paper.B - bg.B)));
				Require(diff <= 8f / 255f, $"{path}: paper #{paper.ToHtml(false)} vs MapBgColor #{bg.ToHtml(false)}");
				report.Add($"#{paper.ToHtml(false)}");
			}

			return $"paper {string.Join(" / ", report)} ~ MapBgColor #{canonical.MapBgColor.ToHtml(false)}";
		});

		foreach (Type powerType in ModEntry.ModelTypes<PowerModel>())
		{
			Check($"power {powerType.Name}", () =>
			{
				PowerModel power = ModelDb.GetById<PowerModel>(ModelDb.GetId(powerType));
				Require(power.Title.Exists() && power.HasSmartDescription, "missing powers loc");
				// 图标经 RitsuLib 的 PowerAssetProfile 指到模组目录；路径不存在时 RitsuLib 退回原版加载器，得到 NOPE 占位而不是 null。
				string expected = ((STS2RitsuLib.Scaffolding.Content.ModPowerTemplate)power).AssetProfile.BigIconPath!;
				Require(expected.StartsWith(RoaringFlareAssets.Root) && power.ResolvedBigIconPath == expected, $"big icon {power.ResolvedBigIconPath}, expected {expected}");
				Texture2D icon = power.Icon;
				Require(icon.GetWidth() == 256, $"icon {icon.ResourcePath} {icon.GetWidth()}px (NOPE placeholder?)");
				return $"{power.Id.Entry}: title={power.Title.GetFormattedText()}, icon {icon.GetWidth()}px";
			});
		}

		Check("affliction Ignited", () =>
		{
			// 引燃（侵蚀）：本地化三个键（标题、悬浮说明、卡面附加文字）、可叠加，卡面覆盖层经 RitsuLib 的 AfflictionAssetProfile 指到模组场景，
			// 场景里原版烟雾（Smog）特效的配色表被换成火焰色。
			AfflictionModel kindled = ModelDb.Affliction<Afflictions.Ignited>();
			string entry = kindled.Id.Entry;
			Require(kindled.Title.Exists() && LocString.Exists("afflictions", $"{entry}.description") && LocString.Exists("afflictions", $"{entry}.extraCardText"),
				$"missing afflictions loc {entry}");
			Require(kindled.IsStackable && kindled.HasExtraCardText && !kindled.CanAfflictUnplayableCards, "stacking / card text flags");
			Require(kindled.OverlayPath == RoaringFlareAssets.AfflictionOverlay("ignited") && kindled.HasOverlay, $"overlay {kindled.OverlayPath}");
			Control overlay = ResourceLoader.Load<PackedScene>(kindled.OverlayPath).Instantiate<Control>();
			try
			{
				CanvasItem? main = overlay.GetNodeOrNull<CanvasItem>("vfx_ui_card_ignited/card_mask/smog_main");
				Require(main?.Material is ShaderMaterial material && material.GetShaderParameter("lut").As<GradientTexture1D>() is { } lut
					&& lut.Gradient.GetColor(1) is { R: > 0.8f, B: < 0.1f }, "fire LUT not applied to the overlay");
			}
			finally
			{
				overlay.Free();
			}

			return $"{entry}: {kindled.Title.GetFormattedText()}, overlay {kindled.OverlayPath}";
		});

		Check("random move machines", () =>
		{
			// 随机出招的怪：每招之后回到同一个随机分支，分支恰好覆盖这几招且都不能连用；与 opening_sim.py 的 rand(...) 对应。
			Type[] randomTypes = [typeof(InfectedPatrolOfficer)];
			List<string> report = [];
			foreach (Type type in randomTypes)
			{
				MonsterModel monster = ModelDb.GetById<MonsterModel>(ModelDb.GetId(type)).ToMutable();
				monster.SetUpForCombat();
				RandomBranchState? loop = monster.MoveStateMachine!.States.Values.OfType<RandomBranchState>().SingleOrDefault();
				Require(loop != null, $"{type.Name}: no random branch");
				List<MoveState> moves = monster.MoveStateMachine.States.Values.OfType<MoveState>().ToList();
				Require(moves.All(m => m.FollowUpState == loop), $"{type.Name}: a move does not return to the branch");
				Require(loop!.States.All(s => s.repeatType == MoveRepeatType.CannotRepeat), $"{type.Name}: repeat rule");
				report.Add($"{type.Name}[{string.Join("/", moves.Select(m => m.Id))}]");
			}

			return string.Join(" ", report);
		});

		Check("talulah boss", () =>
		{
			EncounterModel boss = ModelDb.Encounter<TalulahBoss>();
			// 火球中途才召唤，要在 AllPossibleMonsters 里；场景标记点与预载由逐遭遇检查（CheckEncounterSlots）核对。
			Require(boss.HasScene && boss.Slots.SequenceEqual(Talulah.FormationSlots) && boss.Slots[^1] == Talulah.BossSlot, "boss slots");
			FlareMonster fireball = ModelDb.Monster<Fireball>();
			Require(boss.AllPossibleMonsters.Contains(fireball), $"{fireball.Id.Entry} missing from AllPossibleMonsters");

			// 阶段链：开局点燃 → 噬咬 → 焦土 → 引爆 → 噬咬 …；第一次倒下 → 不息恶火 ×3（每回合召唤火球，第三回合重生）→ 噬咬；第二次倒下即死亡。动画名由逐怪物检查按 RequiredAnimations 核对。
			Talulah talulah = (Talulah)ModelDb.Monster<Talulah>().ToMutable();
			talulah.SetUpForCombat();
			Require(talulah.OpeningStateId == Talulah.IgniteMoveId, $"opening {talulah.OpeningStateId}");
			IReadOnlyDictionary<string, MonsterState> states = talulah.MoveStateMachine!.States;
			Require(((MoveState)states[Talulah.IgniteMoveId]).FollowUpState?.Id == Talulah.BiteMoveId, "ignite -> bite");
			Require(((MoveState)states[Talulah.BiteMoveId]).FollowUpState?.Id == Talulah.ScorchMoveId, "bite -> scorch");
			Require(((MoveState)states[Talulah.ScorchMoveId]).FollowUpState?.Id == Talulah.DetonateMoveId, "scorch -> detonate");
			Require(((MoveState)states[Talulah.UndyingFlameMoveId]).FollowUpState?.Id == Talulah.UndyingFlameSecondMoveId, "undying flame -> 2");
			Require(((MoveState)states[Talulah.UndyingFlameSecondMoveId]).FollowUpState?.Id == Talulah.UndyingFlameLastMoveId, "undying flame 2 -> 3");
			Require(((MoveState)states[Talulah.UndyingFlameLastMoveId]).FollowUpState?.Id == Talulah.BiteMoveId, "undying flame 3 -> bite");
			Require(talulah.CreatureScenePath.EndsWith("/black_snake.tscn"), $"scene {talulah.CreatureScenePath}");

			// 地图节点用静态图：剪影与描边都要能按原版路径加载。
			foreach (string path in new[] { boss.BossNodePath + ".png", boss.BossNodePath + "_outline.png" })
			{
				Require(ResourceLoader.Load<Texture2D>(path) is { } tex && tex.GetWidth() > 0, $"cannot load {path}");
			}

			// 专属背景由 RitsuLib 按 AssetProfile 的背景根场景 + 图层目录挂上；目录打不开时它会退回幕背景，所以逐个核对。
			var bossProfile = ((TalulahBoss)boss).AssetProfile;
			Require(ResourceLoader.Load<PackedScene>(bossProfile.BackgroundScenePath!) != null, $"cannot load {bossProfile.BackgroundScenePath}");
			List<string> bossLayers = DirAccess.GetFilesAt(bossProfile.BackgroundLayersDirectoryPath!).Select(f => f.Replace(".remap", "")).Distinct().ToList();
			Require(bossLayers.Count == 1 && bossLayers[0].Contains("_bg_00_"), $"boss bg layers [{string.Join(",", bossLayers)}]");
			Require(ResourceLoader.Load<PackedScene>($"{bossProfile.BackgroundLayersDirectoryPath}/{bossLayers[0]}") != null, "cannot load boss bg layer");
			// 对局历史头像经 RitsuLib 接到原版取图路径（88×88）。
			foreach (string? path in new[] { bossProfile.RunHistoryIconPath, bossProfile.RunHistoryIconOutlinePath })
			{
				Require(path != null && path.StartsWith(RoaringFlareAssets.Root) && ResourceLoader.Load<Texture2D>(path) is { } tex && tex.GetWidth() == 88,
					$"cannot load run history icon {path}");
			}

			return $"opening {talulah.OpeningStateId}; node {boss.BossNodePath}";
		});

		Check("commando captain elite", () =>
		{
			EncounterModel elite = ModelDb.Encounter<CommandoCaptainNormal>();
			// 队长第三回合交接左/中/右三名援军（按 FormationSlots 前三个槽），三者都要在 AllPossibleMonsters 里；
			// 场景标记点与预载由逐遭遇检查（CheckEncounterSlots）核对。
			Require(elite.RoomType == RoomType.Elite, $"room {elite.RoomType}");
			Require(elite.HasScene && elite.Slots.SequenceEqual(CommandoCaptain.FormationSlots) && elite.Slots[^1] == CommandoCaptain.CaptainSlot, "elite slots");
			foreach (FlareMonster reinforcement in new FlareMonster[] { ModelDb.Monster<UrsusCommando>(), ModelDb.Monster<UrsusArmoredCaster>(), ModelDb.Monster<UrsusCrossbowman>() })
			{
				Require(elite.AllPossibleMonsters.Contains(reinforcement), $"{reinforcement.Id.Entry} missing from AllPossibleMonsters");
			}

			return $"slots=[{string.Join(",", elite.Slots)}]";
		});

		Check("sound effects", () =>
		{
			// 原作音效：各怪物音效槽里的片段、黑蛇与追猎者重生演出和聚合体出场直接写名字的片段，都要在包里能按 AudioStream 加载；
			// 播放挂在原版的 SFX 总线上（游戏把音效音量同步到这条总线）。
			Require(AudioServer.Singleton.GetBusIndex("SFX") >= 0, "no SFX audio bus");
			// 本模组怪物被打中时不播原版受击声（FlareSfx.Install 的前缀补丁）。
			Require(Harmony.GetPatchInfo(AccessTools.Method(typeof(MegaCrit.Sts2.Core.Commands.SfxCmd), nameof(MegaCrit.Sts2.Core.Commands.SfxCmd.PlayDamage)))
				?.Prefixes.Any(p => p.owner == ModEntry.HarmonyId) == true, "vanilla impact sound not muted");
			HashSet<string> clips = [
				"e_skill_bsnake1to2_evolution_1", "e_skill_bsnake1to2_evolution_2", "e_skill_bsnake1to2_evolution_3",
				"e_skill_bsnake1to2_appear", "e_skill_bsnake1to2_finish", "e_skill_bsnake1to2_screenattack_cast",
				"e_skill_bsnake1to2_screenattack_fire", "e_atk_bsnake_n", "a_bat_volcano",
				"e_imp_elec", "e_skill_blackexplo", "e_atk_blackring", "e_skill_bigblackring",
			];
			foreach (Type type in ModEntry.ModelTypes<FlareMonster>())
			{
				clips.UnionWith(((FlareMonster)ModelDb.GetById<MonsterModel>(ModelDb.GetId(type))).AllSoundClips());
			}

			List<string> missing = clips.Where(clip => ResourceLoader.Load<AudioStream>(Audio.FlareSfx.Root + clip + ".ogg") is not { } stream
				|| stream.GetLength() <= 0).ToList();
			Require(missing.Count == 0, $"missing sounds: {string.Join(", ", missing)}");
			int files = DirAccess.GetFilesAt(Audio.FlareSfx.Root.TrimEnd('/')).Count(f => f.EndsWith(".ogg.import", StringComparison.Ordinal));
			return $"{clips.Count} clips used, {files} sound files packed";
		});

		Check("arknights effects", () =>
		{
			// 原作特效（Vfx.AkFx）：包里每份描述都能解析、引用的贴图都在；各怪物用到的特效名都有描述。
			// 着色器只在真正渲染时编译，headless 下核对不到，靠 ROARINGFLARE_VFX_PREVIEW 预览（tools/ak_vfx/preview.py）。
			string[] files = [.. DirAccess.GetFilesAt(Vfx.AkFx.Root.TrimEnd('/')).Where(f => f.EndsWith(".json", StringComparison.Ordinal))];
			Require(files.Length > 0, "no effects packed");
			HashSet<string> textures = [];
			foreach (string file in files)
			{
				Vfx.FxSpec spec = Vfx.FxSpec.Parse(Godot.FileAccess.GetFileAsString(Vfx.AkFx.Root + file));
				Require(spec.Nodes.Length > 0, $"{file} has no nodes");
				foreach (Vfx.FxNodeSpec n in spec.Nodes)
				{
					foreach (Vfx.FxMaterial? mat in new[] { n.Ps?.Render.Mat, n.Ps?.Render.TrailMat, n.Mesh?.Mat, n.Trail?.Mat })
					{
						foreach (string? tex in new[] { mat?.Tex, mat?.Aux })
						{
							if (tex is not null)
							{
								textures.Add(tex);
							}
						}
					}
				}
			}

			List<string> missingTex = textures.Where(t => !ResourceLoader.Exists(Vfx.AkFx.TexturePath(t))).ToList();
			Require(missingTex.Count == 0, $"missing textures: {string.Join(", ", missingTex)}");
			HashSet<string> used = [];
			foreach (Type type in ModEntry.ModelTypes<FlareMonster>())
			{
				used.UnionWith(((FlareMonster)ModelDb.GetById<MonsterModel>(ModelDb.GetId(type))).AllEffectNames());
			}

			List<string> missing = used.Where(name => !Godot.FileAccess.FileExists(Vfx.AkFx.SpecPath(name))).ToList();
			Require(missing.Count == 0, $"missing effects: {string.Join(", ", missing)}");
			return $"{files.Length} effects parsed, {textures.Count} textures, {used.Count} used by monsters";
		});

		Check("music files", () =>
		{
			// 配乐由 FMOD 读原文件（导入设为 keep），包里必须有原始 mp3。
			string[] tracks = [Audio.FlareMusic.TalulahTrackPath, Audio.FlareMusic.TalulahPhaseTwoTrackPath];
			List<string> missing = tracks.Where(track => !Godot.FileAccess.FileExists(track)).ToList();
			Require(missing.Count == 0, $"missing {string.Join(", ", missing)}");
			return string.Join(", ", tracks);
		});

		Check("harbinger flight and landing routes", () =>
		{
			Require(HarbingerFlightPower.AmountForMaxHp(159, 50) == 80 && HarbingerFlightPower.AmountForMaxHp(46, 25) == 12
				&& HarbingerFlightPower.AmountForMaxHp(156, 10) == 16, "flight HP amount rounds up");
			foreach (HarbingerBase monster in new HarbingerBase[]
				{ (CoreHarbinger)ModelDb.Monster<CoreHarbinger>().ToMutable(), (ArtilleryHarbinger)ModelDb.Monster<ArtilleryHarbinger>().ToMutable() })
			{
				Creature creature = new(monster, CombatSide.Enemy, null);
				monster.SetUpForCombat();
				IReadOnlyDictionary<string, MonsterState> states = monster.MoveStateMachine!.States;
				MonsterState mode = states[HarbingerBase.ModeBranchId];
				Require(mode.GetNextState(creature, new Rng(1)) == HarbingerBase.RapidFireMoveId, "grounded -> rapid fire");
				PowerModel flight = ModelDb.Power<HarbingerFlightPower>().ToMutable();
				flight.ApplyInternal(creature, HarbingerFlightPower.AmountForMaxHp(creature.MaxHp, HarbingerBase.StandardFlightPercent), silent: true);
				Require(mode.GetNextState(creature, new Rng(1)) == HarbingerBase.FlightBranchId, "flight -> flying branch");
				flight.RemoveInternal();
				Require(mode.GetNextState(creature, new Rng(1)) == HarbingerBase.RapidFireMoveId, "flight removed -> grounded");
				if (monster is CoreHarbinger)
				{
					Require(monster.OpeningStateId == HarbingerBase.MarkMoveId, "core opens with mark");
					Require(((MoveState)states[HarbingerBase.RapidFireMoveId]).FollowUpState?.Id == CoreHarbinger.RepairMoveId, "rapid -> repair");
					Require(((MoveState)states[CoreHarbinger.RepairMoveId]).FollowUpState?.Id == CoreHarbinger.RestartMoveId, "repair -> restart");
					Require(((MoveState)states[CoreHarbinger.RestartMoveId]).FollowUpState?.Id == HarbingerBase.ModeBranchId, "restart -> mode");
				}
			}
			Require(ModelDb.Power<ArtilleryMarkPower>().InstanceType == PowerInstanceType.Instanced, "marks need independent timers");
			return "flight/landing routes; core MARK, RAPID_FIRE -> REPAIR -> RESTART; independent marks";
		});

		Check("hunter forms", () =>
		{
			// 一阶段 巡猎 → 斩击 → 坍缩恐惧；重整之后是二阶段 2→1→3 循环（连斩 → 强化 → 坍缩恐惧）。
			EmperorsBladeHunter hunter = (EmperorsBladeHunter)ModelDb.Monster<EmperorsBladeHunter>().ToMutable();
			hunter.SetUpForCombat();
			IReadOnlyDictionary<string, MonsterState> states = hunter.MoveStateMachine!.States;
			Require(((MoveState)states[EmperorsBladeHunter.ReformMoveId]).FollowUpState?.Id == EmperorsBladeHunter.RendMoveId, "reform -> rend");
			Require(((MoveState)states[EmperorsBladeHunter.RendMoveId]).FollowUpState?.Id == EmperorsBladeHunter.FortifyMoveId, "rend -> fortify");
			Require(((MoveState)states[EmperorsBladeHunter.FortifyMoveId]).FollowUpState?.Id == EmperorsBladeHunter.SecondDreadMoveId, "fortify -> dread");
			Require(((MoveState)states[EmperorsBladeHunter.SecondDreadMoveId]).FollowUpState?.Id == EmperorsBladeHunter.RendMoveId, "dread -> rend");
			IReadOnlyList<MonsterModel> elite = ((FlareEncounter)ModelDb.Encounter<EmperorsBladeElite>()).CreateMonsters(new Rng(1));
			FlareMonster opening = (FlareMonster)elite.Single();
			opening.SetUpForCombat();
			Require(opening.OpeningStateId == EmperorsBladeHunter.PatrolMoveId, $"elite opening {opening.OpeningStateId}");
			return $"opening {opening.OpeningStateId}; REFORM -> REND -> FORTIFY -> SECOND_DREAD -> REND";
		});

		foreach (Type monster in ModEntry.ModelTypes<FlareMonster>())
		{
			Check($"monster {monster.Name}", () => CheckMonster(monster));
		}

		Log.Info($"{Prefix} Finished with {_failures} failure(s).");
	}

	/// <summary>
	/// 敌人模型：本地化键存在、creature_visuals 场景能实例化、Spine 骨架真正加载（版本不对时为空）、图集贴图非空、代码用到的动画名都在 skel 里。
	/// </summary>
	private static string CheckMonster(Type type)
	{
		FlareMonster canonical = (FlareMonster)ModelDb.GetById<MonsterModel>(ModelDb.GetId(type));
		string entry = canonical.Id.Entry;
		Require(canonical.Title.Exists(), $"missing monsters loc {entry}.name");

		MonsterModel mutable = canonical.ToMutable();
		mutable.SetUpForCombat();
		List<string> moves = [];
		foreach (MonsterState state in mutable.MoveStateMachine!.States.Values)
		{
			if (state is not MoveState || !state.Id.EndsWith("_MOVE", StringComparison.Ordinal))
			{
				continue;
			}

			// 与原版图鉴一致：招式名键去掉 _MOVE 后缀；_2 这类重复状态不查。
			string moveKey = state.Id[..^"_MOVE".Length];
			Require(LocString.Exists("monsters", $"{entry}.moves.{moveKey}.title"), $"missing monsters loc {entry}.moves.{moveKey}.title");
			moves.Add(moveKey);
		}

		string scenePath = FlareMonster.SceneRoot + canonical.SceneName + ".tscn";
		PackedScene? scene = ResourceLoader.Load<PackedScene>(scenePath);
		Require(scene != null, $"cannot load {scenePath}");
		Node root = scene!.Instantiate();
		try
		{
			Node visuals = root.GetNode("%Visuals");
			Require(visuals.GetClass() == "SpineSprite", $"%Visuals is {visuals.GetClass()}");
			foreach (string marker in new[] { "%Bounds", "%IntentPos", "%CenterPos", "%OrbPos", "%TalkPos" })
			{
				Require(root.GetNodeOrNull(marker) != null, $"missing {marker}");
			}

			GodotObject? skeletonData = visuals.Get("skeleton_data_res").AsGodotObject();
			Require(skeletonData != null, "SpineSprite has no skeleton_data_res");
			Require(skeletonData!.Call("is_skeleton_data_loaded").AsBool(), "skeleton data not loaded (Spine version mismatch?)");
			GodotObject? atlas = skeletonData.Get("atlas_res").AsGodotObject();
			Require(atlas != null, "skeleton data has no atlas_res");
			Godot.Collections.Array textures = atlas!.Call("get_textures").AsGodotArray();
			Require(textures.Count > 0 && textures.All(t => t.AsGodotObject() is Texture2D { } tex && tex.GetWidth() > 0),
				$"atlas textures missing ({textures.Count})");

			MegaSkeletonDataResource data = new(skeletonData);
			List<string> missing = canonical.RequiredAnimations.Where(anim => !data.HasAnimation(anim)).ToList();
			Require(missing.Count == 0, $"animations not in skel: {string.Join(", ", missing)}");

			return $"{entry}: moves=[{string.Join(", ", moves)}], anims=[{string.Join(", ", canonical.RequiredAnimations)}], "
				+ $"hp={canonical.MinInitialHp}-{canonical.MaxInitialHp}";
		}
		finally
		{
			root.Free();
		}
	}

	/// <summary>
	/// 按场景标记点站位的遭遇战（照原版卵翼虫）：每个槽位都要有同名 Marker2D，开场阵容的槽位都在 Slots 里；
	/// 中途才召唤的怪物（AllPossibleMonsters 里不在开场阵容的）要列进 AssetProfile.ExtraAssetPaths：原版只预载开场阵容，
	/// 这份清单由 RitsuLib 在 GetAssetPaths 的后置补丁里追加（原版的 ExtraAssetPaths 属性仍是 null，不能拿来核对）。
	/// 不用场景的遭遇战不该给槽位。返回附在日志里的摘要。
	/// </summary>
	private static string CheckEncounterSlots(FlareEncounter encounter, IReadOnlyList<(MonsterModel Monster, string? Slot)> lineup)
	{
		if (!encounter.HasScene)
		{
			Require(encounter.Slots.Count == 0 && lineup.All(entry => entry.Slot == null), "slots without an encounter scene");
			return "";
		}

		Require(encounter.Slots.Count > 0 && encounter.Slots.Distinct().Count() == encounter.Slots.Count, $"slots [{string.Join(",", encounter.Slots)}]");
		Require(lineup.All(entry => entry.Slot != null && encounter.Slots.Contains(entry.Slot)),
			$"lineup slots [{string.Join(",", lineup.Select(entry => entry.Slot))}] not in Slots");
		Control scene = encounter.CreateScene();
		try
		{
			List<string> missing = encounter.Slots.Where(slot => scene.GetNodeOrNull<Marker2D>(slot) == null).ToList();
			Require(missing.Count == 0, $"encounter scene lacks {string.Join(", ", missing)}");
		}
		finally
		{
			scene.Free();
		}

		List<FlareMonster> summons = encounter.AllPossibleMonsters.Cast<FlareMonster>().Where(monster => lineup.All(entry => entry.Monster.Id != monster.Id)).ToList();
		foreach (FlareMonster summon in summons)
		{
			Require(encounter.AssetProfile.ExtraAssetPaths?.Contains(summon.CreatureScenePath) == true, $"{summon.Id.Entry} scene not preloaded");
		}

		return $"; slots=[{string.Join(",", encounter.Slots)}] summons=[{string.Join(",", summons.Select(monster => monster.Id.Entry))}]";
	}

	private static void Check(string name, Func<string> body)
	{
		try
		{
			Log.Info($"{Prefix} PASS {name}: {body()}");
		}
		catch (Exception ex)
		{
			_failures++;
			Log.Error($"{Prefix} FAIL {name}: {ex.GetType().Name}: {ex.Message}");
		}
	}

	private static void Require(bool condition, string detail)
	{
		if (!condition)
		{
			throw new InvalidOperationException(detail);
		}
	}
}
