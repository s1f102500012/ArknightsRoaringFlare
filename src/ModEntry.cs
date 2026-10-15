using ArknightsRoaringFlare.Acts;
using ArknightsRoaringFlare.Encounters;
using ArknightsRoaringFlare.Monsters;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Content;

namespace ArknightsRoaringFlare;

[ModInitializer(nameof(Initialize))]
public static class ModEntry
{
	public const string ModId = "ArknightsRoaringFlare";
	internal const string HarmonyId = "Natsuki.ArknightsRoaringFlare";

	private static bool _initialized;

	public static void Initialize()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		RegisterContent();
		Harmony harmony = new(HarmonyId);
		Integration.ActLikeIt2Bridge.Install(harmony);
		Integration.DoubleBossPatch.Install(harmony);
		Audio.FlareMusic.Install(harmony);
		Audio.FlareSfx.Install(harmony);
		RoaringFlareSelfTest.InstallIfRequested(harmony);
		Vfx.AkFxPreview.InstallIfRequested();
		Log.Info($"[{ModId}] Loaded; registered {nameof(RoaringFlareAct)} as an act-index {RoaringFlareAct.ActIndex} variant through RitsuLib (ActLikeIt2 act choice is optional).");
	}

	// 当前战斗池只保留本地重做的遭遇，旧遭遇定义不再按幕注册；保留项的相对顺序和类名不变。
	// 类名经 RitsuLib 得出模型 id（ARKNIGHTS_ROARING_FLARE_<类别>_<类名>），存档按 id 引用。

	/// <summary>本幕战斗池（弱怪、普通、精英、Boss），经 RitsuLib 按幕注册；原版的 AllBossEncounters 从中按房间类型筛出 Boss。</summary>
	internal static readonly Type[] ActEncounterTypes =
	[
		typeof(PatrolWeak),
		typeof(RavagersWeak),
		typeof(HuskVillageSearchNormal),
		typeof(RavagerPackNormal),
		typeof(CasterSquadNormal),
		typeof(SilentButcherNormal),
		typeof(CommandoCaptainNormal),
		typeof(EmperorsBladeElite),
		typeof(VanguardVeteranElite),
		typeof(TalulahBoss),
		typeof(GuidedArtilleryNormal),
	];

	/// <summary>全部敌人与本模组能力：按类型全集注册（顺序不影响 id）。</summary>
	internal static IEnumerable<Type> ModelTypes<TBase>() =>
		typeof(ModEntry).Assembly.GetTypes()
			.Where(type => !type.IsAbstract && typeof(TBase).IsAssignableFrom(type))
			.OrderBy(type => type.FullName, StringComparer.Ordinal);

	/// <summary>幕、战斗池、怪物、能力、卡牌侵蚀全部经 RitsuLib 注册，必须在模型库初始化之前调用（模组初始化阶段）。</summary>
	private static void RegisterContent()
	{
		ModContentRegistry registry = ModContentRegistry.For(ModId);
		registry.RegisterAct<RoaringFlareAct>();
		foreach (Type encounterType in ActEncounterTypes)
		{
			registry.RegisterActEncounter(typeof(RoaringFlareAct), encounterType);
		}

		foreach (Type monsterType in ModelTypes<FlareMonster>())
		{
			registry.RegisterMonster(monsterType);
		}

		foreach (Type powerType in ModelTypes<PowerModel>())
		{
			registry.RegisterPower(powerType);
		}

		foreach (Type afflictionType in ModelTypes<AfflictionModel>())
		{
			registry.RegisterAffliction(afflictionType);
		}
	}
}
