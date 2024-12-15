using Content.Server.Imperial.MobDrops.Components;
using Content.Shared.Throwing;
using Content.Shared.Mobs;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Imperial.MobDrops;

public sealed partial class MobDropsSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobDropsComponent, MobStateChangedEvent>(OnSuicide); ;
    }

    private void OnSuicide(EntityUid uid, MobDropsComponent component, MobStateChangedEvent args)
    {
        if (!component.AlredyDroped && args.NewMobState.ToString() == "Dead")
        {
            SpawnDrop(component);
            component.AlredyDroped = true;
        }

        return;
    }
    private void SpawnDrop(MobDropsComponent component)
    {
        var result = Spawn(component.Drop.ToString(), Transform(component.Owner).Coordinates);
        if (TryComp<PhysicsComponent>(result, out var phys) && phys.BodyType != BodyType.Static)
            _throwing.TryThrow(result, _random.NextAngle().ToWorldVec());
    }
}
