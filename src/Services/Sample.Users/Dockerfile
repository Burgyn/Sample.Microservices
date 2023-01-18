FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80
ENV IS_DOCKER_RUN=TRUE

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["/src/Services/Sample.Users/Sample.Users.csproj", "/src/Services/Sample.Users/Sample.Users.csproj"]
COPY ["/src/Libs/Sample.Users.Domain/Sample.Users.Domain.csproj", "/src/Libs/Sample.Users.Domain/Sample.Users.Domain.csproj"]

RUN dotnet restore "Services/Sample.Users/Sample.Users.csproj"
# COPY . .
WORKDIR "/src/."

COPY ["/src/Libs/Sample.Users.Domain", "/src/Libs/Sample.Users.Domain"]
RUN dotnet build "Libs/Sample.Users.Domain/Sample.Users.Domain.csproj" -c Release -o /app/build

COPY ["/src/Services/Sample.Users", "/src/Services/Sample.Users"]
RUN dotnet build "Services/Sample.Users/Sample.Users.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Sample.Users/Sample.Users.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Users.dll"]
