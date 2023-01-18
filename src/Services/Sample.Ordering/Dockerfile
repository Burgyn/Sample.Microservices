FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80
ENV IS_DOCKER_RUN=TRUE

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["/src/Services/Sample.Ordering/Sample.Ordering.csproj", "/src/Services/Sample.Ordering/Sample.Ordering.csproj"]

RUN dotnet restore "Services/Sample.Ordering/Sample.Ordering.csproj"
# COPY . .
WORKDIR "/src/."

COPY ["/src/Services/Sample.Ordering", "/src/Services/Sample.Ordering"]
RUN dotnet build "Services/Sample.Ordering/Sample.Ordering.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Services/Sample.Ordering/Sample.Ordering.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Sample.Ordering.dll"]
