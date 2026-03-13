FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/BillingExtractor.Api/BillingExtractor.Api.csproj", "src/BillingExtractor.Api/"]
RUN dotnet restore "src/BillingExtractor.Api/BillingExtractor.Api.csproj"
COPY . .
RUN dotnet build "src/BillingExtractor.Api/BillingExtractor.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/BillingExtractor.Api/BillingExtractor.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BillingExtractor.Api.dll"]
