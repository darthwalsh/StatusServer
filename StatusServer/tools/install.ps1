param($installPath, $toolsPath, $package, $project)

$project.ProjectItems.Item("views").ProjectItems.Item("details.html").Properties.Item("CopyToOutputDirectory").Value = 1
$project.ProjectItems.Item("views").ProjectItems.Item("index.html").Properties.Item("CopyToOutputDirectory").Value = 1
