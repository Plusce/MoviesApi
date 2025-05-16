using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OutputCaching;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.Api.Endpoints.Movies;

public static class CreateMovieEndpoint
{
    public const string Name = "CreateMovie";

    public static IEndpointRouteBuilder MapCreateMovie(this IEndpointRouteBuilder app)
    {
        app.MapPost(ApiEndpoints.Movies.Create,
                // [Authorize(AuthConstants.TrustedMemberPolicyName)] we can do this, but this is not preferred way to handle authorization in minimal api 
                async (CreateMovieRequest request, IMovieService movieService, IOutputCacheStore outputCacheStore,
                    CancellationToken token) =>
                {
                    var movie = request.MapToMovie();
                    await movieService.CreateAsync(movie, token);

                    await outputCacheStore.EvictByTagAsync("movies", token);

                    var movieResponse = movie.MapToResponse();

                    return TypedResults.CreatedAtRoute(movieResponse, GetMovieEndpoint.Name,
                        new { idOrSlug = movieResponse.Id });
                })
            .WithName(Name)
            .Produces<MovieResponse>(StatusCodes.Status201Created)
            .Produces<ValidationFailureResponse>(StatusCodes.Status400BadRequest)
            .RequireAuthorization(AuthConstants.TrustedMemberPolicyName);

        return app;
    }
}