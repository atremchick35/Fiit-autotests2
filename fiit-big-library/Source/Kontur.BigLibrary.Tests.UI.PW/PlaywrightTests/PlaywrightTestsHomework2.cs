using FluentAssertions;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

[NonParallelizable]
[WithAuth("test@mail.com", "Test123456!")]
public class PlaywrightTestsHomework2 : TestBase
{
    private const string BookTitle = "Оптимизация игр в Unity 5";
    private const string BookWriter = "Крис Дикинсон";

    [Test]
    public async Task ViewModeToggle_WhenSwitched_ChangesLayout()
    {
        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        await mainPage.ChangeView.ClickAsync();
        await mainPage.BooksTable.WaitVisibleAsync();
    }

    [Test]
    public async Task BookDetails_WhenOpened_DisplaysValidInfo()
    {
        var bookPage = await OpenBookDetailsAsync();

        await bookPage.BookName.CheckTextAsync(BookTitle);
        await bookPage.BookAuthor.CheckTextAsync(BookWriter);
    }

    [Test]
    public async Task BookStatusChange_WhenToggled_ThenUpdatesCorrectly()
    {
        var bookPage = await OpenBookDetailsAsync();
        await bookPage.FreeState.WaitVisibleAsync();

        await bookPage.CheckoutBook.ClickAsync();
        await bookPage.BusyState.WaitVisibleAsync();
        await bookPage.ReturnBook.ClickAsync();
        await bookPage.FreeState.WaitVisibleAsync();
    }

    [Test]
    public async Task BookFilter_WhenOnlyAvailableSelected_ThenShowsOnlyFreeBooks()
    {
        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        var totalBooksBefore = await mainPage.BookList.GetBooksCountAsync();
        var bookPage = await OpenBookDetailsAsync(mainPage);
        await bookPage.CheckoutBook.ClickAsync();

        mainPage = await bookPage.AllBooks.ClickAndOpenPageAsync<MainPage>();

        await mainPage.FreeOnlyFilter.ClickAsync();
        var totalBooksAfter = await mainPage.BookList.GetBooksCountAsync();
        totalBooksBefore.Should().Be(totalBooksAfter + 1);
        bookPage = await OpenBookDetailsAsync(mainPage);
        await bookPage.ReturnBook.ClickAsync();
    }

    private async Task<BookPage> OpenBookDetailsAsync(MainPage? mainPage = null)
    {
        mainPage ??= await Navigation.GoToPageAsync<MainPage>();
        var book = mainPage.BookList.GetBookItem(BookTitle);
        return await book.ClickAndOpenPageAsync<BookPage>();
    }
}