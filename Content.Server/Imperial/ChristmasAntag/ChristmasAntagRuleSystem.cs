using System.Linq;
using Content.Server.Antag;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Imperial.ChristmasAntag.Components;
using Content.Server.Storage.Components;
using Content.Server.Objectives.Components;
using Content.Server.Mind;
using Content.Server.Storage.EntitySystems;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Ghost;
using Content.Shared.Imperial.ChristmasAntag.Events;
using Content.Shared.Antag;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Tag;
using Content.Shared.Throwing;
using Content.Shared.Item;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Random;

namespace Content.Server.Imperial.ChristmasAntag
{
    public sealed class ChristmasAntagRuleSystem : GameRuleSystem<ChristmasAntagRuleComponent>
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AntagSelectionSystem _antagSelection = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly SharedRoleSystem _roleSystem = default!;
        [Dependency] private readonly ObjectivesSystem _objectives = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly GhostSystem _ghost = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
        [Dependency] private readonly TagSystem _tag = default!;
        [Dependency] private readonly DamageableSystem _damage = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnPlayersSpawned);

            SubscribeLocalEvent<ChristmasAntagRoleComponent, GetBriefingEvent>(OnGetBriefing);
            SubscribeLocalEvent<ChristmasAntagRuleComponent, ObjectivesTextGetInfoEvent>(OnObjectivesTextGetInfo);

            SubscribeLocalEvent<SpawnCursedGiftSpellEvent>(OnSpawnCursedGiftSpell);
            SubscribeLocalEvent<SpawnAshGiftSpellEvent>(OnSpawnAshGiftSpell);
            SubscribeLocalEvent<TryToFindTargetSpellEvent>(TryToFindTargetSpell);
        }

        private void OnPlayersSpawned(RulePlayerJobsAssignedEvent ev)
        {
            var query = QueryActiveRules();
            while (query.MoveNext(out var uid, out _, out var comp, out var gameRule))
            {
                var eligiblePlayers = _antagSelection.GetEligiblePlayers(
                    ev.Players,
                    comp.ChristmasAntagPrototypeId,
                    acceptableAntags: AntagAcceptability.All,
                    allowNonHumanoids: true
                );

                if (eligiblePlayers.Count == 0)
                {
                    GameTicker.EndGameRule(uid, gameRule);
                    continue;
                }

                var christmasAntags = _antagSelection.ChooseAntags(comp.MaxAllowChristmasAntag, eligiblePlayers);

                MakeChristmasAntag(christmasAntags, comp);
            }
        }

        public void MakeChristmasAntag(List<EntityUid> players, ChristmasAntagRuleComponent christmasAntagRule)
        {
            foreach (var christmasAntag in players)
            {
                MakeChristmasAntag(christmasAntag, christmasAntagRule);
            }
        }

        public void MakeChristmasAntag(EntityUid christmasAntag, ChristmasAntagRuleComponent christmasAntagRule)
        {
            if (!_mindSystem.TryGetMind(christmasAntag, out var mindId, out var mind))
                return;

            if (HasComp<ChristmasAntagRoleComponent>(mindId))
                return;

            _roleSystem.MindAddRole(mindId, new ChristmasAntagRoleComponent
            {
                PrototypeId = christmasAntagRule.ChristmasAntagPrototypeId,
            }, silent: true);

            EnsureComp<ChristmasAntagComponent>(christmasAntag);
            GenerateObjectives(mindId, mind, christmasAntagRule.ChristmasAntagObjective);
            _antagSelection.SendBriefing(christmasAntag, MakeBriefing(), null, christmasAntagRule.GreetingSound);
            christmasAntagRule.ChristmasAntagMinds.Add(mindId);
            _actionsSystem.AddAction(christmasAntag, "ActionSpawnCursedGift");
            _actionsSystem.AddAction(christmasAntag, "ActionTryToFindTarget");
        }

        public void AdminMakeChristmasAntag(EntityUid entity)
        {
            var christmasAntagRule = EntityQuery<ChristmasAntagRuleComponent>().FirstOrDefault();
            if (christmasAntagRule == null)
            {
                GameTicker.StartGameRule("ChristmasAntag", out var ruleEntity);
                christmasAntagRule = Comp<ChristmasAntagRuleComponent>(ruleEntity);
            }

            if (HasComp<ChristmasAntagRoleComponent>(entity))
                return;

            MakeChristmasAntag(entity, christmasAntagRule);
        }

        private void GenerateObjectives(EntityUid mindId, MindComponent mind, string proto)
        {
            var objective = _objectives.TryCreateObjective(mindId, mind, proto);
            if (objective == null)
                return;

            _mindSystem.AddObjective(mindId, mind, objective.Value);
            if (TryComp<ChristmasAntagComponent>(mind.OwnedEntity, out var christmasComp))
            {
                if (TryComp<TargetObjectiveComponent>(objective, out var mindTarget) && mindTarget.Target.HasValue)
                    if (TryComp<MindComponent>(mindTarget.Target, out var uidTarget) && uidTarget.OwnedEntity != null)
                        christmasComp.Target = uidTarget.OwnedEntity.Value;
            }
        }

        private void OnGetBriefing(Entity<ChristmasAntagRoleComponent> christmasAntag, ref GetBriefingEvent args)
        {
            if (!TryComp<MindComponent>(christmasAntag.Owner, out var mind) || mind.OwnedEntity == null)
                return;

            args.Append(MakeBriefing());
        }

        private string MakeBriefing()
        {
            var briefing = Loc.GetString("christmasAntag-role-greeting");

            briefing += "\n \n" + Loc.GetString("christmasAntag-briefing") + "\n";
            return briefing;
        }

        private void OnObjectivesTextGetInfo(Entity<ChristmasAntagRuleComponent> christmasAntags, ref ObjectivesTextGetInfoEvent args)
        {
            args.Minds = christmasAntags.Comp.ChristmasAntagMinds;
            args.AgentName = Loc.GetString("roles-antag-christmasAntag-name");
        }

        private void OnSpawnAshGiftSpell(SpawnAshGiftSpellEvent ev)
        {
            if (ev.Handled)
                return;
            ev.Handled = true;

            var result = Spawn("PresentRandomAshNG3", Transform(ev.Performer).Coordinates);

            _throwing.TryThrow(result, _random.NextAngle().ToWorldVec());

            NerfEffectsOnSpawnCursedGift(ev.Performer);
        }

        private void OnSpawnCursedGiftSpell(SpawnCursedGiftSpellEvent ev)
        {
            if (ev.Handled)
                return;
            ev.Handled = true;

            if (!TryComp<ChristmasAntagComponent>(ev.Performer, out var compUser))
                return;

            if (Deleted(compUser.LastCursedGift))
                compUser.LastCursedGift = null;

            if (compUser.LastCursedGift != null)
            {

                var message = _hands.TryPickupAnyHand(ev.Performer, compUser.LastCursedGift.Value)
                    ? "christmasAntag-cursed-gift-recalled"
                    : "christmasAntag-hands-full";
                _popup.PopupEntity(Loc.GetString(message), ev.Performer, ev.Performer);
                NerfEffectsOnSpawnCursedGift(ev.Performer);
                return;
            }
            var result = Spawn("CursedGift", Transform(ev.Performer).Coordinates);
            _throwing.TryThrow(result, _random.NextAngle().ToWorldVec());

            if (!TryComp<CurseGiftComponent>(result, out var compGift))
                return;

            NerfEffectsOnSpawnCursedGift(ev.Performer);
            compGift.OwnerGift = ev.Performer;
            compUser.LastCursedGift = result;
        }

        private void TryToFindTargetSpell(TryToFindTargetSpellEvent ev)
        {
            if (ev.Handled)
                return;
            ev.Handled = true;

            if (!TryComp<ChristmasAntagComponent>(ev.Performer, out var compUser))
                return;

            if (compUser.Target == null)
            {
                _popup.PopupEntity(Loc.GetString("cursed-gift-say-no-target"), ev.Performer, ev.Performer);
                return;
            }

            var antag_mapUid = Transform(ev.Performer).MapUid;
            var target_mapUid = Transform(compUser.Target.Value).MapUid;

            if (antag_mapUid != target_mapUid)
            {
                _popup.PopupEntity("Цель возле Вас не найдена", ev.Performer, ev.Performer);
                return;
            }

            var antag_coords = Transform(ev.Performer).Coordinates;
            var target_coords = Transform(compUser.Target.Value).Coordinates;

            var distance = Math.Sqrt(Math.Pow(target_coords.X - antag_coords.X, 2) + Math.Pow(target_coords.Y - antag_coords.Y, 2));
            var distanceInt = (int) Math.Floor(distance);

            string GetMeterWord(int distance)
            {
                int lastDigit = distance % 10;
                int lastTwoDigits = distance % 100;

                if (lastDigit == 1 && lastTwoDigits != 11)
                    return "метр";
                else if ((lastDigit >= 2 && lastDigit <= 4) && (lastTwoDigits < 10 || lastTwoDigits >= 20))
                    return "метра";
                else
                    return "метров";
            }

            string meterWord = GetMeterWord(distanceInt);
            _popup.PopupEntity($"Расстояние до цели: {distanceInt} {meterWord}", ev.Performer, ev.Performer, PopupType.Medium);
        }

        // Много грифа
        /*private void EffectsOnSpawnCursedGift(EntityUid uid)
        {
            var entities = _lookup.GetEntitiesInRange(uid, 5f);
            var tags = GetEntityQuery<TagComponent>();
            var entityStorage = GetEntityQuery<EntityStorageComponent>();
            var items = GetEntityQuery<ItemComponent>();

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = _ghost.DoGhostBooEvent(ent);

                if (tags.HasComponent(ent) && _tag.HasAnyTag(ent, "Window"))
                {
                    var dspec = new DamageSpecifier();
                    dspec.DamageDict.Add("Structural", 15);
                    _damage.TryChangeDamage(ent, dspec, origin: uid);
                }

                if (entityStorage.TryGetComponent(ent, out var entstorecomp))
                    _entityStorage.OpenStorage(ent, entstorecomp);

                if (items.HasComponent(ent) &&
                    TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                    _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

                if (handled)
                    booCounter++;

                if (booCounter >= 10f)
                    break;
            }
        }*/

        private void NerfEffectsOnSpawnCursedGift(EntityUid uid)
        {
            var entities = _lookup.GetEntitiesInRange(uid, 5f);

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = _ghost.DoGhostBooEvent(ent);

                if (handled)
                    booCounter++;

                if (booCounter >= 10f)
                    break;
            }
        }
    }
}
