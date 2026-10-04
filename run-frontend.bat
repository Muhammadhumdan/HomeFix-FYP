@echo off
cd HomeFix.Client
if not exist node_modules call npm install
call npm run dev
