using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Movies.Api.Auth;
using Movies.Api.Mapping;
using Movies.Application.Services;
using Movies.Contracts.Requests;
using Movies.Contracts.Responses;

namespace Movies.Api.Controllers;

[ApiController] // this adds functionality to the controller
[ApiVersion(1.0)]
// [ApiVersion(2.0)]
public class MoviesController : ControllerBase
{
    private readonly IMovieService _movieService;
    private readonly IOutputCacheStore _outputCacheStore;

    public MoviesController(IMovieService movieService, IOutputCacheStore outputCacheStore)
    {
        _movieService = movieService;
        _outputCacheStore = outputCacheStore;
    }

    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.Movies.Create + "/batch")]
    public async Task<IActionResult> Create([FromBody] IEnumerable<CreateMovieRequest> movies, CancellationToken token)
    {
        foreach (var request in movies)
        {
            var movie = request.MapToMovie();
            await _movieService.CreateAsync(movie, token);
        }

        return NoContent();
    }

    // [ServiceFilter(typeof(ApiKeyAuthFilter))]
    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [HttpPost(ApiEndpoints.Movies.Create)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateMovieRequest request, CancellationToken token)
    // result that represents any form of action in our controller
    {
        var movie = request.MapToMovie();
        await _movieService.CreateAsync(movie, token);

        await _outputCacheStore.EvictByTagAsync("movies", token);

        var movieResponse = movie.MapToResponse();
        return CreatedAtAction(nameof(GetV1), new { idOrSlug = movieResponse.Id }, movieResponse);
    }

    // [MapToApiVersion(1.0)]
    [HttpGet(ApiEndpoints.Movies.Get)]
    [OutputCache(PolicyName = "MovieCache")]
    // [ResponseCache(Duration = 30, VaryByHeader = "Accept, Accept-Encoding", Location = ResponseCacheLocation.Any)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetV1([FromRoute] string idOrSlug, [FromServices] LinkGenerator linkGenerator,
        CancellationToken token)
    {
        var userId = HttpContext.GetUserId();

        var movie = Guid.TryParse(idOrSlug, out var id)
            ? await _movieService.GetByIdAsync(id, userId, token)
            : await _movieService.GetBySlugAsync(idOrSlug, userId, token);

        if (movie == null)
        {
            return NotFound();
        }

        var movieResponse = movie.MapToResponse();

        var movieObj = new { id = movie.Id };

        movieResponse.Links.Add(new Link
        {
            Href = linkGenerator.GetPathByAction(HttpContext, nameof(GetV1), values: new { idOrSlug = movie.Id })!,
            Rel = "self",
            Type = "GET"
        });

        movieResponse.Links.Add(new Link
        {
            Href = linkGenerator.GetPathByAction(HttpContext, nameof(Update), values: movieObj)!,
            Rel = "self",
            Type = "PUT"
        });

        movieResponse.Links.Add(new Link
        {
            Href = linkGenerator.GetPathByAction(HttpContext, nameof(Delete), values: movieObj)!,
            Rel = "self",
            Type = "DELETE"
        });

        return Ok(movieResponse);
    }

    // [ApiVersion(2.0, Deprecated = true)]
    // [MapToApiVersion(2.0)]
    // [HttpGet(ApiEndpoints.Movies.Get)]
    // public async Task<IActionResult> GetV2([FromRoute] string idOrSlug, CancellationToken token)
    // {
    //     var userId = HttpContext.GetUserId();
    //     
    //     var movie = Guid.TryParse(idOrSlug, out var id)
    //         ? await _movieService.GetByIdAsync(id, userId, token)
    //         : await _movieService.GetBySlugAsync(idOrSlug, userId, token);
    //
    //     if (movie == null)
    //     {
    //         return NotFound();
    //     }
    //
    //     var movieResponse = movie.MapToResponse();
    //     return Ok(movieResponse);
    // }

    [HttpGet(ApiEndpoints.Movies.GetAll)]
    // we're verifying the caching based on the parameters passed down
    // [ResponseCache(Duration = 30, VaryByQueryKeys = new[] { "title", "year", "sortBy", "page", "pageSize" },
    //     Location = ResponseCacheLocation.Any)]
    [OutputCache(PolicyName = "MovieCache")]
    [ProducesResponseType(typeof(MoviesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] GetAllMoviesRequest request, CancellationToken token)
    {
        var userId = HttpContext.GetUserId();
        var options = request.MapToOptions().WithUser(userId);
        var movies = await _movieService.GetAllAsync(options, token);
        var movieCount = await _movieService.GetCountAsync(options.Title, options.YearOfRelease, token);
        var moviesResponse = movies.MapToResponse(request.Page.GetValueOrDefault(PagedRequest.DefaultPage),
            request.PageSize.GetValueOrDefault(PagedRequest.DefaultPageSize), movieCount);
        return Ok(moviesResponse);
    }

    [Authorize(AuthConstants.TrustedMemberPolicyName)]
    [ProducesResponseType(typeof(MovieResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationFailureResponse), StatusCodes.Status400BadRequest)]
    [HttpPut(ApiEndpoints.Movies.Update)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateMovieRequest request,
        CancellationToken token)
    {
        var userId = HttpContext.GetUserId();

        var movie = request.MapToMovie(id);
        var updatedMovie = await _movieService.UpdateAsync(movie, userId, token);

        if (updatedMovie is null)
        {
            return NotFound();
        }

        await _outputCacheStore.EvictByTagAsync("movies", token);

        var movieResponse = updatedMovie.MapToResponse();
        return Ok(movieResponse);
    }

    [Authorize(AuthConstants.AdminUserPolicyName)]
    [HttpDelete(ApiEndpoints.Movies.Delete)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken token)
    {
        var deleted = await _movieService.DeleteByIdAsync(id, token);

        if (!deleted)
        {
            return NotFound();
        }

        await _outputCacheStore.EvictByTagAsync("movies", token);

        return Ok(deleted);
    }
}