using BaseLib.Config;

namespace CampfireTrade.CampfireTradeCode.Config;

[ConfigHoverTipsByDefault]
public class CampfireTradeConfig : SimpleModConfig
{
    [ConfigSection("GeneralSettings")]
    public static bool UnlimitedTrades { get; set; } = false;
    
    [ConfigSection("DebugSettings")]
    public static bool EnableDebug { get; set; } = false;
    [ConfigVisibleIf(nameof(EnableDebug))]
    public static bool AllowSoloTrades { get; set; } = false;
}