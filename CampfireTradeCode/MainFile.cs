using System.Reflection;
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

        Harmony harmony = new(ModId);

        harmony.PatchAll();
        
        //Callable.From(ShowCustomTestScene).CallDeferred();
    }
    
    private const string CustomTestScenePath = "res://CampfireTrade/TestScene/CustomSceneTest.tscn";
    
    private static void ShowCustomTestScene()
    {
        PackedScene? packedScene = ResourceLoader.Load<PackedScene>(CustomTestScenePath);

        if (packedScene is null)
        {
            MainFile.Logger.Error(
                $"Could not load custom test scene: " +
                $"{CustomTestScenePath}"
            );
            return;
        }

        CanvasLayer scene = packedScene.Instantiate<CanvasLayer>();

        Label counter = scene.GetNode<Label>("Control/Center/Panel/Content/Counter");
        Button testButton = scene.GetNode<Button>("Control/Center/Panel/Content/TestButton");
        Button closeButton = scene.GetNode<Button>("Control/Center/Panel/Content/CloseButton");

        int pressCount = 0;

        testButton.Pressed += () =>
        {
            pressCount++;

            counter.Text = $"Button pressed {pressCount} " + $"time{(pressCount == 1 ? string.Empty : "s")}";

            GD.Print($"[CampfireTrade] Test button pressed " + $"{pressCount} time(s).");
        };

        closeButton.Pressed += () =>
        {
            GD.Print("[CampfireTrade] Closing custom test scene.");

            scene.QueueFree();
        };

        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            scene.QueueFree();

            MainFile.Logger.Error("The active main loop is not a SceneTree.");
            return;
        }

        sceneTree.Root.AddChild(scene);

        GD.Print("[CampfireTrade] Custom test scene initialized externally.");
    }
}