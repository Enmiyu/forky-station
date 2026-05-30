using Content.Client._Impstation.PersonalEconomy.UI.POS;
using Content.Shared.CCVar;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._Impstation.PersonalEconomy.BUI;

public sealed class POSBoundUserInterface : BoundUserInterface
{

    private ClientBankingSystem _banking;
    private readonly IConfigurationManager _cfg;
    private PoSMenu? _menu;
    private TipMenu? _tipMenu;

    public POSBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _banking = EntMan.System<ClientBankingSystem>();
        _cfg = IoCManager.Resolve<IConfigurationManager>();
    }

    // shows tip on purchase lol
    private void OpenTipMenu(int subtotal)
    {
        _tipMenu?.Close();
        _tipMenu = new TipMenu();
        _tipMenu.SetSubtotal(subtotal);
        _tipMenu.OnTipChosen += amount =>
        {
            if (amount > 0)
                SendPredictedMessage(new PoSTipMessage(amount));
            Close();
        };
        _tipMenu.OpenCentered();
    }

    //why does doing any UI work make me feel like I've been kicked in the head by a horse
    //abandon hope, all ye who enter here

    //todo need to make this allow for negative charge amounts so that pawn shops can exist? or do I want to force them to go through cash only?

    //ok I'm writing out a flowchart for this since I can't keep it all in my head
    //UI opened
        //if we don't have a recipient account or the user is the recipient, create a setup box
            //if the user isn't holding a card, tell them to present one or enter a number
            //if they are, give them the big ol' setup button
                //setup button pressed, hide the button & label, show the proper setup dialogue - this can just go in the setup box
                    //confirm button pressed, set everything up on the comp, keep the window open?
        //if we have a recipient, create a payment box
            //if the user isn't holding a card, tell them to present one or enter a number
            //if they are, give them the payment dialogue

    protected override void Open()
    {
        base.Open();

        var comp = EntMan.GetComponent<PosSystemComponent>(Owner);
        var hasRecipient = comp.RecipientAccount != 0;

        _menu = this.CreateWindow<PoSMenu>();

        // a configured POS shows the customer a charge to pay; an unconfigured one needs the merchant to unlock & set it up
        if (hasRecipient)
            ShowCustomerBox();
        else
            ShowLockBox();

        _menu.OnNumberEntered += s =>
        {
            // on the lock screen the keypad is the merchant's PIN entry
            if (_menu!.LockMode)
            {
                if (int.TryParse(s, out var pin))
                    SendPredictedMessage(new UnlockPosMessage(pin));
                return;
            }

            // the customer presents a card to pay a configured charge - setup is behind the PIN lock, not the keypad
            var localComp = EntMan.GetComponent<PosSystemComponent>(Owner);
            if (localComp.RecipientAccount == 0)
                return;

            //if an invalid number was entered
            if (!int.TryParse(s, out var userAccount) || !_banking.TryGetAccount(userAccount, out var account))
            {
                ShowCustomerBox();
                return;
            }

            //the recipient can't pay themselves
            if (localComp.RecipientAccount == account.Value.Comp.AccountNumber)
                return;

            //we have a recipient and a valid number, set up the payment menu
            var box = _menu.CreatePaymentBox();
            _banking.TryGetAccount(localComp.RecipientAccount, out var recipient);
            //just assume that the bank account will not be null at this point. god help me for when I get around to deleting accounts (:
            //todo make this whole UI account for the fact that these accounts could all get deleted at some point
            //todo also do that for the other one
            var merchantName = string.IsNullOrWhiteSpace(localComp.MerchantName) ? recipient!.Value.Comp.Name : localComp.MerchantName;
            var tax = _banking.PosTaxFor(localComp.Amount);
            box.FillOutDetails(merchantName, recipient!.Value.Comp.AccountNumber, localComp.Amount, tax, _cfg.GetCVar(CCVars.PosTax), localComp.Reason);

            //cancel aborts the sale; closing avoids the held card instantly re-presenting itself and bouncing back here
            box.TransactionCancelled += Close;

            box.TransactionConfirmed += () =>
            {
                // ok, so, we want to do a transaction finally
                // the customer needs to cover the subtotal plus tax
                if (!VerifyTransaction(localComp.RecipientAccount, userAccount, localComp.Amount + tax))
                {
                    box.NoFundsLabel.Visible = true;
                    SendPredictedMessage(new PoSTransactionFailedMessage());
                }
                else
                {
                    box.NoFundsLabel.Visible = false;
                    SendPredictedMessage(new PoSTransactionSuccededMessage());
                    //the sales done, let them tip!!
                    _menu!.Visible = false;
                    OpenTipMenu(localComp.Amount);
                }
            };
        };

        _menu.OnClearButtonPressed += () =>
        {
            if (EntMan.GetComponent<PosSystemComponent>(Owner).RecipientAccount != 0)
                ShowCustomerBox();
        };
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        if (message is PosUnlockedMessage)
            ShowSetupBox();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _tipMenu?.Dispose();
    }

    private void ShowCustomerBox()
    {
        var box = _menu!.CreateInvalidPaymentBox();
        box.MerchantPressed += ShowLockBox;
    }

    private void ShowLockBox()
    {
        var claimed = EntMan.GetComponent<PosSystemComponent>(Owner).OwnerAccount != 0;
        _menu!.CreateLockBox(claimed);
    }

    private void ShowSetupBox()
    {
        var localComp = EntMan.GetComponent<PosSystemComponent>(Owner);
        var box = _menu!.CreateSetupBox();

        //prefill from the comp if it's already configured, otherwise seed the recipient with the owner's account
        if (localComp.RecipientAccount != 0)
            box.FillOutDetails(localComp.RecipientAccount, localComp.Amount, localComp.Reason, localComp.MerchantName);
        else if (localComp.OwnerAccount != 0)
            box.TransferNoEntryBox.Text = $"{localComp.OwnerAccount.Number:000000}";

        box.OnSetupCleared += () =>
        {
            SendPredictedMessage(new UpdatePoSSettingsMessage(0, 0, "", ""));
            ShowLockBox();
        };

        box.OnSetupConfirmed += () =>
        {
            var valid = true;
            //if the recipient doesn't exist, say what's going wrong and mark this as invalid
            if (!VerifyRecipient(box.TransferNoEntryBox.Text, out var number))
            {
                box.InvalidRecipientLabel.Visible = true;
                valid = false;
            }

            //if the transfer amount is 0, say what's going on and mark this as invalid
            if (box.TransferAmount == 0)
            {
                box.InvalidTransferAmountLabel.Visible = true;
                valid = false;
            }

            if (!valid)
            {
                box.SetupConfirmedLabel.Visible = false;
                return;
            }

            box.InvalidRecipientLabel.Visible = false;
            box.InvalidTransferAmountLabel.Visible = false;
            box.SetupConfirmedLabel.Visible = true;

            var amount = box.TransferAmount;
            var reason = box.TransferReasonEntryBox.Text;

            SendPredictedMessage(new UpdatePoSSettingsMessage(number, amount, reason, box.MerchantNameEntryBox.Text));
        };
    }

    //todo these should probably be in a helpers file?
    private bool VerifyRecipient(string recipient, out int recipientNumber)
    {
        recipientNumber = 0;

        var rightLength = recipient.Length == 6;
        if (!rightLength)
            return false;

        var isInt = int.TryParse(recipient, out var number);
        if (!isInt)
            return false;

        var exists = _banking.TryGetAccount(number, out _);
        if (!exists)
            return false;

        recipientNumber = number;
        return true;
    }

    private bool VerifyTransaction(int recipient, int sender, int amount)
    {
        if (!_banking.TryGetAccount(recipient, out _) ||
            !_banking.TryGetAccount(sender, out var senderAcc) ||
            !(senderAcc.Value.Comp.Balance > amount))
            return false;

        return true;
    }
}
