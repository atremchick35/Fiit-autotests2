using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Kontur.BigLibrary.Service.Contracts;
using Kontur.BigLibrary.Service.Exceptions;
using Kontur.BigLibrary.Service.Services.BookService;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Service.Services.ImageService;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Kontur.BigLibrary.Tests.Integration.BookServiceTests;

[NonParallelizable]
public class BookServiceTest
{
    #region WithContainer

    private static readonly IServiceProvider container = new ContainerForBdTests().Build();
    private static readonly IBookService bookService = container.GetRequiredService<IBookService>();
    private static readonly IImageService imageService = container.GetRequiredService<IImageService>();
    private static readonly IBookRepository bookRepository = container.GetRequiredService<IBookRepository>();

    #endregion

    [Test]
    public async Task SaveBookAsync_ReturnSameBook_WhenSaveCorrectBook()
    {
        var imageForSave = new Image { Data = Array.Empty<byte>() };
        var image = await imageService.SaveAsync(imageForSave, CancellationToken.None).ConfigureAwait(false);

        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D. Ullman, Jennifer Widom",
            RubricId = 1,
            ImageId = image.Id!.Value,
            Description = "New_book",
            Count = 3,
            Price = "500"
        };

        var result = await bookService.SaveBookAsync(book, CancellationToken.None).ConfigureAwait(false);

        result.Should().NotBeNull();
        result.Id.Should().NotBeNull();

        result.Name.Should().Be(book.Name);
        result.Author.Should().Be(book.Author);
        result.Description.Should().Be(book.Description);
        result.RubricId.Should().Be(book.RubricId);
        result.ImageId.Should().Be(book.ImageId);
        result.Count.Should().Be(book.Count);
        result.Price.Should().Be(book.Price);

        var savedBook = await bookService.GetBookAsync(result.Id!.Value, CancellationToken.None).ConfigureAwait(false);

        savedBook.Should().NotBeNull();
        savedBook.Id.Should().Be(result.Id);
        savedBook.Name.Should().Be(book.Name);
        savedBook.Author.Should().Be(book.Author);
        savedBook.Description.Should().Be(book.Description);
        savedBook.RubricId.Should().Be(book.RubricId);
        savedBook.ImageId.Should().Be(book.ImageId);
        savedBook.Count.Should().Be(book.Count);
        savedBook.Price.Should().Be(book.Price);
    }

    [Test]
    public async Task SaveBookAsync_ShouldNotCreateBook_WhenRubricDoesNotExist()
    {
        var imageForSave = new Image { Data = Array.Empty<byte>() };
        var image = await imageService.SaveAsync(imageForSave, CancellationToken.None).ConfigureAwait(false);

        var maxBookIdBefore = await bookRepository.GetMaxBookIdAsync(CancellationToken.None).ConfigureAwait(false);

        var book = new Book
        {
            Name = "Database Systems. The Complete Book",
            Author = "Hector Garcia-Molina, Jeffrey D. Ullman, Jennifer Widom",
            RubricId = int.MaxValue,
            ImageId = image.Id!.Value,
            Description = "New_book",
            Count = 3,
            Price = "500"
        };

        var act = () => bookService.SaveBookAsync(book, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая рубрика.");

        var maxBookIdAfter = await bookRepository.GetMaxBookIdAsync(CancellationToken.None).ConfigureAwait(false);
        maxBookIdAfter.Should().Be(maxBookIdBefore);
    }

    [Test]
    public async Task SaveBookAsync_ShouldNotCreateBook_WhenImageDoesNotExist()
    {
        var maxBookIdBefore = await bookRepository.GetMaxBookIdAsync(CancellationToken.None).ConfigureAwait(false);

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

        var act = () => bookService.SaveBookAsync(book, CancellationToken.None);

        await act.Should()
            .ThrowAsync<ValidationException>()
            .WithMessage("Указана несуществующая картинка.");

        var maxBookIdAfter = await bookRepository.GetMaxBookIdAsync(CancellationToken.None).ConfigureAwait(false);
        maxBookIdAfter.Should().Be(maxBookIdBefore);
    }
}