FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80 5353/udp

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DnsChef.csproj", "./"]
RUN dotnet restore "DnsChef.csproj"
COPY . .
RUN dotnet build "DnsChef.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DnsChef.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DnsChef.dll"]