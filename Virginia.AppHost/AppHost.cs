var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.Virginia>("virginia");

builder.Build().Run();
