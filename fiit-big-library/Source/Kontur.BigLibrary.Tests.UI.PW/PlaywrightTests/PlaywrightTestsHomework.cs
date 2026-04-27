using FluentAssertions;
using Microsoft.Playwright;

namespace Kontur.BigLibrary.Tests.UI.PW.PlaywrightTests;

public class PlaywrightTestsHomework
{
    private static readonly Random random = new();
    private readonly string validEmail = random.Next() + "@xx.com";
    private readonly string validPassword = "12345678Qwe!";
    
    private IPlaywright playwright;
    private IBrowser browser;
    private IPage page;
    
    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        playwright = await Playwright.CreateAsync();
        
        var launchOptions = new BrowserTypeLaunchOptions { Headless = false };
        browser = await playwright.Chromium.LaunchAsync(launchOptions);

        var context = await browser.NewContextAsync();
        page = await context.NewPageAsync();

        await page.GotoAsync("http://localhost:5000/");

        var registrationLink = page.Locator("a[href='/register']");
        await registrationLink.ClickAsync();

        var email = page.Locator("input.form-control[type=email]");
        await email.FillAsync(validEmail);

        var password = page.Locator("input#password[type=password]");
        await password.FillAsync(validPassword);

        var passwordConfirmation = page.Locator("input#password-confirmation[type=password]");
        await passwordConfirmation.FillAsync(validPassword);

        var registrationButton = page.Locator("button[type=submit]");
        await registrationButton.ClickAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        if (browser != null)
        {
            await browser.CloseAsync(); 
        }
        playwright?.Dispose();
    }

    [Test]
    public async Task UserRegistration_Success()
    {
        await page.GotoAsync("http://localhost:5000/");

        var searchBookText = "оптимизация игр";
        
        var searchField = page.Locator("[data-tid='search-input']");
        await searchField.FillAsync(searchBookText);
        await searchField.PressAsync("Enter");
        
        await Assertions
            .Expect(page.Locator("[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']"))
            .ToBeVisibleAsync();
    }

    [Test]
    public async Task SecondAvailableBookInList()
    {
        await page.GotoAsync("http://localhost:5000/");
        
        var allBooks = page.Locator("a[data-tid^='bookItem']");
        var secondBook = allBooks.Nth(1);

        await Assertions.Expect(secondBook).ToHaveAttributeAsync("data-tid", "bookItem-Rukovodstvo_k_svodu_znaniy_po_upravleniyu_proektami");
    }

    [Test]
    public async Task CheckToggleBookList()
    {
        await page.GotoAsync("http://localhost:5000/");

        var bookToGet = page.Locator("[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");

        await bookToGet.ClickAsync();
        await page
            .Locator("button.btn.btn-primary")
            .Filter(new LocatorFilterOptions { HasText = "Взять книгу" })
            .ClickAsync();
        
        await page.GotoAsync("http://localhost:5000/");
        
        var toggle = page.Locator("input[type=checkbox]");
        await toggle.ClickAsync();
        var allBooks = page.Locator("a[data-tid^='bookItem']");
        var amountOfBooks = await allBooks.CountAsync();
        amountOfBooks.Should().Be(1);
        
        //Возвращаем на место
        await toggle.ClickAsync();
        var bookToReturn = page.Locator("[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await bookToReturn.ClickAsync();
        await page
            .Locator("button.btn.btn-primary")
            .Filter(new LocatorFilterOptions { HasText = "Вернуть книгу" })
            .ClickAsync();
    }

    [Test]
    public async Task GetBookButton()
    {
        await page.GotoAsync("http://localhost:5000/");
        
        var bookToGet = page.Locator("[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");

        await bookToGet.ClickAsync();
        await page
            .Locator("button.btn.btn-primary")
            .Filter(new LocatorFilterOptions { HasText = "Взять книгу" })
            .ClickAsync();

        await Assertions
            .Expect(page.Locator("[data-tid=\"StateLabelBusy\"]"))
            .ToHaveTextAsync("ЗАНЯТА");
        
        //Возвращаем на место
        await page.GotoAsync("http://localhost:5000/");
        var bookToReturn = page.Locator("[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await bookToReturn.ClickAsync();
        await page
            .Locator("button.btn.btn-primary")
            .Filter(new LocatorFilterOptions { HasText = "Вернуть книгу" })
            .ClickAsync();
    }

    [Test]
    public async Task ReturnToAllBooks_Success()
    {
        await page.GotoAsync("http://localhost:5000/");
        var allBooks = page.Locator("a[data-tid='bookItem-Optimizatsiya_igr_v_Unity_5']");
        await allBooks.ClickAsync();
        
        await page.Locator("a:has-text('Все книги')")
            .ClickAsync(new LocatorClickOptions { Force = true });
        
        var mainTitle = page.Locator("a[data-tid='titleLink']");
        var mainTitleText = await mainTitle.TextContentAsync();

        mainTitleText.Should().Be("Библиотека");
    }

    [Test]
    public async Task ModalWindowHeader()
    {
        await page.GotoAsync("http://localhost:5000/");
        
        await page.Locator("[data-tid=\"book-add\"]").ClickAsync();
        
        var modal = page.GetByRole(AriaRole.Dialog);

        await Assertions
            .Expect(modal.Locator("h5.modal-title"))
            .ToHaveTextAsync("Добавить книгу");
    }

    [Test]
    public async Task ModalWindowBookNameField()
    {
        await page.GotoAsync("http://localhost:5000/");
        
        await page.Locator("[data-tid=\"book-add\"]").ClickAsync();
        
        var modal = page.GetByRole(AriaRole.Dialog);

        var input = modal.GetByLabel("Название книги");

        await Assertions.Expect(input).ToBeVisibleAsync();
    }

    [Test]
    public async Task ModalWindowCreateBookButton()
    {
        await page.GotoAsync("http://localhost:5000/");
    
        await page.Locator("[data-tid=\"book-add\"]").ClickAsync();

        await page.Locator("#bookName").FillAsync("a");
        await page.Locator("#bookAuthor").FillAsync("b");
        await page.Locator("#bookDescription").FillAsync("c");
        
        var fileInput = page.Locator("input[type='file']");

        await Assertions.Expect(fileInput).ToBeVisibleAsync();

        var emptyFile = new FilePayload
        {
            Name = "empty.txt",
            MimeType = "text/plain",
            Buffer = []
        };

        await page.Locator("#bookImageFile").SetInputFilesAsync(emptyFile);

        await page.Locator("#add-book-button").ClickAsync();
    }
}