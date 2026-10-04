FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY shareway-trip-execution-service.csproj ./

RUN dotnet restore shareway-trip-execution-service.csproj

COPY . .

RUN dotnet publish shareway-trip-execution-service.csproj \
    -c Release \
    -o /app/publish 


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "shareway-trip-execution-service.dll"]