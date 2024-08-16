using System.Runtime.Serialization;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Movie.Api.Sdk;
using Movies.Api.Sdk.Consumer;
using Movies.Contracts.Requests;
using Refit;

// var moviesApi = RestService.For<IMoviesApi>("http://localhost:5000");

var services = new ServiceCollection();

// we can use HttpClientFactory instead of simple RestService
// good to publish extension method with documentation describing how to use the service
services
    .AddHttpClient()
    .AddSingleton<AuthTokenProvider>()
    .AddRefitClient<IMoviesApi>(x => new RefitSettings
    {
        // so, we generate that on the flight, calling the identity server using that AuthTokenProvider
        AuthorizationHeaderValueGetter =
            async (req, token) => await x.GetRequiredService<AuthTokenProvider>().GetTokenAsync()
        //Task.FromResult("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJqdGkiOiIxYWJjNTRjZi02M2EzLTQwMWUtYjcwNS01ODhmYjkwZmNiYWYiLCJzdWIiOiJuaWNrQG5pY2tjaGFwc2FzLmNvbSIsImVtYWlsIjoibmlja0BuaWNrY2hhcHNhcy5jb20iLCJ1c2VyaWQiOiJkODU2NmRlMy1iMWE2LTRhOWItYjg0Mi04ZTM4ODdhODJlNDIiLCJhZG1pbiI6dHJ1ZSwidHJ1c3RlZF9tZW1iZXIiOnRydWUsIm5iZiI6MTcyMzgwNTMwMywiZXhwIjoxNzIzODM0MTAzLCJpYXQiOjE3MjM4MDUzMDMsImlzcyI6Imh0dHBzOi8vaWQubmlja2NoYXBzYXMuY29tIiwiYXVkIjoiaHR0cHM6Ly9tb3ZpZXMubmlja2NoYXBzYXMuY29tIn0.oondSK5aomkDBd3WD8uDbXUx-g1JQoFomh_LlUAVA-k")
    })
    .ConfigureHttpClient(x => x.BaseAddress = new Uri("http://localhost:5000"));

var provider = services.BuildServiceProvider();

var moviesApi = provider.GetRequiredService<IMoviesApi>();

var movie = await moviesApi.GetMovieAsync("thunderball-1965");

var newMovie = await moviesApi.CreateMovieAsync(new CreateMovieRequest
{
    Title = "Spiderman 2",
    YearOfRelease = 2002,
    Genres = new[] { "Action" }
});

await moviesApi.UpdateMovieAsync(newMovie.Id, new UpdateMovieRequest
{
    Title = "Spiderman 2",
    YearOfRelease = 2002,
    Genres = new[] { "Action", "Adventure" }
});

await moviesApi.DeleteMovieAsync(newMovie.Id);

var request = new GetAllMoviesRequest
{
    Page = 1,
    PageSize = 10,
    Title = null,
    Year = null,
    SortBy = null
};

var movies = await moviesApi.GetMoviesAsync(request);

Console.WriteLine(JsonSerializer.Serialize(movies));