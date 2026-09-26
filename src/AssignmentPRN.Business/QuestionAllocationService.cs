using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;
using AssignmentPRN.DataAccess.Repositories;

namespace AssignmentPRN.Business;

public sealed class QuestionAllocationService : IQuestionAllocationService
{
    private readonly IQuestionRepository _questionRepository;
    private readonly IQuestionRandomizer _randomizer;

    public QuestionAllocationService(
        IQuestionRepository questionRepository,
        IQuestionRandomizer? randomizer = null)
    {
        _questionRepository = questionRepository ?? throw new ArgumentNullException(nameof(questionRepository));
        _randomizer = randomizer ?? new SecureQuestionRandomizer();
    }

    public Task<ServiceResponse<QuestionAllocationResponse>> AllocateAsync(
        QuestionAllocationRequest request,
        CancellationToken cancellationToken = default)
    {
        return ServiceExecutor.RunAsync<QuestionAllocationResponse>(
            async () =>
            {
                var normalized = NormalizeRequest(request);
                var questions = await LoadQuestionsAsync(normalized.SubjectId, cancellationToken);
                var coreQuestions = questions
                    .Where(question => question.Category == QuestionCategory.Core)
                    .OrderBy(question => question.Id)
                    .ToList();
                var deepQuestions = questions
                    .Where(question => question.Category == QuestionCategory.Deep)
                    .OrderBy(question => question.Id)
                    .ToList();
                ValidateFeasibility(normalized.Candidates, coreQuestions.Count);

                var resultCandidates = new List<QuestionAllocationCandidateResponse>();
                var warnings = new List<string>();
                var previousQuestionIds = new HashSet<int>();

                foreach (var candidate in normalized.Candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var eligibleCore = coreQuestions
                        .Where(question => !previousQuestionIds.Contains(question.Id))
                        .ToList();
                    if (eligibleCore.Count < candidate.RequestedCoreCount)
                    {
                        throw new BusinessValidationException(
                            $"Subject {normalized.SubjectId} does not have enough non-repeating core questions for student {candidate.StudentId}.");
                    }

                    var selectedCore = Shuffle(eligibleCore)
                        .Take(candidate.RequestedCoreCount)
                        .ToList();
                    var currentQuestionIds = selectedCore.Select(question => question.Id).ToHashSet();
                    var eligibleDeep = deepQuestions
                        .Where(question => !previousQuestionIds.Contains(question.Id)
                            && !currentQuestionIds.Contains(question.Id))
                        .ToList();
                    var targetDeepCount = Math.Min(candidate.MaximumDeepCount, eligibleDeep.Count);
                    var selectedDeep = SelectDeep(
                        eligibleDeep,
                        targetDeepCount,
                        selectedCore.Select(question => TopicKey(question.Topic)));
                    currentQuestionIds.UnionWith(selectedDeep.Select(question => question.Id));

                    var assignments = new List<QuestionAllocationAssignmentResponse>();
                    for (var index = 0; index < selectedCore.Count; index++)
                    {
                        assignments.Add(new QuestionAllocationAssignmentResponse
                        {
                            QuestionId = selectedCore[index].Id,
                            Category = QuestionCategory.Core,
                            Position = 1,
                            Order = index + 1
                        });
                    }

                    for (var index = 0; index < selectedDeep.Count; index++)
                    {
                        assignments.Add(new QuestionAllocationAssignmentResponse
                        {
                            QuestionId = selectedDeep[index].Id,
                            Category = QuestionCategory.Deep,
                            Position = 2,
                            Order = index + 1
                        });
                    }

                    resultCandidates.Add(new QuestionAllocationCandidateResponse
                    {
                        StudentId = candidate.StudentId,
                        StartTime = candidate.StartTime,
                        EndTime = candidate.EndTime,
                        RequestedCoreCount = candidate.RequestedCoreCount,
                        MaximumDeepCount = candidate.MaximumDeepCount,
                        ActualCoreCount = selectedCore.Count,
                        ActualDeepCount = selectedDeep.Count,
                        Assignments = assignments
                    });

                    if (candidate.MaximumDeepCount > selectedDeep.Count)
                    {
                        warnings.Add(
                            $"Student {candidate.StudentId} received {selectedDeep.Count} of {candidate.MaximumDeepCount} optional deep questions because {eligibleDeep.Count} non-repeating deep questions were available.");
                    }

                    previousQuestionIds = currentQuestionIds;
                }

                return new QuestionAllocationResponse
                {
                    SubjectId = normalized.SubjectId,
                    Candidates = resultCandidates,
                    Warnings = warnings
                };
            },
            "Questions could not be allocated.");
    }

    private async Task<IReadOnlyList<Question>> LoadQuestionsAsync(
        int subjectId,
        CancellationToken cancellationToken)
    {
        var questions = await _questionRepository.ListBySubjectAsync(subjectId, cancellationToken);
        if (questions is null)
        {
            throw new BusinessValidationException("The selected subject question bank could not be loaded.");
        }

        var result = new List<Question>();
        var ids = new HashSet<int>();
        foreach (var question in questions)
        {
            if (question is null || question.Id <= 0)
            {
                throw new BusinessValidationException("The selected subject question bank contains an invalid question.");
            }

            if (!ids.Add(question.Id))
            {
                throw new BusinessValidationException("The selected subject question bank contains duplicate question identifiers.");
            }

            if (!Enum.IsDefined(typeof(QuestionCategory), question.Category))
            {
                throw new BusinessValidationException("The selected subject question bank contains an unsupported question category.");
            }

            result.Add(question);
        }

        return result;
    }

    private static NormalizedAllocationRequest NormalizeRequest(QuestionAllocationRequest request)
    {
        if (request is null)
        {
            throw new BusinessValidationException("Request is required.");
        }

        var subjectId = BusinessValidation.PositiveId(request.SubjectId, nameof(request.SubjectId));
        if (request.Candidates is null || request.Candidates.Count == 0)
        {
            throw new BusinessValidationException("At least one participant is required.");
        }

        var candidates = new List<NormalizedCandidate>();
        var studentIds = new HashSet<int>();
        var index = 0;
        foreach (var candidate in request.Candidates)
        {
            if (candidate is null)
            {
                throw new BusinessValidationException("Participants cannot contain null entries.");
            }

            var studentId = BusinessValidation.PositiveId(candidate.StudentId, nameof(candidate.StudentId));
            if (!studentIds.Add(studentId))
            {
                throw new BusinessValidationException($"Student {studentId} occurs more than once in the session.");
            }

            var startTime = BusinessValidation.ToUtc(candidate.StartTime);
            var endTime = BusinessValidation.ToUtc(candidate.EndTime);
            if (startTime.HasValue && endTime.HasValue && endTime.Value < startTime.Value)
            {
                throw new BusinessValidationException(
                    $"End time cannot be earlier than start time for student {studentId}.");
            }

            var requestedCoreCount = BusinessValidation.PositiveCount(
                candidate.RequestedCoreCount,
                nameof(candidate.RequestedCoreCount));
            var maximumDeepCount = BusinessValidation.NonNegative(
                candidate.MaximumDeepCount,
                nameof(candidate.MaximumDeepCount));
            candidates.Add(new NormalizedCandidate(
                studentId,
                startTime,
                endTime,
                requestedCoreCount,
                maximumDeepCount,
                index));
            index++;
        }

        var sorted = candidates
            .OrderBy(candidate => candidate.StartTime ?? DateTime.MinValue)
            .ThenBy(candidate => candidate.StudentId)
            .ThenBy(candidate => candidate.Index)
            .ToList();
        return new NormalizedAllocationRequest(subjectId, sorted);
    }

    private static void ValidateFeasibility(
        IReadOnlyList<NormalizedCandidate> candidates,
        int coreQuestionCount)
    {
        var maximumRequestedCore = candidates.Max(candidate => candidate.RequestedCoreCount);
        if (coreQuestionCount < maximumRequestedCore)
        {
            throw new BusinessValidationException(
                $"The subject has {coreQuestionCount} core questions, but {maximumRequestedCore} were requested.");
        }

        for (var index = 1; index < candidates.Count; index++)
        {
            var availableForCandidate = coreQuestionCount - candidates[index - 1].RequestedCoreCount;
            if (availableForCandidate < candidates[index].RequestedCoreCount)
            {
                throw new BusinessValidationException(
                    $"The core question pool cannot support non-repeating assignments for students {candidates[index - 1].StudentId} and {candidates[index].StudentId}.");
            }
        }
    }

    private List<Question> SelectDeep(
        IReadOnlyList<Question> questions,
        int count,
        IEnumerable<string> coreTopics)
    {
        if (count <= 0)
        {
            return new List<Question>();
        }

        var topics = coreTopics.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var remaining = Shuffle(questions.ToList());
        var selected = new List<Question>(count);
        var selectedTopics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        while (selected.Count < count && remaining.Count > 0)
        {
            var preferredQuestions = remaining
                .Where(question => topics.Contains(TopicKey(question.Topic))
                    && !selectedTopics.Contains(TopicKey(question.Topic)))
                .ToList();
            if (preferredQuestions.Count == 0)
            {
                preferredQuestions = remaining
                    .Where(question => !selectedTopics.Contains(TopicKey(question.Topic)))
                    .ToList();
            }

            if (preferredQuestions.Count == 0)
            {
                preferredQuestions = remaining;
            }

            var selectedQuestion = preferredQuestions[NextIndex(preferredQuestions.Count)];
            selected.Add(selectedQuestion);
            selectedTopics.Add(TopicKey(selectedQuestion.Topic));
            remaining.Remove(selectedQuestion);
        }

        return selected;
    }

    private List<Question> Shuffle(IReadOnlyList<Question> questions)
    {
        var result = questions.ToList();
        for (var index = result.Count - 1; index > 0; index--)
        {
            var swapIndex = NextIndex(index + 1);
            (result[index], result[swapIndex]) = (result[swapIndex], result[index]);
        }

        return result;
    }

    private int NextIndex(int maxExclusive)
    {
        var value = _randomizer.Next(maxExclusive);
        if (value < 0 || value >= maxExclusive)
        {
            throw new InvalidOperationException("The random source returned an invalid value.");
        }

        return value;
    }

    private static string TopicKey(string? topic)
    {
        return (topic ?? string.Empty).Trim().ToUpperInvariant();
    }

    private sealed record NormalizedCandidate(
        int StudentId,
        DateTime? StartTime,
        DateTime? EndTime,
        int RequestedCoreCount,
        int MaximumDeepCount,
        int Index);

    private sealed record NormalizedAllocationRequest(
        int SubjectId,
        IReadOnlyList<NormalizedCandidate> Candidates);
}
