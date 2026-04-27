using Kontur.BigLibrary.Service.Contracts;

namespace Kontur.BigLibrary.Tests.Core.Helpers;

public class BookBuilder
{
    private string? name;
    private string? author;
    private string? description;
    private int? id;
    private bool isDeleted;
    private int rubricId = 1;
    private int imageId = 1;
    private string? price;

    public BookBuilder WithName(string? name)
    {
        this.name = name;
        return this;
    }

    public BookBuilder WithAuthor(string author)
    {
        this.author = author;
        return this;
    }

    public BookBuilder WithDescription(string description)
    {
        this.description = description;
        return this;
    }

    public BookBuilder WithId(int id)
    {
        this.id = id;
        return this;
    }

    public BookBuilder WithRubricId(int rubricId)
    {
        this.rubricId = rubricId;
        return this;
    }

    public BookBuilder Delete()
    {
        isDeleted = true;
        return this;
    }

    public BookBuilder WithImage(int imageId)
    {
        this.imageId = imageId;
        return this;
    }

    public BookBuilder WithPrice(string price)
    {
        this.price = price;
        return this;
    }

    public Book Build() => new()
    {
        Name = name ?? $"Default_name{IntGenerator.Get()}",
        Author = author ?? $"Default_author{IntGenerator.Get()}",
        Id = id ?? IntGenerator.Get(),
        Description = description ?? "",
        IsDeleted = isDeleted,
        RubricId = rubricId,
        ImageId = imageId,
        Count = 1,
        Price = price
    };
}