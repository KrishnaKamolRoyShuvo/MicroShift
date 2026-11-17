FROM mcr.microsoft.com/dotnet/sdk:10.0-preview-alpine AS build
WORKDIR /src

COPY ["MicroShift/MicroShift.csproj", "MicroShift/"]
RUN dotnet restore "MicroShift/MicroShift.csproj"

COPY . .
WORKDIR "/src/MicroShift"
RUN dotnet publish "MicroShift.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview-alpine AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MicroShift.dll"]
