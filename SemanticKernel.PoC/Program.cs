#pragma warning disable SKEXP0060
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Handlebars;

using SemanticKernel.PoC.Plugins;

var builder = WebApplication.CreateBuilder(args);


const string chatDeploymentName = "gpt-4";
const string useModel = "gpt-4";
var pluginDirectory = Path.Combine(Directory.GetCurrentDirectory(), "plugins");

builder.Services.AddKernel()
    .Plugins
    .AddFromObject(new CompanySearchPlugin(), "CompanyPlugin")
    .AddFromPromptDirectory(Path.Combine(pluginDirectory, "WriterPlugin"))
    .AddFromPromptDirectory(Path.Combine(pluginDirectory, "EmailPlugin"))
    .AddFromPromptDirectory(Path.Combine(pluginDirectory, "TranslatePlugin"));

builder.Services.AddAzureOpenAIChatCompletion(useModel, "", "");

builder.Services.AddTransient(c =>
{
    var plannerOptions = new FunctionCallingStepwisePlannerOptions()
    {
        // When using OpenAI models, we recommend using low values for temperature and top_p to minimize planner hallucinations.
        ExecutionSettings = new ()
        {
            Temperature = 0.0,
            TopP = 0.0,
        },
        MaxIterations = 10
    };
    var planner = new FunctionCallingStepwisePlanner(plannerOptions);
    return planner;
});
builder.Services.AddTransient(c =>
{
    var plannerOptions = new HandlebarsPlannerOptions()
    {
        // When using OpenAI models, we recommend using low values for temperature and top_p to minimize planner hallucinations.
        ExecutionSettings = new OpenAIPromptExecutionSettings()
        {
            Temperature = 0.0,
            TopP = 0.0,
        },
        // Use gpt-4 or newer models if you want to test with loops.
        // Older models like gpt-35-turbo are less recommended. They do handle loops but are more prone to syntax errors.
        AllowLoops = chatDeploymentName.Contains("gpt-4", StringComparison.OrdinalIgnoreCase),
    };
    var planner = new HandlebarsPlanner(plannerOptions);
    return planner;
});


// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddLogging();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapPost("/ask", 
        async ([FromBody]Ask ask,  Kernel kernel, FunctionCallingStepwisePlanner planner) =>
        {
            var plan = await planner.ExecuteAsync(kernel, $"{ask?.Message}. Return value must be in UTF-8.");
            
            return Results.Text(plan.FinalAnswer, "text/plain", Encoding.UTF8);
        })
    .WithName("Ask")
    .WithOpenApi();

app.MapPost("/ask2", 
        async ([FromBody]Ask ask,  Kernel kernel, HandlebarsPlanner planner) =>
        {
            var plan = await planner.CreatePlanAsync(kernel, $"{ask?.Message}. Return value must be in UTF-8.");
            
            app.Logger.LogInformation(plan.ToString());
            var response = await plan.InvokeAsync(kernel, new KernelArguments());
            
            
            return Results.Text(response, "text/plain", Encoding.UTF8);
        })
    .WithName("Ask2")
    .WithOpenApi();

app.Run();

record Ask(string Message);
