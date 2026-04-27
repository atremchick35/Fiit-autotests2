using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Events;
using Kontur.BigLibrary.Service.Exceptions;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Service.Services.EventService;
using Kontur.BigLibrary.Service.Services.ImageService;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BookServiceTests;

[NonParallelizable]
public class BookServiceMockTest
{
    private static (IServiceProvider Provider,
        IBookService BookService,
        IBookRepository BookRepository,
        IImageService ImageService,
        IEventService EventService,
        ISynonymMaker SynonymMaker) CreateContainer()
    {
        var services = new ServiceCollection();

        var bookRepository = Substitute.For<IBookRepository>();
        var imageService = Substitute.For<IImageService>();
        var eventService = Substitute.For<IEventService>();
        var synonymMaker = Substitute.For<ISynonymMaker>();

        services.AddSingleton(bookRepository);
        services.AddSingleton(imageService);
        services.AddSingleton(eventService);
        services.AddSingleton(synonymMaker);
        services.AddSingleton<IBookService, BookService>();

        var provider = services.BuildServiceProvider();

        return (
            provider,
            provider.GetRequiredService<IBookService>(),
            bookRepository,
            imageService,
            eventService,
            synonymMaker
        );
    }

    [Test]
    public async Task SaveBookAsync_ReturnSameBook_WhenSaveCorrectBook()
    {
        var container = CreateContainer();
        using var _ = container.Provider as IDisposable;

        container.SynonymMaker.Create("Database Systems. The Complete Book").Returns("book-synonym");

        container.BookRepository.GetMaxBookIdAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<int?>(10));

        container.BookRepository.GetRubricAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Rubric { Id = 1 }));

        container.ImageService.GetAsync(100, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Image { Id = 100, Data = Array.Empty<byte>() }));

        container.BookRepository.GetBookBySynonymAsync("book-synonym", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Book>(null));

        container.BookRepository.SaveBookAsync(Arg.Any<Book>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => Task.FromResult(callInfo.Arg<Book>()));

        container.BookRepository.SaveBookIndexAsync(Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        container.EventService.PublishEventAsync(Arg.Any<ChangedEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D. Ullman, Jennifer Widom",
            RubricId = 1,
            ImageId = 100,
            Description = "New_book",
            Count = 3,
            Price = "500"
        };

        var result = await container.BookService.SaveBookAsync(book, CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(11);
        result.Name.Should().Be(book.Name);
        result.Author.Should().Be(book.Author);
        result.Description.Should().Be(book.Description);
        result.RubricId.Should().Be(book.RubricId);
        result.ImageId.Should().Be(book.ImageId);
        result.Count.Should().Be(book.Count);
        result.Price.Should().Be(book.Price);

        await container.BookRepository.Received(1).SaveBookAsync(
            Arg.Is<Book>(b =>
                b.Id == 11 &&
                b.Name == book.Name &&
                b.Author == book.Author &&
                b.Description == book.Description &&
                b.RubricId == book.RubricId &&
                b.ImageId == book.ImageId &&
                b.Count == book.Count &&
                b.Price == book.Price),
            Arg.Any<CancellationToken>());

        await container.BookRepository.Received(1).SaveBookIndexAsync(
            11,
            Arg.Any<string>(),
            "book-synonym",
            Arg.Any<CancellationToken>());

        await container.EventService.Received(1).PublishEventAsync(
            Arg.Any<ChangedEvent>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task SaveBookAsync_ShouldNotCreateBook_WhenRubricDoesNotExist()
    {
        var container = CreateContainer();
        using var _ = container.Provider as IDisposable;

        container.BookRepository.GetRubricAsync(int.MaxValue, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Rubric>(null));

        container.ImageService.GetAsync(100, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Image { Id = 100, Data = Array.Empty<byte>() }));

        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D. Ullman, Jennifer Widom",
            RubricId = int.MaxValue,
            ImageId = 100,
            Description = "New_book",
            Count = 3,
            Price = "500"
        };

        var act = () => container.BookService.SaveBookAsync(book, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая рубрика.");

        await container.BookRepository.DidNotReceiveWithAnyArgs().SaveBookAsync(default!, default);
        await container.BookRepository.DidNotReceiveWithAnyArgs().SaveBookIndexAsync(default, default!, default!, default);
        await container.EventService.DidNotReceiveWithAnyArgs().PublishEventAsync(default!, default);
    }

    [Test]
    public async Task SaveBookAsync_ShouldNotCreateBook_WhenImageDoesNotExist()
    {
        var container = CreateContainer();
        using var _ = container.Provider as IDisposable;

        container.BookRepository.GetRubricAsync(1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new Rubric { Id = 1 }));

        container.ImageService.GetAsync(int.MaxValue, null, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Image>(null));

        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D. Ullman, Jennifer Widom",
            RubricId = 1,
            ImageId = int.MaxValue,
            Description = "New_book",
            Count = 3,
            Price = "500"
        };

        var act = () => container.BookService.SaveBookAsync(book, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая картинка.");

        await container.BookRepository.DidNotReceiveWithAnyArgs().SaveBookAsync(default!, default);
        await container.BookRepository.DidNotReceiveWithAnyArgs().SaveBookIndexAsync(default, default!, default!, default);
        await container.EventService.DidNotReceiveWithAnyArgs().PublishEventAsync(default!, default);
    }
}