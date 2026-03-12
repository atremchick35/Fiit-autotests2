using NUnit.Framework;
using UserCreatorTask;
using UserCreatorTask.UserValidators;

namespace UserValidatorTests;

[TestFixture]
public class NameValidatorTests
{
    [Test]
    [Parallelizable]
    public void ValidateName_WhenNameIsNull_ShouldFail()
    {
        var nameValidator = new NameValidator();
        
        Assert.That(() => nameValidator.IsValid(null), Throws.ArgumentNullException, "email value can't be null");
    }

    [TestCaseSource(nameof(ValidNameClass))]
    [Parallelizable]
    public void ValidateName_ShouldOk(string validName, string cause)
    {
        var nameValidator = new NameValidator();

        var actual = nameValidator.IsValid(validName);
        
        Assert.That(actual, Is.True, cause);
    }

    [TestCaseSource(nameof(InvalidNameClass))]
    [Parallelizable]
    public void ValidateName_WhenWrongName_ShouldFail(string invalidName, string cause)
    {
        var nameValidator = new NameValidator();
        
        var actualIsValid = nameValidator.IsValid(invalidName);
        
        Assert.That(actualIsValid, Is.False, cause);
    }

    private static object[] ValidNameClass =
    [
        new object[]
        {
            "Ivan Ivanov",
            "Consists of exactly two parts (words).\n\n" +
            "The parts are separated by exactly one space.\n\n" +
            "Each part contains only Latin letters (A-Z, a-z).\n\n" +
            "Each part contains at least one letter.\n\n" +
            "No other characters (numbers, hyphens, periods, apostrophes, etc.) are allowed."
        }
    ];

    private static object[] InvalidNameClass =
    [
        new object[] { "Иван", "Name should have only Latin letters" },
        new object[] { "42", "Name should have only Latin letters" },
        new object[] { "Ivan", "Name should consists of exactly two parts (words)" },
        new object[] { "Ivan  Ivanov", "The parts should separated by exactly one space" },
        new object[] { "Ivan ", "Name should have not empty second part" },
        new object[] { "user@domain@com", "Name should have at least one \"@\" symbol" },
        new object[] { "user name@domain.com", "Name should not have spaces" },
        new object[] { "user@domain.", "Name should have not empty zone name" },
        new object[] { "user@domain.c", "Name should have zone name length between 2 and 4" },
        new object[] { "user@domain.community", "Name should have zone name length between 2 and 4" }
    ];
}