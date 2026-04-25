FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copiar el archivo del proyecto y restaurar dependencias
COPY *.csproj ./
RUN dotnet restore

# Copiar el resto del código y publicar
COPY . ./
RUN dotnet publish -c Release -o out

# Crear imagen final
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/out .

# Exponer el puerto por el cual Render enviará el tráfico
EXPOSE 80

# Al arrancar, ejecutar la aplicación
ENTRYPOINT ["dotnet", "PlataformaCreditos.dll"]
