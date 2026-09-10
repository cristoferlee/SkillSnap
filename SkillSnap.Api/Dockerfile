FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["SkillSnap.Api/SkillSnap.Api.csproj", "SkillSnap.Api/"]
COPY ["SkillSnap.Client/SkillSnap.Client.csproj", "SkillSnap.Client/"]
COPY ["SkillSnap.Contracts/SkillSnap.Contracts.csproj", "SkillSnap.Contracts/"]

RUN dotnet restore "SkillSnap.Api/SkillSnap.Api.csproj"

COPY . .
WORKDIR "/src/SkillSnap.Api"

RUN dotnet publish "SkillSnap.Api.csproj" --configuration Release --output /app/publish --no-restore /p:UseAppHost=false

FROM runtime AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SkillSnap.Api.dll"]