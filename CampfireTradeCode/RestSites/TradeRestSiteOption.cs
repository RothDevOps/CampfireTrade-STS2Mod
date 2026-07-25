using CampfireTrade.CampfireTradeCode.Config;
using CampfireTrade.CampfireTradeCode.Trading;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.RestSite;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Nodes.Multiplayer;

namespace CampfireTrade.CampfireTradeCode.RestSites;

public class TradeRestSiteOption(Player owner) : RestSiteOption(owner)
{
    public override string OptionId => "TRADE";
    
    public override bool IsEnabled => CampfireTradeConfig.AllowSoloTrades || Owner.RunState.Players.Count > 1;
    
    public override IEnumerable<string> AssetPaths => base.AssetPaths.Concat(NGenericPopup.AssetPaths);
    
    public override LocString Description
    {
        get
        {
            if (IsEnabled)
                return new LocString("rest_site_ui", "OPTION_" + OptionId + ".description");

            return new LocString("rest_site_ui", "OPTION_" + OptionId + ".descriptionDisabled");
        }
    }

    public override Task<bool> OnSelect()
    {
        if (CampfireTradeConfig.AllowSoloTrades)
        {
            return TradeFlow.DebugShowSelfTradeRequest(Owner);
        }

        return TradeFlow.Start(this, Owner);
    }
    
    public override Task DoLocalPostSelectVfx(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
    
    public override Task DoRemotePostSelectVfx()
    {
        return Task.CompletedTask;
    }
}