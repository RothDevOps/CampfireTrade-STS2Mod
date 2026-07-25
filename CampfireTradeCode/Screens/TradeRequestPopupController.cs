using System.Reflection;
using CampfireTrade.CampfireTradeCode.Config;
using CampfireTrade.CampfireTradeCode.Trading;
using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;
using MegaCrit.Sts2.Core.Platform;
using MegaCrit.Sts2.Core.Runs;

namespace CampfireTrade.CampfireTradeCode.Screens;

public static class TradeRequestPopupController
{
    private const string ScenePath = "res://CampfireTrade/Scenes/trade_request_popup.tscn";
    private const string PopupSceneName = "CampfireTradeRequestPopup";

    private const float CardScale = 0.55f;
    
    private static bool _isPopupOpen;
    
    private static readonly Vector2 GenericPopupSize = new(522f, 600f);
    private static readonly Vector2 VerticalPopupSize = new(573f, 659f);
    
    private static readonly Vector2 BackgroundOverhang = new(35f, 45f);

    public static async Task<bool> ShowRequestToTarget(TradeOffer offer)
    {
        if (_isPopupOpen)
        {
            MainFile.Logger.Error("Cannot show trade request popup: Already open.");
            return false;
        }
        
        _isPopupOpen = true;
        
        PackedScene? packedScene = ResourceLoader.Load<PackedScene>(ScenePath);

        if (packedScene == null)
        {
            MainFile.Logger.Error($"Could not load trade request popup scene: {ScenePath}");
            _isPopupOpen = false;
            return false;
        }

        if (Engine.GetMainLoop() is not SceneTree sceneTree)
        {
            MainFile.Logger.Error("Cannot show trade request popup: active main loop is not a SceneTree.");
            _isPopupOpen = false;
            return false;
        }

        if (!TryCreateGenericPopupParts(out Node background, out Node headerNode, out Node descriptionNode, out Node noButtonNode, out Node yesButtonNode))
        {
            MainFile.Logger.Error("Could not create copied NGenericPopup parts.");
            _isPopupOpen = false;
            return false;
        }

        CanvasLayer scene = packedScene.Instantiate<CanvasLayer>();
        scene.Name = PopupSceneName;
        scene.Layer = 100;

        Control root = scene.GetNode<Control>("%Root");
        ColorRect? localBackstop = scene.GetNodeOrNull<ColorRect>("%Backstop");

        Control popupFrame = scene.GetNode<Control>("%PopupFrame");
        Control panelHost = scene.GetNode<Control>("%PanelHost");

        Control headerHost = scene.GetNode<Control>("%HeaderHost");
        Control descriptionHost = scene.GetNode<Control>("%DescriptionHost");
        Control cardSlot = scene.GetNode<Control>("%CardSlot");

        Control noButtonHost = scene.GetNode<Control>("%NoButtonHost");
        Control yesButtonHost = scene.GetNode<Control>("%YesButtonHost");

        popupFrame.CustomMinimumSize = GenericPopupSize;

        LocString header = new("rest_site_ui", "OPTION_TRADE.requestHeader");

        LocString body = new("rest_site_ui", "OPTION_TRADE.requestBody");
        body.Add("Initiator", GetPlayerName(offer.Initiator));

        LocString decline = new("rest_site_ui", "OPTION_TRADE.decline");
        LocString accept = new("rest_site_ui", "OPTION_TRADE.accept");

        string headerText = header.GetFormattedText();
        string bodyText = body.GetFormattedText();
        string declineText = decline.GetFormattedText();
        string acceptText = accept.GetFormattedText();

        SetNodeText(headerNode, headerText);
        SetNodeText(descriptionNode, bodyText);
        SetPopupButtonText(noButtonNode, declineText);
        SetPopupButtonText(yesButtonNode, acceptText);

        TaskCompletionSource<bool> result = new();

        Node? oldPopup = sceneTree.Root.GetNodeOrNull(PopupSceneName);

        if (oldPopup != null)
        {
            oldPopup.QueueFree();
            NModalContainer.Instance?.HideBackstop();
        }

        bool usingGameBackstop = TryShowGameBackstop();

        if (localBackstop != null)
        {
            localBackstop.Visible = !usingGameBackstop;
        }

        root.MouseFilter = Control.MouseFilterEnum.Stop;

        Action declineAction = () => Complete(scene, result, accepted: false, hideGameBackstop: usingGameBackstop);

        Action acceptAction = () => Complete(scene, result, accepted: true, hideGameBackstop: usingGameBackstop);

        InstallBackground(panelHost, background);
        InstallHostedNode(headerHost, headerNode, new Vector2(480f, 80f), mouseFilter: Control.MouseFilterEnum.Ignore);
        InstallHostedNode(descriptionHost, descriptionNode, new Vector2(470f, 90f), mouseFilter: Control.MouseFilterEnum.Ignore);

        InstallHostedNode(noButtonHost, noButtonNode, new Vector2(180f, 72f), mouseFilter: null);
        InstallHostedNode(yesButtonHost, yesButtonNode, new Vector2(180f, 72f), mouseFilter: null);

        if (!ConnectPopupButton(noButtonNode, declineAction))
        {
            _isPopupOpen = false;
            return FailBeforeShow(scene, usingGameBackstop, "Could not connect copied NoButton.");
        }

        if (!ConnectPopupButton(yesButtonNode, acceptAction))
        {
            _isPopupOpen = false;
            return FailBeforeShow(scene, usingGameBackstop, "Could not connect copied YesButton.");
        }

        InstallEscapeBlocker(root, declineAction);

        sceneTree.Root.AddChild(scene);

        await scene.ToSignal(sceneTree, SceneTree.SignalName.ProcessFrame);

        FitBackgroundToPanelHost(panelHost, background);
        
        root.GrabFocus();

        AddCardPreview(cardSlot, offer.RequestedCard);

        return await result.Task;
    }
    
    private static bool FailBeforeShow(CanvasLayer scene, bool hideGameBackstop, string message)
    {
        MainFile.Logger.Warn(message);

        if (hideGameBackstop)
        {
            NModalContainer.Instance?.HideBackstop();
        }

        if (GodotObject.IsInstanceValid(scene))
        {
            scene.QueueFree();
        }
        
        return false;
    }
    
    private static bool TryCreateGenericPopupParts(out Node background, out Node header, out Node description, out Node noButton, out Node yesButton)
    {
        background = null!;
        header = null!;
        description = null!;
        noButton = null!;
        yesButton = null!;

        NGenericPopup? templatePopup = NGenericPopup.Create();

        if (templatePopup == null)
        {
            MainFile.Logger.Warn("Could not create NGenericPopup template.");
            return false;
        }

        try
        {
            Node? verticalPopup = templatePopup.GetNodeOrNull<Node>("VerticalPopup");

            if (verticalPopup == null)
            {
                MainFile.Logger.Warn("Could not find VerticalPopup inside NGenericPopup template.");
                return false;
            }

            Node? sourceHeader = verticalPopup.GetNodeOrNull<Node>("Header");
            Node? sourceDescription = verticalPopup.GetNodeOrNull<Node>("Description");
            Node? sourceNoButton = verticalPopup.GetNodeOrNull<Node>("NoButton");
            Node? sourceYesButton = verticalPopup.GetNodeOrNull<Node>("YesButton");

            if (sourceHeader == null || sourceDescription == null || sourceNoButton == null || sourceYesButton == null)
            {
                MainFile.Logger.Warn("NGenericPopup template is missing one or more expected nodes.");
                return false;
            }

            background = verticalPopup.Duplicate();
            header = sourceHeader.Duplicate();
            description = sourceDescription.Duplicate();
            noButton = sourceNoButton.Duplicate();
            yesButton = sourceYesButton.Duplicate();

            PrepareBackgroundCopy(background);

            return true;
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to copy NGenericPopup parts: {ex}");
            return false;
        }
        finally
        {
            templatePopup.QueueFree();
        }
    }
    
    private static void PrepareBackgroundCopy(Node background)
    {
        background.Name = "CopiedVerticalPopupBackground";

        foreach (Node child in background.GetChildren())
        {
            if (child is CanvasItem canvasItem)
            {
                canvasItem.Visible = false;
            }

            if (child is Control control)
            {
                control.MouseFilter = Control.MouseFilterEnum.Ignore;
            }
                
        }

        if (background is Control backgroundControl)
        {
            backgroundControl.MouseFilter = Control.MouseFilterEnum.Ignore;
            backgroundControl.SetAnchorsPreset(Control.LayoutPreset.FullRect, keepOffsets: false);
            backgroundControl.ZIndex = -10;
        }
    }
    
    private static void InstallBackground(Control panelHost, Node background)
    {
        ClearChildren(panelHost);

        panelHost.AddChild(background);
        panelHost.MoveChild(background, 0);

        if (background is not Control backgroundControl)
        {
            return;
        }

        backgroundControl.MouseFilter = Control.MouseFilterEnum.Ignore;

        // Do not rely on anchors for the copied NVerticalPopup visual itself.
        // It appears to draw at its original internal size.
        backgroundControl.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);

        backgroundControl.Position = Vector2.Zero;
        backgroundControl.Size = VerticalPopupSize;
        backgroundControl.CustomMinimumSize = VerticalPopupSize;
        backgroundControl.PivotOffset = Vector2.Zero;
        backgroundControl.Scale = Vector2.One;
        backgroundControl.ZIndex = -10;

        if (backgroundControl is CanvasItem canvasItem)
        {
            canvasItem.Visible = false;
        }
    }
    
    private static void FitBackgroundToPanelHost(Control panelHost, Node background)
    {
        if (background is not Control backgroundControl)
        {
            return;
        }

        Vector2 hostSize = panelHost.GetRect().Size;

        if (hostSize.X <= 0f || hostSize.Y <= 0f)
        {
            MainFile.Logger.Warn("Cannot fit popup background because PanelHost has no size.");
            return;
        }

        Vector2 targetSize = hostSize + BackgroundOverhang * 2f;

        backgroundControl.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);

        backgroundControl.Position = -BackgroundOverhang;
        backgroundControl.Size = VerticalPopupSize;
        backgroundControl.CustomMinimumSize = VerticalPopupSize;
        backgroundControl.PivotOffset = Vector2.Zero;

        backgroundControl.Scale = new Vector2(targetSize.X / VerticalPopupSize.X, targetSize.Y / VerticalPopupSize.Y);

        if (backgroundControl is CanvasItem canvasItem)
        {
            canvasItem.Visible = true;
        }

        if (CampfireTradeConfig.AllowSoloTrades)
        {
            MainFile.Logger.Info($"Fitted popup background. HostSize={hostSize}, TargetSize={targetSize}, Scale={backgroundControl.Scale}");
        }
    }
    
    private static void InstallHostedNode(Control host, Node node, Vector2 minimumSize, Control.MouseFilterEnum? mouseFilter)
    {
        ClearChildren(host);

        host.AddChild(node);

        if (node is not Control control)
        {
            return;
        }

        control.Position = Vector2.Zero;
        control.Rotation = 0f;
        control.Scale = Vector2.One;

        control.SetAnchorsPreset(Control.LayoutPreset.TopLeft, keepOffsets: false);

        control.OffsetLeft = 0f;
        control.OffsetTop = 0f;
        control.OffsetRight = 0f;
        control.OffsetBottom = 0f;

        control.CustomMinimumSize = minimumSize;
        control.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        control.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;

        if (mouseFilter.HasValue)
        {
            control.MouseFilter = mouseFilter.Value;
        }
    }
    
    private static void ClearChildren(Node node)
    {
        foreach (Node child in node.GetChildren())
        {
            child.QueueFree();
        }
    }
    
    private static bool TryShowGameBackstop()
    {
        if (NModalContainer.Instance == null)
        {
            return false;
        }

        if (NModalContainer.Instance.OpenModal != null)
        {
            MainFile.Logger.Warn("Trade popup could not use game backstop because another modal is already open.");
            return false;
        }

        NModalContainer.Instance.ShowBackstop();
        return true;
    }
    
    private static void Complete(CanvasLayer scene, TaskCompletionSource<bool> result, bool accepted, bool hideGameBackstop)
    {
        _isPopupOpen = false;

        if (!result.TrySetResult(accepted))
        {
            return;
        }

        if (hideGameBackstop)
        {
            NModalContainer.Instance?.HideBackstop();
        }

        if (GodotObject.IsInstanceValid(scene))
        {
            scene.QueueFree();
        }
    }
    
    private static void InstallEscapeBlocker(Control root, Action onCancel)
    {
        root.FocusMode = Control.FocusModeEnum.All;

        root.GuiInput += inputEvent =>
        {
            bool isCancel =
                inputEvent.IsActionPressed("ui_cancel") ||
                inputEvent is InputEventKey
                {
                    Pressed: true,
                    Echo: false,
                    Keycode: Key.Escape
                };

            if (!isCancel)
            {
                return;
            }

            root.AcceptEvent();
            root.GetViewport().SetInputAsHandled();

            onCancel();
        };
    }
    
    private static bool ConnectPopupButton(Node buttonNode, Action action)
    {
        try
        {
            if (buttonNode is NClickableControl clickableControl)
            {
                clickableControl.Released += _ => action();
                return true;
            }

            if (buttonNode is Button godotButton)
            {
                godotButton.Pressed += () => action();
                return true;
            }

            Callable callable = Callable.From(action);

            if (buttonNode.HasSignal(NClickableControl.SignalName.Released))
            {
                buttonNode.Connect(NClickableControl.SignalName.Released, callable);
                return true;
            }

            if (buttonNode.HasSignal(BaseButton.SignalName.Pressed))
            {
                buttonNode.Connect(BaseButton.SignalName.Pressed, callable);
                return true;
            }
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Failed to connect popup button {buttonNode.Name}: {ex}");
        }

        return false;
    }
    
    private static void SetPopupButtonText(Node buttonNode, string text)
    {
        SetNodeText(buttonNode, text);

        Node? labelNode = buttonNode.FindChild("Label", recursive: true, owned: false);

        if (labelNode != null)
        {
            SetNodeText(labelNode, text);
        }
    }
    
    private static void SetNodeText(Node node, string text)
    {
        try
        {
            PropertyInfo? textProperty = node.GetType().GetProperty("Text");

            if (textProperty != null &&
                textProperty.CanWrite &&
                textProperty.PropertyType == typeof(string))
            {
                textProperty.SetValue(node, text);
                return;
            }
        }
        catch
        {
            // Fall through to Godot property setter.
        }

        try
        {
            node.Set("text", text);
            return;
        }
        catch
        {
            // Fall through to uppercase variant.
        }

        try
        {
            node.Set("Text", text);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Warn($"Could not set text on node {node.Name} [{node.GetType().FullName}]: {ex}");
        }
    }
    
    private static void AddCardPreview(Control cardSlot, CardModel card)
    {
        NCard? cardNode = NCard.Create(card);

        if (cardNode == null)
        {
            MainFile.Logger.Warn($"Trade popup failed to create card preview for {card.Title}.");
            return;
        }

        cardSlot.AddChildSafely(cardNode);

        PileType visualPileType = card.Pile?.Type ?? PileType.Deck;
        cardNode.UpdateVisuals(visualPileType, CardPreviewMode.Normal);

        cardNode.Scale = Vector2.One * CardScale;
        cardNode.MouseFilter = Control.MouseFilterEnum.Ignore;

        Vector2 slotSize = cardSlot.GetRect().Size;

        cardNode.Position = new Vector2(slotSize.X * 0.5f, slotSize.Y * 0.5f);

        MainFile.Logger.Info($"Showing trade request popup card preview for {card.Title}.");
    }
    
    private static string GetPlayerName(Player player)
    {
        try
        {
            return PlatformUtil.GetPlayerName(RunManager.Instance.NetService.Platform, player.NetId);
        }
        catch
        {
            // Debug/singleplayer fallback.
        }

        return $"Player {player.NetId}";
    }
}