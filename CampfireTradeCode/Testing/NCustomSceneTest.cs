using Godot;

namespace CampfireTrade.CampfireTradeCode.Testing;

public partial class NCustomSceneTest : CanvasLayer
{
    public override void _Ready()
    {
        GD.Print("[CampfireTrade] Custom test scene initialized");
        
        Button closeButton = GetNode<Button>("Center/Panel/Content/CloseButton");

        closeButton.Pressed += QueueFree;
    }
}