using Content.Shared.DeviceLinking;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// This stores the destination account, charge & reason for a PoS system.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class PosSystemComponent : Component
{
    [AutoNetworkedField]
    public AccountNumber RecipientAccount = 0;

    [AutoNetworkedField]
    public int Amount = 0;

    [AutoNetworkedField]
    public string Reason = "";

    // device-link ports pulsed on a sale, so the POS can be wired to stuff (imagine a bomb lol)
    [DataField]
    public ProtoId<SourcePortPrototype> SuccessPort = "POSTransactionSucceeded";

    [DataField]
    public ProtoId<SourcePortPrototype> FailPort = "POSTransactionFailed";
}
