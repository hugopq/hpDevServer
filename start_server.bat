@echo off
echo A iniciar serviços Docker...
docker compose up -d
echo.
echo Serviços ativos:
docker compose ps
echo.
echo phpMyAdmin disponível em http://localhost:8080
pause