FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
USER $APP_UID
WORKDIR /app
EXPOSE 6000

FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Bookmarker/Bookmarker.csproj", "Bookmarker/"]
COPY ["PalladiumUtils/PalladiumUtils.csproj", "PalladiumUtils/"]
RUN dotnet restore "Bookmarker/Bookmarker.csproj"
COPY . .
WORKDIR "/src/Bookmarker"
RUN dotnet build "Bookmarker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Bookmarker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV ASPNETCORE_URLS=http://+:6000
ENTRYPOINT ["dotnet", "Bookmarker.dll"]
