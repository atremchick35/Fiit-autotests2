using System;
using Kontur.BigLibrary.Service.Contracts;

namespace Kontur.BigLibrary.Tests.Core.Helpers;

public class ReaderBuilder
{
    private int? id;
    private int bookId;
    private string userName = $"reader_{IntGenerator.Get()}";
    private DateTime startDate = DateTime.UtcNow;

    public ReaderBuilder WithId(int id)
    {
        this.id = id;
        return this;
    }

    public ReaderBuilder WithBookId(int bookId)
    {
        this.bookId = bookId;
        return this;
    }

    public ReaderBuilder WithUserName(string userName)
    {
        this.userName = userName;
        return this;
    }

    public ReaderBuilder WithStartDate(DateTime startDate)
    {
        this.startDate = startDate;
        return this;
    }

    public Reader Build() => new()
    {
        Id = id ?? IntGenerator.Get(),
        BookId = bookId,
        UserName = userName,
        StartDate = startDate
    };
}