FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Oyster/Oyster.csproj", "Oyster/"]
RUN dotnet restore "Oyster/Oyster.csproj"
COPY . .
WORKDIR "/src/Oyster"
RUN dotnet build "Oyster.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Oyster.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Oyster.dll"]
