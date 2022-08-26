using Content.Shared.Damage;

namespace Content.Server.Psionics
{
    [RegisterComponent]
    public sealed class AntiPsionicWeaponComponent : Component
    {

        [DataField("modifiers", required: true)]
        public DamageModifierSet Modifiers = default!;

        [DataField("psychicStaminaDamage")]
        public float PsychicStaminaDamage = 30f;

        [DataField("disableChance")]
        public float DisableChance = 0.3f;
    }
}
