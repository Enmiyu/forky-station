using System.Diagnostics.CodeAnalysis;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.Timing;

namespace Content.Shared._Impstation.PersonalEconomy.Systems;

/// <summary>
/// The main banking system; handles all funds transfers and keeps track of bank accounts etc etc
/// </summary>
public abstract class SharedBankingSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = null!;
    [Dependency] private readonly IGameTiming _timing = null!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = null!;
    [Dependency] private readonly SharedHandsSystem _hands = null!;

    //lookup so we're not full-scanning every transaction
    private readonly Dictionary<int, EntityUid> _accountsByAccess = new();
    private readonly Dictionary<int, EntityUid> _accountsByTransfer = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<BankCardComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ATMComponent, RequestTransactionMessage>(OnTransactionRequested);
        SubscribeLocalEvent<ATMComponent, InsertCardMessage>(OnInsertCardRequested);
        SubscribeLocalEvent<ATMComponent, EjectCardMessage>(OnEjectCardRequested);
        SubscribeLocalEvent<PosSystemComponent, UpdatePoSSettingsMessage>(OnPoSSettingsUpdate);
        SubscribeLocalEvent<PosSystemComponent, PoSTransactionSuccededMessage>(OnTransactionSucceded);
        SubscribeLocalEvent<PosSystemComponent, PoSTransactionFailedMessage>(OnTransactionFailed);

        SubscribeLocalEvent<BankAccountComponent, ComponentStartup>(OnAccountStartup);
        SubscribeLocalEvent<BankAccountComponent, ComponentShutdown>(OnAccountShutdown);
    }

    private void OnAccountStartup(Entity<BankAccountComponent> ent, ref ComponentStartup args)
    {
        _accountsByAccess[ent.Comp.AccessNumber] = ent;
        _accountsByTransfer[ent.Comp.TransferNumber] = ent;
    }

    private void OnAccountShutdown(Entity<BankAccountComponent> ent, ref ComponentShutdown args)
    {
        _accountsByAccess.Remove(ent.Comp.AccessNumber);
        _accountsByTransfer.Remove(ent.Comp.TransferNumber);
    }

    /// <summary>
    /// makes sure cache is insync
    /// </summary>
    protected void ReindexAccount(Entity<BankAccountComponent> ent, AccessNumber oldAccess, TransferNumber oldTransfer)
    {
        if (oldAccess != ent.Comp.AccessNumber)
        {
            _accountsByAccess.Remove(oldAccess);
            _accountsByAccess[ent.Comp.AccessNumber] = ent;
        }

        if (oldTransfer != ent.Comp.TransferNumber)
        {
            _accountsByTransfer.Remove(oldTransfer);
            _accountsByTransfer[ent.Comp.TransferNumber] = ent;
        }
    }

    private void OnTransactionFailed(Entity<PosSystemComponent> ent, ref PoSTransactionFailedMessage args)
    {
        //todo implement this : have the pos system put out a "transaction failed" signal
    }

    private void OnTransactionSucceded(Entity<PosSystemComponent> ent, ref PoSTransactionSuccededMessage args)
    {
        //todo make this put out a "transaction succeded" signal
        //customer pays with the card in their hand
        if (!TryGetHeldCard(args.Actor, out var card))
            return;

        TryMakeTransaction(card.Comp.AccessNumber, ent.Comp.RecipientAccount, ent.Comp.Amount, ent.Comp.Reason);
    }

    private void OnPoSSettingsUpdate(Entity<PosSystemComponent> ent, ref UpdatePoSSettingsMessage args)
    {
        ent.Comp.RecipientAccount = args.Recipient;
        ent.Comp.Amount = args.Amount;
        ent.Comp.Reason = args.Reason;

        Dirty(ent);
    }

    private void OnTransactionRequested(Entity<ATMComponent> ent, ref RequestTransactionMessage args)
    {
        var cardUid = _itemSlots.GetItemOrNull(ent, ent.Comp.CardSlotId);
        if (cardUid == null || !TryComp<BankCardComponent>(cardUid, out var card))
            return;

        TryMakeTransaction(card.AccessNumber, args.RecipientAccount, args.Amount, args.Reason);
    }

    private void OnInsertCardRequested(Entity<ATMComponent> ent, ref InsertCardMessage args)
    {
        if (!TryGetHeldCard(args.Actor, out var card))
            return;

        _itemSlots.TryInsert(ent, ent.Comp.CardSlotId, card.Owner, args.Actor);
    }

    private void OnEjectCardRequested(Entity<ATMComponent> ent, ref EjectCardMessage args)
    {
        // pickup into the player's hand or just falls on floor lol
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.CardSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor);
    }

    // they gotta actually hold the card in their hand
    private bool TryGetHeldCard(EntityUid actor, out Entity<BankCardComponent> card)
    {
        card = default;
        foreach (var held in _hands.EnumerateHeld(actor))
        {
            if (TryComp<BankCardComponent>(held, out var comp))
            {
                card = (held, comp);
                return true;
            }
        }
        return false;
    }

    public bool TryMakeTransaction(AccessNumber sender, TransferNumber recipient, int amount, string reason)
    {
        //todo need to do something for if a transaction becomes invalid after a client confirms it
        if (!VerifyTransaction(sender, recipient, amount))
            return false;

        MakeTransaction(sender, recipient, amount, reason);
        return true;

    }

    private bool VerifyTransaction(AccessNumber sender, TransferNumber recipient, int amount)
    {
        //no Negative Cheese
        if (amount <= 0)
            return false;

        //return false if neither account exists
        if (!TryGetAccount(sender, out var senderAccount) ||
            !TryGetAccountFromTransferNumber(recipient, out var recipientAccount))
            return false;

        //return true if the sender has enough money
        return senderAccount.Value.Comp.Balance >= amount;
    }

    private void MakeTransaction(AccessNumber sender, TransferNumber recipient, int amount, string reason)
    {
        //this should always be true by the time this gets called but
        //could make these out variables from the verify method, maybe?
        //will matter less when I've got a proper cache in buuuuuut
        if (!TryGetAccount(sender, out var senderAccount) || !TryGetAccountFromTransferNumber(recipient, out var recipientAccount))
            return;

        //adjust balances
        senderAccount.Value.Comp.Balance -= amount;
        recipientAccount.Value.Comp.Balance += amount;

        //add transactions!
        AddTransaction(senderAccount.Value, recipientAccount.Value.Comp.Name, -amount, recipient, reason);
        AddTransaction(recipientAccount.Value, senderAccount.Value.Comp.Name, amount, senderAccount.Value.Comp.TransferNumber, reason);
    }

    private void AddTransaction(Entity<BankAccountComponent> account, string otherName, int amount, int from, string reason)
    {
        //limit reason length
        if (reason.Length > 64) //todo make this a cvar
            reason = reason[..64];

        var timestamp = _timing.CurTime.TotalSeconds;
        var transaction = new BankTransaction(from, otherName, amount, timestamp, reason);
        account.Comp.Transactions.Insert(0, transaction);
        //keep at most 10
        while (account.Comp.Transactions.Count > 10) //todo make this a cvar
        {
            account.Comp.Transactions.RemoveAt(account.Comp.Transactions.Count - 1);
        }

        Dirty(account);
    }

    private void OnExamined(Entity<BankCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        args.PushMarkup(Loc.GetString("bank-card-examine-access-number", ("number", $"{ent.Comp.AccessNumber.Number:000000}")),4);
        args.PushMarkup(Loc.GetString("bank-card-examine-transfer-number", ("number", $"{ent.Comp.TransferNumber.Number:0000}")),4);

        if (!TryGetAccount(ent.Comp.AccessNumber, out var account))
            return;

        args.PushMarkup("The two below are for testing!", 3);
        args.PushMarkup(Loc.GetString("bank-card-examine-balance", ("balance", account.Value.Comp.Balance)), 2); //todo remove this
        args.PushMarkup(Loc.GetString("bank-card-examine-salary", ("salary", account.Value.Comp.Salary)), 1); //todo remove this
    }

    public bool TryGetAccountFromTransferNumber(TransferNumber transferNumber, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (!_accountsByTransfer.TryGetValue(transferNumber, out var uid))
            return false;
        if (!TryComp<BankAccountComponent>(uid, out var comp))
            return false;
        account = (uid, comp);
        return true;
    }

    public bool TryGetAccount(AccessNumber accessNumber, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        if (!_accountsByAccess.TryGetValue(accessNumber, out var uid))
            return false;
        if (!TryComp<BankAccountComponent>(uid, out var comp))
            return false;
        account = (uid, comp);
        return true;
    }

    /// <summary>
    /// Set the name for an account
    /// </summary>
    /// <param name="accessNumber"></param>
    /// <param name="name"></param>
    public virtual void SetAccountName(AccessNumber accessNumber, string name)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Value.Comp.Name = name;
        Dirty(account.Value);
    }

    public virtual void SetAccountSalary(AccessNumber accessNumber, int salary)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Value.Comp.Salary = salary;
        Dirty(account.Value);
    }

    public virtual void SetAccountBalance(AccessNumber accessNumber, int balance)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        account.Value.Comp.Balance = balance;
        Dirty(account.Value);
    }

    /// <summary>
    /// Update the details on a bank card to reflect the details of a given account.
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void UpdateCardDetails(Entity<BankCardComponent> card, AccessNumber accessNumber)
    {
        if (!TryGetAccount(accessNumber, out var account))
            return;

        SetCardName(card, account.Value.Comp.Name);
        SetCardNumber(card, account.Value.Comp.AccessNumber);
    }

    /// <summary>
    /// Set the name on a card
    /// </summary>
    /// <param name="card"></param>
    /// <param name="name"></param>
    public virtual void SetCardName(Entity<BankCardComponent> card, string name)
    {
        card.Comp.Name = name;
        _metaData.SetEntityName(card, Loc.GetString(card.Comp.NamedLocId, ("name", name)));
        Dirty(card);
    }

    /// <summary>
    /// set the number on a card
    /// </summary>
    /// <param name="card"></param>
    /// <param name="accessNumber"></param>
    public virtual void SetCardNumber(Entity<BankCardComponent> card, AccessNumber accessNumber)
    {
        card.Comp.AccessNumber = accessNumber;
        Dirty(card);
    }
}
