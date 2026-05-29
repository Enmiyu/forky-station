namespace Content.Shared._Impstation.PersonalEconomy.Components;

/// <summary>
/// This is used to mark an entity as an account management console
/// </summary>
[RegisterComponent]
public sealed partial class AccountManagementConsoleComponent : Component
{
    // slot a card goes in so its banking details can be (re)written
    [DataField]
    public string CardSlotId = "card_slot";
}
