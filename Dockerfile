FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY MyMusic.Core/MyMusic.Core.csproj MyMusic.Core/
COPY MyMusic.Services/MyMusic.Services.csproj MyMusic.Services/
COPY MyMusic.Mongo.Db/MyMusic.Mongo.Db.csproj MyMusic.Mongo.Db/
COPY MyMusic.API/MyMusic.API.csproj MyMusic.API/
RUN dotnet restore MyMusic.API/MyMusic.API.csproj
COPY MyMusic.Core/ MyMusic.Core/
COPY MyMusic.Services/ MyMusic.Services/
COPY MyMusic.Mongo.Db/ MyMusic.Mongo.Db/
COPY MyMusic.API/ MyMusic.API/
RUN dotnet publish MyMusic.API/MyMusic.API.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Stateless: data lives in MongoDB Atlas, so run as the image's non-root user.
USER $APP_UID
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "MyMusic.API.dll"]
