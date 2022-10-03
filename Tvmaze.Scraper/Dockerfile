#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Tvmaze.Scraper/Tvmaze.Scraper.csproj", "Tvmaze.Scraper/"]
RUN dotnet restore "Tvmaze.Scraper/Tvmaze.Scraper.csproj"
COPY . .
WORKDIR "/src/Tvmaze.Scraper"
RUN dotnet build "Tvmaze.Scraper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Tvmaze.Scraper.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tvmaze.Scraper.dll"]
