using FluentValidation;
using Movies.Application.Database;
using Movies.Application.Repositories;
using Movies.Application.Services;

namespace Movies.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    { 
        // just the interface. we don't bring the dependency injection in the application. we're just bringing the abstractions
        services.AddSingleton<IRatingRepository, RatingRepository>();
        services.AddSingleton<IRatingService, RatingService>();
        services.AddSingleton<IMovieService, MovieService>();
        services.AddSingleton<IMovieRepository, MovieRepository>();
        services.AddValidatorsFromAssemblyContaining<IApplicationMarker>(ServiceLifetime.Singleton); // because we're injecting it into the singleton, we're going to change the lifecycle to the singleton as well
        return services;
    }

    public static IServiceCollection AddDatabase(this IServiceCollection services, string connectionString)
    {
        // it's singleton, because it doesn't have to be anything else.
        // the factory will return a new connection everytime
        // so this is effectively singleton masking a transient
        services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
        services.AddSingleton<DbInitializer>(); // Singleton because it will be only used once
        
        return services;
    }
}