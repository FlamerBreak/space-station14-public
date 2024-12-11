using System.Linq;
using Content.Server.Objectives.Systems;
using Content.Server.Roles;
using Content.Server.Objectives.Components;
using Content.Server.Imperial.ChristmasAntag.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Mind;
using Robust.Shared.Random;

namespace Content.Server.Imperial.ChristmasAntag
{
    public sealed class BecomeChristmasAntagConditionSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly TargetObjectiveSystem _target = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BecomeChristmasAntagConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

            SubscribeLocalEvent<ChristmasAntagPickRandomPersonComponent, ObjectiveAssignedEvent>(OnChristmasAntagAssigned);
        }

        private void OnGetProgress(EntityUid uid, BecomeChristmasAntagConditionComponent comp, ref ObjectiveGetProgressEvent args)
        {
            if (!_target.GetTarget(uid, out var target))
                return;

            args.Progress = GetProgress(target.Value);
        }

        private void OnChristmasAntagAssigned(EntityUid uid, ChristmasAntagPickRandomPersonComponent comp, ref ObjectiveAssignedEvent args)
        {
            if (!TryComp<TargetObjectiveComponent>(uid, out var target))
            {
                args.Cancelled = true;
                return;
            }

            if (target.Target != null)
                return;

            var allHumans = _mind.GetAliveHumansExcept(args.MindId);
            if (allHumans.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            var potentialTargets = allHumans.Where(mind => !IsChristmasAntag(mind)).ToList();
            if (potentialTargets.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            var objective_target = _random.Pick(potentialTargets);
            _target.SetTarget(uid, objective_target, target);
        }

        public bool IsChristmasAntag(EntityUid mindId)
        {
            if (!_role.MindHasRole<ChristmasAntagRoleComponent>(mindId))
            {
                return false;
            }

            return true;
        }

        private float GetProgress(EntityUid target)
        {
            if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
                return 0f;

            return IsChristmasAntag(target) ? 1f : 0f;
        }
    }
}
