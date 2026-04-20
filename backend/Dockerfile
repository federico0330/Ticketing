FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TicketingSystem/TicketingSystem.sln", "TicketingSystem/"]
COPY ["TicketingSystem/src/TicketingSystem.Domain/TicketingSystem.Domain.csproj", "TicketingSystem/src/TicketingSystem.Domain/"]
COPY ["TicketingSystem/src/TicketingSystem.Application/TicketingSystem.Application.csproj", "TicketingSystem/src/TicketingSystem.Application/"]
COPY ["TicketingSystem/src/TicketingSystem.Infrastructure/TicketingSystem.Infrastructure.csproj", "TicketingSystem/src/TicketingSystem.Infrastructure/"]
COPY ["TicketingSystem/src/TicketingSystem.Presentation/TicketingSystem.Presentation.csproj", "TicketingSystem/src/TicketingSystem.Presentation/"]
RUN dotnet restore "TicketingSystem/TicketingSystem.sln"

COPY . .
WORKDIR "/src/TicketingSystem/src/TicketingSystem.Presentation"
RUN dotnet build "TicketingSystem.Presentation.csproj" -c Release -o /app/build
RUN dotnet publish "TicketingSystem.Presentation.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TicketingSystem.Presentation.dll"]
