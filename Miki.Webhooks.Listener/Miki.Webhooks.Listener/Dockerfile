FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
EXPOSE 5000
COPY ["Miki.Webhooks.Listener/Miki.Webhooks.Listener/Miki.Webhooks.Listener.csproj", "Miki.Webhooks.Listener/"]
COPY ["Miki.Webhooks.Listener/Miki.Bot.Models/Miki.Bot.Models.csproj", "Miki.Bot.Models/"]
RUN dotnet restore "Miki.Webhooks.Listener/Miki.Webhooks.Listener.csproj"
COPY ["Miki.Webhooks.Listener/", "."]
WORKDIR "/src/Miki.Webhooks.Listener"
RUN dotnet build "Miki.Webhooks.Listener.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Miki.Webhooks.Listener.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Miki.Webhooks.Listener.dll"]