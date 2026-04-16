# ETAPA DE CONSTRUCCIÓN
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copia el archivo de proyecto e instala dependencias
COPY ["TFG_Back.csproj", "./"]
RUN dotnet restore "TFG_Back.csproj"

# Copia todo el código y publica
COPY . .
RUN dotnet publish "TFG_Back.csproj" -c Release -o /app/publish

# ETAPA DE EJECUCIÓN
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Configurar puerto dinámico para Render
ENV ASPNETCORE_URLS=http://+:$PORT

# Exponer puerto dinámico
EXPOSE $PORT

ENTRYPOINT ["dotnet", "TFG_Back.dll"]
