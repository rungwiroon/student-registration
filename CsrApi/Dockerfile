FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CsrApi.csproj", "./"]
RUN dotnet restore "CsrApi.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "CsrApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CsrApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CsrApi.dll"]
