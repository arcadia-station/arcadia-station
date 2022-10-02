using System.Threading;
using Robust.Shared.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Content.Server.Body.Components;
using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.DoAfter;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Actions.ActionTypes;
using Content.Shared.Actions;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Content.Shared.Tag;
using Content.Shared.Examine;
using static Content.Shared.Examine.ExamineSystemShared;

namespace Content.Server.Abilities.Psionics
{
    public sealed class PsionicRegenerationPowerSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly SharedActionsSystem _actions = default!;
        [Dependency] private readonly SolutionContainerSystem _solutionSystem = default!;
        [Dependency] private readonly BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private readonly AudioSystem _audioSystem = default!;
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;
        [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
        [Dependency] private readonly SharedPsionicAbilitiesSystem _psionics = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, ComponentShutdown>(OnShutdown);
            SubscribeLocalEvent<PsionicRegenerationPowerComponent, PsionicRegenerationPowerActionEvent>(OnPowerUsed);

            SubscribeLocalEvent<PsionicRegenerationPowerComponent, DispelledEvent>(OnDispelled);
            SubscribeLocalEvent<PowerSuccessfulEvent>(OnPowerSuccessful);
            SubscribeLocalEvent<PowerCancelledEvent>(OnPowerCancelled);
        }

        private void OnPowerSuccessful(PowerSuccessfulEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.User, out PsionicRegenerationPowerComponent? component))
                return;
            component.CancelToken = null;

            if (TryComp<BloodstreamComponent>(ev.User, out var bloodstream))
            {
                var solution = new Solution();
                solution.AddReagent("PsionicRegenerationEssence", FixedPoint2.New(component.EssenceAmount));
                _bloodstreamSystem.TryAddToChemicals(ev.User, solution, bloodstream);
            }
        }

        private void OnPowerCancelled(PowerCancelledEvent ev)
        {
            if (!EntityManager.TryGetComponent(ev.User, out PsionicRegenerationPowerComponent? component))
                return;
            component.CancelToken = null;

            // DoAfter has no way to run a callback during the process to give
            // small doses of the reagent, so we wait until either the action
            // is cancelled (by being dispelled) or complete to give the
            // appropriate dose. A timestamp delta is used to accomplish this.
            var percentageComplete = Math.Min(1f, (DateTime.Now - ev.StartedAt).TotalSeconds / component.UseDelay);

            if (TryComp<BloodstreamComponent>(ev.User, out var bloodstream))
            {
                var solution = new Solution();
                solution.AddReagent("PsionicRegenerationEssence", FixedPoint2.New(component.EssenceAmount * percentageComplete));
                _bloodstreamSystem.TryAddToChemicals(ev.User, solution, bloodstream);
            }
        }

        private void OnInit(EntityUid uid, PsionicRegenerationPowerComponent component, ComponentInit args)
        {
            if (!_prototypeManager.TryIndex<InstantActionPrototype>("Psionic Regeneration", out var metapsionic))
                return;

            component.PsionicRegenerationPowerAction = new InstantAction(metapsionic);
            _actions.AddAction(uid, component.PsionicRegenerationPowerAction, null);

            if (TryComp<PsionicComponent>(uid, out var psionic) && psionic.PsionicAbility == null)
                psionic.PsionicAbility = component.PsionicRegenerationPowerAction;
        }

        private void OnPowerUsed(EntityUid uid, PsionicRegenerationPowerComponent component, PsionicRegenerationPowerActionEvent args)
        {
            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(uid, component.UseDelay, component.CancelToken.Token)
            {
                BroadcastFinishedEvent = new PowerSuccessfulEvent(component.Owner),
                BroadcastCancelledEvent = new PowerCancelledEvent(component.Owner, DateTime.Now),
            });

            _popupSystem.PopupEntity(Loc.GetString("psionic-regeneration-begin", ("entity", uid)),
                uid,
                // TODO: Use LoS-based Filter when one is available.
                Filter.Pvs(uid).RemoveWhereAttachedEntity(entity => !ExamineSystemShared.InRangeUnOccluded(uid, entity, ExamineRange, null)),
                PopupType.Medium);

            _audioSystem.PlayPvs(component.SoundUse, component.Owner, AudioParams.Default.WithVolume(8f).WithMaxDistance(1.5f).WithRolloffFactor(3.5f));
            _psionics.LogPowerUsed(uid, "psionic regeneration");
            args.Handled = true;
        }

        private void OnShutdown(EntityUid uid, PsionicRegenerationPowerComponent component, ComponentShutdown args)
        {
            if (_prototypeManager.TryIndex<InstantActionPrototype>("Psionic Regeneration", out var metapsionic))
                _actions.RemoveAction(uid, new InstantAction(metapsionic), null);
        }

        private void OnDispelled(EntityUid uid, PsionicRegenerationPowerComponent component, DispelledEvent args)
        {
            if (component.CancelToken != null)
                component.CancelToken.Cancel();

            args.Handled = true;
        }

        private sealed class PowerSuccessfulEvent : EntityEventArgs {
            public EntityUid User;

            public PowerSuccessfulEvent(EntityUid user)
            {
                User = user;
            }
        }

        private sealed class PowerCancelledEvent : EntityEventArgs {
            public EntityUid User;
            public DateTime StartedAt;

            public PowerCancelledEvent(EntityUid user, DateTime startedAt)
            {
                User = user;
                StartedAt = startedAt;
            }
        }
    }

    public sealed class PsionicRegenerationPowerActionEvent : InstantActionEvent {}
}

