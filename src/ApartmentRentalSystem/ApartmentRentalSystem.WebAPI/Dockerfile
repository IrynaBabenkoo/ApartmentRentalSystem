FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .

RUN dotnet restore "ApartmentRentalSystem/ApartmentRentalSystem.WebAPI/ApartmentRentalSystem.WebAPI.csproj"

WORKDIR "/src/ApartmentRentalSystem/ApartmentRentalSystem.WebAPI"
RUN dotnet build "ApartmentRentalSystem.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApartmentRentalSystem.WebAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ApartmentRentalSystem.WebAPI.dll"]