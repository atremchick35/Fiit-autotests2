using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Services.BookService;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BookServiceTests;

[NonParallelizable]
public class HomeworkTest
{
    private static readonly IServiceProvider Container = new ContainerForBdTests().Build();
    private static readonly IBookService BookService = Container.GetRequiredService<IBookService>();
    
    private static bool ValidateRequiredTextFieldLikeCreateBookModal(string value) => value is { Length: > 0 and <= 20 };

    private static bool ValidateImageLikeCreateBookModal(int imageId) => imageId >= 0;

    private static bool ValidateRubricLikeCreateBookModal(int rubricId) => rubricId >= 0;

    private static bool ValidatePriceLikeCreateBookModal(string price) => !string.IsNullOrEmpty(price);
    
    private static bool CanSubmitLikeCreateBookModal(
        string name,
        string author,
        string description,
        int imageId,
        int rubricId) =>
        ValidateRequiredTextFieldLikeCreateBookModal(name)
        && ValidateRequiredTextFieldLikeCreateBookModal(author)
        && ValidateRequiredTextFieldLikeCreateBookModal(description)
        && ValidateImageLikeCreateBookModal(imageId)
        && ValidateRubricLikeCreateBookModal(rubricId);

    [TestCase("", false, TestName = "Текстовое поле: пустая строка")]
    [TestCase(null, false, TestName = "Текстовое поле: null")]
    [TestCase("a", true, TestName = "Текстовое поле: нижняя граница длины — 1 символ")]
    [TestCase("abcdefghijklmnopqrst", true, TestName = "Текстовое поле: верхняя граница длины — 20 символов")]
    [TestCase("abcdefghijklmnopqrstu", false, TestName = "Текстовое поле: 21 символ — за верхней границей")]
    public void ValidateRequiredTextField_ShouldMatchCreateBookModalRules(string value, bool expectedValid) =>
        ValidateRequiredTextFieldLikeCreateBookModal(value).Should().Be(expectedValid);

    [TestCase(-1, false, TestName = "imageId: -1 — нет изображения")]
    [TestCase(0, true, TestName = "imageId: 0 — граница «есть id»")]
    [TestCase(1, true, TestName = "imageId: положительное значение")]
    public void ValidateImage_ShouldMatchCreateBookModalRules(int imageId, bool expectedValid) =>
        ValidateImageLikeCreateBookModal(imageId).Should().Be(expectedValid);

    [TestCase(-1, false, TestName = "rubricId: -1 — рубрика не выбрана")]
    [TestCase(0, true, TestName = "rubricId: 0 — граница по правилу < 0")]
    [TestCase(1, true, TestName = "rubricId: типичный выбор из списка")]
    public void ValidateRubric_ShouldMatchCreateBookModalRules(int rubricId, bool expectedValid) =>
        ValidateRubricLikeCreateBookModal(rubricId).Should().Be(expectedValid);

    [TestCase("", false, TestName = "price: пустая строка (!price в JS)")]
    [TestCase(null, false, TestName = "price: null")]
    [TestCase("0", true, TestName = "price: \"0\" как в initialState — валидно")]
    [TestCase("500", true, TestName = "price: непустая строка")]
    public void ValidatePrice_ShouldMatchCreateBookModalRules(string price, bool expectedValid) =>
        ValidatePriceLikeCreateBookModal(price).Should().Be(expectedValid);

    [TestCase("N", "A", "D", 0, 1, true, TestName = "Отправка формы: все обязательные проверки пройдены")]
    [TestCase("", "A", "D", 0, 1, false, TestName = "Отправка формы: имя пустое")]
    [TestCase("N", "A", "D", -1, 1, false, TestName = "Отправка формы: нет изображения")]
    [TestCase("N", "A", "D", 0, -1, false, TestName = "Отправка формы: рубрика не выбрана")]
    public void CanSubmit_ShouldMatchCreateBookModalGate(
        string name,
        string author,
        string description,
        int imageId,
        int rubricId,
        bool expected) =>
        CanSubmitLikeCreateBookModal(name, author, description, imageId, rubricId).Should().Be(expected);

    public static IEnumerable<TestCaseData> PairwiseExportBookFilterCases()
    {
        yield return new TestCaseData(
                null,
                null,
                null,
                null,
                null,
                BookOrder.ByLastAdding)
            .SetName("Pairwise: Q R B L O Ord=Last");

        yield return new TestCaseData("", "", false, 1, 0, BookOrder.ByRankAndLastAdding)
            .SetName("Pairwise: Q\"\" R\"\" B=false L=1 O=0 Ord=Rank");

        yield return new TestCaseData("HomeworkPairwise", "___no_such_rubric___", true, 100, 5, BookOrder.ByLastAdding)
            .SetName("Pairwise: Q+ R+ B=true L=100 O=5 Ord=Last");

        yield return new TestCaseData("", "___no_such_rubric___", null, null, 5, BookOrder.ByRankAndLastAdding)
            .SetName("Pairwise: Q\"\" R+ B∅ L∅ O=5 Ord=Rank");

        yield return new TestCaseData("HomeworkPairwise", "", true, null, 0, BookOrder.ByRankAndLastAdding)
            .SetName("Pairwise: Q+ R\"\" B=true L∅ O=0 Ord=Rank");

        yield return new TestCaseData(null, "", null, 1, null, BookOrder.ByLastAdding)
            .SetName("Pairwise: Q∅ R\"\" B∅ L=1 O∅ Ord=Last");

        yield return new TestCaseData("HomeworkPairwise", null, false, 100, null, BookOrder.ByLastAdding)
            .SetName("Pairwise: Q+ R∅ B=false L=100 O∅ Ord=Last");

        yield return new TestCaseData(null, "___no_such_rubric___", true, 1, 0, BookOrder.ByLastAdding)
            .SetName("Pairwise: Q∅ R+ B=true L=1 O=0 Ord=Last");

        yield return new TestCaseData("", null, false, 100, 5, BookOrder.ByRankAndLastAdding)
            .SetName("Pairwise: Q\"\" R∅ B=false L=100 O=5 Ord=Rank");

        yield return new TestCaseData("HomeworkPairwise", "", null, 1, 5, BookOrder.ByLastAdding)
            .SetName("Pairwise: Q+ R\"\" B∅ L=1 O=5 Ord=Last");
    }

    [TestCaseSource(nameof(PairwiseExportBookFilterCases))]
    public async Task ExportBooksToXmlAsync_PairwiseFilter_ShouldReturnXmlDocument(
        string query,
        string rubricSynonym,
        bool? isBusy,
        int? limit,
        int? offset,
        BookOrder order)
    {
        var filter = new BookFilter
        {
            Query = query,
            RubricSynonym = rubricSynonym,
            IsBusy = isBusy,
            Limit = limit,
            Offset = offset,
            Order = order
        };

        var xml = await BookService.ExportBooksToXmlAsync(filter, CancellationToken.None);

        xml.Should().NotBeNullOrWhiteSpace().And.Contain("<Books>");
    }
}
