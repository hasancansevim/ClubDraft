#!/usr/bin/env bash
# ClubCraft - Solution ve proje iskeletini oluşturan kurulum script'i.
# Kendi makinende (.NET 8 SDK kurulu olarak) proje kök dizininde çalıştır:
#   chmod +x setup.sh && ./setup.sh
set -e

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$ROOT_DIR"

echo "==> Solution oluşturuluyor..."
dotnet new sln -n ClubCraft --force

SERVICES="Session Draft ClubManagement MatchEngine ReputationFan FinanceSponsorship"

for svc in $SERVICES; do
  echo "==> $svc servisi için projeler oluşturuluyor..."
  SVC_DIR="src/Services/$svc"

  dotnet new classlib -n "ClubCraft.$svc.Domain" -o "$SVC_DIR/Domain" --force
  dotnet new classlib -n "ClubCraft.$svc.Application" -o "$SVC_DIR/Application" --force
  dotnet new classlib -n "ClubCraft.$svc.Infrastructure" -o "$SVC_DIR/Infrastructure" --force
  dotnet new webapi -n "ClubCraft.$svc.API" -o "$SVC_DIR/API" --force

  dotnet sln add "$SVC_DIR/Domain/ClubCraft.$svc.Domain.csproj"
  dotnet sln add "$SVC_DIR/Application/ClubCraft.$svc.Application.csproj"
  dotnet sln add "$SVC_DIR/Infrastructure/ClubCraft.$svc.Infrastructure.csproj"
  dotnet sln add "$SVC_DIR/API/ClubCraft.$svc.API.csproj"

  # Katmanlar arası referanslar (Clean Architecture yönü: API -> Infra/App -> Domain)
  dotnet add "$SVC_DIR/Application" reference "$SVC_DIR/Domain"
  dotnet add "$SVC_DIR/Infrastructure" reference "$SVC_DIR/Application"
  dotnet add "$SVC_DIR/API" reference "$SVC_DIR/Application"
  dotnet add "$SVC_DIR/API" reference "$SVC_DIR/Infrastructure"
done

echo "==> ApiGateway oluşturuluyor..."
dotnet new web -n ClubCraft.ApiGateway -o src/ApiGateway --force
dotnet sln add src/ApiGateway/ClubCraft.ApiGateway.csproj

echo "==> RealtimeHub oluşturuluyor..."
dotnet new web -n ClubCraft.RealtimeHub -o src/Services/RealtimeHub --force
dotnet sln add src/Services/RealtimeHub/ClubCraft.RealtimeHub.csproj

echo "==> BuildingBlocks projeleri oluşturuluyor..."
dotnet new classlib -n ClubCraft.BuildingBlocks.Common -o src/BuildingBlocks/Common --force
dotnet new classlib -n ClubCraft.BuildingBlocks.Contracts -o src/BuildingBlocks/Contracts --force
dotnet new classlib -n ClubCraft.BuildingBlocks.Messaging -o src/BuildingBlocks/Messaging --force

dotnet sln add src/BuildingBlocks/Common/ClubCraft.BuildingBlocks.Common.csproj
dotnet sln add src/BuildingBlocks/Contracts/ClubCraft.BuildingBlocks.Contracts.csproj
dotnet sln add src/BuildingBlocks/Messaging/ClubCraft.BuildingBlocks.Messaging.csproj

dotnet add src/BuildingBlocks/Messaging reference src/BuildingBlocks/Contracts

echo ""
echo "✅ Kurulum tamamlandı. 'dotnet build' ile derleyebilirsin."
echo "Not: NuGet paketlerini (MediatR, FluentValidation, MassTransit, EF Core vb.)"
echo "her servisin ihtiyacına göre ayrıca 'dotnet add package ...' ile eklemen gerekiyor."
