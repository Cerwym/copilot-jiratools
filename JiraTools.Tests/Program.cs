using Microsoft.Testing.Platform.Builder;

var builder = await TestApplication.CreateBuilderAsync(args);
using var app = await builder.BuildAsync();
return await app.RunAsync();
