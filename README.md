# XmlTimeUniqueifier
This is a simple application that runs as a Windows service and does two things:
  - Moves files from one directory to another
  - If the file is XML, it will create unique time stamp values based on previous files seem

## Install
1. Unzip the contents of the release zip file into a folder (e.g. c:\XmlTimeUniqueifier)
2. Run the install.bat script as an administrator

## Uninstall
1. Run the uninstall.bat script as an administrator

## Configuration
1. In the directory the applcation was installed in, open the XmlTimeUniqueifier.exe.config
2. In the <appSettings> tag, configure as needed:
  * SourceDirectory: Directory to read files from
  * DestinationDirectory: Directory to write files to
  * ErrorDirectory: Directory to put error files in
  * UpdateInterval: Interval to check for new files
  * HistoryLength: Number of files check in the past for duplicates (O is infinite)
  * Uniqueifier: Engine to make sure files are unique (DB: on disk persistence, Memory: in memory persistence)

## Running
To start the application, execute the start.bat script as an administrator
To stop the application, execute the stop.bat script as an administrator


