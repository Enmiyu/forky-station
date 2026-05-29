using Content.Shared._Impstation.PersonalEconomy.Components;
using Robust.Shared.Serialization;

namespace Content.Shared._Impstation.PersonalEconomy.Events;

// all sent from the account management console, targeting an account by access number

[Serializable, NetSerializable]
public sealed class SetAccountStatusMessage(AccessNumber account, PaymentStatus status, string reason) : BoundUserInterfaceMessage
{
    public AccessNumber Account = account;
    public PaymentStatus Status = status;
    public string Reason = reason;
}

[Serializable, NetSerializable]
public sealed class SetAccountSalaryMessage(AccessNumber account, int salary) : BoundUserInterfaceMessage
{
    public AccessNumber Account = account;
    public int Salary = salary;
}

[Serializable, NetSerializable]
public sealed class GrantAccountBonusMessage(AccessNumber account, int amount) : BoundUserInterfaceMessage
{
    public AccessNumber Account = account;
    public int Amount = amount;
}

// writes the given account's details onto the card currently in the console slot
[Serializable, NetSerializable]
public sealed class WriteCardMessage(AccessNumber account) : BoundUserInterfaceMessage
{
    public AccessNumber Account = account;
}

// bulk actions over every account in a department

[Serializable, NetSerializable]
public sealed class SetDepartmentStatusMessage(string department, PaymentStatus status, string reason) : BoundUserInterfaceMessage
{
    public string Department = department;
    public PaymentStatus Status = status;
    public string Reason = reason;
}

[Serializable, NetSerializable]
public sealed class GrantDepartmentBonusMessage(string department, int amount) : BoundUserInterfaceMessage
{
    public string Department = department;
    public int Amount = amount;
}
