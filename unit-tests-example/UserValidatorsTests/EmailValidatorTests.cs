using NUnit.Framework;
using UserCreatorTask.UserValidators;

namespace UserValidatorTests;

[TestFixture]
public class EmailValidatorTests
{
    [Test]
    [Parallelizable]
    public void ValidateEmail_WhenEmailIsNull_ShouldFail()
    {
        var emailValidator = new EmailValidator();
        
        Assert.That(() => emailValidator.IsValid(null), Throws.ArgumentNullException);
    }

    [TestCaseSource(nameof(ValidEmailClass))]
    [Parallelizable]
    public void ValidateEmail_ShouldOk(string validEmail, string cause)
    {
        var emailValidator = new EmailValidator();

        var actual = emailValidator.IsValid(validEmail);
        
        Assert.That(actual, Is.True, cause);
    }

    [TestCaseSource(nameof(InvalidEmailClass))]
    [Parallelizable]
    public void ValidateEmail_WhenWrongEmail_ShouldFail(string invalidEmail, string cause)
    {
        var emailValidator = new EmailValidator();

        var actualIsValid = emailValidator.IsValid(invalidEmail);
        
        Assert.That(actualIsValid, Is.False, cause);
    }

    private static object[] ValidEmailClass =
    [
        new object[]
        {
            "first.last@sub.ru.rf",
            "Begin with a non-sequential local part.\n\n" +
            "Contain the @ symbol.\n\nA" +
            "fter @, follow a domain consisting of:\n\n" +
            "One or more levels of subdomains (each level ends with a period).\n\n" +
            "End with a domain extension (the last part after the period) of 2 to 4 characters.\n\n" +
            "Only Latin letters, numbers, and the following symbols are allowed in the entire address: _, -, ., @."
        }
    ];

    private static object[] InvalidEmailClass =
    [
        new object[] { "почта", "Email should have only Latin letters and special symbols: _, -, ., @" },
        new object[] { "qwerty", "Email should have \"@\" symbol" },
        new object[] { "qwerty@", "Email should have domain name after \"@\" separated by \".\"" },
        new object[] { "@domain.com", "Email should have not empty local part" },
        new object[] { "user@domain@com", "Email should have at least one \"@\" symbol" },
        new object[] { "user name@domain.com", "Email should not have spaces" },
        new object[] { "user@domain.", "Email should have not empty zone name" },
        new object[] { "user@domain.c", "Email should have zone name length between 2 and 4" },
        new object[] { "user@domain.community", "Email should have zone name length between 2 and 4" }
    ];
}