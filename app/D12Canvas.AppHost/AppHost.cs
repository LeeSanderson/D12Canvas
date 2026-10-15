var builder = DistributedApplication.CreateBuilder(args);

var api = builder.AddProject<Projects.D12Canvas_App_Api>("api");
var app = builder.AddProject<Projects.D12Canvas_App>("app").WaitFor(api);

api.WithEnvironment("AppOrigins__0", app.GetEndpoint("http"));

builder.Build().Run();
