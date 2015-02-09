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
        $ext = [System.IO.Path]::GetExtension($_.Name.ToLower())
        $embedExts = ".js",".css",".md",".html",".htm",".png",".gif",".jpg",".jpeg",".bmp",".ico",".svg",".tiff",".webp",".webm",".xap",".xaml",".flv",".swf",".xml",".csv",".pdf",".mp3",".wav",".mpg",".ttf",".woff",".eot",".map" 

        if ($embedExts -contains $ext) {         
            $_.Properties.Item("BuildAction").Value = [int]3 # Embed
        }

        # Recursively
        if ($_.ProjectItems) {
            EmbedResources $_.ProjectItems
        }
    }
}

CompileRazorViews $project.ProjectItems
EmbedResources $project.ProjectItems

