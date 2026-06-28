using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CampfireTrade.CampfireTradeCode.Trading;

public sealed class TradeOffer
{
    public required Player Initiator { get; init; }
    public required Player Target { get; init; }
    public required CardModel RequestedCard { get; init; }

    // public int GoldCost { get; init; } = 0;
}