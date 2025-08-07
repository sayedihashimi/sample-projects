var builder = DistributedApplication.CreateBuilder(args);

var photos = builder.AddAzureStorage("storage")
                .RunAsEmulator()
                .AddBlobs("blobs")
                .AddBlobContainer("photos");

builder.AddProject<Projects.WebApplication1>("webapp")
        .WithReference(photos)
        .WaitFor(photos);

builder.Build().Run();
