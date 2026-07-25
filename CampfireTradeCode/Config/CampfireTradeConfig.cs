using BaseLib.Config;

namespace CampfireTrade.CampfireTradeCode.Config;

[ConfigHoverTipsByDefault]
public class CampfireTradeConfig : SimpleModConfig
{
    private static bool _enableDebug = false;

    [ConfigSection("GeneralSettings")]
    public static bool UnlimitedTrades { get; set; } = false;
    
    [ConfigSection("DebugSettings")]
    public static bool EnableDebug
    {
        get => _enableDebug;
        set
        {
            _enableDebug = value;

            if (!_enableDebug)
            {
                AllowSoloTrades = false;
            }
        }
    }
    
    [ConfigVisibleIf(nameof(EnableDebug))]
    public static bool AllowSoloTrades { get; set; } = false;
    
    [ConfigIgnore]
    public static bool AllowSoloTradePopup => EnableDebug && AllowSoloTrades;
}