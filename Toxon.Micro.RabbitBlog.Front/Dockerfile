# Build
FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build-env

WORKDIR /app
# TODO copy just csproj for better layer caching
COPY . .
RUN dotnet restore

RUN dotnet publish Toxon.Micro.RabbitBlog.Front/Toxon.Micro.RabbitBlog.Front.csproj -c Release -o out

# Runtime
FROM mcr.microsoft.com/dotnet/core/runtime:2.2
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Toxon.Micro.RabbitBlog.Front.dll"]