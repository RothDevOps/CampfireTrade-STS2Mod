using Godot;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CampfireTrade.CampfireTradeCode.Trading;

public class TradeExecutor
{
    public static Task<bool> Execute(TradeOffer offer)
    {
        CardPile targetDeck = PileType.Deck.GetPile(offer.Target);
        CardPile initiatorDeck = PileType.Deck.GetPile(offer.Initiator);
        CardModel targetCard = offer.RequestedCard;
        
        // if (!CanPayCost(offer))
        // {
        //     MainFile.Logger.Warn("Trade failed: initiator cannot pay cost.");
        //     return Task.FromResult(false);
        // }

        if (!targetDeck.Cards.Contains(targetCard))
        {
            MainFile.Logger.Warn(
                $"Trade failed: target deck no longer contains card {offer.RequestedCard.Id.Entry}"
            );
            return Task.FromResult(false);
        }
        
        int originalTargetIndex = targetDeck.Cards
            .ToList()
            .IndexOf(targetCard);

        if (originalTargetIndex < 0)
        {
            return Task.FromResult(false);
        }

        try
        {
            targetDeck.RemoveInternal(targetCard);
            
            try
            {
                // Important:
                // CardPile.AddInternal does not change CardModel.Owner.
                // CardModel.Owner blocks direct non-null -> non-null transfer,
                // so we clear it first, then assign the new owner.
                targetCard.Owner = null!;
                targetCard.Owner = offer.Initiator;
                
                initiatorDeck.AddInternal(targetCard);
                initiatorDeck.InvokeCardAddFinished();
            }
            catch (Exception addException)
            {
                MainFile.Logger.Error($"[CampfireTrade] Failed to add traded card to initiator deck: {addException}");
                
                // Best-effort rollback.
                if (initiatorDeck.Cards.Contains(targetCard))
                {
                    initiatorDeck.RemoveInternal(targetCard);
                }
                
                targetCard.Owner = null!;
                targetCard.Owner = offer.Target;
                
                if (!targetDeck.Cards.Contains(targetCard))
                {
                    targetDeck.AddInternal(targetCard, originalTargetIndex);
                    targetDeck.InvokeCardAddFinished();
                }

                return Task.FromResult(false);
            }
            
            //PayCost(offer);
            
            MainFile.Logger.Info(
                $"Trade completed: player {offer.Initiator.NetId} received {offer.RequestedCard.Id.Entry} from player {offer.Target.NetId}"
            );
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            MainFile.Logger.Error($"Trade execution failed: {ex}");
            return Task.FromResult(false);
        }
    }
    
    // public static bool CanPayCost(TradeOffer offer)
    // {
    //     // TODO: Add real cost later
    //     return offer.GoldCost <= 0;
    // }
    
    // private static void PayCost(TradeOffer offer)
    // {
    //     // TODO: No cost yet add later
    //     // subtract gold from initiator
    //     // add gold to target
    // }
}