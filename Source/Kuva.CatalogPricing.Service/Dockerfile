FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Source/Kuva.CatalogPricing.slnx ./Source/
COPY Source/Kuva.CatalogPricing.sln ./Source/
COPY Source/Kuva.CatalogPricing.Entities/*.csproj ./Source/Kuva.CatalogPricing.Entities/
COPY Source/Kuva.CatalogPricing.Repository/*.csproj ./Source/Kuva.CatalogPricing.Repository/
COPY Source/Kuva.CatalogPricing.Business/*.csproj ./Source/Kuva.CatalogPricing.Business/
COPY Source/Kuva.CatalogPricing.Service/*.csproj ./Source/Kuva.CatalogPricing.Service/
RUN dotnet restore ./Source/Kuva.CatalogPricing.Service/Kuva.CatalogPricing.Service.csproj
COPY . .
RUN dotnet publish ./Source/Kuva.CatalogPricing.Service/Kuva.CatalogPricing.Service.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Kuva.CatalogPricing.Service.dll"]
