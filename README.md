# Worthwhile.JarSync software syncronization utility

The repository contains core implementation of the recursive folder syncronization engine. 
It can be used to sync any hierarchical folder/file structure with the help of plugins which implement IJarTreeService.
The default WindowsFileSystem plugin can be used to sync files and folders with ability to skip folders matching a pattern.
Examples of possible extensions include: adding custom processing for source folder, encryption, archiving deleted files, backup to cloud.
By default the utility mirrors the source and excludes bin, obj and node_modules folders.
The utility can be run either as a console or a service application.

## Install and Config

- sc create "Worthwhile.JarSync" binPath="D:\...EXE_PATH...\Worthwhile.JarSync.WindowsService.exe"
- Create source and destination folders and files for testing
- Update appsettings.prod.json to include SyncSteps
- Schedule to run daily/weekly using Task Scheduler, or enable built-in scheduler in appsettings.json using "Scheduler__Enabled": true
- Provide custom implementation of IJarService and IJarItemService if needed
- To get email notifications, set environment variables or update appsettings.json:
   ***** setx Email__WORTHWHILE_COMMUNICATION_SERVICE "endpoint=https://cs-notification..."
   ***** setx Email__WORTHWHILE_NOTIFY_FROM "donotreply@mydomain.com"
   ***** setx Email__WORTHWHILE_NOTIFY_TO "targetemail@mydomain.com"

## Debugging and Testing
- Use appsettings.dev.json for debugging and testing

## Contribution

**Issues and Pull Requests are welcome.** 

To submit a pull request, you should **first [fork](https://docs.github.com/en/free-pro-team@latest/github/getting-started-with-github/fork-a-repo) the repo**.

## Package References

The utility depends on Serilog and several .NET Core 8 NuGet packages

## Uninstall service
- sc.exe delete "Worthwhile.JarSync"
