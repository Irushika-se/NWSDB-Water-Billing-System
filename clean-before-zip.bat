@echo off
REM Run this before compressing the project to a RAR: removes build output.
if exist NWSDB.API\bin rmdir /s /q NWSDB.API\bin
if exist NWSDB.API\obj rmdir /s /q NWSDB.API\obj
if exist QuickPay.PartnerApp\bin rmdir /s /q QuickPay.PartnerApp\bin
if exist QuickPay.PartnerApp\obj rmdir /s /q QuickPay.PartnerApp\obj
if exist .vs rmdir /s /q .vs
echo Done. bin/obj removed - now compress this folder to RAR.
pause
