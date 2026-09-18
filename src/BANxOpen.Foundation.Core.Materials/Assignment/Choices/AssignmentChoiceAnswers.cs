using BANxOpen.Foundation.Contracts.Common;

namespace BANxOpen.Foundation.Core.Materials.Assignment.Choices;

/// <summary>What the user picked, keyed by choice and body, so an effect rule reads the answer for its own body
/// without knowing how the question was grouped.</summary>
public sealed class AssignmentChoiceAnswers
{
    public static readonly AssignmentChoiceAnswers Empty = new(new Dictionary<(string, BodyId), string>());

    private readonly IReadOnlyDictionary<(string ChoiceId, BodyId BodyId), string> _answers;

    private AssignmentChoiceAnswers(IReadOnlyDictionary<(string, BodyId), string> answers) => _answers = answers;

    /// <summary>The option picked for <paramref name="choiceId"/> on <paramref name="bodyId"/>.</summary>
    public bool TryGet(string choiceId, BodyId bodyId, out string optionId)
    {
        if (_answers.TryGetValue((choiceId, bodyId), out var found))
        {
            optionId = found;
            return true;
        }

        optionId = string.Empty;
        return false;
    }

    public static Builder CreateBuilder() => new();

    /// <summary>Accumulates answers as the UI collects them, one call per question asked.</summary>
    public sealed class Builder
    {
        private readonly Dictionary<(string ChoiceId, BodyId BodyId), string> _answers = new();

        internal Builder() { }

        /// <summary>Records <paramref name="optionId"/> as the answer to <paramref name="pending"/>, for every
        /// body the question covered.</summary>
        /// <exception cref="ArgumentException"><paramref name="optionId"/> is not one of the choice's options.</exception>
        public Builder Answer(PendingAssignmentChoice pending, string optionId)
        {
            if (pending.Choice.Find(optionId) is null)
            {
                throw new ArgumentException(
                    $"'{optionId}' is not an option of choice '{pending.Choice.ChoiceId}'.", nameof(optionId));
            }

            foreach (var bodyId in pending.BodyIds)
                _answers[(pending.Choice.ChoiceId, bodyId)] = optionId;

            return this;
        }

        /// <summary>Records an answer the caller already has without collecting the question, e.g. a row picked
        /// in the caller's own dialog. The option id is not validated.</summary>
        public Builder AnswerDirectly(string choiceId, BodyId bodyId, string optionId)
        {
            _answers[(choiceId, bodyId)] = optionId;
            return this;
        }

        public AssignmentChoiceAnswers Build() =>
            _answers.Count == 0 ? Empty : new AssignmentChoiceAnswers(new Dictionary<(string, BodyId), string>(_answers));
    }
}
