FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY JobPortalApi/JobPortalApi.csproj JobPortalApi/
RUN dotnet restore JobPortalApi/JobPortalApi.csproj
COPY JobPortalApi/ JobPortalApi/
RUN dotnet publish JobPortalApi/JobPortalApi.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "JobPortalApi.dll"]
