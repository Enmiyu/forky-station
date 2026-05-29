using Content.Shared._Impstation.PersonalEconomy.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Events;

/// <summary>
/// Request that a transaction attempt is made.
/// </summary>
/// <param name="recipientAccount"> the recipient's account transfer number</param>
/// <param name="amount"> the amount of money to be transferred</param>
/// <param name="reason"> the reason for the transfer</param>
[Serializable, NetSerializable]
public sealed class RequestTransactionMessage(TransferNumber recipientAccount, int amount, string reason)
    : BoundUserInterfaceMessage
{
    public TransferNumber RecipientAccount = recipientAccount;
    public int Amount = amount;
    public string Reason = reason;
}
