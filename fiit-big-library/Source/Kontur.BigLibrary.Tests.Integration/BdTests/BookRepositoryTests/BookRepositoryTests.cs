using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Tests.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BdTests.BookRepositoryTests;

[Parallelizable(ParallelScope.All)]
public class BookRepositoryTests
{
    private static readonly SemaphoreSlim DbGate = new(1, 1);
    private static int nextBookId = 1000000;
    private readonly IServiceProvider serviceProvider;
    private readonly IBookRepository bookRepository;
    private readonly ConcurrentBag<int> createdBookIds = new();
    private const int RetryCount = 30;
    private const int RetryDelayMs = 100;

    private static readonly string LongDescription = string.Join(
        " ",
        Enumerable.Repeat(
            "lorem ipsum dolor sit amet consectetur adipiscing elit sed do eiusmod tempor incididunt ut labore et dolore magna aliqua",
            80));

    public BookRepositoryTests()
    {
        serviceProvider = new Container().Build();
        bookRepository = serviceProvider.GetRequiredService<IBookRepository>();
    }

    [SetUp]
    public async Task SetUp()
    {
        await DbGate.WaitAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        try
        {
            foreach (var id in createdBookIds)
            {
                await bookRepository.DeleteBookAsync(id, CancellationToken.None);
                await bookRepository.DeleteBookIndexAsync(id, CancellationToken.None);
            }
        }
        finally
        {
            DbGate.Release();
        }
    }

    [Test]
    public async Task GetBook_Exists_ReturnBook()
    {
        var expectedBook = await CreateBookAsync(b => b
            .WithName("getbookexists")
            .WithAuthor("authora"));

        var actualBook = await bookRepository.GetBookAsync(expectedBook.Id!.Value, CancellationToken.None);

        actualBook.Should().BeEquivalentTo(expectedBook);
    }

    [Test]
    public async Task GetBook_LongDescription_ReturnBook()
    {
        var expectedBook = await CreateBookAsync(b => b
            .WithName("longdescriptionbook")
            .WithAuthor("authorb")
            .WithDescription(LongDescription));

        var actualBook = await bookRepository.GetBookAsync(expectedBook.Id!.Value, CancellationToken.None);

        actualBook.Should().BeEquivalentTo(expectedBook);
    }

    [Test]
    public async Task GetBook_NotExists_ReturnNull()
    {
        var actualBook = await bookRepository.GetBookAsync(GetNextBookId(), CancellationToken.None);

        actualBook.Should().BeNull();
    }

    [Test]
    public async Task SelectBooks_DeletedBook_IsNotReturned()
    {
        var token = $"deletedtoken{Guid.NewGuid():N}";

        var deletedBook = await CreateBookAsync(b => b
            .WithName($"{token}book")
            .WithAuthor("authore")
            .Delete());

        var result = await WaitForBooksAsync(
            () => bookRepository.SelectBooksAsync(new BookFilter { Query = token }, CancellationToken.None),
            books => true);

        result.Should().NotContain(x => x.Id == deletedBook.Id);
    }

    [Test]
    public async Task SelectBooks_SortAndOffset_AreApplied()
    {
        var token = $"sorttoken{Guid.NewGuid():N}";

        var firstBook = await CreateBookAsync(b => b
            .WithName($"{token}first")
            .WithAuthor("author1")
            .WithDescription(token));
        await Task.Delay(1200);

        var secondBook = await CreateBookAsync(b => b
            .WithName($"{token}second")
            .WithAuthor("author2")
            .WithDescription(token));
        await Task.Delay(1200);

        var thirdBook = await CreateBookAsync(b => b
            .WithName($"{token}third")
            .WithAuthor("author3")
            .WithDescription(token));

        var result = await WaitForBooksAsync(
            () => bookRepository.SelectBooksAsync(new BookFilter
            {
                Query = token,
                Order = BookOrder.ByLastAdding,
                Offset = 1,
                Limit = 2
            }, CancellationToken.None),
            books => books.Count == 2);

        result.Should().HaveCount(2);
        result.Select(x => x.Id).Should().Contain(secondBook.Id);
        result.Select(x => x.Id).Should().Contain(firstBook.Id);
        result.Select(x => x.Id).Should().NotContain(thirdBook.Id);
    }

    [Test]
    public async Task GetBookSummaryBySynonym_MatchesSelectBooksSummary()
    {
        var book = await CreateBookAsync(b => b
            .WithName("summarybook")
            .WithAuthor("authorf"));

        var synonym = $"book {book.Id!.Value}";

        var summaries = await WaitForSummariesAsync(
            () => bookRepository.SelectBooksSummaryAsync(new BookFilter { Synonym = synonym }, CancellationToken.None),
            list => list.Count == 1);

        var summaryBySynonym = await bookRepository.GetBookSummaryBySynonymAsync(synonym, CancellationToken.None);

        summaryBySynonym.Should().BeEquivalentTo(summaries.Single());
    }

    [Test]
    public async Task GetBookSummaryBySynonym_WhenReaderExists_BookIsBusy()
    {
        var book = await CreateBookAsync(b => b
            .WithName("busybook")
            .WithAuthor("authorg"));

        await CreateReaderAsync(book.Id!.Value);

        var summary = await WaitForSummaryAsync(
            () => bookRepository.GetBookSummaryBySynonymAsync($"book {book.Id.Value}", CancellationToken.None),
            x => x.IsBusy);

        summary.IsBusy.Should().BeTrue();
    }

    private async Task<Book> CreateBookAsync(Action<BookBuilder> configure)
    {
        var builder = serviceProvider.GetRequiredService<BookBuilder>();
        configure(builder);

        var book = builder
            .WithId(GetNextBookId())
            .Build();

        await bookRepository.SaveBookAsync(book, CancellationToken.None);
        await bookRepository.SaveBookIndexAsync(
            book.Id!.Value,
            book.GetTextForFts(),
            $"book {book.Id.Value}",
            CancellationToken.None);

        createdBookIds.Add(book.Id.Value);

        return book;
    }

    private async Task CreateReaderAsync(int bookId)
    {
        var readerBuilder = serviceProvider.GetRequiredService<ReaderBuilder>();

        var reader = readerBuilder
            .WithBookId(bookId)
            .WithUserName($"reader_{bookId}")
            .WithStartDate(DateTime.UtcNow)
            .Build();

        await bookRepository.SaveReaderAsync(reader, CancellationToken.None);
    }

    private static int GetNextBookId()
    {
        return Interlocked.Increment(ref nextBookId);
    }

    private async Task<IReadOnlyList<Book>> WaitForBooksAsync(
        Func<Task<IReadOnlyList<Book>>> action,
        Func<IReadOnlyList<Book>, bool> condition)
    {
        Exception? lastError = null;

        for (var i = 0; i < RetryCount; i++)
        {
            try
            {
                var result = await action();
                if (condition(result))
                    return result;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(RetryDelayMs);
        }

        if (lastError != null)
            throw lastError;

        return await action();
    }

    private async Task<IReadOnlyList<BookSummary>> WaitForSummariesAsync(
        Func<Task<IReadOnlyList<BookSummary>>> action,
        Func<IReadOnlyList<BookSummary>, bool> condition)
    {
        Exception lastError = null;

        for (var i = 0; i < RetryCount; i++)
        {
            try
            {
                var result = await action();
                if (condition(result))
                    return result;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(RetryDelayMs);
        }

        if (lastError != null)
            throw lastError;

        return await action();
    }

    private async Task<BookSummary> WaitForSummaryAsync(
        Func<Task<BookSummary>> action,
        Func<BookSummary, bool> condition)
    {
        Exception lastError = null;

        for (var i = 0; i < RetryCount; i++)
        {
            try
            {
                var result = await action();
                if (condition(result))
                    return result;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            await Task.Delay(RetryDelayMs);
        }

        if (lastError != null)
            throw lastError;

        return await action();
    }
}