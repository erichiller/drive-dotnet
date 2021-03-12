

# information on assembly
# https://github.com/dotnet/sdk/blob/1f1485fc216cd87b0289737eb7fe7de417335022/src/Tasks/Microsoft.NET.Build.Tasks/build/Microsoft.NET.DefaultAssemblyInfo.targets
# https://github.com/dotnet/sdk/blob/1f1485fc216cd87b0289737eb7fe7de417335022/src/Tasks/Microsoft.NET.Build.Tasks/build/Microsoft.NET.GenerateAssemblyInfo.targets#L56-L85

# full docs
# https://docs.microsoft.com/en-us/dotnet/core/rid-catalog


$outputPath = ( Join-Path $env:USERPROFILE "Downloads" ([io.fileinfo]$PSScriptRoot).BaseName );

Remove-Item -Force -Recurse $outputPath

Write-Output "Outputting to $outputPath";

dotnet publish `
    --configuration Release `
    --self-contained true `
    -r win10-x64 `
    -p:Version=$(Get-Date -Format "yyyy.MM.dd.HHmm-")$(git log -n1 --format=format:"%H") `
    -p:PublishSingleFile=true `
    --verbosity detailed `
    -o $outputPath
# ([io.fileinfo]$(Get-Location ).Path).BaseName

# dotnet publish -c Release --self-contained -r win10-x64 .\handle\

