dotnet pack -o ../../artifacts
dotnet nuget push artifacts/*.nupkg