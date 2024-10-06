FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["AuthService/AuthAPI/AuthAPI.csproj", "AuthAPI/"]
COPY ["AuthService/AuthService.Core/AuthService.Core.csproj", "AuthService.Core/"]
COPY ["AuthService/AuthService.Application/AuthService.Application.csproj", "AuthService.Application/"]
COPY ["AuthService/AuthService.Infrastructure/AuthService.Infrastructure.csproj", "AuthService.Infrastructure/"]

RUN dotnet restore "./AuthAPI/AuthAPI.csproj"

COPY . .

WORKDIR "/src"
RUN dotnet build "AuthService/AuthAPI/AuthAPI.csproj" -c Release -o /app/build
RUN dotnet publish "AuthService/AuthAPI/AuthAPI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthAPI.dll"]