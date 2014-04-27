param($installPath, $toolsPath, $package, $project)

$targetsFileName = 'ServiceStack.Razor.BuildTask.targets';
$targetsPath = [System.IO.Path]::Combine($toolsPath, $targetsFileName)

# Need to load MSBuild assembly if it's not loaded yet.
Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

# Make the path to the targets file relative.
$projectUri = new-object Uri('file://' + $project.FullName)
$targetUri = new-object Uri('file://' + $targetsPath)
$relativePath = $projectUri.MakeRelativeUri($targetUri).ToString().Replace([System.IO.Path]::AltDirectorySeparatorChar, [System.IO.Path]::DirectorySeparatorChar)

# Remove previous imports to .targets
$msbuild.Xml.Imports | Where-Object {$_.Project.ToLowerInvariant().EndsWith($targetsFileName.ToLowerInvariant()) } | Foreach { 
	$_.Parent.RemoveChild( $_ ) 
	[string]::Format( "Removed import of '{0}'" , $_.Project )
}

# Add the import and save the project
$import = $msbuild.Xml.AddImport($relativePath)
$import.set_Condition( "Exists('$relativePath')" ) | Out-Null
[string]::Format("Added import of '{0}'.", $relativePath )

# Set BuildAction of Razor Views to Compile and Embed Content Files
function EmbedEachItem($projectItems) {
    $projectItems | %{      
        $x = $_.Name.ToLower()
        if ($x -like "*.cshtml") {
            $_.Properties.Item("BuildAction").Value = [int]2 # Compile Razor Views
        }                
        elseif ($x -like "*.md" -or $x -like "*.js" -or $x -like "*.css") {         
            $_.Properties.Item("BuildAction").Value = [int]3 # Embed Content Files
        }
        elseif ($x -like "*.png" -or $x -like "*.gif" -or $x -like "*.jpg" -or $x -like "*.jpeg") {
            $_.Properties.Item("BuildAction").Value = [int]3 # Embed Content Files
        }

        # Recursively
        if ($_.ProjectItems) {
            EmbedEachItem $_.ProjectItems
        }
    }
}

EmbedEachItem $project.ProjectItems
