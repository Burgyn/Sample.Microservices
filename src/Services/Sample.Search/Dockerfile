#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /home/site/wwwroot
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["src/Services/Sample.Search/Sample.Search.csproj", "src/Services/Sample.Search/"]
RUN dotnet restore "src/Services/Sample.Search/Sample.Search.csproj"
COPY . .
WORKDIR "/src/src/Services/Sample.Search"
RUN dotnet build "Sample.Search.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Sample.Search.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true