using Content.Client.CharacterInfo;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Systems;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._Impstation.PersonalEconomy;

public sealed class ClientBankingSystem : SharedBankingSystem
{
    [Dependency] private readonly InventorySystem _inventory = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CharacterInfoSystem.GetCharacterInfoControlsEvent>(OnGetCharacterInfoControls);
    }

    // adds the players account number and pin to character info so they always have it
    private void OnGetCharacterInfoControls(ref CharacterInfoSystem.GetCharacterInfoControlsEvent ev)
    {
        if (GetOwnedAccount(ev.Entity) is not { } account)
            return;

        var box = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Margin = new Thickness(0, 6, 0, 0),
        };
        box.AddChild(new Label
        {
            Text = Loc.GetString("bank-character-heading"),
            StyleClasses = { "LabelHeading" },
        });
        box.AddChild(new Label
        {
            Text = Loc.GetString("bank-character-account", ("number", $"{account.Comp.AccountNumber.Number:000000}")),
        });
        box.AddChild(new Label
        {
            Text = Loc.GetString("bank-character-pin", ("pin", $"{account.Comp.Pin.Number:0000}")),
        });

        ev.Controls.Add(box);
    }

    // finds the players account via the bank card in their PDA
    private Entity<BankAccountComponent>? GetOwnedAccount(EntityUid player)
    {
        if (!_inventory.TryGetSlotEntity(player, "id", out var idUid))
            return null;
        if (!TryComp<PdaComponent>(idUid, out var pda)
            || pda.BankCardSlot.ContainerSlot?.ContainedEntity is not { } card
            || !TryComp<BankCardComponent>(card, out var bankCard))
            return null;

        return TryGetAccount(bankCard.AccountNumber, out var account) ? account : null;
    }
}
