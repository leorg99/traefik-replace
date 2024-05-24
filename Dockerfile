FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine-amd64 AS build-env
WORKDIR /app

# RUN apt update && apt install -y clang zlib1g-dev

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore

# Build and publish a release
WORKDIR /app/src
RUN dotnet publish -c Release -r linux-musl-x64 -p:StaticLink=true -o /app/out

FROM alpine:latest
WORKDIR /app
COPY --from=build-env /app/out /
RUN rm /*.dbg

ENTRYPOINT ["/traefik-replace"]
