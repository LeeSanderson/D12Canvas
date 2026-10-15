using D12Canvas.App.Api.Backlog;
using D12Canvas.App.Shared.Suggestions;

const string AppCorsPolicy = "app";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(cors =>
    cors.AddPolicy(
        AppCorsPolicy,
        policy =>
            policy
                .WithOrigins(builder.Configuration.GetSection("AppOrigins").Get<string[]>() ?? [])
                .WithMethods("POST")
                .WithHeaders("Content-Type")
    )
);
builder.Services.AddSingleton(
    new BacklogFolder(
        builder.Configuration["Backlog:Directory"]
            ?? Path.Combine(
                RepositoryRoot(builder.Environment.ContentRootPath),
                ".scratch",
                "backlog"
            )
    )
);
builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.UseCors(AppCorsPolicy);

app.MapPost(
    "/api/suggestions",
    (SuggestionSubmission submission, BacklogFolder backlog, TimeProvider clock) =>
    {
        try
        {
            return Results.Ok(backlog.Save(submission, clock.GetLocalNow()));
        }
        catch (InvalidSuggestionException invalid)
        {
            return Results.BadRequest(invalid.Message);
        }
    }
);

app.Run();

// The backlog belongs to the repository the server was started from, found by its solution file,
// so it lands in the same folder however the server is launched.
static string RepositoryRoot(string start)
{
    for (
        var directory = new DirectoryInfo(start);
        directory is not null;
        directory = directory.Parent
    )
    {
        if (File.Exists(Path.Combine(directory.FullName, "D12Canvas.slnx")))
        {
            return directory.FullName;
        }
    }

    return start;
}
