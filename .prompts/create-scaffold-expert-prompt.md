I'm working on an instructions file for Copilot that I need you to help me with. Previously I created #file:dotnet-aspnet-codegenerator-expert.md which is a copilot instructions file and tells Copilot how to call `dotnet new` and `dotnet aspnet-codegenerator`. The `dotnet aspnet-codegenerator` tool has been replaced with `dotnet scaffold`. I want to create an instructions file similar to #file:dotnet-aspnet-codegenerator-expert.md but replaces `dotnet aspnet-codegenerator` with `dotnet scaffold`. In the instructions file tell Copilot to refer to the file #file:dotnet-scaffold-help-generated.md to learn how to call `dotnet scaffold`. You don't need to add any examples of how to call `dotnet scaffold`, just have it refer to the file mentioned.

Some differences between `dotnet aspnet-codegenerator` and `dotnet scaffold`.
- You don't need to manually add the package reference for `Microsoft.VisualStudio.Web.CodeGeneration.Design`

I want you to add, or update, the file at `.prompts/dotnet-scaffold-expert-v2.md`.

Ask any clarifying questions.
