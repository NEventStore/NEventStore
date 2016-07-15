@echo off
powershell -NoProfile -ExecutionPolicy unrestricted -Command "& .\build\Build.RunTask.ps1 -buildscript:git -task UpdateVersion"