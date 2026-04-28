FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["CsrApi.csproj", "./"]
RUN dotnet restore "CsrApi.csproj" -a $TARGETARCH
COPY . .
RUN dotnet publish "CsrApi.csproj" -c Release -o /app/publish -a $TARGETARCH --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CsrApi.dll"]
