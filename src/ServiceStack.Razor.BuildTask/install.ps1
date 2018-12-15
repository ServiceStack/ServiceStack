param($installPath, $toolsPath, $package, $project)

# Set BuildAction of Razor Views to Content
function CompileRazorViews($projectItems) {
    $projectItems | %{      
        $x = $_.Name.ToLower()
        if ($x -like "*.cshtml") {
            $_.Properties.Item("BuildAction").Value = [int]2 # Content
        }                

        # Recursively
        if ($_.ProjectItems) {
            CompileRazorViews $_.ProjectItems
        }
    }
}

# Embed Resource Files
function EmbedResources($projectItems) {
    $projectItems | %{      
        $x = $_.Name.ToLower()
        if ($x -like "*.md" -or $x -like "*.js" -or $x -like "*.css") {         
            $_.Properties.Item("BuildAction").Value = [int]3 # Embed
        }
        elseif ($x -like "*.png" -or $x -like "*.gif" -or $x -like "*.jpg" -or $x -like "*.jpeg") {
            $_.Properties.Item("BuildAction").Value = [int]3 # Embed
        }

        # Recursively
        if ($_.ProjectItems) {
            EmbedResources $_.ProjectItems
        }
    }
}

CompileRazorViews $project.ProjectItems
#EmbedResources $project.ProjectItems

