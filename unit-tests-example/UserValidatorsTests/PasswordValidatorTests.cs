using NUnit.Framework;
using UserCreatorTask.UserValidators;

namespace UserValidatorTests;

[TestFixture]
public class PasswordValidatorTests
{
    [Test]
    [Parallelizable]
    public void ValidatePassword_WhenPasswordIsNull_ShouldFail()
    {
        var passwordValidator = new PasswordValidator();
        
        Assert.That(() => passwordValidator.IsValid(null), Throws.ArgumentNullException, "email value can't be null");
    }

    [TestCaseSource(nameof(ValidPasswordClass))]
    [NonParallelizable]
    public void ValidatePassword_ShouldOk(string validPassword, string cause)
    {
        var passwordValidator = new PasswordValidator();

        var actual = passwordValidator.IsValid(validPassword);
        
        Assert.That(actual, Is.True, cause);
    }

    [TestCaseSource(nameof(InvalidPasswordClass))]
    [Parallelizable]
    public void ValidatePassword_WhenWrongPassword_ShouldFail(string invalidPassword, string cause)
    {
        var passwordValidator = new PasswordValidator();

        var actualIsValid = passwordValidator.IsValid(invalidPassword);
        
        Assert.That(actualIsValid, Is.False, cause);
    }

    private static object[] ValidPasswordClass =
    [
        new object[]
        {
            "Ab0!ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            "Validator should check:\n\n" +
            "At least 100 characters long.\n\n" +
            "Contain at least one uppercase Latin letter (A-Z).\n\n" +
            "Contain at least one lowercase Latin letter (a-z).\n\n" +
            "Contain at least one digit (0-9).\n\n" +
            "Contain at least one special character from the list: #, ?, !, @, $, %, ^, &, *, -."
        }
    ];

    private static object[] InvalidPasswordClass =
    [
        new object[] { "пароль", "Password should have only Latin letters"},
        new object[] { "qwerty", "Password should have at least 100 symbols" },
        new object[] { "ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            "Password should have at least one uppercase Latin letter" },
        new object[] { "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
            "Password should have at least one lowercase Latin letter" },
        new object[] { "Afffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            "Password should have at least one digit" },
        new object[] { "A1ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff",
            "Password should have at lest one special character: #, ?, !, @, $, %, ^, &, *, -" }
    ];
}