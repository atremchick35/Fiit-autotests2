using System;
using Kontur.BigLibrary.DataAccess;
using Kontur.BigLibrary.Service.Services.BookService.Repository;
using Kontur.BigLibrary.Tests.Core.Helpers;
using Microsoft.Extensions.DependencyInjection;

namespace Kontur.BigLibrary.Tests.Integration.BdTests.BookRepositoryTests;

public class Container
{
    private IServiceCollection _collection;

    public Container()
    {
        _collection = new ServiceCollection();
        _collection.AddSingleton<IDbConnectionFactory>(x => new DbConnectionFactory(DbHelper.ConnectionString));
        _collection.AddSingleton<IBookRepository, BookRepository>();
        _collection.AddTransient<BookBuilder>();
        _collection.AddTransient<ReaderBuilder>();
    }

    public IServiceProvider Build()
    {
        return _collection.BuildServiceProvider();
    }
}