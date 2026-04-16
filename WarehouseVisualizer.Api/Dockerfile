FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["WarehouseVisualizer.Api/WarehouseVisualizer.Api.csproj", "WarehouseVisualizer.Api/"]
RUN dotnet restore "WarehouseVisualizer.Api/WarehouseVisualizer.Api.csproj"

COPY . .
RUN dotnet publish "WarehouseVisualizer.Api/WarehouseVisualizer.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=0
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "WarehouseVisualizer.Api.dll"]
