using Bookmarker.Data;
using FuzzySharp;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Autocomplete;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;

namespace Bookmarker.Services;

public class AutoCompleteTagListProvider : IAutocompleteProvider
{
    private readonly IInteractionContext _context;
    private readonly IDbContextFactory<BookmarkContext> _contextFactory;

    public AutoCompleteTagListProvider(IInteractionContext context, IDbContextFactory<BookmarkContext> contextFactory)
    {
        _context = context;
        _contextFactory = contextFactory;
    }
    
    public string Identity => "list_autocomplete_tags";

    public async ValueTask<IReadOnlyList<IApplicationCommandOptionChoice>> GetSuggestionsAsync
    (
        IReadOnlyList<IApplicationCommandInteractionDataOption> options,
        string userInput, 
        CancellationToken ct = default
    )
    {
        if (!_context.TryGetUserID(out var uid))
        {
            return [];
        }
        
        await using var db =  await _contextFactory.CreateDbContextAsync(ct);
        var userTags = db.Bookmarks.Where(u => u.UserID == uid).Select(b => b.Tags).ToArray().SelectMany(u => u).Distinct().ToArray();

        if (string.IsNullOrEmpty(userInput))
        {
            return userTags.Take(25).Select(t => new ApplicationCommandOptionChoice(t, t)).ToArray();
        }
        
        
        var applicableTags = userTags.Select(t => (t, score: Fuzz.PartialRatio(userInput, t))).Where(t => t.score > 80);
        
        return applicableTags.OrderByDescending(d => d.score).Take(25).Select(t => new ApplicationCommandOptionChoice(t.t, t.t)).ToArray();

    }
}

