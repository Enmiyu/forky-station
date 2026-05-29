using Content.Server.Stack;
using Content.Shared._Impstation.PersonalEconomy.Components;
using Content.Shared._Impstation.PersonalEconomy.Events;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Stacks;
using Robust.Shared.Prototypes;

namespace Content.Server._Impstation.PersonalEconomy;

/// <summary>
/// Converts spesos into scrip and vice versa. Meant to be brutal.
/// </summary>
public sealed class CurrencyExchangeSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = null!;
    [Dependency] private readonly SharedHandsSystem _hands = null!;
    [Dependency] private readonly StackSystem _stack = null!;

    private static readonly ProtoId<StackPrototype> Spesos = "Credit";
    private static readonly ProtoId<StackPrototype> Scrip = "Scrip";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CurrencyExchangeComponent, InsertCashMessage>(OnInsertCash);
        SubscribeLocalEvent<CurrencyExchangeComponent, EjectCashMessage>(OnEjectCash);
        SubscribeLocalEvent<CurrencyExchangeComponent, ConvertCurrencyMessage>(OnConvert);
    }

    private void OnInsertCash(Entity<CurrencyExchangeComponent> ent, ref InsertCashMessage args)
    {
        // grab the first cheese stack in player hands
        foreach (var held in _hands.EnumerateHeld(args.Actor))
        {
            if (!TryComp<StackComponent>(held, out var stack) || !IsCurrency(stack.StackTypeId))
                continue;

            _itemSlots.TryInsert(ent, ent.Comp.CashSlotId, held, args.Actor);
            break;
        }
    }

    private void OnEjectCash(Entity<CurrencyExchangeComponent> ent, ref EjectCashMessage args)
    {
        if (!_itemSlots.TryGetSlot(ent, ent.Comp.CashSlotId, out var slot))
            return;

        _itemSlots.TryEjectToHands(ent, slot, args.Actor);
    }

    private void OnConvert(Entity<CurrencyExchangeComponent> ent, ref ConvertCurrencyMessage args)
    {
        var cashUid = _itemSlots.GetItemOrNull(ent, ent.Comp.CashSlotId);
        if (cashUid == null || !TryComp<StackComponent>(cashUid, out var stack))
            return;

        if (!TryGetOutput(stack.StackTypeId, out var output))
            return;

        // tax skimmed off, rest paid out in the other currency
        // tax just goes but idk if it should go into the appropriate station bank acc
        // actually yknwo what fuck it i WILL do that but later
        // todo make it pay into appropriate acc
        var paid = stack.Count * (100 - ent.Comp.TaxPercent) / 100;
        QueueDel(cashUid.Value);
        if (paid <= 0)
            return;

        var result = _stack.SpawnNextToOrDrop(paid, output, ent.Owner);
        _hands.PickupOrDrop(args.Actor, result);
    }

    private bool IsCurrency(ProtoId<StackPrototype> stackType)
    {
        return stackType == Spesos || stackType == Scrip;
    }

    private bool TryGetOutput(ProtoId<StackPrototype> input, out ProtoId<StackPrototype> output)
    {
        if (input == Spesos)
        {
            output = Scrip;
            return true;
        }

        if (input == Scrip)
        {
            output = Spesos;
            return true;
        }

        output = default;
        return false;
    }
}
