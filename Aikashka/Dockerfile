FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Aikashka/Aikashka.csproj", "Aikashka/"]
RUN dotnet restore "Aikashka/Aikashka.csproj"
COPY . .
WORKDIR "/src/Aikashka"
RUN dotnet build "Aikashka.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Aikashka.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Aikashka.dll"]
