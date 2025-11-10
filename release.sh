#!/usr/bin/env bash
set -e
shopt -s extglob globstar
clear

cd "$(dirname "$(realpath "$0")")"
echo -e "\e[1;94m==== BUILD ====\e[0m"
dotnet build
echo -e "\e[1;94m====  ZIP  ====\e[0m"
mkdir -p ./zip
path="./zip/abu.AutoScan-$(date -u +%Y%m%dT%H%M%SZ).zip"
zip "$path" -jMM "icon.png" "manifest.json" "README.md" bin/**/abu.AutoScan.dll
echo -e "\e[1;94m===============\nRelease file created at \e[32m\"$path\"\e[0m"
