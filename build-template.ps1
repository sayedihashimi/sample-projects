$scriptDir = split-path -parent $MyInvocation.MyCommand.Definition
$srcDir = $scriptDir
$templateJsonFilePath = (join-path $srcDir template.json)
$outputDir = (Join-Path -path $scriptDir bin)
$objDir = (Join-Path -Path $scriptDir obj)
# This is the value on my machine, it may be different on yours
$pathToNpmCmd = "C:\Program Files\nodejs\npm.cmd"

function Reset-Templates(){
    dotnet new --uninstall $outputDir
    dotnet new --debug:rebuildcache
}

function Clean(){
    [cmdletbinding()]
    param(
        [string]$rootFolder = $scriptDir
    )
    process{
        'clean started, rootFolder "{0}"' -f $rootFolder | write-host
        # delete folders that should not be included in the nuget package
        Get-ChildItem -path $scriptDir -include bin,obj,nupkg,.vs -Recurse -Directory | Select-Object -ExpandProperty FullName | Remove-item -recurse
    }
}

###############################################
# Start script here
###############################################
#"scriptDir: {0},{1},{2},{3}" -f $scriptDir,$srcDir,$outputDir,$templateJsonFilePath | Write-Host
# Reset-Templates
Clean

###############################################
# 1: Prep the output folder
###############################################
# create the output folder
"`nCreating output folders" | Write-Output
New-Item -Path $outputDir -ItemType Directory
New-Item -Path $objDir -ItemType Directory

###############################################
# 2: Invoke npm.cmd to create the react project
###############################################
# TODO: later we need to pass TypeScript if the user has selected that option
Push-Location $outputDir
"Creating react app with npm.cmd command" | Write-Output
& $pathToNpmCmd init --yes vite@latest company.webapplication1.client -- --template=react
Pop-Location

# verify the output folder was created
$reactTemplateOutputDir = (Join-Path $outputDir 'company.webapplication1.client')
if (-not (Test-Path -Path $reactTemplateOutputDir)) {
    throw ('React template directory not found at "{0}" after generation' -f $reactTemplateOutputDir)
}
$esprojOutputPath = (Join-Path $reactTemplateOutputDir 'company.webapplication1.client.esproj')
'Creating the esproj file at "{0}"' | Write-Output
# In this prototype I'm copying a static file, but maybe there is a better way to create this besides what I'm doing here
Copy-Item (Join-Path $scriptDir 'company.webapplication1.client.esproj') -Destination $esprojOutputPath

###############################################
# 3: Find the webapi template location
###############################################

# TODO: Use dotnet cli to find root path and then find webapi template
$aspnetTemplatesNupkgPath = (Join-Path $scriptDir 'microsoft.dotnet.web.projecttemplates.8.0.8.0.8.nupkg')
if(-not (Test-Path $aspnetTemplatesNupkgPath)){
    throw ('aspnet templates nupkg not found at "{0}"' -f $aspnetTemplatesNupkgPath)
}
$nupkgFilename = [System.IO.Path]::GetFileNameWithoutExtension($aspnetTemplatesNupkgPath)
$apiObjDirPath = (Join-Path -Path $objDir 'api')
$aspnetTemplateExtractPath = (Join-Path $apiObjDirPath $nupkgFilename)
New-Item -Path $aspnetTemplateExtractPath -ItemType Directory

###############################################
# 4: Extract aspnet templates to a folder
###############################################
"`nExtracting aspnet templates to folder '{0}'" -f $aspnetTemplateExtractPath | Write-Output
Expand-Archive -Path $aspnetTemplatesNupkgPath -DestinationPath $aspnetTemplateExtractPath
# sleep for 2 seconds to give the os/pwsh a chance to see the extracted files
Start-Sleep -Seconds 2

$webapiTemplateObjPath = (Join-Path -Path $aspnetTemplateExtractPath 'content/WebApi-CSharp')
if(-not (Test-Path $webapiTemplateObjPath)){
    throw ('Extracted web api template not found at "{0}"' -f $webapiTemplateObjPath)
}
$webapiObjTemplateConfigFolder = (Join-Path $webapiTemplateObjPath .template.config)
$webapiObjTemplateJsonDestFile = (Join-Path $apiObjDirPath 'webapi-template.json')
# copy the template.json file before deleting the .template.config folder.
# we will need to use this template.json file to merge into our template json file
Copy-Item -Path (Join-Path $webapiObjTemplateConfigFolder 'template.json') -Destination $webapiObjTemplateJsonDestFile
'Deleting .template.config folder from web api template in obj folder "{0}"' -f $webapiObjTemplateConfigFolder | Write-Output
# delete .template.config folder in api template so that it doesn't get copied into our template
Remove-Item -Path $webapiObjTemplateConfigFolder -Recurse

# copy the template content to the bin folder
$webapiTemplateOutputDir = (Join-Path $outputDir 'Company.WebApplication1.Server')
Move-Item -Path $webapiTemplateObjPath -Destination $webapiTemplateOutputDir 

###############################################
# 5: Now make the bin folder into a template
###############################################
# create the .template.config folder
$templateConfigOutputPath = (Join-Path -Path $outputDir '.template.config')
New-Item -Path $templateConfigOutputPath -ItemType Directory
# copy the template.config file
# TODO: In this prototype I'm copying the file that I manually 
#       merged with the original template.json file and the web api template.json content.
#       In the real script, you would copy the template.json file and then merge the contents
#       that are needed from the web api template.json file.
Copy-Item -Path (Join-Path $scriptDir 'template.complete.json') -Destination (Join-Path -Path $templateConfigOutputPath 'template.json')

###############################################
# 6: Profit
###############################################
# now you should be able to do:
# > dotnet new install <path to $outputDir>
# > dotnet new reactwebapi -h
# > dotnet new reactwebapi -o MyReactWebApp