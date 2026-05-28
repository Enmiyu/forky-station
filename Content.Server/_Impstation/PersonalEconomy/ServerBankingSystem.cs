using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Systems;
using Content.Shared.Station;
using Robust.Server.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Impstation.PersonalEconomy;

/// <summary>
/// This handles...
/// </summary>
public sealed class ServerBankingSystem : SharedBankingSystem
{
    [Dependency] private SharedTransformSystem _xform = null!;
    [Dependency] private PvsOverrideSystem _pvsOverride = null!;
    [Dependency] private IRobustRandom _random = null!;
    [Dependency] private SharedStationSystem _station = null!;

    private readonly EntProtoId _bankAccountProto = "BankAccount";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankCardComponent, ComponentInit>(OnComponentInit);
    }

    //todo should this be a different event?
    private void OnComponentInit(Entity<BankCardComponent> ent, ref ComponentInit args)
    {
        SetupID(ent);
    }

    private void SetupID(Entity<BankCardComponent> ent)
    {
        var account = CreateNewAccount("Unknown", ResolveAccountParent(ent));
        ent.Comp.AccessNumber = account.Comp.AccessNumber;
        ent.Comp.TransferNumber = account.Comp.TransferNumber;
        SetAccountSalary(account.Comp.AccessNumber, ent.Comp.Salary);
        SetAccountBalance(account.Comp.AccessNumber, ent.Comp.StartingBalance);
        Dirty(ent);
    }

    // accounts get parented to the station so they're cleaned up with it
    // i didnt like the _cheeseWorld sorry
    private EntityUid? ResolveAccountParent(EntityUid source)
    {
        var owning = _station.GetOwningStation(source);
        if (owning != null)
            return owning;

        //fallback: any station. if there isn't one, leave it on the source's current parent
        var stations = _station.GetStations();
        return stations.Count > 0 ? stations[0] : null;
    }

    private Entity<BankAccountComponent> CreateNewAccount(string name, EntityUid? parent)
    {
        //generate a unique ID
        var accountNo = _random.Next(1, 1000000);
        while (TryGetAccount(accountNo, out _))
        {
            accountNo = _random.Next(1, 1000000);
        }

        //generate a unique transfer number
        var transferNo = _random.Next(1, 10000);
        while (TryGetAccountFromTransferNumber(transferNo, out _))
        {
            transferNo = _random.Next(1, 10000);
        }

        var newAccount = Spawn(_bankAccountProto);
        if (parent != null)
            _xform.SetParent(newAccount, parent.Value);
        //probably not *great*, but every client needs to know about every bank account at all times because of the way this whole system is set up
        //bank accounts are relatively small (3 comps - xform, meta, bankacc) entities so it's probably fine?
        //there'll also be like, maybe a hundred in a round max? if traitors are Doing Some Shit?

        // TAYDEO NOTE:
        // maybe they dont? original funky bank code just relied on a manual look up at an ATM, since clients didnt
        // really need to know anything? maybe go back to this architecture? We'll See.
        _pvsOverride.AddForceSend(newAccount);

        //create new account
        var bankComp = Comp<BankAccountComponent>(newAccount);

        var oldAccess = bankComp.AccessNumber;
        var oldTransfer = bankComp.TransferNumber;
        bankComp.AccessNumber = accountNo;
        bankComp.TransferNumber = transferNo;
        bankComp.Name = name;
        ReindexAccount((newAccount, bankComp), oldAccess, oldTransfer);

        //and send the comp back off to the client
        Dirty<BankAccountComponent>((newAccount, bankComp));
        return (newAccount, bankComp);
    }
}
