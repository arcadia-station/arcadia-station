using Content.Server.Mind.Components;
using Content.Server.Ghost.Roles.Events;
using Content.Server.Traitor;
using Content.Server.Objectives;
using Content.Server.Chat.Systems;
using Content.Server.Communications;
using Content.Server.Paper;
using Content.Server.Humanoid;
using Content.Shared.Roles;
using Content.Shared.Movement.Systems;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Humanoid;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.Physics.Components;

namespace Content.Server.Fugitive
{
    public sealed class FugitiveSystem : EntitySystem
    {
        private const string FugitiveRole = "Fugitive";
        private const string EscapeObjective = "EscapeShuttleObjectiveFugitive";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly ChatSystem _chat = default!;
        [Dependency] private readonly PaperSystem _paperSystem = default!;


        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<FugitiveComponent, GhostRoleSpawnerUsedEvent>(OnSpawned);
            SubscribeLocalEvent<FugitiveComponent, MindAddedMessage>(OnMindAdded);
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            foreach (var (cd, _) in EntityQuery<FugitiveCountdownComponent, FugitiveComponent>())
            {
                if (cd.AnnounceTime != null && _timing.CurTime > cd.AnnounceTime)
                {
                    _chat.DispatchGlobalAnnouncement(Loc.GetString("station-event-fugitive-hunt-announcement"), colorOverride: Color.Yellow);

                    foreach (var console in EntityQuery<CommunicationsConsoleComponent>())
                    {
                        var paperEnt = Spawn("Paper", Transform(console.Owner).Coordinates);

                        MetaData(paperEnt).EntityName = Loc.GetString("fugi-report-ent-name", ("name", cd.Owner));

                        if (!TryComp<PaperComponent>(paperEnt, out var paper))
                            continue;

                        var report = GenerateFugiReport(cd.Owner);

                        _paperSystem.SetContent(paperEnt, report.ToMarkup(), paper);
                    }

                    RemCompDeferred<FugitiveCountdownComponent>(cd.Owner);
                }
            }
        }

        private void OnSpawned(EntityUid uid, FugitiveComponent component, GhostRoleSpawnerUsedEvent args)
        {
            if (TryComp<FugitiveCountdownComponent>(uid, out var cd))
            {
                Logger.Error("Setting announce time...");
                cd.AnnounceTime = _timing.CurTime + cd.AnnounceCD;
            }
        }

        private void OnMindAdded(EntityUid uid, FugitiveComponent component, MindAddedMessage args)
        {
            if (!TryComp<MindComponent>(uid, out var mindComponent) || mindComponent.Mind == null)
                return;

            var mind = mindComponent.Mind;

            mind.AddRole(new TraitorRole(mind, _prototypeManager.Index<AntagPrototype>(FugitiveRole)));

            mind.TryAddObjective(_prototypeManager.Index<ObjectivePrototype>(EscapeObjective));

            // workaround seperate shitcode moment
            _movementSpeed.RefreshMovementSpeedModifiers(uid);
        }

        private FormattedMessage GenerateFugiReport(EntityUid uid)
        {
            FormattedMessage report = new();
            report.AddMarkup(Loc.GetString("fugi-report-title", ("name", uid)));
            report.PushNewline();
            report.PushNewline();
            report.AddMarkup(Loc.GetString("fugitive-report-first-line", ("name", uid)));
            report.PushNewline();
            report.PushNewline();


            if (!TryComp<HumanoidComponent>(uid, out var humanoidComponent) ||
                !_prototypeManager.TryIndex<SpeciesPrototype>(humanoidComponent.Species, out var species))
            {
                report.AddMarkup(Loc.GetString("fugitive-report-inhuman", ("name", uid)));
                return report;
            }

            report.AddMarkup(Loc.GetString("fugitive-report-morphotype", ("species", Loc.GetString(species.Name))));
            report.PushNewline();
            report.AddMarkup(Loc.GetString("fugitive-report-age", ("age", humanoidComponent.Age)));
            report.PushNewline();

            string sexLine = string.Empty;
            sexLine += humanoidComponent.Sex switch
            {
                Sex.Male => Loc.GetString("fugitive-report-sex-m"),
                Sex.Female => Loc.GetString("fugitive-report-sex-f"),
                _ => Loc.GetString("fugitive-report-sex-n")
            };

            report.AddMarkup(sexLine);

            if (TryComp<PhysicsComponent>(uid, out var physics))
            {
                report.PushNewline();
                report.AddMarkup(Loc.GetString("fugitive-report-weight", ("weight", Math.Round(physics.FixturesMass))));
            }
            report.PushNewline();
            report.PushNewline();
            report.AddMarkup(Loc.GetString("fugitive-report-last-line"));

            return report;
        }
    }
}
