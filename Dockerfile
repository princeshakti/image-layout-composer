# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ImageLayoutComposer/ImageLayoutComposer.csproj ImageLayoutComposer/
RUN dotnet restore ImageLayoutComposer/ImageLayoutComposer.csproj

COPY ImageLayoutComposer/ ImageLayoutComposer/
RUN dotnet publish ImageLayoutComposer/ImageLayoutComposer.csproj \
    -c Release -o /app/publish --no-restore

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p wwwroot/uploads wwwroot/outputs

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ImageLayoutComposer.dll"]
