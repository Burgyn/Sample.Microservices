FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 5300

ENV ASPNETCORE_URLS=http://+:5300

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["/src/Services/Sample.Basket/Sample.Basket.csproj", "/src/Services/Sample.Basket/Sample.Basket.csproj"]
COPY ["/src/Libs/Sample.Basket.Domain/Sample.Basket.Domain.csproj", "/src/Libs/Sample.Basket.Domain/Sample.Basket.Domain.csproj"]

RUN dotnet restore "Services/Sample.Basket/Sample.Basket.csproj"

WORKDIR "/src/."

COPY ["/src/Libs/Sample.Basket.Domain", "/src/Libs/Sample.Basket.Domain"]
RUN dotnet build "Libs/Sample.Basket.Domain/Sample.Basket.Domain.csproj" -c Release -o /app/build

COPY ["/src/Services/Sample.Basket", "/src/Services/Sample.Basket"]
RUN dotnet build "Services/Sample.Basket/Sample.Basket.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Sample.Basket/Sample.Basket.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Basket.dll"]
