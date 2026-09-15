namespace BANxOpen.Foundation.Core.Materials.Rules;

/// <summary>Named stages for rule <c>Order</c> values, so a rule in one module can be placed relative to rules in
/// another without knowing their numbers. A rule that must sit between two stages uses an offset from one of them
/// (e.g. <c>Confirmation + 50</c>); rules sharing a value run in module registration order.</summary>
public static class MaterialRuleOrder
{
    public static class Validation
    {
        /// <summary>Whether the material may go on this kind of body at all.</summary>
        public const int Eligibility = 100;

        /// <summary>What features already on the body allow. Runs before anything that asks the user a question,
        /// so a forbidden material is refused rather than confirmed.</summary>
        public const int FeatureConstraints = 150;

        /// <summary>Questions for the user, such as replacing an existing material.</summary>
        public const int Confirmation = 200;

        /// <summary>Display material and coating data.</summary>
        public const int Appearance = 300;
    }

    public static class SideEffect
    {
        /// <summary>Properties carried over from the material itself.</summary>
        public const int MaterialProperties = 100;

        /// <summary>Display material and body color.</summary>
        public const int Appearance = 200;

        /// <summary>State a feature domain keeps in step with the material, such as Sheet Metal Preferences.</summary>
        public const int DomainState = 300;
    }
}
