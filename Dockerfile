# =========================
# Build stage
# =========================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/publish

# =========================
# Runtime stage
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# 🔥 Install dependency (Java + tools)
RUN apt-get update && apt-get install -y \
    curl \
    unzip \
    openjdk-17-jdk \
    && rm -rf /var/lib/apt/lists/*

# 🔥 Install JBang
RUN curl -Ls https://sh.jbang.dev | bash -s - app setup

# 🔥 Set PATH supaya jbang dikenali .NET
ENV PATH="/root/.jbang/bin:${PATH}"

# 🔥 Working directory
WORKDIR /app

# 🔥 Copy hasil publish
COPY --from=build /app/publish .

# 🔥 Expose port
EXPOSE 5001

# 🔥 ASP.NET binding
ENV ASPNETCORE_URLS=http://+:5001

# 🔥 Run app
ENTRYPOINT ["dotnet", "beresbackend.dll"]