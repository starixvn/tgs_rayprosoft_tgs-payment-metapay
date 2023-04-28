#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
ARG BuildConfiguration=Release
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Tgs.Payment.MetaPay/Tgs.Payment.MetaPay.csproj", "Tgs.Payment.MetaPay/"]
COPY ["MetaPay.Core/MetaPay.Core.csproj", "MetaPay.Core/"]
COPY ["MetaPay.Client/MetaPay.Client.csproj", "MetaPay.Client/"]
RUN dotnet restore "Tgs.Payment.MetaPay/Tgs.Payment.MetaPay.csproj"
COPY . .
WORKDIR "/src/Tgs.Payment.MetaPay"
RUN dotnet build "Tgs.Payment.MetaPay.csproj" -c $BuildConfiguration -o /app/build

FROM build AS publish
ARG BuildConfiguration
RUN dotnet publish "Tgs.Payment.MetaPay.csproj" -c $BuildConfiguration -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Tgs.Payment.MetaPay.dll"]