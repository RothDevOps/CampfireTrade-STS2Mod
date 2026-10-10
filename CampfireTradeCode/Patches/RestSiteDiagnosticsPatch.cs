using Godot;
using HarmonyLib;
using CampfireTrade.CampfireTradeCode.RestSites;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.RestSite;
using MegaCrit.Sts2.Core.Nodes.Rooms;

namespace CampfireTrade.CampfireTradeCode.Patches;

// Observe the synchronized options and UI without modifying either: option indices
// must stay identical on all clients for multiplayer selection to remain valid.
[HarmonyPatch(typeof(RestSiteSynchronizer), nameof(RestSiteSynchronizer.BeginRestSite))]
internal static class RestSiteOptionsDiagnosticsPatch
{
    private static void Finalizer(RestSiteSynchronizer __instance, Exception? __exception)
    {
        try
        {
            if (__exception != null)
            {
                MainFile.Logger.Error($"Rest site option generation failed: {__exception}");
                return;
            }

            MainFile.Logger.Info("Local rest site options after BeginRestSite: " +
                                string.Join(", ", __instance.GetLocalOptions().Select(option =>
                                    $"{option.OptionId} ({option.GetType().FullName})")));

            foreach (var option in __instance.GetLocalOptions().OfType<TradeRestSiteOption>())
            {
                // Check before preloading: a failure there prevents NRestSiteRoom._Ready entirely.
                foreach (string path in option.AssetPaths)
                {
                    bool exists = ResourceLoader.Exists(path);
                    MainFile.Logger.Info($"Trade asset before preload: {path}; exists={exists}");
                    if (!exists)
                        MainFile.Logger.Warn($"Missing Trade asset: {path}. Check the installed PCK and imports.");
                }

                MainFile.Logger.Info($"Trade localization; language={LocManager.Instance.Language}; " +
                                    $"titleExists={LocString.Exists("rest_site_ui", "OPTION_TRADE.name")}; " +
                                    $"descriptionExists={LocString.Exists("rest_site_ui", "OPTION_TRADE.description")}");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Could not inspect local rest site options: {ex}");
        }
    }
}

// Godot can report a child node's _Ready exception independently of the room's _Ready.
[HarmonyPatch(typeof(NRestSiteButton), nameof(NRestSiteButton._Ready))]
internal static class TradeButtonDiagnosticsPatch
{
    private static void Finalizer(NRestSiteButton __instance, Exception? __exception)
    {
        try
        {
            if (__instance.Option is not TradeRestSiteOption)
                return;

            if (__exception != null)
                MainFile.Logger.Error($"Trade button initialization failed: {__exception}");
            else
                MainFile.Logger.Info($"Trade button ready; enabled={__instance.Option.IsEnabled}; " +
                                    $"visible={__instance.Visible}; size={__instance.Size}");
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Could not inspect Trade button: {ex}");
        }
    }
}

[HarmonyPatch(typeof(NRestSiteRoom), nameof(NRestSiteRoom._Ready))]
internal static class RestSiteUiDiagnosticsPatch
{
    // A void finalizer preserves the original exception if room creation failed.
    private static void Finalizer(NRestSiteRoom __instance, Exception? __exception)
    {
        try
        {
            if (__exception != null)
                MainFile.Logger.Error($"Rest site UI initialization failed: {__exception}");

            Control? container = __instance.GetNodeOrNull<Control>("%ChoicesContainer");
            string buttons = container == null ? "container missing" : string.Join(", ",
                container.GetChildren().OfType<NRestSiteButton>().Select(button => button.Option.OptionId));
            MainFile.Logger.Info($"Rest site UI; language={LocManager.Instance.Language}; buttons={buttons}");

            foreach (var option in __instance.Options.Where(option => option.OptionId == "TRADE"))
            {
                foreach (string path in option.AssetPaths)
                    MainFile.Logger.Info($"Trade asset: {path}; exists={ResourceLoader.Exists(path)}");
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Could not inspect rest site UI: {ex}");
        }
    }
}
