using Movies.Application.Models;

namespace Movies.Application.Services;

public interface IRatingService
{
    Task<bool> RateMovieAsync(Guid movieId, int rating, Guid userId, CancellationToken token);
    Task<bool> DeleteRatingAsync(Guid movieId, Guid userId, CancellationToken token);
    Task<IEnumerable<MovieRating>> GetRatingsForUserAsync(Guid userId, CancellationToken token);
}