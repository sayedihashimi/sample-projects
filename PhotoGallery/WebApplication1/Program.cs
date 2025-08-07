using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureBlobContainerClient("photos");
builder.Services.AddRazorComponents();

builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseAntiforgery();

app.MapGet("/", async (BlobContainerClient client) =>
{
    var blobs = client.GetBlobsAsync();
    var photos = new List<string>();
    await foreach (var photo in blobs)
    {
        photos.Add(photo.Name);
    }
    return new RazorComponentResult<PhotoList>(new { Photos = photos });
});

app.MapPost("/upload", async (IFormFile photo, BlobContainerClient client) =>
{
    if (photo.Length > 0)
    {
        var blobClient = client.GetBlobClient(photo.FileName);
        await blobClient.UploadAsync(photo.OpenReadStream(), true);
    }

    return Results.Redirect("/");
});

app.Run();
 