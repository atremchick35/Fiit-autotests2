using FluentAssertions;
using Moq;
using NUnit.Framework;
using UserCreatorTask;
using UserCreatorTask.UserValidators;

namespace UserManagerTests;

[TestFixture]
public class UserManagerTests
{
    private Mock<IUsersRepository> userRepositoryMock = null!;
    private Mock<IEmailService> emailServiceMock = null!;
    private Mock<IUserValidator> userValidatorMock = null!;
    private UserManager userManager = null!;

    [SetUp]
    public void SetUp()
    {
        userRepositoryMock = new Mock<IUsersRepository>();
        emailServiceMock = new Mock<IEmailService>();
        userValidatorMock = new Mock<IUserValidator>();

        userManager = new UserManager(
            userRepositoryMock.Object,
            emailServiceMock.Object,
            userValidatorMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        userRepositoryMock.VerifyAll();
        emailServiceMock.VerifyAll();
        userValidatorMock.VerifyAll();
    }
    
    [Test]
    public void CreateNewUser_ShouldOk()
    {
        var actualCallOrder = new List<string>();
        var expectedOrder = new List<string> { "FirstMethod", "SecondMethod", "ThirdMethod", "FourthMethod" };
        var user = new User("Jonn", "passwd", "email", 21);
        userValidatorMock
            .Setup(x => x.Validate(It.IsAny<User>()))
            .Callback(() => actualCallOrder.Add("FirstMethod"))
            .Returns((true, "ok"));
        userRepositoryMock
            .Setup(x => x.GetUser(It.IsAny<string>()))
            .Callback(() => actualCallOrder.Add("SecondMethod"))
            .Returns((User)null!);
        userRepositoryMock
            .Setup(x => x.SaveUser(It.IsAny<User>()))
            .Callback(() => actualCallOrder.Add("ThirdMethod"));
        emailServiceMock
            .Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => actualCallOrder.Add("FourthMethod"));

        userManager.CreateNewUser(user);

        CollectionAssert.AreEqual(expectedOrder, actualCallOrder);
        userValidatorMock
            .Verify(x => x.Validate(user), Times.Once);
        userRepositoryMock
            .Verify(x => x.GetUser(user.Email), Times.Once);
        userRepositoryMock
            .Verify(x => x.SaveUser(user), Times.Once);
        emailServiceMock
            .Verify(x => x.SendEmail(user.Name, "Welcome", "Thank you for registering!"), Times.Once);
        userRepositoryMock
            .Verify(x => x.DeleteUser(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public void CreateNewUser_WhenUserInvalid_ShouldFail()
    {
        var user = new User("Jonn", "passwd", "email", 21);
        userValidatorMock
            .Setup(x => x.Validate(It.IsAny<User>()))
            .Returns((false, "Validation failed - age or email invalid"));

        var exception = Assert.Throws<InvalidUserException>(() => 
            userManager.CreateNewUser(user));
        Assert.AreEqual("Validation failed - age or email invalid", exception!.Message);
        userValidatorMock
            .Verify(x => x.Validate(user), Times.Once);
        userRepositoryMock
            .Verify(x => x.GetUser(It.IsAny<string>()), Times.Never);
        userRepositoryMock
            .Verify(x => x.SaveUser(It.IsAny<User>()), Times.Never);
        emailServiceMock
            .Verify(x => x.SendEmail(
                    It.IsAny<string>(), 
                    It.IsAny<string>(), 
                    It.IsAny<string>()), 
                Times.Never);
        userRepositoryMock
            .Verify(x => x.DeleteUser(It.IsAny<string>()), Times.Never);
    }
    [Test]
    public void CreateNewUser_WhenUserAlreadyExists_ShouldFail()
    {
        var user = new User("Jonn", "passwd", "email", 21);
        userValidatorMock
            .Setup(x => x.Validate(It.IsAny<User>()))
            .Returns((true, "ok"));
        userRepositoryMock
            .Setup(x => x.GetUser(It.IsAny<string>()))
            .Returns(user);

        var exception = Assert.Throws<InvalidUserException>(() => userManager.CreateNewUser(user));
        userValidatorMock
            .Verify(x => x.Validate(user), Times.Once());
        userRepositoryMock
            .Verify(x => x.GetUser(user.Email), Times.Once);
        userRepositoryMock
            .Verify(x => x.DeleteUser(It.IsAny<string>()), Times.Never);
        userRepositoryMock
            .Verify(x => x.SaveUser(It.IsAny<User>()), Times.Never);

        Assert.AreEqual("UserAlreadyExists", exception!.Message);
    }

    [Test]
    public void DeleteUser_ShouldOk()
    {
        var actualCallOrder = new List<string>();
        var expectedOrder = new List<string> { "FirstMethod", "SecondMethod", "ThirdMethod" };
        var user = new User("Jonn", "passwd", "email", 21);
        userRepositoryMock
            .Setup(x => x.GetUser(It.IsAny<string>()))
            .Callback(() => actualCallOrder.Add("FirstMethod"))
            .Returns(user);
        userRepositoryMock
            .Setup(x => x.DeleteUser(It.IsAny<string>()))
            .Callback(() => actualCallOrder.Add("SecondMethod"));
        emailServiceMock
            .Setup(x => x.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback(() => actualCallOrder.Add("ThirdMethod"));

        userManager.DeleteUser(user.Email);
        
        CollectionAssert.AreEqual(expectedOrder, actualCallOrder);
        userRepositoryMock
            .Verify(x => x.GetUser(user.Email), Times.Once);
        userRepositoryMock
            .Verify(x => x.DeleteUser(user.Email), Times.Once);
        emailServiceMock
            .Verify(x => x.SendEmail(user.Name, "Goodbye", "Your account has been deleted."), Times.Once);
        userRepositoryMock
            .Verify(x => x.SaveUser(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public void DeleteUser_WhenUserDoesNotExist_ShouldFail()
    {
        var user = new User("Jonn", "passwd", "email", 21);
        userRepositoryMock.Setup(x => x.GetUser(It.IsAny<string>()));

        var exception = Assert.Throws<InvalidOperationException>(() => userManager.DeleteUser(user.Email));
        Assert.AreEqual("User not found", exception!.Message);
    }

    [Test]
    public void GetAdultUsers_ShouldOk()
    {
        var youngUser = new User("Jack", "passwd", "mail", 17);
        var adultUser = new User("Jonn", "passwd", "email", 21);
        userRepositoryMock
            .Setup(x => x.GetAllUsers())
            .Returns([youngUser, adultUser]);

        var actualUsers = userManager.GetAdultUsers();

        actualUsers.Should().NotBeNull();
        actualUsers.Count.Should().Be(1);
        actualUsers[0].Should().Be(adultUser);
        userRepositoryMock
            .Verify(x => x.GetAllUsers(), Times.Once);
    }
}