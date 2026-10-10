using System.Reflection;
using System.Globalization;
using BaseLib.Config;
using CampfireTrade.CampfireTradeCode.Config;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Modding;

namespace CampfireTrade.CampfireTradeCode;

//You're recommended but not required to keep all your code in this package and all your assets in the CampfireTrade folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile
{
    public const string ModId = "CampfireTrade"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static void Initialize()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string stage = "registering Godot scripts";
        Logger.Info($"Initializing {assembly.FullName}; runtime={Environment.Version}; " +
                    $"build={assembly.ManifestModule.ModuleVersionId}; " +
                    $"OS={Environment.OSVersion}; culture={CultureInfo.CurrentCulture.Name}; " +
                    $"game={typeof(ModInitializerAttribute).Assembly.GetName().Version}; " +
                    $"BaseLib={typeof(ModConfigRegistry).Assembly.GetName().Version}");
        try
        {
            Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(assembly);
            stage = "registering configuration";
            ModConfigRegistry.Register(ModId, new CampfireTradeConfig());
            stage = "applying Harmony patches";
            Harmony harmony = new(ModId);
            harmony.PatchAll(assembly);
            Logger.Info("Initialization complete. Patched methods: " +
                        string.Join(", ", harmony.GetPatchedMethods().Select(method =>
                            $"{method.DeclaringType?.FullName}.{method.Name}")));
            MethodInfo? generate = AccessTools.Method(typeof(RestSiteOption), nameof(RestSiteOption.Generate));
            if (generate != null)
                Logger.Info("RestSiteOption.Generate patch owners: " +
                            string.Join(", ", Harmony.GetPatchInfo(generate)?.Owners ?? Enumerable.Empty<string>()));
        }
        catch (Exception ex)
        {
            Logger.Error($"Initialization failed while {stage}: {ex}");
            throw;
        }
    }
}
