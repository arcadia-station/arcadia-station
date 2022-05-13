using Content.Server.Construction;
using Content.Server.Destructible.Thresholds;
using Content.Server.Destructible.Thresholds.Behaviors;
using Content.Server.Destructible.Thresholds.Triggers;
using Content.Server.Explosion.EntitySystems;
using Content.Server.Stack;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Destructible;

namespace Content.Server.Destructible
{
    [UsedImplicitly]
    public sealed class DestructibleSystem : SharedDestructibleSystem
    {
        [Dependency] public readonly IRobustRandom Random = default!;
        public new IEntityManager EntityManager => base.EntityManager;

        [Dependency] public readonly AudioSystem AudioSystem = default!;
        [Dependency] public readonly ConstructionSystem ConstructionSystem = default!;
        [Dependency] public readonly ExplosionSystem ExplosionSystem = default!;
        [Dependency] public readonly StackSystem StackSystem = default!;
        [Dependency] public readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] public readonly IComponentFactory ComponentFactory = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<DestructibleComponent, DamageChangedEvent>(Execute);
        }

        /// <summary>
        ///     Check if any thresholds were reached. if they were, execute them.
        /// </summary>
        public void Execute(EntityUid uid, DestructibleComponent component, DamageChangedEvent args)
        {
            foreach (var threshold in component.Thresholds)
            {
                if (threshold.Reached(args.Damageable, this))
                {
                    RaiseLocalEvent(uid, new DamageThresholdReached(component, threshold));

                    threshold.Execute(uid, this, EntityManager);
                }

                // if destruction behavior (or some other deletion effect) occurred, don't run other triggers.
                if (EntityManager.IsQueuedForDeletion(uid) || Deleted(uid))
                    return;
            }
        }

        // FFS this shouldn't be this hard. Maybe this should just be a field of the destructible component. Its not
        // like there is currently any entity that is NOT just destroyed upon reaching a total-damage value.
        /// <summary>
        ///     Figure out how much damage an entity needs to have in order to be destroyed.
        /// </summary>
        /// <remarks>
        ///     This assumes that this entity has some sort of destruction or breakage behavior triggered by a
        ///     total-damage threshold.
        /// </remarks>
        public FixedPoint2 DestroyedAt(EntityUid uid, DestructibleComponent? destructible = null)
        {
            if (!Resolve(uid, ref destructible, logMissing: false))
                return FixedPoint2.MaxValue;

            // We have nested for loops here, but the vast majority of components only have one threshold with 1-3 behaviors.
            // Really, this should probably just be a property of the damageable component.
            var damageNeeded = FixedPoint2.MaxValue;
            foreach (var threshold in destructible.Thresholds)
            {
                if (threshold.Trigger is not DamageTrigger trigger)
                    continue;

                foreach (var behavior in threshold.Behaviors)
                {
                    if (behavior is DoActsBehavior actBehavior &&
                        actBehavior.HasAct(ThresholdActs.Destruction | ThresholdActs.Breakage))
                    {
                        damageNeeded = Math.Min(damageNeeded.Float(), trigger.Damage);
                    }
                }
            }
            return damageNeeded;
        }
    }

    // Currently only used for destructible integration tests. Unless other uses are found for this, maybe this should just be removed and the tests redone.
    /// <summary>
    ///     Event raised when a <see cref="DamageThreshold"/> is reached.
    /// </summary>
    public sealed class DamageThresholdReached : EntityEventArgs
    {
        public readonly DestructibleComponent Parent;

        public readonly DamageThreshold Threshold;

        public DamageThresholdReached(DestructibleComponent parent, DamageThreshold threshold)
        {
            Parent = parent;
            Threshold = threshold;
        }
    }
}
