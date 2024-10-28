FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
EXPOSE 8080
COPY publish/ app/
WORKDIR /app
ENTRYPOINT ["dotnet", "AleProjects.Cms.Web.dll"]