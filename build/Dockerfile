FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base

WORKDIR /app
ENV IS_DOCKER_RUN=TRUE

COPY "/output/{PROJECT}" .
ENTRYPOINT ["dotnet", "{PROJECT}.dll"]
