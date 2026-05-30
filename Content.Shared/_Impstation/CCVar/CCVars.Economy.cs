using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

public sealed partial class CCVars
{
    /// <summary>
    ///     If command can cashout the stations scrip for spesos
    /// </summary>
    public static readonly CVarDef<bool> ScripStationCashout =
        CVarDef.Create("economy.scrip_station_cashout", true, CVar.REPLICATED);
}
