using AssignmentPRN.Business;
using AssignmentPRN.DataAccess.Data;
using AssignmentPRN.DataAccess.Entities;
using AssignmentPRN.DataAccess.Enums;
using AssignmentPRN.DataAccess.Repositories;

namespace AssignmentPRN.Tests;

public sealed class QuestionAllocationServiceTests
{
    [Fact]
    public void NullableUtcDateTimeConverter_RoundTripsLocalTimeAsUtc()
    {
        var converter = new NullableUtcDateTimeConverter();
        var localValue = DateTime.SpecifyKind(
            new DateTime(2026, 10, 1, 9, 30, 0),
            DateTimeKind.Local);

        var storedValue = (DateTime?)converter.ConvertToProvider(localValue);
        var loadedValue = (DateTime?)converter.ConvertFromProvider(storedValue);

        Assert.Equal(localValue.ToUniversalTime(), storedValue);
        Assert.Equal(DateTimeKind.Utc, loadedValue!.Value.Kind);
        Assert.Equal(localValue, loadedValue.Value.ToLocalTime());
    }

    [Fact]
    public async Task AllocateAsync_DoesNotRepeatQuestionsBetweenConsecutiveCandidates()
    {
        var service = CreateService(CreateQuestionBank(4, 4));
        var response = await service.AllocateAsync(new QuestionAllocationRequest
        {
            SubjectId = 1,
            Candidates = new[]
            {
                CreateCandidate(2, 2, new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc)),
                CreateCandidate(1, 2, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc))
            }
        });

        Assert.True(response.Success, response.Error ?? "Allocation failed.");
        var candidates = Assert.IsType<List<QuestionAllocationCandidateResponse>>(response.Data!.Candidates);
        Assert.Equal(2, candidates.Count);
        Assert.Equal(1, candidates[0].StudentId);
        Assert.Equal(2, candidates[1].StudentId);
        Assert.Equal(2, candidates[0].ActualCoreCount);
        Assert.Equal(2, candidates[0].ActualDeepCount);
        Assert.Equal(2, candidates[1].ActualCoreCount);
        Assert.Equal(2, candidates[1].ActualDeepCount);

        var firstIds = candidates[0].Assignments.Select(assignment => assignment.QuestionId).ToHashSet();
        var secondIds = candidates[1].Assignments.Select(assignment => assignment.QuestionId).ToHashSet();

        Assert.Empty(firstIds.Intersect(secondIds));
    }

    [Fact]
    public async Task AllocateAsync_RejectsCorePoolThatCannotAvoidAdjacentRepetition()
    {
        var service = CreateService(CreateQuestionBank(3, 4));
        var response = await service.AllocateAsync(new QuestionAllocationRequest
        {
            SubjectId = 1,
            Candidates = new[]
            {
                CreateCandidate(1, 2, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
                CreateCandidate(2, 2, new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc))
            }
        });

        Assert.False(response.Success);
        Assert.Contains("cannot support non-repeating", response.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AllocateAsync_UsesDeepQuestionsOnlyUpToAvailableNonRepeatingPool()
    {
        var service = CreateService(CreateQuestionBank(4, 1));
        var response = await service.AllocateAsync(new QuestionAllocationRequest
        {
            SubjectId = 1,
            Candidates = new[]
            {
                CreateCandidate(1, 2, new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
                CreateCandidate(2, 2, new DateTime(2026, 10, 1, 9, 30, 0, DateTimeKind.Utc))
            }
        });

        Assert.True(response.Success, response.Error ?? "Allocation failed.");
        var candidates = response.Data!.Candidates;
        Assert.Equal(1, candidates[0].ActualDeepCount);
        Assert.Equal(0, candidates[1].ActualDeepCount);
        Assert.Equal(2, response.Data.Warnings.Count);

        var firstIds = candidates[0].Assignments.Select(assignment => assignment.QuestionId).ToHashSet();
        var secondIds = candidates[1].Assignments.Select(assignment => assignment.QuestionId).ToHashSet();
        Assert.Empty(firstIds.Intersect(secondIds));
    }

    private static QuestionAllocationService CreateService(IReadOnlyList<Question> questions)
    {
        return new QuestionAllocationService(
            new StubQuestionRepository(questions),
            new ZeroRandomizer());
    }

    private static QuestionAllocationCandidateRequest CreateCandidate(
        int studentId,
        int coreCount,
        DateTime startTime)
    {
        return new QuestionAllocationCandidateRequest
        {
            StudentId = studentId,
            StartTime = startTime,
            EndTime = startTime.AddMinutes(30),
            RequestedCoreCount = coreCount,
            MaximumDeepCount = 2
        };
    }

    private static IReadOnlyList<Question> CreateQuestionBank(int coreCount, int deepCount)
    {
        var questions = new List<Question>();
        for (var index = 1; index <= coreCount; index++)
        {
            questions.Add(CreateQuestion(index, QuestionCategory.Core, $"Core topic {index}", index % 3 + 1));
        }

        for (var index = 1; index <= deepCount; index++)
        {
            questions.Add(CreateQuestion(
                coreCount + index,
                QuestionCategory.Deep,
                $"Core topic {(index % coreCount) + 1}",
                index % 3 + 3));
        }

        return questions;
    }

    private static Question CreateQuestion(
        int id,
        QuestionCategory category,
        string topic,
        int difficulty)
    {
        return new Question
        {
            Id = id,
            SubjectId = 1,
            Category = category,
            Topic = topic,
            Content = $"Question {id}",
            Difficulty = difficulty
        };
    }

    private sealed class StubQuestionRepository : IQuestionRepository
    {
        private readonly IReadOnlyList<Question> _questions;

        public StubQuestionRepository(IReadOnlyList<Question> questions)
        {
            _questions = questions;
        }

        public Task<IReadOnlyList<Question>> ListBySubjectAsync(
            int subjectId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_questions);
        }

        public Task<Question?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_questions.SingleOrDefault(question => question.Id == id));
        }

        public Task<Question> CreateAsync(
            Question question,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task UpdateAsync(
            Question question,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task DeleteAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class ZeroRandomizer : IQuestionRandomizer
    {
        public int Next(int maxExclusive)
        {
            return 0;
        }
    }
}
