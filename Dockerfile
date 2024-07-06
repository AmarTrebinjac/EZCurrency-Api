FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /App
# Configure port for https
ENV ASPNETCORE_URLS=https://+:3000;
# Expose port
EXPOSE 3000

# Copy everything
COPY . /App
# Restore as distinct layers
RUN dotnet restore
# Build and publish a release
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /App
COPY --from=build-env /App/out .
ENTRYPOINT ["dotnet", "CurrencyConverter.dll"]