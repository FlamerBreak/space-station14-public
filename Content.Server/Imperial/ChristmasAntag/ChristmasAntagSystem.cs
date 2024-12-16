using Content.Server.Imperial.ChristmasAntag.Components;
using Content.Server.Polymorph.Systems;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Actions;
using Robust.Shared.Timing;
using Content.Server.Medical;

namespace Content.Server.Imperial.ChristmasAntag;

public sealed class ChristmasAntagSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PolymorphSystem _polymorphSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly VomitSystem _vomitSystem = default!;

    public override void Initialize()
    {
    }

    public override void Update(float frameTime)
    {
        var timerQuery = EntityQueryEnumerator<ChristmasAntagComponent, TransformComponent>();
        while (timerQuery.MoveNext(out var uid, out var component, out var transform))
        {
            if (component.NextUpdate > _timing.CurTime)
                continue;

            if (component.Target == null)
                continue;

            if (component.TargetGoaled == true)
                continue;

            if (component.CurseTimer == TimeSpan.FromMinutes(5))
                _popup.PopupEntity(Loc.GetString("cursed-gift-polymorph-message-5min"), component.Owner, component.Owner, PopupType.Medium);

            if (component.CurseTimer == TimeSpan.FromMinutes(3))
                _popup.PopupEntity(Loc.GetString("cursed-gift-polymorph-message-3min"), component.Owner, component.Owner, PopupType.Medium);

            if (component.CurseTimer == TimeSpan.FromMinutes(1))
            {
                _popup.PopupEntity(Loc.GetString("cursed-gift-polymorph-message-1min"), component.Owner, component.Owner, PopupType.Medium);
                _vomitSystem.Vomit(component.Owner);
                Spawn("Ash", Transform(component.Owner).Coordinates);

            }

            if (component.CurseTimer == TimeSpan.FromSeconds(30))
            {
                _popup.PopupEntity(Loc.GetString("cursed-gift-polymorph-message-30sec"), component.Owner, component.Owner, PopupType.Medium);
                _vomitSystem.Vomit(component.Owner);
                Spawn("Ash", Transform(component.Owner).Coordinates);
            }

            if (component.CurseTimer == TimeSpan.FromSeconds(5))
                _popup.PopupEntity(Loc.GetString("cursed-gift-polymorph-message-0sec"), component.Owner, component.Owner, PopupType.Medium);

            if (component.CurseTimer == TimeSpan.Zero)
                CursedGiftSmite(component.Owner, component);

            component.NextUpdate = _timing.CurTime + component.UpdateInterval;

            component.CurseTimer -= TimeSpan.FromSeconds(1);
        }
    }

    public void CursedGiftSmite(EntityUid target, ChristmasAntagComponent comp)
    {
        var sysMan = IoCManager.Resolve<IEntitySystemManager>();
        sysMan.GetEntitySystem<ExplosionSystem>().QueueExplosion(target, "Default", 1, 1, 1);
        QueueDel(target);
        if (comp.LastCursedGift != null)
            EntityManager.DeleteEntity(comp.LastCursedGift);

        var result = _polymorphSystem.PolymorphEntity(target, "CursedGiftSmite");
        if (result != null)
        {
            RemComp<CurseGiftComponent>(result.Value);
            _actionsSystem.AddAction(result.Value, "ActionSpawnAshGift");
        }
    }
}
