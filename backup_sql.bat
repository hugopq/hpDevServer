@echo off
echo A fazer backup de todas as bases de dados...
docker exec mysql_central mysqldump -u root -proot --all-databases > backup_%date:~6,4%%date:~3,2%%date:~0,2%.sql
echo Backup concluído! Ficheiro: backup_%date:~6,4%%date:~3,2%%date:~0,2%.sql
pause