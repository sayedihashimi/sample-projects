using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http.HttpResults;
using WebApplication1.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddAzureBlobContainerClient("photos");
builder.Services.AddRazorComponents();

var app = builder.Build();

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

app.Run();
 