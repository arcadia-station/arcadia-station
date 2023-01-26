using Content.Shared.EntityHealthBar;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.Graphics;
using Robust.Client.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Client.EntityHealthBar
{
    public sealed class ShowHealthBarsSystem : EntitySystem
    {
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IPrototypeManager _protoMan = default!;
        [Dependency] private readonly IOverlayManager _overlayMan = default!;

        private EntityHealthBarOverlay _overlay = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<ShowHealthBarsComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerAttachedEvent>(OnPlayerAttached);
            SubscribeLocalEvent<ShowHealthBarsComponent, PlayerDetachedEvent>(OnPlayerDetached);
            SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

            _overlay = new(EntityManager, _protoMan);
        }

        private void OnInit(EntityUid uid, ShowHealthBarsComponent component, ComponentInit args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlay.DamageContainer = component.DamageContainer;
                _overlay.CheckLOS = component.CheckLOS;
                _overlayMan.AddOverlay(_overlay);
            }


        }
        private void OnRemove(EntityUid uid, ShowHealthBarsComponent component, ComponentRemove args)
        {
            if (_player.LocalPlayer?.ControlledEntity == uid)
            {
                _overlay.DamageContainer = null;
                _overlay.CheckLOS = false;
                _overlay.Reset();
                _overlayMan.RemoveOverlay(_overlay);
            }
        }

        private void OnPlayerAttached(EntityUid uid, ShowHealthBarsComponent component, PlayerAttachedEvent args)
        {
            _overlay.DamageContainer = null;
            _overlay.CheckLOS = component.CheckLOS;
            _overlay.Reset();
            _overlayMan.AddOverlay(_overlay);
        }

        private void OnPlayerDetached(EntityUid uid, ShowHealthBarsComponent component, PlayerDetachedEvent args)
        {
            _overlay.DamageContainer = null;
            _overlay.CheckLOS = false;
            _overlay.Reset();
            _overlayMan.RemoveOverlay(_overlay);
        }

        private void OnRoundRestart(RoundRestartCleanupEvent args)
        {
            _overlay.DamageContainer = null;
            _overlay.CheckLOS = false;
            _overlay.Reset();
            _overlayMan.RemoveOverlay(_overlay);
        }
    }
}
