using System.Net;
using FluentAssertions;
using Kontur.BigLibrary.Tests.Core.ApiClients;
using NUnit.Framework;

namespace Tests.ApiSystem;

[TestFixture]
public class BooksApiTests : BooksApiTestBase
{
    [OneTimeSetUp]
    public void SetUpClients()
    {
        authApiClient = new AuthApiClient();
        booksApiClient = new BooksApiClient();
    }

    #region 1) Встать в очередь
    
    [Test]
    public void EnqueueBook_BookOccupiedByAnotherUser_Success()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var book = CreateBook(user1.token);
        
        booksApiClient.CheckoutBook(book.Id.ToString(), user1.email, user1.token);

        var response = booksApiClient.EnqueueBook(book.Id.ToString(), user2.email, user2.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var queue = GetReadersInQueue(book.Id.ToString(), user1.token);
        queue.Should().NotBeEmpty("Вы встали в очередь.");
    }

    [Test]
    public void EnqueueBook_UserAlreadyInQueue_ReturnsError()
    {
        var user = CreateUser();
        var book = CreateBook(user.token);
        
        booksApiClient.EnqueueBook(book.Id.ToString(), user.email, user.token);

        var response = booksApiClient.EnqueueBook(book.Id.ToString(), user.email, user.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Вы уже стоите в очереди.");
    }

    [Test]
    public void EnqueueBook_BookOccupiedByCurrentUser_ReturnsError()
    {
        var user = CreateUser();
        var book = CreateBook(user.token);
        
        booksApiClient.CheckoutBook(book.Id.ToString(), user.email, user.token);

        var response = booksApiClient.EnqueueBook(book.Id.ToString(), user.email, user.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Вы уже взяли эту книгу.");
    }

    #endregion

    #region 2) Взять книгу

    [Test]
    public void CheckoutBook_BookIsFreeAndNoQueue_Success()
    {
        var user = CreateUser();
        var book = CreateBook(user.token);

        var response = booksApiClient.CheckoutBook(book.Id.ToString(), user.email, user.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var readers = GetBookReaders(book.Id.ToString(), user.token);
        readers.Should().NotBeEmpty("Вы взяли книгу.");
    }

    [Test]
    public void CheckoutBook_BookOccupiedByAnotherUser_ReturnsError()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var book = CreateBook(user1.token);
        
        booksApiClient.CheckoutBook(book.Id.ToString(), user1.email, user1.token);

        var response = booksApiClient.CheckoutBook(book.Id.ToString(), user2.email, user2.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Книга занята.");
    }

    [Test]
    public void CheckoutBook_BookIsFreeButAnotherUserFirstInQueue_ReturnsError()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var user3 = CreateUser();
        var book = CreateBook(user1.token);

        booksApiClient.CheckoutBook(book.Id.ToString(), user1.email, user1.token);
        booksApiClient.EnqueueBook(book.Id.ToString(), user2.email, user2.token);
        booksApiClient.ReturnBook(book.Id.ToString(), user1.email, user1.token);

        var response = booksApiClient.CheckoutBook(book.Id.ToString(), user3.email, user3.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK, "Вы взяли книгу.");
    }

    [Test]
    public void CheckoutBook_BookIsFreeAndCurrentUserIsFirstInQueue_Success()
    {
        var user1 = CreateUser();
        var user2 = CreateUser();
        var book = CreateBook(user1.token);

        booksApiClient.CheckoutBook(book.Id.ToString(), user1.email, user1.token);
        booksApiClient.EnqueueBook(book.Id.ToString(), user2.email, user2.token);
        booksApiClient.ReturnBook(book.Id.ToString(), user1.email, user1.token);

        var response = booksApiClient.CheckoutBook(book.Id.ToString(), user2.email, user2.token);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var readers = GetBookReaders(book.Id.ToString(), user2.token);
        readers.Should().NotBeEmpty("Вы взяли книгу.");
    }

    #endregion
}