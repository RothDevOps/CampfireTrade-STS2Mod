using System.Reflection;
using BaseLib.Config;
using CampfireTrade.CampfireTradeCode.Config;
using CampfireTrade.CampfireTradeCode.Testing;
using Godot;
using HarmonyLib;
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
        //If you want to use scripts defined in your mod for Godot scenes, uncomment the following line.
        Godot.Bridge.ScriptManagerBridge.LookupScriptsInAssembly(Assembly.GetExecutingAssembly());
        
        ModConfigRegistry.Register(ModId, new CampfireTradeConfig());

        Harmony harmony = new(ModId);

        harmony.PatchAll();
    }
}