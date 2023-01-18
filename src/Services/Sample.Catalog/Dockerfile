FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80
ENV IS_DOCKER_RUN=TRUE

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["/src/Services/Sample.Catalog/Sample.Catalog.csproj", "/src/Services/Sample.Catalog/Sample.Catalog.csproj"]

RUN dotnet restore "Services/Sample.Catalog/Sample.Catalog.csproj"
# COPY . .
WORKDIR "/src/."

COPY ["/src/Services/Sample.Catalog", "/src/Services/Sample.Catalog"]
RUN dotnet build "Services/Sample.Catalog/Sample.Catalog.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Sample.Catalog/Sample.Catalog.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Catalog.dll"]
