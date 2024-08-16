using Dapper;

namespace Movies.Application.Database;

public class DbInitializer
{
    private readonly IDbConnectionFactory _dbConnectionFactory;

    public DbInitializer(IDbConnectionFactory dbConnectionFactory)
    {
        _dbConnectionFactory = dbConnectionFactory;
    }

    public async Task InitializeAsync()
    {
        using var connection = await _dbConnectionFactory.CreateConnectionAsync();

        await connection.ExecuteAsync("""
                                       create table if not exists movies (
                                           id UUID primary key,
                                           slug TEXT not null,
                                           title TEXT not null,
                                           yearofrelease integer not null
                                       );
                                       """);

        await connection.ExecuteAsync("""
                                      create unique index concurrently if not exists movies_slug_idx
                                      on movies
                                      using btree(slug)
                                      """); // index makes more efficient point reads using the slug

        await connection.ExecuteAsync("""
                                      create table if not exists genres (
                                          movieId UUID references movies (Id),
                                          name Text not null
                                      );
                                      """);
        
        await connection.ExecuteAsync("""
                                      create table if not exists ratings (
                                          userid UUID,
                                          movieId UUID references movies (Id),
                                          rating integer not null,
                                          primary key(userid, movieid)
                                      );
                                      """);
    }
}