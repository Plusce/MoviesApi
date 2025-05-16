namespace Movies.Api.Endpoints.Movies;

public static class MovieEndpointExtensions
{
    public static IEndpointRouteBuilder MapMovieEndpoints(this IEndpointRouteBuilder app)
    {
        // we can provide such grouping
        // var group = app.MapGroup("movies");
        // group.MapGetMovie();
        
        app.MapGetMovie();
        app.MapCreateMovie();
        app.MapGetAllMovies();
        app.MapUpdateMovie();
        app.MapDeleteMovie();
        
        return app;
    }
}