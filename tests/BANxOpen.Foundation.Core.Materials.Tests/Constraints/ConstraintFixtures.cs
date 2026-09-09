using BANxOpen.Foundation.Contracts.Common;
using BANxOpen.Foundation.Core.Materials.Constraints;

namespace BANxOpen.Foundation.Core.Materials.Tests.Constraints;

internal static class ConstraintFixtures
{
    /// <summary>A constraint that accepts only the named materials, so a test can say what it means
    /// without restating the predicate plumbing each time.</summary>
    public static MaterialConstraint AllowOnly(
        string sourceLabel,
        string reasonCode,
        params string[] allowedNames) =>
        new(
            DomainId: "TEST",
            SourceLabel: sourceLabel,
            ReasonCode: reasonCode,
            IsSatisfiedBy: candidate => allowedNames.Contains(candidate.Name),
            DescribeViolation: candidate => $"'{candidate.Name}' is not allowed.");

    /// <summary>A provider with a fixed answer and a log of which bodies it was asked about — the log is
    /// what makes caching observable.</summary>
    public sealed class FakeConstraintProvider : IFeatureMaterialConstraintProvider
    {
        private readonly IReadOnlyList<MaterialConstraint> _constraints;

        public FakeConstraintProvider(string domainId, params MaterialConstraint[] constraints)
        {
            DomainId = domainId;
            _constraints = constraints;
        }

        public string DomainId { get; }

        public List<BodyId> QueriedBodies { get; } = new();

        public IReadOnlyList<MaterialConstraint> ConstraintsFor(BodyId bodyId)
        {
            QueriedBodies.Add(bodyId);
            return _constraints;
        }
    }
}