param($installPath, $toolsPath, $package, $project)

<#
$targetsFileName = 'ServiceStack.Razor.BuildTask.targets';

# Need to load MSBuild assembly if it's not loaded yet.
Add-Type -AssemblyName 'Microsoft.Build, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'

# Grab the loaded MSBuild project for the project
$msbuild = [Microsoft.Build.Evaluation.ProjectCollection]::GlobalProjectCollection.GetLoadedProjects($project.FullName) | Select-Object -First 1

# Remove imports to .targets
$msbuild.Xml.Imports | Where-Object {$_.Project.ToLowerInvariant().EndsWith($targetsFileName.ToLowerInvariant()) } | Foreach { 
	$_.Parent.RemoveChild( $_ ) 
	[string]::Format( "Removed import of '{0}'" , $_.Project )
}
#>