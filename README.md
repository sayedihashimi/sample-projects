# Prototype compound template

This demonstrates how you can create a compound template which contains two parts.

1. Front-end created by a cli tool
1. Backend created by the aspnet core web api template

## Explanation of the files in this repo 

`template.json` - this is a file that we will need to create and fill in all the metadata with
all the correct content.

`template.complete.json` - this is a file that contains the content from `template.json` and the
`template.json` in the web api template. I didn't copy all the content from the web api template,
just the relevant parts. It's clear in the `template.complete.json` what content was added.
In the real implemenation this file wouldn't exist. You would take the `template.json` and then
merge the proper content from the web api template.

`templatepack.csproj` - this is the file that we will need to create to pack the template
into a nuget package. I haven't finished the NuGet package creation part yet, so this file
is not currently being used.

`company.webapplication1.client.esproj` - this is an esproj that I copied from an existing project.
This file looks pretty static so you could probably just add this file to the real implemantation,
but maybe there is a way to generate that.

`build-template.ps1` - this is a script that can be used to build the template. It will perform the following steps.

1. Clean out folders from previous run
1. Create an output folder `bin` where we will build the template
1. Create a temp folder `obj` where we will extract the web api template and modify it
1. Call `npm.cmd` to create the front-end project in `bin`
1. Add the `.esproj` file to the front-end folder in `bin`
1. Find aspnet core template nupkg (hard coded file in this prototype)
1. Extract template nupkg to `obj` folder
1. Copy `template.config` from web api template to `obj/api/webapi-template.json`. 
   Note: this file isn't used in this prototype, but in the real implemenation you'll need to keep this file
   so that you can merge it with our `template.json` file
1. Delete `.template.config` folder from web api template in `obj` folder
1. Copy web api template content folder to its folder in `bin`
1. Create `template.config` in `bin`. In this prototype I'm just copying a file that I manually created.
1. Profit

## Work remaining

1. Update strings in `template.json`
2. Discover path to the aspnet templates nupkg (or extracted folder if it exists)
3. Create a `template.json` file that merges the `template.json` here and the `template.json` from the web api project template.
4. Add a parameter so the user can pick JavaScript/TypeScript. This will be a pretty big update, 
   so we can figure out how to do it together later. Initial thoughts are that we create two different templates and merge them
   together by using the same groupIdentity value.
5. When we create a SPA project in VS after the files are created, there are a few
modifications that are performed. For example a Project Reference is added in
the server project to the client project. There are some other modifications
that need to take place besides that.
