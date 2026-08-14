using System;
using System.Collections.Generic;

namespace GabLuchi.Services;

public record AppFilterData(string? Type, IReadOnlyList<string> Genres, bool Windows, bool Mac, bool Linux, int? ReleaseYear, DateTime? ReleaseDate, string? ReleaseDateText, bool IsFree, int? Metacritic, long? Reviews, bool IsAdult);
