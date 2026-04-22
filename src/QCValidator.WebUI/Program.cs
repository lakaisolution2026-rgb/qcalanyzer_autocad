using QCValidator.WebUI.Components;
using QCValidator.Application.Interfaces;
using QCValidator.Application.Services;
using QCValidator.Infrastructure.Providers;
using QCValidator.Infrastructure.Generators;
using QuestPDF.Infrastructure;

// QuestPDF community license (free for small businesses)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register the infrastructure providers used by the validation service
builder.Services.AddSingleton<ITemplateProvider, ExcelTemplateProvider>();
builder.Services.AddSingleton<IReportGenerator, JsonReportGenerator>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
