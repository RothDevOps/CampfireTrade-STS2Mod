using Godot;

namespace CampfireTrade.CampfireTradeCode.Testing;

[GlobalClass]
public partial class NCustomSceneTest : CanvasLayer
{
	// private int _pressCount;
	//
	// private Label _counter = null!;
	// private Button _testButton = null!;
	// private Button _closeButton = null!;
	//
	// public override void _Ready()
	// {
	//     base._Ready();
	//     //GD.Print("[CampfireTrade] Custom test scene initialized");
	//     
	//     // _counter = GetNode<Label>("Control/Center/Panel/Content/Counter") ?? throw new InvalidOperationException("Counter node not found.");
	//     // _testButton = GetNode<Button>("Control/Center/Panel/Content/TestButton") ?? throw new InvalidOperationException("Test button not found.");
	//     // _closeButton = GetNode<Button>("Control/Center/Panel/Content/CloseButton") ?? throw new InvalidOperationException("Close button not found.");
	//     //
	//     // _testButton.Pressed += OnTestButtonPressed;
	//     // _closeButton.Pressed += OnCloseButtonPressed;
	//
	//     //GD.Print("[CampfireTrade] Custom test scene initialized.");
	// }
	
	// private void TestMethod()
	// {
	// }
	//
	// public override void _ExitTree()
	// {
	//     // Disconnecting is defensive and prevents lingering delegates
	//     // if the node is removed in an unusual way.
	//     if (IsInstanceValid(_testButton))
	//     {
	//         _testButton.Pressed -= OnTestButtonPressed;
	//     }
	//
	//     if (IsInstanceValid(_closeButton))
	//     {
	//         _closeButton.Pressed -= OnCloseButtonPressed;
	//     }
	// }
	//
	// private void OnTestButtonPressed()
	// {
	//     _pressCount++;
	//
	//     _counter.Text = $"Button pressed {_pressCount} time{(_pressCount == 1 ? "" : "s")}";
	//
	//     GD.Print($"[CampfireTrade] Test button pressed {_pressCount} time(s).");
	// }
	//
	// private void OnCloseButtonPressed()
	// {
	//     GD.Print("[CampfireTrade] Closing custom test scene.");
	//     QueueFree();
	// }
}
