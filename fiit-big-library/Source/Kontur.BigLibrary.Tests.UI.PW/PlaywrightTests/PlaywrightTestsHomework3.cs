using FluentAssertions;
using Kontur.BigLibrary.Tests.Core.Helpers.StringGenerator;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Controls;
using Kontur.BigLibrary.Tests.UI.PW.PageObjects.Pages;
using Kontur.BigLibrary.Tests.UI.PW.PlaywrightCore;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

[NonParallelizable]
public class PlaywrightTestsHomework3 : TestBase
{
    [Test]
    public async Task BookCreatedByFirstUser_IsVisibleForSecondUser()
    {
        var password = StringGenerator.GetValidPassword();
        var user1Email = StringGenerator.GetEmail();
        var user2Email = StringGenerator.GetEmail();
        var bookName = StringGenerator.GetRandomString(14);
        var author = StringGenerator.GetRandomString(10);

        TestData.GetOrCreateUserAndGetToken(user1Email, password);
        TestData.GetOrCreateUserAndGetToken(user2Email, password);

        var loginPage = await Navigation.GoToPageAsync<LoginPage>();
        await SignInAsync(loginPage, user1Email, password);

        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        await AddBookViaUiAsync(mainPage, bookName, author);
        await mainPage.BookList.GetBookItem(bookName).WaitVisibleAsync();

        await LogoutViaJwtTokenAsync(mainPage);

        loginPage = await Navigation.GoToPageAsync<LoginPage>();
        await SignInAsync(loginPage, user2Email, password);

        mainPage = await Navigation.GoToPageAsync<MainPage>();
        await mainPage.BookList.GetBookItem(bookName).WaitVisibleAsync();
    }

    [Test]
    public async Task BookCreatedViaUi_IsReturnedByApi()
    {
        var password = StringGenerator.GetValidPassword();
        var userEmail = StringGenerator.GetEmail();
        var bookName = StringGenerator.GetRandomString(14);
        var author = StringGenerator.GetRandomString(10);

        TestData.GetOrCreateUserAndGetToken(userEmail, password);

        var loginPage = await Navigation.GoToPageAsync<LoginPage>();
        await SignInAsync(loginPage, userEmail, password);

        var mainPage = await Navigation.GoToPageAsync<MainPage>();
        await AddBookViaUiAsync(mainPage, bookName, author);
        await mainPage.BookList.GetBookItem(bookName).WaitVisibleAsync();

        var booksFromApi = TestData.GetAllBooks();
        booksFromApi.Should().Contain(book => book.Name == bookName && book.Author == author);
    }

    private async Task SignInAsync(LoginPage loginPage, string email, string password)
    {
        await loginPage.Email.FillAsync(email);
        await loginPage.Password.FillAsync(password);
        await loginPage.SignInButton.ClickAsync();
    }

    private async Task LogoutViaJwtTokenAsync(PageBase page)
    {
        await page.Page.EvaluateAsync("() => localStorage.removeItem('jwtToken')");
        await page.Page.ReloadAsync();
    }

    private async Task AddBookViaUiAsync(MainPage mainPage, string bookName, string author)
    {
        var modal = await mainPage.AddBookButton.ClickAndOpenModalAsync<AddBookModal>();
        await modal.WaitVisibleAsync();
        await modal.NameInput.FillAsync(bookName);
        await modal.AuthorInput.FillAsync(author);
        await modal.DescriptionInput.FillAsync(StringGenerator.GetRandomString(20));
        await modal.RubricDropdown.SelectByText("Администрирование");
        await modal.UploadImage.SetInputFilesAsync(TestData.ValidImagePath);
        await modal.AddBookSubmit.ClickAsync();
        await modal.WaitInvisibleAsync();
        await mainPage.RefreshAsync();
    }
}
