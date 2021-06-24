FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 5000

ENV ASPNETCORE_URLS=http://+:5000
ENV IS_DOCKER_RUN=TRUE

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["nuget.config", "/src/Gateways/Sample.ApiGateway/"]
COPY ["/src/Gateways/Sample.ApiGateway/Sample.ApiGateway.csproj", "/src/Gateways/Sample.ApiGateway/"]
COPY ["/src/Libs/Sample.Users.Domain/Sample.Users.Domain.csproj", "/src/Libs/Sample.Users.Domain/Sample.Users.Domain.csproj"]
COPY ["/src/Libs/Sample.Basket.Domain/Sample.Basket.Domain.csproj", "/src/Libs/Sample.Basket.Domain/Sample.Basket.Domain.csproj"]

RUN dotnet restore "Gateways\Sample.ApiGateway\Sample.ApiGateway.csproj"

WORKDIR "/src/."

COPY ["/src/Libs/Sample.Basket.Domain", "/src/Libs/Sample.Basket.Domain"]
COPY ["/src/Libs/Sample.Users.Domain", "/src/Libs/Sample.Users.Domain"]
COPY ["/src/Gateways/Sample.ApiGateway", "/src/Gateways/Sample.ApiGateway"]

RUN dotnet build "Libs/Sample.Basket.Domain/Sample.Basket.Domain.csproj" -c Release -o /app/build
RUN dotnet build "Libs/Sample.Users.Domain/Sample.Users.Domain.csproj" -c Release -o /app/build

RUN dotnet build "Gateways/Sample.ApiGateway/Sample.ApiGateway.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Gateways/Sample.ApiGateway/Sample.ApiGateway.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.ApiGateway.dll"]
