using Content.Shared.Clothing.Components;
using Content.Shared.Clothing.EntitySystems;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;
using Content.Client._Starfall.Particles; // _Starfall
using Content.Shared._Starfall.Particles; // _Starfall
using Robust.Shared.Prototypes;

namespace Content.Client.Movement.Systems;

public sealed partial class JetpackSystem : SharedJetpackSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly ClothingSystem _clothing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _mapSystem = default!;
    [Dependency] private readonly ParticleSystem _particles = default!; // _Starfall

    private static readonly ProtoId<ParticleEffectPrototype> JetpackEffect = "JetpackTrail"; // _Starfall // Funky Adjusted to JetpackTrail
    private readonly Dictionary<EntityUid, ActiveEmitter> _activeJetpackParticles = new(); // _Starfall

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<JetpackComponent, AppearanceChangeEvent>(OnJetpackAppearance);
    }

    protected override bool CanEnable(EntityUid uid, JetpackComponent component)
    {
        // No predicted atmos so you'd have to do a lot of funny to get this working.
        return false;
    }

    // _Starfall Start
    private void OnJetpackAppearance(EntityUid uid, JetpackComponent component, ref AppearanceChangeEvent args)
    {
        Appearance.TryGetData<bool>(uid, JetpackVisuals.Enabled, out var enabled, args.Component);

        if (TryComp<ClothingComponent>(uid, out var clothing))
            _clothing.SetEquippedPrefix(uid, enabled ? "on" : null, clothing);

        if (enabled)
        {
            // Already running?
            if (_activeJetpackParticles.ContainsKey(uid))
                return;

            var emitter = _particles.CreateParticle(JetpackEffect, uid);

            if (emitter != null)
                _activeJetpackParticles[uid] = emitter;
        }
        else
        {
            if (_activeJetpackParticles.TryGetValue(uid, out var emitter))
            {
                _particles.RemoveParticle(emitter);
                _activeJetpackParticles.Remove(uid);
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;
    }
    // _Starfall End
}
