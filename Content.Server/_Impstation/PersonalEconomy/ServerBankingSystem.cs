using Content.Server.StationRecords.Systems;
using Content.Shared._Impstation.PersonalEconomy;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.PDA;
using Content.Shared.Roles;
using Content.Shared.Station;
using Content.Shared.StationRecords;
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
    [Dependency] private IPrototypeManager _proto = null!;
    [Dependency] private InventorySystem _inventory = null!;

    private readonly EntProtoId _bankAccountProto = "BankAccount";

    // job, stacked salary
    private readonly Dictionary<string, int> _salaryByJob = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankCardComponent, ComponentInit>(OnComponentInit);
        // after StationRecordsSystem so the record key is already stamped onto the PDA
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete, after: [typeof(StationRecordsSystem)]);

        PopulateSalaries();
    }

    // link the account to its owner's station record so payroll can read criminal status.
    // the record key lives on the PDA in the id slot, which is also where the bank card is
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        if (!_inventory.TryGetSlotEntity(ev.Mob, "id", out var idUid))
            return;

        if (!TryComp<StationRecordKeyStorageComponent>(idUid, out var keyStorage) || keyStorage.Key is not { } key)
            return;

        if (!TryComp<PdaComponent>(idUid, out var pda)
            || pda.BankCardSlot.ContainerSlot?.ContainedEntity is not { } card
            || !TryComp<BankCardComponent>(card, out var bankCard))
            return;

        if (TryGetAccount(bankCard.AccessNumber, out var account))
        {
            account.Value.Comp.StationRecordId = key.Id;
        }
    }

    // sums every PaymentSalaryPrototype a job appears in (e.g. base wage + hazard pay)
    private void PopulateSalaries()
    {
        _salaryByJob.Clear();
        foreach (var proto in _proto.EnumeratePrototypes<PaymentSalaryPrototype>())
        {
            foreach (var role in proto.Roles)
            {
                _salaryByJob[role] = _salaryByJob.GetValueOrDefault(role.Id) + proto.Salary;
            }
        }
    }

    /// <summary>
    /// Stacked salary for a job, or 0 if no salary prototype covers it.
    /// </summary>
    public int GetSalaryForJob(string jobId)
    {
        return _salaryByJob.GetValueOrDefault(jobId);
    }

    //todo should this be a different event?
    private void OnComponentInit(Entity<BankCardComponent> ent, ref ComponentInit args)
    {
        // blank cards stay account-less until written at a console
        if (!ent.Comp.AutoCreateAccount)
            return;

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

    public Entity<BankAccountComponent> CreateNewAccount(string name, EntityUid? parent)
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
